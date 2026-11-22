using NUnit.Framework;

// D9.6a — escala dos monstros por tier (docs/superpowers/specs/2026-11-22-d9-6ab-monstros-xp-nivel-design.md, 3.1):
// melee típico pela tabela; ranged vida ×0,7 / dano ×1,4; mini boss vida ×4 / dano ×1,5 / XP ×3;
// boss XP ×10; cada monstro mantém a proporção dele na média do tier.
public class MonsterScalingTests
{
    [Test]
    public void TargetHealth_ByKind()
    {
        Assert.AreEqual(485f, MonsterScaling.TargetHealth(5, MonsterKind.Melee, 1f), 1e-3f);
        Assert.AreEqual(485f * 0.7f, MonsterScaling.TargetHealth(5, MonsterKind.Ranged, 1f), 1e-3f);
        Assert.AreEqual(485f * 4f, MonsterScaling.TargetHealth(5, MonsterKind.MiniBoss, 1f), 1e-3f);
        Assert.AreEqual(727.5f, MonsterScaling.TargetHealth(5, MonsterKind.Melee, 1.5f), 1e-3f);
    }

    [Test]
    public void TargetDamage_T9Ranged() =>
        Assert.AreEqual(910f * 1.4f, MonsterScaling.TargetDamage(9, MonsterKind.Ranged, 1f), 1e-3f);

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
}
