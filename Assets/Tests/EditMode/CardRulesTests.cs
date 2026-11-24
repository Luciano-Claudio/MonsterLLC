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

    // ---- Etapa 3, Task 1: bônus gerais, vida por nível, valor base ----

    private CardBonuses Bonuses(List<CardDefinition> pool, Dictionary<string, int> owned)
    {
        var b = new CardBonuses();
        b.Recompute(pool, owned);
        return b;
    }

    [Test]
    public void General_damage_multiplies_ability_damage()
    {
        var pool = new List<CardDefinition> { Card("p", CardAbility.Primary, CardStat.Damage, 1.25f), Card("g", CardAbility.None, CardStat.Damage, 1.10f) };
        var b = Bonuses(pool, new Dictionary<string, int> { { "p", 1 }, { "g", 1 } });
        Assert.AreEqual(1.375f, b.Get(CardAbility.Primary, CardStat.Damage), 1e-4f);
        Assert.AreEqual(1.10f, b.Get(CardAbility.None, CardStat.Damage), 1e-4f);
    }

    [Test]
    public void General_cooldown_combines_with_floor()
    {
        var pool = new List<CardDefinition> { Card("s", CardAbility.Shift, CardStat.Cooldown, 0.10f), Card("g", CardAbility.None, CardStat.Cooldown, 0.15f) };
        var b = Bonuses(pool, new Dictionary<string, int> { { "s", 2 }, { "g", 1 } });
        Assert.AreEqual(0.8f * 0.85f, b.Get(CardAbility.Shift, CardStat.Cooldown), 1e-4f);
        b = Bonuses(pool, new Dictionary<string, int> { { "s", 5 }, { "g", 3 } });
        Assert.GreaterOrEqual(b.Get(CardAbility.Shift, CardStat.Cooldown), 0.2f - 1e-5f);
    }

    [Test]
    public void Item_health_adds_to_max_health()
    {
        var item = Card("i", CardAbility.None, CardStat.MoveSpeed, 0.08f);
        item.kind = CardKind.Item; item.healthPerLevel = 0.10f;
        var b = Bonuses(new List<CardDefinition> { item }, new Dictionary<string, int> { { "i", 3 } });
        Assert.AreEqual(1.3f, b.Get(CardAbility.None, CardStat.MaxHealth), 1e-4f);
        Assert.AreEqual(1.24f, b.Get(CardAbility.None, CardStat.MoveSpeed), 1e-4f);
    }

    [Test]
    public void Base_value_counts_once()
    {
        var blast = Card("b", CardAbility.None, CardStat.BlastChance, 0.05f);
        blast.baseValue = 0.05f;
        var pool = new List<CardDefinition> { blast };
        Assert.AreEqual(0.10f, Bonuses(pool, new Dictionary<string, int> { { "b", 1 } }).Sum(CardAbility.None, CardStat.BlastChance), 1e-4f);
        Assert.AreEqual(0.30f, Bonuses(pool, new Dictionary<string, int> { { "b", 5 } }).Sum(CardAbility.None, CardStat.BlastChance), 1e-4f);
        Assert.AreEqual(0f, Bonuses(pool, new Dictionary<string, int>()).Sum(CardAbility.None, CardStat.BlastChance), 1e-4f);
    }

    // ---- Etapa 3, Task 2: oferta por fonte ----

    private static readonly OfferRules Rules = new OfferRules { maxItemSlots = 6, statEveryLevels = 5, chestStatChance01 = 0.15f };

    private CardDefinition Kind(string id, CardKind kind, int maxLevel = 5)
    {
        var c = Card(id, CardAbility.None, CardStat.MoveSpeed, 0.1f, maxLevel);
        c.kind = kind;
        return c;
    }

    // 6 upgrades, 8 itens, 6 stats.
    private List<CardDefinition> MixedPool()
    {
        var pool = new List<CardDefinition>();
        for (int i = 0; i < 6; i++) pool.Add(Kind("u" + i, CardKind.Upgrade));
        for (int i = 0; i < 8; i++) pool.Add(Kind("i" + i, CardKind.Item));
        for (int i = 0; i < 6; i++) pool.Add(Kind("s" + i, CardKind.Stat, 3));
        return pool;
    }

    [Test]
    public void Level_5_offers_only_stats()
    {
        var pool = MixedPool(); var rng = new System.Random(7); var result = new List<CardDefinition>();
        foreach (int lvl in new[] { 5, 10 })
        {
            CardRules.RollOffer(OfferSource.LevelUp, lvl, pool, new Dictionary<string, int>(), "", 3, rng, Rules, result);
            Assert.AreEqual(3, result.Count);
            foreach (var c in result) Assert.AreEqual(CardKind.Stat, c.kind);
        }
    }

    [Test]
    public void Level_5_without_stats_falls_back_to_common()
    {
        var pool = MixedPool(); var owned = new Dictionary<string, int>();
        for (int i = 0; i < 6; i++) owned["s" + i] = 3;
        var result = new List<CardDefinition>();
        CardRules.RollOffer(OfferSource.LevelUp, 5, pool, owned, "", 3, new System.Random(3), Rules, result);
        Assert.AreEqual(3, result.Count);
        foreach (var c in result) Assert.AreNotEqual(CardKind.Stat, c.kind);
    }

    [Test]
    public void Common_level_mixes_upgrades_and_items_never_stats()
    {
        var pool = MixedPool(); var rng = new System.Random(11); var result = new List<CardDefinition>();
        bool sawUpgrade = false, sawItem = false;
        for (int n = 0; n < 200; n++)
        {
            CardRules.RollOffer(OfferSource.LevelUp, 3, pool, new Dictionary<string, int>(), "", 3, rng, Rules, result);
            foreach (var c in result)
            {
                Assert.AreNotEqual(CardKind.Stat, c.kind);
                if (c.kind == CardKind.Upgrade) sawUpgrade = true;
                if (c.kind == CardKind.Item) sawItem = true;
            }
        }
        Assert.IsTrue(sawUpgrade && sawItem);
    }

    [Test]
    public void Six_items_blocks_new_items_everywhere()
    {
        var pool = MixedPool(); var owned = new Dictionary<string, int>();
        for (int i = 0; i < 6; i++) owned["i" + i] = 1; // i6 e i7 ficam de fora
        var rng = new System.Random(5); var result = new List<CardDefinition>();
        for (int n = 0; n < 200; n++)
        {
            CardRules.RollOffer(OfferSource.LevelUp, 3, pool, owned, "", 3, rng, Rules, result);
            foreach (var c in result) Assert.IsFalse(c.id == "i6" || c.id == "i7");
            CardRules.RollOffer(OfferSource.Vendor, 3, pool, owned, "", 3, rng, Rules, result);
            Assert.AreEqual(3, result.Count);
            foreach (var c in result) { Assert.AreEqual(CardKind.Item, c.kind); Assert.IsTrue(owned.ContainsKey(c.id)); }
        }
        Assert.AreEqual(6, CardRules.CountOwnedItems(pool, owned));
    }

    [Test]
    public void Vendor_with_all_items_maxed_returns_empty()
    {
        var pool = MixedPool(); var owned = new Dictionary<string, int>();
        for (int i = 0; i < 6; i++) owned["i" + i] = 5;
        var result = new List<CardDefinition>();
        CardRules.RollOffer(OfferSource.Vendor, 3, pool, owned, "", 3, new System.Random(1), Rules, result);
        Assert.AreEqual(0, result.Count);
    }

    [Test]
    public void Chest_stat_chance_zero_and_one()
    {
        var pool = MixedPool(); var rng = new System.Random(9); var result = new List<CardDefinition>();
        var never = Rules; never.chestStatChance01 = 0f;
        var always = Rules; always.chestStatChance01 = 1f;
        for (int n = 0; n < 50; n++)
        {
            CardRules.RollOffer(OfferSource.Chest, 3, pool, new Dictionary<string, int>(), "", 3, rng, never, result);
            Assert.AreEqual(3, result.Count);
            foreach (var c in result) Assert.AreEqual(CardKind.Upgrade, c.kind);
            CardRules.RollOffer(OfferSource.Chest, 3, pool, new Dictionary<string, int>(), "", 3, rng, always, result);
            Assert.AreEqual(3, result.Count);
            var ids = new HashSet<string>();
            foreach (var c in result) { Assert.AreEqual(CardKind.Stat, c.kind); ids.Add(c.id); }
            Assert.AreEqual(3, ids.Count);
        }
    }
}
