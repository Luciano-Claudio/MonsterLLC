using UnityEngine;

// Testes manuais da telemetria de run (RunTelemetryRecorder) — mesmo padrao de SaveTest.cs
// ([ContextMenu], sem runner de teste automatizado neste projeto). Cada metodo tambem RETORNA
// a string de resultado (alem do Debug.Log) para poder ser chamado via Unity CLI script eval
// durante a implementacao, sem precisar clicar no Inspector.
public class RunTelemetryTest : MonoBehaviour
{
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
}
