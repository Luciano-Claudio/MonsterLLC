using UnityEngine;

public class Vendor : Interactable
{
    public int pricePerEssence = 1; // valor placeholder — Economia real chega na Deadline 9 do roadmap

    public override void Interact(Transform interactor)
    {
        var bag = BagController.Instance.Bag;
        int totalSold = 0;

        for (int i = bag.Slots.Count - 1; i >= 0; i--)
        {
            if (bag.Slots[i].itemName != "Monster Essence") continue;
            totalSold += bag.Slots[i].quantity;
            bag.RemoveSlot(i);
        }

        if (totalSold == 0)
        {
            Debug.Log("[Vendor] Nada pra vender.");
            return;
        }

        int goldEarned = totalSold * pricePerEssence;
        MainMenuUI.CurrentRun.gold += goldEarned;
        DemandTracker.Instance.RegisterSale("Monster Essence", totalSold);

        Debug.Log($"[Vendor] Vendeu {totalSold}x Monster Essence por {goldEarned} gold. Total: {MainMenuUI.CurrentRun.gold}");
        GameEvents.GoldChanged(MainMenuUI.CurrentRun.gold);
        GameEvents.BagChanged(bag);
    }
}
