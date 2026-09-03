using NUnit.Framework;

public class SaveManagerTests
{
    [Test]
    public void SaveAndLoad_RoundTripsCorrectly()
    {
        var original = new RunState { day = 7, gold = 350, weaponTier = "Copper", hero = "Barbarian" };
        SaveManager.Save(original);

        var loaded = SaveManager.Load();

        Assert.AreEqual(original.day, loaded.day);
        Assert.AreEqual(original.gold, loaded.gold);
        Assert.AreEqual(original.weaponTier, loaded.weaponTier);
        Assert.AreEqual(original.hero, loaded.hero);
    }
}
