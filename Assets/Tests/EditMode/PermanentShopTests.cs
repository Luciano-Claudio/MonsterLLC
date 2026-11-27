using System.IO;
using NUnit.Framework;
using UnityEngine;

// Loja permanente (docs/superpowers/specs/2026-11-26-loja-permanente-design.md): comprar 1 nível por vez,
// vender tudo devolve 100% (inclusive as desligadas), níveis por herói, save antigo/corrompido carrega.
public class PermanentShopTests
{
    private string path;
    private PermanentUpgradeDefinition a, b, single;

    [SetUp]
    public void SetUp()
    {
        path = Path.Combine(Application.temporaryCachePath, "profile_shop_test.json");
        if (File.Exists(path)) File.Delete(path);
        PermanentGold.UseFileForTests(path);
        a = Def("perm_test_a", 5, new[] { 50, 100, 300, 500, 1000 });
        b = Def("perm_test_b", 3, new[] { 100, 200, 500, 1000, 2000 });
        single = Def("perm_test_single", 1, new[] { 10000 });
    }

    [TearDown]
    public void TearDown()
    {
        PermanentGold.UseFileForTests(null);
        if (File.Exists(path)) File.Delete(path);
        Object.DestroyImmediate(a); Object.DestroyImmediate(b); Object.DestroyImmediate(single);
    }

    private static PermanentUpgradeDefinition Def(string id, int max, int[] prices)
    {
        var d = ScriptableObject.CreateInstance<PermanentUpgradeDefinition>();
        d.id = id; d.maxLevel = max; d.prices = prices;
        return d;
    }

    [Test]
    public void Buy_spends_price_and_raises_level()
    {
        PermanentGold.Add(400);
        Assert.IsTrue(PermanentShop.Buy("Ranger", a));
        Assert.AreEqual(1, PermanentShop.Level("Ranger", a.id));
        Assert.AreEqual(350, PermanentGold.Amount);
        Assert.IsTrue(PermanentShop.Buy("Ranger", a));
        Assert.AreEqual(250, PermanentGold.Amount);
        Assert.AreEqual(300, PermanentShop.NextPrice(a, PermanentShop.Level("Ranger", a.id)));
    }

    [Test]
    public void Cannot_buy_without_gold_or_past_max()
    {
        PermanentGold.Add(40);
        Assert.IsFalse(PermanentShop.Buy("Ranger", a));
        Assert.AreEqual(40, PermanentGold.Amount);
        Assert.AreEqual(0, PermanentShop.Level("Ranger", a.id));

        PermanentGold.Add(20000);
        Assert.IsTrue(PermanentShop.Buy("Ranger", single));
        Assert.IsFalse(PermanentShop.Buy("Ranger", single));
        Assert.AreEqual(-1, PermanentShop.NextPrice(single, 1));
        Assert.AreEqual(1, PermanentShop.Level("Ranger", single.id));
    }

    [Test]
    public void RefundAll_returns_everything_even_if_disabled()
    {
        PermanentGold.Add(250);
        PermanentShop.Buy("Mage", a);
        PermanentShop.Buy("Mage", a);
        PermanentShop.Buy("Mage", b);
        Assert.AreEqual(0, PermanentGold.Amount);
        PermanentShop.SetActive("Mage", b.id, false);
        Assert.AreEqual(0, PermanentShop.EffectiveLevel("Mage", b.id));
        Assert.AreEqual(2, PermanentShop.EffectiveLevel("Mage", a.id));

        long refunded = PermanentShop.RefundAll("Mage", new[] { a, b });
        Assert.AreEqual(250, refunded);
        Assert.AreEqual(250, PermanentGold.Amount);
        Assert.AreEqual(0, PermanentShop.Level("Mage", a.id));
        Assert.AreEqual(0, PermanentShop.Level("Mage", b.id));
        Assert.IsTrue(PermanentShop.IsActive("Mage", b.id));
    }

    [Test]
    public void Levels_are_per_hero()
    {
        PermanentGold.Add(1000);
        PermanentShop.Buy("Ranger", a);
        Assert.AreEqual(1, PermanentShop.Level("Ranger", a.id));
        Assert.AreEqual(0, PermanentShop.Level("Mage", a.id));
    }

    [Test]
    public void Levels_survive_a_reload()
    {
        PermanentGold.Add(1000);
        PermanentShop.Buy("Barbarian", a);
        PermanentShop.SetActive("Barbarian", a.id, false);
        PermanentGold.UseFileForTests(path); // relê do disco
        Assert.AreEqual(1, PermanentShop.Level("Barbarian", a.id));
        Assert.IsFalse(PermanentShop.IsActive("Barbarian", a.id));
        Assert.AreEqual(950, PermanentGold.Amount);
    }

    [Test]
    public void Old_profile_with_only_gold_loads()
    {
        File.WriteAllText(path, "{\"gold\": 7}");
        PermanentGold.UseFileForTests(path);
        Assert.AreEqual(7, PermanentGold.Amount);
        Assert.AreEqual(0, PermanentShop.Level("Ranger", a.id));
    }

    [Test]
    public void Corrupt_profile_loads_empty()
    {
        File.WriteAllText(path, "{lixo");
        PermanentGold.UseFileForTests(path);
        Assert.AreEqual(0, PermanentGold.Amount);
        Assert.AreEqual(0, PermanentShop.Level("Ranger", a.id));
    }

    [Test]
    public void Total_spent_sums_the_bought_levels()
    {
        Assert.AreEqual(0, PermanentShop.TotalSpent(a, 0));
        Assert.AreEqual(450, PermanentShop.TotalSpent(a, 3));
        Assert.AreEqual(1950, PermanentShop.TotalSpent(a, 5));
    }
}
