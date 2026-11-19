using NUnit.Framework;

// Mage (D9.1): no frame 7 de cada volta do attack_continuous, o fogo termina se o tempo desde
// que ele acendeu já cobriu a duração EXTRA. Extra 0 = termina na 1ª volta (ataque base).
public class PrimaryDurationTests
{
    [Test]
    public void BaseDuration_EndsOnFirstCheck()
    {
        Assert.IsTrue(PrimaryDuration.ShouldEnd(elapsedSinceFireStart: 0.375f, extraDuration: 0f));
    }

    [Test]
    public void ExtraDuration_NotReached_KeepsLooping()
    {
        Assert.IsFalse(PrimaryDuration.ShouldEnd(elapsedSinceFireStart: 0.375f, extraDuration: 0.75f));
    }

    [Test]
    public void ExtraDuration_Reached_Ends()
    {
        Assert.IsTrue(PrimaryDuration.ShouldEnd(elapsedSinceFireStart: 1.125f, extraDuration: 0.75f));
    }
}
