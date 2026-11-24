using UnityEngine;
using UnityEngine.UI;

// Etapas 1+2 (spec 6.3) — tela de 3 cartas. O USUÁRIO monta o visual na cena (como a barra de XP e o
// Resumo total) e liga aqui: o objeto com este script fica ATIVO; `root` é o painel que aparece/some;
// `slots` são as 3 cartas (CardSlotUI); `rerollButton` troca as 3 cartas (1 vez por oferta).
// Quem abre/fecha/pausa é o HeroCards; esta tela só mostra e repassa os cliques.
public class CardScreenUI : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private CardSlotUI[] slots = new CardSlotUI[3];
    [SerializeField] private Button rerollButton;

    private HeroCards owner;

    public static CardScreenUI Find() => FindAnyObjectByType<CardScreenUI>(FindObjectsInactive.Include);

    public void Show(HeroCards cards)
    {
        owner = cards;
        var offer = cards.CurrentOffer;
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null) continue;
            if (i < offer.Count) slots[i].Bind(offer[i], cards.LevelOf(offer[i]), OnSlotClicked);
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

    private void OnSlotClicked(CardDefinition card)
    {
        if (owner != null) owner.Choose(card);
    }

    private void OnRerollClicked()
    {
        if (owner != null) owner.Reroll();
    }
}
