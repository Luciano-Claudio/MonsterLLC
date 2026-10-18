using System.Collections.Generic;
using UnityEngine;

// Substitui o BossSpawnManager antigo (removido junto com o resto do sistema de Floor) —
// Sprint P7. Segue o design de docs/new/Mudanca_Estrutural_Mapa_Unico_Spawn_Bosses_
// ATUALIZADO_V2.md (Seção 17): Boss Timer decide QUANDO, a mesma distribuição de Tier usada
// pelo monstro comum (WorldTierSelector) decide QUAL Tier priorizar — mas aqui é o Tier de
// MAIOR peso (DominantTier), nunca um roll. Identidade: nenhum BossDefinitionSO repete DENTRO
// do mesmo giro de um Tier (UsedBossIdsToday) — decisão do usuário: ao esgotar TODOS os
// BossDefinitionSO de um Tier específico, esse Tier reseta sozinho (volta a valer tudo de
// novo) em vez de travar até o próximo ActualDay, pra continuar girando bosses se ainda
// sobrar tempo de dia (ver TryPickUnusedBossForTier). O reset diário (GameEvents.OnDayStart)
// continua existindo por cima disso, mas na prática o reset por-Tier tende a disparar primeiro.
public class WorldBossSpawnManager : MonoBehaviour
{
    public WorldGenerationSettings worldSettings;
    public BossSpawnSettings bossSettings;

    [Header("Debug / Teste")]
    [Tooltip("Mesmo padrão de WorldPopulationManager/ChunkTerrainGenerator — ignora o Dia real da run e usa testDayOverride no lugar.")]
    public bool useTestDayOverride;
    [Range(1, 30)]
    public int testDayOverride = 1;

    private Transform player;
    private readonly List<GameObject> aliveBosses = new();
    private readonly HashSet<BossDefinitionSO> usedBossIdsToday = new();
    private readonly List<BossDefinitionSO> candidateScratch = new(); // reaproveitado em TryPickUnusedBossForTier
    private readonly float[] tierPercentsBuffer = new float[WorldTierSelector.MonsterTierCount];
    private readonly bool[] tierVisitedScratch = new bool[WorldTierSelector.MonsterTierCount]; // ordem de predominância em TryPickBossDefinition

    private float bossTimer;
    private float replacementCooldownTimer; // proteção contra reroll por distância (doc, Seção 17) — andar de um lado pro outro na borda do bossDespawnDistance não gera replacement repetido

    private int ActualDay => useTestDayOverride ? testDayOverride : (MainMenuUI.CurrentRun?.day ?? 1);

    private void OnEnable()
    {
        GameEvents.OnHeroSpawned += HandleHeroSpawned;
        GameEvents.OnDayStart += HandleDayStart;
    }

    private void OnDisable()
    {
        GameEvents.OnHeroSpawned -= HandleHeroSpawned;
        GameEvents.OnDayStart -= HandleDayStart;
    }

    private void HandleHeroSpawned(Transform hero) => player = hero;

    // Novo ActualDay -> UsedBossIdsToday.Clear() (doc, Seção 17.3) — mesmo gatilho que
    // DayTimer.ResetForNewDay() já usa pra avisar o resto do jogo (ShopHandler/MainMenuUI).
    private void HandleDayStart() => usedBossIdsToday.Clear();

    private void Update()
    {
        if (!GameplayGate.IsActive || player == null || worldSettings == null || bossSettings == null) return;

        aliveBosses.RemoveAll(b => b == null); // null = morreu (EnemyController.Die()) — sem replacement automático, só o Boss Timer normal repõe

        if (replacementCooldownTimer > 0f)
        {
            replacementCooldownTimer -= Time.deltaTime;
        }
        else
        {
            for (int i = aliveBosses.Count - 1; i >= 0; i--)
            {
                float distance = Vector2.Distance(aliveBosses[i].transform.position, player.position);
                if (distance <= bossSettings.bossDespawnDistance) continue;

                // Boss Distance Despawn nunca é morte (sem loot/kill) — Destroy() direto, igual
                // WorldPopulationManager faz com monstro comum. Repõe a vaga na hora, sem tocar
                // em bossTimer (Replacement != novo Boss Timer Event).
                Destroy(aliveBosses[i]);
                aliveBosses.RemoveAt(i);
                replacementCooldownTimer = bossSettings.bossReplacementCooldown;
                TrySpawnBoss();
                break; // só 1 replacement por frame — o cooldown que acabou de começar cobre o resto deste ciclo
            }
        }

        bossTimer += Time.deltaTime;
        if (bossTimer < bossSettings.bossSpawnInterval) return;
        bossTimer = 0f;

        TrySpawnBoss();
    }

    private void TrySpawnBoss()
    {
        if (bossSettings.roster == null || bossSettings.roster.Count == 0) return;
        if (!TryPickBossDefinition(player.position, out var definition)) return; // pool do dia esgotada em todos os Tiers elegíveis — vaga fica pro próximo ciclo

        float minDistanceFromOrigin = worldSettings.safeZoneRadius + bossSettings.spawnSafetyPadding;
        if (!WorldSpawnUtility.TryGetSpawnPosition(
                player.position, bossSettings.bossReplacementSpawnMinDistance, bossSettings.bossReplacementSpawnMaxDistance,
                minDistanceFromOrigin, bossSettings.maxSpawnPositionAttempts, out var spawnPosition,
                bossSettings.spawnObstacleClearanceRadius, bossSettings.obstacleLayerMask))
        {
            return; // nenhuma posição válida nas tentativas — tenta de novo no próximo ciclo, nunca loop infinito (doc, Seção 11.6)
        }

        var bossObj = Instantiate(definition.prefab, spawnPosition, Quaternion.identity);
        WorldSpawnUtility.RestrictToOpenWorldGraph(bossObj);

        // Todo boss nasce já em combate, mesmo longe do jogador (doc, Seção 17.6: "Immediate
        // Aggro") — sem fase de patrulha/idle esperando observationRadius, diferente do
        // monstro comum.
        var controller = bossObj.GetComponent<EnemyController>();
        if (controller != null)
        {
            controller.SetIsBoss(true);
            controller.ForceImmediateAggro();
        }

        usedBossIdsToday.Add(definition); // só marca usado depois do spawn de fato acontecer — se a posição falhar acima, a entrada continua disponível
        aliveBosses.Add(bossObj);
    }

    // DominantTier = Tier de maior peso na distribuição atual (nunca um roll, diferente do
    // monstro comum). Se a pool desse Tier já esgotou hoje (todo BossDefinitionSO em
    // UsedBossIdsToday), avança pro próximo Tier por ordem de predominância (doc, Seção 17.4).
    private bool TryPickBossDefinition(Vector2 playerPosition, out BossDefinitionSO chosen)
    {
        chosen = null;

        float distanceFromOrigin = playerPosition.magnitude;
        int effectiveDay = WorldTierSelector.ComputeEffectiveSpawnDay(
            distanceFromOrigin, worldSettings.safeZoneRadius, worldSettings.distancePerSpawnDay, ActualDay);

        var dayDef = worldSettings.daySpawnTable != null ? worldSettings.daySpawnTable.GetDay(effectiveDay) : null;
        bool isNight = DayTimer.Instance != null && DayTimer.Instance.IsNight;
        WorldTierSelector.ComputeTierPercentsNonAlloc(dayDef, tierPercentsBuffer, isNight: isNight);

        for (int t = 0; t < WorldTierSelector.MonsterTierCount; t++) tierVisitedScratch[t] = false;

        for (int rank = 0; rank < WorldTierSelector.MonsterTierCount; rank++)
        {
            int tierIndex = PickNextStrongestTier();
            if (tierIndex < 0) break; // nenhum Tier com peso > 0 restante — defesa, tabela de dia não deveria vir zerada

            if (TryPickUnusedBossForTier(tierIndex + 1, out chosen)) return true;
        }

        return false;
    }

    // Acha o Tier de maior peso ainda não visitado nesta chamada de TryPickBossDefinition —
    // chamado em sequência pra percorrer a ordem de predominância inteira (maior peso primeiro).
    private int PickNextStrongestTier()
    {
        int bestIndex = -1;
        float bestPercent = 0f;
        for (int t = 0; t < WorldTierSelector.MonsterTierCount; t++)
        {
            if (tierVisitedScratch[t]) continue;
            if (tierPercentsBuffer[t] <= bestPercent) continue;
            bestPercent = tierPercentsBuffer[t];
            bestIndex = t;
        }

        if (bestIndex >= 0) tierVisitedScratch[bestIndex] = true;
        return bestIndex;
    }

    // "Bag sem repetição" dentro do Tier (doc, Seção 17.3) — sorteio uniforme entre os ainda
    // não usados hoje (decisão do usuário: sem peso configurável por BossDefinitionSO).
    private bool TryPickUnusedBossForTier(int tierNumber1Based, out BossDefinitionSO chosen)
    {
        chosen = null;
        candidateScratch.Clear();
        bool anyDefinitionForTier = false;

        foreach (var def in bossSettings.roster)
        {
            if (def == null || def.prefab == null || def.tier != tierNumber1Based) continue;
            anyDefinitionForTier = true;
            if (usedBossIdsToday.Contains(def)) continue;
            candidateScratch.Add(def);
        }

        // Pool do Tier esgotada HOJE, mas existe gente cadastrada nesse Tier (decisão do
        // usuário: diferente de esperar o próximo ActualDay pra reciclar — aqui reseta só os
        // BossDefinitionSO desse Tier específico e tenta de novo, pra continuar girando bosses
        // enquanto ainda sobrar tempo de dia). Não mexe nos outros Tiers.
        if (candidateScratch.Count == 0 && anyDefinitionForTier)
        {
            usedBossIdsToday.RemoveWhere(def => def.tier == tierNumber1Based);
            foreach (var def in bossSettings.roster)
            {
                if (def == null || def.prefab == null || def.tier != tierNumber1Based) continue;
                candidateScratch.Add(def);
            }
        }

        if (candidateScratch.Count == 0) return false; // nenhum BossDefinitionSO cadastrado nesse Tier ainda (bestiário incompleto)

        chosen = candidateScratch[Random.Range(0, candidateScratch.Count)];
        return true;
    }
}
