using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Telemetria de run pra discussao de balanceamento/diversao (foco no Barbarian) — grava um
// JSON completo por sessao de jogo. Design: docs/superpowers/specs/2026-11-16-run-telemetry-design.md
// Plano: docs/superpowers/plans/2026-11-16-run-telemetry.md
//
// So observa — nunca altera economia/dano/drop. Nasce via SystemsBootstrap.Awake()
// (AddComponent + Init), igual DayStatsTracker/KillGoldReward. Desligado (enabled=false no
// Init): nao assina nenhum evento, nao roda timer, nao cria pasta/arquivo nenhum — custo zero.
public class RunTelemetryRecorder : MonoBehaviour
{
    // Guarda de singleton (Revisao do builder-check, item 3) — "Voltar ao Menu Principal"
    // recarrega a cena Main_Menu, que tem seu proprio GameObject "Systems"/SystemsBootstrap,
    // entao um 2o RunTelemetryRecorder nasceria assinando os mesmos eventos estaticos ao mesmo
    // tempo que o 1o. A copia nova se autodestroi aqui; o original (e seu runId/arquivo)
    // sobrevive ao reload sem interrupcao.
    public static RunTelemetryRecorder Instance { get; private set; }

    private bool telemetryEnabled;
    private RunTelemetryData data;
    private string filePath;

    // Fronteiras em aberto — não-nulo entre GameEvents.OnDayStart e o OnDayEnd/abandono
    // seguinte (currentDayAttempt) ou entre OnDayEnd e o próximo OnDayStart (currentShopVisit).
    // Consumidos pelas Tasks 6/7 (sampler e registro de vendas/perks).
    private DayAttemptRecord currentDayAttempt;
    private ShopVisitRecord currentShopVisit;
    private readonly Dictionary<int, int> attemptsPerDay = new();

    // Acumuladores de ociosidade — incrementados no Update() do sampler (Task 6), resetados
    // aqui a cada início de dia e em HandleDamageTaken conforme a direção do hit.
    private float secondsSinceLastHitDealt;
    private float secondsSinceLastHitReceived;

    // Só pra teste manual (Test 5) — nenhum código de produção lê isto.
    public int DebugCurrentAttemptIndex => currentDayAttempt?.attemptIndex ?? -1;

    private float sampleTimer;

    // Só pra teste manual (Test 6) — nenhum código de produção lê isto.
    public int DebugSnapshotCount => currentDayAttempt?.snapshots.Count ?? 0;
    public void DebugTickSampler(float deltaTime) => TickSampler(deltaTime);

    // Timer manual (não Coroutine — GameplayGate nunca pausa Time.timeScale, padrão repetido
    // no resto do projeto) — amostra 1x por segundo de jogo ativo. Os 2 acumuladores de
    // ociosidade incrementam junto, só enquanto o gate estiver ativo.
    private void Update() => TickSampler(Time.deltaTime);

    private void TickSampler(float deltaTime)
    {
        if (!telemetryEnabled) return;
        if (!GameplayGate.IsActive || currentDayAttempt == null) return;

        secondsSinceLastHitDealt += deltaTime;
        secondsSinceLastHitReceived += deltaTime;

        sampleTimer += deltaTime;
        if (sampleTimer < 1f) return;
        sampleTimer = 0f;
        CaptureSnapshot();
    }

    private void CaptureSnapshot()
    {
        var hero = HeroController.Instance;
        currentDayAttempt.snapshots.Add(new SnapshotRecord
        {
            secondOfDay = DayTimer.Instance.CurrentDayDuration - DayTimer.Instance.timeRemaining,
            aliveMonsters = WorldPopulationManager.Instance != null ? WorldPopulationManager.Instance.AliveCount : 0,
            aliveBosses = WorldBossSpawnManager.Instance != null ? WorldBossSpawnManager.Instance.AliveCount : 0,
            heroHpPercent = hero != null ? hero.stats.health / hero.EffectiveMaxHealth * 100f : 0f,
            heroPosition = hero != null ? new SerializableVector2(hero.transform.position) : new SerializableVector2(),
            gold = MainMenuUI.CurrentRun?.gold ?? 0,
            secondsSinceLastHitDealt = secondsSinceLastHitDealt,
            secondsSinceLastHitReceived = secondsSinceLastHitReceived,
            isNight = DayTimer.Instance.IsNight,
        });
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    // Chamado 1x por SystemsBootstrap.Awake(), junto com os outros managers "globais" do
    // GameObject "Systems" (mesmo padrao de DayStatsTracker.Init()/KillGoldReward.Init()).
    public void Init(bool enabled)
    {
        telemetryEnabled = enabled;
        if (!telemetryEnabled) return; // custo zero — nenhum dado criado, nenhum evento assinado

        string runId = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        data = new RunTelemetryData
        {
            runId = runId,
            hero = MainMenuUI.CurrentRun?.hero,
            startedAtUtc = DateTime.UtcNow.ToString("o"),
        };
        filePath = Path.Combine(Application.persistentDataPath, "Telemetry", $"run_{runId}.json");

        GameEvents.OnDayStart += HandleDayStart;
        GameEvents.OnDayEnd += HandleDayEnd;
        GameEvents.OnEnemyKilledDetailed += HandleEnemyKilled;
        GameEvents.OnDamageTaken += HandleDamageTaken;
    }

    // Fecha a visita à loja em aberto (se houver — não há na 1ª vez, Dia 1) e abre a tentativa
    // de dia nova. attemptsPerDay conta por NÚMERO de dia (não globalmente) — um retry depois
    // de morrer reabre o MESMO dayNumber, então a 2ª tentativa daquele número vira attemptIndex=2,
    // enquanto o próximo número de dia (sobrevivência) começa do 1 de novo.
    private void HandleDayStart()
    {
        if (currentShopVisit != null)
        {
            data.shopVisits.Add(currentShopVisit);
            currentShopVisit = null;
        }

        int dayNumber = MainMenuUI.CurrentRun.day;
        attemptsPerDay[dayNumber] = attemptsPerDay.GetValueOrDefault(dayNumber) + 1;
        currentDayAttempt = new DayAttemptRecord
        {
            dayNumber = dayNumber,
            attemptIndex = attemptsPerDay[dayNumber],
            startedAtUtc = DateTime.UtcNow.ToString("o"),
        };
        secondsSinceLastHitDealt = 0f;
        secondsSinceLastHitReceived = 0f;
    }

    // Fecha a tentativa de dia em aberto (sobrevivência OU morte — OnDayEnd dispara nos 2
    // casos, DayResolver.PlayerSurvivedLastDay diferencia) e abre a visita à loja que segue.
    private void HandleDayEnd()
    {
        if (currentDayAttempt == null) return; // segurança — não deveria disparar sem HandleDayStart antes

        float elapsed = DayTimer.Instance.CurrentDayDuration - DayTimer.Instance.timeRemaining;
        currentDayAttempt.outcome = DayResolver.PlayerSurvivedLastDay ? "survived" : "died";
        currentDayAttempt.elapsedSeconds = elapsed;
        if (!DayResolver.PlayerSurvivedLastDay) currentDayAttempt.deathAtSecond = elapsed;
        currentDayAttempt.reachedNight = DayTimer.Instance.IsNight;
        currentDayAttempt.creaturesSlain = DayStatsTracker.CreaturesSlain;
        currentDayAttempt.bossesDefeated = DayStatsTracker.BossesDefeated;
        currentDayAttempt.goldFromMonsters = DayStatsTracker.GoldFromMonsters;
        currentDayAttempt.goldFromBosses = DayStatsTracker.GoldFromBosses;
        data.dayAttempts.Add(currentDayAttempt);
        currentDayAttempt = null;

        currentShopVisit = new ShopVisitRecord
        {
            goldAtOpen = MainMenuUI.CurrentRun.gold,
        };

        FlushToDisk();
    }

    private void HandleEnemyKilled(string enemyName, bool isBoss, int directHits, int dotTicks, float maxHealth, int energyReward, Vector3 position)
    {
        if (currentDayAttempt == null) return;
        currentDayAttempt.kills.Add(new KillRecord
        {
            secondOfDay = DayTimer.Instance.CurrentDayDuration - DayTimer.Instance.timeRemaining,
            enemyName = enemyName,
            isBoss = isBoss,
            directHitsTaken = directHits,
            dotTicksTaken = dotTicks,
            maxHealth = maxHealth,
            energyReward = energyReward,
            position = new SerializableVector2(position),
        });
    }

    private void HandleDamageTaken(Vector3 position, float amount, bool isPlayer, bool isDirectPlayerHit)
    {
        if (currentDayAttempt == null) return;
        float secondOfDay = DayTimer.Instance.CurrentDayDuration - DayTimer.Instance.timeRemaining;
        var record = new DamageEventRecord { secondOfDay = secondOfDay, amount = amount, isDirectHit = isDirectPlayerHit };
        if (isPlayer)
        {
            currentDayAttempt.hitsTakenByPlayer.Add(record);
            secondsSinceLastHitReceived = 0f;
        }
        else
        {
            currentDayAttempt.hitsDealtByPlayer.Add(record);
            secondsSinceLastHitDealt = 0f;
        }
    }

    // Regrava o JSON inteiro (overwrite) — chamado nas fronteiras naturais (fim de dia, fim de
    // visita a loja, retorno ao Menu Principal com tentativa aberta — tasks seguintes), nunca a
    // cada amostra. Nunca propaga excecao de I/O pro resto do jogo (Anti-goals do design).
    public void FlushToDisk()
    {
        if (!telemetryEnabled || data == null) return;

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            File.WriteAllText(filePath, JsonUtility.ToJson(data, true));
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[RunTelemetryRecorder] Falha ao escrever telemetria: {e.Message}");
        }
    }
}

[Serializable]
public class SerializableVector2
{
    public float x;
    public float y;

    public SerializableVector2() { }
    public SerializableVector2(Vector2 v) { x = v.x; y = v.y; }
    public SerializableVector2(Vector3 v) { x = v.x; y = v.y; }
}

[Serializable]
public class RunTelemetryData
{
    public string runId;
    public string hero;
    public string startedAtUtc;
    public List<ShopVisitRecord> shopVisits = new();
    public List<DayAttemptRecord> dayAttempts = new();
}

[Serializable]
public class ShopVisitRecord
{
    public int beforeDayNumber;
    public long goldAtOpen;
    public long goldAtClose;
    public List<ItemSoldRecord> itemsSold = new();
    public List<PerkPurchaseRecord> perksPurchased = new();
    public List<PerkUndoRecord> perksUndone = new();
}

[Serializable]
public class DayAttemptRecord
{
    public int dayNumber;
    public int attemptIndex;
    public string outcome; // "survived" | "died" | "abandoned"
    public string startedAtUtc;
    public float elapsedSeconds;
    public bool reachedNight;
    public int creaturesSlain;
    public int bossesDefeated;
    public long goldFromMonsters;
    public long goldFromBosses;
    public long survivalGoldBonusGranted;
    public float deathAtSecond;
    public List<KillRecord> kills = new();
    public List<DamageEventRecord> hitsTakenByPlayer = new();
    public List<DamageEventRecord> hitsDealtByPlayer = new();
    public List<ItemSoldRecord> itemsSoldMidDay = new();
    public List<SnapshotRecord> snapshots = new();
}

[Serializable]
public class KillRecord
{
    public float secondOfDay;
    public string enemyName;
    public bool isBoss;
    public int directHitsTaken;
    public int dotTicksTaken;
    public float maxHealth;
    public int energyReward;
    public SerializableVector2 position;
}

[Serializable]
public class DamageEventRecord
{
    public float secondOfDay;
    public float amount;
    public bool isDirectHit;
}

[Serializable]
public class ItemSoldRecord
{
    public string itemName;
    public long quantity;
    public long subtotalGold;
}

[Serializable]
public class PerkPurchaseRecord
{
    public float secondsSinceShopOpen;
    public string perkId;
    public string displayName;
    public int rankAfterPurchase;
    public long cost;
    public long goldBefore;
    public long goldAfter;
}

[Serializable]
public class PerkUndoRecord
{
    public string perkId;
    public string displayName;
    public int rankAfterUndo;
    public long refund;
}

[Serializable]
public class SnapshotRecord
{
    public float secondOfDay;
    public int aliveMonsters;
    public int aliveBosses;
    public float heroHpPercent;
    public SerializableVector2 heroPosition;
    public long gold;
    public float secondsSinceLastHitDealt;
    public float secondsSinceLastHitReceived;
    public bool isNight;
}
