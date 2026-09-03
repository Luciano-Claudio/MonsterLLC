using UnityEngine;

public class AttackBudgetManager : MonoBehaviour
{
    public static AttackBudgetManager Instance { get; private set; }

    public int meleeBudget = 3; // 🔢 GDD Seção 14 — placeholder, balanceamento real pendente
    public int rangedBudget = 2;

    private AttackBudgetTracker tracker;

    public int MeleeInUse => tracker.MeleeInUse;
    public int RangedInUse => tracker.RangedInUse;

    private void Awake()
    {
        Instance = this;
        tracker = new AttackBudgetTracker(meleeBudget, rangedBudget);
    }

    public bool TryReserveSlot(AttackType type) => tracker.TryReserve(type);
    public void ReleaseSlot(AttackType type) => tracker.Release(type);
}
