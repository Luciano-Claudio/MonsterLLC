using UnityEngine;

// Catálogo de cartas (docs/superpowers/specs/2026-11-25-catalogo-cartas-design.md) — o PONTO ÚNICO de
// todo dano DIRETO dos heróis (golpe do Barbarian, Thunder Blades, flechas e facas do Ranger, chama e
// bola de fogo do Mage). Aplica os bônus das cartas ligados ao herói e ao alvo, sorteia o crítico,
// aplica o dano e avisa quem precisa (OnDirectHit). DOT, pets, espinhos e ticks no chão NÃO passam por aqui.
public struct HeroHit
{
    public EnemyController target;
    public CardAbility ability;
    public float damage;
    public bool crit;
    public bool killed;
    public float overkill;  // dano além da vida que o alvo tinha (0 em projéteis com reserva de perfuração)
    public float hitWeight; // 1 = acerto inteiro; tick da chama do Mage = intervalo / ciclo
}

public static class HeroDamage
{
    public static event System.Action<HeroHit> OnDirectHit;

    // Começo de um ataque primário (golpe do Barbarian, disparo do Ranger, ciclo da chama do Mage): os
    // bônus guardados de "próximo primário" (Relentless Momentum, Crowd Breaker) passam a valer para os
    // acertos DESTE ataque, e a contagem de alvos recomeça.
    // Chama do Mage: cada volta (ciclo de 12 quadros) conta como 1 ataque primário — avisa as cartas que
    // contam por ataque (Echo Fang, Predator's Focus, Crowd Breaker) e abre o próximo ataque.
    public static event System.Action OnFlameCycle;
    public static void FlameCycle()
    {
        OnFlameCycle?.Invoke();
        BeginPrimaryAttack();
    }

    public static void BeginPrimaryAttack()
    {
        CardRunState.NextPrimaryBonus = CardRunState.PendingPrimaryBonus;
        CardRunState.NextPrimaryArea = CardRunState.PendingPrimaryArea;
        CardRunState.PendingPrimaryBonus = 0f;
        CardRunState.PendingPrimaryArea = 0f;
        CardRunState.AttackTargets = 0;
    }

    // pierce: projétil com reserva de perfuração (flecha, faca, Thunder Blade) — o que sobra continua no
    // projétil, então nunca gera Overkill. juice: false = sem shake/zoom (ex.: tick da chama).
    public static HeroHit Direct(EnemyController target, float baseDamage, CardAbility ability,
        float hitWeight = 1f, bool canCrit = true, bool juice = true, bool pierce = false)
    {
        var hit = new HeroHit { target = target, ability = ability, hitWeight = hitWeight };
        if (target == null || target.IsDead || baseDamage <= 0f) return hit;

        var cards = HeroController.Cards;
        var t = RunSettings.Current;
        float damage = baseDamage * HeroDamageRules.SelfMultiplier(BuildSelf(ability, t), t)
            * HeroDamageRules.TargetMultiplier(BuildTarget(target, ability, t), BuildCards(t), t);
        if (ability == CardAbility.Primary)
            damage += HeroDamageRules.SovereignBonus(target.stats.maxHealth, cards.Sum(CardAbility.None, CardStat.BossHealthPercentDamage), hitWeight);

        if (canCrit && Random.value < HeroDamageRules.CritChance(cards.Sum(CardAbility.None, CardStat.CritChance), t))
        {
            hit.crit = true;
            damage *= HeroDamageRules.CritMultiplier(cards.Sum(CardAbility.None, CardStat.CritDamage));
        }

        float before = target.stats.health;
        target.TakeDamage(damage, isDirectPlayerHit: juice);
        hit.damage = damage;
        hit.killed = target.IsDead || HealthSystem.IsDead(target.stats.health);
        hit.overkill = hit.killed && !pierce ? Mathf.Max(0f, damage - before) : 0f;
        OnDirectHit?.Invoke(hit);
        return hit;
    }

    private static SelfInfo BuildSelf(CardAbility ability, RunSettings t)
    {
        var cards = HeroController.Cards;
        var hero = HeroController.Instance;
        float health01 = hero != null && hero.EffectiveMaxHealth > 0f ? hero.stats.health / hero.EffectiveMaxHealth : 1f;
        return new SelfInfo
        {
            ability = ability,
            endlessHunger = cards.Sum(CardAbility.None, CardStat.EndlessHunger) > 0f
                ? Mathf.Min(CardRunState.Kills / Mathf.Max(1, t.endlessKillsPerStep) * t.endlessPerStep, t.endlessCap) : 0f,
            finalStand = cards.Sum(CardAbility.None, CardStat.FinalStand) > 0f && health01 < t.finalStandThreshold,
            feverStacks = cards.Sum(CardAbility.None, CardStat.CriticalFever) > 0f ? CardRunState.FeverStacks : 0,
            bloodFrenzy = CardRunState.BloodFrenzyRemaining > 0f,
            nextPrimaryBonus = CardRunState.NextPrimaryBonus,
        };
    }

    private static TargetInfo BuildTarget(EnemyController target, CardAbility ability, RunSettings t)
    {
        var status = target.GetComponent<StatusEffectController>();
        bool focus = ability == CardAbility.Primary && HeroController.Cards.Sum(CardAbility.None, CardStat.PredatorsFocus) > 0f
            && ReferenceEquals(CardRunState.FocusTarget, target);
        return new TargetInfo
        {
            health01 = target.stats.maxHealth > 0f ? target.stats.health / target.stats.maxHealth : 1f,
            hasDot = status != null && status.HasAnyDot,
            isBoss = target.IsBoss,
            isMiniBoss = target.IsMiniBoss,
            ability = ability,
            focusStacks = focus ? CardRunState.FocusStacks : 0,
        };
    }

    private static DamageCards BuildCards(RunSettings t)
    {
        var cards = HeroController.Cards;
        return new DamageCards
        {
            executeBonus = cards.Sum(CardAbility.None, CardStat.ExecuteBonus),
            dotTargetBonus = cards.Sum(CardAbility.None, CardStat.DotTargetBonus),
            elementalSupremacy = cards.Sum(CardAbility.None, CardStat.ElementalSupremacy) > 0f,
            finalJudgment = cards.Sum(CardAbility.None, CardStat.FinalJudgment) > 0f,
            trophyBonus = cards.Sum(CardAbility.None, CardStat.TrophyHunter) > 0f
                ? Mathf.Min(CardRunState.BossesKilled * t.trophyPerBoss, t.trophyCap) : 0f,
        };
    }
}
