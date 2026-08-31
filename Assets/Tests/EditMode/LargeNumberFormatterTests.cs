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
}
