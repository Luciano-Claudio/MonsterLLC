using System.Collections.Generic;
using UnityEngine;

// Projétil da Ultimate do Rogue — mesma estrutura do MageFireball (GDD Seção 13, Ground
// Target/Impact Area): voo em ÂNGULO LIVRE (rotacionado de verdade, não travado nas 8
// direções) até colidir com um monstro ou alcançar o alcance máximo, então explode em área
// (Animation Event dedicado aplica o dano de impacto, não Explode() direto — mesma separação
// Hit/End do fireball). ÚNICA diferença em relação ao Mage: sem fase "Grounded" — não deixa
// superfície persistente no chão depois da explosão, some no fim do clipe.
public class RogueBomb : MonoBehaviour
{
    [Header("Voo — ângulo livre, direção inicial do sprite é o Norte (+Y)")]
    [SerializeField] private float speed = 8f; // 🔢 ajustável
    [SerializeField] private float defaultMaxDistance = 10f; // 🔢 ajustável — não escala por Tier de Arma ainda (mesmo placeholder do vineCount/arrowCount)

    [Header("Explosão — dano em área no final da trajetória")]
    [SerializeField] private float explosionRadius = 2.5f; // 🔢 ajustável — maior que o Pulso, é a Ultimate
    [SerializeField] private Animator animator; // opcional — só se houver clipe de explosão dedicado (ver Explode())

    [Header("Debug — só pra visualização em Editor, não afeta gameplay")]
    [SerializeField] private bool showExplosionGizmo = false;
    [SerializeField] private float explosionGizmoOffsetY = 0f; // sobe o centro do gizmo/dano em relação ao pivô — mesmo padrão da Vine/EnemyController

    private Vector2 direction;
    private float damage;
    private LayerMask enemyLayerMask;
    private float distanceTraveled;
    private bool exploded;
    private bool explosionHitFired;
    private readonly List<EnemyController> explosionTargets = new();

    private Vector3 ExplosionCenter => transform.position + Vector3.up * explosionGizmoOffsetY;

    public void Launch(Vector2 dir, float bombDamage, LayerMask layerMask)
    {
        direction = dir.normalized;
        damage = bombDamage;
        enemyLayerMask = layerMask;

        // Sprite de referência nasce apontando pra "cima" (N, +Y) — mesma técnica da
        // RangerArrow/EnemyProjectile/MageFireball.
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void Update()
    {
        if (!GameplayGate.IsActive) return;
        if (exploded) return;

        // position direto (espaço de mundo), não Translate — o Transform está rotacionado,
        // então os eixos locais giraram junto (mesmo motivo da RangerArrow/MageFireball).
        float step = speed * Time.deltaTime;
        transform.position += (Vector3)(direction * step);
        distanceTraveled += step;
        if (distanceTraveled >= defaultMaxDistance) Explode();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (exploded) return;
        // "Colide" = colide com um monstro — nunca dano de contato direto, só interrompe o
        // voo e detona a explosão.
        var enemy = other.GetComponent<EnemyController>();
        if (enemy != null) Explode();
    }

    private void Explode()
    {
        if (exploded) return; // evita detonar 2x se Update() e OnTriggerEnter2D colidirem no mesmo frame
        exploded = true;
        explosionHitFired = false;
        // A rotação em ângulo livre só faz sentido durante o voo — a explosão tem orientação
        // fixa própria, não a de voo (mesmo critério do MageFireball).
        transform.rotation = Quaternion.identity;

        if (animator != null)
        {
            animator.SetTrigger("ExplodeTrigger"); // dano vem de AnimationExplodeHitEvent()
        }
        else
        {
            // Sem Animator/clipe dedicado, não existe Animation Event pra disparar o dano —
            // aplica na hora e some (placeholder aceitável).
            ApplyExplosionDamage();
            Destroy(gameObject);
        }
    }

    // Animation Event, no frame de impacto do clipe de explosão — dano em área (só chamado se
    // um Animator dedicado estiver configurado no prefab — ver Explode()).
    public void AnimationExplodeHitEvent()
    {
        if (explosionHitFired) return;
        explosionHitFired = true;
        ApplyExplosionDamage();
    }

    private void ApplyExplosionDamage()
    {
        var hits = Physics2D.OverlapCircleAll(ExplosionCenter, explosionRadius, enemyLayerMask);
        // Dedup obrigatório — mesmo bug do primário do Mage (ver EnemyController.CollectDistinct()).
        EnemyController.CollectDistinct(hits, hits.Length, explosionTargets);
        foreach (var enemy in explosionTargets) enemy.TakeDamage(damage);
    }

    // Animation Event, no último frame do clipe de explosão (só chamado se um Animator
    // dedicado estiver configurado no prefab — ver Explode()).
    public void AnimationExplodeEndEvent()
    {
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        if (!showExplosionGizmo) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(ExplosionCenter, explosionRadius);
    }
}
