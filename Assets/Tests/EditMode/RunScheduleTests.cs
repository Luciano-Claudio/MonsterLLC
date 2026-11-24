using NUnit.Framework;
using UnityEngine;

// Etapas 1+2 — regras da run de 25 min (docs/superpowers/specs/2026-11-24-etapas-1-2-run-25min-cartas-design.md, 5.x).
public class RunScheduleTests
{
    private static readonly int[] SpecOrder = { 1, 1, 1, 2, 2, 2, 3, 3, 3, 4, 4, 4, 5, 5, 5, 6, 6, 6, 7, 7, 8, 9, 9, 9, 10 };

    private RunSettings settings;

    [SetUp] public void CreateSettings() => settings = ScriptableObject.CreateInstance<RunSettings>();
    [TearDown] public void DestroySettings() => Object.DestroyImmediate(settings);

    private RunSchedule Spec() => new RunSchedule(SpecOrder, settings);

    [Test]
    public void Boss_slots_are_one_minute_apart_and_divine_god_is_at_1440()
    {
        var s = Spec();
        Assert.AreEqual(0f, s.BossSpawnSecond(0));
        Assert.AreEqual(1440f, s.BossSpawnSecond(24));
        Assert.AreEqual(1440f, s.FinalBossSecond);
        Assert.AreEqual(1, s.SlotsDueBy(0f));
        Assert.AreEqual(2, s.SlotsDueBy(60f));
        Assert.AreEqual(25, s.SlotsDueBy(5000f));
    }

    [Test]
    public void Night_follows_60_day_30_night_cycle_and_locks_at_final_boss()
    {
        var s = Spec();
        Assert.IsFalse(s.IsNight(59f));
        Assert.IsTrue(s.IsNight(60f));
        Assert.IsTrue(s.IsNight(89f));
        Assert.IsFalse(s.IsNight(90f));
        Assert.IsTrue(s.IsNight(1440f));
        Assert.IsTrue(s.IsNight(1441f));
        Assert.IsTrue(s.IsNight(1600f));
    }

    [Test]
    public void Tier_ramps_to_next_after_last_boss_of_tier()
    {
        var s = Spec();
        Assert.AreEqual(1, s.TierShares(119f, out int a, out float shareA, out _));
        Assert.AreEqual(1, a);

        Assert.AreEqual(2, s.TierShares(165f, out a, out shareA, out int b));
        Assert.AreEqual(1, a);
        Assert.AreEqual(2, b);
        Assert.AreEqual(0.5f, shareA, 0.01f);

        Assert.AreEqual(1, s.TierShares(210f, out a, out _, out _));
        Assert.AreEqual(2, a);
    }

    [Test]
    public void Late_tiers_never_have_three_at_once_and_t10_is_never_horde()
    {
        var s = Spec();
        Assert.AreEqual(2, s.TierShares(1210f, out int a, out float shareA, out int b));
        Assert.AreEqual(8, a);
        Assert.AreEqual(9, b);
        Assert.AreEqual(1f - 10f / 90f, shareA, 0.01f);

        Assert.AreEqual(1, s.TierShares(1300f, out a, out _, out _));
        Assert.AreEqual(9, a);
        Assert.AreEqual(1, s.TierShares(1480f, out a, out _, out _));
        Assert.AreEqual(9, a);
        Assert.AreEqual(9, s.CurrentTier(1480f));
    }

    [Test]
    public void Enrage_speed_after_60s_damage_after_120s_and_steps()
    {
        var s = Spec();
        Assert.AreEqual(1f, s.EnrageSpeedMultiplier(59f));
        Assert.AreEqual(1.5f, s.EnrageSpeedMultiplier(60f));
        Assert.AreEqual(1f, s.EnrageDamageMultiplier(119f));
        Assert.AreEqual(1.5f, s.EnrageDamageMultiplier(120f), 1e-4f);
        Assert.AreEqual(1.5f * 1.25f, s.EnrageDamageMultiplier(150f), 1e-4f);
        Assert.AreEqual(1.5f * 1.5f, s.EnrageDamageMultiplier(180f), 1e-4f);
    }

    [Test]
    public void Clock_pauses_only_at_end_with_final_boss_alive()
    {
        var s = Spec();
        Assert.IsTrue(s.ClockPaused(1500f, true));
        Assert.IsFalse(s.ClockPaused(1500f, false));
        Assert.IsFalse(s.ClockPaused(1499f, true));
    }

    [Test]
    public void Mini_boss_is_15_to_45s_after_its_boss()
    {
        var s = Spec();
        Assert.AreEqual(195f, s.MiniBossSecond(3, 0f));
        Assert.AreEqual(225f, s.MiniBossSecond(3, 1f));
    }

    [Test]
    public void Empty_slot_and_missing_final_boss_do_not_break()
    {
        var s = new RunSchedule(new[] { 1, 0, 1, 2 }, settings);
        Assert.AreEqual(1500f, s.FinalBossSecond);
        Assert.IsTrue(s.IsNight(1430f));  // 1430 mod 90 = 80 -> noite
        Assert.IsFalse(s.IsNight(1460f)); // 1460 mod 90 = 20 -> dia (sem Divine God, sem noite travada)
        Assert.DoesNotThrow(() => s.TierShares(500f, out _, out _, out _));
        Assert.AreEqual(2, s.CurrentTier(500f));
    }

    [Test]
    public void Empty_list_keeps_tier_one()
    {
        var s = new RunSchedule(new int[0], settings);
        Assert.AreEqual(1500f, s.FinalBossSecond);
        Assert.AreEqual(1, s.TierShares(900f, out int a, out _, out _));
        Assert.AreEqual(1, a);
        Assert.AreEqual(0, s.SlotsDueBy(900f));
    }
}
