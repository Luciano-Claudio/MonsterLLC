using NUnit.Framework;

public class AttackCooldownTests
{
    [Test]
    public void NewCooldown_IsReadyImmediately()
    {
        var cooldown = new AttackCooldown(1f);
        Assert.IsTrue(cooldown.IsReady);
    }

    [Test]
    public void TryConsume_WhenReady_ReturnsTrueAndStartsCooldown()
    {
        var cooldown = new AttackCooldown(1f);
        Assert.IsTrue(cooldown.TryConsume());
        Assert.IsFalse(cooldown.IsReady);
    }

    [Test]
    public void TryConsume_WhileOnCooldown_ReturnsFalse()
    {
        var cooldown = new AttackCooldown(1f);
        cooldown.TryConsume();
        Assert.IsFalse(cooldown.TryConsume());
    }

    [Test]
    public void Tick_AfterFullDuration_BecomesReadyAgain()
    {
        var cooldown = new AttackCooldown(1f);
        cooldown.TryConsume();
        cooldown.Tick(0.6f);
        Assert.IsFalse(cooldown.IsReady);
        cooldown.Tick(0.4f);
        Assert.IsTrue(cooldown.IsReady);
    }
}
