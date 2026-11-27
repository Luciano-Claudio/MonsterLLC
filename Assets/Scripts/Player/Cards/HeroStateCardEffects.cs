using System.Collections.Generic;
using UnityEngine;

// Catálogo de cartas (docs/superpowers/specs/2026-11-25-catalogo-cartas-design.md) — cartas ligadas ao
// estado do herói: Thornwake Armor, Colossus Heart (escala), Gravebound Plate, Last Ember (tempo e
// invulnerabilidade; a morte evitada fica no HeroController.TakeDamage), Wellspring Chalice, Spirit
// Link, Final Stand, Scars of Battle, Moving Fortress, Adrenaline Rush, Golden Momentum, Relentless
// Momentum e o tamanho do Titan's Tribute. A redução de dano extra vai em CardRunState.ExtraReduction.
public class HeroStateCardEffects : MonoBehaviour
{
    private static readonly List<EnemyController> nearby = new();
    private float crowdTimer;
    private int crowdCount;
    private float lastScale = -1f;

    public static float ExtraReduction => CardRunState.ExtraReduction;

    private void OnEnable()
    {
        HeroController.OnHeroDirectDamaged += HandleDirectDamaged;
        HeroController.OnShiftUsed += HandleShiftUsed;
        GameEvents.OnXpCollected += HandleXpCollected;
        GameEvents.OnGoldCoinCollected += HandleGoldCoinCollected;
        GameEvents.OnStatItemCollected += HandleLootCollected;
        GameEvents.OnRerollDiceCollected += HandleLootCollected;
    }

    private void OnDisable()
    {
        HeroController.OnHeroDirectDamaged -= HandleDirectDamaged;
        HeroController.OnShiftUsed -= HandleShiftUsed;
        GameEvents.OnXpCollected -= HandleXpCollected;
        GameEvents.OnGoldCoinCollected -= HandleGoldCoinCollected;
        GameEvents.OnStatItemCollected -= HandleLootCollected;
        GameEvents.OnRerollDiceCollected -= HandleLootCollected;
    }

    private void Update()
    {
        if (GameplayGate.IsActive) Tick(Time.deltaTime);
    }

    private static float S(CardStat stat) => HeroController.Cards.Sum(CardAbility.None, stat);

    private void Tick(float dt)
    {
        var t = RunSettings.Current;
        CardRunState.RunSeconds += dt;
        Decay(ref CardRunState.EmberInvulnerableRemaining, dt);
        Decay(ref CardRunState.SpeedBuffRemaining, dt);
        Decay(ref CardRunState.AdrenalineRemaining, dt);
        Decay(ref CardRunState.AdrenalineCooldown, dt);
        Decay(ref CardRunState.WellspringCooldown, dt);
        Decay(ref CardRunState.ThornCooldown, dt);
        if (CardRunState.ScarRemaining > 0f && (CardRunState.ScarRemaining -= dt) <= 0f) CardRunState.ScarStacks = 0;

        var hero = HeroController.Instance;
        if (hero == null) { CardRunState.ExtraReduction = 0f; return; }

        // Movimento contínuo (Relentless Momentum, Moving Fortress).
        if (hero.IsMovingInput) CardRunState.MovingSeconds += dt;
        else { CardRunState.MovingSeconds = 0f; CardRunState.RelentlessArmed = false; }
        if (!CardRunState.RelentlessArmed && S(CardStat.RelentlessMomentum) > 0f && CardRunState.MovingSeconds >= t.relentlessMoveSeconds)
        {
            CardRunState.RelentlessArmed = true;
            CardRunState.PendingPrimaryBonus += t.relentlessDamage;
            CardRunState.PendingPrimaryArea += t.relentlessArea;
        }

        // Gravebound: conta os inimigos perto 4x por segundo (não por frame).
        float crowd = S(CardStat.CrowdArmor);
        if (crowd > 0f)
        {
            crowdTimer -= dt;
            if (crowdTimer <= 0f)
            {
                crowdTimer = 0.25f;
                CardEffectsUtil.Nearby(hero.transform.position, t.graveboundRadius, null, nearby);
                crowdCount = Mathf.Min(nearby.Count, t.graveboundMaxEnemies);
            }
        }
        else crowdCount = 0;

        float max = hero.EffectiveMaxHealth;
        float health01 = max > 0f ? hero.stats.health / max : 1f;
        float reduction = crowdCount * crowd;
        if (S(CardStat.FinalStand) > 0f && health01 < t.finalStandThreshold) reduction += t.finalStandReduction;
        if (S(CardStat.ScarsOfBattle) > 0f) reduction += CardRunState.ScarStacks * t.scarPerStack;
        if (S(CardStat.MovingFortress) > 0f && CardRunState.MovingSeconds >= t.movingFortressSeconds) reduction += t.movingFortressReduction;
        CardRunState.ExtraReduction = reduction;

        // Escala (Colossus Heart / Titan's Tribute) — só quando muda.
        float scale = hero.BaseScale * HeroController.CardSizeFactor;
        if (!Mathf.Approximately(scale, lastScale))
        {
            lastScale = scale;
            hero.transform.localScale = Vector3.one * scale;
        }
    }

    private static void Decay(ref float timer, float dt) { if (timer > 0f) timer = Mathf.Max(0f, timer - dt); }

    private static void HandleDirectDamaged(HeroController hero, float amount)
    {
        var t = RunSettings.Current;
        if (S(CardStat.ScarsOfBattle) > 0f)
        {
            CardRunState.ScarStacks = Mathf.Min(CardRunState.ScarStacks + 1, t.scarMaxStacks);
            CardRunState.ScarRemaining = t.scarDuration;
        }

        float max = hero.EffectiveMaxHealth;
        if (S(CardStat.AdrenalineRush) > 0f && max > 0f && hero.stats.health / max < t.adrenalineThreshold && CardRunState.AdrenalineCooldown <= 0f)
        {
            CardRunState.AdrenalineRemaining = t.adrenalineDuration;
            CardRunState.AdrenalineCooldown = t.adrenalineCooldown;
        }

        float thorns = S(CardStat.ThornSpikes);
        if (thorns > 0f && CardRunState.ThornCooldown <= 0f && t.thornProjectilePrefab != null)
        {
            CardRunState.ThornCooldown = t.thornCooldown;
            int level = Mathf.Max(1, Mathf.RoundToInt(thorns / Mathf.Max(0.0001f, t.thornDamagePerLevel)));
            int count = t.thornBaseCount + level;
            float damage = max * thorns;
            for (int i = 0; i < count; i++)
            {
                float angle = i * Mathf.PI * 2f / count;
                var dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                var obj = HeroProjectilePool.Spawn(t.thornProjectilePrefab, hero.transform.position, HeroController.HerosContainer); // fila de projéteis
                obj.transform.localScale = Vector3.one * t.thornScale;
                var p = obj.GetComponent<HeroProjectile>();
                if (p == null) continue;
                p.SetCountsAsHeroHit(false);
                p.Launch(dir, damage, t.thornSpeed, t.thornRange, 0f);
            }
        }
    }

    private static void HandleShiftUsed(HeroController hero)
    {
        var t = RunSettings.Current;
        float heal = S(CardStat.ShiftHeal);
        if (heal > 0f && CardRunState.WellspringCooldown <= 0f)
        {
            hero.HealFromCard(hero.EffectiveMaxHealth * heal);
            CardRunState.WellspringCooldown = t.wellspringCooldown;
        }
        if (S(CardStat.SpiritLink) > 0f) hero.AddUltimateEnergyPercent(t.spiritLinkEnergyPercent);
    }

    private static void HandleGoldCoinCollected(int gold) => HandleXpCollected(gold);
    private static void HandleLootCollected() => HandleXpCollected(1);

    // Golden Momentum: qualquer loot coletado (gema de XP ou moeda de gold).
    private static void HandleXpCollected(int xp)
    {
        if (S(CardStat.GoldenMomentum) > 0f) CardRunState.SpeedBuffRemaining = RunSettings.Current.goldenMomentumDuration;
    }
}
