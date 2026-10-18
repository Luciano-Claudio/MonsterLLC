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
// Fase A (escopo desta rodada, decisão do usuário): sem Boss (fica pra rodada própria, P7),
// sem ciclo Dia/Noite real (usa sempre dayWeights, igual o terreno já faz —
// DaySpawnDefinition.nightWeights fica reservado pra quando o ciclo dia/noite existir de
// verdade), sem Simulation Rings (todo monstro comum sempre simula em full — aceitável
// porque a população é pequena, Min/Target/Max, e o despawn por distância já limita o total
// de monstros vivos ao mesmo tempo).
public class WorldPopulationManager : MonoBehaviour
{
    public WorldGenerationSettings worldSettings;
    public MonsterSpawnSettings spawnSettings;

    [Header("Debug / Teste")]
    [Tooltip("Se marcado, ignora o Dia real da run (MainMenuUI.CurrentRun.day) e usa testDayOverride no lugar — mesmo padrão de ChunkTerrainGenerator.")]
    public bool useTestDayOverride;
    [Range(1, 30)]
    public int testDayOverride = 1;

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

    private void OnEnable() => GameEvents.OnHeroSpawned += HandleHeroSpawned;
    private void OnDisable() => GameEvents.OnHeroSpawned -= HandleHeroSpawned;

    private void HandleHeroSpawned(Transform hero) => player = hero;

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
        WorldTierSelector.ComputeTierPercentsNonAlloc(dayDef, tierPercentsBuffer);

        int tierIndex = WorldTierSelector.RollMonsterTier(Random.value, tierPercentsBuffer);
        if (tierIndex < 0) return; // tabela de dia sem nenhum peso > 0 — defesa, não deveria acontecer com a tabela real

        if (!TryPickEnemyDefinition(tierIndex + 1, out var definition)) return; // roster inteiro vazio, nem Tier1 cadastrado

        float minDistanceFromOrigin = worldSettings.safeZoneRadius + spawnSettings.spawnSafetyPadding;
        if (!WorldSpawnUtility.TryGetSpawnPosition(
                player.position, spawnSettings.replacementSpawnMinDistance, spawnSettings.replacementSpawnMaxDistance,
                minDistanceFromOrigin, spawnSettings.maxSpawnPositionAttempts, out var spawnPosition,
                spawnSettings.spawnObstacleClearanceRadius, spawnSettings.obstacleLayerMask))
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
    // incompleto — só Tier1/Tier2 prontos por enquanto) cai pro Tier1, mesmo critério de
    // "nunca deixar buraco" já usado pelo terreno (ChunkTerrainGenerator.ResolveBiome com
    // tierBiomes[slot vazio]).
    private bool TryPickEnemyDefinition(int tierNumber1Based, out EnemyDefinitionSO chosen)
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
            return tierNumber1Based != 1 && TryPickEnemyDefinition(1, out chosen);
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
