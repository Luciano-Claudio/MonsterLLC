using System.Collections.Generic;
using UnityEngine;

// Etapas 1+2 (docs/superpowers/specs/2026-11-24-etapas-1-2-run-25min-cartas-design.md, 5.2/5.3/5.6) —
// 1 boss por minuto, na ORDEM do RunSettings (escolhida pelo usuário), via DayTimer.Schedule.
// - Vários bosses podem estar vivos; só o Divine God (último espaço) segura o relógio em 0.
// - Enrage: com 1 min vivo ganha velocidade, com 2 min ganha dano (e o dano continua subindo).
// - À noite, +velocidade (RunSettings.nightSpeedMultiplier), somado ao enrage.
// - Boss longe demais do herói é teleportado pra perto (fora da câmera), como antes.
// Saiu: Boss Timer por dia, escolha por peso de tier e a "bag sem repetição" (a ordem é fixa).
public class WorldBossSpawnManager : MonoBehaviour
{
    public static WorldBossSpawnManager Instance { get; private set; }

    public WorldGenerationSettings worldSettings;
    public BossSpawnSettings bossSettings; // distâncias de spawn/teleporte (o intervalo agora vem do RunSettings)

    private Transform player;
    private readonly List<EnemyController> aliveBosses = new();
    public int AliveCount => aliveBosses.Count;

    private int spawnedSlots;
    private EnemyController finalBoss;
    private bool finalBossKilledFired;
    public int BossesKilled { get; private set; }

    private float replacementCooldownTimer; // proteção contra teleporte repetido na borda do bossDespawnDistance

    private void Awake() => Instance = this;

    // Mantidos só por compatibilidade com os botões de debug antigos (dia/tier não existem mais na run).
    public void SyncDayOverride(bool enabled, int day) { }
    public void SyncTierIsolation(bool enabled, int tier) { }

    private void OnEnable()
    {
        GameEvents.OnHeroSpawned += HandleHeroSpawned;
        GameEvents.OnDayStart += HandleRunStart;
        GameEvents.OnClearAllEnemies += HandleClearAllEnemies;
        GameEvents.OnEnemyKilled += HandleEnemyKilled;
    }

    private void OnDisable()
    {
        GameEvents.OnHeroSpawned -= HandleHeroSpawned;
        GameEvents.OnDayStart -= HandleRunStart;
        GameEvents.OnClearAllEnemies -= HandleClearAllEnemies;
        GameEvents.OnEnemyKilled -= HandleEnemyKilled;
    }

    private void HandleHeroSpawned(Transform hero) => player = hero;

    private void HandleRunStart()
    {
        spawnedSlots = 0;
        finalBoss = null;
        finalBossKilledFired = false;
        BossesKilled = 0;
    }

    private void HandleEnemyKilled(int energy, bool isBoss)
    {
        if (isBoss) BossesKilled++;
    }

    private void HandleClearAllEnemies()
    {
        foreach (var boss in aliveBosses)
        {
            if (boss != null) Destroy(boss.gameObject);
        }
        aliveBosses.Clear();
    }

    private void Update()
    {
        if (!GameplayGate.IsActive || HeroController.IsDying) return;
        Tick(Time.deltaTime);
    }

    private void Tick(float deltaTime)
    {
        var timer = DayTimer.Instance;
        if (timer == null || timer.Schedule == null || player == null || worldSettings == null || bossSettings == null) return;

        for (int i = aliveBosses.Count - 1; i >= 0; i--)
            if (aliveBosses[i] == null || aliveBosses[i].IsDead) aliveBosses.RemoveAt(i);

        UpdateFinalBoss(timer);
        SpawnDueBosses(timer);
        ApplyRunMultipliers(timer);
        TeleportFarBoss(deltaTime);
    }

    private void UpdateFinalBoss(DayTimer timer)
    {
        if (finalBoss == null && !finalBossKilledFired) { timer.FinalBossAlive = false; return; }
        if (finalBossKilledFired) return;
        if (finalBoss.IsDead)
        {
            finalBossKilledFired = true;
            timer.FinalBossAlive = false;
            GameEvents.FinalBossKilled();
        }
    }

    private void SpawnDueBosses(DayTimer timer)
    {
        var schedule = timer.Schedule;
        var order = timer.Settings != null ? timer.Settings.bossOrder : null;
        int due = schedule.SlotsDueBy(timer.RunElapsed);
        while (spawnedSlots < due)
        {
            var definition = order != null && spawnedSlots < order.Count ? order[spawnedSlots] : null;
            if (definition == null || definition.prefab == null) { spawnedSlots++; continue; } // espaço vazio: minuto sem boss

            var spawned = SpawnBoss(definition);
            if (spawned == null) return; // nenhuma posição válida agora — tenta de novo no próximo frame
            spawnedSlots++;
            if (definition.tier == 10)
            {
                finalBoss = spawned;
                timer.FinalBossAlive = true;
            }
            GameEvents.BossGroupSpawned(new[] { spawned.transform });
        }
    }

    private void ApplyRunMultipliers(DayTimer timer)
    {
        var schedule = timer.Schedule;
        float night = timer.IsNight && timer.Settings != null ? timer.Settings.nightSpeedMultiplier : 1f;
        for (int i = 0; i < aliveBosses.Count; i++)
        {
            var boss = aliveBosses[i];
            boss.SetRunMultipliers(schedule.EnrageSpeedMultiplier(boss.AliveSeconds) * night,
                schedule.EnrageDamageMultiplier(boss.AliveSeconds));
        }
    }

    private void TeleportFarBoss(float deltaTime)
    {
        if (replacementCooldownTimer > 0f) { replacementCooldownTimer -= deltaTime; return; }
        for (int i = aliveBosses.Count - 1; i >= 0; i--)
        {
            float distance = Vector2.Distance(aliveBosses[i].transform.position, player.position);
            if (distance <= bossSettings.bossDespawnDistance) continue;
            if (TeleportBossNearPlayer(aliveBosses[i])) replacementCooldownTimer = bossSettings.bossReplacementCooldown;
            break; // só 1 por frame
        }
    }

    private bool TeleportBossNearPlayer(EnemyController boss)
    {
        float minDistanceFromOrigin = worldSettings.safeZoneRadius + bossSettings.spawnSafetyPadding;
        if (!WorldSpawnUtility.TryGetSpawnPosition(
                player.position, bossSettings.bossReplacementSpawnMinDistance, bossSettings.bossReplacementSpawnMaxDistance,
                minDistanceFromOrigin, bossSettings.maxSpawnPositionAttempts, out var newPosition,
                bossSettings.spawnObstacleClearanceRadius, bossSettings.obstacleLayerMask, preferOutsideCameraView: true))
        {
            return false;
        }

        boss.CancelAttackForTeleport(); // cancela golpe telegrafado ANTES do salto
        boss.transform.position = newPosition;
        return true;
    }

    private EnemyController SpawnBoss(BossDefinitionSO definition)
    {
        float minDistanceFromOrigin = worldSettings.safeZoneRadius + bossSettings.spawnSafetyPadding;
        bool preferOutsideView = Random.value < bossSettings.outsideCameraViewChance;
        Vector3 spawnPosition;
#if UNITY_EDITOR
        if (debugFixedSpawn) spawnPosition = player.position + Vector3.right * 8f; else
#endif
        if (!WorldSpawnUtility.TryGetSpawnPosition(
                player.position, bossSettings.bossReplacementSpawnMinDistance, bossSettings.bossReplacementSpawnMaxDistance,
                minDistanceFromOrigin, bossSettings.maxSpawnPositionAttempts, out spawnPosition,
                bossSettings.spawnObstacleClearanceRadius, bossSettings.obstacleLayerMask, preferOutsideView))
        {
            return null;
        }

        var bossObj = Instantiate(definition.prefab, spawnPosition, Quaternion.identity, EnemyController.BossesContainer);
        WorldSpawnUtility.RestrictToOpenWorldGraph(bossObj);
        var controller = bossObj.GetComponent<EnemyController>();
        if (controller == null) { Destroy(bossObj); return null; }
        controller.SetIsBoss(true);
        controller.SetSpawnInfo(definition.tier, false);
        controller.ForceImmediateAggro();
        aliveBosses.Add(controller);
        return controller;
    }

#if UNITY_EDITOR
    // Só para verificação no Editor (eval): roda 1 passo ignorando pausa/estado do jogo.
    // Fora do Play Mode não existe mundo pra achar posição de spawn: a eval usa uma posição fixa.
    private bool debugFixedSpawn;

    public void DebugTick(Transform hero)
    {
        player = hero;
        debugFixedSpawn = !Application.isPlaying;
        Tick(0f);
    }

    public IReadOnlyList<EnemyController> DebugAliveBosses => aliveBosses;
#endif
}
