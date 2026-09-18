using UnityEngine;

// Script genérico de projétil reto de monstro — qualquer Ranged usa esse mesmo componente
// no prefab do próprio projétil (RangedEnemyController chama Launch() no Animation Event de
// disparo). 1 sprite só, rotacionado de verdade em vez de trocar de pose entre 8 variações —
// mesmo padrão do RangerArrow: rotação contínua acompanha o ângulo exato sem parecer
// estranho, e o collider (mesmo Transform) gira junto, sempre alinhado com o visual.
public class EnemyProjectile : MonoBehaviour
{
    public float speed = 6f;
    public float damage = 5f;
    public float lifetime = 3f;
    public FloorDefinition ownerFloor;

    private Vector2 direction;
    private float timer;

    public void Launch(Vector2 dir, float dmg)
    {
        direction = dir.normalized;
        damage = dmg;

        // Sprite de referência nasce apontando pra "cima" (N, +Y) — por isso o -90°: sem
        // ele, ângulo 0 (Leste) deixaria o sprite ainda apontando pra cima em vez de deitado
        // na horizontal.
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void Update()
    {
        if (!GameplayGate.IsActive) return;
        if (!FloorActivationCheck.IsActive(ownerFloor, FloorManager.Instance.CurrentFloor)) return;

        // position direto (espaço de mundo), não Translate — Translate usa espaço LOCAL por
        // padrão, e com a flecha rotacionada os eixos locais giram junto, fazendo
        // "direction" (um vetor de mundo) apontar pro lado errado.
        transform.position += (Vector3)(direction * speed * Time.deltaTime);
        timer += Time.deltaTime;
        if (timer >= lifetime) Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        var hero = other.GetComponent<HeroController>();
        if (hero != null) hero.TakeDamage(damage);

        Destroy(gameObject);
    }
}
