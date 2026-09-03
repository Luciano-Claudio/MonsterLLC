using NUnit.Framework;

public class DemandCalculatorTests
{
    [Test]
    public void GetDemand_Day1_Returns40() => Assert.AreEqual(40, DemandCalculator.GetDemand(1));

    [Test]
    public void GetDemand_Day2_Returns80() => Assert.AreEqual(80, DemandCalculator.GetDemand(2));

    [Test]
    public void GetDemand_Day5_Returns640() => Assert.AreEqual(640, DemandCalculator.GetDemand(5));
}
