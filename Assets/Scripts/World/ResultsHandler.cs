using UnityEngine;

public class ResultsHandler : MonoBehaviour
{
    private void OnEnable() => GameEvents.OnGameStateChanged += HandleStateChanged;
    private void OnDisable() => GameEvents.OnGameStateChanged -= HandleStateChanged;

    private void HandleStateChanged(GameState state)
    {
        if (state != GameState.Results) return;
        // Etapas 1+2: Results = fim da run (vitória). Não existe mais Loja nem save entre dias —
        // o Resumo total (DayResolver.EndRun) leva pro menu.
        Debug.Log("[ResultsHandler] Run encerrada.");
    }
}
