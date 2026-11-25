using System.Collections.Generic;
using UnityEngine;

// Catálogo de cartas (docs/superpowers/specs/2026-11-25-catalogo-cartas-design.md) — cartas de DOT:
// - Blight Crucible / Ashen Hourglass / Cursed Diadem: preenchem os DotModifiers (lidos pelo StatusEffectController);
// - Affliction Heart: a cada N ticks, explosão em volta do alvo;
// - Affliction Harvest: a cada N ticks, energia da Ultimate;
// - Plague Herald: inimigo que morre com DOT passa 1 aplicação do DOT mais forte de cada tipo a vizinhos;
// - Death's Echo: inimigo morto POR DOT explode (sem espalhar).
public class DotCardEffects : MonoBehaviour
{
    private static readonly List<StatusStack> strongest = new();
    private static readonly List<EnemyController> nearby = new();
    private static readonly Collider2D[] overlap = new Collider2D[64];

    private void OnEnable()
    {
        GameEvents.OnDotTick += HandleDotTick;
        GameEvents.OnEnemyDiedWithDot += HandleDiedWithDot;
        GameEvents.OnDayStart += HandleRunStart;
    }

    private void OnDisable()
    {
        GameEvents.OnDotTick -= HandleDotTick;
        GameEvents.OnEnemyDiedWithDot -= HandleDiedWithDot;
        GameEvents.OnDayStart -= HandleRunStart;
        DotModifiers.Reset();
    }

    private static void HandleRunStart() => DotModifiers.Reset();

    // Barato (6 somas), e assim as cartas valem no mesmo frame em que são pegas.
    private void Update()
    {
        var cards = HeroController.Cards;
        int blight = Mathf.RoundToInt(cards.Sum(CardAbility.None, CardStat.DotStacks));
        DotModifiers.MaxStacks = DotStackRules.MaxStacks(blight);
        DotModifiers.DamageMultiplier = DotStackRules.DotDamageMultiplier(blight);
        DotModifiers.DurationMultiplier = 1f + cards.Sum(CardAbility.None, CardStat.DotDuration);
        DotModifiers.CritChance = cards.Sum(CardAbility.None, CardStat.DotCrit);
        DotModifiers.CritMultiplier = HeroDamageRules.CritMultiplier(cards.Sum(CardAbility.None, CardStat.CritDamage));
    }

    private static void HandleDotTick(StatusEffectController status, StatusEffectType type, float damage, bool crit)
    {
        var cards = HeroController.Cards;
        var t = RunSettings.Current;
        CardRunState.DotTicks++;
        float explosion = cards.Sum(CardAbility.None, CardStat.DotExplosion);
        if (explosion > 0f && status != null && CardRunState.DotTicks % Mathf.Max(1, t.dotExplosionEveryTicks) == 0)
            CardEffectsUtil.Explode(status.transform.position, t.dotExplosionRadius, PrimaryDamage() * explosion, status.GetComponent<EnemyController>());
        if (cards.Sum(CardAbility.None, CardStat.AfflictionHarvest) > 0f && CardRunState.DotTicks % Mathf.Max(1, t.afflictionHarvestEvery) == 0
            && HeroController.Instance != null)
            HeroController.Instance.AddUltimateEnergyPercent(t.afflictionHarvestEnergyPercent);
    }

    private static void HandleDiedWithDot(StatusEffectController status, Vector3 position, bool killedByDot)
    {
        var cards = HeroController.Cards;
        var t = RunSettings.Current;
        var dying = status != null ? status.GetComponent<EnemyController>() : null;

        float spread = cards.Sum(CardAbility.None, CardStat.DotSpread);
        if (spread > 0f && status != null && Random.value < spread)
        {
            status.CollectStrongestDots(strongest);
            CardEffectsUtil.Nearby(position, t.plagueRadius, dying, nearby);
            int targets = 0;
            for (int i = 0; i < nearby.Count && targets < t.plagueTargets; i++)
            {
                var other = nearby[i].GetComponent<StatusEffectController>();
                if (other == null) continue;
                // dps já tem os bônus da 1ª aplicação; repassa sem aplicar o bônus de dano de novo.
                foreach (var s in strongest)
                    other.ApplyStatusEffect(s.type, s.baseDuration, s.damagePerSecond / Mathf.Max(0.0001f, DotModifiers.DamageMultiplier));
                targets++;
            }
        }

        if (killedByDot && cards.Sum(CardAbility.None, CardStat.DeathsEcho) > 0f)
            CardEffectsUtil.Explode(position, t.deathsEchoRadius, PrimaryDamage() * t.deathsEchoPercent, dying);
    }

    // "Dano do primário" das explosões: o de um acerto do primário agora, com as cartas, sem crítico/Shift.
    public static float PrimaryDamage() => HeroController.Instance != null ? HeroController.Instance.EffectsBaseDamage : 0f;
}

// Utilidades das cartas: explosão em área (dano não-direto) e vizinhos de um ponto.
public static class CardEffectsUtil
{
    private static readonly Collider2D[] buffer = new Collider2D[128];
    private static readonly List<EnemyController> targets = new();

    public static void Explode(Vector2 center, float radius, float damage, EnemyController exclude = null)
    {
        if (damage <= 0f) return;
        Nearby(center, radius, exclude, targets);
        foreach (var e in targets) e.TakeDamage(damage, isDirectPlayerHit: false);
    }

    public static void Nearby(Vector2 center, float radius, EnemyController exclude, List<EnemyController> into)
    {
        int count = Physics2D.OverlapCircle(center, radius, ContactFilter2D.noFilter, buffer);
        EnemyController.CollectDistinct(buffer, count, into);
        for (int i = into.Count - 1; i >= 0; i--)
            if (into[i] == null || into[i] == exclude || into[i].IsDead) into.RemoveAt(i);
        into.Sort((a, b) => ((Vector2)a.transform.position - center).sqrMagnitude.CompareTo(((Vector2)b.transform.position - center).sqrMagnitude));
    }
}
