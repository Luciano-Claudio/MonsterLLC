using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    public Vector2 direction;
    public float speed = 6f;
    public float damage = 5f;
    public float lifetime = 3f;
    public FloorDefinition ownerFloor;

    private float timer;

    private void Update()
    {
        if (!GameplayGate.IsActive) return;
        if (!FloorActivationCheck.IsActive(ownerFloor, FloorManager.Instance.CurrentFloor)) return;

        transform.Translate(direction * speed * Time.deltaTime);
        timer += Time.deltaTime;
        if (timer >= lifetime) Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        other.GetComponent<HeroController>()?.TakeDamage(damage);
        Destroy(gameObject);
    }
}
