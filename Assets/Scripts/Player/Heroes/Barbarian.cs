using UnityEngine;

public class Barbarian : HeroController
{
    public float attackRadius = 2f;
    public int ultimateBurstCount = 8;
    public float ultimateBurstRadius = 3f;

    protected override void PrimaryAttack()
    {
        Debug.Log("[Barbarian] Golpe frontal em área.");
        Vector2 attackCenter = (Vector2)transform.position + AimDirection * attackRadius;
        Collider2D[] hits = Physics2D.OverlapCircleAll(attackCenter, attackRadius);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Enemy"))
                Debug.Log($"[Barbarian] Atingiu {hit.name} (dano placeholder: {stats.damage})");
        }
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
