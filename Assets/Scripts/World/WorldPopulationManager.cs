using System.Collections.Generic;
using UnityEngine;

// Substitui FloorPopulationManager pro mundo aberto — mesmo modelo de população/crise/
// escalação (PopulationConfig), mas 1 população GLOBAL (não por-Floor), seguindo o pipeline
// de spawn do doc de design (docs/new/Mudanca_Estrutural_Mapa_Unico_Spawn_Bosses_
// ATUALIZADO_V2.md, Seção 14): ActualDay -> distância da origem -> EffectiveSpawnDay ->
// pesos dos 10 Tiers (WorldTierSelector.ComputeTierPercentsNonAlloc) -> Roll Tier -> Roll
// Monster Inside Tier (Seção 16) -> posição no Spawn Annulus ao redor do jogador
// (WorldSpawnUtility) -> spawn.
//
// Fase A (escopo original desta rodada, decisão do usuário): sem Boss (implementado depois,
// Sprint P7, ver WorldBossSpawnManager), sem Simulation Rings (todo monstro comum sempre
// simula em full — aceitável porque a população é pequena, Min/Target/Max, e o despawn por
// distância já limita o total de monstros vivos ao mesmo tempo).
//
// D9.4 (docs/superpowers/specs/2026-11-20-d9-4-horda-cerco-design.md): a população segue a
// curva da horda — meta por dia da run e TEMPO REAL do dia (não pausa com boss vivo), com picos
// e teto — e é reposta em fluxo contínuo (spawnsPerSecond). Substitui a rampa Min/Target/Max da
// D8.1 e a crise.
public class WorldPopulationManager : MonoBehaviour
{
    public static WorldPopulationManager Instance { get; private set; }

    public WorldGenerationSettings worldSettings;
    public MonsterSpawnSettings spawnSettings;

    [Header("Debug / Teste")]
    [Tooltip("Se marcado, ignora o Dia real da run (MainMenuUI.CurrentRun.day) e usa testDayOverride no lugar — mesmo padrão de ChunkTerrainGenerator.")]
    public bool useTestDayOverride;
    [Range(1, 10)]
    public int testDayOverride = 1;

    [Header("Debug / Teste — Isolamento de Tier (só teste, não existe no jogo real)")]
    [Tooltip("Se marcado, ignora toda a % normal de distribuição por Dia e força 100% dos monstros comuns vindo só do testTierOverride — de propósito quebra o sistema de % (não tem mais % de tier nenhum enquanto isto estiver ligado). Depois de marcar/mudar o Tier, usa o botão de contexto abaixo pra resetar os monstros vivos e já nascer todo mundo sob a regra nova.")]
    public bool useTierIsolationOverride;
    [Range(1, 10)]
    public int testTierOverride = 1;

    private Transform player;
    private readonly List<EnemyController> aliveEnemies = new(); // D9.4 — EnemyController (não GameObject): contagem por setor/reposicionamento

    // Telemetria de run (docs/superpowers/specs/2026-11-16-run-telemetry-design.md) — leitura
    // read-only, nenhuma mudança de comportamento.
    public int AliveCount => aliveEnemies.Count + aliveMiniBosses.Count; // telemetria: comuns + mini bosses

    // Mini bosses (2026-11-21): lista própria — fora da meta da horda, mas entram no
    // reposicionamento, na contagem de setores e no "limpar todos".
    private readonly List<EnemyController> aliveMiniBosses = new();
    public int MiniBossAliveCount => aliveMiniBosses.Count;
    private bool[] miniBossEligible = System.Array.Empty<bool>();
    private int lastMiniBossIndex = -1;
    private float nextMiniBossAt;
    private readonly List<EnemyDefinitionSO> candidateScratch = new(); // reaproveitado em TryPickEnemyDefinition, nunca alocado por spawn

    // D9.4 — fluxo contínuo: acumulador fracionário de spawnsPerSecond (no máximo 3 guardados,
    // pra nunca sair rajada depois de um período sem lugar válido).
    private float spawnAccumulator;

    // Revisão final do D9.4 (achado 2): no máximo 16 reposicionamentos por frame; para no 1º sem lugar.
    private RepositionBudget repositionBudget = new RepositionBudget(16);
    private const float MaxSpawnAccumulator = 3f;

    // D9.4 — meta atual de monstros comuns vivos (curva por dia + tempo real do dia + picos).
    public int CurrentTarget { get; private set; }

    private int ActualDay => useTestDayOverride ? testDayOverride : (MainMenuUI.CurrentRun?.day ?? 1);

    private void Awake() => Instance = this;

    // Chamado pelo ChunkTerrainGenerator.RegenerateWorld() — mesmo motivo/comentário completo
    // em WorldBossSpawnManager.SyncDayOverride(): sem isso, o bioma mudava de Tier no botão
    // "Regenerar Mundo" mas o spawn de monstro comum continuava usando o Dia antigo (campo
    // useTestDayOverride/testDayOverride próprio, nunca sincronizado).
    public void SyncDayOverride(bool enabled, int day)
    {
        useTestDayOverride = enabled;
        testDayOverride = day;
    }

    // Chamado pelo DebugResetForTierIsolation do OUTRO manager (ver lá embaixo) — sem isso,
    // bug real reportado pelo usuário: isolar o Tier só em WorldPopulationManager (ou só em
    // WorldBossSpawnManager) deixava o outro lado inteiro de fora, nascendo bosses/monstros de
    // Tiers passados enquanto o lado "isolado" já estava certinho.
    public void SyncTierIsolation(bool enabled, int tier)
    {
        useTierIsolationOverride = enabled;
        testTierOverride = tier;
    }

    // Botão de contexto (clique direito no componente no Inspector) — espelha o bool/Tier pro
    // WorldBossSpawnManager ANTES de limpar, então 1 clique em QUALQUER um dos dois já isola
    // monstro comum E boss juntos, sempre no mesmo Tier.
    [ContextMenu("Debug: Resetar e isolar Tier")]
    public void DebugResetForTierIsolation()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[WorldPopulationManager] \"Resetar e isolar Tier\" só funciona em Play Mode.");
            return;
        }

        WorldBossSpawnManager.Instance?.SyncTierIsolation(useTierIsolationOverride, testTierOverride);
        GameEvents.ClearAllEnemies();
        EnemyController.DestroyAllSpawned(); // pega até projétil/summon que ClearAllEnemies não rastreia
        LootDrop.DestroyAllOnGround(); // só debug — poluição visual/FPS numa sessão de teste, nunca no fluxo normal
        Debug.Log(useTierIsolationOverride
            ? $"[WorldPopulationManager] Isolamento de Tier ativo — só nasce Tier {testTierOverride} agora (monstro comum e boss)."
            : "[WorldPopulationManager] useTierIsolationOverride está desmarcado — resetou, mas a distribuição normal por % continua valendo.");
    }

    private void OnEnable()
    {
        GameEvents.OnHeroSpawned += HandleHeroSpawned;
        GameEvents.OnClearAllEnemies += HandleClearAllEnemies;
        GameEvents.OnDayStart += HandleDayStart;
        GameEvents.OnDayNightPhaseChanged += HandleDayNightPhaseChanged;
    }

    private void OnDisable()
    {
        GameEvents.OnHeroSpawned -= HandleHeroSpawned;
        GameEvents.OnClearAllEnemies -= HandleClearAllEnemies;
        GameEvents.OnDayStart -= HandleDayStart;
        GameEvents.OnDayNightPhaseChanged -= HandleDayNightPhaseChanged;
    }

    // D9.4 — relógio da horda: tempo REAL do dia (não pausa com boss vivo, ao contrário do
    // cronômetro). Zera em todo início de dia, retry incluso. Só anda com o jogo ativo (nunca
    // no loading, na pausa ou na animação de morte) — ver Update().
    private readonly HordeClock hordeClock = new HordeClock();
    public float HordeElapsed => hordeClock.Elapsed;

    [Header("Debug / Teste — Horda (D9.4)")]
    [Tooltip("Se > 0, todo dia começa com o relógio da horda já nesse tempo (segundos) — pra testar o fim da curva sem esperar. 0 = desligado.")]
    public float debugHordeStartSeconds = 0f;

    // Etapas 1+2 — o segundo da run vem do relógio da run (DayTimer.RunElapsed, fonte única); o
    // HordeClock próprio só vale sem DayTimer (cena de teste sem relógio).
    private float RunSeconds => DayTimer.Instance != null && DayTimer.Instance.Schedule != null ? DayTimer.Instance.RunElapsed : HordeElapsed;
    private RunSchedule Schedule => DayTimer.Instance != null ? DayTimer.Instance.Schedule : null;
    private bool IsNight => DayTimer.Instance != null && DayTimer.Instance.IsNight;
    private float NightSpeed => IsNight && DayTimer.Instance.Settings != null ? DayTimer.Instance.Settings.nightSpeedMultiplier : 1f;

    private int miniBossSlot; // espaço de boss cujo mini boss (1 por intervalo entre bosses) ainda não nasceu

    private void HandleDayStart() // = início da run
    {
        hordeClock.Reset(debugHordeStartSeconds);
        lastMiniBossIndex = -1;
        miniBossSlot = 0;
        ScheduleNextMiniBoss();
    }

    // 1 mini boss por intervalo entre um boss e o seguinte (15-45 s depois do boss, RunSettings).
    private void ScheduleNextMiniBoss()
    {
        var schedule = Schedule;
        nextMiniBossAt = schedule != null && miniBossSlot < schedule.SlotCount - 1
            ? schedule.MiniBossSecond(miniBossSlot, Random.value)
            : float.MaxValue; // depois do último boss (Divine God) não nasce mais mini boss
    }

    private void AdvanceMiniBossSlot()
    {
        miniBossSlot++;
        ScheduleNextMiniBoss();
    }

    // Buff noturno (+velocidade) nos comuns e mini bosses vivos — só na troca de fase, nunca por frame.
    private void HandleDayNightPhaseChanged(bool isNight)
    {
        float speed = NightSpeed;
        foreach (var e in aliveEnemies) if (e != null) e.SetRunMultipliers(speed, 1f);
        foreach (var m in aliveMiniBosses) if (m != null) m.SetRunMultipliers(speed, 1f);
    }

    private void HandleHeroSpawned(Transform hero) => player = hero;

    // "Regenerar Mundo" / compra de Tier Suppression — nunca é morte (sem loot/kill/energia,
    // mesmo critério do Distance Despawn). A população volta a 0, abaixo da meta, e o fluxo
    // contínuo do Update() repõe sozinho no ritmo de spawnsPerSecond.
    private void HandleClearAllEnemies()
    {
        foreach (var enemy in aliveEnemies)
        {
            if (enemy != null) Destroy(enemy.gameObject);
        }
        aliveEnemies.Clear();
        foreach (var miniBoss in aliveMiniBosses)
        {
            if (miniBoss != null) Destroy(miniBoss.gameObject);
        }
        aliveMiniBosses.Clear();
    }

    private void Update()
    {
        // HeroController.IsDying (não GameplayGate — ver comentário na declaração dele):
        // sem isso, a população reabastecia sozinha ainda durante a animação de morte (o
        // GameState só muda pra Results no fim dela, GameplayGate continua achando que é
        // Gameplay normal) — achado real do usuário, monstro reabastecido ficava vivo até
        // dentro da Loja.
        hordeClock.Tick(Time.deltaTime, GameplayGate.IsActive && !HeroController.IsDying);
        if (!GameplayGate.IsActive || HeroController.IsDying || player == null || worldSettings == null || spawnSettings == null) return;

        aliveEnemies.RemoveAll(e => e == null);
        aliveMiniBosses.RemoveAll(e => e == null);

        // D9.4 — anti-kite: média de pra onde o herói anda; setores recontados no 1º spawn do frame.
        Kite.AddVelocity(HeroController.Velocity, Time.deltaTime);
        sectorsPreparedThisFrame = false;

        // D9.4 — reposicionamento por distância (no lugar de Destroy + spawn novo): o monstro
        // comum que ficou longe reaparece no anel fora da tela, pela mesma regra de setor e
        // anti-kite — quem foge encontra a horda de volta na frente. Mesmo monstro, mesma vida e
        // status. Sem ponto válido: fica onde está e tenta no próximo frame. Nunca é morte (sem
        // EnemyKilled, sem loot).
        // Limite nunca menor que o anel de spawn (revisão final, achado 1 — ver SpawnRing.RepositionThreshold).
        WorldSpawnUtility.GetViewExtents(out _, out float viewHalfWidth, out float viewHalfHeight);
        float despawnDistance = SpawnRing.RepositionThreshold(viewHalfWidth, viewHalfHeight, spawnSettings.offscreenMargin,
            spawnSettings.ringJitter, WorldSpawnUtility.SnapTolerance, spawnSettings.enemyDespawnDistance);
        float despawnSqr = despawnDistance * despawnDistance;
        repositionBudget.BeginFrame();
        RepositionFar(aliveMiniBosses, despawnSqr); // mini bosses primeiro — são poucos e mais importantes de manter por perto
        RepositionFar(aliveEnemies, despawnSqr);

        UpdateMiniBosses();

        // D9.4 — meta pela curva da horda (dia da run + tempo real do dia) e fluxo contínuo,
        // no lugar do "1 a cada respawnInterval" e das rajadas da crise.
        int nightNumber = !IsNight ? 0 : Schedule != null ? Mathf.Max(1, Schedule.NightNumberAt(RunSeconds)) : 1;
        CurrentTarget = spawnSettings.horde.TargetAlive(RunSeconds, nightNumber, PermanentBonuses.HordeFactor); // + Maldição
        if (aliveEnemies.Count >= CurrentTarget)
        {
            spawnAccumulator = 0f;
            return;
        }

        spawnAccumulator = Mathf.Min(MaxSpawnAccumulator, spawnAccumulator + spawnSettings.spawnsPerSecond * Time.deltaTime);
        while (spawnAccumulator >= 1f && aliveEnemies.Count < CurrentTarget)
        {
            if (!SpawnOne()) break; // sem ponto válido fora da tela agora — tenta no próximo frame (acumulador guardado, limitado)
            spawnAccumulator -= 1f;
        }
    }

    // Reposicionamento por distância de uma lista (D9.4), dividindo o mesmo orçamento do frame.
    private void RepositionFar(List<EnemyController> list, float despawnSqr)
    {
        Vector2 heroPos = player.position;
        for (int i = 0; i < list.Count; i++)
        {
            var enemy = list[i];
            if (enemy.IsDead || enemy.IsBoss) continue;
            if (((Vector2)enemy.transform.position - heroPos).sqrMagnitude <= despawnSqr) continue;
            if (!repositionBudget.TryConsume()) return; // orçamento do frame acabou (ou o anel está bloqueado) — o resto fica pro próximo frame
            if (TryPickRingPosition(out var newPosition, out bool inFront))
            {
                enemy.TeleportTo(newPosition);
                Kite.RecordSpawn(inFront);
            }
            else repositionBudget.MarkBlocked();
        }
    }

    // Percentuais de tier do momento (dia efetivo pela distância da origem, Dia/Noite, supressão,
    // isolamento de debug) — mesma conta pro spawn comum e pra elegibilidade dos mini bosses.
    private int RollRunTier()
    {
        if (useTierIsolationOverride) return testTierOverride;
        var schedule = Schedule;
        if (schedule == null) return 1;
        int n = schedule.TierShares(RunSeconds, out int tierA, out float shareA, out int tierB);
        return n == 1 || Random.value < shareA ? tierA : tierB;
    }

    // Mini bosses (etapas 1+2): 1 por intervalo entre bosses (RunSettings: 15-45 s depois do boss),
    // sozinho e sem aviso, sorteado entre os do tier atual da run ou abaixo, sem repetir o último. Nasce mesmo com outro vivo (quem não mata, acumula). Sem lugar fora da
    // tela: tenta no próximo frame, sem reagendar. Nunca é boss (sem câmera, sem pausa).
    private void UpdateMiniBosses()
    {
        var roster = spawnSettings.miniBossRoster;
        if (roster == null || roster.Count == 0 || RunSeconds < nextMiniBossAt) return;

        if (miniBossEligible.Length != roster.Count) miniBossEligible = new bool[roster.Count];
        int currentTier = Schedule != null ? Schedule.CurrentTier(RunSeconds) : 1;
        for (int i = 0; i < roster.Count; i++)
        {
            var def = roster[i];
            miniBossEligible[i] = def != null && def.prefab != null && def.tier >= 1 && def.tier <= currentTier;
        }

        int pick = MiniBossRules.Pick(miniBossEligible, roster.Count, lastMiniBossIndex, Random.value);
        if (pick < 0)
        {
            AdvanceMiniBossSlot(); // nenhum elegível ainda (T1 não tem mini boss) — este intervalo fica sem
            return;
        }

        if (!TryPickRingPosition(out var spawnPosition, out bool spawnedInFront)) return; // sem lugar fora da tela — próximo frame
        Kite.RecordSpawn(spawnedInFront);

        var obj = Instantiate(roster[pick].prefab, spawnPosition, Quaternion.identity, EnemyController.EnemysContainer);
        WorldSpawnUtility.RestrictToOpenWorldGraph(obj);
        var controller = obj.GetComponent<EnemyController>();
        if (controller == null)
        {
            Destroy(obj); // defesa: prefab sem EnemyController
            AdvanceMiniBossSlot();
            return;
        }
        PrepareMiniBoss(controller, roster[pick].tier);
        controller.SetRunMultipliers(NightSpeed, 1f);
        aliveMiniBosses.Add(controller);
        lastMiniBossIndex = pick;
        AdvanceMiniBossSlot();
    }

    // Revisão final (achado crítico): os 5 prefabs vieram de boss com `isBoss: 1` salvo no
    // Inspector. Sem desligar aqui, o mini boss continuaria boss em tudo (sem reposicionamento,
    // sem knockback/empurrão, ouro de boss, BossesDefeated). O prefab fica intacto (decisão do
    // usuário: nenhuma mudança neles); o flag é corrigido no nascimento, como o
    // WorldBossSpawnManager faz no sentido contrário.
    public static void PrepareMiniBoss(EnemyController controller, int tier)
    {
        controller.SetIsBoss(false);
        controller.SetSpawnInfo(tier, true); // D9.6b — XP de mini boss (×3 do tier)
        controller.ForceImmediateAggro();
    }

    private bool SpawnOne()
    {
        if (spawnSettings.roster == null || spawnSettings.roster.Count == 0) return false;

        // Etapas 1+2 — tier pela mistura da run (RunSchedule.TierShares: o tier seguinte entra aos
        // poucos depois do último boss do tier atual). Isolamento de debug continua forçando 1 tier.
        int suppressedTierCount = 0;
        int tierIndex = RollRunTier() - 1;

        // Com isolamento ligado, nunca cai pro Tier de fallback (ver TryPickEnemyDefinition) —
        // se o Tier isolado não tiver bestiário nenhum cadastrado ainda, o certo é não nascer
        // NADA (silêncio), não vazar pra Tier1/outro Tier por baixo dos panos: o ponto inteiro
        // do isolamento é testar só aquele Tier, nunca ver outro aparecendo "disfarçado".
        bool allowFallbackTier = !useTierIsolationOverride;
        if (!TryPickEnemyDefinition(tierIndex + 1, suppressedTierCount, allowFallbackTier, out var definition)) return false; // roster vazio pra esse Tier (ou, sem isolamento, nem o Tier de fallback cadastrado)

        // D9.4 — sempre fora da tela, no setor do anel com menos monstros (cerco), com peso
        // extra pra frente da fuga (anti-kite). Sem lugar válido: não nasce neste frame.
        if (!TryPickRingPosition(out var spawnPosition, out bool spawnedInFront)) return false;
        Kite.RecordSpawn(spawnedInFront);

        var enemyObj = Instantiate(definition.prefab, spawnPosition, Quaternion.identity, EnemyController.EnemysContainer);
        // ownerFloor fica null (default) — EnemyController/FloorActivationCheck já tratam
        // null como "sempre ativo", sem precisar de nenhuma mudança nesses scripts.
        WorldSpawnUtility.RestrictToOpenWorldGraph(enemyObj);

        // D9.4 (decisão do usuário, 2026-11-20): todo monstro comum nasce com o herói como alvo,
        // em qualquer dia (antes, só no retry). Estilo Vampire Survivors: a horda nasce fora da
        // tela e já vem na direção do herói.
        var enemyController = enemyObj.GetComponent<EnemyController>();
        if (enemyController != null)
        {
            enemyController.ForceImmediateAggro();
            enemyController.SetSpawnInfo(definition.tier, false); // D9.6b — XP pelo tier
            if (IsNight) enemyController.SetRunMultipliers(NightSpeed, 1f);
        }

        if (enemyController != null) aliveEnemies.Add(enemyController);
        else Destroy(enemyObj); // prefab sem EnemyController não é monstro comum — defesa, não deveria acontecer
        return true;
    }

    // ---------------- D9.4 — cerco por setores + anti-kite ----------------

    private int[] sectorCounts = System.Array.Empty<int>();
    private float[] sectorBias = System.Array.Empty<float>();
    private int[] sectorOrder = System.Array.Empty<int>();
    private bool sectorsPreparedThisFrame;
    private KiteTracker kite;

    private KiteTracker Kite => kite ??= new KiteTracker(spawnSettings.kiteWindowSeconds, spawnSettings.kiteMinSpeed, spawnSettings.kiteShareWindowSpawns);

    // Conta os monstros comuns vivos por setor (ângulo em relação ao herói, qualquer distância)
    // e o peso do anti-kite. 1x por frame; cada spawn incrementa o setor que usou, então o
    // próximo spawn do mesmo frame já vai pra outro lado.
    private void PrepareSectors()
    {
        int n = Mathf.Max(1, spawnSettings.sectorCount);
        if (sectorCounts.Length != n)
        {
            sectorCounts = new int[n];
            sectorBias = new float[n];
            sectorOrder = new int[n];
        }
        System.Array.Clear(sectorCounts, 0, n);

        Vector2 heroPos = player.position;
        for (int i = 0; i < aliveEnemies.Count; i++)
        {
            var enemy = aliveEnemies[i];
            if (enemy == null) continue;
            sectorCounts[SpawnRing.SectorOf((Vector2)enemy.transform.position - heroPos, n)]++;
        }
        for (int i = 0; i < aliveMiniBosses.Count; i++)
        {
            var miniBoss = aliveMiniBosses[i];
            if (miniBoss == null) continue;
            sectorCounts[SpawnRing.SectorOf((Vector2)miniBoss.transform.position - heroPos, n)]++;
        }

        bool hasFlee = Kite.TryGetFleeDirection(out Vector2 flee) && Kite.FrontShare < spawnSettings.kiteMaxFrontShare;
        for (int i = 0; i < n; i++)
            sectorBias[i] = hasFlee && SpawnRing.IsFrontSector(i, n, flee, spawnSettings.kiteArcDegrees) ? spawnSettings.kiteBias : 0f;

        sectorsPreparedThisFrame = true;
    }

    // Tenta os setores do menos cheio pro mais cheio até achar um ponto válido fora da tela.
    private bool TryPickRingPosition(out Vector3 position, out bool inFront)
    {
        position = default;
        inFront = false;
        if (!sectorsPreparedThisFrame) PrepareSectors();

        int n = sectorCounts.Length;
        SpawnSectorPicker.FillOrder(sectorCounts, sectorBias, n, Random.Range(0, n), sectorOrder);

        float minDistanceFromOrigin = worldSettings.safeZoneRadius + spawnSettings.spawnSafetyPadding;
        int attemptsPerSector = Mathf.Max(1, spawnSettings.maxSpawnPositionAttempts / 4);
        Vector2 heroPos = player.position;
        for (int k = 0; k < n; k++)
        {
            int sector = sectorOrder[k];
            if (!WorldSpawnUtility.TryGetSpawnPositionInSector(heroPos, sector, n, spawnSettings.offscreenMargin, spawnSettings.ringJitter,
                    minDistanceFromOrigin, attemptsPerSector, spawnSettings.spawnObstacleClearanceRadius, spawnSettings.obstacleLayerMask, out position))
                continue;

            sectorCounts[sector]++;
            inFront = sectorBias[sector] > 0f;
            return true;
        }
        return false;
    }

    // "Roll Monster Inside Tier" (doc, Seção 16) — roleta ponderada por
    // EnemyDefinitionSO.weight entre as entradas do roster cujo tier bate com
    // `tierNumber1Based`. Tier sorteado sem NENHUMA entrada cadastrada ainda (bestiário
    // incompleto — só Tier1/Tier2 prontos por enquanto) cai pro Tier NÃO SUPRIMIDO mais baixo
    // (`suppressedTierCount + 1`, nunca um Tier1 fixo) — sem isso, suprimir o Tier1 e rolar um
    // Tier vazio (ex.: Tier5, sem bestiário ainda) voltaria a spawnar Tier1 mesmo assim,
    // anulando a compra. Mesmo critério de "nunca deixar buraco" já usado pelo terreno
    // (ChunkTerrainGenerator.ResolveBiome com tierBiomes[slot vazio]). `allowFallbackTier`
    // (padrão true) — desligado pelo isolamento de Tier de debug (useTierIsolationOverride):
    // ali um Tier vazio deve ficar em silêncio, nunca vazar pra outro Tier escondido.
    private bool TryPickEnemyDefinition(int tierNumber1Based, int suppressedTierCount, bool allowFallbackTier, out EnemyDefinitionSO chosen)
    {
        chosen = null;
        candidateScratch.Clear();
        bool rangedBlocked = RangedGate.Blocks(1, RunSeconds, spawnSettings.rangedUnlockSeconds); // só no começo da run
        float totalWeight = 0f;

        foreach (var def in spawnSettings.roster)
        {
            if (def == null || def.prefab == null || def.tier != tierNumber1Based) continue;
            if (rangedBlocked && def.isRanged) continue; // D9.4 — Dia 1: sem quem atira de longe nos primeiros segundos
            candidateScratch.Add(def);
            totalWeight += Mathf.Max(0f, def.weight);
        }

        if (candidateScratch.Count == 0)
        {
            if (!allowFallbackTier) return false;
            int fallbackTier = suppressedTierCount + 1;
            return tierNumber1Based != fallbackTier && TryPickEnemyDefinition(fallbackTier, suppressedTierCount, allowFallbackTier, out chosen);
        }

        float roll = Random.value * totalWeight;
        float cumulative = 0f;
        foreach (var def in candidateScratch)
        {
            cumulative += Mathf.Max(0f, def.weight);
            if (roll <= cumulative)
            {
                chosen = def;
                return true;
            }
        }

        chosen = candidateScratch[candidateScratch.Count - 1]; // sobra de arredondamento
        return true;
    }
}
