using System.Collections.Generic;
using UnityEngine;

// Catálogo de cartas (docs/superpowers/specs/2026-11-25-catalogo-cartas-design.md) — cartas que reagem a
// um ACERTO direto do herói (HeroDamage.OnDirectHit) ou a um ABATE (GameEvents.OnEnemyKilledDetailed):
// Crimson Pact, Echo Fang, Shattered Fortune, Overkill, Relentless Casting, Critical Fever, Crowd
// Breaker, Predator's Focus, Reaper's Feast, Titan's Tribute (vida), Trophy Hunter, Survivor's Legacy,
// Blood Frenzy, Warrior's Feast e Unbroken. (Endless Hunger e Soul Furnace são lidos direto no dano/energia.)
// Os efeitos que causam dano extra (eco, segundo impacto, overkill) passam pelo ponto único com
// CardRunState.InEcho ligado, para nunca gerarem outro efeito em cadeia.
public class HitCardEffects : MonoBehaviour
{
    private static readonly List<EnemyController> nearby = new();

    private void OnEnable()
    {
        HeroDamage.OnDirectHit += HandleHit;
        GameEvents.OnEnemyKilledDetailed += HandleKill;
        GameEvents.OnDamageTaken += HandleDamageTaken;
    }

    private void OnDisable()
    {
        HeroDamage.OnDirectHit -= HandleHit;
        GameEvents.OnEnemyKilledDetailed -= HandleKill;
        GameEvents.OnDamageTaken -= HandleDamageTaken;
    }

    private void Update()
    {
        if (!GameplayGate.IsActive) return;
        float dt = Time.deltaTime;
        if (CardRunState.FeverRemaining > 0f && (CardRunState.FeverRemaining -= dt) <= 0f) CardRunState.FeverStacks = 0;
        if (CardRunState.BloodFrenzyRemaining > 0f) CardRunState.BloodFrenzyRemaining -= dt;
    }

    private static float S(CardStat stat) => HeroController.Cards.Sum(CardAbility.None, stat);

    private static void HandleHit(HeroHit hit)
    {
        var t = RunSettings.Current;
        var hero = HeroController.Instance;
        if (hero == null) return;

        // Crimson Pact — o dano já vem proporcional ao tick (chama do Mage).
        float lifesteal = S(CardStat.Lifesteal);
        if (lifesteal > 0f) hero.HealFromCard(hit.damage * lifesteal);

        // Critical Fever
        if (hit.crit && S(CardStat.CriticalFever) > 0f)
        {
            CardRunState.FeverStacks = Mathf.Min(CardRunState.FeverStacks + 1, t.feverMaxStacks);
            CardRunState.FeverRemaining = t.feverDuration;
        }

        if (hit.ability == CardAbility.Primary)
        {
            // Relentless Casting — limite por segundo.
            if (S(CardStat.RelentlessCasting) > 0f)
            {
                float now = Time.time;
                if (now - CardRunState.RelentlessCastingWindowStart >= 1f) { CardRunState.RelentlessCastingWindowStart = now; CardRunState.RelentlessCastingInWindow = 0f; }
                float cut = Mathf.Min(t.relentlessCastingPerHit * hit.hitWeight, t.relentlessCastingPerSecond - CardRunState.RelentlessCastingInWindow);
                if (cut > 0f) { hero.ReduceShiftCooldown(cut); CardRunState.RelentlessCastingInWindow += cut; }
            }

            if (!CardRunState.InEcho && hit.hitWeight >= 1f)
            {
                // Crowd Breaker — o bônus fica guardado para o PRÓXIMO primário.
                CardRunState.AttackTargets++;
                if (S(CardStat.CrowdBreaker) > 0f && CardRunState.AttackTargets == t.crowdBreakerTargets)
                    CardRunState.PendingPrimaryBonus += t.crowdBreakerBonus;
                // Predator's Focus — as pilhas valem nos próximos acertos neste mesmo alvo.
                if (ReferenceEquals(CardRunState.FocusTarget, hit.target)) CardRunState.FocusStacks++;
                else { CardRunState.FocusTarget = hit.target; CardRunState.FocusStacks = 1; }
            }
        }

        if (CardRunState.InEcho) return;

        // Echo Fang — a cada N acertos inteiros do primário (no Mage, a contagem por ciclo é a Task 10).
        float echo = S(CardStat.EchoFang);
        if (echo > 0f && hit.ability == CardAbility.Primary && hit.hitWeight >= 1f)
        {
            CardRunState.PrimaryHits++;
            if (CardRunState.PrimaryHits % Mathf.Max(1, t.echoFangEvery) == 0 && hit.target != null && !hit.target.IsDead)
                Echo(hit.target, hit.damage * echo, hit.ability, hit.hitWeight);
        }

        // Shattered Fortune
        float second = S(CardStat.CritSecondImpact);
        if (hit.crit && second > 0f && Random.value < second && hit.target != null && !hit.target.IsDead)
            Echo(hit.target, hit.damage * t.critSecondImpactPercent, hit.ability, hit.hitWeight);

        // Overkill
        if (hit.killed && hit.overkill > 0f && S(CardStat.Overkill) > 0f && hit.target != null)
        {
            CardEffectsUtil.Nearby(hit.target.transform.position, t.overkillRange, hit.target, nearby);
            if (nearby.Count > 0) Echo(nearby[0], hit.overkill * t.overkillPercent, hit.ability, 1f);
        }
    }

    private static void Echo(EnemyController target, float damage, CardAbility ability, float weight)
    {
        CardRunState.InEcho = true;
        try { HeroDamage.Direct(target, damage, ability, weight, canCrit: false); }
        finally { CardRunState.InEcho = false; }
    }

    private static void HandleKill(string enemyName, bool isBoss, int directHits, int dotTicks, float maxHealth, int energyReward, Vector3 position)
    {
        var t = RunSettings.Current;
        var hero = HeroController.Instance;
        float oldMax = hero != null ? hero.EffectiveMaxHealth : 0f;

        CardRunState.Kills++;
        CardRunState.KillsSinceDamage++;
        if (isBoss) CardRunState.BossesKilled++;
        if (hero == null) return;

        // Titan's Tribute: a vida máxima mudou no degrau — mantém a proporção de vida atual.
        if (S(CardStat.TitansTribute) > 0f && !Mathf.Approximately(oldMax, hero.EffectiveMaxHealth)) hero.OnCardBonusesChanged(oldMax);

        float max = hero.EffectiveMaxHealth;

        // Reaper's Feast — com limite por segundo.
        float killHeal = S(CardStat.KillHeal);
        if (killHeal > 0f)
        {
            float now = Time.time;
            if (now - CardRunState.KillHealWindowStart >= 1f) { CardRunState.KillHealWindowStart = now; CardRunState.KillHealInWindow = 0f; }
            float amount = Mathf.Min(max * killHeal, max * t.killHealCapPerSecond - CardRunState.KillHealInWindow);
            if (amount > 0f) { hero.HealFromCard(amount); CardRunState.KillHealInWindow += amount; }
        }

        // Blood Frenzy
        if (S(CardStat.BloodFrenzy) > 0f && max > 0f && hero.stats.health / max < t.bloodFrenzyThreshold)
            CardRunState.BloodFrenzyRemaining = t.bloodFrenzyDuration;

        // Warrior's Feast
        if (isBoss && S(CardStat.WarriorsFeast) > 0f)
        {
            hero.HealFromCard(max * t.warriorsHeal);
            hero.AddShield(max * t.warriorsShield, t.warriorsDuration);
        }

        // Unbroken
        if (S(CardStat.Unbroken) > 0f && CardRunState.KillsSinceDamage >= t.unbrokenKills)
        {
            hero.AddShield(max * t.unbrokenShield, t.unbrokenDuration);
            CardRunState.KillsSinceDamage = 0;
        }
    }

    // Dano recebido pelo herói zera a sequência do Unbroken.
    private static void HandleDamageTaken(Vector3 position, float amount, bool isPlayer, bool isDirectPlayerHit)
    {
        if (isPlayer && amount > 0f) CardRunState.KillsSinceDamage = 0;
    }
}
