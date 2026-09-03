public static class GameplayGate
{
    public static bool IsActive =>
        (TimeManager.Instance == null || !TimeManager.Instance.IsPaused) &&
        (GameStateManager.Instance == null || GameStateManager.Instance.CurrentState == GameState.Gameplay);
}
