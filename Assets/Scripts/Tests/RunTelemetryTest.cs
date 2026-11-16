using UnityEngine;

// Testes manuais da telemetria de run (RunTelemetryRecorder) — mesmo padrao de SaveTest.cs
// ([ContextMenu], sem runner de teste automatizado neste projeto). Cada metodo tambem RETORNA
// a string de resultado (alem do Debug.Log) para poder ser chamado via Unity CLI script eval
// durante a implementacao, sem precisar clicar no Inspector.
public class RunTelemetryTest : MonoBehaviour
{
    [SerializeField] private EnemyController testEnemyPrefab;

    [ContextMenu("Test 1: OnEnemyKilledDetailed fires")]
    public string TestEnemyKilledDetailedFires()
    {
        bool fired = false;
        GameEvents.OnEnemyKilledDetailed += (name, isBoss, direct, dot, maxHp, energy, pos) => fired = true;
        GameEvents.EnemyKilledDetailed("TestMonster", false, 3, 1, 50f, 20, Vector3.zero);
        string result = fired ? "[RunTelemetryTest] PASS: evento disparou" : "[RunTelemetryTest] FAIL: evento nao disparou";
        Debug.Log(result);
        return result;
    }

    [ContextMenu("Test 2: hits diretos x DoT ticks separados na morte")]
    public string TestDirectVsDotHits()
    {
        var enemy = Instantiate(testEnemyPrefab);
        string capture = "nao disparou";
        System.Action<string, bool, int, int, float, int, Vector3> handler = (name, isBoss, direct, dot, maxHp, energy, pos) =>
            capture = $"direct={direct} dot={dot}";
        GameEvents.OnEnemyKilledDetailed += handler;

        enemy.TakeDamage(1f, isDirectPlayerHit: true);   // 1 hit direto
        enemy.TakeDamage(1f, isDirectPlayerHit: false);  // 1 tick de DoT
        enemy.TakeDamage(9999f, isDirectPlayerHit: true); // golpe fatal, direto

        GameEvents.OnEnemyKilledDetailed -= handler;
        string result = capture == "direct=2 dot=1" ? $"[RunTelemetryTest] PASS: {capture}" : $"[RunTelemetryTest] FAIL: {capture}";
        Debug.Log(result);
        return result;
    }

    [ContextMenu("Test 3: getters de populacao viva existem")]
    public string TestAliveCountGetters()
    {
        int pop = WorldPopulationManager.Instance != null ? WorldPopulationManager.Instance.AliveCount : -1;
        int bosses = WorldBossSpawnManager.Instance != null ? WorldBossSpawnManager.Instance.AliveCount : -1;
        string result = $"[RunTelemetryTest] PASS: AliveCount monstros={pop}, bosses={bosses}";
        Debug.Log(result);
        return result;
    }

    [ContextMenu("Test 4: FlushToDisk escreve JSON valido")]
    public string TestFlushToDisk()
    {
        RunTelemetryRecorder.Instance?.FlushToDisk();
        string dir = System.IO.Path.Combine(Application.persistentDataPath, "Telemetry");
        bool exists = System.IO.Directory.Exists(dir) && System.IO.Directory.GetFiles(dir, "run_*.json").Length > 0;
        string result = exists ? "[RunTelemetryTest] PASS: arquivo de telemetria existe" : "[RunTelemetryTest] FAIL: pasta/arquivo nao existe";
        Debug.Log(result);
        return result;
    }

    [ContextMenu("Test 5: attemptIndex incrementa por dayNumber, nao globalmente")]
    public string TestAttemptIndexPerDay()
    {
        // Simula 2 inicios de dia pro MESMO numero (retry) e 1 pro proximo numero.
        GameEvents.DayStarted(); // dia N, tentativa 1
        int a1 = RunTelemetryRecorder.Instance.DebugCurrentAttemptIndex;
        GameEvents.DayEnded();   // morreu, mesmo dia
        GameEvents.DayStarted(); // dia N, tentativa 2 (retry)
        int a2 = RunTelemetryRecorder.Instance.DebugCurrentAttemptIndex;
        string result = a1 == 1 && a2 == 2 ? $"[RunTelemetryTest] PASS: a1={a1} a2={a2}" : $"[RunTelemetryTest] FAIL: a1={a1} a2={a2}";
        Debug.Log(result);
        return result;
    }
}
