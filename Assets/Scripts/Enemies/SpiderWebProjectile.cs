using UnityEngine;

// Teia da Spider Queen (Bestiário — exceção pontual). Voa reto, rotacionado de verdade na
// direção real (mesma técnica de todo projétil do projeto) — sem fase de impacto própria:
// ao alcançar o limite ou acertar o player, some na hora. Causa o Efeito Ice (decisão do
// usuário: "o mesmo do freeze") só quando acerta de verdade, nunca por alcançar o limite —
// sem dano nenhum, só a incapacitação (ver HeroController.Update(), que converte Ice ativo
// em SetTrapped() de verdade). A arte do ícone de Ice ainda não existe — o usuário vai
// clonar/ajustar o visual de outro Efeito depois; o código já referencia o tipo certo do
// enum, não precisa de mudança quando a arte chegar.
public class SpiderWebProjectile : MonoBehaviour
{
    [SerializeField] private float speed = 7f; // 🔢 ajustável
    [SerializeField] private float maxDistance = 8f; // 🔢 ajustável
    [SerializeField] private float trapDuration = 3f; // 🔢 Bestiário: "não consegue andar por alguns segundos"

    private Vector2 direction;
    private FloorDefinition ownerFloor;
    private float distanceTraveled;
    private bool done;

    public void Launch(Vector2 dir, FloorDefinition floor)
    {
        direction = dir.normalized;
        ownerFloor = floor;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void Update()
    {
        if (!GameplayGate.IsActive) return;
        if (!FloorActivationCheck.IsActive(ownerFloor, FloorManager.Instance.CurrentFloor)) return;
        if (done) return;

        float step = speed * Time.deltaTime;
        transform.position += (Vector3)(direction * step);
        distanceTraveled += step;
        if (distanceTraveled >= maxDistance) Vanish(); // alcançou o limite sem acertar — some sem causar efeito
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (done) return;
        if (!other.CompareTag("Player")) return;

        var hero = other.GetComponent<HeroController>();
        if (hero != null)
        {
            var statusController = hero.GetComponent<StatusEffectController>();
            // damagePerSecond: 0 — Ice aqui é só incapacitação (GDD Seção 33), sem DoT. O
            // ícone (quando a arte existir) fica em cima disso automaticamente, mesmo
            // critério de qualquer outro Efeito.
            if (statusController != null) statusController.ApplyStatusEffect(StatusEffectType.Ice, trapDuration, 0f);
        }

        Vanish();
    }

    private void Vanish()
    {
        if (done) return;
        done = true;
        Destroy(gameObject);
    }
}
