using NUnit.Framework;
using UnityEngine;

// Catálogo de cartas (docs/superpowers/specs/2026-11-25-catalogo-cartas-design.md) — regras puras do
// ponto único de acerto: crítico, bônus ligados ao alvo e ao herói, Sovereign's Bane, estado da run.
public class HeroDamageRulesTests
{
    private RunSettings t;

    [SetUp] public void Setup() => t = ScriptableObject.CreateInstance<RunSettings>();

    [TearDown]
    public void Cleanup()
    {
        Object.DestroyImmediate(t);
        CardRunState.Reset();
    }

    private static DamageCards NoCards() => default;

    [Test]
    public void Base_crit_is_5_percent_plus_cards()
    {
        Assert.AreEqual(0.05f, HeroDamageRules.CritChance(0f, t), 1e-5f);
        Assert.AreEqual(0.15f, HeroDamageRules.CritChance(0.10f, t), 1e-5f);
    }

    [Test]
    public void Crit_multiplier_is_2_plus_razor_crown()
    {
        Assert.AreEqual(2f, HeroDamageRules.CritMultiplier(0f), 1e-5f);
        Assert.AreEqual(2.5f, HeroDamageRules.CritMultiplier(0.5f), 1e-5f);
    }

    [Test]
    public void Executioner_only_below_30_percent()
    {
        var c = NoCards(); c.executeBonus = 0.2f;
        var low = new TargetInfo { health01 = 0.29f, ability = CardAbility.Primary };
        var high = new TargetInfo { health01 = 0.31f, ability = CardAbility.Primary };
        Assert.AreEqual(1.2f, HeroDamageRules.TargetMultiplier(low, c, t), 1e-4f);
        Assert.AreEqual(1f, HeroDamageRules.TargetMultiplier(high, c, t), 1e-4f);
    }

    [Test]
    public void Elemental_supremacy_only_primary_with_dot()
    {
        var c = NoCards(); c.elementalSupremacy = true;
        Assert.AreEqual(1.25f, HeroDamageRules.TargetMultiplier(new TargetInfo { health01 = 1f, hasDot = true, ability = CardAbility.Primary }, c, t), 1e-4f);
        Assert.AreEqual(1f, HeroDamageRules.TargetMultiplier(new TargetInfo { health01 = 1f, hasDot = true, ability = CardAbility.Ultimate }, c, t), 1e-4f);
        Assert.AreEqual(1f, HeroDamageRules.TargetMultiplier(new TargetInfo { health01 = 1f, hasDot = false, ability = CardAbility.Primary }, c, t), 1e-4f);
    }

    [Test]
    public void Withering_eye_any_ability_with_dot()
    {
        var c = NoCards(); c.dotTargetBonus = 0.16f;
        Assert.AreEqual(1.16f, HeroDamageRules.TargetMultiplier(new TargetInfo { health01 = 1f, hasDot = true, ability = CardAbility.Ultimate }, c, t), 1e-4f);
    }

    [Test]
    public void Final_judgment_only_ultimate_vs_boss_or_miniboss()
    {
        var c = NoCards(); c.finalJudgment = true;
        Assert.AreEqual(1.3f, HeroDamageRules.TargetMultiplier(new TargetInfo { health01 = 1f, isBoss = true, ability = CardAbility.Ultimate }, c, t), 1e-4f);
        Assert.AreEqual(1.3f, HeroDamageRules.TargetMultiplier(new TargetInfo { health01 = 1f, isMiniBoss = true, ability = CardAbility.Ultimate }, c, t), 1e-4f);
        Assert.AreEqual(1f, HeroDamageRules.TargetMultiplier(new TargetInfo { health01 = 1f, isBoss = true, ability = CardAbility.Primary }, c, t), 1e-4f);
        Assert.AreEqual(1f, HeroDamageRules.TargetMultiplier(new TargetInfo { health01 = 1f, ability = CardAbility.Ultimate }, c, t), 1e-4f);
    }

    [Test]
    public void Trophy_hunter_only_vs_boss()
    {
        var c = NoCards(); c.trophyBonus = 0.15f;
        Assert.AreEqual(1.15f, HeroDamageRules.TargetMultiplier(new TargetInfo { health01 = 1f, isBoss = true, ability = CardAbility.Primary }, c, t), 1e-4f);
        Assert.AreEqual(1f, HeroDamageRules.TargetMultiplier(new TargetInfo { health01 = 1f, ability = CardAbility.Primary }, c, t), 1e-4f);
    }

    [Test]
    public void Predator_focus_caps_at_50_percent()
    {
        var c = NoCards();
        Assert.AreEqual(1.25f, HeroDamageRules.TargetMultiplier(new TargetInfo { health01 = 1f, focusStacks = 5, ability = CardAbility.Primary }, c, t), 1e-4f);
        Assert.AreEqual(1.5f, HeroDamageRules.TargetMultiplier(new TargetInfo { health01 = 1f, focusStacks = 30, ability = CardAbility.Primary }, c, t), 1e-4f);
    }

    [Test]
    public void Sovereign_scales_with_hit_weight()
    {
        Assert.AreEqual(10f, HeroDamageRules.SovereignBonus(1000f, 0.01f, 1f), 1e-4f);
        Assert.AreEqual(2.5f, HeroDamageRules.SovereignBonus(1000f, 0.01f, 0.25f), 1e-4f);
    }

    [Test]
    public void Self_bonuses_multiply()
    {
        var s = new SelfInfo
        {
            ability = CardAbility.Primary, endlessHunger = 0.10f, finalStand = true, feverStacks = 5,
            bloodFrenzy = true, nextPrimaryBonus = 0.5f,
        };
        float expected = 1.10f * 1.20f * 1.10f * 1.25f * 1.5f;
        Assert.AreEqual(expected, HeroDamageRules.SelfMultiplier(s, t), 1e-4f);
        s.ability = CardAbility.Ultimate; // o bônus de "próximo primário" não vale na Ultimate
        Assert.AreEqual(expected / 1.5f, HeroDamageRules.SelfMultiplier(s, t), 1e-4f);
    }

    [Test]
    public void Run_state_reset_clears_everything()
    {
        CardRunState.Kills = 10; CardRunState.KillsSinceDamage = 4; CardRunState.BossesKilled = 2;
        CardRunState.FeverStacks = 3; CardRunState.NextPrimaryBonus = 0.4f; CardRunState.FocusStacks = 7;
        CardRunState.DotTicks = 9; CardRunState.LastEmberTime = 12f; CardRunState.MovingSeconds = 3f; CardRunState.ScarStacks = 2;
        CardRunState.Reset();
        Assert.AreEqual(0, CardRunState.Kills);
        Assert.AreEqual(0, CardRunState.KillsSinceDamage);
        Assert.AreEqual(0, CardRunState.BossesKilled);
        Assert.AreEqual(0, CardRunState.FeverStacks);
        Assert.AreEqual(0f, CardRunState.NextPrimaryBonus);
        Assert.AreEqual(0, CardRunState.FocusStacks);
        Assert.IsNull(CardRunState.FocusTarget);
        Assert.AreEqual(0, CardRunState.DotTicks);
        Assert.AreEqual(float.NegativeInfinity, CardRunState.LastEmberTime);
        Assert.AreEqual(0f, CardRunState.MovingSeconds);
        Assert.AreEqual(0, CardRunState.ScarStacks);
    }
}
