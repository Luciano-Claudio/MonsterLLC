using UnityEngine;

public class EnemyKillSimulator : MonoBehaviour
{
    public int energyPerKill = 20; // placeholder — valor real varia por monstro (GDD Seção 11, 🔢)
    public bool simulateBoss;

    [ContextMenu("Simulate Enemy Killed")]
    public void SimulateKill() => GameEvents.EnemyKilled(energyPerKill, simulateBoss);
}
