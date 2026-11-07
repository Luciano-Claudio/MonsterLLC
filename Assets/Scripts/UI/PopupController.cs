using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

// Popup/tooltip genérico de hover (GDD Deadline 8.1, Seção 6). Só Popup_Box por agora
// (Inventory + Hero Select, confirmados com o usuário) — Popup_Box_With_Header entra quando a
// infra de Perks existir (R9). 1 instância por Canvas (Main_Menu e a cena de gameplay têm a
// sua própria) — qualquer IPointerEnterHandler da cena chama PopupController.Instance.
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
    [Tooltip("Distância vertical entre o topo do cursor e a base do popup.")]
    [SerializeField] private float verticalGapAboveCursor = 20f;
    [SerializeField] private float animDuration = 0.12f;

    private RectTransform popupInstance;
    private CanvasGroup canvasGroup;
    private TMP_Text titleLabel;
    private TMP_Text descriptionLabel;
    private GameObject valuePrefixGO; // "Text" — rótulo estático "Value:", criado pelo usuário
    private TMP_Text valueLabel; // "Value" — número dinâmico (ex. "50g"), ao lado do rótulo
    private Coroutine animRoutine;
    private GameObject currentSource;

    private void Awake()
    {
        Instance = this;

        var canvas = GetComponentInParent<Canvas>();
        popupInstance = Instantiate(popupBoxPrefab, canvas.transform);
        popupInstance.name = "Popup_Box_Instance";

        canvasGroup = popupInstance.GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = popupInstance.gameObject.AddComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = false; // tooltip nunca rouba hover/clique de quem está embaixo

        titleLabel = popupInstance.Find("Title_Text")?.GetComponent<TMP_Text>();
        descriptionLabel = popupInstance.Find("Description_Text")?.GetComponent<TMP_Text>();
        valuePrefixGO = popupInstance.Find("Text")?.gameObject; // opcional — só existe no Popup_Box
        valueLabel = popupInstance.Find("Value")?.GetComponent<TMP_Text>(); // opcional

        popupInstance.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!popupInstance.gameObject.activeSelf) return;

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
        float halfWidth = popupInstance.sizeDelta.x * 0.5f;
        float halfHeight = popupInstance.sizeDelta.y * 0.5f;

        Vector2 targetPos = mousePos + new Vector2(0f, halfHeight + verticalGapAboveCursor);
        targetPos.x = Mathf.Clamp(targetPos.x, halfWidth, Screen.width - halfWidth);
        targetPos.y = Mathf.Clamp(targetPos.y, halfHeight, Screen.height - halfHeight);
        popupInstance.position = targetPos;
    }

    public void ShowSimple(GameObject source, string title, string description, string valueText = null)
    {
        currentSource = source;
        titleLabel.text = title;
        descriptionLabel.text = description;

        bool hasValue = valueText != null && valueLabel != null;
        if (valueLabel != null) valueLabel.gameObject.SetActive(hasValue);
        if (valuePrefixGO != null) valuePrefixGO.SetActive(hasValue);
        if (hasValue) valueLabel.text = valueText;

        if (animRoutine != null) StopCoroutine(animRoutine);
        if (!popupInstance.gameObject.activeSelf)
        {
            canvasGroup.alpha = 0f;
            popupInstance.localScale = new Vector3(0.8f, 0.8f, 1f);
        }
        popupInstance.gameObject.SetActive(true);
        animRoutine = StartCoroutine(AnimateTo(1f, 1f, deactivateAtEnd: false));
    }

    public void Hide()
    {
        currentSource = null;
        if (!popupInstance.gameObject.activeSelf) return;
        if (animRoutine != null) StopCoroutine(animRoutine);
        animRoutine = StartCoroutine(AnimateTo(0f, 0.8f, deactivateAtEnd: true));
    }

    private IEnumerator AnimateTo(float targetAlpha, float targetScale, bool deactivateAtEnd)
    {
        float startAlpha = canvasGroup.alpha;
        float startScale = popupInstance.localScale.x;
        float t = 0f;
        while (t < animDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / animDuration);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, p);
            float scale = Mathf.Lerp(startScale, targetScale, p);
            popupInstance.localScale = new Vector3(scale, scale, 1f);
            yield return null;
        }
        canvasGroup.alpha = targetAlpha;
        popupInstance.localScale = new Vector3(targetScale, targetScale, 1f);
        if (deactivateAtEnd) popupInstance.gameObject.SetActive(false);
    }

    // Monta "<valor antigo riscado> -> <valor novo>" (ex.: preço de item subindo por um perk,
    // R9+) — usa a tag nativa de riscado do TMP (<s>), já que Rich Text já vem ligado nesses
    // prefabs. Ninguém chama isto ainda (Perks não existe); existe pronto pro Value opcional.
    public static string FormatValueChange(long oldValue, long newValue) =>
        $"<s>{LargeNumberFormatter.Format(oldValue)}</s> → {LargeNumberFormatter.Format(newValue)}";
}
