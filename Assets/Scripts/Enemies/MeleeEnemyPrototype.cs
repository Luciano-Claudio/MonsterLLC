using UnityEngine;

public class MeleeEnemyPrototype : MonoBehaviour
{
    public EnemyStats stats = new EnemyStats();
    public int energyReward = 20; // quanto de Energia concede ao matar (GDD Seção 11, 🔢 valor real pendente)

    private Transform player;
    private float lastAttackTime = -999f;

    private void Start()
    {
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;
    }

    private void Update()
    {
        if (TimeManager.Instance != null && TimeManager.Instance.IsPaused) return;
        if (player == null || HealthSystem.IsDead(stats.health)) return;

        float distance = Vector2.Distance(transform.position, player.position);

        if (distance <= stats.attackRadius) TryAttack();
        else if (distance <= stats.observationRadius) ChasePlayer();
    }

    private void ChasePlayer()
    {
        Vector2 direction = (player.position - transform.position).normalized;
        transform.Translate(direction * stats.moveSpeed * Time.deltaTime);
    }

    private void TryAttack()
    {
        if (Time.time - lastAttackTime < stats.attackCooldown) return;
        lastAttackTime = Time.time;

        var hero = player.GetComponent<HeroController>();
        if (hero == null) return;

        Debug.Log($"[MeleeEnemyPrototype] Ataca o herói por {stats.damage}.");
        hero.TakeDamage(stats.damage);
    }

    public void TakeDamage(float amount)
    {
        stats.health = HealthSystem.ApplyDamage(stats.health, amount);
        Debug.Log($"[MeleeEnemyPrototype] Recebeu {amount} de dano. HP = {stats.health}/{stats.maxHealth}");

        if (HealthSystem.IsDead(stats.health)) Die();
    }

    private void Die()
    {
        Debug.Log("[MeleeEnemyPrototype] Morreu.");
        GameEvents.EnemyKilled(energyReward);
        Destroy(gameObject);
    }
}
