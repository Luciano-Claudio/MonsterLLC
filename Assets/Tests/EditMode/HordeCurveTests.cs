using NUnit.Framework;
using UnityEngine;

// Etapas 1+2 — curva da horda de 25 min: meta de dia ~50 no minuto 1 e ~150 no minuto 3; à noite
// o dobro, sem passar do limite de desempenho (300). Sem picos periódicos (a noite é o pico).
public class HordeCurveTests
{
    [Test] public void Run_starts_low() => Assert.AreEqual(10, new HordeCurveSettings().TargetAlive(0f, false));
    [Test] public void Minute_one_is_50() => Assert.AreEqual(50, new HordeCurveSettings().TargetAlive(60f, false));
    [Test] public void Minute_three_is_150() => Assert.AreEqual(150, new HordeCurveSettings().TargetAlive(180f, false));
    [Test] public void Day_stays_near_150_later() => Assert.AreEqual(150, new HordeCurveSettings().TargetAlive(900f, false));
    [Test] public void Night_doubles() => Assert.AreEqual(100, new HordeCurveSettings().TargetAlive(60f, true));
    // Loja permanente — Maldição: +20% de horda por nível (×2 no 5), sempre abaixo do limite de 300.
    [Test] public void Curse_multiplies_the_target() => Assert.AreEqual(100, new HordeCurveSettings().TargetAlive(60f, false, 2f));
    [Test] public void Curse_respects_the_cap() => Assert.AreEqual(300, new HordeCurveSettings().TargetAlive(180f, false, 3f));
    [Test] public void Night_at_minute_three_hits_cap() => Assert.AreEqual(300, new HordeCurveSettings().TargetAlive(180f, true));
    [Test]
    public void Night_never_exceeds_cap()
    {
        var s = new HordeCurveSettings { dayTargetBySecond = HordeCurveSettings.LinearCurve(new Vector2(0f, 400f), new Vector2(1500f, 400f)) };
        Assert.AreEqual(300, s.TargetAlive(900f, true));
        Assert.AreEqual(300, s.TargetAlive(900f, false));
    }

    [Test]
    public void RangedGate_Day1_BlocksBefore30()
    {
        Assert.IsTrue(RangedGate.Blocks(1, 29.9f, 30f));
        Assert.IsFalse(RangedGate.Blocks(1, 30f, 30f));
    }

    [Test] public void RangedGate_Day2_NeverBlocks() => Assert.IsFalse(RangedGate.Blocks(2, 0f, 30f));

    [Test]
    public void HordeClock_AdvancesWhenActive()
    {
        var c = new HordeClock();
        c.Reset(0f);
        c.Tick(1f, true);
        Assert.AreEqual(1f, c.Elapsed, 1e-5f);
    }

    [Test]
    public void HordeClock_DoesNotAdvanceWhenInactive()
    {
        var c = new HordeClock();
        c.Reset(0f);
        c.Tick(1f, false);
        Assert.AreEqual(0f, c.Elapsed, 1e-5f);
    }

    [Test]
    public void HordeClock_ResetUsesOffset()
    {
        var c = new HordeClock();
        c.Reset(180f);
        Assert.AreEqual(180f, c.Elapsed, 1e-5f);
    }
}
