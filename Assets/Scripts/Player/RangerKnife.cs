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
public class RangerKnife : MonoBehaviour, IPooledProjectile
{
    private enum Phase { Flying, Grounded }
    private Phase phase = Phase.Flying;

    [SerializeField] private string groundedStateName = "Grounded"; // nome do 9º estado no Animator

    // Simulation Rings (Seção 24 do GDD, resolvido 2026-10-31) — sem jogador por perto pra se
    // importar com quem está pisando na faca fincada, não tem motivo pra ticar dano em
    // ninguém. A duração continua contando normal enquanto suspenso (a faca ainda some no
    // tempo certo) — só o tick de dano fica pausado. Mesmo raio padrão do anel próximo dos
    // monstros (EnemyController.simulationNearRingRadius), duplicado aqui de propósito (não é
    // um singleton compartilhado, cada efeito calibra o próprio).
    [SerializeField] private float simulationRingRadius = 15f; // 🔢 ajustável

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
    private readonly List<EnemyController> groundedTickTargets = new();

    // Catálogo de cartas — projéteis (ProjectileCards): mesmos efeitos da flecha, só na fase de voo.
    private float launchGroundedDamage, launchSpeed, launchDistance;
    private bool isChild;
    private EnemyController ignoreTarget; // filho: o inimigo que gerou a cópia não é acertado de novo em voo
    private EnemyController homingTarget;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        circleCollider = GetComponent<CircleCollider2D>();
        if (circleCollider != null) flightRadius = circleCollider.radius;
        // Ver PhysicsUtility — sem Rigidbody2D, o trigger deste projétil às vezes nunca
        // disparava (tunelamento, bug crítico reportado pelo usuário, 2026-10-25).
        PhysicsUtility.EnsureKinematicRigidbody2D(gameObject);
    }

    private float flightRadius; // raio do collider no prefab (BecomeGrounded troca pelo raio da área no chão)

    // Fila de projéteis (HeroProjectilePool): volta tudo ao que um Instantiate novo teria.
    public void ResetForReuse()
    {
        phase = Phase.Flying;
        flightSpeed = 0f; maxDistance = 0f; flightKnockbackForce = 0f;
        groundedDuration = 0f; groundedTickInterval = 0f; groundedRadius = 0f; groundedKnockbackForce = 0f; bleedDuration = 0f;
        direction = Vector2.zero; flightDamageReserve = 0f; groundedDamagePerTick = 0f; bleedDamagePerSecond = 0f;
        distanceTraveled = 0f; groundedElapsed = 0f; groundedTick = null;
        enemiesInRange.Clear(); groundedTickTargets.Clear();
        launchGroundedDamage = 0f; launchSpeed = 0f; launchDistance = 0f;
        isChild = false; ignoreTarget = null; homingTarget = null;
        if (circleCollider != null) circleCollider.radius = flightRadius;
    }

    public void Launch(Vector2 dir, float flightDamage, float groundedDamage, float bleedPerSecond,
        float speed, float distance, float flightKnockback, float groundDuration, float groundTickInterval, float groundRadius, float groundKnockback, float bleedDur)
    {
        direction = dir.normalized;
        flightDamageReserve = flightDamage;
        groundedDamagePerTick = groundedDamage;
        bleedDamagePerSecond = bleedPerSecond;
        flightSpeed = speed * ProjectileCards.SpeedFactor;      // Windpiercer
        maxDistance = distance * ProjectileCards.SpeedFactor;
        launchGroundedDamage = groundedDamage; launchSpeed = speed; launchDistance = distance;
        if (!isChild) transform.localScale *= ProjectileCards.SizeFactor; // Gravity Core
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
    private void SpawnCardChildren(EnemyController hit, float damageDealt)
    {
        if (isChild || damageDealt <= 0f) return;
        var t = RunSettings.Current;
        if (ProjectileCards.SplitChance > 0f && Random.value < ProjectileCards.SplitChance)
        {
            SpawnChild(ProjectileCards.Rotate(direction, 45f), damageDealt * t.shardDamagePercent, t.shardDamagePercent, hit);
            SpawnChild(ProjectileCards.Rotate(direction, -45f), damageDealt * t.shardDamagePercent, t.shardDamagePercent, hit);
        }
        if (ProjectileCards.Ricochet)
        {
            var next = ProjectileCards.RicochetTarget(transform.position, hit);
            if (next != null) SpawnChild(((Vector2)(next.transform.position - transform.position)).normalized, damageDealt * t.ricochetDamagePercent, t.ricochetDamagePercent, hit);
        }
    }

    // percent: a mesma fração do voo vale para o dano no chão e o sangramento da cópia.
    private void SpawnChild(Vector2 dir, float reserve, float percent, EnemyController source)
    {
        var obj = HeroProjectilePool.SpawnLike(gameObject, transform.position, transform.parent);
        obj.transform.localScale = transform.localScale; // mesma escala do pai (o clone antigo copiava)
        var child = obj.GetComponent<RangerKnife>();
        child.isChild = true;
        child.ignoreTarget = source;
        child.Launch(dir, reserve, launchGroundedDamage * percent, bleedDamagePerSecond * percent, launchSpeed, launchDistance, flightKnockbackForce,
            groundedDuration, groundedTickInterval, groundedRadius, groundedKnockbackForce, bleedDuration);
    }

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
        var steered = ProjectileCards.Steer(transform.position, direction, ref homingTarget, Time.deltaTime); // Predator's Eye
        if (steered != direction) direction = steered;
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
            if (ReferenceEquals(enemy, ignoreTarget)) return;
            float damageDealt = Mathf.Min(flightDamageReserve, enemy.stats.health);
            HeroDamage.Direct(enemy, damageDealt, CardAbility.Ultimate, pierce: true); // catálogo de cartas: ponto único
            enemy.ApplyKnockback(direction, flightKnockbackForce);
            ApplyBleeding(enemy);
            SpawnCardChildren(enemy, damageDealt);

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
            HeroProjectilePool.Release(gameObject);
            return;
        }

        // Simulation Rings — sem jogador por perto, suspende só o tick de dano (ver campo
        // simulationRingRadius acima); a contagem de groundedDuration já aconteceu antes
        // deste if, então a faca continua desaparecendo no tempo certo mesmo abandonada.
        if (!IsPlayerNearby()) return;

        groundedTick.Tick(Time.deltaTime);
        if (!groundedTick.TryConsume()) return;

        enemiesInRange.RemoveWhere(e => e == null); // limpa quem morreu desde o último tick

        // Cópia pro foreach — TakeDamage() pode matar o monstro e ApplyKnockback() pode empurrar
        // outro pra fora do raio, e os dois desligam/tiram o Collider2D de cima da faca, o que
        // dispara OnTriggerExit2D NESTA MESMA RangerKnife de volta, fazendo
        // enemiesInRange.Remove() enquanto o foreach original ainda estava rodando nele
        // (InvalidOperationException: "Collection was modified" — mesmo bug já corrigido no
        // rastro "Grounded" do MageFireball e na área do Rogue).
        groundedTickTargets.Clear();
        groundedTickTargets.AddRange(enemiesInRange);

        foreach (var enemy in groundedTickTargets)
        {
            enemy.TakeDamage(groundedDamagePerTick, isDirectPlayerHit: false); // tick da faca fincada, sem Game Feel — só o hit de voo (linha ~101) aciona
            Vector2 away = ((Vector2)enemy.transform.position - (Vector2)transform.position).normalized;
            enemy.ApplyKnockback(away, groundedKnockbackForce);
            ApplyBleeding(enemy);
        }
    }

    // Único player do jogo — nunca via física, só pela tag (mesmo critério do EnemyProjectile/
    // OrcShamanTotem/MageFireball).
    private bool IsPlayerNearby()
    {
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        return playerObj != null && Vector2.Distance(transform.position, playerObj.transform.position) <= simulationRingRadius;
    }
}
