using System.Collections.Generic;
using UnityEngine;

// Martelo arremessado (GDD Seção 17.7) — projétil reto na direção exata da mira (sem snap de
// 8 direções, diferente do HeroProjectile), sprite nasce apontando pro Norte e gira pra mira
// real (mesma técnica do RogueBomb/MageFireball/ClericProjectile). Pedido do usuário: SEM
// Animation Events neste projétil — tanto o dano em área quanto a destruição no fim do clipe
// de impacto são decididos em código, por tempo/estado, não por evento disparado pelo clipe.
// 2 dados de dano: o acerto direto (único, só o monstro realmente tocado em voo) e a explosão
// no impacto (área, sempre metade do dano recebido em Launch — não é o que "sobrou" de nada).
//
// speed/maxRange/explosionRadius NÃO são serializados aqui de propósito — são valores de
// upgrade, decididos pelo Paladin.cs (o controlador) e recebidos em Launch(). Assim, quando
// upgrades existirem, só se edita o Paladin.cs — nunca este prefab. impactDuration continua
// aqui porque é timing de animação (duração do clipe de impacto), não balanceamento.
public class PaladinHammer : MonoBehaviour
{
    [SerializeField] private Animator animator; // opcional — Idle(voo)/Impact
    [SerializeField] private float impactDuration = 0.5f; // 🔢 duração do clipe de impacto — sem Animation Event, destrói por tempo

    [Header("Debug — só pra visualização em Editor, não afeta gameplay")]
    [SerializeField] private bool showExplosionGizmo = true;
    [SerializeField] private float explosionGizmoOffsetY = 0f; // sobe o centro do gizmo/dano em relação ao pivô — mesmo padrão da Vine/RogueBomb/EnemyController

    private Vector2 direction;
    private float hitDamage;
    private float speed;
    private float maxRange;
    private float explosionRadius;
    private LayerMask enemyLayerMask;
    private float distanceTraveled;
    private bool impacted;
    private float impactElapsed;
    private readonly List<EnemyController> explosionTargets = new();

    private Vector3 ExplosionCenter => transform.position + Vector3.up * explosionGizmoOffsetY;

    public void Launch(Vector2 launchDirection, float damage, LayerMask layerMask, float projectileSpeed, float projectileMaxRange, float projectileExplosionRadius)
    {
        direction = launchDirection.normalized;
        hitDamage = damage;
        enemyLayerMask = layerMask;
        speed = projectileSpeed;
        maxRange = projectileMaxRange;
        explosionRadius = projectileExplosionRadius;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void Update()
    {
        if (!GameplayGate.IsActive) return;

        if (impacted)
        {
            impactElapsed += Time.deltaTime;
            if (impactElapsed >= impactDuration) Destroy(gameObject);
            return;
        }

        float step = speed * Time.deltaTime;
        transform.position += (Vector3)(direction * step);
        distanceTraveled += step;
        if (distanceTraveled >= maxRange) Impact();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (impacted) return;
        if (!other.CompareTag("Enemy")) return;

        var enemy = other.GetComponent<EnemyController>();
        if (enemy == null) return;

        // Dano direto, único — só o monstro realmente tocado pelo martelo em voo.
        enemy.TakeDamage(hitDamage);
        Impact();
    }

    // Chamada tanto por acerto direto quanto por fim de alcance — o dano em área sai na hora,
    // no exato momento da troca pra animação de impacto (pedido do usuário: sem Animation Event).
    private void Impact()
    {
        if (impacted) return;
        impacted = true;
        impactElapsed = 0f;
        // Explosão tem orientação fixa própria, não a de voo (mesmo critério do RogueBomb/MageFireball).
        transform.rotation = Quaternion.identity;

        ApplyExplosionDamage();

        if (animator != null) animator.Play("Impact");
    }

    private void ApplyExplosionDamage()
    {
        float explosionDamage = hitDamage * 0.5f; // GDD: sempre metade do dano do Paladin recebido em Launch
        var hits = Physics2D.OverlapCircleAll(ExplosionCenter, explosionRadius, enemyLayerMask);
        // Dedup obrigatório — mesmo bug do primário do Mage (ver EnemyController.CollectDistinct()).
        EnemyController.CollectDistinct(hits, hits.Length, explosionTargets);
        foreach (var enemy in explosionTargets) enemy.TakeDamage(explosionDamage);
    }

    private void OnDrawGizmosSelected()
    {
        if (!showExplosionGizmo) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(ExplosionCenter, explosionRadius);
    }
}
