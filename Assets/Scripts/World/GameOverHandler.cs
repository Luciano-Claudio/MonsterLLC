using UnityEngine;

public class GameOverHandler : MonoBehaviour
{
    private void OnEnable() => GameEvents.OnGameStateChanged += HandleStateChanged;
    private void OnDisable() => GameEvents.OnGameStateChanged -= HandleStateChanged;

    private void HandleStateChanged(GameState state)
    {
        if (state != GameState.GameOver) return;
        Debug.Log("[GameOverHandler] Game Over — retornando ao Menu. O save NÃO é tocado aqui (GDD Seção 43).");
        GameStateManager.Instance.SetState(GameState.MainMenu);
    }
}
