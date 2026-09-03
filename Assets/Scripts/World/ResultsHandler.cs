using UnityEngine;

public class ResultsHandler : MonoBehaviour
{
    private void OnEnable() => GameEvents.OnGameStateChanged += HandleStateChanged;
    private void OnDisable() => GameEvents.OnGameStateChanged -= HandleStateChanged;

    private void HandleStateChanged(GameState state)
    {
        if (state != GameState.Results) return;
        Debug.Log($"[ResultsHandler] Dia concluído. Gold total: {MainMenuUI.CurrentRun.gold}. Demanda: {DemandTracker.Instance.Sold}/{DemandTracker.Instance.Target}.");

        // Único lugar do projeto que chama Save() — exatamente o fluxo da GDD Seção 43:
        // "Fim do dia (demanda cumprida) → Resultados → Loja → SAVE AUTOMÁTICO".
        SaveManager.Save(MainMenuUI.CurrentRun);
        Debug.Log("[ResultsHandler] Save automático realizado ao entrar na Loja.");

        GameStateManager.Instance.SetState(GameState.Shop);
    }
}
