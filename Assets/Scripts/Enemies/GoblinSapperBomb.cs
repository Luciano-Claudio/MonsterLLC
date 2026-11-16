using UnityEngine;

// Objeto independente do Goblin Sapper (a bomba não é o Goblin Sapper) — nasce nos pés do
// jogador com sua própria animação de carga, e tem 1 Animation Event no frame exato da
// explosão. Se o jogador ainda estiver dentro do círculo NESSE frame, sofre dano — pode se
// afastar antes disso e escapar ileso (única janela de esquiva real da ficha).
public class GoblinSapperBomb : MonoBehaviour
{
    [SerializeField] private float explosionRadius = 1.5f; // 🔢 ajustável
    private float damage;
    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void Initialize(float bombDamage)
    {
        damage = bombDamage;
    }

    // Bug real reportado pelo usuário (2026-11-15): a animação da bomba (e, por tabela, o
    // Animation Event da explosão) continuava correndo em tempo real durante a pausa — esse
    // projeto nunca usa Time.timeScale pra pausar (GameplayGate é só flag lógica), então o
    // Animator de QUALQUER GameObject segue tocando sozinho a menos que algo pare ele de
    // propósito (animator.speed). Antes (comentário removido) achávamos que não precisava de
    // Update() nenhum aqui porque "o Animator já conduz o tempo sozinho" — verdade, mas isso é
    // justamente o problema: ele conduz mesmo pausado.
    private void Update()
    {
        if (animator == null) return;
        animator.speed = GameplayGate.IsActive ? 1f : 0f;
    }

    // Animation Event, no frame exato da explosão — só aplica o dano. A destruição real
    // fica pro fim de verdade do clipe (AnimationBombEndEvent), depois do estouro visual
    // já ter tocado por completo (mesmo padrão de todo AnimationXEndEvent do projeto).
    //
    // 2026-10-26 — pendência antiga (bomba "parou de funcionar", dano 0 intermitente) fechada
    // de vez: trocado Physics2D.OverlapCircle por distância direto contra o player (mesmo
    // critério agora usado em EnemyProjectile.ApplyImpactExplosion) — só existe 1 player no
    // jogo, não precisa de consulta de física nenhuma pra saber se ele está no raio.
    public void AnimationExplodeEvent()
    {
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null) return;
        if (Vector2.Distance(transform.position, playerObj.transform.position) > explosionRadius) return;

        var hero = playerObj.GetComponent<HeroController>();
        if (hero != null) hero.TakeDamage(damage);
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
