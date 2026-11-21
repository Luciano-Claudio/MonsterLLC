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

    // D9.4 (spec D9, Seção 2.5; formato pedido pelo usuário em 2026-11-21) — aviso em 2 círculos:
    // a área completa (raio real) desde o plantio, e um preenchimento que cresce do centro e
    // completa no frame exato do AnimationExplodeEvent (tempo lido do próprio clipe). Some na
    // explosão. Sprite/sorting iguais à área do Burning Skull.
    [Header("Aviso de área (D9.4)")]
    [SerializeField] private Sprite warningSprite;
    [SerializeField] private int warningSortingLayerId;
    [SerializeField] private int warningSortingOrder = 0;
    [SerializeField] private Color warningColor = new Color(1f, 0.15f, 0.1f, 0.22f);    // 🔢 área completa, bem transparente
    [SerializeField] private Color warningFillColor = new Color(1f, 0.15f, 0.1f, 0.5f); // 🔢 preenchimento, um pouco mais forte
    private ExplosionWarningArea warning;
    private float explodeAtSeconds; // tempo do AnimationExplodeEvent no clipe (0 = não achou: aviso já cheio)

    private void Awake()
    {
        animator = GetComponent<Animator>();
        explodeAtSeconds = FindExplodeEventTime();
        if (warningSprite != null)
        {
            warning = ExplosionWarningArea.Create(transform, warningSprite, warningSortingLayerId, warningSortingOrder, warningColor, warningFillColor);
            warning.Show(explosionRadius);
            warning.SetProgress(0f);
        }
    }

    // 1x por bomba (Awake) — procura nos clipes do controller o evento de explosão.
    private float FindExplodeEventTime()
    {
        if (animator == null || animator.runtimeAnimatorController == null) return 0f;
        foreach (var clip in animator.runtimeAnimatorController.animationClips)
            foreach (var ev in clip.events)
                if (ev.functionName == nameof(AnimationExplodeEvent)) return ev.time;
        return 0f;
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
        if (warning != null && warning.gameObject.activeSelf)
        {
            var state = animator.GetCurrentAnimatorStateInfo(0);
            warning.SetProgress(TelegraphRules.Progress(state.normalizedTime * state.length, explodeAtSeconds));
        }
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
        if (warning != null) warning.Hide(); // D9.4 — explodiu: o aviso some
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
