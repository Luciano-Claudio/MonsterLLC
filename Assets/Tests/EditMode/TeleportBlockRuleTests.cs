using NUnit.Framework;

// Bug real (2026-11-19): o teleporte do Mage / dash do Assassin parava no corpo dos monstros.
// A trava contra colliders sólidos existe pra props/água/árvores, nunca pra monstro.
public class TeleportBlockRuleTests
{
    private const int EnemyLayer = 9;
    private const int DefaultLayer = 0;

    [Test]
    public void SolidProp_Blocks()
    {
        Assert.IsTrue(TeleportBlockRule.Blocks(DefaultLayer, isTrigger: false, isCaster: false, enemyLayer: EnemyLayer));
    }

    [Test]
    public void MonsterBody_DoesNotBlock()
    {
        Assert.IsFalse(TeleportBlockRule.Blocks(EnemyLayer, isTrigger: false, isCaster: false, enemyLayer: EnemyLayer));
    }

    [Test]
    public void Trigger_DoesNotBlock()
    {
        Assert.IsFalse(TeleportBlockRule.Blocks(DefaultLayer, isTrigger: true, isCaster: false, enemyLayer: EnemyLayer));
    }

    [Test]
    public void OwnCollider_DoesNotBlock()
    {
        Assert.IsFalse(TeleportBlockRule.Blocks(DefaultLayer, isTrigger: false, isCaster: true, enemyLayer: EnemyLayer));
    }
}
