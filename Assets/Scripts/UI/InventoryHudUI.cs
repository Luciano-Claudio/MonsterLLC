using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Barra inferior de 10 ícones, sempre visível durante a gameplay (fora do TAB) — mostra SEMPRE
// os 10 primeiros slots fixos da Bag (índice 0-9), nunca o resto, mesmo com slots extras
// comprados (GDD Seção 37). Fica em "Inventory_Background".
public class InventoryHudUI : MonoBehaviour
{
    public LootVisualSettings visuals;

    private Image[] icons;
    private TMP_Text[] quantityLabels;

    private void Awake()
    {
        int count = transform.childCount;
        icons = new Image[count];
        quantityLabels = new TMP_Text[count];

        for (int i = 0; i < count; i++)
        {
            var item = transform.GetChild(i);
            icons[i] = item.GetComponent<Image>();
            quantityLabels[i] = item.Find("Quantity").GetComponent<TMP_Text>();
        }
    }

    private void OnEnable()
    {
        GameEvents.OnBagChanged += Refresh;
        if (BagController.Instance != null) Refresh(BagController.Instance.Bag);
    }

    private void OnDisable() => GameEvents.OnBagChanged -= Refresh;

    private void Refresh(Bag bag)
    {
        for (int i = 0; i < icons.Length; i++)
        {
            var slot = bag.Slots[i];
            bool hasItem = !slot.IsEmpty;
            icons[i].gameObject.SetActive(hasItem);
            quantityLabels[i].gameObject.SetActive(hasItem);
            if (!hasItem) continue;

            icons[i].sprite = visuals.GetSprite(slot.itemName, slot.quantity);
            quantityLabels[i].text = LargeNumberFormatter.Format(slot.quantity);
        }
    }
}
