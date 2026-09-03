using UnityEngine;

public class Barbarian : HeroController
{
    public float attackRadius = 2f;
    public int ultimateBurstCount = 8;
    public float ultimateBurstRadius = 3f;

    private Vector2 AttackCenter => (Vector2)transform.position + AimDirection * attackRadius;

    protected override void PrimaryAttack()
    {
        Debug.Log("[Barbarian] Golpe frontal em área.");
        Collider2D[] hits = Physics2D.OverlapCircleAll(AttackCenter, attackRadius);
        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Enemy")) continue;
            var enemy = hit.GetComponent<EnemyController>();
            if (enemy != null) enemy.TakeDamage(stats.damage);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(AttackCenter, attackRadius);
    }

    protected override void UseUltimate()
    {
        Debug.Log("[Barbarian] Ultimate — salto + rajada em área.");
        for (int i = 0; i < ultimateBurstCount; i++)
        {
            float angle = i * (360f / ultimateBurstCount) * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            Debug.DrawRay(transform.position, dir * ultimateBurstRadius, Color.red, 1f);
        }
    }
}
