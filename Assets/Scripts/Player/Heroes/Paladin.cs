using System.Collections.Generic;
using UnityEngine;

public class Paladin : HeroController
{
    // ===================== Primário — martelo arremessado (GDD Seção 17.7) =====================
    [Header("Ataque primário — martelo (projétil reto, mira livre)")]
    [SerializeField] private GameObject hammerProjectilePrefab; // precisa ter PaladinHammer
    [SerializeField] private LayerMask enemyLayerMask; // repassado pro PaladinHammer — mesmo padrão do Rogue/RogueBomb
    // Controlador decide TUDO sobre o martelo e repassa pro prefab a cada Launch() — o
    // PaladinHammer não guarda nenhum valor de balanceamento próprio, só wiring (Animator/
    // Collider) e a duração do clipe de impacto (isso sim é dele, é timing de animação, não
    // upgrade). Assim, quando upgrades existirem, só se edita aqui — nunca o prefab do martelo.
    [SerializeField] private float hammerSpeed = 12f; // 🔢 ajustável
    [SerializeField] private float hammerMaxRange = 10f; // 🔢 ajustável
    [SerializeField] private float hammerExplosionRadius = 2f; // 🔢 ajustável
    // Upgrade futuro — até 5 martelos por lançamento, cada um com dano cheio (sem dividir a
    // reserva entre eles, mesmo critério dos 8 projéteis da Ultimate do Barbarian). A partir
    // de 2, os martelos extras abrem em leque: pares completos ficam simétricos (±15°, ±30°),
    // e a contagem ÍMPAR de extras (2 e 4) resolve o último pra cima ou pra baixo conforme o
    // quadrante da mira (Y da mira >= 0 → pra cima, como NE; < 0 → pra baixo, como SE).
    // Ver GetHammerAngles().
    [SerializeField] private int hammerCount = 1; // 🔢 upgradable — 1 a 5 (ver OnValidate)
    private readonly List<float> hammerAngles = new();

    // Mesma rede de segurança do Barbarian — Animation Event de fim de ação nunca disparar
    // não pode travar o herói pra sempre em isAttacking.
    [SerializeField] private float maxActionDuration = 3f; // 🔢 ajustável
    private float actionElapsed;
    private bool hammerThrowFired;

    // ===================== Ultimate — espadas orbitando (Orbiting Hitbox) =====================
    [Header("Ultimate — espadas orbitando")]
    [SerializeField] private PaladinOrbitingBlades orbitingBlades; // filho dedicado, já no prefab, começa "Empty"
    [SerializeField] private float ultimateDamageMultiplier = 2f; // 🔢 GDD: "2x o dano do Paladin"
    [SerializeField] private float ultimateDuration = 5f; // 🔢 "por alguns segundos"
    // Mesmo motivo do martelo — bladeCount/orbitSpeed são valores de upgrade, controlados
    // aqui e repassados pro PaladinOrbitingBlades em cada Activate(). O componente nas
    // espadas só guarda wiring (os 8 slots), nada de balanceamento.
    [SerializeField] private int bladeCount = 2; // 🔢 upgradable no futuro — só 2, 4 ou 8 (nunca ímpar, ver OnValidate)
    [SerializeField] private float orbitSpeedDegreesPerSecond = 360f; // 🔢 ajustável

    // ===================== Secundária (Shift) — shield bash =====================
    [Header("Habilidade Secundária (Shift) — shield bash (4 triggers cardeais simultâneos)")]
    [SerializeField] private Collider2D shieldBashHitboxN;
    [SerializeField] private Collider2D shieldBashHitboxS;
    [SerializeField] private Collider2D shieldBashHitboxE;
    [SerializeField] private Collider2D shieldBashHitboxW;
    [SerializeField] private float shieldBashDamageMultiplier = 1f; // 🔢 ajustável
    [SerializeField] private float shieldBashKnockbackForce = 4f; // 🔢 ajustável
    [SerializeField] private float maxShieldBashDuration = 2f; // 🔢 rede de segurança
    private float shieldBashElapsed;
    private bool shieldBashHitFired;

    // ===================== Passiva — shield periódico com vida própria =====================
    [Header("Passiva — shield periódico")]
    [SerializeField] private Animator shieldFrontAnimator; // DomeStart/DomeCycle/DomeEnd/None — na frente (ShieldDome)
    [SerializeField] private Animator shieldBackAnimator;   // DomeStart/DomeCycle/DomeEnd/None — atrás (ShieldBase), mesmos nomes de estado, Controller próprio
    [SerializeField] private float shieldMaxHealth = 20f; // 🔢 ajustável
    [SerializeField] private float shieldRecoverInterval = 30f; // 🔢 GDD: "a cada 30s, ajustável"
    // Cor do FloatingCombatText quando o hit é absorvido pelo shield — azul bebê, pra
    // diferenciar visualmente de "machucou de verdade" (cor normal do dano).
    [SerializeField] private Color shieldBlockedTextColor = new Color(0.537f, 0.812f, 0.941f);
    private float shieldCurrentHealth;
    private float shieldCooldownRemaining;
    private bool shieldActive;

    private readonly List<EnemyController> hitTargets = new();
    private static readonly Collider2D[] OverlapBuffer = new Collider2D[16];

#if UNITY_EDITOR
    private void OnValidate()
    {
        // GDD: nunca número ímpar — sempre 2, 4 ou 8.
        if (bladeCount >= 8) bladeCount = 8;
        else if (bladeCount >= 4) bladeCount = 4;
        else bladeCount = 2;

        hammerCount = Mathf.Clamp(hammerCount, 1, 5);
    }
#endif

    protected override void Awake()
    {
        base.Awake();
        // Correção: o Paladin NÃO nasce com o shield ativo — espera o 1º intervalo completo
        // (shieldRecoverInterval, 30s) antes de receber a passiva pela primeira vez, igual a
        // qualquer recarga normal depois que o shield quebra (ver Update() abaixo).
        shieldActive = false;
        shieldCurrentHealth = 0f;
        shieldCooldownRemaining = shieldRecoverInterval;
    }

    protected override void Update()
    {
        if (!GameplayGate.IsActive) return;
        base.Update();

        if (isAttacking)
        {
            actionElapsed += Time.deltaTime;
            if (actionElapsed >= maxActionDuration)
            {
                Debug.LogWarning("[Paladin] Animation Event de fim do martelo nunca chegou — forçando fim (verifique o Animator Controller).");
                isAttacking = false;
            }
        }

        if (isUsingSecondaryAbility)
        {
            shieldBashElapsed += Time.deltaTime;
            if (shieldBashElapsed >= maxShieldBashDuration)
            {
                Debug.LogWarning("[Paladin] Animation Event de fim do shield bash nunca chegou — forçando fim (verifique o Animator Controller).");
                isUsingSecondaryAbility = false;
            }
        }

        if (!shieldActive && shieldCooldownRemaining > 0f)
        {
            shieldCooldownRemaining -= Time.deltaTime;
            if (shieldCooldownRemaining <= 0f) ActivateShield();
        }
    }

    // ===================== Primário =====================

    protected override void PrimaryAttack()
    {
        isAttacking = true;
        actionElapsed = 0f;
        hammerThrowFired = false;
        AnimatorTrigger("AttackTrigger");
    }

    // Animation Event, no frame exato em que o martelo é solto da mão.
    public void AnimationHammerThrowEvent()
    {
        if (hammerThrowFired) return;
        hammerThrowFired = true;

        if (hammerProjectilePrefab == null) return;

        GetHammerAngles(hammerCount, RawAimDirection, hammerAngles);
        foreach (float angle in hammerAngles)
        {
            Vector2 dir = RotateDegrees(RawAimDirection, angle);
            var obj = Instantiate(hammerProjectilePrefab, transform.position, Quaternion.identity);
            var hammer = obj.GetComponent<PaladinHammer>();
            // Dano cheio por martelo, sem dividir reserva — mesmo critério dos 8 projéteis da
            // Ultimate do Barbarian (mais martelos = mais dano total, não o mesmo dano mais fraco).
            if (hammer != null) hammer.Launch(dir, stats.damage, enemyLayerMask, hammerSpeed, hammerMaxRange, hammerExplosionRadius);
        }
    }

    // Leque de ângulos a partir da direção da mira. 1 = só 0° (reto). A partir de 2, abre em
    // pares simétricos (±15°, ±30°...); contagem par (2, 4) sobra 1 martelo "solteiro" nesse
    // par, resolvido pra cima ou pra baixo conforme o quadrante da mira atual (y>=0 → cima,
    // como NE; y<0 → baixo, como SE) — pedido explícito do usuário.
    private static void GetHammerAngles(int count, Vector2 aim, List<float> results)
    {
        results.Clear();
        results.Add(0f);

        int fullPairs = (count - 1) / 2;
        for (int tier = 1; tier <= fullPairs; tier++)
        {
            float tierAngle = tier * 15f;
            results.Add(tierAngle);
            results.Add(-tierAngle);
        }

        if (count % 2 == 0)
        {
            float extraAngle = (fullPairs + 1) * 15f;
            results.Add(aim.y >= 0f ? extraAngle : -extraAngle);
        }
    }

    private static Vector2 RotateDegrees(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }

    // Animation Event, no fim do clipe de ataque.
    public void AnimationAttackEndEvent()
    {
        isAttacking = false;
    }

    // ===================== Ultimate =====================

    protected override void UseUltimate()
    {
        // Pose do corpo (ex.: erguer o martelo invocando as espadas) — só a flourish inicial,
        // bem mais curta que ultimateDuration. Não seta isAttacking de propósito (Seção 0,
        // item 5: Ultimate não bloqueia nada) — o Paladin continua livre pra andar/atacar
        // enquanto as espadas (PaladinOrbitingBlades, independente deste trigger) orbitam.
        // As espadas só surgem no Animation Event (ver AnimationUltimateBladesStartEvent),
        // não no clique do botão.
        AnimatorTrigger("UltimateTrigger");
    }

    // Animation Event, no meio do clipe de pose da Ultimate — é aqui que as espadas começam a surgir.
    public void AnimationUltimateBladesStartEvent()
    {
        if (orbitingBlades != null)
            orbitingBlades.Activate(ultimateDuration, stats.damage * ultimateDamageMultiplier, bladeCount, orbitSpeedDegreesPerSecond);
    }

    // Animation Event, no fim do clipe de pose da Ultimate.
    public void AnimationUltimateEndEvent() { }

    // Sem override de CanUseUltimate()/IsUltimateActive()/CancelUltimate() — Seção 0, item 5.

    // ===================== Secundária (Shift) — shield bash =====================

    protected override void UseSecondaryAbility()
    {
        shieldBashElapsed = 0f;
        shieldBashHitFired = false;
        AnimatorTrigger("SecondaryAbilityTrigger");
    }

    // Imune a dano durante a animação inteira (GDD) — reaproveita a flag que o próprio
    // HeroController já liga/desliga ao redor da Habilidade Secundária.
    protected override bool IsDamageImmune => isUsingSecondaryAbility;

    // Animation Event único — os 4 triggers disparam juntos (GDD: "simultaneamente"), não
    // escolhido pela mira como o golpe do Barbarian.
    public void AnimationShieldBashHitEvent()
    {
        if (shieldBashHitFired) return;
        shieldBashHitFired = true;

        float damage = stats.damage * shieldBashDamageMultiplier;
        ApplyShieldBashHit(shieldBashHitboxN, damage);
        ApplyShieldBashHit(shieldBashHitboxS, damage);
        ApplyShieldBashHit(shieldBashHitboxE, damage);
        ApplyShieldBashHit(shieldBashHitboxW, damage);
    }

    private void ApplyShieldBashHit(Collider2D hitbox, float damage)
    {
        if (hitbox == null) return;

        int count = hitbox.Overlap(ContactFilter2D.noFilter, OverlapBuffer);
        // Dedup por trigger isolado — Seção 0, item 6.
        EnemyController.CollectDistinct(OverlapBuffer, count, hitTargets);
        foreach (var enemy in hitTargets)
        {
            enemy.TakeDamage(damage);
            Vector2 direction = ((Vector2)enemy.transform.position - (Vector2)transform.position).normalized;
            enemy.ApplyKnockback(direction, shieldBashKnockbackForce);
        }
    }

    // Animation Event, no fim do clipe de shield bash.
    public void AnimationShieldBashEndEvent()
    {
        isUsingSecondaryAbility = false;
    }

    // ===================== Passiva — shield periódico =====================

    public override void TakeDamage(float amount)
    {
        // Absorção total enquanto o shield tiver QUALQUER vida restante — mesmo um hit que
        // sozinho exceda a vida restante do shield não vaza pro Paladin (GDD: "o shield
        // absorve o hit inteiro que o estoura, e só então quebra"). Só o PRÓXIMO hit, já sem
        // shield, volta a afetar a vida real do Paladin.
        if (shieldActive && shieldCurrentHealth > 0f)
        {
            shieldCurrentHealth -= amount;
            // Mesmo bloqueado, o jogador precisa ver que o hit aconteceu — só numa cor
            // diferente (azul bebê), sem afetar a vida real nem passar pelo HealthSystem.
            GameEvents.DamageBlocked(GetFloatingTextSpawnPosition(), amount, shieldBlockedTextColor);
            if (shieldCurrentHealth <= 0f) BreakShield();
            return;
        }

        base.TakeDamage(amount);
    }

    private void ActivateShield()
    {
        shieldActive = true;
        shieldCurrentHealth = shieldMaxHealth;
        if (shieldFrontAnimator != null) shieldFrontAnimator.Play("DomeStart");
        if (shieldBackAnimator != null) shieldBackAnimator.Play("DomeStart");
    }

    private void BreakShield()
    {
        shieldActive = false;
        shieldCurrentHealth = 0f;
        shieldCooldownRemaining = shieldRecoverInterval;
        if (shieldFrontAnimator != null) shieldFrontAnimator.Play("DomeEnd");
        if (shieldBackAnimator != null) shieldBackAnimator.Play("DomeEnd");
    }

    // Seção 0, item 9 — morte reativa o shield na hora, descartando cooldown pendente.
    protected override void OnHeroDeath()
    {
        ActivateShield();
    }
}
