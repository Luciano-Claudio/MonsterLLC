using UnityEngine;

// Projétil do ataque primário do Cleric — persegue o monstro-alvo (Homing) até acertá-lo ou
// perdê-lo; ao acertar com reserva de dano sobrando, PARA de perseguir e continua reto na
// última direção (GDD Seção 17.6), podendo ainda perfurar outros monstros no caminho (mesmo
// comportamento de reserva-por-hit do RangerArrow). Rotação contínua do sprite durante o voo
// — mesma técnica da flecha do Ranger (GDD), não o HeroProjectile (esse troca entre 8 poses
// fixas via Animator, não rotaciona; não serve aqui porque o projétil muda de direção em
// voo). Ao esgotar a reserva num hit OU alcançar maxDistance sem acertar ninguém, toca a
// animação de impacto (orientação fixa, mesmo padrão do RogueBomb/MageFireball) antes de
// sumir de verdade.
// speed/maxDistance/knockbackForce NÃO são serializados aqui de propósito — são valores de
// upgrade, decididos pelo Cleric (controlador) e recebidos em Launch(). Upgrades futuros só
// editam o Cleric.cs, nunca este prefab.
public class ClericProjectile : MonoBehaviour
{
    [SerializeField] private Animator animator; // opcional — só se houver clipe de impacto dedicado (ver Impact())

    private EnemyController homingTarget;
    private Vector2 direction;
    private float damageReserve;
    private float speed;
    private float maxDistance;
    private float knockbackForce;
    private LayerMask enemyLayerMask;
    private float distanceTraveled;
    private bool isHoming = true;
    private bool impacted;

    public void Launch(EnemyController target, float reserve, LayerMask layerMask, float projectileSpeed, float projectileMaxDistance, float projectileKnockbackForce)
    {
        homingTarget = target;
        damageReserve = reserve;
        enemyLayerMask = layerMask;
        speed = projectileSpeed;
        maxDistance = projectileMaxDistance;
        knockbackForce = projectileKnockbackForce;
        direction = ((Vector2)target.transform.position - (Vector2)transform.position).normalized;
        ApplyRotation();
    }

    private void Update()
    {
        if (!GameplayGate.IsActive) return;
        if (impacted) return;

        // Alvo morreu/destruído antes de alcançar — GDD não cobre esse caso; tratado como o
        // mesmo desfecho de "acertou com reserva sobrando": para de perseguir, continua reto.
        if (isHoming && homingTarget == null) isHoming = false;

        if (isHoming)
        {
            direction = ((Vector2)homingTarget.transform.position - (Vector2)transform.position).normalized;
            ApplyRotation();
        }

        float step = speed * Time.deltaTime;
        transform.position += (Vector3)(direction * step);
        distanceTraveled += step;
        if (distanceTraveled >= maxDistance) Impact();
    }

    private void ApplyRotation()
    {
        // Mesmo ajuste do EnemyProjectile/RangerArrow — sprite de referência aponta pra "cima".
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (impacted) return;

        var enemy = other.GetComponent<EnemyController>();
        if (enemy == null) return;

        float damageDealt = Mathf.Min(damageReserve, enemy.stats.health);
        enemy.TakeDamage(damageDealt);
        enemy.ApplyKnockback(direction, knockbackForce);

        damageReserve -= damageDealt;
        if (damageReserve <= 0f) { Impact(); return; }

        isHoming = false; // reserva sobrando — para de perseguir, segue reto (GDD)
        if (enemy == homingTarget) homingTarget = null;
    }

    // Esgotou a reserva num hit OU alcançou maxDistance sem acertar ninguém — os 2 únicos
    // jeitos do projétil "acabar" (GDD).
    private void Impact()
    {
        if (impacted) return; // evita disparar 2x se Update() e OnTriggerEnter2D colidirem no mesmo frame
        impacted = true;

        // A rotação em ângulo livre só faz sentido durante o voo — a animação de impacto tem
        // orientação fixa própria, não a de voo (mesmo critério do RogueBomb/MageFireball).
        transform.rotation = Quaternion.identity;

        if (animator != null) animator.SetTrigger("ImpactTrigger");
        else Destroy(gameObject); // sem Animator/clipe dedicado, some na hora (placeholder aceitável)
    }

    // Animation Event, no último frame do clipe de impacto (só chamado se um Animator
    // dedicado estiver configurado no prefab — ver Impact()).
    public void AnimationImpactEndEvent()
    {
        Destroy(gameObject);
    }
}
