using UnityEngine;
using UnityEngine.UI;

// Barras da gameplay com 6 espaços, sempre visíveis. Etapa 3: "Inventory_Background" mostra os ITENS
// do herói (HeroCards.OwnedItems). Etapa 4 (docs/superpowers/specs/2026-11-24-etapa-4-especializacoes-tags-design.md):
// o mesmo componente no "Atribute_Background", com kind = Stat, mostra os ATRIBUTOS (OwnedStats).
// Na ordem em que foram pegos, só o ícone (o nível não aparece mais — o "Quantity" saiu). Espaço
// vazio fica escondido. Upgrades de habilidade não aparecem em barra nenhuma.
public class InventoryHudUI : MonoBehaviour
{
    [SerializeField] private CardKind kind = CardKind.Item;

    private Image[] icons;

    private void Awake()
    {
        int count = transform.childCount;
        icons = new Image[count];
        for (int i = 0; i < count; i++) icons[i] = transform.GetChild(i).GetComponent<Image>();
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
        var list = cards == null ? null : kind == CardKind.Stat ? cards.OwnedStats : cards.OwnedItems;
        for (int i = 0; i < icons.Length; i++)
        {
            bool has = list != null && i < list.Count && list[i] != null;
            if (icons[i] == null) continue;
            icons[i].gameObject.SetActive(has);
            if (has) icons[i].sprite = list[i].icon;
        }
    }
}
