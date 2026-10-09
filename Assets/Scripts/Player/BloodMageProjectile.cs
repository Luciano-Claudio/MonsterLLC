using System.Collections.Generic;
using UnityEngine;

// Primário do Blood Mage — projétil reto em 1 de 8 direções fixas, com RESERVA de dano: vai
// pro Impact assim que a reserva esgotar OU a distância máxima ser alcançada, o que vier
// primeiro (GDD Seção 17.10/13). Reserva padrão = o próprio dano (BloodMage.cs,
// damageReserveMultiplier = 1) — cada projétil vale exatamente 1 hit por padrão; só perfura
// mais de 1 monstro se esse multiplicador for upado. Sem valor de balanceamento próprio —
// tudo recebido via Launch() (convenção desde a Sprint 27b).
public class BloodMageProjectile : MonoBehaviour
{
    [SerializeField] private Animator animator; // opcional — só se houver clipe de impacto dedicado

    private Vector2 direction;
    private float hitDamage; // dano de um hit "cheio"
    private float remainingReserve;
    private LayerMask enemyLayerMask;
    private float speed;
    private float maxDistance;
    private float distanceTraveled;
    private bool impacted;
    private readonly HashSet<EnemyController> hitEnemies = new HashSet<EnemyController>();

    public void Launch(Vector2 dir, float fullHitDamage, float reserve, LayerMask layerMask, float projectileSpeed, float maxTravelDistance)
    {
        direction = dir.normalized;
        hitDamage = fullHitDamage;
        remainingReserve = reserve;
        enemyLayerMask = layerMask;
        speed = projectileSpeed;
        maxDistance = maxTravelDistance;

        // Sprite de referência nasce apontando pra "cima" (N, +Y) — mesma técnica do
        // PaladinHammer/RangerArrow/MageFireball.
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void Update()
    {
        if (!GameplayGate.IsActive) return;
        if (impacted) return;

        float step = speed * Time.deltaTime;
        transform.position += (Vector3)(direction * step);
        distanceTraveled += step;
        if (distanceTraveled >= maxDistance) Impact();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (impacted) return;
        // Mesmo filtro do PaladinHammer/RangerArrow — monstros têm hitboxes de ataque
        // Untagged (AttackHitbox_N/S/E/W) como filhos; sem isso, o projétil já "gasta" o
        // OnTriggerEnter2D contra elas (GetComponent falha, early return) antes de alcançar o
        // corpo principal de verdade, dependendo da ordem física dos contatos no frame.
        if (!other.CompareTag("Enemy")) return;

        var enemy = other.GetComponent<EnemyController>();
        if (enemy == null || hitEnemies.Contains(enemy)) return;
        hitEnemies.Add(enemy);

        float damageDealt = Mathf.Min(hitDamage, remainingReserve);
        enemy.TakeDamage(damageDealt);
        remainingReserve -= damageDealt;

        if (remainingReserve <= 0f) Impact();
    }

    private void Impact()
    {
        if (impacted) return;
        impacted = true;
        transform.rotation = Quaternion.identity;

        if (animator != null)
        {
            animator.SetTrigger("ImpactTrigger"); // destruição vem de AnimationImpactEndEvent
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Animation Event, no fim do clipe de impacto (só chamado se houver Animator dedicado).
    public void AnimationImpactEndEvent()
    {
        Destroy(gameObject);
    }
}
