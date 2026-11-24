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
    [SerializeField] private CardSlotUI[] slots = new CardSlotUI[3];
    [SerializeField] private Button rerollButton;
    [SerializeField] private KindStyle[] kindStyles = new KindStyle[0];

    private HeroCards owner;

    public static CardScreenUI Find() => FindAnyObjectByType<CardScreenUI>(FindObjectsInactive.Include);

    public bool IsVisible => root != null && root.activeInHierarchy;

    // O painel fica ligado na cena para edição; no jogo começa escondido.
    private void Awake()
    {
        if (root != null && root != gameObject && owner == null) root.SetActive(false);
    }

    public KindStyle StyleOf(CardKind kind)
    {
        foreach (var s in kindStyles) if (s != null && s.kind == kind) return s;
        return null;
    }

    public void Show(HeroCards cards)
    {
        owner = cards;
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
        (root != null ? root : gameObject).SetActive(true);
    }

    public void Hide()
    {
        if (root != null) root.SetActive(false);
    }

    // Teclas 1/2/3 (e as do teclado numérico) escolhem a carta da esquerda para a direita.
    private void Update()
    {
        if (!IsVisible || PauseMenuUI.IsOpen) return;
        var kb = Keyboard.current;
        if (kb == null) return;
        if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame) PressSlot(0);
        else if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame) PressSlot(1);
        else if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame) PressSlot(2);
    }

    public void PressSlot(int index)
    {
        if (index >= 0 && index < slots.Length && slots[index] != null) slots[index].Click();
    }

    private void OnSlotClicked(CardDefinition card)
    {
        if (owner != null) owner.Choose(card);
    }

    private void OnRerollClicked()
    {
        if (owner != null) owner.Reroll();
    }
}
