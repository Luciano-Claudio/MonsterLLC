using UnityEngine;

public class MeleeEnemyController : EnemyController
{
    protected override AttackType AttackType => AttackType.Melee;

    protected override void Move()
    {
        Vector2 direction = (player.position - transform.position).normalized;
        transform.Translate(direction * stats.moveSpeed * Time.deltaTime);
        SetMoveDirection(direction);
    }

    protected override void ExecuteHit()
    {
        var hero = player.GetComponent<HeroController>();
        if (hero == null) return;

        // GDD Seção 22: o alvo travou no início do Telegraph — se o player se afastou
        // o suficiente durante a janela de reação, o golpe erra (sem dano, sem log).
        float distanceAtLock = Vector2.Distance(lockedTargetPosition, transform.position);
        if (distanceAtLock > stats.attackRadius) return;

        Debug.Log($"[MeleeEnemyController] Ataca o herói por {stats.damage}.");
        hero.TakeDamage(stats.damage);
    }
}
