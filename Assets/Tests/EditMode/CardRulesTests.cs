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
            CardRules.Roll(pool, owned, "Barbarian", SpecTag.None, 3, rng, result);
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
        CardRules.Roll(pool, new Dictionary<string, int>(), "Barbarian", SpecTag.None, 3, new System.Random(1), result);
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
            CardRules.Roll(pool, new Dictionary<string, int>(), "Barbarian", SpecTag.None, 3, rng, result);
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

    private static readonly OfferRules Rules = new OfferRules { maxItemSlots = 6, statEveryLevels = 5, chestStatChance01 = 0.15f, levelUpUpgradeChance01 = 0.6f, maxItemsPerOffer = 2, maxStatSlots = 6 };

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
            CardRules.RollOffer(OfferSource.LevelUp, lvl, pool, new Dictionary<string, int>(), "", SpecTag.None, 3, rng, Rules, result);
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
        CardRules.RollOffer(OfferSource.LevelUp, 5, pool, owned, "", SpecTag.None, 3, new System.Random(3), Rules, result);
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
            CardRules.RollOffer(OfferSource.LevelUp, 3, pool, new Dictionary<string, int>(), "", SpecTag.None, 3, rng, Rules, result);
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
            CardRules.RollOffer(OfferSource.LevelUp, 3, pool, owned, "", SpecTag.None, 3, rng, Rules, result);
            foreach (var c in result) Assert.IsFalse(c.id == "i6" || c.id == "i7");
            CardRules.RollOffer(OfferSource.Vendor, 3, pool, owned, "", SpecTag.None, 3, rng, Rules, result);
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
        CardRules.RollOffer(OfferSource.Vendor, 3, pool, owned, "", SpecTag.None, 3, new System.Random(1), Rules, result);
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
            CardRules.RollOffer(OfferSource.Chest, 3, pool, new Dictionary<string, int>(), "", SpecTag.None, 3, rng, never, result);
            Assert.AreEqual(3, result.Count);
            foreach (var c in result) Assert.AreEqual(CardKind.Upgrade, c.kind);
            CardRules.RollOffer(OfferSource.Chest, 3, pool, new Dictionary<string, int>(), "", SpecTag.None, 3, rng, always, result);
            Assert.AreEqual(3, result.Count);
            var ids = new HashSet<string>();
            foreach (var c in result) { Assert.AreEqual(CardKind.Stat, c.kind); ids.Add(c.id); }
            Assert.AreEqual(3, ids.Count);
        }
    }

    // ---- Etapa 3, revisão final ----

    [Test]
    public void Chest_fills_with_stats_when_upgrades_run_out()
    {
        var pool = MixedPool(); var owned = new Dictionary<string, int>();
        for (int i = 0; i < 6; i++) owned["u" + i] = 5; // todos os upgrades no máximo
        var never = Rules; never.chestStatChance01 = 0f;
        var result = new List<CardDefinition>();
        CardRules.RollOffer(OfferSource.Chest, 3, pool, owned, "", SpecTag.None, 3, new System.Random(2), never, result);
        Assert.AreEqual(3, result.Count);
        foreach (var c in result) Assert.AreEqual(CardKind.Stat, c.kind);
    }

    [Test]
    public void GetOwn_ignores_general_bonus()
    {
        var pool = new List<CardDefinition> { Card("s", CardAbility.Shift, CardStat.Damage, 1.5f), Card("g", CardAbility.None, CardStat.Damage, 2f) };
        var b = new CardBonuses();
        b.Recompute(pool, new Dictionary<string, int> { { "s", 1 }, { "g", 1 } });
        Assert.AreEqual(1.5f, b.GetOwn(CardAbility.Shift, CardStat.Damage), 1e-4f);
        Assert.AreEqual(3f, b.Get(CardAbility.Shift, CardStat.Damage), 1e-4f);
    }

    // ===== Etapa 4: tags das especializações e level up 60% upgrade (máx. 2 itens) =====

    private CardDefinition Tagged(string id, CardKind kind, SpecTag tags)
    {
        var c = Kind(id, kind);
        c.requiredTags = tags;
        return c;
    }

    private List<CardDefinition> BigPool(int upgrades, int items, int stats, int statMaxLevel = 1)
    {
        var pool = new List<CardDefinition>();
        for (int i = 0; i < upgrades; i++) pool.Add(Kind("u" + i, CardKind.Upgrade));
        for (int i = 0; i < items; i++) pool.Add(Kind("i" + i, CardKind.Item));
        for (int i = 0; i < stats; i++) pool.Add(Kind("s" + i, CardKind.Stat, statMaxLevel));
        return pool;
    }

    [Test]
    public void Tagged_card_never_offered_without_tag()
    {
        var pool = BigPool(6, 8, 6);
        var quake = Tagged("quake_up", CardKind.Upgrade, SpecTag.Earthquake);
        var quakeItem = Tagged("quake_item", CardKind.Item, SpecTag.Earthquake);
        var quakeStat = Tagged("quake_stat", CardKind.Stat, SpecTag.Earthquake);
        pool.Add(quake); pool.Add(quakeItem); pool.Add(quakeStat);
        var always = Rules; always.chestStatChance01 = 1f;
        var tags = SpecTag.Dot | SpecTag.Tank | SpecTag.Size;
        var rng = new System.Random(11);
        var result = new List<CardDefinition>();
        var owned = new Dictionary<string, int>();
        for (int n = 0; n < 1000; n++)
        {
            CardRules.RollOffer(OfferSource.LevelUp, 2, pool, owned, "", tags, 3, rng, Rules, result);
            CollectionAssert.DoesNotContain(result, quake); CollectionAssert.DoesNotContain(result, quakeItem);
            CardRules.RollOffer(OfferSource.LevelUp, 5, pool, owned, "", tags, 3, rng, Rules, result);
            CollectionAssert.DoesNotContain(result, quakeStat);
            CardRules.RollOffer(OfferSource.Chest, 3, pool, owned, "", tags, 3, rng, always, result);
            CollectionAssert.DoesNotContain(result, quake); CollectionAssert.DoesNotContain(result, quakeStat);
            CardRules.RollOffer(OfferSource.Vendor, 3, pool, owned, "", tags, 3, rng, Rules, result);
            CollectionAssert.DoesNotContain(result, quakeItem);
        }
    }

    [Test]
    public void Tagged_card_offered_when_hero_has_one_of_its_tags()
    {
        var pool = BigPool(4, 4, 0);
        var ricochet = Tagged("ricochet", CardKind.Upgrade, SpecTag.Projectile | SpecTag.Hitscan);
        pool.Add(ricochet);
        var rng = new System.Random(5);
        var result = new List<CardDefinition>();
        bool seen = false;
        for (int n = 0; n < 200 && !seen; n++)
        {
            CardRules.RollOffer(OfferSource.LevelUp, 2, pool, new Dictionary<string, int>(), "", SpecTag.Hitscan, 3, rng, Rules, result);
            seen = result.Contains(ricochet);
        }
        Assert.IsTrue(seen);
    }

    [Test]
    public void Untagged_cards_show_for_heroes_without_specialization()
    {
        var pool = BigPool(4, 4, 0);
        pool.Add(Tagged("pet_up", CardKind.Upgrade, SpecTag.Pet));
        var result = new List<CardDefinition>();
        var rng = new System.Random(8);
        for (int n = 0; n < 300; n++)
        {
            CardRules.RollOffer(OfferSource.LevelUp, 2, pool, new Dictionary<string, int>(), "", SpecTag.None, 3, rng, Rules, result);
            Assert.AreEqual(3, result.Count);
            foreach (var c in result) Assert.AreEqual(SpecTag.None, c.requiredTags);
        }
    }

    [Test]
    public void LevelUp_never_more_than_two_items()
    {
        var pool = BigPool(10, 10, 0);
        var rng = new System.Random(21);
        var result = new List<CardDefinition>();
        for (int n = 0; n < 1000; n++)
        {
            CardRules.RollOffer(OfferSource.LevelUp, 2, pool, new Dictionary<string, int>(), "", SpecTag.None, 3, rng, Rules, result);
            Assert.AreEqual(3, result.Count);
            Assert.AreEqual(3, new HashSet<CardDefinition>(result).Count);
            int items = 0;
            foreach (var c in result) if (c.kind == CardKind.Item) items++;
            Assert.LessOrEqual(items, 2);
        }
    }

    [Test]
    public void LevelUp_upgrade_share_is_about_sixty_percent()
    {
        var pool = BigPool(10, 10, 0);
        var rng = new System.Random(33);
        var result = new List<CardDefinition>();
        int upgrades = 0, total = 0;
        for (int n = 0; n < 3000; n++)
        {
            CardRules.RollOffer(OfferSource.LevelUp, 2, pool, new Dictionary<string, int>(), "", SpecTag.None, 3, rng, Rules, result);
            foreach (var c in result) { total++; if (c.kind == CardKind.Upgrade) upgrades++; }
        }
        float share = upgrades / (float)total;
        Assert.That(share, Is.InRange(0.58f, 0.70f), "fração de upgrades = " + share);
    }

    [Test]
    public void LevelUp_can_give_three_upgrades()
    {
        var pool = BigPool(10, 10, 0);
        var rng = new System.Random(44);
        var result = new List<CardDefinition>();
        bool three = false;
        for (int n = 0; n < 1000 && !three; n++)
        {
            CardRules.RollOffer(OfferSource.LevelUp, 2, pool, new Dictionary<string, int>(), "", SpecTag.None, 3, rng, Rules, result);
            int u = 0;
            foreach (var c in result) if (c.kind == CardKind.Upgrade) u++;
            three = u == 3;
        }
        Assert.IsTrue(three);
    }

    [Test]
    public void LevelUp_without_upgrades_falls_back_to_items_then_stats()
    {
        var pool = BigPool(0, 1, 5);
        var rng = new System.Random(55);
        var result = new List<CardDefinition>();
        for (int n = 0; n < 200; n++)
        {
            CardRules.RollOffer(OfferSource.LevelUp, 2, pool, new Dictionary<string, int>(), "", SpecTag.None, 3, rng, Rules, result);
            Assert.AreEqual(3, result.Count);
            int items = 0, stats = 0;
            foreach (var c in result) { if (c.kind == CardKind.Item) items++; if (c.kind == CardKind.Stat) stats++; }
            Assert.AreEqual(1, items);
            Assert.AreEqual(2, stats);
        }
    }

    [Test]
    public void No_stats_after_six_owned()
    {
        var pool = BigPool(6, 0, 8);
        var owned = new Dictionary<string, int>();
        for (int i = 0; i < 6; i++) owned["s" + i] = 1;
        var always = Rules; always.chestStatChance01 = 1f;
        var rng = new System.Random(66);
        var result = new List<CardDefinition>();
        for (int n = 0; n < 300; n++)
        {
            CardRules.RollOffer(OfferSource.LevelUp, 5, pool, owned, "", SpecTag.None, 3, rng, Rules, result);
            Assert.AreEqual(3, result.Count);
            foreach (var c in result) Assert.AreNotEqual(CardKind.Stat, c.kind);
            CardRules.RollOffer(OfferSource.Chest, 3, pool, owned, "", SpecTag.None, 3, rng, always, result);
            foreach (var c in result) Assert.AreNotEqual(CardKind.Stat, c.kind);
        }
        owned.Remove("s5"); // 5 possuídos: volta a ter Stat no nível 5
        CardRules.RollOffer(OfferSource.LevelUp, 5, pool, owned, "", SpecTag.None, 3, rng, Rules, result);
        Assert.Greater(result.Count, 0);
        foreach (var c in result) Assert.AreEqual(CardKind.Stat, c.kind);
    }

    // ===== Etapa 4 (revisão do balanceamento): upgrades gerais de dano e vida multiplicam =====

    [Test]
    public void General_health_upgrade_multiplies_item_and_stat_health()
    {
        var item = Kind("it", CardKind.Item); item.stat = CardStat.MoveSpeed; item.healthPerLevel = 0.10f;
        var vit = Kind("vit", CardKind.Stat, 1); vit.stat = CardStat.MaxHealth; vit.valuePerLevel = 0.25f;
        var hp = Kind("hp", CardKind.Upgrade, 10); hp.stat = CardStat.MaxHealth; hp.valuePerLevel = 1.2f;
        var pool = new List<CardDefinition> { item, vit, hp };
        var b = Bonuses(pool, new Dictionary<string, int> { { "it", 2 }, { "vit", 1 }, { "hp", 2 } });
        // itens e Vitality somam (1 + 0,20 + 0,25); o upgrade multiplica (×1,2²)
        Assert.AreEqual(1.45f * 1.44f, b.Get(CardAbility.None, CardStat.MaxHealth), 1e-4f);
        Assert.IsTrue(hp.Multiplicative);
        Assert.IsFalse(vit.Multiplicative);
    }

    [Test]
    public void General_damage_upgrade_multiplies_every_ability()
    {
        var dmg = Kind("dmg", CardKind.Upgrade, 10); dmg.stat = CardStat.Damage; dmg.valuePerLevel = 1.3f;
        var pool = new List<CardDefinition> { dmg };
        var b = Bonuses(pool, new Dictionary<string, int> { { "dmg", 3 } });
        float expected = 1.3f * 1.3f * 1.3f;
        Assert.AreEqual(expected, b.Get(CardAbility.Primary, CardStat.Damage), 1e-4f);
        Assert.AreEqual(expected, b.Get(CardAbility.Shift, CardStat.Damage), 1e-4f);
        Assert.AreEqual(expected, b.Get(CardAbility.Ultimate, CardStat.Damage), 1e-4f);
    }

    // ===== Pedido do usuário (2026-11-25): 1º level up com dano e vida garantidos; itens que o herói já
    // tem saem mais (level up e vendedor) =====

    [Test]
    public void First_level_up_always_offers_damage_and_health()
    {
        var pool = BigPool(6, 6, 0);
        var dmg = Kind("general_damage", CardKind.Upgrade, 10);
        var hp = Kind("general_max_health", CardKind.Upgrade, 10);
        pool.Add(dmg); pool.Add(hp);
        var rules = Rules; rules.firstLevelUpCards = new[] { dmg, hp };
        var rng = new System.Random(71);
        var result = new List<CardDefinition>();
        for (int n = 0; n < 300; n++)
        {
            CardRules.RollOffer(OfferSource.LevelUp, 2, pool, new Dictionary<string, int>(), "", SpecTag.None, 3, rng, rules, result);
            Assert.AreEqual(3, result.Count);
            CollectionAssert.Contains(result, dmg);
            CollectionAssert.Contains(result, hp);
            Assert.AreEqual(3, new HashSet<CardDefinition>(result).Count);
        }
        // nos outros níveis, nada é garantido
        int both = 0;
        for (int n = 0; n < 300; n++)
        {
            CardRules.RollOffer(OfferSource.LevelUp, 3, pool, new Dictionary<string, int>(), "", SpecTag.None, 3, rng, rules, result);
            if (result.Contains(dmg) && result.Contains(hp)) both++;
        }
        Assert.Less(both, 300);
    }

    [Test]
    public void Owned_items_are_more_likely_in_vendor_and_level_up()
    {
        var pool = BigPool(10, 10, 0);
        var owned = new Dictionary<string, int> { { "i0", 1 } };
        var rules = Rules; rules.ownedItemWeight = 3f;
        var rng = new System.Random(81);
        var result = new List<CardDefinition>();
        int vendorHits = 0, vendorTotal = 4000;
        for (int n = 0; n < vendorTotal; n++)
        {
            CardRules.RollOffer(OfferSource.Vendor, 3, pool, owned, "", SpecTag.None, 1, rng, rules, result);
            if (result[0].id == "i0") vendorHits++;
        }
        // peso 3 contra 9 itens de peso 1: 3/12 = 25% (sem peso seria 10%)
        Assert.That(vendorHits / (float)vendorTotal, Is.InRange(0.21f, 0.29f));

        rules.levelUpUpgradeChance01 = 0f; // só itens, 1 carta
        int lvlHits = 0;
        for (int n = 0; n < vendorTotal; n++)
        {
            CardRules.RollOffer(OfferSource.LevelUp, 3, pool, owned, "", SpecTag.None, 1, rng, rules, result);
            if (result[0].id == "i0") lvlHits++;
        }
        Assert.That(lvlHits / (float)vendorTotal, Is.InRange(0.21f, 0.29f));
    }

    // ===== Pedido do usuário (2026-11-25): no sorteio de Upgrade, primário + dano + vida pesam mais,
    // mais ainda no começo =====

    private OfferRules PriorityRules()
    {
        var r = Rules;
        r.levelUpUpgradeChance01 = 1f; // só upgrades
        r.priorityUpgradeWeightEarly = 2.5f; r.priorityUpgradeWeightLate = 1.5f;
        r.priorityFadeStartLevel = 5; r.priorityFadeEndLevel = 20;
        return r;
    }

    [Test]
    public void Priority_weight_fades_from_early_to_late()
    {
        var r = PriorityRules();
        Assert.AreEqual(2.5f, CardRules.PriorityUpgradeWeight(3, r), 1e-4f);
        Assert.AreEqual(2.5f, CardRules.PriorityUpgradeWeight(5, r), 1e-4f);
        Assert.AreEqual(2.0f, CardRules.PriorityUpgradeWeight(12, r), 0.05f);
        Assert.AreEqual(1.5f, CardRules.PriorityUpgradeWeight(20, r), 1e-4f);
        Assert.AreEqual(1.5f, CardRules.PriorityUpgradeWeight(45, r), 1e-4f);
    }

    [Test]
    public void Primary_damage_and_health_upgrades_are_more_likely_early()
    {
        var pool = new List<CardDefinition>();
        var primary = Kind("prim_area", CardKind.Upgrade); primary.ability = CardAbility.Primary; primary.stat = CardStat.Area;
        pool.Add(primary);
        for (int i = 0; i < 9; i++) { var u = Kind("shift" + i, CardKind.Upgrade); u.ability = CardAbility.Shift; u.stat = CardStat.Cooldown; pool.Add(u); }
        var rules = PriorityRules();
        var rng = new System.Random(91);
        var result = new List<CardDefinition>();
        int early = 0, late = 0, n = 6000;
        for (int k = 0; k < n; k++)
        {
            CardRules.RollOffer(OfferSource.LevelUp, 3, pool, new Dictionary<string, int>(), "", SpecTag.None, 1, rng, rules, result);
            if (result[0] == primary) early++;
            CardRules.RollOffer(OfferSource.LevelUp, 31, pool, new Dictionary<string, int>(), "", SpecTag.None, 1, rng, rules, result);
            if (result[0] == primary) late++;
        }
        // cedo: 2,5 / 11,5 ≈ 21,7%; tarde: 1,5 / 10,5 ≈ 14,3%; sem peso seria 10%
        Assert.That(early / (float)n, Is.InRange(0.19f, 0.245f));
        Assert.That(late / (float)n, Is.InRange(0.12f, 0.165f));
    }

    [Test]
    public void General_damage_and_health_count_as_priority_but_other_general_do_not()
    {
        var dmg = Kind("d", CardKind.Upgrade); dmg.stat = CardStat.Damage;
        var hp = Kind("h", CardKind.Upgrade); hp.stat = CardStat.MaxHealth;
        var mag = Kind("m", CardKind.Upgrade); mag.stat = CardStat.Magnet;
        var prim = Kind("p", CardKind.Upgrade); prim.ability = CardAbility.Primary; prim.stat = CardStat.ProjectileCount;
        var ult = Kind("u", CardKind.Upgrade); ult.ability = CardAbility.Ultimate; ult.stat = CardStat.Area;
        Assert.IsTrue(CardRules.IsPriorityUpgrade(dmg));
        Assert.IsTrue(CardRules.IsPriorityUpgrade(hp));
        Assert.IsTrue(CardRules.IsPriorityUpgrade(prim));
        Assert.IsFalse(CardRules.IsPriorityUpgrade(mag));
        Assert.IsFalse(CardRules.IsPriorityUpgrade(ult));
    }
}
