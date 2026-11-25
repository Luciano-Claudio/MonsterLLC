using UnityEngine;

// Catálogo de cartas (docs/superpowers/specs/2026-11-25-catalogo-cartas-design.md) — contas puras do
// ponto único de acerto do herói (HeroDamage.Direct). Os valores secundários vêm do RunSettings.

// Bônus das cartas que dependem do ALVO, já somados (Cards.Sum) por quem chama.
public struct DamageCards
{
    public float executeBonus;      // Executioner's Mark
    public float dotTargetBonus;    // Withering Eye
    public bool elementalSupremacy; // atributo
    public bool finalJudgment;      // atributo
    public float trophyBonus;       // Trophy Hunter, já com o número de chefes mortos e o teto
}

public struct TargetInfo
{
    public float health01;
    public bool hasDot;
    public bool isBoss;
    public bool isMiniBoss;
    public CardAbility ability;
    public int focusStacks; // Predator's Focus (pilhas neste alvo)
}

// Bônus ligados ao HERÓI no momento do acerto.
public struct SelfInfo
{
    public CardAbility ability;
    public float endlessHunger;    // já com o teto
    public bool finalStand;        // atributo ativo e vida abaixo do limite
    public int feverStacks;        // Critical Fever
    public bool bloodFrenzy;       // buff ativo
    public float nextPrimaryBonus; // Relentless Momentum + Crowd Breaker pendentes (só no primário)
}

public static class HeroDamageRules
{
    public static float CritChance(float cardCritChance, RunSettings t)
        => (t != null ? t.baseCritChancePercent / 100f : 0.05f) + cardCritChance;

    public static float CritMultiplier(float cardCritDamage) => 2f + cardCritDamage;

    public static float TargetMultiplier(in TargetInfo target, in DamageCards c, RunSettings t)
    {
        float m = 1f;
        if (c.executeBonus > 0f && target.health01 < t.executeThreshold) m *= 1f + c.executeBonus;
        if (target.hasDot)
        {
            m *= 1f + c.dotTargetBonus;
            if (c.elementalSupremacy && target.ability == CardAbility.Primary) m *= 1f + t.elementalSupremacyBonus;
        }
        if (target.isBoss) m *= 1f + c.trophyBonus;
        if (c.finalJudgment && target.ability == CardAbility.Ultimate && (target.isBoss || target.isMiniBoss))
            m *= 1f + t.finalJudgmentBonus;
        if (target.focusStacks > 0) m *= 1f + Mathf.Min(target.focusStacks * t.focusPerHit, t.focusCap);
        return m;
    }

    public static float SelfMultiplier(in SelfInfo s, RunSettings t)
    {
        float m = 1f + s.endlessHunger;
        if (s.finalStand) m *= 1f + t.finalStandDamage;
        if (s.feverStacks > 0) m *= 1f + s.feverStacks * t.feverPerStack;
        if (s.bloodFrenzy) m *= 1f + t.bloodFrenzyBonus;
        if (s.ability == CardAbility.Primary) m *= 1f + s.nextPrimaryBonus;
        return m;
    }

    // Sovereign's Bane: % da vida máxima do alvo, proporcional ao peso do acerto (tick da chama do Mage).
    public static float SovereignBonus(float targetMaxHealth, float percent, float hitWeight)
        => targetMaxHealth * percent * hitWeight;
}
