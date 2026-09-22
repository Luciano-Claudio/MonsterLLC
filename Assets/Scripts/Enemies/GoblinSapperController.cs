using UnityEngine;

// Exceção à regra padrão (Bestiário) — ciclo armado (persegue) -> planta bomba no contato ->
// foge (sem bomba) até recarregar -> volta a perseguir. Some-se a isso uma camada de dano de
// contato passivo o tempo todo (EnemyContactDamage), armado ou não. hasAttackAnimation deve
// ficar FALSE no Inspector — a ficha não lista clipe "attack" nenhum (só idle/idle_bomb/walk/
// bomb_walk/damage/bomb_damage/die).
public class GoblinSapperController : EnemyController
{
    private enum SapperPhase { Armed, Fleeing }

    [Header("Bomba (GDD Bestiário — Goblin Sapper)")]
    [SerializeField] private GameObject bombPrefab; // precisa ter GoblinSapperBomb
    [SerializeField] private float bombExplosionDamageMultiplier = 2f; // 🔢 "deve ser maior que o de contato"

    [Header("Fuga e recarga")]
    [SerializeField] private float reloadDuration = 4f; // 🔢 ajustável
    [SerializeField] private float minSafeDistanceFromPlayer = 6f; // 🔢 até que distância foge direto do player antes de vagar sem rumo
    [SerializeField] private float safeFleeRadius = 4f; // 🔢 raio de cada ponto aleatório de vagar (uma vez já "seguro")

    [Header("Auto-explosão ao morrer")]
    [SerializeField] private float dieExplosionDamageMultiplier = 2.5f; // 🔢 ajustável
    [SerializeField] private float dieExplosionRadius = 1.8f; // 🔢 ajustável

    private SapperPhase phase = SapperPhase.Armed;
    private float fleeElapsed;
    private PatrolAI fleeAI;

    protected override void Awake()
    {
        base.Awake();
        fleeAI = new PatrolAI(1f, 2f, 1.5f, 3f); // 🔢 timers curtos de ir/parar dentro do raio seguro
        var contactDamage = GetComponent<EnemyContactDamage>();
        if (contactDamage != null) contactDamage.Initialize(stats.attackDamage, stats.attackAnimationCooldown);
    }

    protected override void Move()
    {
        if (animator != null) animator.SetBool("IsArmed", phase == SapperPhase.Armed);

        Vector2 toPlayer = player.position - transform.position;
        float distance = toPlayer.magnitude;

        if (phase == SapperPhase.Armed)
        {
            if (distance <= stats.attackRadius)
            {
                PlantBomb();
                return;
            }
            SetMoving(true);
            MoveInDirection(toPlayer.normalized, "Walk_Bomb"); // estado armado tem nome próprio, diferente do "Walk" default
            return;
        }

        // Fleeing — recarrega por tempo, e ativamente aumenta distância se o player alcançar.
        fleeElapsed += Time.deltaTime;
        if (fleeElapsed >= reloadDuration)
        {
            phase = SapperPhase.Armed;
            fleeElapsed = 0f;
            return;
        }

        if (distance < minSafeDistanceFromPlayer)
        {
            SetMoving(true);
            MoveInDirection(-toPlayer.normalized); // ainda perto demais -- prioriza afastar direto
            return;
        }

        fleeAI.Tick(Time.deltaTime, () => (Vector2)transform.position + Random.insideUnitCircle * safeFleeRadius);

        // Mesma proteção já usada em UpdatePatrol() (EnemyController) — sem isso, ao chegar
        // perto o bastante do ponto aleatório de vagar, a distância até ele encolhe perto de
        // zero e o vetor normalizado vira ruído: ultrapassa o ponto e volta a cada frame,
        // fazendo a diagonal escolhida (NE/NW/SE/SW) pular aleatoriamente entre as 4.
        Vector2 toWanderTarget = fleeAI.WalkTarget - (Vector2)transform.position;
        bool wandering = fleeAI.CurrentPhase == PatrolPhase.Walking && toWanderTarget.sqrMagnitude >= 0.0001f;
        SetMoving(wandering);
        if (wandering) MoveInDirection(toWanderTarget.normalized);
    }

    private void PlantBomb()
    {
        if (bombPrefab != null)
        {
            var bombObj = Instantiate(bombPrefab, player.position, Quaternion.identity);
            var bomb = bombObj.GetComponent<GoblinSapperBomb>();
            if (bomb != null) bomb.Initialize(stats.attackDamage * bombExplosionDamageMultiplier, ownerFloor);
        }
        phase = SapperPhase.Fleeing;
        fleeElapsed = 0f;
    }

    // Nunca chamados de verdade — hasAttackAnimation=false já impede o fluxo que os invocaria.
    protected override bool InAttackRange() => false;
    protected override void ExecuteAttackHit() { }

    // Animation Event no frame de explosão do próprio clipe "die" (auto-explosão ao morrer,
    // centrada no próprio Goblin Sapper).
    public void AnimationDieExplodeEvent()
    {
        var results = new Collider2D[4];
        int count = Physics2D.OverlapCircle(transform.position, dieExplosionRadius, ContactFilter2D.noFilter, results);
        for (int i = 0; i < count; i++)
        {
            if (!results[i].CompareTag("Player")) continue;
            var hero = results[i].GetComponent<HeroController>();
            if (hero != null) hero.TakeDamage(stats.attackDamage * dieExplosionDamageMultiplier);
        }
    }
}
