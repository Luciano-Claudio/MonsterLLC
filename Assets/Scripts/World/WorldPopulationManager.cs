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
// distância já limita o total de monstros vivos ao mesmo tempo). Ciclo Dia/Noite real
// (DaySpawnDefinition.nightWeights) implementado depois também — ver DayTimer.IsNight, lido
// abaixo em SpawnOne().
public class WorldPopulationManager : MonoBehaviour
{
    public static WorldPopulationManager Instance { get; private set; }

    public WorldGenerationSettings worldSettings;
    public MonsterSpawnSettings spawnSettings;

    [Header("Debug / Teste")]
    [Tooltip("Se marcado, ignora o Dia real da run (MainMenuUI.CurrentRun.day) e usa testDayOverride no lugar — mesmo padrão de ChunkTerrainGenerator.")]
    public bool useTestDayOverride;
    [Range(1, 30)]
    public int testDayOverride = 1;

    [Header("Debug / Teste — Isolamento de Tier (só teste, não existe no jogo real)")]
    [Tooltip("Se marcado, ignora toda a % normal de distribuição por Dia e força 100% dos monstros comuns vindo só do testTierOverride — de propósito quebra o sistema de % (não tem mais % de tier nenhum enquanto isto estiver ligado). Depois de marcar/mudar o Tier, usa o botão de contexto abaixo pra resetar os monstros vivos e já nascer todo mundo sob a regra nova.")]
    public bool useTierIsolationOverride;
    [Range(1, 10)]
    public int testTierOverride = 1;

    private Transform player;
    private readonly List<GameObject> aliveEnemies = new();
    private readonly List<EnemyDefinitionSO> candidateScratch = new(); // reaproveitado em TryPickEnemyDefinition, nunca alocado por spawn
    private readonly float[] tierPercentsBuffer = new float[WorldTierSelector.MonsterTierCount];

    private float respawnTimer;
    private bool escalated; // true depois que minimum virou o antigo target e target virou o antigo maximum — só 1 vez, maximum é teto
    private int crisisStreak;

    private int ActiveMinimum => escalated ? spawnSettings.populationTarget : spawnSettings.populationMinimum;
    private int ActiveTarget => escalated ? spawnSettings.populationMaximum : spawnSettings.populationTarget;

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

    // Botão de contexto (clique direito no componente no Inspector) — mesmo padrão do
    // "Regenerar Mundo" do ChunkTerrainGenerator, só que aqui não precisa sincronizar nada
    // com outro manager: WorldBossSpawnManager tem seu próprio useTierIsolationOverride/
    // testTierOverride independente, e os dois já escutam o mesmo GameEvents.ClearAllEnemies.
    [ContextMenu("Debug: Resetar e isolar Tier")]
    public void DebugResetForTierIsolation()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[WorldPopulationManager] \"Resetar e isolar Tier\" só funciona em Play Mode.");
            return;
        }

        GameEvents.ClearAllEnemies();
        Debug.Log(useTierIsolationOverride
            ? $"[WorldPopulationManager] Isolamento de Tier ativo — só nasce Tier {testTierOverride} agora."
            : "[WorldPopulationManager] useTierIsolationOverride está desmarcado — resetou, mas a distribuição normal por % continua valendo.");
    }

    private void OnEnable()
    {
        GameEvents.OnHeroSpawned += HandleHeroSpawned;
        GameEvents.OnClearAllEnemies += HandleClearAllEnemies;
    }

    private void OnDisable()
    {
        GameEvents.OnHeroSpawned -= HandleHeroSpawned;
        GameEvents.OnClearAllEnemies -= HandleClearAllEnemies;
    }

    private void HandleHeroSpawned(Transform hero) => player = hero;

    // "Regenerar Mundo" / compra de Tier Suppression — nunca é morte (sem loot/kill/energia,
    // mesmo critério do Distance Despawn). A população volta a 0, que já é <= ActiveMinimum,
    // então a crise de repovoamento dispara sozinha no próximo Update() (mesmo mecanismo que já
    // cobre o preenchimento inicial do jogo, ver HandleCrisis).
    private void HandleClearAllEnemies()
    {
        foreach (var enemy in aliveEnemies)
        {
            if (enemy != null) Destroy(enemy);
        }
        aliveEnemies.Clear();
    }

    private void Update()
    {
        if (!GameplayGate.IsActive || player == null || worldSettings == null || spawnSettings == null) return;

        aliveEnemies.RemoveAll(e => e == null);

        // Distance Despawn + replacement imediato (doc, Seção 11.1-11.3). Despawn NUNCA é
        // morte: Destroy() direto aqui nunca passa por EnemyController.Die()/TakeDamage(),
        // então não dispara GameEvents.EnemyKilled nem loot — já correto só por não chamar
        // nenhum método de morte do monstro.
        for (int i = aliveEnemies.Count - 1; i >= 0; i--)
        {
            float distance = Vector2.Distance(aliveEnemies[i].transform.position, player.position);
            if (distance <= spawnSettings.enemyDespawnDistance) continue;

            Destroy(aliveEnemies[i]);
            aliveEnemies.RemoveAt(i);
            SpawnOne(); // 1 despawn = 1 replacement imediato, sem resetar o cooldown normal abaixo
        }

        if (aliveEnemies.Count <= ActiveMinimum)
        {
            HandleCrisis();
            return;
        }

        if (aliveEnemies.Count >= ActiveTarget) return;

        respawnTimer += Time.deltaTime;
        if (respawnTimer < spawnSettings.respawnInterval) return;
        respawnTimer = 0f;

        SpawnOne();
    }

    // Mesmo modelo do FloorPopulationManager: crise = caiu até o mínimo (ou abaixo), reabastece
    // na hora (sem cooldown) e conta mais 1 na sequência; `crisisStreakToEscalate` crises
    // seguidas sobem o patamar PERMANENTEMENTE (minimum := target antigo, target := maximum
    // antigo). Só escala 1 vez — maximum é teto. Também cobre o preenchimento inicial: a
    // população começa em 0, que já é <= ActiveMinimum, então a primeira crise acontece
    // sozinha no primeiro Update() depois do herói nascer, sem precisar de um gatilho de
    // "entrada" separado (diferente do FloorPopulationManager, que precisava detectar a
    // borda de "Floor ativou agora").
    private void HandleCrisis()
    {
        crisisStreak++;
        if (!escalated && crisisStreak >= spawnSettings.crisisStreakToEscalate)
        {
            escalated = true;
            crisisStreak = 0;
        }

        int missing = ActiveTarget - aliveEnemies.Count;
        for (int i = 0; i < missing; i++) SpawnOne();
    }

    private void SpawnOne()
    {
        if (spawnSettings.roster == null || spawnSettings.roster.Count == 0) return;

        // Safe Zone/EffectiveSpawnDay são centrados na origem do mundo (0,0) — mesma origem
        // que a centralização de chunk (ChunkCoord.ToWorldOrigin) já usa como centro real.
        float distanceFromOrigin = player.position.magnitude;
        int effectiveDay = WorldTierSelector.ComputeEffectiveSpawnDay(
            distanceFromOrigin, worldSettings.safeZoneRadius, worldSettings.distancePerSpawnDay, ActualDay);

        var dayDef = worldSettings.daySpawnTable != null ? worldSettings.daySpawnTable.GetDay(effectiveDay) : null;
        bool isNight = DayTimer.Instance != null && DayTimer.Instance.IsNight;
        int suppressedTierCount = MainMenuUI.CurrentRun?.suppressedTierCount ?? 0;
        int forcedTierIndex = useTierIsolationOverride ? testTierOverride - 1 : -1;
        WorldTierSelector.ComputeTierPercentsNonAlloc(dayDef, tierPercentsBuffer, suppressedTierCount, isNight, forcedTierIndex);

        int tierIndex = WorldTierSelector.RollMonsterTier(Random.value, tierPercentsBuffer);
        if (tierIndex < 0) return; // tabela de dia sem nenhum peso > 0 — defesa, não deveria acontecer com a tabela real

        // Com isolamento ligado, nunca cai pro Tier de fallback (ver TryPickEnemyDefinition) —
        // se o Tier isolado não tiver bestiário nenhum cadastrado ainda, o certo é não nascer
        // NADA (silêncio), não vazar pra Tier1/outro Tier por baixo dos panos: o ponto inteiro
        // do isolamento é testar só aquele Tier, nunca ver outro aparecendo "disfarçado".
        bool allowFallbackTier = !useTierIsolationOverride;
        if (!TryPickEnemyDefinition(tierIndex + 1, suppressedTierCount, allowFallbackTier, out var definition)) return; // roster vazio pra esse Tier (ou, sem isolamento, nem o Tier de fallback cadastrado)

        float minDistanceFromOrigin = worldSettings.safeZoneRadius + spawnSettings.spawnSafetyPadding;
        // Rolado por spawn individual (não por EnemyDefinitionSO) — decisão do usuário: uma %
        // fixa dos monstros nasce perto/à vista do jogador de propósito, dá um ar de perigo.
        bool preferOutsideView = Random.value < spawnSettings.outsideCameraViewChance;
        if (!WorldSpawnUtility.TryGetSpawnPosition(
                player.position, spawnSettings.replacementSpawnMinDistance, spawnSettings.replacementSpawnMaxDistance,
                minDistanceFromOrigin, spawnSettings.maxSpawnPositionAttempts, out var spawnPosition,
                spawnSettings.spawnObstacleClearanceRadius, spawnSettings.obstacleLayerMask, preferOutsideView))
        {
            return; // nenhuma posição válida nas tentativas — tenta de novo no próximo ciclo, nunca loop infinito (doc, Seção 11.6)
        }

        var enemyObj = Instantiate(definition.prefab, spawnPosition, Quaternion.identity);
        // ownerFloor fica null (default) — EnemyController/FloorActivationCheck já tratam
        // null como "sempre ativo", sem precisar de nenhuma mudança nesses scripts.
        WorldSpawnUtility.RestrictToOpenWorldGraph(enemyObj);

        aliveEnemies.Add(enemyObj);
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
        float totalWeight = 0f;

        foreach (var def in spawnSettings.roster)
        {
            if (def == null || def.prefab == null || def.tier != tierNumber1Based) continue;
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
