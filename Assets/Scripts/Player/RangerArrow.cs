using UnityEngine;

public class RangerArrow : MonoBehaviour
{
    [SerializeField] private float speed = 10f;
    [SerializeField] private float lifetime = 2f; // 🔢 alcance efetivo da flecha — placeholder de balanceamento
    [SerializeField] private float knockbackForce = 4f; // 🔢 ajustável — GDD Seção 17.2

    // 1 sprite só, rotacionado de verdade em vez de trocar de pose (Animator/Blend Tree
    // removido) — a flecha voa em direção livre (RawAimDirection, não travada nas 8
    // direções), então só rotação contínua acompanha o ângulo exato sem parecer estranho.
    // Bônus: o collider (mesmo Transform) gira junto com o sprite, então fica sempre
    // alinhado com o visual, não importa a direção — antes, com sprite fixo trocando de
    // pose, o collider "justinho" só batia certo com a pose Norte original.
    private Vector2 direction;

    // Reserva de perfuração (GDD Seção 13/17.2) — mesmo mecanismo do HeroProjectile do
    // Barbarian: ao acertar, gasta só o mínimo entre a reserva e a vida do alvo, e continua
    // a mesma trajetória se sobrar, em vez de morrer no primeiro contato.
    private float damageReserve;
    private float timer;

    public void Launch(Vector2 dir, float reserve)
    {
        direction = dir.normalized;
        damageReserve = reserve;

        // Sprite de referência nasce apontando pra "cima" (N, +Y) — por isso o -90°: sem
        // ele, ângulo 0 (Leste) deixaria o sprite ainda apontando pra cima em vez de deitado
        // na horizontal.
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void Update()
    {
        if (!GameplayGate.IsActive) return;

        // position direto (espaço de mundo), não Translate — Translate usa espaço LOCAL por
        // padrão, e agora que a flecha está rotacionada os eixos locais giraram junto,
        // fazendo "direction" (um vetor de mundo) apontar pro lado errado.
        transform.position += (Vector3)(direction * speed * Time.deltaTime);
        timer += Time.deltaTime;
        if (timer >= lifetime) Destroy(gameObject);
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
