using UnityEngine;

public class RangedEnemyController : EnemyController
{
    public float projectileSpeed = 6f;
    public GameObject projectilePrefab;

    // Mesma fração já usada antes do pivô — mantém a mesma "zona de conforto" de kiting
    // sem precisar de mais um campo em EnemyStats.
    private const float MinRangeFraction = 0.8f;

    // 2026-11-01 — Deadline 8.1, IA de perseguição inteligente. O ponto-alvo
    // (GetSmartApproachPoint) já nasce a stats.attackRadius*MinRangeFraction de distância da
    // posição PREVISTA do player, no mesmo lado em que este monstro já está — se o player se
    // afastar, o ponto se afasta junto (persegue); se o player chegar perto demais, o ponto
    // fica "atrás" dele relativo ao monstro (foge). Mesma conta resolve os 2 casos antigos
    // (aproximar/fugir) sem precisar de 2 branches — e já antecipa a posição em vez de só
    // reagir à distância atual.
    protected override void Move(float deltaTime)
    {
        Vector2 approachPoint = GetSmartApproachPoint(stats.attackRadius * MinRangeFraction);
        Vector2 toTarget = approachPoint - (Vector2)transform.position;

        if (toTarget.magnitude > 0.3f) // 🔢 zona morta perto do ponto ideal — evita tremer parado
        {
            SetMoving(true);
            MoveInDirection(ApplyPackCohesion(toTarget.normalized));
        }
        else
        {
            SetMoving(false); // dentro do alcance ideal -> idle_combat, enquanto o cooldown não libera
        }
    }

    protected override bool InAttackRange() =>
        Vector2.Distance(transform.position, player.position) <= stats.attackRadius;

    protected override void ExecuteAttackHit()
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning("[RangedEnemyController] Sem projectilePrefab — dano aplicado direto como fallback.");
            var heroFallback = player.GetComponent<HeroController>();
            if (heroFallback != null) heroFallback.TakeDamage(stats.attackDamage);
            return;
        }

        var projObj = Instantiate(projectilePrefab, transform.position, Quaternion.identity, EnemyController.ContainerFor(IsBoss));
        var proj = projObj.GetComponent<EnemyProjectile>() ?? projObj.AddComponent<EnemyProjectile>();
        proj.speed = projectileSpeed;
        // Sem telegraph — mira a posição atual do player no instante exato em que a
        // animação de conjuração manda o Animation Event, não uma posição travada. Passa a
        // distância real pro Launch() garantir alcance suficiente (ver comentário lá) — o
        // player pode ter se afastado desde o InAttackRange() que autorizou este ataque.
        Vector2 toPlayer = player.position - transform.position;
        proj.Launch(toPlayer.normalized, stats.attackDamage, toPlayer.magnitude, IsBoss);
    }
}
