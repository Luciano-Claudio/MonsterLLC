using UnityEngine;

// Slug (Bestiário — summon do Bicephalous, Andar 9): nasce quando o projétil do Bicephalous
// atinge o player ou o limite da trajetória ("caso 4" da taxonomia de projéteis — o próprio
// projétil vira o monstro, ver EnemyProjectile.spawnsMonsterOnImpact). Tem 2 fases, não dá
// pra usar MeleeEnemyController NEM RangedEnemyController prontos porque muda de uma pra
// outra em tempo real — por isso implementa as duas, direto, estendendo EnemyController:
//
// Fase pequena (igual a um Slime — ver SlimeEnemyController): dano de contato passivo via
// EnemyContactDamage, sem animação de ataque real (hasAttackAnimation=false). Enquanto isso,
// um timer próprio (growDuration) conta — se o Slug não morrer antes dele estourar, toca
// Grow (Animation Event no último frame chama AnimationGrowEndEvent) e vira a fase crescida.
//
// Fase crescida: vira um Ranged de verdade (hasAttackAnimation=true a partir daqui), com
// sprite diferente (pasta Grow), e se comporta exatamente como o Bicephalous — mesmo
// projétil (que também pode virar outro Slug no impacto), só que com um pouco menos de dano.
public class SlugController : EnemyController
{
    [Header("Fase pequena (contato, igual a um Slime)")]
    [SerializeField] private Collider2D contactHitbox;

    [Header("Transformação (Grow) — serializado pra poder encurtar em teste")]
    [SerializeField] private float growDuration = 10f; // 🔢 ajustável (testes podem usar um valor bem menor)

    [Header("Fase crescida (igual ao Bicephalous)")]
    [SerializeField] private float projectileSpeed = 6f;
    [SerializeField] private GameObject projectilePrefab;
    // stats.attackDamage continua sendo o dano de CONTATO da fase pequena (igual ao Slime) —
    // a fase crescida usa esse campo à parte porque o Bestiário pede um dano diferente
    // (um pouco menor que o do Bicephalous) pro ataque à distância, não o mesmo número.
    [SerializeField] private float grownAttackDamage = 5f;

    private const float MinRangeFraction = 0.8f; // mesma fração do RangedEnemyController

    private float growElapsed;
    private bool isGrown;
    private bool isGrowing;

    protected override void Awake()
    {
        base.Awake();
        hasAttackAnimation = false; // fase pequena nunca usa o fluxo de ataque animado (só contato)

        var contactDamage = GetComponent<EnemyContactDamage>();
        if (contactDamage != null) contactDamage.Initialize(stats.attackDamage, stats.attackAnimationCooldown);
    }

    protected override void Update()
    {
        base.Update();

        if (isGrown || isGrowing || IsDead || !GameplayGate.IsActive) return;

        growElapsed += Time.deltaTime;
        if (growElapsed < growDuration) return;

        isGrowing = true;
        SetMoving(false);
        if (animator != null) animator.SetTrigger("GrowTrigger");
    }

    // Animation Event, último frame do Grow — a partir daqui o Slug passa a se comportar
    // como o Bicephalous (Ranged de verdade).
    public void AnimationGrowEndEvent()
    {
        isGrowing = false;
        isGrown = true;
        hasAttackAnimation = true;

        if (contactHitbox != null) contactHitbox.enabled = false;
        var contactDamage = GetComponent<EnemyContactDamage>();
        if (contactDamage != null) contactDamage.enabled = false;

        if (animator != null) animator.SetBool("IsGrown", true);
    }

    protected override void Move(float deltaTime)
    {
        if (isGrowing) return; // comprometido com a animação de crescer, não se move

        if (!isGrown)
        {
            // Fase pequena — igual ao SlimeEnemyController: persegue direto, para ao encostar.
            Vector2 toPlayer = player.position - transform.position;
            if (IsPlayerTouchingContactHitbox())
            {
                SetMoving(false);
                return;
            }
            SetMoving(true);
            MoveInDirection(toPlayer.normalized);
            return;
        }

        // Fase crescida — igual ao RangedEnemyController: mantém distância, foge se o player
        // chegar perto demais.
        Vector2 toPlayerGrown = player.position - transform.position;
        float distance = toPlayerGrown.magnitude;

        if (distance > stats.attackRadius)
        {
            SetMoving(true);
            MoveInDirection(toPlayerGrown.normalized);
        }
        else if (distance < stats.attackRadius * MinRangeFraction)
        {
            SetMoving(true);
            MoveInDirection(-toPlayerGrown.normalized);
        }
        else
        {
            SetMoving(false);
        }
    }

    private bool IsPlayerTouchingContactHitbox()
    {
        if (contactHitbox == null) return false;
        var heroCollider = player.GetComponent<Collider2D>();
        return heroCollider != null && contactHitbox.IsTouching(heroCollider);
    }

    protected override bool InAttackRange()
    {
        if (!isGrown) return false; // fase pequena nunca usa esse fluxo (hasAttackAnimation=false)
        return Vector2.Distance(transform.position, player.position) <= stats.attackRadius;
    }

    protected override void ExecuteAttackHit()
    {
        if (!isGrown) return; // nunca deveria ser chamado aqui, mas sem efeito se for

        if (projectilePrefab == null)
        {
            var heroFallback = player.GetComponent<HeroController>();
            if (heroFallback != null) heroFallback.TakeDamage(grownAttackDamage);
            return;
        }

        var projObj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        var proj = projObj.GetComponent<EnemyProjectile>() ?? projObj.AddComponent<EnemyProjectile>();
        proj.speed = projectileSpeed;
        Vector2 toPlayer = player.position - transform.position;
        proj.Launch(toPlayer.normalized, grownAttackDamage, toPlayer.magnitude);
    }
}
