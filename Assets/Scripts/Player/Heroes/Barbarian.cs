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

    // Objeto filho, ativo enquanto a passiva estiver valendo alguma coisa. GDD Seção 33
    // (Efeitos Nocivos): passiva de herói nunca é "Efeito" e sempre renderiza NA FRENTE de
    // qualquer Efeito Nocivo (status de monstro sobre o herói) — o Sorting Layer/Order in
    // Layer desse GameObject precisa ficar numericamente acima do que a camada de Efeito
    // vier a usar quando esse sistema for implementado.
    [Header("Passiva — mais dano com vida perdida (GDD Seção 17.1)")]
    [SerializeField] private GameObject passiveGlowEffect;

    // Rede de segurança — se o Animation Event de fim (attack ou ultimate) nunca disparar,
    // força o fim da ação em vez de travar o herói pra sempre em "isAttacking" (mesmo
    // padrão do EnemyController pro attack/die).
    [SerializeField] private float maxActionDuration = 3f; // 🔢 ajustável
    private float actionElapsed;

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

        UpdatePassiveGlow();
    }

    protected override void PrimaryAttack()
    {
        if (isAttacking) return;
        isAttacking = true;
        actionElapsed = 0f;
        AnimatorTrigger("AttackTrigger");
    }

    // Animation Event, no frame exato em que a espada toca o chão.
    public void AnimationAttackHitEvent()
    {
        Collider2D hitbox = GetHitboxForFacing();
        if (hitbox == null) return;

        float damage = stats.damage * GetPassiveDamageMultiplier();

        var results = new Collider2D[16];
        int count = hitbox.Overlap(ContactFilter2D.noFilter, results);
        for (int i = 0; i < count; i++)
        {
            if (!results[i].CompareTag("Enemy")) continue;
            var enemy = results[i].GetComponent<EnemyController>();
            if (enemy == null) continue;

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

    protected override void UseUltimate()
    {
        if (isAttacking) return;
        isAttacking = true;
        actionElapsed = 0f;
        AnimatorTrigger("UltimateTrigger");
    }

    // Animation Event, no frame exato em que o Barbarian cai no chão.
    public void AnimationUltimateLandEvent()
    {
        if (ultimateProjectilePrefab == null) return;

        float reserve = stats.damage * GetPassiveDamageMultiplier() * ultimateDamageMultiplier;
        foreach (Vector2 dir in EightDirections)
        {
            var obj = Instantiate(ultimateProjectilePrefab, transform.position, Quaternion.identity);
            var projectile = obj.GetComponent<HeroProjectile>();
            if (projectile != null) projectile.Launch(dir, reserve);
        }
    }

    // Animation Event, no fim do clipe de ultimate.
    public void AnimationUltimateEndEvent()
    {
        isAttacking = false;
    }

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

    private void UpdatePassiveGlow()
    {
        if (passiveGlowEffect == null) return;
        passiveGlowEffect.SetActive(GetPassiveDamageSteps() > 0);
    }
}
