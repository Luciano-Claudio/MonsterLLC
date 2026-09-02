using NUnit.Framework;

public class EnergySystemTests
{
    [Test]
    public void AddEnergy_CapsAtMax()
    {
        Assert.AreEqual(100f, EnergySystem.AddEnergy(90f, 100f, 50f));
    }

    [Test]
    public void AddEnergy_BelowMax_AddsNormally()
    {
        Assert.AreEqual(70f, EnergySystem.AddEnergy(50f, 100f, 20f));
    }

    [Test]
    public void IsReady_AtMax_ReturnsTrue()
    {
        Assert.IsTrue(EnergySystem.IsReady(100f, 100f));
    }

    [Test]
    public void IsReady_BelowMax_ReturnsFalse()
    {
        Assert.IsFalse(EnergySystem.IsReady(80f, 100f));
    }
}
