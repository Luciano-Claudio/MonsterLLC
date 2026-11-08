using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

// Popup/tooltip genérico de hover (GDD Deadline 8.1, Seção 6). 2 variações, nunca as 2 juntas:
// Popup_Box (simples — Inventory/Hero Select/Mapa) e Popup_Box_With_Header (Perks, ligado a
// partir do R9 — PerkButtonUI). 1 instância de cada por Canvas (Main_Menu e a cena de gameplay
// têm a sua própria) — qualquer IPointerEnterHandler da cena chama PopupController.Instance.
//
// Segue o mouse, mas sempre ACIMA dele (nunca centrado/diagonal) — pivot do popup é o centro,
// então o X acompanha o cursor e o Y fica deslocado pra cima por meia altura do popup + um
// respiro (verticalGapAboveCursor), com clamp pra nunca vazar da tela. Mouse.current, não
// Input.mousePosition legado, mesmo critério do resto do projeto (Input System package).
// Fade (CanvasGroup.alpha) + scale juntos, sem Animator, mesmo estilo já usado no Resumo do Dia
// (ResumePainelUI.GrowNumber).
public class PopupController : MonoBehaviour
{
    public static PopupController Instance { get; private set; }

    [SerializeField] private RectTransform popupBoxPrefab;
    [SerializeField] private RectTransform popupBoxWithHeaderPrefab;
    [Tooltip("Distância vertical entre o topo do cursor e a base do popup.")]
    [SerializeField] private float verticalGapAboveCursor = 20f;
    [SerializeField] private float animDuration = 0.12f;

    // Popup_Box (simples)
    private RectTransform simpleInstance;
    private CanvasGroup simpleCanvasGroup;
    private TMP_Text simpleTitleLabel;
    private TMP_Text simpleDescriptionLabel;
    private GameObject simpleValuePrefixGO; // "Text" — rótulo estático "Value:", criado pelo usuário
    private TMP_Text simpleValueLabel; // "Value" — número dinâmico (ex. "50g"), ao lado do rótulo

    // Popup_Box_With_Header (Perks, R9)
    private RectTransform headerInstance;
    private CanvasGroup headerCanvasGroup;
    private TMP_Text headerTitleLabel;
    private TMP_Text headerLevelLabel;
    private TMP_Text headerDescriptionLabel;
    private TMP_Text headerNextLevelLabel;
    private TMP_Text headerValueLabel;

    // Qual dos 2 está visível agora — nunca os 2 ao mesmo tempo.
    private RectTransform activeInstance;
    private CanvasGroup activeCanvasGroup;
    private GameObject currentSource;
    private Coroutine animRoutine;

    private void Awake()
    {
        Instance = this;
        var canvas = GetComponentInParent<Canvas>();

        if (popupBoxPrefab != null)
        {
            simpleInstance = Instantiate(popupBoxPrefab, canvas.transform);
            simpleInstance.name = "Popup_Box_Instance";
            simpleCanvasGroup = EnsureCanvasGroup(simpleInstance);

            simpleTitleLabel = simpleInstance.Find("Title_Text")?.GetComponent<TMP_Text>();
            simpleDescriptionLabel = simpleInstance.Find("Description_Text")?.GetComponent<TMP_Text>();
            simpleValuePrefixGO = simpleInstance.Find("Text")?.gameObject; // opcional
            simpleValueLabel = simpleInstance.Find("Value")?.GetComponent<TMP_Text>(); // opcional

            simpleInstance.gameObject.SetActive(false);
        }

        if (popupBoxWithHeaderPrefab != null)
        {
            headerInstance = Instantiate(popupBoxWithHeaderPrefab, canvas.transform);
            headerInstance.name = "Popup_Box_With_Header_Instance";
            headerCanvasGroup = EnsureCanvasGroup(headerInstance);

            headerTitleLabel = headerInstance.Find("Title_Text")?.GetComponent<TMP_Text>();
            headerLevelLabel = headerInstance.Find("Level_Text")?.GetComponent<TMP_Text>();
            headerDescriptionLabel = headerInstance.Find("Description_Text")?.GetComponent<TMP_Text>();
            headerNextLevelLabel = headerInstance.Find("Next_Level_Text")?.GetComponent<TMP_Text>();
            headerValueLabel = headerInstance.Find("Value")?.GetComponent<TMP_Text>();

            headerInstance.gameObject.SetActive(false);
        }
    }

    private static CanvasGroup EnsureCanvasGroup(RectTransform rect)
    {
        var canvasGroup = rect.GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = rect.gameObject.AddComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = false; // tooltip nunca rouba hover/clique de quem está embaixo
        return canvasGroup;
    }

    private void Update()
    {
        if (activeInstance == null || !activeInstance.gameObject.activeSelf) return;

        // Rede de segurança (achado real do usuário): clicar num botão que desativa o próprio
        // painel na hora (ex.: trocar de tela) nunca dispara OnPointerExit nele — o Unity só
        // detecta "saiu do elemento" comparando o raycast de frame em frame, não quando o
        // elemento é desativado no meio do clique. Sem isto, o popup ficava preso pra sempre.
        if (currentSource == null || !currentSource.activeInHierarchy)
        {
            Hide();
            return;
        }

        if (Mouse.current == null) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        float halfWidth = activeInstance.sizeDelta.x * 0.5f;
        float halfHeight = activeInstance.sizeDelta.y * 0.5f;

        Vector2 targetPos = mousePos + new Vector2(0f, halfHeight + verticalGapAboveCursor);
        targetPos.x = Mathf.Clamp(targetPos.x, halfWidth, Screen.width - halfWidth);
        targetPos.y = Mathf.Clamp(targetPos.y, halfHeight, Screen.height - halfHeight);
        activeInstance.position = targetPos;
    }

    public void ShowSimple(GameObject source, string title, string description, string valueText = null)
    {
        if (simpleInstance == null) return;

        simpleTitleLabel.text = title;
        simpleDescriptionLabel.text = description;

        bool hasValue = valueText != null && simpleValueLabel != null;
        if (simpleValueLabel != null) simpleValueLabel.gameObject.SetActive(hasValue);
        if (simpleValuePrefixGO != null) simpleValuePrefixGO.SetActive(hasValue);
        if (hasValue) simpleValueLabel.text = valueText;

        Activate(simpleInstance, simpleCanvasGroup, source);
    }

    // Popup de Perk (Seção 6: "X/5, nome do perk, e a comparação real do efeito") — usado por
    // PerkButtonUI. levelText/nextLevelText/valueText ficam null-tolerantes porque esses campos
    // são opcionais no prefab.
    public void ShowPerk(GameObject source, string title, string levelText, string description, string nextLevelText, string valueText)
    {
        if (headerInstance == null) return;

        headerTitleLabel.text = title;
        if (headerLevelLabel != null) headerLevelLabel.text = levelText;
        headerDescriptionLabel.text = description;
        if (headerNextLevelLabel != null) headerNextLevelLabel.text = nextLevelText;

        if (headerValueLabel != null)
        {
            bool hasValue = valueText != null;
            headerValueLabel.gameObject.SetActive(hasValue);
            if (hasValue) headerValueLabel.text = valueText;
        }

        Activate(headerInstance, headerCanvasGroup, source);
    }

    private void Activate(RectTransform instance, CanvasGroup canvasGroup, GameObject source)
    {
        currentSource = source;

        // Se o OUTRO popup estava visível (ex.: mouse saiu de um perk e entrou direto num item
        // de inventário), esconde ele na hora, sem animação — só 1 popup por vez.
        if (activeInstance != null && activeInstance != instance && activeInstance.gameObject.activeSelf)
        {
            activeInstance.gameObject.SetActive(false);
            activeCanvasGroup.alpha = 0f;
        }

        if (animRoutine != null) StopCoroutine(animRoutine);
        if (!instance.gameObject.activeSelf)
        {
            canvasGroup.alpha = 0f;
            instance.localScale = new Vector3(0.8f, 0.8f, 1f);
        }
        instance.gameObject.SetActive(true);

        activeInstance = instance;
        activeCanvasGroup = canvasGroup;
        animRoutine = StartCoroutine(AnimateTo(canvasGroup, instance, 1f, 1f, deactivateAtEnd: false));
    }

    public void Hide()
    {
        currentSource = null;
        if (activeInstance == null || !activeInstance.gameObject.activeSelf) return;
        if (animRoutine != null) StopCoroutine(animRoutine);
        animRoutine = StartCoroutine(AnimateTo(activeCanvasGroup, activeInstance, 0f, 0.8f, deactivateAtEnd: true));
    }

    private IEnumerator AnimateTo(CanvasGroup canvasGroup, RectTransform rect, float targetAlpha, float targetScale, bool deactivateAtEnd)
    {
        float startAlpha = canvasGroup.alpha;
        float startScale = rect.localScale.x;
        float t = 0f;
        while (t < animDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / animDuration);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, p);
            float scale = Mathf.Lerp(startScale, targetScale, p);
            rect.localScale = new Vector3(scale, scale, 1f);
            yield return null;
        }
        canvasGroup.alpha = targetAlpha;
        rect.localScale = new Vector3(targetScale, targetScale, 1f);
        if (deactivateAtEnd) rect.gameObject.SetActive(false);
    }

    // Monta "<valor antigo riscado> -> <valor novo>" (ex.: preço de item subindo por um perk) —
    // usa a tag nativa de riscado do TMP (<s>), já que Rich Text já vem ligado nesses prefabs.
    public static string FormatValueChange(long oldValue, long newValue) =>
        $"<s>{LargeNumberFormatter.Format(oldValue)}</s> → {LargeNumberFormatter.Format(newValue)}";
}
