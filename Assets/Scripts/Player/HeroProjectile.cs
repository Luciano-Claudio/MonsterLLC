using UnityEngine;

// Projétil reto de herói (GDD Seção 13) — carrega uma reserva de dano em vez de aplicar o
// dano cheio de uma vez: ao acertar um monstro, gasta da reserva só o mínimo entre ela e a
// vida do alvo, e continua a mesma trajetória se sobrar. Diferente do projétil de monstro
// comum (EnemyProjectile), que aplica dano cheio e é destruído no primeiro contato.
public class HeroProjectile : MonoBehaviour
{
    [SerializeField] private float speed = 10f;
    [SerializeField] private float maxDistance = 8f; // 🔢 alcance — placeholder de balanceamento
    [SerializeField] private float knockbackForce = 4f; // 🔢 ajustável

    // Opcional — 8 estados soltos (N/NE/E/SE/S/SW/W/NW), sem Blend Tree e sem parâmetro
    // nenhum: a direção não muda depois do disparo, então Launch() só dá Play() direto pelo
    // nome do estado uma vez, sem passar pelo grafo de transições.
    private Animator animator;

    private Vector2 direction;
    private float damageReserve;
    private float distanceTraveled;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void Launch(Vector2 dir, float reserve)
    {
        direction = dir.normalized;
        damageReserve = reserve;
        if (animator != null) animator.Play(DirectionUtility.GetDirectionName(direction));
    }

    private void Update()
    {
        if (!GameplayGate.IsActive) return;

        float step = speed * Time.deltaTime;
        transform.Translate(direction * step);
        distanceTraveled += step;
        if (distanceTraveled >= maxDistance) Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Enemy")) return;

        var enemy = other.GetComponent<EnemyController>();
        if (enemy == null) return;

        float damageDealt = Mathf.Min(damageReserve, enemy.stats.health);
        enemy.TakeDamage(damageDealt);
        enemy.ApplyKnockback(direction, knockbackForce);

        damageReserve -= damageDealt;
        if (damageReserve <= 0f) Destroy(gameObject);
    }
}
