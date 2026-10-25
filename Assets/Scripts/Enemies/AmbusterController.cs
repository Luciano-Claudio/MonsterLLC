using UnityEngine;

// Ambuster (Bestiário Andar 9, boss): o ataque dele é totalmente customizado, não dá pra
// herdar nem de MeleeEnemyController nem de RangedEnemyController — por isso estende
// EnemyController direto, igual ao SlugController.
//
// Ciclo do ataque (mesma técnica do teleporte do Mage — Shift/MageTeleportProjectile,
// lado do herói):
// 1. attack_start (4 direções) — o próprio Ambuster "se preparando", parado. Animation
//    Event no ÚLTIMO frame (AnimationAttackStartEndEvent) coleta a posição ATUAL do player
//    (snapshot, não recalculada depois — se o player andar, o projétil erra), desliga a
//    sprite do Ambuster (ele "vira" o projétil) e nasce um AmbusterProjectile de verdade —
//    um GameObject à parte, não uma animação — que viaja sozinho até aquela posição.
// 2. Enquanto o projétil viaja, o Ambuster fica invisível e parado no estado "Projectile"
//    do Animator (placeholder, não representa o voo — quem воa de verdade é o
//    AmbusterProjectile). Se o projétil tocar o player no meio do caminho, aplica metade
//    do dano normal e já pousa ALI (não continua até a posição original).
// 3. Quando o projétil chega (OnProjectileArrived, callback dele) — o Ambuster reaparece
//    exatamente onde o projétil parou, religa a sprite e toca attack_end (4 direções): 3
//    Animation Events de dano em área ao redor da posição atual (o 1º já no frame 0 —
//    "o dano começa assim que o attack_end começa"), mais o AnimationAttackEndEvent padrão
//    no último frame fechando o golpe.
public class AmbusterController : EnemyController
{
    private const float MinRangeFraction = 0.8f; // mesma fração do RangedEnemyController

    [Header("Projétil (GameObject à parte — mesma técnica do teleporte do Mage)")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float projectileSpeed = 10f; // 🔢 ajustável
    [SerializeField] private float projectileTouchRadius = 0.6f; // 🔢 ajustável — alcance de toque durante o voo
    [SerializeField] private SpriteRenderer ambusterSpriteRenderer; // desligada enquanto o projétil viaja

    // Triggers de ataque (um por direção diagonal) — mesma técnica do MeleeEnemyController
    // (attackHitboxNE/NW/SE/SW + GetHitboxForFacing), só que aqui os 3 hits do attack_end
    // testam IsTouching() direto, sem passar pelo fluxo de AnimationHitEvent/ExecuteAttackHit
    // (que no Ambuster nunca dispara de verdade). Pedido explícito do usuário: capsules, não
    // OverlapCircle por código — assim o local/forma do dano fica 100% ajustável no Editor
    // (com gizmo de verdade), igual a todo resto do bestiário.
    [Header("Triggers do attack_end (1 capsule por direção diagonal)")]
    [SerializeField] private Collider2D attackHitboxNE;
    [SerializeField] private Collider2D attackHitboxNW;
    [SerializeField] private Collider2D attackHitboxSE;
    [SerializeField] private Collider2D attackHitboxSW;

    private bool isFlying;

    protected override void Move(float deltaTime)
    {
        if (isFlying) return; // o Ambuster fica parado/invisível nessa fase

        Vector2 toPlayer = player.position - transform.position;
        float distance = toPlayer.magnitude;

        if (distance > stats.attackRadius)
        {
            SetMoving(true);
            MoveInDirection(toPlayer.normalized);
        }
        else if (distance < stats.attackRadius * MinRangeFraction)
        {
            SetMoving(true);
            MoveInDirection(-toPlayer.normalized);
        }
        else
        {
            SetMoving(false);
        }
    }

    protected override bool InAttackRange() =>
        Vector2.Distance(transform.position, player.position) <= stats.attackRadius;

    // Nunca chamado de verdade — o attack_start não tem AnimationHitEvent nenhum, o golpe
    // real é todo orientado pelos eventos próprios abaixo.
    protected override void ExecuteAttackHit() { }

    // Animation Event, último frame do attack_start.
    public void AnimationAttackStartEndEvent()
    {
        if (player == null) return;

        isFlying = true;
        if (ambusterSpriteRenderer != null) ambusterSpriteRenderer.enabled = false;

        if (projectilePrefab == null)
        {
            OnProjectileArrived(transform.position); // rede de segurança — sem prefab, "chega" na hora
            return;
        }

        var obj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        var projectile = obj.GetComponent<AmbusterProjectile>();
        if (projectile != null)
        {
            projectile.Launch(player.position, projectileSpeed, stats.attackDamage * 0.5f, projectileTouchRadius, player, OnProjectileArrived);
        }
        else
        {
            Destroy(obj);
            OnProjectileArrived(transform.position);
        }
    }

    // Callback do AmbusterProjectile — chamado no instante exato em que ele chega (seja no
    // alvo original, seja onde acertou o player no meio do caminho).
    private void OnProjectileArrived(Vector3 landingPosition)
    {
        isFlying = false;
        if (ai != null) ai.Teleport(landingPosition);
        else transform.position = landingPosition;
        if (ambusterSpriteRenderer != null) ambusterSpriteRenderer.enabled = true;
        if (animator != null) animator.SetTrigger("LandTrigger");
    }

    // Animation Event, 3x dentro do attack_end (1º já no frame 0) — testa só o trigger da
    // direção atual (GetHitboxForFacing), igual ao golpe real do MeleeEnemyController.
    public void AnimationAreaHitEvent()
    {
        var hitbox = GetHitboxForFacing();
        if (hitbox == null) return;

        var heroCollider = player.GetComponent<Collider2D>();
        if (heroCollider == null) return;
        if (!hitbox.IsTouching(heroCollider)) return;

        var hero = player.GetComponent<HeroController>();
        if (hero != null) hero.TakeDamage(stats.attackDamage);
    }

    // Mesmo critério de quadrante diagonal do MeleeEnemyController.GetHitboxForFacing()
    // (sem variante ortogonal — o attack_end do Ambuster é só diagonal mesmo).
    private Collider2D GetHitboxForFacing()
    {
        bool east = AimDirection.x >= 0f;
        bool north = AimDirection.y >= 0f;

        if (north) return east ? attackHitboxNE : attackHitboxNW;
        return east ? attackHitboxSE : attackHitboxSW;
    }
}
