using NUnit.Framework;

// PetSummoner (D9.1, 2026-11-19, decisão do usuário): "tem pet?" (bool) -> "tem a quantidade
// x em jogo?" -> se não, toca a animação de invocar 1x por pet que falta, esperando o herói
// ficar livre. Conferência só quando pedida (escolha da variante, início do dia).
public class PetSummonRulesTests
{
    [Test]
    public void NoPet_NothingMissing()
    {
        Assert.AreEqual(0, PetSummonRules.Missing(hasPet: false, wanted: 2, alive: 0));
    }

    [Test]
    public void HasPet_CountsMissing()
    {
        Assert.AreEqual(2, PetSummonRules.Missing(hasPet: true, wanted: 3, alive: 1));
    }

    [Test]
    public void HasPet_AllAlive_NothingMissing()
    {
        Assert.AreEqual(0, PetSummonRules.Missing(hasPet: true, wanted: 1, alive: 1));
    }

    [Test]
    public void MoreAliveThanWanted_NothingMissing()
    {
        Assert.AreEqual(0, PetSummonRules.Missing(hasPet: true, wanted: 1, alive: 2));
    }

    [Test]
    public void StartSummon_WhenPendingFreeAndMissing()
    {
        Assert.AreEqual(PetSummonStep.StartSummon, PetSummonRules.Next(pending: true, heroBusy: false, missing: 1));
    }

    [Test]
    public void Wait_WhenHeroBusy()
    {
        Assert.AreEqual(PetSummonStep.Wait, PetSummonRules.Next(pending: true, heroBusy: true, missing: 1));
    }

    [Test]
    public void Done_WhenNothingMissing()
    {
        Assert.AreEqual(PetSummonStep.Done, PetSummonRules.Next(pending: true, heroBusy: false, missing: 0));
    }

    [Test]
    public void Idle_WhenNotPending()
    {
        Assert.AreEqual(PetSummonStep.Idle, PetSummonRules.Next(pending: false, heroBusy: false, missing: 1));
    }
}
