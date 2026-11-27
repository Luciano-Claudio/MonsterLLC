using NUnit.Framework;

// Loja permanente — rerolls viram estoque da run; Treasure Sense (baú) e Merchant's Whisper (vendedor)
// dão o 1º reroll daquela tela de graça.
public class RerollRulesTests
{
    [Test]
    public void Free_reroll_only_at_the_matching_screen()
    {
        Assert.IsTrue(RerollRules.FreeFor(OfferSource.Chest, treasureSense: true, merchantsWhisper: false));
        Assert.IsFalse(RerollRules.FreeFor(OfferSource.Vendor, treasureSense: true, merchantsWhisper: false));
        Assert.IsTrue(RerollRules.FreeFor(OfferSource.Vendor, treasureSense: false, merchantsWhisper: true));
        Assert.IsFalse(RerollRules.FreeFor(OfferSource.Chest, treasureSense: false, merchantsWhisper: true));
        Assert.IsFalse(RerollRules.FreeFor(OfferSource.LevelUp, true, true));
        Assert.IsFalse(RerollRules.FreeFor(OfferSource.StatItem, true, true));
    }

    [Test]
    public void Spends_the_free_one_before_the_stock()
    {
        int stock = 2; bool free = true;
        Assert.IsTrue(RerollRules.Spend(ref stock, ref free));
        Assert.AreEqual(2, stock); Assert.IsFalse(free);
        Assert.IsTrue(RerollRules.Spend(ref stock, ref free));
        Assert.AreEqual(1, stock);
    }

    [Test]
    public void Stock_never_goes_negative()
    {
        int stock = 0; bool free = false;
        Assert.IsFalse(RerollRules.Spend(ref stock, ref free));
        Assert.AreEqual(0, stock);
        Assert.IsFalse(RerollRules.CanReroll(0, false));
        Assert.IsTrue(RerollRules.CanReroll(0, true));
        Assert.IsTrue(RerollRules.CanReroll(1, false));
    }
}
