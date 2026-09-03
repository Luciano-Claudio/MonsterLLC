using UnityEngine;

public class ShopHandler : MonoBehaviour
{
    private void OnEnable() => GameEvents.OnGameStateChanged += HandleStateChanged;
    private void OnDisable() => GameEvents.OnGameStateChanged -= HandleStateChanged;

    private void HandleStateChanged(GameState state)
    {
        if (state != GameState.Shop) return;
        Debug.Log("[ShopHandler] Loja aberta. Use \"Buy Next Weapon Tier\" e/ou \"Start Next Day\".");
    }

    // Stub — nao e o sistema real de Weapon Tier (15 tiers, precos, multiplicadores
    // de dano/vida). So o suficiente pra provar que uma compra na Loja sobrevive ao
    // Save/Load. Vira o sistema de verdade na Deadline 9 (Sprint 33).
    private static readonly string[] tierOrder = { "Basic", "Copper", "Iron" };

    [ContextMenu("Buy Next Weapon Tier")]
    public void BuyNextTier()
    {
        int currentIndex = System.Array.IndexOf(tierOrder, MainMenuUI.CurrentRun.weaponTier);
        if (currentIndex == -1 || currentIndex >= tierOrder.Length - 1)
        {
            Debug.Log("[ShopHandler] Nenhum tier seguinte disponível (stub só vai até Iron nesta sprint).");
            return;
        }

        int price = (currentIndex + 1) * 50; // placeholder — preços reais são Deadline 9
        if (MainMenuUI.CurrentRun.gold < price)
        {
            Debug.Log($"[ShopHandler] Gold insuficiente ({MainMenuUI.CurrentRun.gold}/{price}) para {tierOrder[currentIndex + 1]}.");
            return;
        }

        MainMenuUI.CurrentRun.gold -= price;
        MainMenuUI.CurrentRun.weaponTier = tierOrder[currentIndex + 1];
        GameEvents.GoldChanged(MainMenuUI.CurrentRun.gold);
        Debug.Log($"[ShopHandler] Comprou {MainMenuUI.CurrentRun.weaponTier} por {price} gold.");
    }

    [ContextMenu("Start Next Day")]
    public void StartNextDay()
    {
        MainMenuUI.CurrentRun.day++;
        DayTimer.Instance.ResetForNewDay(100f);
        DemandTracker.Instance.StartDay(MainMenuUI.CurrentRun.day);
        GameStateManager.Instance.SetState(GameState.Gameplay);
        Debug.Log($"[ShopHandler] Iniciando Dia {MainMenuUI.CurrentRun.day}.");
    }
}
