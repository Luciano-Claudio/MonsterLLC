using UnityEngine;

// Projétil reto de herói (GDD Seção 13) — carrega uma reserva de dano em vez de aplicar o
// dano cheio de uma vez: ao acertar um monstro, gasta da reserva só o mínimo entre ela e a
// vida do alvo, e continua a mesma trajetória se sobrar. Diferente do projétil de monstro
// comum (EnemyProjectile), que aplica dano cheio e é destruído no primeiro contato.
//
// speed/maxDistance/knockbackForce NÃO são serializados aqui de propósito — são valores de
// upgrade, decididos pelo herói que instancia este prefab (hoje só o Barbarian) e recebidos
// em Launch(). Assim, upgrades futuros só editam o controlador do herói, nunca este prefab.
//
// Infra pros perks de Cima do Barbarian (Seção 11, R10 — construída com default neutro, sem
// nenhum perk escrevendo ainda, isso é conteúdo da próxima fase). Cada capacidade é um Set*()
// separado, opt-in, nunca exigido por Launch() — zero risco pro caminho existente (Launch()
// em si não mudou 1 caractere).
public class HeroProjectile : MonoBehaviour
{
    // Opcional — 8 estados soltos (N/NE/E/SE/S/SW/W/NW), sem Blend Tree e sem parâmetro
    // nenhum: a direção não muda depois do disparo, então Launch() só dá Play() direto pelo
    // nome do estado uma vez, sem passar pelo grafo de transições.
    private Animator animator;

    private Vector2 direction;
    private float damageReserve;
    private float speed;
    private float maxDistance;
    private float knockbackForce;
    private float distanceTraveled;

    // Cima #26 (Renovação de Batalha — cura por inimigo atingido) e #24 (Trilha de Fúria —
    // Ground Crack no caminho) precisam saber QUEM disparou, pra devolver o efeito pro herói
    // certo. Nulo por padrão — projétil funciona idêntico a antes sem isso.
    private HeroController owner;
    public void SetOwner(HeroController owner) => this.owner = owner;

    // Cima #23 (Explosão de Impacto) — dano em área no ponto de impacto, % do dano JÁ aplicado
    // no alvo principal (não da reserva inteira, pra não dobrar o dano de perfuração). Raio 0 =
    // sem efeito (default).
    private float explosionRadius;
    private float explosionDamagePercent;
    public void SetExplosion(float radius, float damagePercentOfHit)
    {
        explosionRadius = radius;
        explosionDamagePercent = damagePercentOfHit;
    }

    // Cima #26 (Renovação de Batalha) — cura o dono, % do dano causado em CADA acerto (não só
    // no 1º). 0 = sem efeito (default).
    private float healPercentPerHit;
    public void SetHealOnHit(float percentOfDamageDealt) => healPercentPerHit = percentOfDamageDealt;

    // Cima #24 (Trilha de Fúria) — rolado periodicamente (groundCrackTrailTickInterval) enquanto
    // o projétil viaja; prefab nulo = sem efeito (default). Reaproveita o MESMO prefab de
    // GroundCrack do Barbarian (passado por quem chama SetGroundCrackTrail), nunca cria um novo.
    [SerializeField] private GameObject groundCrackTrailPrefab; // pode vir null — opcional, setado via SetGroundCrackTrail
    private float groundCrackTrailChance;
    private float groundCrackTrailDamagePerSecond;
    private float groundCrackTrailTickInterval = 0.5f;
    private float groundCrackTrailDuration = 3f;
    private float groundCrackTrailElapsed;

    public void SetGroundCrackTrail(GameObject prefab, float chancePerTick, float damagePerSecond, float tickInterval, float duration)
    {
        groundCrackTrailPrefab = prefab;
        groundCrackTrailChance = chancePerTick;
        groundCrackTrailDamagePerSecond = damagePerSecond;
        groundCrackTrailTickInterval = tickInterval;
        groundCrackTrailDuration = duration;
    }

    // Cima #30 (Fragmentação Sem Fim) — ao acertar, se divide em 2 filhos (metade do dano/
    // reserva cada, direções desviadas ±45° da atual), que podem se dividir de novo até
    // fragmentDepthRemaining chegar a 0 — CAP explícito, nunca recursão infinita.
    // fragmentOnHit=false (default) = sem efeito, comportamento idêntico a antes.
    private bool fragmentOnHit;
    private int fragmentDepthRemaining;
    private float fragmentDamageDecayPercent = 50f;
    public void SetFragmentation(bool enabled, int depthRemaining, float damageDecayPercent)
    {
        fragmentOnHit = enabled;
        fragmentDepthRemaining = depthRemaining;
        fragmentDamageDecayPercent = damageDecayPercent;
    }

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void Launch(Vector2 dir, float reserve, float projectileSpeed, float projectileMaxDistance, float projectileKnockbackForce)
    {
        direction = dir.normalized;
        damageReserve = reserve;
        speed = projectileSpeed;
        maxDistance = projectileMaxDistance;
        knockbackForce = projectileKnockbackForce;
        if (animator != null) animator.Play(DirectionUtility.GetDirectionName(direction));
    }

    private void Update()
    {
        if (!GameplayGate.IsActive) return;

        float step = speed * Time.deltaTime;
        transform.Translate(direction * step);
        distanceTraveled += step;

        TickGroundCrackTrail(Time.deltaTime);

        if (distanceTraveled >= maxDistance) Destroy(gameObject);
    }

    private void TickGroundCrackTrail(float deltaTime)
    {
        if (groundCrackTrailPrefab == null || groundCrackTrailChance <= 0f) return;

        groundCrackTrailElapsed += deltaTime;
        if (groundCrackTrailElapsed < groundCrackTrailTickInterval) return;
        groundCrackTrailElapsed = 0f;

        if (Random.value >= groundCrackTrailChance) return;

        var crackObj = Instantiate(groundCrackTrailPrefab, transform.position, Quaternion.identity, HeroController.HerosContainer);
        var crack = crackObj.GetComponent<GroundCrack>();
        if (crack != null) crack.Initialize(QuadrantDirection(direction), 1f, groundCrackTrailDamagePerSecond, groundCrackTrailTickInterval);
        Destroy(crackObj, groundCrackTrailDuration);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Enemy")) return;

        var enemy = other.GetComponent<EnemyController>();
        if (enemy == null) return;

        float damageDealt = Mathf.Min(damageReserve, enemy.stats.health);
        enemy.TakeDamage(damageDealt);
        enemy.ApplyKnockback(direction, knockbackForce);

        if (healPercentPerHit > 0f) owner?.HealFromProjectile(damageDealt * healPercentPerHit / 100f);
        if (explosionRadius > 0f) ApplyExplosion(enemy, damageDealt);

        damageReserve -= damageDealt;

        if (fragmentOnHit && fragmentDepthRemaining > 0)
        {
            SpawnFragments();
            Destroy(gameObject);
            return;
        }

        if (damageReserve <= 0f) Destroy(gameObject);
    }

    // Dano em área no ponto de impacto, % do dano JÁ aplicado no alvo principal — nunca atinge
    // o mesmo alvo 2x (Collider2D[] filtra ele fora via referência).
    private void ApplyExplosion(EnemyController hitEnemy, float mainHitDamage)
    {
        float explosionDamage = mainHitDamage * explosionDamagePercent / 100f;
        if (explosionDamage <= 0f) return;

        var results = new Collider2D[32];
        int count = Physics2D.OverlapCircle(transform.position, explosionRadius, ContactFilter2D.noFilter, results);
        for (int i = 0; i < count; i++)
        {
            var other = results[i].GetComponent<EnemyController>();
            if (other == null || other == hitEnemy) continue;
            other.TakeDamage(explosionDamage);
        }
    }

    // Spawna os 2 filhos ANTES de destruir o pai — metade do dano/reserva, mesma configuração
    // (owner/explosão/cura/trilha), 1 nível mais perto do limite (fragmentDepthRemaining - 1).
    private void SpawnFragments()
    {
        float childReserve = damageReserve * (1f - fragmentDamageDecayPercent / 100f);
        if (childReserve <= 0f) return;

        Vector2 dirA = Quaternion.Euler(0, 0, 45f) * direction;
        Vector2 dirB = Quaternion.Euler(0, 0, -45f) * direction;

        SpawnFragment(dirA, childReserve);
        SpawnFragment(dirB, childReserve);
    }

    private void SpawnFragment(Vector2 dir, float reserve)
    {
        var obj = Instantiate(gameObject, transform.position, Quaternion.identity, transform.parent);
        var projectile = obj.GetComponent<HeroProjectile>();
        projectile.Launch(dir, reserve, speed, maxDistance - distanceTraveled, knockbackForce);
        projectile.SetOwner(owner);
        projectile.SetExplosion(explosionRadius, explosionDamagePercent);
        projectile.SetHealOnHit(healPercentPerHit);
        projectile.SetGroundCrackTrail(groundCrackTrailPrefab, groundCrackTrailChance, groundCrackTrailDamagePerSecond, groundCrackTrailTickInterval, groundCrackTrailDuration);
        projectile.SetFragmentation(true, fragmentDepthRemaining - 1, fragmentDamageDecayPercent);
    }

    // Mesmo critério de quadrante que Barbarian.GetHitboxForFacing() já usa — pra GroundCrack
    // escolher a animação certa (NE/NW/SE/SW) a partir de uma direção qualquer de voo.
    private static GroundCrack.Direction QuadrantDirection(Vector2 dir)
    {
        bool east = dir.x >= 0f;
        bool north = dir.y >= 0f;
        if (north) return east ? GroundCrack.Direction.NE : GroundCrack.Direction.NW;
        return east ? GroundCrack.Direction.SE : GroundCrack.Direction.SW;
    }
}
