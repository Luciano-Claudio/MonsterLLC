using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

// Etapas 1+2 — cartas (spec 6.3/6.4): sorteio de 3 distintas sem as que já estão no nível máximo
// nem as de outro herói; bônus de dano multiplicam entre si (×1,25 por nível), os outros somam.
public class CardRulesTests
{
    private readonly List<CardDefinition> created = new();

    private CardDefinition Card(string id, CardAbility ability, CardStat stat, float value, int maxLevel = 5, string heroId = "")
    {
        var c = ScriptableObject.CreateInstance<CardDefinition>();
        c.id = id; c.ability = ability; c.stat = stat; c.valuePerLevel = value; c.maxLevel = maxLevel; c.heroId = heroId;
        created.Add(c);
        return c;
    }

    [TearDown]
    public void Cleanup()
    {
        foreach (var c in created) Object.DestroyImmediate(c);
        created.Clear();
    }

    private List<CardDefinition> TenCards()
    {
        var list = new List<CardDefinition>();
        for (int i = 0; i < 10; i++) list.Add(Card("c" + i, CardAbility.None, CardStat.MoveSpeed, 0.08f));
        return list;
    }

    [Test]
    public void Roll_gives_three_distinct_and_never_a_maxed_card()
    {
        var pool = TenCards();
        var owned = new Dictionary<string, int> { { "c0", 5 } }; // c0 no máximo
        var rng = new System.Random(1234);
        var result = new List<CardDefinition>();
        for (int n = 0; n < 100; n++)
        {
            CardRules.Roll(pool, owned, "Barbarian", 3, rng, result);
            Assert.AreEqual(3, result.Count);
            Assert.AreEqual(3, new HashSet<CardDefinition>(result).Count);
            Assert.IsFalse(result.Exists(c => c.id == "c0"));
        }
    }

    [Test]
    public void Roll_returns_fewer_when_not_enough_cards()
    {
        var pool = new List<CardDefinition> { Card("a", CardAbility.None, CardStat.Magnet, 0.25f), Card("b", CardAbility.None, CardStat.MaxHealth, 0.15f) };
        var result = new List<CardDefinition>();
        CardRules.Roll(pool, new Dictionary<string, int>(), "Barbarian", 3, new System.Random(1), result);
        Assert.AreEqual(2, result.Count);
    }

    [Test]
    public void Roll_never_offers_another_heros_card()
    {
        var pool = TenCards();
        pool.Add(Card("mage_only", CardAbility.Primary, CardStat.Damage, 1.25f, heroId: "Mage"));
        var rng = new System.Random(7);
        var result = new List<CardDefinition>();
        for (int n = 0; n < 200; n++)
        {
            CardRules.Roll(pool, new Dictionary<string, int>(), "Barbarian", 3, rng, result);
            Assert.IsFalse(result.Exists(c => c.id == "mage_only"));
        }
    }

    [Test]
    public void Damage_multiplies_per_level()
    {
        var pool = new List<CardDefinition> { Card("dmg", CardAbility.Primary, CardStat.Damage, 1.25f) };
        var bonuses = new CardBonuses();
        bonuses.Recompute(pool, new Dictionary<string, int> { { "dmg", 2 } });
        Assert.AreEqual(1.5625f, bonuses.Get(CardAbility.Primary, CardStat.Damage), 1e-4f);
        Assert.AreEqual(1f, bonuses.Get(CardAbility.Shift, CardStat.Damage), 1e-4f);
    }

    [Test]
    public void Other_stats_add_per_level()
    {
        var pool = new List<CardDefinition> { Card("spd", CardAbility.None, CardStat.MoveSpeed, 0.08f) };
        var bonuses = new CardBonuses();
        bonuses.Recompute(pool, new Dictionary<string, int> { { "spd", 3 } });
        Assert.AreEqual(1.24f, bonuses.Get(CardAbility.None, CardStat.MoveSpeed), 1e-4f);
    }

    [Test]
    public void Cooldown_has_a_floor()
    {
        var pool = new List<CardDefinition> { Card("cd", CardAbility.Shift, CardStat.Cooldown, 0.10f, maxLevel: 20) };
        var bonuses = new CardBonuses();
        bonuses.Recompute(pool, new Dictionary<string, int> { { "cd", 10 } });
        Assert.AreEqual(0.2f, bonuses.Get(CardAbility.Shift, CardStat.Cooldown), 1e-4f);
    }
}
