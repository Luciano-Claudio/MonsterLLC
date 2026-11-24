using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Barra inferior ("Inventory_Background", 6 espaços), sempre visível na gameplay. Etapa 3
// (docs/superpowers/specs/2026-11-24-etapa-3-bau-vendedor-itens-design.md, 5.2): mostra os ITENS do
// herói (HeroCards.OwnedItems), na ordem em que foram pegos — ícone e nível no texto "Quantity".
// Espaço vazio fica escondido. Não mostra mais os materiais da Bag.
public class InventoryHudUI : MonoBehaviour
{
    private Image[] icons;
    private TMP_Text[] levelLabels;

    private void Awake()
    {
        int count = transform.childCount;
        icons = new Image[count];
        levelLabels = new TMP_Text[count];

        for (int i = 0; i < count; i++)
        {
            var item = transform.GetChild(i);
            icons[i] = item.GetComponent<Image>();
            var label = item.Find("Quantity");
            levelLabels[i] = label != null ? label.GetComponent<TMP_Text>() : null;
        }
    }

    private void OnEnable()
    {
        GameEvents.OnItemsChanged += Refresh;
        Refresh();
    }

    private void OnDisable() => GameEvents.OnItemsChanged -= Refresh;

    private void Refresh()
    {
        if (icons == null) return;
        var cards = HeroCards.Instance;
        var items = cards != null ? cards.OwnedItems : null;
        for (int i = 0; i < icons.Length; i++)
        {
            bool has = items != null && i < items.Count && items[i] != null;
            if (icons[i] != null) icons[i].gameObject.SetActive(has);
            if (!has) continue;
            if (icons[i] != null) icons[i].sprite = items[i].icon;
            if (levelLabels[i] != null) levelLabels[i].text = cards.LevelOf(items[i]).ToString();
        }
    }
}
