using UnityEngine;

// IA genérica de pet de início de dia (GDD: "Pets de início de dia (Mage/Blood Mage)") —
// Phoenix e Elemental de Sangue usam esta MESMA classe, só trocando prefab/Animator (arte).
// Não é EnemyController nem HeroController: sem Vida própria, sem TakeDamage — o pet nunca é
// alvo válido porque EnemyController só procura GameObject.FindGameObjectWithTag("Player")
// (conferido no código atual); contanto que este prefab NÃO tenha a tag "Player", já é
// invisível pra detecção de monstro sem nenhuma flag extra.
//
// Só 3 estados de verdade (sem Idle/Walk separados): "fly" cobre parado e se movendo ao
// mesmo tempo (Blend Tree 4 direções, sempre mostrando a última direção conhecida mesmo
// parado), "attack" e "die"/disappear (tocado quando o herói morre, não Destroy() instantâneo).
public class PetController : MonoBehaviour
{
    [SerializeField] private float chaseRadius = 6f; // 🔢 raio ao redor do DONO (não do pet) onde aceita perseguir
    [SerializeField] private float leashDistance = 2.5f; // 🔢 distância que tenta manter do dono sem alvo
    [SerializeField] private float moveSpeed = 3.5f; // 🔢
    [SerializeField] private float contactRange = 0.6f; // 🔢 alcance de contato, igual Melee comum

    [Header("Ataque — 4 hitboxes fixas por direção, igual golpe do Barbarian/Melee comum")]
    [SerializeField] private Collider2D attackHitboxNE;
    [SerializeField] private Collider2D attackHitboxNW;
    [SerializeField] private Collider2D attackHitboxSE;
    [SerializeField] private Collider2D attackHitboxSW;
    [SerializeField] private float attackCooldownDuration = 1f; // 🔢
    [SerializeField] private float damage = 2f; // 🔢 — cartas específicas (dano/velocidade/vel. ataque) chegam no Card Framework, Sprint 35

    private Transform owner;
    private Transform teleportPoint; // pra onde o pet salta quando o jogador muda de andar
    private Animator animator;
    private AttackCooldown attackCooldown;
    private Transform currentTarget;
    // Direção NE/NW/SE/SW só (Blend Tree de 4 pontos, mesmo esquema do quadrante do
    // Barbarian) — nunca fica em (0,0): precisa de um valor inicial não-zero pro Blend Tree
    // já nascer numa pose válida antes do primeiro movimento/ataque.
    private Vector2 facing = new Vector2(0.7f, -0.7f);
    private bool isAttacking;
    private bool attackHitFired;
    private bool isDead;

    public void Initialize(Transform petOwner, Transform spawnPoint)
    {
        owner = petOwner;
        teleportPoint = spawnPoint;
    }

    private void Awake()
    {
        animator = GetComponent<Animator>();
        attackCooldown = new AttackCooldown(attackCooldownDuration);
    }

    private void OnEnable()
    {
        GameEvents.OnFloorChanged += HandleFloorChanged;
    }

    private void OnDisable()
    {
        GameEvents.OnFloorChanged -= HandleFloorChanged;
    }

    // Mudar de andar teleporta o pet direto pro spawn point, em vez de deixá-lo "andando" de
    // um andar pro outro atrás do dono — o alvo também é descartado, já que um monstro do
    // andar anterior não faz mais sentido como perseguição (Combat Scope, Seção 11: pets só
    // combatem no Floor atual do jogador).
    private void HandleFloorChanged(FloorDefinition floor)
    {
        currentTarget = null;
        Vector3 destination = teleportPoint != null ? teleportPoint.position : (owner != null ? owner.position : transform.position);
        transform.position = destination;
    }

    private void Update()
    {
        if (!GameplayGate.IsActive || owner == null || isDead) return;

        attackCooldown.Tick(Time.deltaTime);
        if (isAttacking) return; // golpe em andamento -- não se move nem re-escolhe alvo

        FindTarget();

        if (currentTarget != null)
        {
            float distToTarget = Vector2.Distance(transform.position, currentTarget.position);
            if (distToTarget <= contactRange) TryAttack();
            else MoveTowards(currentTarget.position);
        }
        else
        {
            float distToOwner = Vector2.Distance(transform.position, owner.position);
            if (distToOwner > leashDistance) MoveTowards(owner.position);
            // Dentro do leash e sem alvo: fica parado, mantendo a última pose de "fly"
            // (não existe pose de "parado" separada — o Blend Tree cobre os dois casos).
        }
    }

    // Raio de perseguição centrado no DONO, não no pet (GDD, explícito) — um pet que já esteja
    // longe perseguindo não passa a "enxergar" mais longe do que o próprio herói enxergaria.
    // Trava no alvo escolhido até ele morrer de verdade (GameObject destruído) — não reavalia
    // por distância a cada frame, senão o pet fica trocando de alvo toda vez que o jogador se
    // move e um monstro diferente vira "o mais próximo". currentTarget (Transform) já vira
    // null sozinho quando o GameObject é destruído (Unity sobrecarrega "!=" pra isso).
    private void FindTarget()
    {
        if (currentTarget != null) return;

        var hits = Physics2D.OverlapCircleAll(owner.position, chaseRadius);
        float closest = float.MaxValue;
        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Enemy")) continue;
            float dist = Vector2.Distance(transform.position, hit.transform.position);
            if (dist < closest)
            {
                closest = dist;
                currentTarget = hit.transform;
            }
        }
    }

    private void MoveTowards(Vector2 destination)
    {
        Vector2 dir = (destination - (Vector2)transform.position).normalized;
        facing = dir;
        transform.position += (Vector3)(dir * moveSpeed * Time.deltaTime);
        if (animator != null)
        {
            animator.SetFloat("DirX", dir.x);
            animator.SetFloat("DirY", dir.y);
        }
    }

    private void TryAttack()
    {
        if (!attackCooldown.TryConsume()) return;

        isAttacking = true;
        attackHitFired = false;
        if (currentTarget != null) facing = (currentTarget.position - transform.position).normalized;
        if (animator != null)
        {
            animator.SetFloat("DirX", facing.x);
            animator.SetFloat("DirY", facing.y);
            animator.SetTrigger("AttackTrigger");
        }
    }

    // Animation Event -- mesmo instante exato do golpe do Barbarian/Melee comum.
    public void AnimationAttackHitEvent()
    {
        if (attackHitFired) return;
        attackHitFired = true;

        Collider2D hitbox = GetHitboxForFacing();
        if (hitbox == null) return;

        var results = new Collider2D[16];
        int count = hitbox.Overlap(ContactFilter2D.noFilter, results);
        for (int i = 0; i < count; i++)
        {
            if (!results[i].CompareTag("Enemy")) continue;
            var enemy = results[i].GetComponent<EnemyController>();
            if (enemy != null) enemy.TakeDamage(damage);
        }
    }

    // Animation Event, fim do clipe de ataque.
    public void AnimationAttackEndEvent()
    {
        isAttacking = false;
    }

    // Mesmo critério de 4 quadrantes do Barbarian.GetHitboxForFacing().
    private Collider2D GetHitboxForFacing()
    {
        bool east = facing.x >= 0f;
        bool north = facing.y >= 0f;
        if (north) return east ? attackHitboxNE : attackHitboxNW;
        return east ? attackHitboxSE : attackHitboxSW;
    }

    // Chamado pelo Mage/Blood Mage quando o herói morre (GDD: "o pet também vai para a
    // animação de desaparecer") — toca o clipe de die/disappear e só destrói o objeto quando
    // ele termina (AnimationDieEndEvent), não Destroy() instantâneo.
    public void Disappear()
    {
        if (isDead) return;
        isDead = true;
        isAttacking = false;
        if (animator != null) animator.SetTrigger("DieTrigger");
    }

    // Animation Event, no fim do clipe de die/disappear.
    public void AnimationDieEndEvent()
    {
        Destroy(gameObject);
    }
}
