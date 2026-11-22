using NUnit.Framework;

// D9.6b — nível do herói (docs/superpowers/specs/2026-11-22-d9-6ab-monstros-xp-nivel-design.md, 3.2–3.4):
// +40% de dano (piso +1) e +30% de vida (piso +5) por nível; tabela de XP; cap por dia = 2·dia+1;
// XP acima do cap vira gold (1 por XP, até 500/dia).
public class HeroLevelRulesTests
{
    [Test]
    public void DamageAt_Barbarian()
    {
        Assert.AreEqual(2f, HeroLevelRules.DamageAt(2f, 1), 1e-4f);
        Assert.AreEqual(3f, HeroLevelRules.DamageAt(2f, 2), 1e-4f); // piso +1 (2×1,4 = 2,8 < 3)
        Assert.AreEqual(44f, HeroLevelRules.DamageAt(2f, 10), 1f);
        Assert.AreEqual(1280f, HeroLevelRules.DamageAt(2f, 20), 1280f * 0.02f);
    }

    [Test]
    public void HealthAt_Barbarian()
    {
        Assert.AreEqual(55f, HeroLevelRules.HealthAt(42f, 2), 1f);
        Assert.AreEqual(446f, HeroLevelRules.HealthAt(42f, 10), 446f * 0.01f);
        Assert.AreEqual(6150f, HeroLevelRules.HealthAt(42f, 20), 6150f * 0.02f);
    }

    [Test]
    public void XpToNext_Table()
    {
        Assert.AreEqual(40, HeroLevelRules.XpToNext(1));
        Assert.AreEqual(15500, HeroLevelRules.XpToNext(9));
        Assert.AreEqual(3570000, HeroLevelRules.XpToNext(19));
        Assert.AreEqual(long.MaxValue, HeroLevelRules.XpToNext(20));
    }

    [Test]
    public void LevelCap()
    {
        Assert.AreEqual(3, HeroLevelRules.LevelCapForDay(1));
        Assert.AreEqual(11, HeroLevelRules.LevelCapForDay(5));
        Assert.AreEqual(20, HeroLevelRules.LevelCapForDay(10));
    }

    [Test]
    public void AddXp_LevelsUp()
    {
        var r = HeroLevelRules.AddXp(1, 0, 50, 1, 0, 500);
        Assert.AreEqual(2, r.level);
        Assert.AreEqual(10, r.xpIntoLevel);
        Assert.AreEqual(1, r.levelsGained);
    }

    [Test]
    public void AddXp_MultipleLevelsInOneKill()
    {
        var r = HeroLevelRules.AddXp(1, 0, 1000, 5, 0, 500);
        Assert.AreEqual(4, r.level);
        Assert.AreEqual(180, r.xpIntoLevel); // 1000 − (40 + 260 + 520)
        Assert.AreEqual(3, r.levelsGained);
        Assert.AreEqual(0, r.goldGained);
    }

    [Test]
    public void AddXp_StopsAtDayCap_OverflowToGold()
    {
        var r = HeroLevelRules.AddXp(3, 0, 1000, 1, 0, 500);
        Assert.AreEqual(3, r.level);
        Assert.AreEqual(519, r.xpIntoLevel); // barra cheia: XpToNext(3) − 1
        Assert.AreEqual(481, r.goldGained);
        Assert.AreEqual(481, r.goldConvertedToday);
    }

    [Test]
    public void AddXp_AtCapAndGoldLimit_DiscardsRest()
    {
        var r = HeroLevelRules.AddXp(3, 519, 100, 1, 500, 500);
        Assert.AreEqual(3, r.level);
        Assert.AreEqual(519, r.xpIntoLevel);
        Assert.AreEqual(0, r.goldGained);
        Assert.AreEqual(500, r.goldConvertedToday);
    }

    [Test]
    public void AddXp_AtMaxLevel_NoOverflowIndex()
    {
        var r = HeroLevelRules.AddXp(20, 0, 100, 10, 0, 500);
        Assert.AreEqual(20, r.level);
        Assert.AreEqual(100, r.goldGained);
    }
}
