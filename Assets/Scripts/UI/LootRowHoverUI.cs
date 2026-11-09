using UnityEngine;
using UnityEngine.EventSystems;

// Hover de 1 linha de item no Loot_Sold do Resumo do Dia (GDD Deadline 8.1, Seção 6, extensão
// pedida pelo usuário) — mesmo critério do InventorySlotUI/HeroCardHoverUI (AddComponent em
// runtime via ResumePainelUI, nunca pré-colocado na cena). Item ainda não conhecido (DayStatsTracker.
// IsItemKnown == false) não mostra popup — mesma regra que já esconde nome/preço da linha.
public class LootRowHoverUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private string itemName;
    private LootPricingSettings pricingSettings;

    public void Setup(string itemName, LootPricingSettings pricingSettings)
    {
        this.itemName = itemName;
        this.pricingSettings = pricingSettings;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!DayStatsTracker.IsItemKnown(itemName)) return;
        if (pricingSettings == null || !pricingSettings.TryGetPrice(itemName, out long sellValue)) return;

        pricingSettings.TryGetDescription(itemName, out string description);
        PopupController.Instance?.ShowItem(gameObject, itemName, description ?? "???", $"{LargeNumberFormatter.Format(sellValue)}g");
    }

    public void OnPointerExit(PointerEventData eventData) => PopupController.Instance?.Hide();
}
