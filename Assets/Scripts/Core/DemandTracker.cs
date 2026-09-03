using UnityEngine;

public class DemandTracker : MonoBehaviour
{
    public static DemandTracker Instance { get; private set; }
    public int Sold { get; private set; }
    public int Target { get; private set; }

    private void Awake() => Instance = this;

    public void StartDay(int day)
    {
        Sold = 0;
        Target = DemandCalculator.GetDemand(day);
        GameEvents.DemandChanged(Sold, Target);
    }

    public void RegisterSale(string itemName, int amount)
    {
        if (itemName != "Monster Essence") return;
        Sold += amount;
        GameEvents.DemandChanged(Sold, Target);
    }

    public bool IsMet() => Sold >= Target;
}
