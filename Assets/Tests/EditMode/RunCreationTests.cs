using NUnit.Framework;

public class RunCreationTests
{
    [Test]
    public void CreateNewRun_SetsFieldsCorrectly()
    {
        var run = RunCreation.CreateNewRun("Standard", "Barbarian", "Tower");

        Assert.AreEqual("Standard", run.mode);
        Assert.AreEqual("Barbarian", run.hero);
        Assert.AreEqual("Tower", run.map);
        Assert.AreEqual(1, run.day);
        Assert.AreEqual(0, run.gold);
    }
}
