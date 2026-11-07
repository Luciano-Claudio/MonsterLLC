using UnityEngine;

public class ResultsHandler : MonoBehaviour
{
    private void OnEnable() => GameEvents.OnGameStateChanged += HandleStateChanged;
    private void OnDisable() => GameEvents.OnGameStateChanged -= HandleStateChanged;

    private void HandleStateChanged(GameState state)
    {
        if (state != GameState.Results) return;
        Debug.Log($"[ResultsHandler] Dia {(DayResolver.PlayerSurvivedLastDay ? "sobrevivido" : "perdido (morte)")}. Gold total: {MainMenuUI.CurrentRun.gold}.");

        // Único lugar do projeto que chama Save() — só em sucesso de verdade (GDD Deadline 8.1,
        // Seção 3: morrer nunca "desfaz" um dia já vencido antes, mas também não é progresso
        // novo pra salvar — só repete o dia atual até o jogador superá-lo).
        if (DayResolver.PlayerSurvivedLastDay)
        {
            SaveManager.Save(MainMenuUI.CurrentRun);
            Debug.Log("[ResultsHandler] Save automático realizado ao entrar na Loja.");
        }

        GameStateManager.Instance.SetState(GameState.Shop);
    }
}
