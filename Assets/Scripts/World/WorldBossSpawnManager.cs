using System.Collections.Generic;
using UnityEngine;

// Substitui o BossSpawnManager antigo (removido junto com o resto do sistema de Floor) —
// Sprint P7. Segue o design de docs/new/Mudanca_Estrutural_Mapa_Unico_Spawn_Bosses_
// ATUALIZADO_V2.md (Seção 17): Boss Timer decide QUANDO, a mesma distribuição de Tier usada
// pelo monstro comum (WorldTierSelector) decide QUAL Tier priorizar — mas aqui é por ORDEM DE
// PREDOMINÂNCIA (maior peso primeiro), nunca um roll.
//
// Identidade "bag sem repetição" (UsedBossIdsToday) — decisão do usuário (2026-10-21,
// corrigindo o comportamento antigo): esgotar os BossDefinitionSO de UM Tier NÃO reseta mais
// só aquele Tier sozinho — isso fazia o Tier dominante (maior %) nunca dar espaço pros outros,
// porque ele se auto-resetava e continuava sendo escolhido pra sempre. Agora, esgotar um Tier
// só avança pro PRÓXIMO Tier de maior peso ainda com boss disponível (TryPickFromRankedTiers);
// o reset geral (volta tudo a valer) só acontece quando TODOS os Tiers elegíveis (peso > 0)
// já esgotaram TODOS os seus BossDefinitionSO hoje. O reset diário (GameEvents.OnDayStart)
// continua existindo por cima disso.
public class WorldBossSpawnManager : MonoBehaviour
{
    public static WorldBossSpawnManager Instance { get; private set; }

    public WorldGenerationSettings worldSettings;
    public BossSpawnSettings bossSettings;

    [Tooltip("Quantos bosses nascem juntos a cada Boss Timer periódico (🔢 ajustável). Revertido pra 1 (decisão do usuário, 2026-10-25) — chegou a ser 2 por um tempo, mas voltou ao padrão de 1 boss por ciclo. NÃO se aplica ao replacement por distância, que continua repondo 1 por vez.")]
    public int bossesPerSpawnCycle = 1;

    [Header("Debug / Teste")]
    [Tooltip("Mesmo padrão de WorldPopulationManager/ChunkTerrainGenerator — ignora o Dia real da run e usa testDayOverride no lugar.")]
    public bool useTestDayOverride;
    [Range(1, 30)]
    public int testDayOverride = 1;

    [Header("Debug / Teste — Isolamento de Tier (só teste, não existe no jogo real)")]
    [Tooltip("Se marcado, ignora toda a % normal/ordem de predominância e força 100% dos bosses vindo só do testTierOverride — de propósito quebra o sistema de % (não tem mais % de tier nenhum enquanto isto estiver ligado). Depois de marcar/mudar o Tier, usa o botão de contexto abaixo pra resetar os bosses vivos e já nascer todo mundo sob a regra nova.")]
    public bool useTierIsolationOverride;
    [Range(1, 10)]
    public int testTierOverride = 1;

    private Transform player;
    private readonly List<GameObject> aliveBosses = new();
    private readonly HashSet<BossDefinitionSO> usedBossIdsToday = new();
    private readonly List<BossDefinitionSO> candidateScratch = new(); // reaproveitado em TryPickUnusedBossForTier
    private readonly List<Transform> spawnBatchScratch = new(); // reaproveitado em Update, pro Boss Timer periódico
    private readonly float[] tierPercentsBuffer = new float[WorldTierSelector.MonsterTierCount];
    private readonly bool[] tierVisitedScratch = new bool[WorldTierSelector.MonsterTierCount]; // ordem de predominância em TryPickBossDefinition

    private float bossTimer;
    private float replacementCooldownTimer; // proteção contra reroll por distância (doc, Seção 17) — andar de um lado pro outro na borda do bossDespawnDistance não gera replacement repetido

    private int ActualDay => useTestDayOverride ? testDayOverride : (MainMenuUI.CurrentRun?.day ?? 1);

    private void Awake() => Instance = this;

    // Chamado pelo ChunkTerrainGenerator.RegenerateWorld() — bug real reportado: o botão
    // "Regenerar Mundo" só repintava o bioma com o override do PRÓPRIO ChunkTerrainGenerator;
    // WorldPopulationManager/WorldBossSpawnManager tinham seus próprios campos
    // useTestDayOverride/testDayOverride, nunca sincronizados automaticamente — resultado: o
    // bioma mudava de Tier mas monstro/boss continuavam nascendo no Dia antigo (cada um lia o
    // próprio ActualDay, nunca atualizado). Sincroniza os 3 de uma vez só no clique do botão.
    public void SyncDayOverride(bool enabled, int day)
    {
        useTestDayOverride = enabled;
        testDayOverride = day;
    }

    // Chamado pelo DebugResetForTierIsolation do OUTRO manager (ver lá embaixo) — sem isso,
    // bug real reportado pelo usuário: isolar o Tier só em WorldBossSpawnManager (ou só em
    // WorldPopulationManager) deixava o outro lado inteiro de fora, nascendo bosses/monstros de
    // Tiers passados enquanto o lado "isolado" já estava certinho.
    public void SyncTierIsolation(bool enabled, int tier)
    {
        useTierIsolationOverride = enabled;
        testTierOverride = tier;
    }

    // Botão de contexto (clique direito no componente no Inspector) — espelha o bool/Tier pro
    // WorldPopulationManager ANTES de limpar, então 1 clique em QUALQUER um dos dois já isola
    // monstro comum E boss juntos, sempre no mesmo Tier.
    [ContextMenu("Debug: Resetar e isolar Tier")]
    public void DebugResetForTierIsolation()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[WorldBossSpawnManager] \"Resetar e isolar Tier\" só funciona em Play Mode.");
            return;
        }

        WorldPopulationManager.Instance?.SyncTierIsolation(useTierIsolationOverride, testTierOverride);
        GameEvents.ClearAllEnemies();
        LootDrop.DestroyAllOnGround(); // só debug — poluição visual/FPS numa sessão de teste, nunca no fluxo normal
        Debug.Log(useTierIsolationOverride
            ? $"[WorldBossSpawnManager] Isolamento de Tier ativo — só nasce Tier {testTierOverride} agora (monstro comum e boss)."
            : "[WorldBossSpawnManager] useTierIsolationOverride está desmarcado — resetou, mas a distribuição normal por % continua valendo.");
    }

    private void OnEnable()
    {
        GameEvents.OnHeroSpawned += HandleHeroSpawned;
        GameEvents.OnDayStart += HandleDayStart;
        GameEvents.OnClearAllEnemies += HandleClearAllEnemies;
    }

    private void OnDisable()
    {
        GameEvents.OnHeroSpawned -= HandleHeroSpawned;
        GameEvents.OnDayStart -= HandleDayStart;
        GameEvents.OnClearAllEnemies -= HandleClearAllEnemies;
    }

    private void HandleHeroSpawned(Transform hero) => player = hero;

    // Novo ActualDay -> UsedBossIdsToday.Clear() (doc, Seção 17.3) — mesmo gatilho que
    // DayTimer.ResetForNewDay() já usa pra avisar o resto do jogo (ShopHandler/MainMenuUI).
    private void HandleDayStart() => usedBossIdsToday.Clear();

    // "Regenerar Mundo" / compra de Tier Suppression — nunca é morte (sem loot/kill). Não mexe
    // em usedBossIdsToday nem em bossTimer de propósito — a identidade diária e o timer
    // continuam contando normal, só os bosses vivos agora são removidos (o próximo Boss Timer
    // já respeita Tier Suppression sozinho, via TryPickBossDefinition).
    private void HandleClearAllEnemies()
    {
        foreach (var boss in aliveBosses)
        {
            if (boss != null) Destroy(boss);
        }
        aliveBosses.Clear();
    }

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
                // em bossTimer (Replacement != novo Boss Timer Event). Replacement sempre repõe
                // só 1 (quem nasce de novo é 1 boss só, diferente do Boss Timer periódico).
                Destroy(aliveBosses[i]);
                aliveBosses.RemoveAt(i);
                replacementCooldownTimer = bossSettings.bossReplacementCooldown;
                var replacement = SpawnOneBoss();
                if (replacement != null) GameEvents.BossGroupSpawned(new[] { replacement.transform });
                break; // só 1 replacement por frame — o cooldown que acabou de começar cobre o resto deste ciclo
            }
        }

        bossTimer += Time.deltaTime;
        if (bossTimer < bossSettings.bossSpawnInterval) return;
        bossTimer = 0f;

        // Boss Timer periódico — spawna bossesPerSpawnCycle bosses juntos (hoje, 1 de novo) e
        // dispara UM reveal só com o grupo inteiro (o reveal em si continua funcionando pra
        // qualquer tamanho de grupo, não precisou mudar nada em BossEncounterCameraController).
        spawnBatchScratch.Clear();
        for (int i = 0; i < bossesPerSpawnCycle; i++)
        {
            var spawned = SpawnOneBoss();
            if (spawned != null) spawnBatchScratch.Add(spawned.transform);
        }
        if (spawnBatchScratch.Count > 0) GameEvents.BossGroupSpawned(spawnBatchScratch.ToArray()); // snapshot — spawnBatchScratch é reaproveitado no próximo ciclo
    }

    private GameObject SpawnOneBoss()
    {
        if (bossSettings.roster == null || bossSettings.roster.Count == 0) return null;
        if (!TryPickBossDefinition(player.position, out var definition)) return null; // pool do dia esgotada em todos os Tiers elegíveis — vaga fica pro próximo ciclo

        float minDistanceFromOrigin = worldSettings.safeZoneRadius + bossSettings.spawnSafetyPadding;
        // Rolado por spawn individual (não por BossDefinitionSO) — mesmo critério do monstro comum.
        bool preferOutsideView = Random.value < bossSettings.outsideCameraViewChance;
        if (!WorldSpawnUtility.TryGetSpawnPosition(
                player.position, bossSettings.bossReplacementSpawnMinDistance, bossSettings.bossReplacementSpawnMaxDistance,
                minDistanceFromOrigin, bossSettings.maxSpawnPositionAttempts, out var spawnPosition,
                bossSettings.spawnObstacleClearanceRadius, bossSettings.obstacleLayerMask, preferOutsideView))
        {
            return null; // nenhuma posição válida nas tentativas — tenta de novo no próximo ciclo, nunca loop infinito (doc, Seção 11.6)
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

        return bossObj;
    }

    // DominantTier = Tier de maior peso na distribuição atual (nunca um roll, diferente do
    // monstro comum). Se a pool desse Tier já esgotou hoje (todo BossDefinitionSO em
    // UsedBossIdsToday), avança pro próximo Tier por ordem de predominância (doc, Seção 17.4) —
    // isso é o que "dá espaço" pros Tiers de peso menor nascerem (exemplo do usuário: 50%
    // Tier1/25% Tier2/25% Tier3, Tier1 esgotado -> tenta Tier2, depois Tier3). Só se TODOS os
    // Tiers elegíveis (peso > 0) esgotarem é que reseta geral (usedBossIdsToday.Clear()) e
    // tenta a rodada inteira de novo, uma única vez.
    private bool TryPickBossDefinition(Vector2 playerPosition, out BossDefinitionSO chosen)
    {
        float distanceFromOrigin = playerPosition.magnitude;
        int effectiveDay = WorldTierSelector.ComputeEffectiveSpawnDay(
            distanceFromOrigin, worldSettings.safeZoneRadius, worldSettings.distancePerSpawnDay, ActualDay);

        var dayDef = worldSettings.daySpawnTable != null ? worldSettings.daySpawnTable.GetDay(effectiveDay) : null;
        bool isNight = DayTimer.Instance != null && DayTimer.Instance.IsNight;
        int suppressedTierCount = MainMenuUI.CurrentRun?.suppressedTierCount ?? 0;
        int forcedTierIndex = useTierIsolationOverride ? testTierOverride - 1 : -1;
        WorldTierSelector.ComputeTierPercentsNonAlloc(dayDef, tierPercentsBuffer, suppressedTierCount, isNight, forcedTierIndex);

        if (TryPickFromRankedTiers(out chosen)) return true;

        usedBossIdsToday.Clear();
        return TryPickFromRankedTiers(out chosen);
    }

    // Percorre os Tiers elegíveis (peso > 0) em ordem de predominância, maior peso primeiro,
    // e usa o primeiro que ainda tiver algum BossDefinitionSO não usado hoje.
    private bool TryPickFromRankedTiers(out BossDefinitionSO chosen)
    {
        chosen = null;
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
    // não usados hoje (decisão do usuário: sem peso configurável por BossDefinitionSO). SEM
    // auto-reset por Tier aqui (ver comentário de TryPickBossDefinition) — exaurir este Tier só
    // retorna false, pra TryPickFromRankedTiers avançar pro próximo Tier da ordem de
    // predominância. Vale tanto pra Tier sem nenhum BossDefinitionSO cadastrado (bestiário
    // incompleto) quanto pra Tier com todos já usados hoje — os dois casos dão passagem igual.
    private bool TryPickUnusedBossForTier(int tierNumber1Based, out BossDefinitionSO chosen)
    {
        chosen = null;
        candidateScratch.Clear();

        foreach (var def in bossSettings.roster)
        {
            if (def == null || def.prefab == null || def.tier != tierNumber1Based) continue;
            if (usedBossIdsToday.Contains(def)) continue;
            candidateScratch.Add(def);
        }

        if (candidateScratch.Count == 0) return false;

        chosen = candidateScratch[Random.Range(0, candidateScratch.Count)];
        return true;
    }
}
