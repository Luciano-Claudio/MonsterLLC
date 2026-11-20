using NUnit.Framework;

// Druid (D9.1): a forma acompanha o bool isFeral comparando, todo frame, a forma DESEJADA com a
// ATUAL — troca assim que o Druid ficar livre. Bug da revisão final: desligar isFeral com o urso
// ocupado (garra, transformação) perdia a mudança e o Druid ficava urso pra sempre.
public class DruidVariantRulesTests
{
    [Test]
    public void WantsFeral_Human_Free_TransformsIn()
    {
        Assert.AreEqual(DruidFormAction.TransformIn, DruidVariantRules.Next(wantFeral: true, isBear: false, transforming: false, busy: false));
    }

    [Test]
    public void WantsHuman_Bear_Free_TransformsOut()
    {
        Assert.AreEqual(DruidFormAction.TransformOut, DruidVariantRules.Next(wantFeral: false, isBear: true, transforming: false, busy: false));
    }

    [Test]
    public void WantsHuman_Bear_Busy_Waits()
    {
        Assert.AreEqual(DruidFormAction.None, DruidVariantRules.Next(wantFeral: false, isBear: true, transforming: false, busy: true));
    }

    [Test]
    public void WantsHuman_BearBecameFreeLater_TransformsOut()
    {
        // ocupado num frame, livre no seguinte — a mudança não se perde
        DruidVariantRules.Next(wantFeral: false, isBear: true, transforming: false, busy: true);
        Assert.AreEqual(DruidFormAction.TransformOut, DruidVariantRules.Next(wantFeral: false, isBear: true, transforming: false, busy: false));
    }

    [Test]
    public void Transforming_Waits()
    {
        Assert.AreEqual(DruidFormAction.None, DruidVariantRules.Next(wantFeral: false, isBear: false, transforming: true, busy: false));
    }

    [Test]
    public void AlreadyInWantedForm_DoesNothing()
    {
        Assert.AreEqual(DruidFormAction.None, DruidVariantRules.Next(wantFeral: true, isBear: true, transforming: false, busy: false));
        Assert.AreEqual(DruidFormAction.None, DruidVariantRules.Next(wantFeral: false, isBear: false, transforming: false, busy: false));
    }
}
