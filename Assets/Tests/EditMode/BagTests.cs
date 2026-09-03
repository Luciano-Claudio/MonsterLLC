using NUnit.Framework;

public class BagTests
{
    [Test]
    public void AddItem_FitsCompletely_ReturnsFullAmount()
    {
        var bag = new Bag(maxSlots: 5, stackSize: 16);
        int added = bag.AddItem("Monster Essence", 10);
        Assert.AreEqual(10, added);
        Assert.AreEqual(1, bag.Slots.Count);
        Assert.AreEqual(10, bag.Slots[0].quantity);
    }

    [Test]
    public void AddItem_ExceedsStackSize_CreatesNewSlot()
    {
        var bag = new Bag(maxSlots: 5, stackSize: 16);
        bag.AddItem("Monster Essence", 16);
        int added = bag.AddItem("Monster Essence", 10);
        Assert.AreEqual(10, added);
        Assert.AreEqual(2, bag.Slots.Count);
    }

    [Test]
    public void AddItem_NoSpaceLeft_ReturnsZero()
    {
        var bag = new Bag(maxSlots: 1, stackSize: 16);
        bag.AddItem("Monster Essence", 16); // enche o único slot
        int added = bag.AddItem("Monster Essence", 10);
        Assert.AreEqual(0, added);
    }

    [Test]
    public void AddItem_PartialFit_ReturnsOnlyWhatFit()
    {
        var bag = new Bag(maxSlots: 1, stackSize: 16);
        int added = bag.AddItem("Monster Essence", 30); // só cabe 16 (1 slot × stack 16)
        Assert.AreEqual(16, added);
    }

    [Test]
    public void Clear_RemovesAllSlots()
    {
        var bag = new Bag(maxSlots: 5, stackSize: 16);
        bag.AddItem("Monster Essence", 10);
        bag.Clear();
        Assert.AreEqual(0, bag.Slots.Count);
    }
}
