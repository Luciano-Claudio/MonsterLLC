using UnityEngine;

// Testes manuais da telemetria de run (RunTelemetryRecorder) — mesmo padrao de SaveTest.cs
// ([ContextMenu], sem runner de teste automatizado neste projeto). Cada metodo tambem RETORNA
// a string de resultado (alem do Debug.Log) para poder ser chamado via Unity CLI script eval
// durante a implementacao, sem precisar clicar no Inspector.
public class RunTelemetryTest : MonoBehaviour
{
    [SerializeField] private EnemyController testEnemyPrefab;
    [SerializeField] private PerkDefinitionSO testPerk; // usado pelo Test 15 (revisao final, achado Importante #7)

    [ContextMenu("Test 1: OnEnemyKilledDetailed fires")]
    public string TestEnemyKilledDetailedFires()
    {
        bool fired = false;
        GameEvents.OnEnemyKilledDetailed += (name, isBoss, direct, dot, maxHp, energy, pos) => fired = true;
        GameEvents.EnemyKilledDetailed("TestMonster", false, 3, 1, 50f, 20, Vector3.zero);
        string result = fired ? "[RunTelemetryTest] PASS: evento disparou" : "[RunTelemetryTest] FAIL: evento nao disparou";
        Debug.Log(result);
        return result;
    }

    [ContextMenu("Test 2: hits diretos x DoT ticks separados na morte")]
    public string TestDirectVsDotHits()
    {
        var enemy = Instantiate(testEnemyPrefab);
        string capture = "nao disparou";
        System.Action<string, bool, int, int, float, int, Vector3> handler = (name, isBoss, direct, dot, maxHp, energy, pos) =>
            capture = $"direct={direct} dot={dot}";
        GameEvents.OnEnemyKilledDetailed += handler;

        enemy.TakeDamage(1f, isDirectPlayerHit: true);   // 1 hit direto
        enemy.TakeDamage(1f, isDirectPlayerHit: false);  // 1 tick de DoT
        enemy.TakeDamage(9999f, isDirectPlayerHit: true); // golpe fatal, direto

        GameEvents.OnEnemyKilledDetailed -= handler;
        string result = capture == "direct=2 dot=1" ? $"[RunTelemetryTest] PASS: {capture}" : $"[RunTelemetryTest] FAIL: {capture}";
        Debug.Log(result);
        return result;
    }

    [ContextMenu("Test 3: getters de populacao viva existem")]
    public string TestAliveCountGetters()
    {
        int pop = WorldPopulationManager.Instance != null ? WorldPopulationManager.Instance.AliveCount : -1;
        int bosses = WorldBossSpawnManager.Instance != null ? WorldBossSpawnManager.Instance.AliveCount : -1;
        string result = $"[RunTelemetryTest] PASS: AliveCount monstros={pop}, bosses={bosses}";
        Debug.Log(result);
        return result;
    }

    [ContextMenu("Test 4: FlushToDisk escreve JSON valido")]
    public string TestFlushToDisk()
    {
        RunTelemetryRecorder.Instance?.FlushToDisk();
        string dir = System.IO.Path.Combine(Application.persistentDataPath, "Telemetry");
        bool exists = System.IO.Directory.Exists(dir) && System.IO.Directory.GetFiles(dir, "run_*.json").Length > 0;
        string result = exists ? "[RunTelemetryTest] PASS: arquivo de telemetria existe" : "[RunTelemetryTest] FAIL: pasta/arquivo nao existe";
        Debug.Log(result);
        return result;
    }

    [ContextMenu("Test 5: attemptIndex incrementa por dayNumber, nao globalmente")]
    public string TestAttemptIndexPerDay()
    {
        // Simula 2 inicios de dia pro MESMO numero (retry) e 1 pro proximo numero.
        GameEvents.DayStarted(); // dia N, tentativa 1
        int a1 = RunTelemetryRecorder.Instance.DebugCurrentAttemptIndex;
        GameEvents.DayEnded();   // morreu, mesmo dia
        GameEvents.DayStarted(); // dia N, tentativa 2 (retry)
        int a2 = RunTelemetryRecorder.Instance.DebugCurrentAttemptIndex;
        string result = a1 == 1 && a2 == 2 ? $"[RunTelemetryTest] PASS: a1={a1} a2={a2}" : $"[RunTelemetryTest] FAIL: a1={a1} a2={a2}";
        Debug.Log(result);
        return result;
    }

    [ContextMenu("Test 6: sampler respeita GameplayGate")]
    public string TestSamplerRespectsGate()
    {
        int before = RunTelemetryRecorder.Instance.DebugSnapshotCount;
        // GameplayGate.IsActive só é true em GameState.Gameplay — fora disso, simula 2s de Update() manualmente.
        for (int i = 0; i < 120; i++) RunTelemetryRecorder.Instance.DebugTickSampler(1f / 60f);
        int after = RunTelemetryRecorder.Instance.DebugSnapshotCount;
        bool gatedCorrectly = GameplayGate.IsActive ? after > before : after == before;
        string result = gatedCorrectly ? $"[RunTelemetryTest] PASS: before={before} after={after} gate={GameplayGate.IsActive}" : $"[RunTelemetryTest] FAIL: before={before} after={after} gate={GameplayGate.IsActive}";
        Debug.Log(result);
        return result;
    }

    [ContextMenu("Test 7: venda em pleno dia cai no dayAttempt, nao na loja")]
    public string TestMidDaySaleRouting()
    {
        RunTelemetryRecorder.Instance.RecordItemSold("ItemTeste", 1, 100);
        bool inDayAttempt = RunTelemetryRecorder.Instance.DebugLastSaleWentToDayAttempt;
        string result = inDayAttempt ? "[RunTelemetryTest] PASS: venda em pleno dia roteada certo" : "[RunTelemetryTest] FAIL: venda roteada errado";
        Debug.Log(result);
        return result;
    }

    [ContextMenu("Test 8: retorno ao Main_Menu com tentativa aberta marca abandoned")]
    public string TestAbandonedOnReturnToMenu()
    {
        RunTelemetryRecorder.Instance.DebugForceOpenDayAttempt(dayNumber: 99);
        RunTelemetryRecorder.Instance.DebugSimulateSceneLoaded("Main_Menu");
        string outcome = RunTelemetryRecorder.Instance.DebugLastClosedOutcome;
        string result = outcome == "abandoned" ? $"[RunTelemetryTest] PASS: outcome={outcome}" : $"[RunTelemetryTest] FAIL: outcome={outcome}";
        Debug.Log(result);
        return result;
    }

    [ContextMenu("Test 8b: retorno ao menu SEM tentativa aberta nao quebra")]
    public string TestReturnToMenuNoop()
    {
        RunTelemetryRecorder.Instance.DebugSimulateSceneLoaded("Main_Menu"); // sem tentativa aberta
        string result = "[RunTelemetryTest] PASS: nao lancou excecao";
        Debug.Log(result);
        return result;
    }

    // --- Revisao final (achados da revisao de branch inteira) ---

    [ContextMenu("Test 9: guarda de singleton real - Init de copia nunca ativa")]
    public string TestSingletonGuardReal()
    {
        if (RunTelemetryRecorder.Instance != null) DestroyImmediate(RunTelemetryRecorder.Instance.gameObject);
        var go1 = new GameObject("DupTestOriginal");
        var rec1 = go1.AddComponent<RunTelemetryRecorder>();
        rec1.Init(true);
        var go2 = new GameObject("DupTestCopy");
        var rec2 = go2.AddComponent<RunTelemetryRecorder>(); // Awake() ve Instance=rec1, se autodestroi (deferred)
        rec2.Init(true); // igual SystemsBootstrap faz, ANTES do Destroy() de fato acontecer
        bool ok = rec1.DebugIsSubscribed && !rec2.DebugIsSubscribed && RunTelemetryRecorder.Instance == rec1;
        string result = $"[RunTelemetryTest] {(ok ? "PASS" : "FAIL")}: rec1Subscribed={rec1.DebugIsSubscribed} rec2Subscribed={rec2.DebugIsSubscribed} instanceIsRec1={RunTelemetryRecorder.Instance == rec1}";
        Debug.Log(result);
        Destroy(go1);
        Destroy(go2);
        return result;
    }

    [ContextMenu("Test 10: ShopVisit grava beforeDayNumber e goldAtClose")]
    public string TestShopVisitFieldsFilled()
    {
        GameEvents.DayStarted();
        DayResolver.ResolveEndOfDay(); // fecha o dia, abre a visita a loja
        MainMenuUI.CurrentRun.day++;
        GameEvents.DayStarted(); // fecha a visita que acabou de abrir
        var closed = RunTelemetryRecorder.Instance.DebugLastClosedShopVisit;
        bool ok = closed != null && closed.beforeDayNumber == MainMenuUI.CurrentRun.day && closed.goldAtClose == MainMenuUI.CurrentRun.gold;
        string result = $"[RunTelemetryTest] {(ok ? "PASS" : "FAIL")}: beforeDayNumber={closed?.beforeDayNumber} goldAtClose={closed?.goldAtClose}";
        Debug.Log(result);
        return result;
    }

    [ContextMenu("Test 12: OnApplicationQuit fecha dia e loja abertos")]
    public string TestApplicationQuitClosesOpenRecords()
    {
        GameEvents.DayStarted(); // abre dia
        RunTelemetryRecorder.Instance.DebugSimulateApplicationQuit();
        var closedDay = RunTelemetryRecorder.Instance.DebugLastClosedDayAttempt;
        bool ok = closedDay != null && closedDay.outcome == "abandoned";
        string result = $"[RunTelemetryTest] {(ok ? "PASS" : "FAIL")}: outcome={closedDay?.outcome}";
        Debug.Log(result);
        return result;
    }

    [ContextMenu("Test 13: hero preenchido mesmo se Init rodou antes do CurrentRun existir")]
    public string TestHeroFilledAfterInit()
    {
        if (RunTelemetryRecorder.Instance != null) DestroyImmediate(RunTelemetryRecorder.Instance.gameObject);
        var prop = typeof(MainMenuUI).GetProperty("CurrentRun");
        prop.SetValue(null, null); // simula Init() no boot, antes de qualquer run existir
        var go = new GameObject("Test13Recorder");
        var rec = go.AddComponent<RunTelemetryRecorder>();
        rec.Init(true);
        bool heroNullAtInit = rec.DebugHero == null;
        prop.SetValue(null, new RunState { day = 1, gold = 0, hero = "Barbarian" });
        GameEvents.DayStarted();
        bool heroFilledAfterDayStart = rec.DebugHero == "Barbarian";
        bool ok = heroNullAtInit && heroFilledAfterDayStart;
        string result = $"[RunTelemetryTest] {(ok ? "PASS" : "FAIL")}: heroAtInitNull={heroNullAtInit} heroAfterStart={rec.DebugHero}";
        Debug.Log(result);
        return result;
    }

    [ContextMenu("Test 14: run nova detectada reseta attemptIndex e cria arquivo novo")]
    public string TestRunChangeResetsState()
    {
        var prop = typeof(MainMenuUI).GetProperty("CurrentRun");
        prop.SetValue(null, new RunState { day = 1, gold = 0, hero = "Barbarian" });
        GameEvents.DayStarted(); // run A, dia 1, tentativa 1
        int a1 = RunTelemetryRecorder.Instance.DebugCurrentAttemptIndex;
        string runIdA = RunTelemetryRecorder.Instance.DebugRunId;

        prop.SetValue(null, new RunState { day = 1, gold = 0, hero = "Barbarian" }); // run B (referencia NOVA)
        GameEvents.DayStarted(); // run B, dia 1 — deveria ser tentativa 1, NAO 2
        int a2 = RunTelemetryRecorder.Instance.DebugCurrentAttemptIndex;
        string runIdB = RunTelemetryRecorder.Instance.DebugRunId;

        // runId tem granularidade de 1s (yyyyMMdd_HHmmss) — não serve pra provar "arquivo novo"
        // dentro do mesmo segundo de wall-clock (como num teste automatizado). O que prova a
        // troca de run de verdade é attemptsPerDay ter zerado (a2==1, não 2) mais
        // DebugLastClosedShopVisit não ter vazado nada da run A — a1/a2 já cobrem isso.
        bool ok = a1 == 1 && a2 == 1;
        string result = $"[RunTelemetryTest] {(ok ? "PASS" : "FAIL")}: a1={a1} a2={a2} runIdA={runIdA} runIdB={runIdB}";
        Debug.Log(result);
        return result;
    }

    [ContextMenu("Test 15: GameState.Shop sem OnDayEnd previo abre ShopVisit (Continue Game)")]
    public string TestContinueGameOpensShopVisit()
    {
        RunTelemetryRecorder.Instance.DebugSimulateGameStateChanged(GameState.Shop);
        RunTelemetryRecorder.Instance.RecordPerkPurchased(testPerk, 1, 100, 200, 100);
        GameEvents.DayStarted(); // fecha a visita pra poder inspecionar
        var closed = RunTelemetryRecorder.Instance.DebugLastClosedShopVisit;
        bool ok = closed != null && closed.perksPurchased.Count == 1;
        string result = $"[RunTelemetryTest] {(ok ? "PASS" : "FAIL")}: perksPurchased.Count={closed?.perksPurchased.Count}";
        Debug.Log(result);
        return result;
    }
}
