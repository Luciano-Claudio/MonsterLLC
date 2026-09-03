using UnityEngine;

public class ResultsHandler : MonoBehaviour
{
    private void OnEnable() => GameEvents.OnGameStateChanged += HandleStateChanged;
    private void OnDisable() => GameEvents.OnGameStateChanged -= HandleStateChanged;

    private void HandleStateChanged(GameState state)
    {
        if (state != GameState.Results) return;
        Debug.Log($"[ResultsHandler] Dia concluído. Gold total: {MainMenuUI.CurrentRun.gold}. Demanda: {DemandTracker.Instance.Sold}/{DemandTracker.Instance.Target}.");
        GameStateManager.Instance.SetState(GameState.Shop);
    }
}
