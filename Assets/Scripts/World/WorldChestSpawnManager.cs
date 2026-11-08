using UnityEngine;

// Spawn de Baú no mundo aberto (GDD Deadline 8.1, Seção 8) — mesmo padrão arquitetural de
// WorldBossSpawnManager/WorldPopulationManager (singleton Instance, GameEvents.OnHeroSpawned
// guarda o player, WorldSpawnUtility.TryGetSpawnPosition pro anel ao redor dele), mas bem mais
// simples: SEM Tier/roster, SEM Timer periódico, SEM despawn por distância/replacement — é 1
// roll só por dia (ChestSpawnSettings.chestSpawnChancePercent), que pode nem acontecer
// (decisão do usuário: baú é raro/forte de propósito). Se acontecer, nasce exatamente 1 baú,
// que só é removido quando aberto (Chest/ChestMimicController já cuidam disso sozinhos) ou
// quando o mundo inteiro é limpo (HandleClearAllEnemies abaixo).
//
// Funcionamento do Chest_Popup (perks/talentos de recompensa) fica pra depois — decisão do
// usuário (2026-11-08): "no máximo a gente já pode fazer os spawns deles, mas o funcionamento
// deixa só pra quando a gente terminar os perks e talentos". Este arquivo é só o spawn.
public class WorldChestSpawnManager : MonoBehaviour
{
    public static WorldChestSpawnManager Instance { get; private set; }

    public WorldGenerationSettings worldSettings;
    public ChestSpawnSettings chestSettings;

    [Header("Prefabs")]
    [Tooltip("Baú normal (Assets/Prefabs/World/Chest.prefab).")]
    public GameObject chestPrefab;
    [Tooltip("Chest Mimic disfarçado — mesma sprite de baú fechado, só revela o monstro ao interagir.")]
    public GameObject chestMimicPrefab;

    private Transform player;
    private GameObject aliveChest; // só 1 por dia nesta fase — null = nenhum baú vivo agora

    // Pra WorldTargetPointerUI (seta de borda de tela) — null quando não tem baú vivo hoje.
    public Transform AliveChestTransform => aliveChest != null ? aliveChest.transform : null;

    // Find-or-create "//CHESTS" — mesmo padrão de EnemyController.EnemysContainer/
    // BossesContainer/lootContainer (GameplayBootstrap.herosContainer também segue isso).
    private static Transform chestsContainer;
    private static Transform ChestsContainer
    {
        get
        {
            if (chestsContainer == null)
            {
                var existing = GameObject.Find("//CHESTS");
                chestsContainer = existing != null ? existing.transform : new GameObject("//CHESTS").transform;
            }
            return chestsContainer;
        }
    }

    private void Awake() => Instance = this;

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

    // "Regenerar Mundo"/novo dia — mesmo motivo de WorldBossSpawnManager.HandleClearAllEnemies:
    // um baú (aberto ou não) não faz sentido sobreviver pro próximo dia, nem fisicamente (o
    // mundo em volta mudou) nem logicamente (a Seção 8 não prevê baú "guardado" de um dia pro
    // outro).
    private void HandleClearAllEnemies()
    {
        if (aliveChest != null) Destroy(aliveChest);
        aliveChest = null;
    }

    private void HandleDayStart()
    {
        if (chestSettings == null || worldSettings == null || player == null) return;
        if (Random.value * 100f >= chestSettings.chestSpawnChancePercent) return; // maioria dos dias: nenhum baú — de propósito

        SpawnOneChest();
    }

    private void SpawnOneChest()
    {
        float minDistanceFromOrigin = worldSettings.safeZoneRadius + chestSettings.spawnSafetyPadding;

        // Decisão do usuário (2026-11-08, com desenho): baú só nasce a partir de
        // minChunksAway chunks de distância do jogador — força exploração, nunca nasce perto.
        // Chebyshev-distance(chunk) >= minChunksAway implica Euclidean >= minChunksAway *
        // chunkSize sempre (e o inverso: Chebyshev <= minChunksAway-1 implica Euclidean <
        // minChunksAway * chunkSize) — então um anel circular com esse raio reproduz exatamente
        // a zona quadrada do desenho, sem precisar de um check de grid separado.
        float spawnMinDistance = chestSettings.minChunksAway * (float)worldSettings.chunkSize;
        float spawnMaxDistance = chestSettings.maxChunksAway * (float)worldSettings.chunkSize;

        if (!WorldSpawnUtility.TryGetSpawnPosition(
                player.position, spawnMinDistance, spawnMaxDistance,
                minDistanceFromOrigin, chestSettings.maxSpawnPositionAttempts, out var spawnPosition,
                chestSettings.spawnObstacleClearanceRadius, chestSettings.obstacleLayerMask))
        {
            return; // nenhuma posição válida — sem baú hoje, não trava em loop (mesmo critério do Boss)
        }

        bool isMimic = chestMimicPrefab != null && Random.value * 100f < chestSettings.mimicChancePercent;
        GameObject prefab = isMimic ? chestMimicPrefab : chestPrefab;
        if (prefab == null) return;

        aliveChest = Instantiate(prefab, spawnPosition, Quaternion.identity, ChestsContainer);
        WorldSpawnUtility.RestrictToOpenWorldGraph(aliveChest); // no-op pro Chest normal (sem Seeker) — só importa pro Mimic
    }
}
