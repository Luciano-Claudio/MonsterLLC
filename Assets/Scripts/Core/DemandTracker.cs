using UnityEngine;

public class DemandTracker : MonoBehaviour
{
    public static DemandTracker Instance { get; private set; }
    // long (não int) — "40 x 2^(Dia-1)" (DemandCalculator) já passa de 2,1 bilhões por volta
    // do Dia 28, bem antes do fim dos 30 dias.
    public long Sold { get; private set; }
    public long Target { get; private set; }

    private void Awake() => Instance = this;

    public void StartDay(int day)
    {
        Sold = 0;
        Target = DemandCalculator.GetDemand(day);
        GameEvents.DemandChanged(Sold, Target);
    }

    public void RegisterSale(string itemName, long amount)
    {
        if (itemName != "Monster Essence") return;
        Sold += amount;
        GameEvents.DemandChanged(Sold, Target);
    }

    public bool IsMet() => Sold >= Target;
}
