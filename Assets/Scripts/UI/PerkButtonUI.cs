using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 1 botão de perk (GDD Deadline 8.1, Seção 6/7.3/8-38, R9) — criado em runtime via AddComponent
// por PerkTreeController (mesmo critério do InventorySlotUI: os ~121 GameObjects já existem na
// hierarquia, feitos à mão pelo usuário; isso só liga o comportamento neles). Vive no GameObject
// RAIZ do perk (ex. "World_Background_Perk (1)"), não no "Perk_Button" filho — o Button de
// verdade mora 1 nível abaixo, dentro de "Perk_Button".
public class PerkButtonUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private PerkTreeController owner;
    private PerkDefinitionSO definition;
    private PerkTreeId tree;
    private int line;

    private Image rootImage; // Image do próprio GameObject raiz — "background dourado" (Seção 6.1, 3º estado)
    private Button button;
    private Image buttonImage; // Image de "Perk_Button" — alvo do swap ativo/inativo/inativo-complete
    private TMP_Text nameLabel;
    private Image icon;
    private Transform pipsContainer; // "Perk_Qtd_Content" — número de filhos varia por perk (rank máximo)

    public PerkDefinitionSO Definition => definition;
    public PerkTreeId Tree => tree;
    public int Line => line;

    private void Awake()
    {
        rootImage = GetComponent<Image>();
        button = GetComponentInChildren<Button>(true);
        if (button != null)
        {
            buttonImage = button.GetComponent<Image>();
            button.onClick.AddListener(OnClick);

            nameLabel = button.transform.Find("Perk_Name")?.GetComponent<TMP_Text>();
            icon = button.transform.Find("Perk_Icon")?.GetComponent<Image>();
            pipsContainer = button.transform.Find("Perk_Qtd_Content");
        }
    }

    public void Setup(PerkTreeController owner, PerkDefinitionSO definition, PerkTreeId tree, int line)
    {
        this.owner = owner;
        this.definition = definition;
        this.tree = tree;
        this.line = line;

        if (nameLabel != null) nameLabel.text = definition.displayName;
        if (icon != null && definition.icon != null) icon.sprite = definition.icon;
    }

    // unlocked = a LINHA deste perk já está liberada (100% da linha anterior com rank ≥ 1, ou
    // Central comprado se for linha 1) — decidido pelo PerkTreeController, nunca calculado aqui.
    public void Refresh(bool unlocked)
    {
        if (definition == null || button == null) return;

        bool maxed = PerkState.IsMaxed(definition);
        bool canBuy = unlocked && !maxed && PerkState.CanAfford(definition);

        // Selectable.interactable, ao MUDAR de valor, dispara a troca de sprite na hora
        // (DoStateTransition), lendo o spriteState de ENTÃO — por isso o spriteState precisa
        // estar atualizado ANTES de tocar em "interactable", nunca depois. Achado real: Central
        // (1 rank só) comprava e ficava preso no sprite Locked em vez de trocar pro Maxed,
        // porque o código antigo setava "interactable" primeiro (disparando a troca com o
        // disabledSprite ainda apontando pro Locked da leitura anterior) e só DEPOIS atualizava
        // o spriteState pro Maxed — tarde demais, a transição já tinha acontecido.
        if (buttonImage != null)
        {
            var state = button.spriteState;
            state.disabledSprite = maxed ? owner.ButtonMaxedSprite : owner.ButtonLockedSprite;
            button.spriteState = state;
        }

        button.interactable = unlocked && !maxed;

        if (rootImage != null)
        {
            // Maxed também usa o background "pode comprar" (pedido do usuário) — não só
            // enquanto ainda há o que comprar.
            var bgSprite = (canBuy || maxed) ? owner.BackgroundCanAffordSprite : owner.BackgroundDefaultSprite;
            if (bgSprite != null) rootImage.sprite = bgSprite;
        }

        RefreshPips(PerkState.GetRank(definition.perkId));
    }

    // Pips acendem da esquerda pra direita conforme o rank atual — a contagem de filhos de
    // Perk_Qtd_Content É o rank máximo visual (o usuário ajusta manualmente por perk, nunca lido
    // de PerkDefinitionSO.costPerRank.Length diretamente aqui).
    private void RefreshPips(int rank)
    {
        if (pipsContainer == null) return;

        for (int i = 0; i < pipsContainer.childCount; i++)
        {
            var pip = pipsContainer.GetChild(i).GetComponent<Image>();
            if (pip == null) continue;

            var sprite = i < rank ? owner.PipFilledSprite : owner.PipEmptySprite;
            if (sprite != null) pip.sprite = sprite;
        }
    }

    private void OnClick()
    {
        if (!PerkState.TryBuy(definition)) return;
        owner.RefreshAll();

        // Achado real do usuário: o popup não atualizava o rank/preço novo depois de comprar —
        // só recalculava de novo quando o mouse saía e voltava a entrar (OnPointerEnter).
        // O clique só dispara com o mouse ainda EM CIMA deste botão (comportamento padrão do
        // Button do uGUI), então o popup já está visível pra este mesmo perk — só recalcular o
        // texto de novo. ShowPopupFor -> PopupController.Activate() não reinicia o fade/scale
        // quando o popup já está visível (só troca o texto), então não "some e aparece de novo".
        owner.ShowPopupFor(this);
    }

    public void OnPointerEnter(PointerEventData eventData) => owner.ShowPopupFor(this);
    public void OnPointerExit(PointerEventData eventData) => PopupController.Instance?.Hide();
}
