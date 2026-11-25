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

    // Catálogo de cartas — Lasting Flames: +10% do ciclo por nível (1 a 9); nível 10 = infinita enquanto segura.
    [Test]
    public void Extra_seconds_by_level()
    {
        Assert.AreEqual(0f, PrimaryDuration.ExtraSeconds(0, 1f), 1e-5f);
        Assert.AreEqual(0.5f, PrimaryDuration.ExtraSeconds(5, 1f), 1e-5f);
        Assert.AreEqual(0.9f * 0.75f, PrimaryDuration.ExtraSeconds(9, 0.75f), 1e-5f);
        Assert.IsTrue(float.IsPositiveInfinity(PrimaryDuration.ExtraSeconds(10, 1f)));
    }

    [Test]
    public void Level_10_is_infinite_while_held()
    {
        Assert.IsFalse(PrimaryDuration.ShouldEnd(1000f, float.PositiveInfinity, attackHeld: true));
    }

    [Test]
    public void Infinite_ends_on_release()
    {
        Assert.IsTrue(PrimaryDuration.ShouldEnd(0.1f, float.PositiveInfinity, attackHeld: false));
    }

    [Test]
    public void Finite_ignores_the_button()
    {
        Assert.IsTrue(PrimaryDuration.ShouldEnd(1f, 0.5f, attackHeld: true));
        Assert.IsFalse(PrimaryDuration.ShouldEnd(0.2f, 0.5f, attackHeld: true));
    }
}
