using UnityEngine;

public enum GameState { Gameplay, Paused }

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }
    public GameState CurrentState { get; private set; } = GameState.Gameplay;

    private void Awake() => Instance = this;
    public void SetState(GameState state) => CurrentState = state;
}
