using NUnit.Framework;

// D9.6a — escala dos monstros por tier (docs/superpowers/specs/2026-11-22-d9-6ab-monstros-xp-nivel-design.md, 3.1):
// melee típico pela tabela; ranged vida ×0,7 / dano ×1,4; mini boss vida ×4 / dano ×1,5 / XP ×3;
// boss XP ×10; cada monstro mantém a proporção dele na média do tier.
public class MonsterScalingTests
{
    [SetUp] public void ResetTables() => MonsterScaling.ResetTables();

    [Test]
    public void TargetHealth_ByKind()
    {
        Assert.AreEqual(34.93f, MonsterScaling.TargetHealth(5, MonsterKind.Melee, 1f), 1e-3f);
        Assert.AreEqual(34.93f * 0.7f, MonsterScaling.TargetHealth(5, MonsterKind.Ranged, 1f), 1e-3f);
        Assert.AreEqual(34.93f * 4f, MonsterScaling.TargetHealth(5, MonsterKind.MiniBoss, 1f), 1e-3f);
        Assert.AreEqual(52.395f, MonsterScaling.TargetHealth(5, MonsterKind.Melee, 1.5f), 1e-3f);
    }

    [Test]
    public void TargetDamage_T9Ranged() =>
        Assert.AreEqual(58.95f * 1.4f, MonsterScaling.TargetDamage(9, MonsterKind.Ranged, 1f), 1e-3f);

    [Test]
    public void XpForKill()
    {
        Assert.AreEqual(50, MonsterScaling.XpForKill(4, false, false));
        Assert.AreEqual(150, MonsterScaling.XpForKill(4, false, true));
        Assert.AreEqual(500, MonsterScaling.XpForKill(4, true, false));
    }

    [Test]
    public void XpForKill_UnknownTier_IsT1() => Assert.AreEqual(1, MonsterScaling.XpForKill(0, false, false));

    [Test]
    public void TierSpeed_Range()
    {
        Assert.AreEqual(2.5f, MonsterScaling.TierSpeed(1), 1e-3f);
        Assert.AreEqual(4f, MonsterScaling.TierSpeed(10), 1e-3f);
        for (int t = 2; t <= 10; t++) Assert.Greater(MonsterScaling.TierSpeed(t), MonsterScaling.TierSpeed(t - 1));
    }

    // Etapas 1+2 — 1,5x por tier a partir da média real do T1 (spec 5.4; ruling da Task 2).
    [Test]
    public void Tables_grow_1_5x_per_tier()
    {
        Assert.AreEqual(10.35f, MonsterScaling.TierHealth[2], 0.01f);
        Assert.AreEqual(265.26f, MonsterScaling.TierHealth[10], 0.01f);
        Assert.AreEqual(58.95f, MonsterScaling.TierDamage[9], 0.01f);
    }

    // Boss = 20x vida / 2x dano do melee do tier; Divine God 30x vida (spec 5.4).
    [Test]
    public void Boss_targets_are_multiples_of_tier_melee()
    {
        Assert.AreEqual(138f, MonsterScaling.TargetBossHealth(1, false, 1f), 0.01f);
        Assert.AreEqual(7957.8f, MonsterScaling.TargetBossHealth(10, true, 1f), 0.5f);
        Assert.AreEqual(10.34f, MonsterScaling.TargetBossDamage(3, 1f), 0.01f);
    }
}
