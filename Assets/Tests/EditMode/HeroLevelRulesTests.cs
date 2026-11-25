using NUnit.Framework;

// Etapas 1+2 — curva de nível até 60 (spec 6.2): 1º nível em 15 XP; o 40 tem que ser bem possível
// até o minuto 25 (~3.400 XP acumulados); o 60 é só pra quem quebra o jogo (~15.000). Subir de
// nível não dá status — só abre as cartas.
public class HeroLevelRulesTests
{
    [Test] public void First_level_costs_5() => Assert.AreEqual(5, HeroLevelRules.XpToNext(1)); // pedido do usuário (2026-11-25): 15 era alto demais
    [Test] public void Level_2_costs_10() => Assert.AreEqual(10, HeroLevelRules.XpToNext(2));
    [Test] public void Level_5_costs_40() => Assert.AreEqual(40, HeroLevelRules.XpToNext(5));

    // Pedido do usuário (2026-11-25): só múltiplos de 5, e cada nível mais caro que o anterior.
    [Test]
    public void Every_cost_is_a_multiple_of_5_and_strictly_increasing()
    {
        long prev = 0;
        for (int l = 1; l < HeroLevelRules.MaxLevel; l++)
        {
            long xp = HeroLevelRules.XpToNext(l);
            Assert.AreEqual(0, xp % 5, "nível " + l);
            Assert.Greater(xp, prev, "nível " + l);
            prev = xp;
        }
    }
    [Test] public void Level_40_costs_215() => Assert.AreEqual(215, HeroLevelRules.XpToNext(40));
    [Test] public void Level_59_costs_1495() => Assert.AreEqual(1495, HeroLevelRules.XpToNext(59));
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
        Assert.AreEqual(4440, to40);
        Assert.AreEqual(16375, to60);
    }

    [Test]
    public void One_level_up()
    {
        var r = HeroLevelRules.AddXp(1, 0, 5);
        Assert.AreEqual(2, r.level);
        Assert.AreEqual(0, r.xpIntoLevel);
        Assert.AreEqual(1, r.levelsGained);
    }

    // Review Focus 1 — várias gemas de uma vez sobem vários níveis.
    [Test]
    public void Several_levels_at_once()
    {
        var r = HeroLevelRules.AddXp(1, 0, 5 + 10 + 5);
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
