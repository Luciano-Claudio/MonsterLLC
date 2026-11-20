using NUnit.Framework;

// D9.3 (otimização da horda, medido no D9.2): regras puras por trás dos cortes de custo dos
// monstros — espécie cacheada como número, consultas espaçadas com fase aleatória e
// Simulation Rings por distância mesmo em combate.
public class HordePerformanceRulesTests
{
    // --- SpeciesRegistry -------------------------------------------------------------------

    [Test]
    public void Species_CloneAndPrefabName_SameId()
    {
        Assert.AreEqual(SpeciesRegistry.IdFor("Rat_Test"), SpeciesRegistry.IdFor("Rat_Test(Clone)"));
    }

    [Test]
    public void Species_DifferentNames_DifferentIds()
    {
        Assert.AreNotEqual(SpeciesRegistry.IdFor("Wolf_Test(Clone)"), SpeciesRegistry.IdFor("Bat_Test(Clone)"));
    }

    [Test]
    public void Species_SameName_StableId()
    {
        int first = SpeciesRegistry.IdFor("Orc_Test(Clone)");
        Assert.AreEqual(first, SpeciesRegistry.IdFor("Orc_Test(Clone)"));
    }

    // --- IntervalTimer ---------------------------------------------------------------------

    [Test]
    public void Timer_FirstTick_SpreadByPhase()
    {
        var a = new IntervalTimer(0.25f, 0f, startPhase01: 0f);
        var b = new IntervalTimer(0.25f, 0f, startPhase01: 0.8f);
        Assert.IsTrue(a.TryTick(0f), "fase 0 dispara já no início");
        Assert.IsFalse(b.TryTick(0f), "fase 0,8 espera 0,2 s");
        Assert.IsTrue(b.TryTick(0.2f));
    }

    [Test]
    public void Timer_TicksOncePerInterval()
    {
        var t = new IntervalTimer(0.25f, 0f, startPhase01: 0f);
        Assert.IsTrue(t.TryTick(0f));
        Assert.IsFalse(t.TryTick(0.1f));
        Assert.IsFalse(t.TryTick(0.24f));
        Assert.IsTrue(t.TryTick(0.25f));
    }

    [Test]
    public void Timer_LongPause_DoesNotBurst()
    {
        var t = new IntervalTimer(0.25f, 0f, startPhase01: 0f);
        Assert.IsTrue(t.TryTick(0f));
        Assert.IsTrue(t.TryTick(10f), "voltou depois de muito tempo: 1 tick");
        Assert.IsFalse(t.TryTick(10.01f), "sem rajada de ticks atrasados");
    }

    // --- SimulationRingRules ---------------------------------------------------------------

    private const float Near = 15f;
    private const float CombatNear = 12f;

    [Test]
    public void Ring_PatrolInsideNear_Full()
    {
        Assert.IsTrue(SimulationRingRules.IsFullSimulation(10f, inCombat: false, isBoss: false, Near, CombatNear));
    }

    [Test]
    public void Ring_PatrolOutsideNear_Reduced()
    {
        Assert.IsFalse(SimulationRingRules.IsFullSimulation(20f, inCombat: false, isBoss: false, Near, CombatNear));
    }

    [Test]
    public void Ring_CombatClose_Full()
    {
        Assert.IsTrue(SimulationRingRules.IsFullSimulation(8f, inCombat: true, isBoss: false, Near, CombatNear));
    }

    [Test]
    public void Ring_CombatFar_Reduced()
    {
        // Antes do D9.3, todo monstro em combate era "perto", não importa a distância.
        Assert.IsFalse(SimulationRingRules.IsFullSimulation(30f, inCombat: true, isBoss: false, Near, CombatNear));
    }

    [Test]
    public void Ring_BossInCombat_AlwaysFull()
    {
        Assert.IsTrue(SimulationRingRules.IsFullSimulation(60f, inCombat: true, isBoss: true, Near, CombatNear));
    }
}
