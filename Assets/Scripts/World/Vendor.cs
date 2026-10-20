using UnityEngine;

public class Vendor : Interactable
{
    // Preço por material (GDD Seção 39) — substitui o antigo pricePerEssence fixo (Sprint 31:
    // vendia só "Monster Essence", hardcoded a 1 gold). Itens sem entrada em pricingSettings
    // não são vendidos (ficam na Bag).
    public LootPricingSettings pricingSettings;

    // Carrinho ao lado do Vendor (GameObject separado, Sprint 32 adiantado) — efeito puramente
    // cosmético, nunca bloqueia a venda real se ficar null (ex.: cena de teste sem o carrinho).
    public VendorCart cart;

    private Animator animator;

    private void Awake() => animator = GetComponent<Animator>();

    // Sprint 32 — Shop_Opening toca no início do dia, transita por conta própria (Exit Time, no
    // próprio Animator Controller) pra Idle_Shop_Open; Shop_Closing toca no fim do dia e fica lá
    // até o próximo OpenTrigger. Os 2 triggers são AnyState, então interrompem qualquer estado
    // atual sem precisar checar em qual animação o Vendor está agora.
    private void OnEnable()
    {
        GameEvents.OnDayStart += HandleDayStart;
        GameEvents.OnDayEnd += HandleDayEnd;
    }

    private void OnDisable()
    {
        GameEvents.OnDayStart -= HandleDayStart;
        GameEvents.OnDayEnd -= HandleDayEnd;
    }

    private void HandleDayStart()
    {
        if (animator != null) animator.SetTrigger("OpenTrigger");
    }

    private void HandleDayEnd()
    {
        if (animator != null) animator.SetTrigger("CloseTrigger");
    }

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
            if (cart != null) cart.AcceptSale(slot.itemName, slot.quantity, interactor.position);
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
