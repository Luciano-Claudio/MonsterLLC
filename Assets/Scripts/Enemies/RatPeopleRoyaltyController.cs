using UnityEngine;

// Rat People Royalty (Bestiário — exceção pontual, Melee/Ranged híbrido, mesmo padrão
// arquitetural da Spider Queen). 2 raios de ataque independentes, igual todo Melee/Ranged
// comum já tem o próprio attackRadius: dentro de stats.attackRadius (melee) ataca corpo a
// corpo de verdade (attack, 4 direções diagonais, hitbox + AnimationMeleeHitEvent) —
// substitui o antigo dano de contato passivo; entre o attackRadius e o rangedAttackRadius,
// arremessa (Throw_Rat, 4 direções diagonais); além do rangedAttackRadius, nenhum dos dois
// alcança — só persegue. Igual à Spider Queen, nunca foge: sempre tenta voltar a fechar
// distância pro melee. Não usa o pipeline genérico de Attack/AttackTrigger da base
// (hasAttackAnimation deve ficar FALSE no Inspector) — melee e arremesso são tratados
// inteiramente em Move(), cada um com seu próprio trigger/cooldown/evento.
public class RatPeopleRoyaltyController : EnemyController
{
    [Header("Ataque corpo a corpo (4 direções diagonais)")]
    [SerializeField] private Collider2D attackHitboxNE;
    [SerializeField] private Collider2D attackHitboxNW;
    [SerializeField] private Collider2D attackHitboxSE;
    [SerializeField] private Collider2D attackHitboxSW;
    [SerializeField] private float meleeCooldownDuration = 1.5f; // 🔢 ajustável

    [Header("Arremesso de Rat People (Bestiário — exceção pontual)")]
    [SerializeField] private GameObject throwProjectilePrefab; // precisa ter RatPeopleThrowProjectile
    [SerializeField] private float throwCooldownDuration = 5f; // 🔢 ajustável
    [SerializeField] private float rangedAttackRadius = 6f; // 🔢 ajustável — além disso, nem o arremesso alcança, só persegue

    private AttackCooldown meleeCooldown;
    private AttackCooldown throwCooldown;
    private bool isAttacking; // melee em andamento
    private bool isThrowing; // arremesso em andamento

    protected override void Awake()
    {
        base.Awake();
        meleeCooldown = new AttackCooldown(meleeCooldownDuration);
        throwCooldown = new AttackCooldown(throwCooldownDuration);
    }

    protected override void Move()
    {
        // Comprometido com melee ou arremesso — não se move nem reinicia outra ação no meio
        // dela (mesmo critério de "ação real" usado em toda outra classe do projeto).
        if (isAttacking || isThrowing)
        {
            SetMoving(false);
            return;
        }

        Vector2 toPlayer = player.position - transform.position;
        float distance = toPlayer.magnitude;

        if (distance <= stats.attackRadius)
        {
            SetMoving(false); // colado no player -> idle_combat, tenta melee pelo cooldown próprio
            meleeCooldown.Tick(Time.deltaTime);
            if (meleeCooldown.TryConsume())
            {
                StartMeleeAttack();
                return; // evita iniciar melee e arremesso no mesmo frame (1 trigger por vez)
            }
        }
        else
        {
            SetMoving(true); // SEMPRE aproxima (nunca foge) — tenta voltar pro melee
            MoveInDirection(toPlayer.normalized);

            // Só tenta o arremesso se também estiver dentro do alcance dele — igual um Ranged
            // comum, além do rangedAttackRadius nem esse alcançaria.
            if (distance <= rangedAttackRadius)
            {
                throwCooldown.Tick(Time.deltaTime);
                if (throwCooldown.TryConsume()) StartThrow();
            }
        }
    }

    private void StartMeleeAttack()
    {
        isAttacking = true;
        SetMoving(false);
        if (animator != null) animator.SetTrigger("AttackTrigger");
    }

    private void StartThrow()
    {
        isThrowing = true;
        SetMoving(false);
        if (animator != null) animator.SetTrigger("ThrowTrigger");
    }

    // Animation Event, no frame exato em que o golpe conecta (clipes attack_se/sw/ne/nw).
    public void AnimationMeleeHitEvent()
    {
        var hitbox = GetHitboxForFacing();
        if (hitbox == null) return;

        var heroCollider = player.GetComponent<Collider2D>();
        if (heroCollider == null) return;
        if (!hitbox.IsTouching(heroCollider)) return; // player fora do trigger nesse frame exato -> o golpe erra

        var hero = player.GetComponent<HeroController>();
        if (hero != null) hero.TakeDamage(stats.attackDamage);
    }

    // Animation Event, no fim do clipe de ataque corpo a corpo.
    public void AnimationMeleeEndEvent()
    {
        isAttacking = false;
    }

    // Animation Event, no frame exato do arremesso (clipes Throw_Rat_se/sw/ne/nw).
    public void AnimationThrowEvent()
    {
        if (throwProjectilePrefab == null || player == null) return;

        var obj = Instantiate(throwProjectilePrefab, transform.position, Quaternion.identity);
        var proj = obj.GetComponent<RatPeopleThrowProjectile>();
        if (proj != null) proj.Launch((player.position - transform.position).normalized, stats.attackDamage, ownerFloor);
    }

    // Animation Event, no fim do clipe de arremesso.
    public void AnimationThrowEndEvent()
    {
        isThrowing = false;
    }

    // Classifica AimDirection (sempre em direção ao player) num dos 4 quadrantes diagonais —
    // mesmo critério do MeleeEnemyController, pra escolher a hitbox certa do golpe.
    private Collider2D GetHitboxForFacing()
    {
        bool east = AimDirection.x >= 0f;
        bool north = AimDirection.y >= 0f;

        if (north) return east ? attackHitboxNE : attackHitboxNW;
        return east ? attackHitboxSE : attackHitboxSW;
    }

    // Pipeline genérico de Attack nunca é usado — melee e arremesso são tratados inteiramente
    // em Move()/StartMeleeAttack()/StartThrow() acima, igual ao Goblin Sapper.
    protected override bool InAttackRange() => false;
    protected override void ExecuteAttackHit() { }
}
