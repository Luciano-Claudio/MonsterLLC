using NUnit.Framework;

public class SlotPoolTests
{
    [Test]
    public void NewPool_IsNotFull()
    {
        var pool = new SlotPool(3);
        Assert.IsFalse(pool.IsFull);
    }

    [Test]
    public void TryReserve_BelowMax_ReturnsTrueAndIncrements()
    {
        var pool = new SlotPool(3);
        Assert.IsTrue(pool.TryReserve());
        Assert.AreEqual(1, pool.Occupied);
    }

    [Test]
    public void TryReserve_AtMax_ReturnsFalse()
    {
        var pool = new SlotPool(1);
        Assert.IsTrue(pool.TryReserve());
        Assert.IsFalse(pool.TryReserve());
        Assert.AreEqual(1, pool.Occupied);
    }

    [Test]
    public void Release_FreesUpASlotForTheNextReserve()
    {
        var pool = new SlotPool(1);
        pool.TryReserve();
        pool.Release();
        Assert.IsFalse(pool.IsFull);
        Assert.IsTrue(pool.TryReserve());
    }

    [Test]
    public void Release_AtZero_NeverGoesNegative()
    {
        var pool = new SlotPool(1);
        pool.Release();
        pool.Release();
        Assert.AreEqual(0, pool.Occupied);
    }
}
