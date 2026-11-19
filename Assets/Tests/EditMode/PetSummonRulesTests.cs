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
    public void PickSpawn_OnlyAmongFreePoints()
    {
        // pontos 0 e 2 ocupados: o sorteio só pode cair em 1 ou 3
        var occupied = new[] { true, false, true, false };
        Assert.AreEqual(1, PetSummonRules.PickSpawnIndex(occupied, random01: 0f));
        Assert.AreEqual(3, PetSummonRules.PickSpawnIndex(occupied, random01: 0.99f));
    }

    [Test]
    public void PickSpawn_AllOccupied_FallsBackToAnyPoint()
    {
        var occupied = new[] { true, true };
        Assert.AreEqual(1, PetSummonRules.PickSpawnIndex(occupied, random01: 0.99f));
    }

    [Test]
    public void PickSpawn_NoPoints_ReturnsMinusOne()
    {
        Assert.AreEqual(-1, PetSummonRules.PickSpawnIndex(new bool[0], random01: 0.5f));
    }

    [Test]
    public void Idle_WhenNotPending()
    {
        Assert.AreEqual(PetSummonStep.Idle, PetSummonRules.Next(pending: false, heroBusy: false, missing: 1));
    }
}
