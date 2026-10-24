using UnityEngine;

// Área de explosão do Burning Skull (Bestiário Andar 5 — kamikaze) — filho do próprio
// Burning Skull, nasce em escala 0 e cresce (m_Scale) até `finalRadius` durante a animação
// Die, puxada por um Animation Event (ver BurningSkullController.AnimationExplosionGrowStartEvent).
// Puramente visual/telegraph — quem decide o dano de verdade é
// BurningSkullController.AnimationExplosionHitEvent(), que lê `FinalRadius` daqui (fonte
// única do raio real, pra nunca o visual e o dano ficarem dessincronizados).
// Sprite neutra (gradiente branco->transparente, sem outline, Explosion_Area.png em
// Sprites/Monsters/Floor5/Burning_Skull — mesmo PPU/filtro de AreaIndicatorCircle.png,
// Sprites/Effects), Sorting Layer Background; a cor vermelha vem do `areaColor` aplicado
// em runtime, não está "queimada" nos pixels — dá pra reusar a mesma sprite com outra cor
// se precisar depois. Decisão do usuário (mesmo critério já usado no indicador de área do
// Totem do Orc Shaman): nada aqui é calculado/auto-ajustado por código além do crescimento
// em si — os valores abaixo são pra calibrar à mão no Inspector.
public class BurningSkullExplosionArea : MonoBehaviour
{
    [Header("Crescimento (dispara no início do Die — ver AnimationExplosionGrowStartEvent)")]
    [SerializeField] private float finalRadius = 2f; // 🔢 também é o raio REAL de dano, ver BurningSkullController
    [SerializeField] private float growDuration = 0.6f; // 🔢 quanto tempo até atingir finalRadius
    [SerializeField] private float growDelay = 0f; // 🔢 atraso opcional antes de começar a crescer
    [SerializeField] private AnimationCurve growCurve = AnimationCurve.Linear(0, 0, 1, 1); // 🔢 crescimento não-linear (ex.: acelerar perto do fim)

    [Header("Visual — sprite neutra (branco+alpha), cor aplicada aqui em runtime")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color areaColor = new Color(1f, 0.15f, 0.1f, 0.75f); // 🔢 vermelho puxado pro transparente

    [Header("Gizmo — mostra o raio REAL de dano, independente do estado atual da escala")]
    [SerializeField] private bool showDamageRadiusGizmo = true;
    [SerializeField] private Color gizmoColor = new Color(1f, 0.2f, 0.1f, 1f);

    public float FinalRadius => finalRadius;

    private float elapsed;
    private bool growing;

    private void Awake()
    {
        transform.localScale = Vector3.zero;
        if (spriteRenderer != null) spriteRenderer.color = areaColor;
    }

    private void Update()
    {
        if (!growing) return;

        elapsed += Time.deltaTime;
        if (elapsed < growDelay) return;

        float t = growDuration <= 0f ? 1f : Mathf.Clamp01((elapsed - growDelay) / growDuration);
        float scale = finalRadius * growCurve.Evaluate(t);
        transform.localScale = new Vector3(scale, scale, 1f);
    }

    // Chamado pelo BurningSkullController (Animation Event no início do clipe Die).
    public void BeginGrowth()
    {
        elapsed = 0f;
        growing = true;
    }

    private void OnDrawGizmos()
    {
        if (!showDamageRadiusGizmo) return;
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, finalRadius);
    }
}
