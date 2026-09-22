using UnityEngine;

// Objeto independente do Goblin Sapper (a bomba não é o Goblin Sapper) — nasce nos pés do
// jogador com sua própria animação de carga, e tem 1 Animation Event no frame exato da
// explosão. Se o jogador ainda estiver dentro do círculo NESSE frame, sofre dano — pode se
// afastar antes disso e escapar ileso (única janela de esquiva real da ficha).
public class GoblinSapperBomb : MonoBehaviour
{
    [SerializeField] private float explosionRadius = 1.5f; // 🔢 ajustável
    private float damage;
    private FloorDefinition ownerFloor;

    public void Initialize(float bombDamage, FloorDefinition floor)
    {
        damage = bombDamage;
        ownerFloor = floor;
    }

    private void Update()
    {
        // Respeita Floor Sleep como qualquer outra entidade com timing próprio (ex.:
        // EnemyProjectile) — o Animator já conduz o tempo até a explosão sozinho via
        // Animation Event; este Update() só existe pro early-return ficar no mesmo padrão.
        if (!GameplayGate.IsActive) return;
        if (!FloorActivationCheck.IsActive(ownerFloor, FloorManager.Instance.CurrentFloor)) return;
    }

    // Animation Event, no frame exato da explosão — só aplica o dano. A destruição real
    // fica pro fim de verdade do clipe (AnimationBombEndEvent), depois do estouro visual
    // já ter tocado por completo (mesmo padrão de todo AnimationXEndEvent do projeto).
    public void AnimationExplodeEvent()
    {
        var results = new Collider2D[4];
        int count = Physics2D.OverlapCircle(transform.position, explosionRadius, ContactFilter2D.noFilter, results);
        for (int i = 0; i < count; i++)
        {
            if (!results[i].CompareTag("Player")) continue;
            var hero = results[i].GetComponent<HeroController>();
            if (hero != null) hero.TakeDamage(damage);
        }
    }

    // Animation Event, no último frame do clipe — só agora o GameObject é destruído de
    // verdade.
    public void AnimationBombEndEvent()
    {
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
