using NUnit.Framework;
using UnityEngine;

// D9.4 — curva da horda: meta de monstros vivos por (dia, tempo real do dia), com picos e teto.
// Valores iniciais do design (docs/superpowers/specs/2026-11-20-d9-4-horda-cerco-design.md).
public class HordeCurveTests
{
    private static HordeCurveSettings MakeSettings(float spikeMultiplier = 1.5f)
    {
        return new HordeCurveSettings
        {
            shape = new AnimationCurve(
                new Keyframe(0f, 0f), new Keyframe(0.125f, 0.0357f), new Keyframe(0.25f, 0.1429f),
                new Keyframe(0.5f, 0.3571f), new Keyframe(0.75f, 0.6429f), new Keyframe(1f, 1f)),
            secondsToCeiling = 240f,
            days = new[]
            {
                new DayPopulation { start = 10, ceiling = 150 },
                new DayPopulation { start = 15, ceiling = 180 },
                new DayPopulation { start = 25, ceiling = 210 },
                new DayPopulation { start = 30, ceiling = 240 },
                new DayPopulation { start = 40, ceiling = 270 },
                new DayPopulation { start = 50, ceiling = 300 },
                new DayPopulation { start = 50, ceiling = 300 },
                new DayPopulation { start = 50, ceiling = 300 },
                new DayPopulation { start = 50, ceiling = 300 },
                new DayPopulation { start = 50, ceiling = 300 },
            },
            spikeInterval = 60f,
            spikeDuration = 15f,
            spikeMultiplier = spikeMultiplier,
            hardCap = 300,
        };
    }

    [Test] public void Curve_Day1_Start_Is10() => Assert.AreEqual(10, MakeSettings().TargetAlive(0f, 1));

    // 240 s cai numa janela de pico (240 % 60 = 0) — o teto "puro" é medido sem pico.
    [Test] public void Curve_Day1_At240_IsCeiling150() => Assert.AreEqual(150, MakeSettings(spikeMultiplier: 1f).TargetAlive(240f, 1));

    [Test] public void Curve_AfterCeiling_StaysAtCeiling() => Assert.AreEqual(150, MakeSettings().TargetAlive(400f, 1)); // 400 % 60 = 40, fora do pico

    [Test] public void Curve_Day1_At30_Is15() => Assert.AreEqual(15, MakeSettings().TargetAlive(30f, 1), 1);

    [Test] public void Curve_Day3_Start_Is25() => Assert.AreEqual(25, MakeSettings().TargetAlive(0f, 3));

    [Test]
    public void Curve_SpikeWindow_Adds50Percent()
    {
        var noSpike = MakeSettings(spikeMultiplier: 1f);
        var s = MakeSettings();
        Assert.AreEqual(Mathf.RoundToInt(noSpike.TargetAlive(65f, 1) * 1.5f), s.TargetAlive(65f, 1), 1);
        Assert.AreEqual(noSpike.TargetAlive(80f, 1), s.TargetAlive(80f, 1));
    }

    [Test]
    public void Curve_NoSpikeBeforeFirstInterval()
    {
        Assert.AreEqual(MakeSettings(spikeMultiplier: 1f).TargetAlive(10f, 1), MakeSettings().TargetAlive(10f, 1));
    }

    [Test] public void Curve_SpikeNeverExceedsHardCap() => Assert.AreEqual(300, MakeSettings().TargetAlive(245f, 10));

    [Test]
    public void Curve_DayOutOfRange_IsClamped()
    {
        var s = MakeSettings();
        Assert.AreEqual(s.TargetAlive(0f, 1), s.TargetAlive(0f, 0));
        Assert.AreEqual(s.TargetAlive(0f, 10), s.TargetAlive(0f, 15));
    }

    // Entre as chaves a subida é linear (sem "estacionar" em cada chave): 75 s = 1/4 do caminho
    // entre 60 s (30 vivos) e 120 s (60 vivos) -> 37,5.
    [Test] public void DefaultShape_IsLinearBetweenKeys() => Assert.AreEqual(38, new HordeCurveSettings().TargetAlive(75f, 1), 1);

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
