using NUnit.Framework;

// Loja permanente — loot de boss e dado de reroll (docs/superpowers/specs/2026-11-26-loja-permanente-design.md).
public class LootDropRulesTests
{
    [Test]
    public void Stat_item_chance_goes_from_10_to_50_with_luck()
    {
        Assert.AreEqual(0.10f, LootDropRules.StatItemChance(0), 1e-5f);
        Assert.AreEqual(0.30f, LootDropRules.StatItemChance(2), 1e-5f);
        Assert.AreEqual(0.50f, LootDropRules.StatItemChance(4), 1e-5f);
    }

    [Test]
    public void Dice_chance_doubles_at_luck_4()
    {
        Assert.AreEqual(0.01f, LootDropRules.DiceChance(false, 0), 1e-6f);
        Assert.AreEqual(0.02f, LootDropRules.DiceChance(false, 4), 1e-6f);
        Assert.AreEqual(0.05f, LootDropRules.DiceChance(true, 0), 1e-6f);
        Assert.AreEqual(0.10f, LootDropRules.DiceChance(true, 4), 1e-6f);
    }

    [Test]
    public void Negative_luck_counts_as_zero()
    {
        Assert.AreEqual(0.10f, LootDropRules.StatItemChance(-3), 1e-5f);
        Assert.AreEqual(0.01f, LootDropRules.DiceChance(false, -3), 1e-6f);
    }
}
