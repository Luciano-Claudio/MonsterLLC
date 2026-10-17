using UnityEngine;

public class DayTimer : MonoBehaviour
{
    public static DayTimer Instance { get; private set; }
    public float timeRemaining = 100f;

    [Tooltip("Duração padrão (em segundos) de 1 ciclo dia/noite, usada tanto no Dia 1 (MainMenuUI) quanto em todo dia seguinte (ShopHandler) quando eles chamam ResetForNewDay() sem argumento. Único lugar pra configurar isso — hoje é só pra teste (explorar mais tempo), no futuro vira a base de compras que aumentam a duração do dia.")]
    public float defaultDayDuration = 100f;

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

    // Sem argumento = usa defaultDayDuration (o campo configurável no Inspector) — é o que
    // MainMenuUI/ShopHandler devem chamar normalmente. O overload com argumento continua
    // existindo pra quem precisar de uma duração pontual diferente da padrão.
    public void ResetForNewDay() => ResetForNewDay(defaultDayDuration);

    public void ResetForNewDay(float duration)
    {
        timeRemaining = duration;
        dayEnded = false;
        GameEvents.TimeChanged(timeRemaining);
        // Fonte única de "um novo dia começou" — cobre tanto o Dia 1 (MainMenuUI) quanto
        // todo dia seguinte (ShopHandler), sem precisar duplicar a chamada nos dois lugares.
        GameEvents.DayStarted();
    }

    private void EndDay()
    {
        dayEnded = true;
        DayResolver.ResolveEndOfDay();
    }
}
