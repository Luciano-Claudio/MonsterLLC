using UnityEngine;

public class RangedEnemyController : EnemyController
{
    public float projectileSpeed = 6f;
    public GameObject projectilePrefab;

    // Mesma fração já usada antes do pivô — mantém a mesma "zona de conforto" de kiting
    // sem precisar de mais um campo em EnemyStats.
    private const float MinRangeFraction = 0.8f;

    protected override void Move()
    {
        Vector2 toPlayer = player.position - transform.position;
        float distance = toPlayer.magnitude;

        if (distance > stats.attackRadius)
        {
            SetMoving(true);
            MoveInDirection(toPlayer.normalized);
        }
        else if (distance < stats.attackRadius * MinRangeFraction)
        {
            // Foge se o player chegar perto demais.
            SetMoving(true);
            MoveInDirection(-toPlayer.normalized);
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

        var projObj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        var proj = projObj.GetComponent<EnemyProjectile>() ?? projObj.AddComponent<EnemyProjectile>();
        proj.speed = projectileSpeed;
        proj.ownerFloor = ownerFloor;
        // Sem telegraph — mira a posição atual do player no instante exato em que a
        // animação de conjuração manda o Animation Event, não uma posição travada.
        proj.Launch((player.position - transform.position).normalized, stats.attackDamage);
    }
}
