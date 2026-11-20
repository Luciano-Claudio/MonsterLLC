#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Unity.Profiling;
using Unity.Profiling.LowLevel;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.InputSystem;

// D9.2 — TESTE DE DENSIDADE DA HORDA (DESCARTÁVEL — apagar depois do relatório, não entra no jogo).
//
// Pergunta: quantos monstros vivos o jogo aguenta hoje e o que trava primeiro?
// Só existe no Editor (#if UNITY_EDITOR) e nasce sozinho em qualquer cena com herói
// (RuntimeInitializeOnLoadMethod) — nenhuma cena/prefab é alterada.
//
// Teclas (em Play Mode, com uma run começada e o herói em jogo):
//   F8  = começa a sequência completa (ou pula pra próxima etapa se já estiver rodando)
//   F9  = aborta e restaura tudo
// Etapas: 100 / 200 / 300 monstros com o herói PARADO, depois as mesmas com o herói ANDANDO EM
// CÍRCULO, depois 200 monstros + 200 e 500 "projéteis/efeitos" falsos (sprite animado + trigger
// físico se movendo). Cada etapa: enche a população, 3s de aquecimento, 12s medindo.
// Durante o teste: o herói não morre (vida cheia todo frame) e o cronômetro do dia não corre.
// O relatório vai pra <projeto>/DensityReports/density_<data>.md e pro Console.
public class HordeDensityTest : MonoBehaviour
{
    private static readonly int[] MonsterCounts = { 100, 200, 300 };
    private static readonly int[] EffectCounts = { 200, 500 };
    private const int EffectsStageMonsters = 200;
    private const float FillTimeout = 25f;
    private const float WarmupSeconds = 3f;
    private const float MeasureSeconds = 12f;

    // Marcadores do Profiler que interessam (casam por "contém" no nome; descobertos em runtime,
    // Marcadores de nível alto, por nome EXATO (scripts, física, Animator, render, GC, criar objeto).
    private static readonly string[] ExactMarkers =
    {
        "PlayerLoop",
        "Update.ScriptRunBehaviourUpdate", "PreLateUpdate.ScriptRunBehaviourLateUpdate", "FixedUpdate.ScriptRunBehaviourFixedUpdate",
        "FixedUpdate.Physics2DFixedUpdate", "Physics2D.Simulate", "PreUpdate.Physics2DUpdate", "PreLateUpdate.Physics2DLateUpdate",
        "PreLateUpdate.DirectorUpdateAnimationBegin", "PreLateUpdate.DirectorUpdateAnimationEnd", "Update.DirectorUpdate",
        "PostLateUpdate.FinishFrameRendering", "UniversalRenderTotal", "Gfx.WaitForPresentOnGfxThread", "Gfx.WaitForPresent",
        "GC.Collect", "Instantiate", "Destroy"
    };
    // A* Pathfinding Project: nomes não confirmados — casa por "contém", sem subtarefas/jobs.
    private static readonly string[] PathfindingKeywords = { "RVO", "Pathfinding", "AstarPath", "Seeker", "AIPath" };
    private static readonly string[] NoiseWords = { "Job", "Task", "Burst", "Cleanup", "Debug", "Gizmo", "Editor", "Scene" };

    private class StageResult
    {
        public string name;
        public int monstersTarget, monstersAlive, effects, rvoEnabled, aiEnabled;
        public readonly List<float> frameMs = new List<float>();
        public readonly Dictionary<string, double> markerMsSum = new Dictionary<string, double>();
        public double gcBytesSum;
        public int frames;
    }

    private readonly List<StageResult> results = new List<StageResult>();
    private readonly List<(string name, ProfilerRecorder rec)> recorders = new List<(string, ProfilerRecorder)>();
    private ProfilerRecorder gcRecorder;
    private Coroutine running;
    private MonsterSpawnSettings originalSettings;
    private string status = "F8 = começar teste de densidade";
    private string instruction = "";
    private readonly List<GameObject> effects = new List<GameObject>();
    private RuntimeAnimatorController effectController;
    private bool frozenTimer;
    private float frozenTimeRemaining;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        var go = new GameObject("[HordeDensityTest]");
        DontDestroyOnLoad(go);
        go.AddComponent<HordeDensityTest>();
    }

    private void Awake()
    {
        effectController = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animation/Status/Status.controller");
    }

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb != null && kb.f8Key.wasPressedThisFrame)
        {
            if (running == null) running = StartCoroutine(RunAll());
            else skipStage = true;
        }
        if (kb != null && kb.f9Key.wasPressedThisFrame) Abort("abortado (F9)");

        if (running == null) return;
        KeepHeroAlive();
        if (frozenTimer && DayTimer.Instance != null) DayTimer.Instance.timeRemaining = frozenTimeRemaining;
    }

    private bool skipStage;

    private IEnumerator RunAll()
    {
        var pop = WorldPopulationManager.Instance;
        if (pop == null || HeroController.Instance == null)
        {
            status = "Comece uma run (herói em jogo) antes de apertar F8.";
            running = null;
            yield break;
        }

        results.Clear();
        StartRecorders();
        originalSettings = pop.spawnSettings;
        pop.spawnSettings = Instantiate(originalSettings); // cópia: nunca altera o asset
        pop.spawnSettings.maxCrisisSpawnsPerFrame = 20;
        pop.spawnSettings.enemyDespawnDistance = Mathf.Max(pop.spawnSettings.enemyDespawnDistance, 60f);
        if (DayTimer.Instance != null) { frozenTimer = true; frozenTimeRemaining = DayTimer.Instance.timeRemaining; }

        foreach (int n in MonsterCounts)
            yield return RunStage($"{n} monstros — herói PARADO", n, 0, "Fique PARADO (não ande, não ataque).");
        foreach (int n in MonsterCounts)
            yield return RunStage($"{n} monstros — herói ANDANDO", n, 0, "ANDE EM CÍRCULO o tempo todo (sem atacar). Você atravessa os monstros nesta etapa.", passThroughMonsters: true);
        foreach (int k in EffectCounts)
            yield return RunStage($"{EffectsStageMonsters} monstros + {k} efeitos", EffectsStageMonsters, k, "Fique PARADO.");

        string path = WriteReport();
        Restore();
        status = $"PRONTO — relatório em {path}";
        instruction = "";
        running = null;
    }

    private static void TrimPopulation(int target)
    {
        var enemies = new List<EnemyController>(FindObjectsByType<EnemyController>(FindObjectsSortMode.None));
        enemies.RemoveAll(e => e.IsDead || e.IsBoss);
        if (enemies.Count <= target) return;
        var hero = GameObject.FindGameObjectWithTag("Player");
        Vector2 heroPos = hero != null ? (Vector2)hero.transform.position : Vector2.zero;
        enemies.Sort((a, b) => ((Vector2)b.transform.position - heroPos).sqrMagnitude
            .CompareTo(((Vector2)a.transform.position - heroPos).sqrMagnitude));
        for (int i = 0; i < enemies.Count - target; i++) Destroy(enemies[i].gameObject);
    }

    private IEnumerator RunStage(string name, int monsters, int effectCount, string todo, bool passThroughMonsters = false)
    {
        // Etapas andando: com 300 monstros grudados os colliders prendem o herói — desliga só a
        // colisão herói x monstro (mesmo recurso da Coruja do Druid); monstros seguem perseguindo.
        SetHeroPassThrough(passThroughMonsters);
        var pop = WorldPopulationManager.Instance;
        pop.spawnSettings.populationMinimum = monsters;
        pop.spawnSettings.populationTarget = monsters;
        pop.spawnSettings.populationMaximum = monsters;
        SetEffects(effectCount);
        instruction = todo;
        skipStage = false;

        // Correção do D9.2: baixar a meta não some com quem já existe — sem isso, as etapas
        // depois da de 300 rodavam com 300. Destrói o excedente (mais longe do herói primeiro);
        // o WorldPopulationManager limpa as referências nulas sozinho.
        TrimPopulation(monsters);
        yield return null;

        float t = 0f;
        while (pop.AliveCount < monsters * 0.95f && t < FillTimeout && !skipStage)
        {
            status = $"{name}: enchendo ({pop.AliveCount}/{monsters})";
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        t = 0f;
        while (t < WarmupSeconds && !skipStage) { status = $"{name}: aquecendo"; t += Time.unscaledDeltaTime; yield return null; }

        var r = new StageResult { name = name, monstersTarget = monsters, effects = effectCount };
        t = 0f;
        while (t < MeasureSeconds && !skipStage)
        {
            status = $"{name}: MEDINDO {MeasureSeconds - t:0}s";
            float ms = Time.unscaledDeltaTime * 1000f;
            r.frameMs.Add(ms);
            r.frames++;
            foreach (var (markerName, rec) in recorders)
                if (rec.Valid) { r.markerMsSum.TryGetValue(markerName, out double s); r.markerMsSum[markerName] = s + rec.LastValue / 1e6; }
            if (gcRecorder.Valid) r.gcBytesSum += gcRecorder.LastValue;
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        r.monstersAlive = pop.AliveCount;
        CountAgents(r);
        results.Add(r);
        skipStage = false;
    }

    private void CountAgents(StageResult r)
    {
        foreach (var rvo in FindObjectsByType<Pathfinding.RVO.RVOController>(FindObjectsSortMode.None))
            if (rvo.enabled) r.rvoEnabled++;
        foreach (var ai in FindObjectsByType<Pathfinding.AIPath>(FindObjectsSortMode.None))
            if (ai.enabled && !ai.isStopped) r.aiEnabled++;
    }

    // "Projéteis/efeitos" falsos: sprite com Animator em loop (clipe Fire do Status.controller) +
    // Rigidbody2D cinemático com trigger, girando em volta do herói — custo de render + Animator +
    // física de trigger, como os itens/efeitos da Deadline 9 vão pôr na tela.
    private void SetEffects(int count)
    {
        while (effects.Count > count) { Destroy(effects[effects.Count - 1]); effects.RemoveAt(effects.Count - 1); }
        var hero = HeroController.Instance;
        while (effects.Count < count && hero != null)
        {
            var go = new GameObject("DensityEffect");
            // D9.3: na layer dos ataques do herói (colide só com Enemy), igual aos projéteis
            // reais depois da matriz de colisão. No D9.2 estava na Default (colide com tudo).
            int heroAttackLayer = LayerMask.NameToLayer("HeroAttack");
            if (heroAttackLayer >= 0) go.layer = heroAttackLayer;
            go.transform.position = hero.transform.position + (Vector3)(Random.insideUnitCircle * 8f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 50;
            var anim = go.AddComponent<Animator>();
            anim.runtimeAnimatorController = effectController;
            if (effectController != null) anim.Play("Fire");
            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.4f;
            go.AddComponent<DensityEffectMover>().Init(hero.transform, Random.Range(2f, 9f), Random.Range(-3f, 3f));
            effects.Add(go);
        }
    }

    private bool passThroughActive;

    private void SetHeroPassThrough(bool enabled)
    {
        if (passThroughActive == enabled) return;
        var hero = HeroController.Instance;
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (hero == null || enemyLayer < 0) return;
        Physics2D.IgnoreLayerCollision(hero.gameObject.layer, enemyLayer, enabled);
        passThroughActive = enabled;
    }

    private static void KeepHeroAlive()
    {
        var hero = HeroController.Instance;
        if (hero == null) return;
        hero.stats.health = hero.EffectiveMaxHealth;
    }

    private void StartRecorders()
    {
        StopRecorders();
        var handles = new List<ProfilerRecorderHandle>();
        ProfilerRecorderHandle.GetAvailable(handles);
        var seen = new HashSet<string>();
        foreach (var h in handles)
        {
            var d = ProfilerRecorderHandle.GetDescription(h);
            if (d.UnitType != ProfilerMarkerDataUnit.TimeNanoseconds) continue;
            string n = d.Name;
            if (seen.Contains(n)) continue;
            bool match = System.Array.IndexOf(ExactMarkers, n) >= 0;
            if (!match)
            {
                foreach (var k in PathfindingKeywords) if (n.Contains(k)) { match = true; break; }
                if (match) foreach (var bad in NoiseWords) if (n.Contains(bad)) { match = false; break; }
            }
            if (!match) continue;
            seen.Add(n);
            recorders.Add((n, ProfilerRecorder.StartNew(d.Category, n, 1)));
            if (recorders.Count >= 60) break;
        }
        gcRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame", 1);
    }

    private void StopRecorders()
    {
        foreach (var (_, rec) in recorders) rec.Dispose();
        recorders.Clear();
        if (gcRecorder.Valid) gcRecorder.Dispose();
    }

    private string WriteReport()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# Teste de densidade da horda — {System.DateTime.Now:yyyy-MM-dd HH:mm}");
        sb.AppendLine();
        sb.AppendLine("Medido no Editor (tem overhead do Editor; serve pra comparar etapas e achar o gargalo, não como FPS final).");
        sb.AppendLine();
        sb.AppendLine("| Etapa | Vivos | FPS médio | ms médio | ms p95 | ms p99 | pior ms | GC KB/frame | AIPath andando | RVO ligado |");
        sb.AppendLine("|---|---|---|---|---|---|---|---|---|---|");
        foreach (var r in results)
        {
            if (r.frames == 0) continue;
            var sorted = new List<float>(r.frameMs); sorted.Sort();
            float avg = 0f; foreach (var f in sorted) avg += f; avg /= sorted.Count;
            float p95 = sorted[Mathf.Min(sorted.Count - 1, (int)(sorted.Count * 0.95f))];
            float p99 = sorted[Mathf.Min(sorted.Count - 1, (int)(sorted.Count * 0.99f))];
            sb.AppendLine($"| {r.name} | {r.monstersAlive} | {1000f / avg:0} | {avg:0.0} | {p95:0.0} | {p99:0.0} | {sorted[sorted.Count - 1]:0.0} | {r.gcBytesSum / r.frames / 1024.0:0.0} | {r.aiEnabled} | {r.rvoEnabled} |");
        }
        foreach (var r in results)
        {
            if (r.frames == 0) continue;
            sb.AppendLine();
            sb.AppendLine($"## {r.name} — marcadores do Profiler (ms médio por frame, top 15)");
            var list = new List<KeyValuePair<string, double>>(r.markerMsSum);
            list.Sort((a, b) => b.Value.CompareTo(a.Value));
            sb.AppendLine("| Marcador | ms/frame |");
            sb.AppendLine("|---|---|");
            for (int i = 0; i < Mathf.Min(15, list.Count); i++)
                sb.AppendLine($"| {list[i].Key} | {list[i].Value / r.frames:0.00} |");
        }

        string dir = Path.Combine(Application.dataPath, "..", "DensityReports");
        Directory.CreateDirectory(dir);
        string path = Path.GetFullPath(Path.Combine(dir, $"density_{System.DateTime.Now:yyyyMMdd_HHmmss}.md"));
        File.WriteAllText(path, sb.ToString());
        Debug.Log($"[HordeDensityTest] Relatório salvo em {path}\n{sb}");
        return path;
    }

    private void Abort(string reason)
    {
        if (running != null) StopCoroutine(running);
        running = null;
        if (results.Count > 0) WriteReport(); // relatório parcial: as etapas já medidas não se perdem
        Restore();
        status = $"Teste {reason}. F8 = começar de novo";
        instruction = "";
    }

    private void Restore()
    {
        SetHeroPassThrough(false);
        var pop = WorldPopulationManager.Instance;
        if (pop != null && originalSettings != null) pop.spawnSettings = originalSettings;
        originalSettings = null;
        SetEffects(0);
        frozenTimer = false;
        StopRecorders();
    }

    private void OnDestroy() => Restore();

    private void OnGUI()
    {
        var style = new GUIStyle(GUI.skin.box) { fontSize = 16, alignment = TextAnchor.UpperLeft, wordWrap = true };
        int alive = WorldPopulationManager.Instance != null ? WorldPopulationManager.Instance.AliveCount : 0;
        string text = $"[Teste de densidade]  FPS {1f / Mathf.Max(Time.unscaledDeltaTime, 0.0001f):0}  |  monstros vivos {alive}\n{status}";
        if (!string.IsNullOrEmpty(instruction)) text += $"\n>>> {instruction}";
        text += "\nF8 = começar / pular etapa   F9 = abortar";
        GUI.Box(new Rect(10, 10, 560, 110), text, style);
    }
}

// Move o efeito falso em círculo em volta do herói (via Rigidbody2D, como um projétil de verdade).
public class DensityEffectMover : MonoBehaviour
{
    private Transform center;
    private float radius, angularSpeed, angle;
    private Rigidbody2D body;

    public void Init(Transform centerTransform, float orbitRadius, float speed)
    {
        center = centerTransform;
        radius = orbitRadius;
        angularSpeed = Mathf.Abs(speed) < 0.5f ? 1f : speed;
        angle = Random.value * Mathf.PI * 2f;
        body = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        if (center == null || body == null) return;
        angle += angularSpeed * Time.fixedDeltaTime;
        body.MovePosition((Vector2)center.position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
    }
}
#endif
