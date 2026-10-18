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

    // Stub — nao e o sistema real de Tier Suppression (nome tematico, preco balanceado, UI de
    // loja). So o suficiente pra provar que a supressao sobrevive ao Save/Load e afeta o
    // spawn de monstro/boss de verdade (ver RunState.suppressedTierCount,
    // WorldTierSelector.ComputeTierPercentsNonAlloc). Doc de design, Secao 12: SEMPRE
    // sequencial a partir do Tier1 (nunca escolha livre) — por isso so existe "comprar o
    // PROXIMO", sem parametro de qual Tier.
    [ContextMenu("Buy Next Tier Suppression")]
    public void BuyNextTierSuppression()
    {
        if (MainMenuUI.CurrentRun.suppressedTierCount >= WorldTierSelector.MonsterTierCount)
        {
            Debug.Log("[ShopHandler] Todos os Tiers já suprimidos.");
            return;
        }

        int nextTierNumber = MainMenuUI.CurrentRun.suppressedTierCount + 1; // 1-based, só pro log
        int price = nextTierNumber * 100; // placeholder — preço real de balanceamento fica pra quando a mecânica virar de verdade
        if (MainMenuUI.CurrentRun.gold < price)
        {
            Debug.Log($"[ShopHandler] Gold insuficiente ({MainMenuUI.CurrentRun.gold}/{price}) para suprimir Tier {nextTierNumber}.");
            return;
        }

        MainMenuUI.CurrentRun.gold -= price;
        MainMenuUI.CurrentRun.suppressedTierCount++;
        GameEvents.GoldChanged(MainMenuUI.CurrentRun.gold);
        // Decisão do usuário: NÃO limpa a cena na hora da compra — monstros/bosses do Tier
        // recém-suprimido continuam vivos até morrerem/despawnarem naturalmente por distância;
        // só o REPLACEMENT a partir daqui já respeita a supressão. GameEvents.ClearAllEnemies()
        // fica só pro botão de debug "Regenerar Mundo" (ver ChunkTerrainGenerator).
        Debug.Log($"[ShopHandler] Tier {nextTierNumber} suprimido por {price} gold.");
    }

    // Cheat de debug — só pra testar compras (Weapon Tier, Tier Suppression) sem precisar
    // vender loot de verdade no Vendor primeiro. Nunca chamado pelo fluxo real do jogo.
    [ContextMenu("Debug: Give 1000 Gold")]
    public void DebugGiveGold()
    {
        MainMenuUI.CurrentRun.gold += 1000;
        GameEvents.GoldChanged(MainMenuUI.CurrentRun.gold);
        Debug.Log($"[ShopHandler] (Debug) +1000 gold — total agora: {MainMenuUI.CurrentRun.gold}.");
    }

    [ContextMenu("Start Next Day")]
    public void StartNextDay()
    {
        MainMenuUI.CurrentRun.day++;
        DayTimer.Instance.ResetForNewDay();
        DemandTracker.Instance.StartDay(MainMenuUI.CurrentRun.day);
        GameStateManager.Instance.SetState(GameState.Gameplay);
        Debug.Log($"[ShopHandler] Iniciando Dia {MainMenuUI.CurrentRun.day}.");
    }
}
