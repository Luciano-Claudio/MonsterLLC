using System.Collections.Generic;
using UnityEngine;

// Ultimate do Mage (GDD Seção 17.2/13) — Ground Target/Impact Area que vira Persistent Area,
// mesmo mecanismo da RangerKnife (voo -> chão), só que num objeto de 3 fases em vez de 2:
// Projectile (voo em ÂNGULO LIVRE, rotacionado de verdade — exceção do MVP, os outros
// projéteis retos travam nas 8 direções) -> Explosion (Animation Event aplica o dano de
// impacto, alto, numa área circular) -> GroundedFire (fica no chão causando dano em tick,
// igual as facas do Ranger, até acabar o tempo).
// Nenhum valor de voo/área/status é serializado aqui de propósito — todos são upgrade,
// decididos pelo Mage (controlador) e recebidos em Launch(). Upgrades futuros só editam o
// Mage.cs, nunca este prefab.
public class MageFireball : MonoBehaviour
{
    private enum Phase { Flying, Exploding, Grounded, Disappearing }
    private Phase phase = Phase.Flying;

    private float flightSpeed;
    private float maxTravelDistance;
    private float explosionRadius;
    private float groundedDuration;
    private float groundedTickInterval;
    private float groundedRadius;
    private float fireStatusDuration;

    private Animator animator;
    private CircleCollider2D circleCollider;
    private Vector2 direction;
    private float impactDamage;
    private float groundedDamagePerSecond;
    private float groundedDamagePerTick;
    private float fireStatusDamagePerSecond;
    private float distanceTraveled;
    private bool explosionHitFired;
    private float groundedElapsed;
    private AttackCooldown groundedTick;
    private readonly HashSet<EnemyController> enemiesInRange = new();
    private readonly List<EnemyController> explosionTargets = new();

    private void Awake()
    {
        animator = GetComponent<Animator>();
        circleCollider = GetComponent<CircleCollider2D>();
    }

    // damagePerSecond/fireDamagePerSecond são TAXAS (GDD: "0,5x o dano do Mage por segundo"
    // na área, "metade do dano do Mage por segundo" no status Fire) — o dano real de cada
    // tick da área só é calculado em AnimationExplosionEndEvent(), multiplicando pela
    // duração real do tick (groundedTickInterval), pra não depender dele ser exatamente 1s;
    // o status Fire já recebe a taxa pronta, porque quem tica ele é o StatusEffectController
    // do próprio monstro, não este script (ver ApplyStatusEffect).
    public void Launch(Vector2 dir, float impact, float damagePerSecond, float fireDamagePerSecond,
        float speed, float travelDistance, float impactRadius, float groundDuration, float groundTickInterval, float groundRadius, float burnDuration)
    {
        direction = dir.normalized;
        impactDamage = impact;
        groundedDamagePerSecond = damagePerSecond;
        fireStatusDamagePerSecond = fireDamagePerSecond;
        flightSpeed = speed;
        maxTravelDistance = travelDistance;
        explosionRadius = impactRadius;
        groundedDuration = groundDuration;
        groundedTickInterval = groundTickInterval;
        groundedRadius = groundRadius;
        fireStatusDuration = burnDuration;

        // Sprite de referência nasce apontando pra "cima" (N, +Y) — mesma técnica da
        // RangerArrow/EnemyProjectile.
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void Update()
    {
        if (!GameplayGate.IsActive) return;

        if (phase == Phase.Flying) UpdateFlying();
        else if (phase == Phase.Grounded) UpdateGrounded();
    }

    private void UpdateFlying()
    {
        // position direto (espaço de mundo), não Translate — o Transform está rotacionado,
        // então os eixos locais giraram junto (mesmo motivo da RangerArrow/RangerKnife).
        float step = flightSpeed * Time.deltaTime;
        transform.position += (Vector3)(direction * step);
        distanceTraveled += step;
        if (distanceTraveled >= maxTravelDistance) Explode();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Enemy")) return;

        if (phase == Phase.Flying)
        {
            Explode();
        }
        else if (phase == Phase.Grounded)
        {
            // Só entra na lista de quem toma dano no próximo tick — não aplica na hora, pra
            // não dar 2 hits (entrada + tick) no mesmo instante (mesmo padrão da RangerKnife).
            var enemy = other.GetComponent<EnemyController>();
            if (enemy != null) enemiesInRange.Add(enemy);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (phase != Phase.Grounded) return;
        if (!other.CompareTag("Enemy")) return;
        var enemy = other.GetComponent<EnemyController>();
        if (enemy != null) enemiesInRange.Remove(enemy);
    }

    private void Explode()
    {
        phase = Phase.Exploding;
        explosionHitFired = false;
        // A rotação em ângulo livre só faz sentido durante o voo — a partir daqui (Explosion,
        // e depois GroundedFire/Disappearing) o sprite fica na orientação normal, sem torcer
        // conforme a direção em que a bola veio voando.
        transform.rotation = Quaternion.identity;
        // Collider passa a representar a área de explosão de verdade (em vez de um
        // OverlapCircle com um raio calculado à parte, que podia dessincronizar do visual e
        // "às vezes pegava, às vezes não" — relatado pelo usuário). /scale.x pelo mesmo
        // motivo do groundedRadius abaixo: explosionRadius já é o valor FINAL, mas
        // circleCollider.radius é local e a escala do GameObject multiplica de novo.
        if (circleCollider != null) circleCollider.radius = explosionRadius / transform.localScale.x;
        if (animator != null) animator.SetTrigger("ExplodeTrigger");
    }

    // Animation Event, no frame de impacto do clipe "Explosion" — dano alto numa área
    // circular (GDD: "4x o dano do Mage"), independente de quantos monstros tem dentro.
    public void AnimationExplosionHitEvent()
    {
        if (explosionHitFired) return;
        explosionHitFired = true;

        if (circleCollider == null) return;

        // Overlap() do próprio collider (já redimensionado em Explode()) — mesmo padrão
        // confiável usado no golpe do Barbarian/shield bash do Paladin/fire do Mage, em vez
        // de um OverlapCircle com raio calculado à parte.
        var results = new Collider2D[16];
        int count = circleCollider.Overlap(ContactFilter2D.noFilter, results);
        // Dedup obrigatório — mesmo bug do primário do Mage (ver EnemyController.CollectDistinct()).
        // O dano no chão (UpdateGrounded) já é seguro — usa enemiesInRange, um HashSet.
        EnemyController.CollectDistinct(results, count, explosionTargets);
        foreach (var enemy in explosionTargets) enemy.TakeDamage(impactDamage);
    }

    // Animation Event, no fim do clipe "Explosion" — vira a área persistente no chão (mesmo
    // instante que a RangerKnife chama BecomeGrounded()).
    public void AnimationExplosionEndEvent()
    {
        phase = Phase.Grounded;
        groundedElapsed = 0f;
        groundedTick = new AttackCooldown(groundedTickInterval);
        groundedDamagePerTick = groundedDamagePerSecond * groundedTickInterval;
        // groundedRadius já vem FINAL (ver Launch()), mas circleCollider.radius é um valor
        // LOCAL — Unity multiplica pela escala do GameObject de novo na hora de calcular a
        // forma real no mundo. Sem dividir aqui, a área ficaria ao quadrado (ex.: upgrade 2x
        // virando 4x de raio de verdade).
        if (circleCollider != null) circleCollider.radius = groundedRadius / transform.localScale.x;
        if (animator != null) animator.Play("GroundedFire");

        // Pega de graça quem já estava exatamente em cima do ponto de pouso — mudar o raio
        // do collider não reemite OnTriggerEnter2D pra quem já estava sobreposto. Overlap()
        // do próprio collider (já redimensionado acima) — mesmo motivo do fix da explosão.
        if (circleCollider != null)
        {
            var results = new Collider2D[16];
            int count = circleCollider.Overlap(ContactFilter2D.noFilter, results);
            for (int i = 0; i < count; i++)
            {
                if (!results[i].CompareTag("Enemy")) continue;
                var enemy = results[i].GetComponent<EnemyController>();
                if (enemy != null) enemiesInRange.Add(enemy);
            }
        }
    }

    private void UpdateGrounded()
    {
        groundedElapsed += Time.deltaTime;
        if (groundedElapsed >= groundedDuration)
        {
            phase = Phase.Disappearing;
            if (animator != null) animator.SetTrigger("DisappearTrigger");
            return;
        }

        groundedTick.Tick(Time.deltaTime);
        if (!groundedTick.TryConsume()) return;

        enemiesInRange.RemoveWhere(e => e == null); // limpa quem morreu desde o último tick

        foreach (var enemy in enemiesInRange)
        {
            var statusEffects = enemy.GetComponent<StatusEffectController>();
            // Imune a Fire (ex.: futuro Fire Elemental) não leva o dano da superfície nem
            // pega o status — só o impacto direto da explosão (AnimationExplosionHitEvent)
            // continua acertando normal, esse aqui é o rastro "elemental" propriamente dito.
            if (statusEffects != null && statusEffects.IsImmuneTo(StatusEffectType.Fire)) continue;

            enemy.TakeDamage(groundedDamagePerTick);
            // Além do dano da área em si, quem está dentro no tick ganha o status Fire —
            // continua queimando por conta própria mesmo depois de sair da área.
            if (statusEffects != null) statusEffects.ApplyStatusEffect(StatusEffectType.Fire, fireStatusDuration, fireStatusDamagePerSecond);
        }
    }

    // Animation Event, no fim do clipe "Disapear" — só agora o objeto é destruído de
    // verdade (não mais Destroy() instantâneo assim que o tempo no chão acaba).
    public void AnimationDisappearEndEvent()
    {
        Destroy(gameObject);
    }

    // Visualiza no Editor o raio real dos 2 círculos (impacto e área no chão) — pra montar o
    // Collider2D/sprite/posição certinho sem precisar rodar em Play Mode pra ver o tamanho.
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);

        Gizmos.color = new Color(1f, 0.5f, 0f); // laranja
        Gizmos.DrawWireSphere(transform.position, groundedRadius);
    }
}
