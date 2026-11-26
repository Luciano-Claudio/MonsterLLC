using NUnit.Framework;

public class LargeNumberFormatterTests
{
    [Test]
    public void Format_BelowThousand_ReturnsPlainNumber()
    {
        Assert.AreEqual("850", LargeNumberFormatter.Format(850));
    }

    [Test]
    public void Format_Million_ReturnsMSuffix()
    {
        Assert.AreEqual("2.3m", LargeNumberFormatter.Format(2_300_000));
    }

    [Test]
    public void Format_Trillion_ReturnsTSuffix()
    {
        Assert.AreEqual("1t", LargeNumberFormatter.Format(1_000_000_000_000));
    }

    // Bug real (2026-11-26): herói vivo com 0,4 de vida aparecia "0" (e ainda usava Shift e curava).
    // Vida atual arredonda pra CIMA: "0" só quando morreu de verdade.
    [Test]
    public void FormatHealth_AliveNeverShowsZero()
    {
        Assert.AreEqual("1", LargeNumberFormatter.FormatHealth(0.4));
        Assert.AreEqual("1", LargeNumberFormatter.FormatHealth(0.01));
        Assert.AreEqual("43", LargeNumberFormatter.FormatHealth(42.2));
        Assert.AreEqual("0", LargeNumberFormatter.FormatHealth(0));
        Assert.AreEqual("0", LargeNumberFormatter.FormatHealth(-3));
        Assert.AreEqual("2.3m", LargeNumberFormatter.FormatHealth(2_300_000));
    }
}
