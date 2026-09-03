using UnityEngine;

public class ShopHandler : MonoBehaviour
{
    private void OnEnable() => GameEvents.OnGameStateChanged += HandleStateChanged;
    private void OnDisable() => GameEvents.OnGameStateChanged -= HandleStateChanged;

    private void HandleStateChanged(GameState state)
    {
        if (state != GameState.Shop) return;
        Debug.Log("[ShopHandler] Loja aberta (esqueleto — sem compras ainda). Use \"Start Next Day\" pra avançar.");
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
