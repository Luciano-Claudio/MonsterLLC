using UnityEngine;

public class Vendor : Interactable
{
    // Preço por material (GDD Seção 39) — substitui o antigo pricePerEssence fixo (Sprint 31:
    // vendia só "Monster Essence", hardcoded a 1 gold). Itens sem entrada em pricingSettings
    // não são vendidos (ficam na Bag).
    public LootPricingSettings pricingSettings;

    public override void Interact(Transform interactor)
    {
        var bag = BagController.Instance.Bag;
        long goldEarned = 0;

        for (int i = bag.Slots.Count - 1; i >= 0; i--)
        {
            var slot = bag.Slots[i];
            if (!pricingSettings.TryGetPrice(slot.itemName, out long sellValue)) continue;

            goldEarned += slot.quantity * sellValue;
            DemandTracker.Instance.RegisterSale(slot.itemName, slot.quantity);
            bag.RemoveSlot(i);
        }

        if (goldEarned == 0)
        {
            Debug.Log("[Vendor] Nada pra vender.");
            return;
        }

        MainMenuUI.CurrentRun.gold += goldEarned;

        Debug.Log($"[Vendor] Vendeu itens por {goldEarned} gold. Total: {MainMenuUI.CurrentRun.gold}");
        GameEvents.GoldChanged(MainMenuUI.CurrentRun.gold);
        GameEvents.BagChanged(bag);
    }
}
