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
        if (!telemetryEnabled) return; // custo zero — nenhum dado criado, nenhum evento assinado (tasks seguintes)

        string runId = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        data = new RunTelemetryData
        {
            runId = runId,
            hero = MainMenuUI.CurrentRun?.hero,
            startedAtUtc = DateTime.UtcNow.ToString("o"),
        };
        filePath = Path.Combine(Application.persistentDataPath, "Telemetry", $"run_{runId}.json");
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
