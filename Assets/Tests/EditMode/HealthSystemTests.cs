using NUnit.Framework;

public class HealthSystemTests
{
    [Test]
    public void ApplyDamage_ReducesHealth()
    {
        Assert.AreEqual(70f, HealthSystem.ApplyDamage(100f, 30f));
    }

    [Test]
    public void ApplyDamage_ClampsAtZero()
    {
        Assert.AreEqual(0f, HealthSystem.ApplyDamage(10f, 50f));
    }

    [Test]
    public void IsDead_AtZero_ReturnsTrue()
    {
        Assert.IsTrue(HealthSystem.IsDead(0f));
    }

    [Test]
    public void IsDead_AboveZero_ReturnsFalse()
    {
        Assert.IsFalse(HealthSystem.IsDead(5f));
    }
}
