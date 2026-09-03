using UnityEngine;

public static class DayResolver
{
    public static void ResolveEndOfDay()
    {
        // Loot ainda na Bag e destruido ao fim do dia - GDD Secao 37/38.
        BagController.Instance.Bag.Clear();
        GameEvents.BagChanged(BagController.Instance.Bag);

        if (DemandTracker.Instance.IsMet())
        {
            Debug.Log("[DayResolver] Demanda cumprida — indo para Resultados.");
            GameStateManager.Instance.SetState(GameState.Results);
        }
        else
        {
            Debug.Log("[DayResolver] Demanda NÃO cumprida — GAME OVER.");
            GameStateManager.Instance.SetState(GameState.GameOver);
        }
    }
}
