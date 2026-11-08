using System.Collections.Generic;
using UnityEngine;

// Spawn de Baú no mundo aberto (GDD Deadline 8.1, Seção 8) — mesmo padrão arquitetural de
// WorldBossSpawnManager/WorldPopulationManager (singleton Instance, GameEvents.OnHeroSpawned
// guarda o player, WorldSpawnUtility.TryGetSpawnPosition pro anel ao redor dele), mas bem mais
// simples: SEM Tier/roster, SEM Timer periódico, SEM despawn por distância/replacement — é 1
// roll só por dia (ChestSpawnSettings.chestSpawnChancePercent), que pode nem acontecer
// (decisão do usuário: baú é raro/forte de propósito). Se acontecer, nasce 1 baú (+1/rank de
// Baú Extra, World Perk — ver WorldPerkEffects.ExtraChestCount), que só é removido quando
// aberto (Chest/ChestMimicController já cuidam disso sozinhos) ou quando o mundo inteiro é
// limpo (HandleClearAllEnemies abaixo).
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

    // Baú Extra (World Perk, Seção 8, item 12) — +1 baú instanciado/rank, por isso lista (não
    // mais 1 GameObject só). Cada instância rola Mimic de forma independente.
    private readonly List<GameObject> aliveChests = new();

    // Pra WorldTargetPointerUI (seta de borda de tela) — aponta pro mais PRÓXIMO do jogador
    // (não só "o primeiro"), já que agora pode ter mais de 1 vivo ao mesmo tempo. Null quando
    // não tem baú vivo nenhum hoje.
    public Transform AliveChestTransform
    {
        get
        {
            Transform nearest = null;
            float nearestSqrDistance = float.MaxValue;
            foreach (var chest in aliveChests)
            {
                if (chest == null) continue;
                float sqrDistance = player != null
                    ? (chest.transform.position - player.position).sqrMagnitude
                    : 0f;
                if (nearest != null && sqrDistance >= nearestSqrDistance) continue;
                nearest = chest.transform;
                nearestSqrDistance = sqrDistance;
            }
            return nearest;
        }
    }

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
        foreach (var chest in aliveChests)
        {
            if (chest != null) Destroy(chest);
        }
        aliveChests.Clear();
    }

    private void HandleDayStart()
    {
        if (chestSettings == null || worldSettings == null || player == null) return;
        if (Random.value * 100f >= chestSettings.chestSpawnChancePercent) return; // maioria dos dias: nenhum baú — de propósito

        // Baú Extra (World Perk) — +1 baú/rank, cada um com sua própria tentativa de posição
        // e seu próprio roll de Mimic independente.
        int chestCount = 1 + WorldPerkEffects.ExtraChestCount;
        for (int i = 0; i < chestCount; i++) SpawnOneChest();
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

        var spawned = Instantiate(prefab, spawnPosition, Quaternion.identity, ChestsContainer);
        WorldSpawnUtility.RestrictToOpenWorldGraph(spawned); // no-op pro Chest normal (sem Seeker) — só importa pro Mimic
        aliveChests.Add(spawned);
    }
}
