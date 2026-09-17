using UnityEngine;

public class RangerArrow : MonoBehaviour
{
    [SerializeField] private float speed = 10f;
    [SerializeField] private float lifetime = 2f; // 🔢 alcance efetivo da flecha — placeholder de balanceamento

    private Vector2 direction;
    private float damage;
    private float timer;

    public void Launch(Vector2 dir, float dmg)
    {
        direction = dir.normalized;
        damage = dmg;
    }

    private void Update()
    {
        if (!GameplayGate.IsActive) return;

        transform.Translate(direction * speed * Time.deltaTime);
        timer += Time.deltaTime;
        if (timer >= lifetime) Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Enemy")) return;

        var enemy = other.GetComponent<EnemyController>();
        if (enemy != null) enemy.TakeDamage(damage);

        // Sem perfuração — GDD Seção 13 permite as duas variantes pra Straight Projectile,
        // não confirma qual o Ranger usa. Assumindo sem perfuração até decisão explícita. 🔢
        Destroy(gameObject);
    }
}
