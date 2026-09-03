public class AttackBudgetTracker
{
    public int MeleeBudget { get; }
    public int RangedBudget { get; }
    public int MeleeInUse { get; private set; }
    public int RangedInUse { get; private set; }

    public AttackBudgetTracker(int meleeBudget, int rangedBudget)
    {
        MeleeBudget = meleeBudget;
        RangedBudget = rangedBudget;
    }

    public bool TryReserve(AttackType type)
    {
        if (type == AttackType.Melee)
        {
            if (MeleeInUse >= MeleeBudget) return false;
            MeleeInUse++;
            return true;
        }

        if (RangedInUse >= RangedBudget) return false;
        RangedInUse++;
        return true;
    }

    public void Release(AttackType type)
    {
        if (type == AttackType.Melee) MeleeInUse = System.Math.Max(0, MeleeInUse - 1);
        else RangedInUse = System.Math.Max(0, RangedInUse - 1);
    }
}
