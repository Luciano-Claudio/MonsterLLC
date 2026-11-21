using UnityEngine;

// Área de explosão do Burning Skull (Bestiário Andar 5 — kamikaze) — filho do próprio
// Burning Skull. D9.4: 2 círculos — a área completa (raio `finalRadius`) e um preenchimento que
// cresce durante a animação Die, puxado por um Animation Event (ver
// BurningSkullController.AnimationExplosionGrowStartEvent).
// Puramente visual/telegraph — quem decide o dano de verdade é
// BurningSkullController.AnimationExplosionHitEvent(), que lê `FinalRadius` daqui (fonte
// única do raio real, pra nunca o visual e o dano ficarem dessincronizados).
// Sprite neutra (gradiente branco->transparente, sem outline, Explosion_Area.png em
// Sprites/Monsters/Floor5/Burning_Skull — mesmo PPU/filtro de AreaIndicatorCircle.png,
// Sprites/Effects), Sorting Layer Background; a cor vermelha vem de outerColor/innerColor,
// aplicada em runtime, não está "queimada" nos pixels. Decisão do usuário (mesmo critério já
// usado no indicador de área do Totem do Orc Shaman): nada aqui é calculado/auto-ajustado por
// código além do crescimento em si — os valores abaixo são pra calibrar à mão no Inspector.
public class BurningSkullExplosionArea : MonoBehaviour
{
    [Header("Crescimento (dispara no início do Die — ver AnimationExplosionGrowStartEvent)")]
    [SerializeField] private float finalRadius = 2f; // 🔢 também é o raio REAL de dano, ver BurningSkullController
    [SerializeField] private float growDuration = 0.6f; // 🔢 quanto tempo até o preenchimento completar a área (= instante da explosão)
    [SerializeField] private float growDelay = 0f; // 🔢 atraso opcional antes de começar a crescer
    [SerializeField] private AnimationCurve growCurve = AnimationCurve.Linear(0, 0, 1, 1); // 🔢 crescimento não-linear (ex.: acelerar perto do fim)

    [Header("Visual — 2 círculos (D9.4, formato pedido pelo usuário): área completa fraca + preenchimento que cresce")]
    [Tooltip("Círculo externo: a área completa (este SpriteRenderer, sprite neutra branca+alpha).")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color outerColor = new Color(1f, 0.15f, 0.1f, 0.22f); // 🔢 vermelho bem transparente
    [SerializeField] private Color innerColor = new Color(1f, 0.15f, 0.1f, 0.5f);  // 🔢 um pouco mais forte, ainda transparente

    [Header("Gizmo — mostra o raio REAL de dano, independente do estado atual da escala")]
    [SerializeField] private bool showDamageRadiusGizmo = true;
    [SerializeField] private Color gizmoColor = new Color(1f, 0.2f, 0.1f, 1f);

    public float FinalRadius => finalRadius;

    // D9.4 (spec D9, Seção 2.5; formato pedido pelo usuário em 2026-11-21):
    // - perto do herói (SetPreview), mostra só o círculo externo: a área completa da explosão;
    // - no clipe Die (BeginGrowth), o círculo interno nasce do centro e cresce até preencher a
    //   área no instante da explosão (growDuration, calibrado com o evento de dano do clipe).
    private Transform inner;
    private bool previewVisible;
    private float elapsed;
    private bool growing;

    public void SetPreview(bool visible)
    {
        if (growing || previewVisible == visible) return;
        previewVisible = visible;
        transform.localScale = visible ? new Vector3(finalRadius, finalRadius, 1f) : Vector3.zero;
    }

    private void Awake()
    {
        transform.localScale = Vector3.zero;
        if (spriteRenderer == null) return;
        spriteRenderer.color = outerColor;

        var innerGo = new GameObject("Fill");
        innerGo.transform.SetParent(spriteRenderer.transform, false);
        innerGo.transform.localScale = Vector3.zero;
        var innerRenderer = innerGo.AddComponent<SpriteRenderer>();
        innerRenderer.sprite = spriteRenderer.sprite;
        innerRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
        innerRenderer.sortingOrder = spriteRenderer.sortingOrder + 1;
        innerRenderer.color = innerColor;
        inner = innerGo.transform;
    }

    private void Update()
    {
        if (!growing) return;
        if (!GameplayGate.IsActive) return; // pausado: congela o crescimento

        elapsed += Time.deltaTime;
        if (elapsed < growDelay) return;

        float t = growCurve.Evaluate(TelegraphRules.Progress(elapsed - growDelay, growDuration));
        if (inner != null) inner.localScale = new Vector3(t, t, 1f);
    }

    // Chamado pelo BurningSkullController (Animation Event no início do clipe Die).
    public void BeginGrowth()
    {
        elapsed = 0f;
        growing = true;
        previewVisible = false;
        transform.localScale = new Vector3(finalRadius, finalRadius, 1f); // área completa já visível
        if (inner != null) inner.localScale = Vector3.zero;
    }

    private void OnDrawGizmos()
    {
        if (!showDamageRadiusGizmo) return;
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, finalRadius);
    }
}
