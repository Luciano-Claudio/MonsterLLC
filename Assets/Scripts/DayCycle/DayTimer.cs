using UnityEngine;

public class DayTimer : MonoBehaviour
{
    public static DayTimer Instance { get; private set; }
    public float timeRemaining = 100f;
    private bool dayEnded;

    private void Awake() => Instance = this;
    private void Start() => GameEvents.TimeChanged(timeRemaining);

    private void Update()
    {
        if (dayEnded) return;
        // GameplayGate já cobre pausa (TimeManager) e o estado do jogo (GameStateManager)
        // — sem isso, o timer contava desde o load da Scene, mesmo parado no MainMenu,
        // porque só checava pausa e nunca o GameState.
        if (!GameplayGate.IsActive) return;

        timeRemaining -= Time.deltaTime;
        GameEvents.TimeChanged(Mathf.Max(timeRemaining, 0f));

        if (timeRemaining <= 0f)
        {
            timeRemaining = 0f;
            EndDay();
        }
    }

    public void ApplyPenalty(float seconds)
    {
        if (dayEnded) return;
        timeRemaining = Mathf.Max(timeRemaining - seconds, 0f);
        GameEvents.TimeChanged(timeRemaining);
        if (timeRemaining <= 0f) EndDay();
    }

    public void ResetForNewDay(float duration)
    {
        timeRemaining = duration;
        dayEnded = false;
        GameEvents.TimeChanged(timeRemaining);
    }

    private void EndDay()
    {
        dayEnded = true;
        DayResolver.ResolveEndOfDay();
    }
}
