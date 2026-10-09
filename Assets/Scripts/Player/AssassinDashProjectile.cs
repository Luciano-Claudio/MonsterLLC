using System.Collections.Generic;
using UnityEngine;

// Projétil de "ida e volta" do ataque do Assassin (Deadly Dash normal e Thousand Blades
// sombrio, GDD Seção 17.9 — redesenho a pedido do usuário). Não existe mais dano contínuo ao
// longo do trajeto do PRÓPRIO Assassin: ele fica invisível no lugar (ver Assassin.cs,
// AnimationDashStartEndEvent) enquanto este projétil anda pra frente (Start), bate 1x em área
// via trigger (Effect) e volta pro ponto exato de origem (End) — ao terminar, avisa o
// Assassin pra reaparecer ali mesmo. Controlador decide TODO valor de balanceamento e repassa
// em Launch() — este script só guarda wiring (Animator/Collider), mesmo critério de todo
// projétil do projeto desde a Sprint 27b.
public class AssassinDashProjectile : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private CircleCollider2D effectCollider; // trigger — raio setado em Launch()

    private Assassin owner;
    private Vector2 direction;
    private float damage;
    private float speed;
    private float knockbackForce;
    private LayerMask enemyLayerMask;
    private bool effectHitFired;
    private readonly List<EnemyController> hitTargets = new();
    private static readonly Collider2D[] OverlapBuffer = new Collider2D[16];

    public void Launch(Assassin dashOwner, Vector2 dashDirection, float dashDamage, float projectileSpeed,
        float dashKnockbackForce, float effectRadius, LayerMask layerMask)
    {
        owner = dashOwner;
        direction = dashDirection.normalized;
        damage = dashDamage;
        speed = projectileSpeed;
        knockbackForce = dashKnockbackForce;
        enemyLayerMask = layerMask;

        if (effectCollider != null) effectCollider.radius = effectRadius;

        // Sprite de referência nasce apontando pra "cima" (N, +Y) — mesma técnica do
        // PaladinHammer/RangerArrow/MageFireball.
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void Update()
    {
        if (!GameplayGate.IsActive) return;

        // Distância implícita (velocidade × duração real do clipe), mesmo critério de todo
        // dash/projétil do projeto — "Start" anda pra frente, "End" anda de volta. Sem
        // movimento nenhum em "Effect" (parado, só o hit em área).
        float step = speed * Time.deltaTime;
        if (AnimatorStateCheck.IsInState(animator, "Start"))
            transform.position += (Vector3)(direction * step);
        else if (AnimatorStateCheck.IsInState(animator, "End"))
            transform.position -= (Vector3)(direction * step);
    }

    // Animation Event, no frame de impacto do "Effect" — 1 hit em área só (sem perseguir
    // múltiplos frames de overlap), mesmo critério do antigo Thousand Blades, agora
    // compartilhado pelos 2 ataques (Deadly Dash normal e Thousand Blades sombrio).
    public void AnimationProjectileEffectHitEvent()
    {
        if (effectHitFired) return;
        effectHitFired = true;

        int count = effectCollider.Overlap(ContactFilter2D.noFilter, OverlapBuffer);
        // Dedup obrigatório — mesmo bug do primário do Mage (ver EnemyController.CollectDistinct()).
        EnemyController.CollectDistinct(OverlapBuffer, count, hitTargets);
        foreach (var enemy in hitTargets)
        {
            // Com dashProjectileCount > 1, os raios de 2 projéteis vizinhos podem se
            // interceptar — TryClaimDashHit() garante que um monstro na intersecção só leva
            // dano do PRIMEIRO projétil que o alcançar nesta leva, nunca de 2+.
            if (owner != null && !owner.TryClaimDashHit(enemy)) continue;

            enemy.TakeDamage(damage);
            enemy.ApplyKnockback(direction, knockbackForce);
        }
    }

    // Animation Event, no último frame do clipe "End" — o projétil já voltou pro ponto de
    // origem (via Update() acima); avisa o Assassin (passando a própria referência — com
    // dashProjectileCount > 1, o Assassin só reaparece quando TODOS os projéteis da leva
    // tiverem avisado) e se destrói.
    public void AnimationProjectileReturnedEvent()
    {
        if (owner != null) owner.OnDashProjectileReturned(this);
        Destroy(gameObject);
    }
}
