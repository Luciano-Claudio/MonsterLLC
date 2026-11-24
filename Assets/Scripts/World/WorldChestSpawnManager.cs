using System.Collections.Generic;
using UnityEngine;

// Baú (e mímico) no mundo aberto — etapa 3 (docs/superpowers/specs/2026-11-24-etapa-3-bau-vendedor-itens-design.md, 5.4).
// - No início da run, sorteia os segundos de aparição (RewardSchedule: RunSettings.chestMin/MaxCount,
//   espalhados em faixas iguais, nada nos primeiros rewardQuietSeconds).
// - Na hora certa nasce 1 baú (mimicChancePercent de virar mímico) num anel ao redor do herói
//   (ChestSpawnSettings.min/maxChunksAway). Só 1 baú vivo por vez: se a hora do próximo chega com
//   um ainda fechado, ela ESPERA (nunca some nem duplica).
// - Abrir/matar o mímico -> ChestRewardSystem -> oferta de 3 cartas (HeroCards, fonte Chest).
// Mesmo padrão dos outros spawners (Instance, OnHeroSpawned guarda o player, WorldSpawnUtility).
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
    private GameObject aliveChest;
    private float[] chestTimes = new float[0];
    private int nextChest;
    private readonly System.Random rng = new();

    // Pra WorldTargetPointerUI (seta de borda de tela). Null quando não tem baú vivo.
    public Transform AliveChestTransform => aliveChest != null ? aliveChest.transform : null;
    public IReadOnlyList<float> ChestTimes => chestTimes;

    // Find-or-create "//CHESTS" — mesmo padrão de EnemyController.EnemysContainer/BossesContainer.
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
        GameEvents.OnDayStart += HandleRunStart;
        GameEvents.OnClearAllEnemies += HandleClearAllEnemies;
    }

    private void OnDisable()
    {
        GameEvents.OnHeroSpawned -= HandleHeroSpawned;
        GameEvents.OnDayStart -= HandleRunStart;
        GameEvents.OnClearAllEnemies -= HandleClearAllEnemies;
    }

    private void HandleHeroSpawned(Transform hero) => player = hero;

    // "Regenerar Mundo": o baú não sobrevive à limpeza do mundo.
    private void HandleClearAllEnemies()
    {
        if (aliveChest != null) Destroy(aliveChest);
        aliveChest = null;
    }

    // Início da run (DayTimer.StartRun dispara OnDayStart): agenda nova.
    private void HandleRunStart()
    {
        HandleClearAllEnemies();
        var s = RunSettings.Current;
        chestTimes = s.chestsEnabled
            ? RewardSchedule.Build(s.chestMinCount, s.chestMaxCount, s.runSeconds, s.rewardQuietSeconds, rng)
            : new float[0];
        nextChest = 0;
    }

    private void Update()
    {
        if (!GameplayGate.IsActive || HeroController.IsDying) return;
        Tick();
    }

    private void Tick()
    {
        var timer = DayTimer.Instance;
        if (timer == null || player == null) return;
        float now = timer.RunElapsed;

        // Hora do próximo baú chegou e não há baú vivo: nasce. Com baú vivo, a hora espera.
        if (nextChest < chestTimes.Length && now >= chestTimes[nextChest] && aliveChest == null)
        {
            if (SpawnOneChest()) nextChest++;
        }
    }

    private bool SpawnOneChest()
    {
        if (chestSettings == null || worldSettings == null) return false;
        if (!TryGetRingPosition(out var spawnPosition)) return false; // sem lugar agora: tenta no próximo frame

        bool isMimic = chestMimicPrefab != null && Random.value * 100f < chestSettings.mimicChancePercent;
        GameObject prefab = isMimic ? chestMimicPrefab : chestPrefab;
        if (prefab == null) return false;

        var spawned = Instantiate(prefab, spawnPosition, Quaternion.identity, ChestsContainer);
        WorldSpawnUtility.RestrictToOpenWorldGraph(spawned); // no-op pro Chest normal (sem Seeker) — só importa pro Mimic
        aliveChest = spawned;
        return true;
    }

    // Anel de spawn (decisão do usuário, 2026-11-08): a partir de minChunksAway chunks do jogador —
    // força exploração, nunca nasce em cima dele.
    private bool TryGetRingPosition(out Vector3 position)
    {
#if UNITY_EDITOR
        if (debugFixedSpawn) { position = player.position + Vector3.right * 8f; return true; }
#endif
        float minDistanceFromOrigin = worldSettings.safeZoneRadius + chestSettings.spawnSafetyPadding;
        float spawnMinDistance = chestSettings.minChunksAway * (float)worldSettings.chunkSize;
        float spawnMaxDistance = chestSettings.maxChunksAway * (float)worldSettings.chunkSize;
        return WorldSpawnUtility.TryGetSpawnPosition(
            player.position, spawnMinDistance, spawnMaxDistance,
            minDistanceFromOrigin, chestSettings.maxSpawnPositionAttempts, out position,
            chestSettings.spawnObstacleClearanceRadius, chestSettings.obstacleLayerMask);
    }

#if UNITY_EDITOR
    // Só para verificação no Editor (eval): roda um tick ignorando pausa, com posição fixa.
    private bool debugFixedSpawn;

    public void DebugTick(Transform hero)
    {
        player = hero;
        debugFixedSpawn = !Application.isPlaying;
        Tick();
    }
#endif
}
