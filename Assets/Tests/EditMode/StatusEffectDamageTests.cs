using NUnit.Framework;

// Revisão final D9.1 (achado Important #1): Stun é aplicado com dano 0 — sem esta regra, cada
// tick chamava TakeDamage(0): texto flutuante "0", DamageTrigger no monstro e contagem de DoT.
public class StatusEffectDamageTests
{
    [Test]
    public void ZeroDamageEffect_DoesNotDealDamage()
    {
        Assert.IsFalse(StatusEffectController.DealsDamage(0f));
    }

    [Test]
    public void PositiveDamageEffect_DealsDamage()
    {
        Assert.IsTrue(StatusEffectController.DealsDamage(5f));
    }
}
