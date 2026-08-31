using UnityEngine;

public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance { get; private set; }
    public bool IsPaused { get; private set; }

    private void Awake() => Instance = this;

    public void Pause() => IsPaused = true;
    public void Resume() => IsPaused = false;
    public void TogglePause() => IsPaused = !IsPaused;
}
