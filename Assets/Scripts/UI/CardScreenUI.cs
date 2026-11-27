using TMPro;
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Cards_Popup — a mesma tela de 3 cartas para todos os casos: subir de nível (os 3 tipos), baú
// (upgrades/atributos) e vendedor (itens). O usuário monta o visual na cena; este script fica num
// objeto SEMPRE ATIVO acima do painel (UI_On_Game) e liga:
// - `root`: o painel (Cards_Popup), que aparece/some;
// - `slots`: as 3 cartas, da ESQUERDA para a DIREITA (= teclas 1, 2, 3);
// - `rerollButton`: troca as 3 cartas (1 vez por oferta);
// - `kindStyles`: fundo, rótulo e cor de cada tipo (Item / Upgrade / Stat).
// Quem abre/fecha/pausa é o HeroCards; esta tela só mostra e repassa cliques e teclas.
// Confirmação (pedido do usuário, 2026-11-25 — clique de ataque caía na tela e escolhia sem querer):
// clicar numa carta (ou 1/2/3) só abre o Panel_Confirm com a escolha no `confirmInfo`; a carta só é
// aplicada no Yes (ou Enter). No fecha e volta pra escolha. Reroll e oferta nova fecham o painel.
public class CardScreenUI : MonoBehaviour
{
    [Serializable]
    public class KindStyle
    {
        public CardKind kind;
        public Sprite background;
        public string label;
        public Color labelColor = Color.white;
    }

    [SerializeField] private GameObject root;
    // Loja permanente — o contador "Reroll_Quantity" (criado pelo usuário no Cards_Popup), achado pelo nome.
    private TMP_Text rerollQuantity;
    private bool rerollQuantitySearched;
    [SerializeField] private CardSlotUI[] slots = new CardSlotUI[3];
    [SerializeField] private Button rerollButton;
    [SerializeField] private KindStyle[] kindStyles = new KindStyle[0];

    [Header("Confirmação (Panel_Confirm)")]
    [SerializeField] private GameObject confirmPanel;
    [SerializeField] private TMP_Text confirmInfo;
    [SerializeField] private Button confirmYes;
    [SerializeField] private Button confirmNo;
    [Tooltip("{0} = nome da carta (na cor do tipo), {1} = nível que ela vai ter.")]
    [SerializeField] private string confirmFormat = "You picked: {0} (Lv {1})";

    private HeroCards owner;
    private CardDefinition pendingCard;

    public bool IsConfirming => confirmPanel != null && confirmPanel.activeSelf;

    public static CardScreenUI Find() => FindAnyObjectByType<CardScreenUI>(FindObjectsInactive.Include);

    public bool IsVisible => root != null && root.activeInHierarchy;

    // O painel fica ligado na cena para edição; no jogo começa escondido.
    private void Awake()
    {
        if (root != null && root != gameObject && owner == null) root.SetActive(false);
        if (confirmPanel != null) confirmPanel.SetActive(false);
        if (confirmYes != null) { confirmYes.onClick.RemoveListener(ConfirmChoice); confirmYes.onClick.AddListener(ConfirmChoice); }
        if (confirmNo != null) { confirmNo.onClick.RemoveListener(CancelChoice); confirmNo.onClick.AddListener(CancelChoice); }
    }

    public KindStyle StyleOf(CardKind kind)
    {
        foreach (var s in kindStyles) if (s != null && s.kind == kind) return s;
        return null;
    }

    public void Show(HeroCards cards)
    {
        owner = cards;
        CloseConfirm();
        var offer = cards.CurrentOffer;
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null) continue;
            if (i < offer.Count) slots[i].Bind(offer[i], cards.LevelOf(offer[i]), StyleOf(offer[i].kind), OnSlotClicked);
            else slots[i].gameObject.SetActive(false);
        }
        if (rerollButton != null)
        {
            rerollButton.onClick.RemoveListener(OnRerollClicked);
            rerollButton.onClick.AddListener(OnRerollClicked);
            rerollButton.interactable = cards.RerollAvailable;
        }
        UpdateRerollQuantity(cards.RerollStock);
        (root != null ? root : gameObject).SetActive(true);
    }

    public void Hide()
    {
        CloseConfirm();
        if (root != null) root.SetActive(false);
    }

    // Teclas 1/2/3 (e as do teclado numérico) escolhem a carta da esquerda para a direita.
    private void Update()
    {
        if (!IsVisible || PauseMenuUI.IsOpen) return;
        var kb = Keyboard.current;
        if (kb == null) return;
        if (IsConfirming && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)) { ConfirmChoice(); return; }
        if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame) PressSlot(0);
        else if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame) PressSlot(1);
        else if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame) PressSlot(2);
    }

    public void PressSlot(int index)
    {
        if (index >= 0 && index < slots.Length && slots[index] != null) slots[index].Click();
    }

    // Clique na carta: sem painel de confirmação na cena, escolhe direto (comportamento antigo).
    private void OnSlotClicked(CardDefinition card)
    {
        if (card == null || owner == null) return;
        if (confirmPanel == null) { owner.Choose(card); return; }
        pendingCard = card;
        if (confirmInfo != null)
        {
            var style = StyleOf(card.kind);
            string name = style != null ? $"<color=#{ColorUtility.ToHtmlStringRGB(style.labelColor)}>{card.title}</color>" : card.title;
            confirmInfo.text = string.Format(confirmFormat, name, owner.LevelOf(card) + 1);
        }
        confirmPanel.SetActive(true);
    }

    public void ConfirmChoice()
    {
        var card = pendingCard;
        CloseConfirm();
        if (card != null && owner != null) owner.Choose(card);
    }

    public void CancelChoice() => CloseConfirm();

    private void CloseConfirm()
    {
        pendingCard = null;
        if (confirmPanel != null) confirmPanel.SetActive(false);
    }

    private TMP_Text RerollQuantity
    {
        get
        {
            if (rerollQuantitySearched) return rerollQuantity;
            rerollQuantitySearched = true;
            var top = root != null ? root.transform : transform;
            foreach (var t in top.GetComponentsInChildren<TMP_Text>(true))
                if (t.name == "Reroll_Quantity") { rerollQuantity = t; break; }
            return rerollQuantity;
        }
    }

    private void UpdateRerollQuantity(int stock)
    {
        var label = RerollQuantity;
        if (label != null) label.text = stock.ToString();
    }

    private void OnRerollClicked()
    {
        CloseConfirm();
        if (owner != null) owner.Reroll();
    }
}
