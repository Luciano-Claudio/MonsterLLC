using System.Collections.Generic;
using UnityEngine;

public class Barbarian : HeroController
{
    // 4 triggers fixos (um por diagonal) — mesmo padrão do golpe direcional do Bestiário
    // (MeleeEnemyController.GetHitboxForFacing, Seção 22): o GameObject nunca vira, só a
    // animação muda, então o trigger certo depende de onde a mira apontava quando o golpe
    // começou (congelada em AimDirection enquanto isAttacking, ver HeroController).
    [Header("Ataque primário — golpe no chão (GDD Seção 17.1)")]
    [SerializeField] private Collider2D attackHitboxNE;
    [SerializeField] private Collider2D attackHitboxNW;
    [SerializeField] private Collider2D attackHitboxSE;
    [SerializeField] private Collider2D attackHitboxSW;
    [SerializeField] private GameObject groundCrackPrefab;
    [SerializeField] private float groundCrackDuration = 3f; // 🔢 ajustável
    [SerializeField] private float knockbackForce = 4f; // 🔢 ajustável

    [Header("Ultimate — salto + 8 projéteis retos")]
    [SerializeField] private GameObject ultimateProjectilePrefab; // precisa ter HeroProjectile
    [SerializeField] private float ultimateDamageMultiplier = 2f; // 🔢 GDD: "2x o dano atual da arma"
    // Controlador decide TUDO sobre o projétil e repassa pro prefab em cada Launch() — ver
    // comentário no topo do HeroProjectile.cs.
    [SerializeField] private float projectileSpeed = 10f; // 🔢 ajustável
    [SerializeField] private float projectileMaxDistance = 8f; // 🔢 ajustável
    [SerializeField] private float projectileKnockbackForce = 4f; // 🔢 ajustável

    // Habilidade Secundária (Shift) — buff temporário de dano e velocidade (GDD Seção 16/17.1,
    // Sprint 18→19). 1 animação só, sem direções, sem fases (igual "die") — não bloqueia
    // ataque/ultimate/movimento, e não é cancelável: roda até o fim sozinha.
    [Header("Habilidade Secundária (Shift) — dano e velocidade temporários")]
    [SerializeField] private float secondaryAbilityDamageMultiplier = 2f; // 🔢 ajustável
    [SerializeField] private float secondaryAbilitySpeedMultiplier = 1.5f; // 🔢 ajustável
    [SerializeField] private float secondaryAbilityDuration = 5f; // 🔢 ajustável
    private float secondaryAbilityRemaining;
    private float baseMoveSpeed;

    // Rede de segurança — se o Animation Event de fim (attack ou ultimate) nunca disparar,
    // força o fim da ação em vez de travar o herói pra sempre em "isAttacking" (mesmo
    // padrão do EnemyController pro attack/die).
    [SerializeField] private float maxActionDuration = 3f; // 🔢 ajustável
    private float actionElapsed;

    // Upgrade futuro — Barbarian literalmente cresce (escala o Transform inteiro, sprite +
    // colliders + hitboxes de ataque incluídos) até 3x o tamanho original. 1 = tamanho normal.
    [SerializeField] private float sizeMultiplier = 1f; // 🔢 upgradable — até 3x (ver OnValidate)

    // Attack e Ultimate são Blend Tree 2D Freeform Directional — pra quase qualquer ângulo
    // de mira, 2 clipes diagonais tocam misturados ao mesmo tempo, e cada clipe carrega seu
    // próprio Animation Event de hit. O Animator dispara o evento de TODO clipe com peso > 0
    // na mistura, não só do dominante — sem essa trava, um golpe/ultimate só chamava o
    // Hit/Land Event 2x (dano dobrado no golpe, 8 projéteis da ultimate saindo em dobro).
    // Mesma causa raiz já corrigida em EnemyController.AnimationHitEvent().
    private bool attackHitFired;
    private bool ultimateLandFired;
    private readonly List<EnemyController> hitTargets = new();

    private static readonly Vector2[] EightDirections =
    {
        Vector2.up,                                   // N
        new Vector2(0.7071f, 0.7071f),                 // NE
        Vector2.right,                                 // E
        new Vector2(0.7071f, -0.7071f),                // SE
        Vector2.down,                                  // S
        new Vector2(-0.7071f, -0.7071f),                // SW
        Vector2.left,                                  // W
        new Vector2(-0.7071f, 0.7071f),                // NW
    };

#if UNITY_EDITOR
    private void OnValidate()
    {
        sizeMultiplier = Mathf.Clamp(sizeMultiplier, 1f, 3f);
    }
#endif

    protected override void Awake()
    {
        base.Awake();
        baseMoveSpeed = stats.moveSpeed;
        transform.localScale = Vector3.one * sizeMultiplier;
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
                Debug.LogWarning("[Barbarian] Animation Event de fim de ação nunca chegou — forçando fim (verifique o Animator Controller).");
                isAttacking = false;
            }
        }

        if (secondaryAbilityRemaining > 0f)
        {
            secondaryAbilityRemaining -= Time.deltaTime;
            if (secondaryAbilityRemaining <= 0f)
            {
                stats.moveSpeed = baseMoveSpeed;
                isUsingSecondaryAbility = false;
            }
        }
    }

    protected override void PrimaryAttack()
    {
        if (isAttacking) return;
        isAttacking = true;
        actionElapsed = 0f;
        attackHitFired = false;
        AnimatorTrigger("AttackTrigger");
    }

    // Animation Event, no frame exato em que a espada toca o chão.
    public void AnimationAttackHitEvent()
    {
        if (attackHitFired) return;
        attackHitFired = true;

        Collider2D hitbox = GetHitboxForFacing();
        if (hitbox == null) return;

        float damage = stats.damage * GetPassiveDamageMultiplier() * GetSecondaryAbilityDamageMultiplier();

        var results = new Collider2D[16];
        int count = hitbox.Overlap(ContactFilter2D.noFilter, results);
        // Dedup obrigatório — todo monstro tem 2 Collider2D no mesmo GameObject (corpo +
        // trigger genérico), então Overlap() sem isso acertava o mesmo monstro 2x (mesmo bug
        // real já corrigido no primário do Mage — ver EnemyController.CollectDistinct()).
        EnemyController.CollectDistinct(results, count, hitTargets);
        foreach (var enemy in hitTargets)
        {
            enemy.TakeDamage(damage);
            enemy.ApplyKnockback(AimDirection, knockbackForce);
        }

        if (groundCrackPrefab != null)
        {
            var crack = Instantiate(groundCrackPrefab, hitbox.bounds.center, Quaternion.identity);
            Destroy(crack, groundCrackDuration);
        }
    }

    // Animation Event, no fim do clipe de ataque.
    public void AnimationAttackEndEvent()
    {
        isAttacking = false;
    }

    // isAttacking agora é checado por CanUseUltimate() (HeroController), antes de gastar
    // energia — não precisa mais desse "return" aqui dentro, virado tarde demais pra impedir
    // o gasto.
    protected override bool CanUseUltimate() => !isAttacking;

    protected override void UseUltimate()
    {
        isAttacking = true;
        actionElapsed = 0f;
        ultimateLandFired = false;
        AnimatorTrigger("UltimateTrigger");
    }

    // Animation Event, no frame exato em que o Barbarian cai no chão.
    public void AnimationUltimateLandEvent()
    {
        if (ultimateLandFired) return;
        ultimateLandFired = true;

        if (ultimateProjectilePrefab == null) return;

        float reserve = stats.damage * GetPassiveDamageMultiplier() * GetSecondaryAbilityDamageMultiplier() * ultimateDamageMultiplier;
        foreach (Vector2 dir in EightDirections)
        {
            var obj = Instantiate(ultimateProjectilePrefab, transform.position, Quaternion.identity);
            // Mesmo multiplicador do próprio corpo — Barbarian maior upgrada joga projéteis maiores junto.
            obj.transform.localScale = Vector3.one * sizeMultiplier;
            var projectile = obj.GetComponent<HeroProjectile>();
            if (projectile != null) projectile.Launch(dir, reserve, projectileSpeed, projectileMaxDistance, projectileKnockbackForce);
        }
    }

    // Animation Event, no fim do clipe de ultimate.
    public void AnimationUltimateEndEvent()
    {
        isAttacking = false;
    }

    // Não seta isAttacking — de propósito: essa habilidade não bloqueia nada, ataque/
    // ultimate/movimento continuam funcionando normalmente durante o buff. Não cancelável
    // (sem override de CancelSecondaryAbility) — roda até secondaryAbilityDuration acabar.
    protected override void UseSecondaryAbility()
    {
        secondaryAbilityRemaining = secondaryAbilityDuration;
        stats.moveSpeed = baseMoveSpeed * secondaryAbilitySpeedMultiplier;
        AnimatorTrigger("SecondaryAbilityTrigger");
    }

    private float GetSecondaryAbilityDamageMultiplier() =>
        secondaryAbilityRemaining > 0f ? secondaryAbilityDamageMultiplier : 1f;

    // Classifica AimDirection (congelada em isAttacking) num dos 4 quadrantes diagonais —
    // mesmo critério que o Blend Tree 2D do Animator usa pra escolher entre atk_ne/nw/se/sw.
    private Collider2D GetHitboxForFacing()
    {
        bool east = AimDirection.x >= 0f;
        bool north = AimDirection.y >= 0f;

        if (north) return east ? attackHitboxNE : attackHitboxNW;
        return east ? attackHitboxSE : attackHitboxSW;
    }

    // GDD Seção 17.1 — passiva: a cada 10% de Vida Máxima perdida, +25% de dano, até +200%
    // (3x total) ao perder 80% ou mais.
    private int GetPassiveDamageSteps()
    {
        float hpLostPercent = (1f - stats.health / stats.maxHealth) * 100f;
        return Mathf.Clamp(Mathf.FloorToInt(hpLostPercent / 10f), 0, 8);
    }

    private float GetPassiveDamageMultiplier() => 1f + GetPassiveDamageSteps() * 0.25f;
}
