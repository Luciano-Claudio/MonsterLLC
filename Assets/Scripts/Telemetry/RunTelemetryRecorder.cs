using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    private RunState lastSeenRun;

    // Revisão final (achado Crítico #1 do reviewer) — só vira true depois que Init() passa da
    // guarda de singleton de verdade. Só pra teste manual — nenhum código de produção lê isto.
    public bool DebugIsSubscribed { get; private set; }
    public ShopVisitRecord DebugLastClosedShopVisit => data != null && data.shopVisits.Count > 0 ? data.shopVisits[^1] : null;
    public DayAttemptRecord DebugLastClosedDayAttempt => data != null && data.dayAttempts.Count > 0 ? data.dayAttempts[^1] : null;
    public string DebugRunId => data?.runId;
    public string DebugHero => data?.hero;
    public void DebugSimulateApplicationQuit() => HandleApplicationQuit();
    public void DebugSimulateGameStateChanged(GameState state) => HandleGameStateChanged(state);

    // Fronteiras em aberto — não-nulo entre GameEvents.OnDayStart e o OnDayEnd/abandono
    // seguinte (currentDayAttempt) ou entre OnDayEnd e o próximo OnDayStart (currentShopVisit).
    // Consumidos pelas Tasks 6/7 (sampler e registro de vendas/perks).
    private DayAttemptRecord currentDayAttempt;
    private ShopVisitRecord currentShopVisit;
    private readonly Dictionary<int, int> attemptsPerDay = new();
    private float shopOpenRealtime;

    // Só pra teste manual (Test 7) — nenhum código de produção lê isto.
    public bool DebugLastSaleWentToDayAttempt { get; private set; }

    // Acumuladores de ociosidade — incrementados no Update() do sampler (Task 6), resetados
    // aqui a cada início de dia e em HandleDamageTaken conforme a direção do hit.
    private float secondsSinceLastHitDealt;
    private float secondsSinceLastHitReceived;

    // Só pra teste manual (Test 5) — nenhum código de produção lê isto.
    public int DebugCurrentAttemptIndex => currentDayAttempt?.attemptIndex ?? -1;

    private float sampleTimer;

    // Só pra teste manual (Test 6) — nenhum código de produção lê isto.
    public int DebugSnapshotCount => currentDayAttempt?.snapshots.Count ?? 0;
    public SnapshotRecord DebugLastSnapshot => currentDayAttempt != null && currentDayAttempt.snapshots.Count > 0 ? currentDayAttempt.snapshots[^1] : null;
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
        float runSecond = DayTimer.Instance != null ? DayTimer.Instance.RunElapsed : 0f;
        currentDayAttempt.snapshots.Add(new SnapshotRecord
        {
            secondOfDay = runSecond,
            runSecond = runSecond, // etapas 1+2 — segundo da run (mesmo valor; secondOfDay mantido pros relatórios antigos)
            maxBossEnrageSeconds = WorldBossSpawnManager.Instance != null ? WorldBossSpawnManager.Instance.MaxBossAliveSeconds : 0f,
            gemsOnGround = XpGemPool.Instance != null ? XpGemPool.Instance.ActiveCount : 0,
            aliveMonsters = WorldPopulationManager.Instance != null ? WorldPopulationManager.Instance.AliveCount : 0,
            aliveBosses = WorldBossSpawnManager.Instance != null ? WorldBossSpawnManager.Instance.AliveCount : 0,
            heroHpPercent = hero != null ? hero.stats.health / hero.EffectiveMaxHealth * 100f : 0f,
            heroPosition = hero != null ? new SerializableVector2(hero.transform.position) : new SerializableVector2(),
            secondsSinceLastHitDealt = secondsSinceLastHitDealt,
            secondsSinceLastHitReceived = secondsSinceLastHitReceived,
            isNight = DayTimer.Instance != null && DayTimer.Instance.IsNight,
            isAttackWalking = hero != null && hero.IsAttackWalking,
            heroLevel = MainMenuUI.CurrentRun?.level ?? 1,
            heroXpIntoLevel = MainMenuUI.CurrentRun?.xpIntoLevel ?? 0,
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
    //
    // Achado Crítico da revisão final: Destroy() (Awake() acima) só executa de verdade no FIM
    // do frame — SystemsBootstrap chama Init() na cópia já condenada antes disso acontecer. Sem
    // este guard, a cópia assinava os mesmos eventos estáticos que o original, criando um 2º
    // arquivo incompleto (sem sampler, sem perks/vendas que só chegam via Instance) toda vez
    // que "Voltar ao Menu Principal" recarrega a cena Main_Menu (que tem seu próprio
    // SystemsBootstrap). Com o guard, Init() na cópia não faz nada — Instance continua apontando
    // só pro original.
    public void Init(bool enabled)
    {
        if (Instance != this) return;

        telemetryEnabled = enabled;
        if (!telemetryEnabled) return; // custo zero — nenhum dado criado, nenhum evento assinado

        StartNewRunData();
        DebugIsSubscribed = true;

        GameEvents.OnDayStart += HandleDayStart;
        GameEvents.OnRunEnded += HandleRunEnded;
        GameEvents.OnCardGranted += HandleCardGranted;
        GameEvents.OnEnemyKilledDetailed += HandleEnemyKilled;
        GameEvents.OnDamageTaken += HandleDamageTaken;
        GameEvents.OnGameStateChanged += HandleGameStateChanged;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    // O original sobrevive indefinidamente (DontDestroyOnLoad via "Systems") — só desassina de
    // verdade se ESTE for o singleton vivo (uma cópia condenada por Init()==no-op nunca assinou
    // nada, então nunca precisa desassinar). Protege também contra "Enter Play Mode sem Reload
    // Domain" (citado em GameplayBootstrap), onde Awake() não roda de novo do zero.
    private void OnDestroy()
    {
        if (Instance != this) return;
        if (telemetryEnabled)
        {
            GameEvents.OnDayStart -= HandleDayStart;
            GameEvents.OnRunEnded -= HandleRunEnded;
            GameEvents.OnCardGranted -= HandleCardGranted;
            GameEvents.OnEnemyKilledDetailed -= HandleEnemyKilled;
            GameEvents.OnDamageTaken -= HandleDamageTaken;
            GameEvents.OnGameStateChanged -= HandleGameStateChanged;
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }
        Instance = null;
    }

    // Extraído pra ser reaproveitado quando uma run NOVA é detectada em pleno voo (achado
    // Importante #6) — nunca mistura attemptIndex/shopVisits de 2 runs diferentes (New
    // Game/Continue depois de "Voltar ao Menu Principal") no mesmo arquivo.
    private void StartNewRunData()
    {
        string runId = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        data = new RunTelemetryData
        {
            runId = runId,
            hero = MainMenuUI.CurrentRun?.hero,
            startedAtUtc = DateTime.UtcNow.ToString("o"),
        };
        filePath = Path.Combine(Application.persistentDataPath, "Telemetry", $"run_{runId}.json");
        attemptsPerDay.Clear();
        lastSeenRun = MainMenuUI.CurrentRun;
    }

    // Continue Game pula direto pra GameState.Shop sem passar por OnDayEnd (GameplayBootstrap.
    // Start(), case GameState.Shop reemite OnGameStateChanged) — sem isso, a 1ª visita à loja de
    // um "Continue" perdia toda compra de perk (currentShopVisit ficava null pra sempre nesse
    // caminho, já que RecordPerkPurchased só grava se currentShopVisit != null).
    private void HandleGameStateChanged(GameState state)
    {
        if (state != GameState.Shop || currentShopVisit != null) return;
        currentShopVisit = new ShopVisitRecord { goldAtOpen = MainMenuUI.CurrentRun?.gold ?? 0 };
        shopOpenRealtime = Time.realtimeSinceStartup;
    }

    // Fecha o que estiver em aberto (tentativa de dia + visita à loja) como "abandoned"/parcial
    // e grava — reaproveitado por HandleSceneLoadedByName ("Voltar ao Menu") e por
    // HandleApplicationQuit (achado Importante #4: sem isso, apertar Stop no Editor ou fechar o
    // jogo no meio de um dia ou ainda dentro da Loja perdia os dois, silenciosamente).
    private void CloseOpenRecordsAndFlush(string dayAttemptOutcome)
    {
        if (currentDayAttempt != null)
        {
            currentDayAttempt.outcome = dayAttemptOutcome;
            data.dayAttempts.Add(currentDayAttempt);
            currentDayAttempt = null;
        }
        if (currentShopVisit != null)
        {
            currentShopVisit.beforeDayNumber = MainMenuUI.CurrentRun?.day ?? 0;
            currentShopVisit.goldAtClose = MainMenuUI.CurrentRun?.gold ?? 0;
            data.shopVisits.Add(currentShopVisit);
            currentShopVisit = null;
        }
        FlushToDisk();
    }

    private void OnApplicationQuit() => HandleApplicationQuit();

    private void HandleApplicationQuit()
    {
        if (!telemetryEnabled) return;
        CloseOpenRecordsAndFlush("abandoned");
    }

    // "Voltar ao Menu Principal" (PauseMenuUI.GoToMainMenu()) só faz SceneManager.LoadScene
    // ("Main_Menu") — NUNCA passa por GameStateManager.SetState, então GameEvents.
    // OnGameStateChanged não dispara nesse caminho; sceneLoaded é o único sinal confiável.
    // Sem morte nem sobrevivência, OnDayEnd nunca dispara pra fechar a tentativa em andamento —
    // fecha aqui como "abandoned" pra não perder o dia nem deixar ele vazar pra próxima run.
    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode) => HandleSceneLoadedByName(scene.name);

    private void HandleSceneLoadedByName(string sceneName)
    {
        if (sceneName != "Main_Menu") return;
        if (currentDayAttempt == null && currentShopVisit == null) return; // nada pra fechar
        CloseOpenRecordsAndFlush("abandoned");
    }

    // Só pra teste manual (Test 8/8b) — nenhum código de produção lê/chama isto.
    public string DebugLastClosedOutcome => data.dayAttempts.Count > 0 ? data.dayAttempts[^1].outcome : null;
    public void DebugForceOpenDayAttempt(int dayNumber) => currentDayAttempt = new DayAttemptRecord { dayNumber = dayNumber, attemptIndex = 1 };
    public void DebugSimulateSceneLoaded(string sceneName) => HandleSceneLoadedByName(sceneName);

    // Fecha a visita à loja em aberto (se houver — não há na 1ª vez, Dia 1) e abre a tentativa
    // de dia nova. attemptsPerDay conta por NÚMERO de dia (não globalmente) — um retry depois
    // de morrer reabre o MESMO dayNumber, então a 2ª tentativa daquele número vira attemptIndex=2,
    // enquanto o próximo número de dia (sobrevivência) começa do 1 de novo.
    private void HandleDayStart()
    {
        if (MainMenuUI.CurrentRun != lastSeenRun)
        {
            // Run nova detectada em pleno voo (achado Importante #6) — New Game/Continue depois
            // de "Voltar ao Menu Principal" sem o jogo ser relançado. Uma visita à loja órfã da
            // run anterior já devia ter sido fechada/gravada no arquivo ANTIGO por
            // HandleSceneLoadedByName (ver lá) — nunca herdada pela run nova; descartar aqui é
            // só uma rede de segurança caso essa fronteira não tenha disparado.
            currentShopVisit = null;
            StartNewRunData();
        }

        if (currentShopVisit != null)
        {
            currentShopVisit.beforeDayNumber = MainMenuUI.CurrentRun.day;
            currentShopVisit.goldAtClose = MainMenuUI.CurrentRun.gold;
            data.shopVisits.Add(currentShopVisit);
            currentShopVisit = null;
        }

        data.hero = MainMenuUI.CurrentRun.hero;

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
        FlushToDisk(); // achado Importante #4 — sem isso, a visita à loja que acabou de fechar acima só existia em memória
    }

    // Etapas 1+2 — fecha a tentativa (= a run inteira de 25 min): vitória no Divine God ou morte.
    // Não existe mais Loja nem gold de run.
    private void HandleRunEnded(bool victory)
    {
        if (currentDayAttempt == null) return; // segurança — não deveria disparar sem HandleDayStart antes

        float elapsed = DayTimer.Instance != null ? DayTimer.Instance.RunElapsed : 0f;
        currentDayAttempt.outcome = victory ? "victory" : "died";
        currentDayAttempt.elapsedSeconds = elapsed;
        if (!victory) currentDayAttempt.deathAtSecond = elapsed;
        currentDayAttempt.reachedNight = DayTimer.Instance != null && DayTimer.Instance.IsNight;
        currentDayAttempt.creaturesSlain = DayStatsTracker.CreaturesSlain;
        currentDayAttempt.bossesDefeated = WorldBossSpawnManager.Instance != null ? WorldBossSpawnManager.Instance.BossesKilled : DayStatsTracker.BossesDefeated;
        currentDayAttempt.heroLevel = MainMenuUI.CurrentRun?.level ?? 1;
        data.dayAttempts.Add(currentDayAttempt);
        currentDayAttempt = null;
        FlushToDisk();
    }

    private void HandleCardGranted(string cardId, int levelAfter)
    {
        if (!telemetryEnabled || currentDayAttempt == null) return;
        currentDayAttempt.cardPicks.Add(new CardPickRecord
        {
            runSecond = DayTimer.Instance != null ? DayTimer.Instance.RunElapsed : 0f,
            cardId = cardId,
            levelAfter = levelAfter,
            heroLevel = MainMenuUI.CurrentRun?.level ?? 1,
        });
    }

    // Chamado por PerkState.TryBuy() — só grava se houver uma visita à loja em aberto (perks só
    // se compram na Loja, nunca em pleno dia).
    public void RecordPerkPurchased(PerkDefinitionSO perk, int rankAfter, long cost, long goldBefore, long goldAfter)
    {
        if (currentShopVisit == null) return;
        currentShopVisit.perksPurchased.Add(new PerkPurchaseRecord
        {
            perkId = perk.perkId,
            displayName = perk.displayName,
            rankAfterPurchase = rankAfter,
            cost = cost,
            goldBefore = goldBefore,
            goldAfter = goldAfter,
            secondsSinceShopOpen = Time.realtimeSinceStartup - shopOpenRealtime,
        });
    }

    // Chamado por PerkState.UndoLastPurchase() — mesma guarda de currentShopVisit acima.
    public void RecordPerkUndone(PerkDefinitionSO perk, int rankAfter, long refund)
    {
        if (currentShopVisit == null) return;
        currentShopVisit.perksUndone.Add(new PerkUndoRecord
        {
            perkId = perk.perkId,
            displayName = perk.displayName,
            rankAfterUndo = rankAfter,
            refund = refund,
        });
    }

    // Chamado por DayStatsTracker.RegisterItemSold() — cobre os 2 call sites existentes
    // (Vendor.cs, venda manual na Loja; LootDrop.cs, auto-venda do Ímã Vivo em pleno dia).
    // Roteia pelo que estiver aberto agora: visita à loja (venda manual) ou tentativa de dia
    // (auto-venda mid-day) — nunca os 2 ao mesmo tempo (ver HandleDayStart/HandleDayEnd).
    public void RecordItemSold(string itemName, long quantity, long subtotal)
    {
        var record = new ItemSoldRecord { itemName = itemName, quantity = quantity, subtotalGold = subtotal };
        if (currentShopVisit != null)
        {
            currentShopVisit.itemsSold.Add(record);
            DebugLastSaleWentToDayAttempt = false;
        }
        else if (currentDayAttempt != null)
        {
            currentDayAttempt.itemsSoldMidDay.Add(record);
            DebugLastSaleWentToDayAttempt = true;
        }
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
    public float deathAtSecond;
    public int heroLevel; // etapas 1+2 — nível no fim da run
    public List<CardPickRecord> cardPicks = new(); // etapas 1+2 — cartas escolhidas
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
    public bool isAttackWalking; // D9.1 — andando durante o ataque primário naquele segundo
    public int heroLevel;         // D9.6b — nível naquele segundo (nível por minuto, calibragem da curva)
    public long heroXpIntoLevel;  // D9.6b — XP dentro do nível
    public float runSecond;          // etapas 1+2 — segundo da run
    public float maxBossEnrageSeconds; // etapas 1+2 — maior tempo vivo entre os bosses vivos (enrage)
    public int gemsOnGround;         // etapas 1+2 — gemas de XP no chão
}

[Serializable]
public class CardPickRecord
{
    public float runSecond;
    public string cardId;
    public int levelAfter;
    public int heroLevel;
}
