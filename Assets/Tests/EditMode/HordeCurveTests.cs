using NUnit.Framework;
using UnityEngine;

// Curva da horda de 25 min — pedido do usuário (2026-11-27): de dia 10 -> 25 (min 1) -> 50 (min 2) -> 100 (min 3)
// -> 150 (min 4) e fica em 150; à noite +10% (1ª noite), +25% (2ª), +50% (3ª) e o dobro da 4ª em diante,
// sempre abaixo do limite de desempenho (300).
public class HordeCurveTests
{
    private static int T(float t, int night = 0, float curse = 1f) => new HordeCurveSettings().TargetAlive(t, night, curse);

    [Test] public void Run_starts_low() => Assert.AreEqual(10, T(0f));
    [Test] public void Minute_one_is_25() => Assert.AreEqual(25, T(60f));
    [Test] public void Minute_two_is_50() => Assert.AreEqual(50, T(120f));
    [Test] public void Minute_three_is_100() => Assert.AreEqual(100, T(180f));
    [Test] public void Minute_four_is_150() => Assert.AreEqual(150, T(240f));
    [Test] public void Day_stays_150_later() => Assert.AreEqual(150, T(900f));
    [Test] public void Grows_gradually_between_minutes() => Assert.AreEqual(18, T(30f)); // 10 -> 25: metade = 17,5

    [Test] public void First_night_is_plus_10_percent() => Assert.AreEqual(34, T(75f, 1));    // 31,25 × 1,1
    [Test] public void Second_night_is_plus_25_percent() => Assert.AreEqual(94, T(150f, 2)); // 75 × 1,25
    [Test] public void Third_night_is_plus_50_percent() => Assert.AreEqual(225, T(240f, 3)); // 150 × 1,5
    [Test] public void Fourth_night_doubles() => Assert.AreEqual(300, T(330f, 4));          // 150 × 2
    [Test] public void Later_nights_keep_doubling_under_the_cap() => Assert.AreEqual(300, T(870f, 10));

    // Loja permanente — Maldição: +20% de horda por nível (×2 no 5), sempre abaixo do limite de 300.
    [Test] public void Curse_multiplies_the_target() => Assert.AreEqual(50, T(60f, 0, 2f));
    [Test] public void Curse_respects_the_cap() => Assert.AreEqual(300, T(240f, 0, 3f));

    [Test]
    public void Night_never_exceeds_cap()
    {
        var s = new HordeCurveSettings { dayTargetBySecond = HordeCurveSettings.LinearCurve(new Vector2(0f, 400f), new Vector2(1500f, 400f)) };
        Assert.AreEqual(300, s.TargetAlive(900f, 5));
        Assert.AreEqual(300, s.TargetAlive(900f, 0));
    }

    // Qual noite é, pelo segundo da run (60 s de dia + 30 s de noite): 0 = dia.
    [Test]
    public void Night_number_from_run_second()
    {
        Assert.AreEqual(0, RunSchedule.NightNumber(30f, 60f, 30f));
        Assert.AreEqual(1, RunSchedule.NightNumber(60f, 60f, 30f));
        Assert.AreEqual(1, RunSchedule.NightNumber(89f, 60f, 30f));
        Assert.AreEqual(0, RunSchedule.NightNumber(90f, 60f, 30f));
        Assert.AreEqual(2, RunSchedule.NightNumber(150f, 60f, 30f));
        Assert.AreEqual(4, RunSchedule.NightNumber(330f, 60f, 30f));
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
