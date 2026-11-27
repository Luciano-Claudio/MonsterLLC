using System.Collections.Generic;
using NUnit.Framework;

// Loja permanente — fórmulas dos bônus da run (docs/superpowers/specs/2026-11-26-loja-permanente-design.md).
public class PermanentBonusesTests
{
    private static readonly Dictionary<PermanentStat, float> Values = new()
    {
        { PermanentStat.Damage, 0.10f }, { PermanentStat.MaxHealth, 0.10f }, { PermanentStat.Armor, 0.02f },
        { PermanentStat.Regen, 0.001f }, { PermanentStat.MoveSpeed, 0.05f }, { PermanentStat.Cooldown, 0.05f },
        { PermanentStat.UltimateCharge, 0.10f }, { PermanentStat.UltimateCost, 0.05f }, { PermanentStat.Magnet, 0.20f },
        { PermanentStat.Xp, 0.05f }, { PermanentStat.Gold, 0.10f }, { PermanentStat.Luck, 0.10f },
        { PermanentStat.Curse, 0.20f }, { PermanentStat.Revival, 1f }, { PermanentStat.Reroll, 1f },
        { PermanentStat.Size, 0.05f }, { PermanentStat.Multishot, 1f }, { PermanentStat.FlameArea, 0.10f },
    };

    [TearDown] public void TearDown() => PermanentBonuses.Reset();

    private static void Build(params (PermanentStat stat, int level)[] levels)
    {
        var d = new Dictionary<PermanentStat, int>();
        foreach (var l in levels) d[l.stat] = l.level;
        PermanentBonuses.BuildFrom(d, Values);
    }

    [Test]
    public void No_upgrades_is_neutral()
    {
        Build();
        Assert.AreEqual(1f, PermanentBonuses.DamageFactor, 1e-5f);
        Assert.AreEqual(1f, PermanentBonuses.MaxHealthFactor, 1e-5f);
        Assert.AreEqual(1f, PermanentBonuses.MoveSpeedFactor, 1e-5f);
        Assert.AreEqual(1f, PermanentBonuses.CooldownFactor, 1e-5f);
        Assert.AreEqual(0f, PermanentBonuses.ArmorAdd, 1e-5f);
        Assert.AreEqual(0, PermanentBonuses.ExtraArrows);
        Assert.AreEqual(1, PermanentBonuses.StartRerolls);
        Assert.IsFalse(PermanentBonuses.HasRevival);
    }

    [Test]
    public void Damage_5_is_plus_50_percent()
    {
        Build((PermanentStat.Damage, 5));
        Assert.AreEqual(1.5f, PermanentBonuses.DamageFactor, 1e-5f);
    }

    [Test]
    public void Curse_multiplies_hero_health_speed_xp_and_gold()
    {
        Build((PermanentStat.MaxHealth, 3), (PermanentStat.MoveSpeed, 3), (PermanentStat.Xp, 5), (PermanentStat.Gold, 5), (PermanentStat.Curse, 5));
        Assert.AreEqual(1.3f * 2f, PermanentBonuses.MaxHealthFactor, 1e-4f);
        Assert.AreEqual(1.15f * 2f, PermanentBonuses.MoveSpeedFactor, 1e-4f);
        Assert.AreEqual(1.25f * 2f, PermanentBonuses.XpFactor, 1e-4f);
        Assert.AreEqual(1.5f * 2f, PermanentBonuses.GoldFactor, 1e-4f);
        Assert.AreEqual(5, PermanentBonuses.CurseLevel);
    }

    [Test]
    public void Reductions_and_counts()
    {
        Build((PermanentStat.Cooldown, 3), (PermanentStat.UltimateCost, 3), (PermanentStat.Armor, 5), (PermanentStat.Reroll, 5),
            (PermanentStat.Multishot, 2), (PermanentStat.Revival, 1), (PermanentStat.Regen, 3), (PermanentStat.Luck, 4));
        Assert.AreEqual(0.85f, PermanentBonuses.CooldownFactor, 1e-5f);
        Assert.AreEqual(0.85f, PermanentBonuses.UltimateCostFactor, 1e-5f);
        Assert.AreEqual(0.10f, PermanentBonuses.ArmorAdd, 1e-5f);
        Assert.AreEqual(6, PermanentBonuses.StartRerolls);
        Assert.AreEqual(2, PermanentBonuses.ExtraArrows);
        Assert.IsTrue(PermanentBonuses.HasRevival);
        Assert.AreEqual(0.003f, PermanentBonuses.RegenPercentPerSecond, 1e-6f);
        Assert.AreEqual(4, PermanentBonuses.LuckLevel);
    }

    [Test]
    public void Reset_goes_back_to_neutral()
    {
        Build((PermanentStat.Damage, 5), (PermanentStat.Curse, 5));
        PermanentBonuses.Reset();
        Assert.AreEqual(1f, PermanentBonuses.DamageFactor, 1e-5f);
        Assert.AreEqual(0, PermanentBonuses.CurseLevel);
    }

    [Test]
    public void Gold_bonus_keeps_the_fraction_between_pickups()
    {
        Build((PermanentStat.Gold, 1)); // ×1,1
        long total = 0;
        for (int i = 0; i < 10; i++) total += PermanentBonuses.ScaleGold(1);
        Assert.AreEqual(11, total);
    }
}
