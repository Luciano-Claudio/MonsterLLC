using System.Collections.Generic;
using UnityEngine;

// Faca da Ultimate do Ranger (GDD Seção 17.2/13) — exceção à regra geral de projétil: em
// vez de sumir ao esgotar a reserva ou alcançar a distância máxima, fica no chão como
// Persistent Area, continuando a causar dano por um tempo. Direção sempre uma das 8 fixas
// (nunca mira livre, ao contrário da RangerArrow) — voo usa Animator com 8 estados soltos,
// cada um com sua própria animação de 4 frames (mesmo padrão do HeroProjectile do
// Barbarian: sem Blend Tree, a direção não muda depois do disparo, só Play() por nome uma
// vez no lançamento). Chão é um 9º estado só (sem variação de direção, é área no chão).
// Nenhum valor de voo/área/bleed é serializado aqui de propósito — todos são upgrade,
// decididos pelo Ranger (controlador) e recebidos em Launch(). groundedStateName continua
// aqui porque é wiring (nome do estado no Animator), não balanceamento.
public class RangerKnife : MonoBehaviour
{
    private enum Phase { Flying, Grounded }
    private Phase phase = Phase.Flying;

    [SerializeField] private string groundedStateName = "Grounded"; // nome do 9º estado no Animator

    private float flightSpeed;
    private float maxDistance;
    private float flightKnockbackForce;
    private float groundedDuration;
    private float groundedTickInterval;
    private float groundedRadius;
    private float groundedKnockbackForce;
    private float bleedDuration;

    private Animator animator;
    private CircleCollider2D circleCollider;
    private Vector2 direction;
    private float flightDamageReserve;
    private float groundedDamagePerTick;
    private float bleedDamagePerSecond;
    private float distanceTraveled;
    private float groundedElapsed;
    private AttackCooldown groundedTick;
    private readonly HashSet<EnemyController> enemiesInRange = new();

    private void Awake()
    {
        animator = GetComponent<Animator>();
        circleCollider = GetComponent<CircleCollider2D>();
    }

    public void Launch(Vector2 dir, float flightDamage, float groundedDamage, float bleedPerSecond,
        float speed, float distance, float flightKnockback, float groundDuration, float groundTickInterval, float groundRadius, float groundKnockback, float bleedDur)
    {
        direction = dir.normalized;
        flightDamageReserve = flightDamage;
        groundedDamagePerTick = groundedDamage;
        bleedDamagePerSecond = bleedPerSecond;
        flightSpeed = speed;
        maxDistance = distance;
        flightKnockbackForce = flightKnockback;
        groundedDuration = groundDuration;
        groundedTickInterval = groundTickInterval;
        groundedRadius = groundRadius;
        groundedKnockbackForce = groundKnockback;
        bleedDuration = bleedDur;

        if (animator != null) animator.Play(DirectionUtility.GetDirectionName(direction));
    }

    // Efeito Nocivo Bleeding — usado tanto no hit em voo quanto no tick no chão. Imune a
    // Bleeding não recebe (mesmo padrão do Fire Elemental com Fire, ver MageFireball).
    private void ApplyBleeding(EnemyController enemy)
    {
        var statusEffects = enemy.GetComponent<StatusEffectController>();
        if (statusEffects != null) statusEffects.ApplyStatusEffect(StatusEffectType.Bleeding, bleedDuration, bleedDamagePerSecond);
    }

    private void Update()
    {
        if (!GameplayGate.IsActive) return;

        if (phase == Phase.Flying) UpdateFlying();
        else UpdateGrounded();
    }

    private void UpdateFlying()
    {
        // position direto (espaço de mundo), não Translate — Translate usa espaço LOCAL por
        // padrão, e a faca está rotacionada, então os eixos locais giraram junto.
        float step = flightSpeed * Time.deltaTime;
        transform.position += (Vector3)(direction * step);
        distanceTraveled += step;
        if (distanceTraveled >= maxDistance) BecomeGrounded();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Enemy")) return;
        var enemy = other.GetComponent<EnemyController>();
        if (enemy == null) return;

        if (phase == Phase.Flying)
        {
            float damageDealt = Mathf.Min(flightDamageReserve, enemy.stats.health);
            enemy.TakeDamage(damageDealt);
            enemy.ApplyKnockback(direction, flightKnockbackForce);
            ApplyBleeding(enemy);

            flightDamageReserve -= damageDealt;
            if (flightDamageReserve <= 0f) BecomeGrounded();
        }
        else
        {
            // Grounded: só entra na lista de quem toma dano no próximo tick — não aplica na
            // hora, pra não dar 2 hits (entrada + tick) no mesmo instante.
            enemiesInRange.Add(enemy);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (phase != Phase.Grounded) return;
        if (!other.CompareTag("Enemy")) return;
        var enemy = other.GetComponent<EnemyController>();
        if (enemy != null) enemiesInRange.Remove(enemy);
    }

    private void BecomeGrounded()
    {
        phase = Phase.Grounded;
        groundedElapsed = 0f;
        groundedTick = new AttackCooldown(groundedTickInterval);
        if (circleCollider != null) circleCollider.radius = groundedRadius;
        if (animator != null) animator.Play(groundedStateName);

        // Pega de graça quem já estava exatamente em cima do ponto de pouso — mudar o raio
        // do collider não reemite OnTriggerEnter2D pra quem já estava sobreposto.
        var hits = Physics2D.OverlapCircleAll(transform.position, groundedRadius);
        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Enemy")) continue;
            var enemy = hit.GetComponent<EnemyController>();
            if (enemy != null) enemiesInRange.Add(enemy);
        }
    }

    private void UpdateGrounded()
    {
        groundedElapsed += Time.deltaTime;
        if (groundedElapsed >= groundedDuration)
        {
            Destroy(gameObject);
            return;
        }

        groundedTick.Tick(Time.deltaTime);
        if (!groundedTick.TryConsume()) return;

        enemiesInRange.RemoveWhere(e => e == null); // limpa quem morreu desde o último tick

        foreach (var enemy in enemiesInRange)
        {
            enemy.TakeDamage(groundedDamagePerTick);
            Vector2 away = ((Vector2)enemy.transform.position - (Vector2)transform.position).normalized;
            enemy.ApplyKnockback(away, groundedKnockbackForce);
            ApplyBleeding(enemy);
        }
    }
}
