using NUnit.Framework;

public class AttackBudgetTrackerTests
{
    [Test]
    public void TryReserve_WithinBudget_Succeeds()
    {
        var tracker = new AttackBudgetTracker(meleeBudget: 2, rangedBudget: 1);
        Assert.IsTrue(tracker.TryReserve(AttackType.Melee));
        Assert.IsTrue(tracker.TryReserve(AttackType.Melee));
    }

    [Test]
    public void TryReserve_ExceedsBudget_Fails()
    {
        var tracker = new AttackBudgetTracker(meleeBudget: 1, rangedBudget: 1);
        Assert.IsTrue(tracker.TryReserve(AttackType.Melee));
        Assert.IsFalse(tracker.TryReserve(AttackType.Melee));
    }

    [Test]
    public void Release_FreesSlotForReuse()
    {
        var tracker = new AttackBudgetTracker(meleeBudget: 1, rangedBudget: 1);
        tracker.TryReserve(AttackType.Melee);
        tracker.Release(AttackType.Melee);
        Assert.IsTrue(tracker.TryReserve(AttackType.Melee));
    }

    [Test]
    public void MeleeAndRanged_AreIndependentBudgets()
    {
        var tracker = new AttackBudgetTracker(meleeBudget: 1, rangedBudget: 1);
        Assert.IsTrue(tracker.TryReserve(AttackType.Melee));
        Assert.IsTrue(tracker.TryReserve(AttackType.Ranged)); // não compete com o slot de Melee
    }
}
