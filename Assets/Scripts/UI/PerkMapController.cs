using UnityEngine;
using UnityEngine.EventSystems;

// Área de pan+zoom da Árvore de Perks (GDD Seção 6/6.1) — Content guarda os ~120 nós de perk em
// posições fixas; o jogador arrasta (OnDrag) e dá zoom no scroll (OnScroll), sempre travado dentro
// dos limites do Viewport. Este script fica no próprio Viewport — precisa de um Graphic com
// raycastTarget=true nele (Image/Mask do Viewport, já existente) pra receber os eventos do
// EventSystem. Não exige RectMask2D de propósito — Mask (stencil clássico) clipa igual.
public class PerkMapController : MonoBehaviour, IDragHandler, IScrollHandler
{
    [SerializeField] private RectTransform content; // filho direto deste Viewport, guarda os nós de perk
    [SerializeField] private float minZoom = 0.5f;
    [SerializeField] private float maxZoom = 2f;
    [SerializeField] private float defaultZoom = 1f;
    [SerializeField] private float scrollZoomSpeed = 0.1f; // %/notch do scroll

    private RectTransform viewport; // este próprio RectTransform — define a área visível (tem o RectMask2D)
    private Canvas canvas;

    private void Awake()
    {
        viewport = (RectTransform)transform;
        canvas = GetComponentInParent<Canvas>();
    }

    // Reabre sempre centralizado no zoom padrão — mesmo critério do InventoryPanelController
    // (nunca lembra estado de uma visita anterior à tela).
    private void OnEnable() => ResetView();

    // Cada herói tem o próprio Content agora (R10 — ver PerkTreeController.ResolveHeroContent),
    // então o Content "fixo" arrastado no Inspector deixou de existir — achado real do
    // usuário: continuava apontando pro Content ORIGINAL (renomeado "Assassin" na duplicação),
    // que fica desativado pra sempre, então arrastar/zoom parou de fazer efeito em nada visível.
    // PerkTreeController chama isto 1x, logo depois de descobrir qual Content é do herói atual.
    public void SetContent(RectTransform newContent)
    {
        content = newContent;
        if (isActiveAndEnabled) ResetView();
    }

    public void ResetView()
    {
        if (content == null) return;
        content.localScale = Vector3.one * defaultZoom;
        // anchoredPosition = 0 só centraliza o Content se o pivot dele for (0.5, 0.5). O Content
        // real usa pivot (0, 1) (canto superior esquerdo, pra facilitar posicionar os 121 nós em
        // coordenadas positivas) — por isso o cálculo via PivotOffset() em vez de Vector2.zero
        // (achado real, usuário reportou o Content pulando pro canto ao entrar em Play).
        content.anchoredPosition = -PivotToCenterOffset();
        ClampContentPosition();
    }

    // Vetor do PIVOT até o CENTRO visual do Content (ex.: pivot (0,1) = canto superior esquerdo →
    // centro fica à direita e pra baixo, por isso width positivo e height negativo). Somar isso ao
    // anchoredPosition dá a posição do centro no espaço do pai; zero quando o pivot já é (0.5, 0.5).
    private Vector2 PivotToCenterOffset()
    {
        return (new Vector2(0.5f, 0.5f) - content.pivot) * content.rect.size * content.localScale.x;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (content == null) return;

        // delta vem em pixels de tela — scaleFactor converte pra unidades do Canvas. Dividir só
        // por scaleFactor (nunca pelo localScale do Content) é o que faz o arrasto acompanhar o
        // mouse 1:1 em qualquer nível de zoom: anchoredPosition vive no espaço do PAI (Viewport),
        // que não é afetado pela escala do próprio Content.
        content.anchoredPosition += eventData.delta / canvas.scaleFactor;
        ClampContentPosition();
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (content == null || Mathf.Approximately(eventData.scrollDelta.y, 0f)) return;

        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        RectTransformUtility.ScreenPointToWorldPointInRectangle(content, eventData.position, cam, out Vector3 worldPointBeforeZoom);
        // InverseTransformPoint já divide pela escala ATUAL — o resultado é a coordenada do Content
        // num "grid" fixo que não muda quando a escala muda a seguir.
        Vector3 localPoint = content.InverseTransformPoint(worldPointBeforeZoom);

        float newZoom = Mathf.Clamp(content.localScale.x * (1f + eventData.scrollDelta.y * scrollZoomSpeed), minZoom, maxZoom);
        content.localScale = Vector3.one * newZoom;

        // Mesmo ponto local, agora na escala NOVA — a diferença entre onde ele caiu e onde estava
        // antes é exatamente o quanto o Content precisa deslocar pra manter esse ponto embaixo do
        // cursor (zoom no cursor, não no centro da tela).
        Vector3 worldPointAfterZoom = content.TransformPoint(localPoint);
        content.position += worldPointBeforeZoom - worldPointAfterZoom;

        ClampContentPosition();
    }

    // Trava o Content pra nunca sobrar espaço vazio dentro do Viewport. Funciona com QUALQUER
    // pivot: converte anchoredPosition pro centro visual do Content, clampa o centro, e converte
    // de volta — em vez de assumir pivot (0.5, 0.5) como antes.
    private void ClampContentPosition()
    {
        Vector2 offsetToCenter = PivotToCenterOffset();
        Vector2 centerPos = content.anchoredPosition + offsetToCenter;

        Vector2 contentSize = content.rect.size * content.localScale.x;
        Vector2 viewportSize = viewport.rect.size;

        float maxX = Mathf.Max(0f, (contentSize.x - viewportSize.x) * 0.5f);
        float maxY = Mathf.Max(0f, (contentSize.y - viewportSize.y) * 0.5f);

        centerPos.x = Mathf.Clamp(centerPos.x, -maxX, maxX);
        centerPos.y = Mathf.Clamp(centerPos.y, -maxY, maxY);
        content.anchoredPosition = centerPos - offsetToCenter;
    }
}
