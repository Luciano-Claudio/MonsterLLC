using UnityEngine;

public abstract class EnemyController : MonoBehaviour
{
    public EnemyStats stats = new EnemyStats();
    public AttackTiming attackTiming = new AttackTiming();
    public int energyReward = 20;
    public int monsterEssenceDropAmount = 1; // quantidade dropada por abate (GDD Seção 38, 🔢 valor de balanceamento pendente)
    public FloorDefinition ownerFloor;

    protected Transform player;
    private float lastAttackTime = -999f;
    private bool isDead;

    private enum AttackState { Idle, Telegraph, Active, Recovery }
    private AttackState attackState = AttackState.Idle;
    private float attackStateTimer;

    protected virtual void Start()
    {
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;
    }

    protected virtual void Update()
    {
        if (!GameplayGate.IsActive) return;
        if (!FloorActivationCheck.IsActive(ownerFloor, FloorManager.Instance.CurrentFloor)) return;
        if (isDead || player == null) return;

        if (attackState != AttackState.Idle)
        {
            UpdateAttackState();
            return; // travado durante o próprio ataque — não persegue nem re-ataca no meio do timing
        }

        float distance = Vector2.Distance(transform.position, player.position);

        if (distance <= stats.attackRadius) TryStartAttack();
        else if (distance <= stats.observationRadius) Move();
    }

    protected abstract void Move();
    protected abstract void ExecuteHit(); // dano de verdade acontece aqui — no instante certo do timing, não no início do ataque
    protected abstract AttackType AttackType { get; }

    private void TryStartAttack()
    {
        if (Time.time - lastAttackTime < stats.attackCooldown) return;
        if (!AttackBudgetManager.Instance.TryReserveSlot(ownerFloor, AttackType)) return; // sem slot — fica esperando, tenta de novo no próximo frame
        lastAttackTime = Time.time;
        attackState = AttackState.Telegraph;
        attackStateTimer = 0f;
        Debug.Log($"[{GetType().Name}] Telegraph...");
    }

    private void UpdateAttackState()
    {
        attackStateTimer += Time.deltaTime;

        switch (attackState)
        {
            case AttackState.Telegraph:
                if (attackStateTimer >= attackTiming.telegraphDuration)
                {
                    ExecuteHit();
                    attackState = AttackState.Active;
                    attackStateTimer = 0f;
                }
                break;

            case AttackState.Active:
                if (attackStateTimer >= attackTiming.hitboxActiveDuration)
                {
                    attackState = AttackState.Recovery;
                    attackStateTimer = 0f;
                }
                break;

            case AttackState.Recovery:
                if (attackStateTimer >= attackTiming.recoveryDuration)
                {
                    attackState = AttackState.Idle;
                    AttackBudgetManager.Instance.ReleaseSlot(ownerFloor, AttackType);
                }
                break;
        }
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;
        stats.health = HealthSystem.ApplyDamage(stats.health, amount);
        Debug.Log($"[{GetType().Name}] Recebeu {amount} de dano. HP = {stats.health}/{stats.maxHealth}");
        if (HealthSystem.IsDead(stats.health)) Die();
    }

    protected virtual void Die()
    {
        isDead = true;
        Debug.Log($"[{GetType().Name}] Morreu.");

        // Morreu no meio do próprio ataque (Telegraph/Active/Recovery) — sem isso o
        // slot do AttackBudgetManager nunca seria liberado (vazamento permanente).
        if (attackState != AttackState.Idle) AttackBudgetManager.Instance.ReleaseSlot(ownerFloor, AttackType);

        GameEvents.EnemyKilled(energyReward);

        var lootObj = new GameObject("Loot_MonsterEssence");
        lootObj.transform.position = transform.position;
        var drop = lootObj.AddComponent<LootDrop>();
        drop.loot = new LootDefinition { itemName = "Monster Essence", quantity = monsterEssenceDropAmount };

        Destroy(gameObject);
    }
}
