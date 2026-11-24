using NUnit.Framework;

// Etapas 1+2 — curva de nível até 60 (spec 6.2): 1º nível em 15 XP; o 40 tem que ser bem possível
// até o minuto 25 (~3.400 XP acumulados); o 60 é só pra quem quebra o jogo (~15.000). Subir de
// nível não dá status — só abre as cartas.
public class HeroLevelRulesTests
{
    [Test] public void First_level_costs_15() => Assert.AreEqual(15, HeroLevelRules.XpToNext(1));
    [Test] public void Level_40_costs_137() => Assert.AreEqual(137, HeroLevelRules.XpToNext(40));
    [Test] public void Level_59_costs_1492() => Assert.AreEqual(1492, HeroLevelRules.XpToNext(59));
    [Test] public void Level_60_is_max() => Assert.AreEqual(long.MaxValue, HeroLevelRules.XpToNext(60));

    [Test]
    public void Cumulative_xp_to_40_and_to_60()
    {
        long to40 = 0, to60 = 0;
        for (int l = 1; l <= 59; l++)
        {
            if (l <= 39) to40 += HeroLevelRules.XpToNext(l);
            to60 += HeroLevelRules.XpToNext(l);
        }
        Assert.That(to40, Is.InRange(3200, 3500));
        Assert.That(to60, Is.InRange(14500, 15500));
    }

    [Test]
    public void One_level_up()
    {
        var r = HeroLevelRules.AddXp(1, 0, 15);
        Assert.AreEqual(2, r.level);
        Assert.AreEqual(0, r.xpIntoLevel);
        Assert.AreEqual(1, r.levelsGained);
    }

    // Review Focus 1 — várias gemas de uma vez sobem vários níveis.
    [Test]
    public void Several_levels_at_once()
    {
        var r = HeroLevelRules.AddXp(1, 0, 15 + 23 + 5);
        Assert.AreEqual(3, r.level);
        Assert.AreEqual(5, r.xpIntoLevel);
        Assert.AreEqual(2, r.levelsGained);
    }

    [Test]
    public void Max_level_discards_xp()
    {
        var r = HeroLevelRules.AddXp(60, 0, 999);
        Assert.AreEqual(60, r.level);
        Assert.AreEqual(0, r.levelsGained);
    }

    [Test]
    public void Reaching_max_mid_gain_stops_there()
    {
        var r = HeroLevelRules.AddXp(59, 0, 100000);
        Assert.AreEqual(60, r.level);
        Assert.AreEqual(1, r.levelsGained);
    }
}
