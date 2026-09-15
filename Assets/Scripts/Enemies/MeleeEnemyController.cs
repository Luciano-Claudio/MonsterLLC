using UnityEngine;

public class MeleeEnemyController : EnemyController
{
    // 4 clipes de ataque diagonais (NE/NW/SE/SW), cada um com seu próprio trigger filho
    // fixo naquela direção — o GameObject nunca vira, só a animação muda, então o trigger
    // certo depende de pra que lado o monstro estava olhando quando o ataque começou.
    // Ficam sem uso se hasAttackAnimation for falso (ex.: Slimes).
    [Header("Triggers de ataque (um por direção diagonal)")]
    [SerializeField] private Collider2D attackHitboxNE;
    [SerializeField] private Collider2D attackHitboxNW;
    [SerializeField] private Collider2D attackHitboxSE;
    [SerializeField] private Collider2D attackHitboxSW;

    // Enquanto a "vaga" perto do player estiver cheia (MeleeAttackSlotManager), o monstro
    // não fecha até o attackRadius — fica flanqueando no flankRadius, alternando andar na
    // borda do círculo e idle_combat. Reaproveita o PatrolAI (mesma lógica de alternância
    // com timers aleatórios) só que com o alvo sempre recalculado num ponto do anel, não
    // num ponto fixo de patrulha.
    [Header("Flanco (quando a vaga perto do player está cheia)")]
    [SerializeField] private float minFlankIdleDuration = 1f;
    [SerializeField] private float maxFlankIdleDuration = 2.5f;
    [SerializeField] private float minFlankWalkDuration = 1f;
    [SerializeField] private float maxFlankWalkDuration = 2f;
    [SerializeField] private float flankArcDegrees = 60f; // o quanto o próximo ponto do anel pode variar em relação ao atual

    private PatrolAI flankAI;
    private bool hasAttackSlot;
    private float flankAngle;
    private bool flankAngleInitialized;

    protected override void Awake()
    {
        base.Awake();
        flankAI = new PatrolAI(minFlankIdleDuration, maxFlankIdleDuration, minFlankWalkDuration, maxFlankWalkDuration);
    }

    protected override void Move()
    {
        Vector2 toPlayer = player.position - transform.position;
        float distance = toPlayer.magnitude;

        if (distance <= stats.attackRadius)
        {
            SetMoving(false); // colado no player -> idle_combat, enquanto os cooldowns não liberam
            return;
        }

        if (hasAttackSlot || distance > stats.flankRadius)
        {
            // já garantiu vaga, ou ainda está longe demais até pra flanquear -> aproxima normalmente
            SetMoving(true);
            MoveInDirection(toPlayer.normalized);
            return;
        }

        // chegou no anel de flanco sem vaga garantida — tenta pegar uma agora
        if (MeleeAttackSlotManager.Instance == null || MeleeAttackSlotManager.Instance.TryReserveSlot())
        {
            hasAttackSlot = true;
            SetMoving(true);
            MoveInDirection(toPlayer.normalized);
            return;
        }

        UpdateFlanking(toPlayer);
    }

    private void UpdateFlanking(Vector2 toPlayer)
    {
        if (!flankAngleInitialized)
        {
            // começa a orbitar a partir de onde já estava em relação ao player, sem pular
            flankAngle = Mathf.Atan2(-toPlayer.y, -toPlayer.x);
            flankAngleInitialized = true;
        }

        flankAI.Tick(Time.deltaTime, PickNextFlankPoint);

        bool orbiting = flankAI.CurrentPhase == PatrolPhase.Walking;
        SetMoving(orbiting);
        if (orbiting) MoveInDirection((flankAI.WalkTarget - (Vector2)transform.position).normalized);

        // tenta de novo a cada frame enquanto flanqueia — barato (comparação de int) e
        // garante que a vaga seja ocupada assim que alguém morrer/liberar, sem atraso.
        if (MeleeAttackSlotManager.Instance == null || MeleeAttackSlotManager.Instance.TryReserveSlot())
            hasAttackSlot = true;
    }

    private Vector2 PickNextFlankPoint()
    {
        float arcRad = flankArcDegrees * Mathf.Deg2Rad;
        flankAngle += Random.Range(-arcRad, arcRad);
        Vector2 offset = new Vector2(Mathf.Cos(flankAngle), Mathf.Sin(flankAngle)) * stats.flankRadius;
        return (Vector2)player.position + offset;
    }

    protected override bool InAttackRange() =>
        Vector2.Distance(transform.position, player.position) <= stats.attackRadius;

    protected override void ExecuteAttackHit()
    {
        var hitbox = GetHitboxForFacing();
        if (hitbox == null) return;

        var heroCollider = player.GetComponent<Collider2D>();
        if (heroCollider == null) return;
        if (!hitbox.IsTouching(heroCollider)) return; // player fora do trigger nesse frame exato -> o golpe erra

        var hero = player.GetComponent<HeroController>();
        if (hero == null) return;

        Debug.Log($"[MeleeEnemyController] Golpe real conecta: {stats.attackDamage}.");
        hero.TakeDamage(stats.attackDamage);
    }

    // Classifica AimDirection (sempre em direção ao player) num dos 4 quadrantes
    // diagonais — mesmo critério que o Blend Tree 2D Freeform Directional usa pra
    // escolher entre atk_ne/nw/se/sw.
    private Collider2D GetHitboxForFacing()
    {
        bool east = AimDirection.x >= 0f;
        bool north = AimDirection.y >= 0f;

        if (north) return east ? attackHitboxNE : attackHitboxNW;
        return east ? attackHitboxSE : attackHitboxSW;
    }

    protected override void Die()
    {
        if (hasAttackSlot && MeleeAttackSlotManager.Instance != null)
            MeleeAttackSlotManager.Instance.ReleaseSlot();

        base.Die();
    }
}
