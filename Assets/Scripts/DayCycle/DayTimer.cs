using UnityEngine;

public class DayTimer : MonoBehaviour
{
    public static DayTimer Instance { get; private set; }
    public float timeRemaining = 100f;

    [Tooltip("Duração padrão (em segundos) de 1 ciclo dia/noite, usada tanto no Dia 1 (MainMenuUI) quanto em todo dia seguinte (ShopHandler) quando eles chamam ResetForNewDay() sem argumento. Único lugar pra configurar isso — hoje é só pra teste (explorar mais tempo), no futuro vira a base de compras que aumentam a duração do dia.")]
    public float defaultDayDuration = 100f;

    private bool dayEnded;

    // Duração REAL do dia em curso (pode diferir de defaultDayDuration — ver o overload com
    // argumento de ResetForNewDay) — guardada pra calcular a fase Dia/Noite (50/50, docs/new/
    // Mudanca_Estrutural_Mapa_Unico_Spawn_Bosses_ATUALIZADO_V2.md, Seção 3) proporcional à
    // duração de verdade do dia, não a um valor fixo.
    private float currentDayDuration = 100f;

    // Fase atual (false = Dia, true = Noite) — 50% da duração do dia é Dia, os outros 50% são
    // Noite, nunca quebra essa proporção (doc, Seção 3: "Nenhum bônus de tempo poderá quebrar
    // essa proporção"). Todo dia novo começa em Dia (ver ResetForNewDay).
    public bool IsNight { get; private set; }

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
        UpdateDayNightPhase();

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
        UpdateDayNightPhase();
        if (timeRemaining <= 0f) EndDay();
    }

    // Recalcula a fase a partir do tempo decorrido (currentDayDuration - timeRemaining) e só
    // dispara GameEvents.DayNightPhaseChanged na TROCA de fase (edge trigger), nunca todo
    // frame — mesmo critério de outros eventos pontuais do projeto (ex.: OnDayStart).
    private void UpdateDayNightPhase()
    {
        if (currentDayDuration <= 0f) return;

        float elapsed = currentDayDuration - timeRemaining;
        bool nowIsNight = elapsed >= currentDayDuration * 0.5f;
        if (nowIsNight == IsNight) return;

        IsNight = nowIsNight;
        GameEvents.DayNightPhaseChanged(IsNight);
    }

    // Sem argumento = usa defaultDayDuration (o campo configurável no Inspector) — é o que
    // MainMenuUI/ShopHandler devem chamar normalmente. O overload com argumento continua
    // existindo pra quem precisar de uma duração pontual diferente da padrão.
    public void ResetForNewDay() => ResetForNewDay(defaultDayDuration);

    public void ResetForNewDay(float duration)
    {
        timeRemaining = duration;
        currentDayDuration = duration;
        dayEnded = false;
        IsNight = false; // todo dia novo começa em Dia — doc, Seção 3
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
