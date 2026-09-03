using UnityEngine;

public enum GameState
{
    MainMenu,
    ModeSelect,
    HeroSelect,
    MapSelect,
    Gameplay,
    Paused,
    Results,
    Shop,
    GameOver
}

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }
    public GameState CurrentState { get; private set; } = GameState.MainMenu;

    private void Awake() => Instance = this;

    public void SetState(GameState state)
    {
        CurrentState = state;
        GameEvents.GameStateChanged(state);
    }
}
