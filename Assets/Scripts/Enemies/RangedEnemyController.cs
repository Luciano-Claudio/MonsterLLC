using UnityEngine;

public class RangedEnemyController : EnemyController
{
    public float projectileSpeed = 6f;
    public GameObject projectilePrefab;

    protected override AttackType AttackType => AttackType.Ranged;

    protected override void Move()
    {
        Vector2 toPlayer = player.position - transform.position;

        // Mantém distância: se está muito perto, afasta; senão, se aproxima até o raio de ataque.
        Vector2 dir = toPlayer.magnitude < stats.attackRadius * 0.8f ? -toPlayer.normalized : toPlayer.normalized;
        transform.Translate(dir * stats.moveSpeed * Time.deltaTime);
        SetMoveDirection(dir);
    }

    protected override void ExecuteHit()
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning("[RangedEnemyController] Sem projectilePrefab — dano aplicado direto como fallback.");
            player.GetComponent<HeroController>()?.TakeDamage(stats.damage);
            return;
        }

        var projObj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        var proj = projObj.GetComponent<EnemyProjectile>() ?? projObj.AddComponent<EnemyProjectile>();
        // GDD Seção 22: mira a posição travada no início do Telegraph, não a posição
        // atual do player no instante do disparo — senão a esquiva vira impossível.
        proj.direction = (lockedTargetPosition - (Vector2)transform.position).normalized;
        proj.speed = projectileSpeed;
        proj.damage = stats.damage;
        proj.ownerFloor = ownerFloor;
    }
}
