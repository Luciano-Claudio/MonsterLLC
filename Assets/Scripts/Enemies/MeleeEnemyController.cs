using UnityEngine;

public class MeleeEnemyController : EnemyController
{
    protected override void Move()
    {
        Vector2 direction = (player.position - transform.position).normalized;
        transform.Translate(direction * stats.moveSpeed * Time.deltaTime);
    }

    protected override void ExecuteHit()
    {
        var hero = player.GetComponent<HeroController>();
        if (hero == null) return;
        Debug.Log($"[MeleeEnemyController] Ataca o herói por {stats.damage}.");
        hero.TakeDamage(stats.damage);
    }
}
