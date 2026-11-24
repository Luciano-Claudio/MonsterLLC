using UnityEngine;

// Etapas 1+2 (docs/superpowers/specs/2026-11-24-etapas-1-2-run-25min-cartas-design.md, 5.1/5.6) — o
// relógio da RUN de 25 min (não existe mais "dia"). O nome ficou DayTimer porque luz, relógio do HUD,
// barra, horda e telemetria já escutam OnTimeChanged/DayNightPhaseChanged daqui.
// - timeRemaining = runSeconds - RunElapsed, em contagem regressiva; chega em 0 e fica parado lá
//   enquanto o Divine God estiver vivo (ele é quem termina a run — DayResolver.EndRun).
// - Noite pelo RunSchedule (ciclo dia/noite + noite travada a partir do Divine God).
public class DayTimer : MonoBehaviour
{
    public static DayTimer Instance { get; private set; }
    public float timeRemaining = 1500f;

    private bool runEnded;

    // D9.4 (decisão do usuário, 2026-11-20): conta desde o começo. O loading continua de fora: o
    // Update() só anda com GameplayGate ativo, e o estado só vira Gameplay quando a tela de loading some.
    private bool clockStarted;
    public bool IsClockRunning => clockStarted && !runEnded;

    // Duração da run (RunSettings.runSeconds). Mantém o nome antigo: a barra do HUD, o relógio e a
    // telemetria leem isto como "duração total".
    private float currentDayDuration = 1500f;
    public float CurrentDayDuration => currentDayDuration;

    public bool IsNight { get; private set; }

    public RunSchedule Schedule { get; private set; }
    public RunSettings Settings { get; private set; }
    public float RunElapsed { get; private set; } // segundos de run decorridos (nunca pausa com boss)
    public bool FinalBossAlive { get; set; } // setado pelo WorldBossSpawnManager

    public float ElapsedFraction01 => currentDayDuration > 0f ? Mathf.Clamp01(RunElapsed / currentDayDuration) : 0f;

    private void Awake() => Instance = this;
    private void OnEnable() => GameEvents.OnFinalBossKilled += HandleFinalBossKilled;
    private void OnDisable() => GameEvents.OnFinalBossKilled -= HandleFinalBossKilled;
    private static void HandleFinalBossKilled() => DayResolver.EndRun(true); // vitória
    private void Start() => GameEvents.TimeChanged(timeRemaining);

    private void Update()
    {
        if (!IsClockRunning) return;
        // GameplayGate cobre pausa e o estado do jogo; HeroController.IsDying para o relógio no
        // instante da morte (o estado só vira Results no fim da animação "Die").
        if (!GameplayGate.IsActive || HeroController.IsDying) return;
        Tick(Time.deltaTime);
    }

    private void Tick(float deltaTime)
    {
        if (Schedule == null) return;
        RunElapsed += deltaTime;
        timeRemaining = Mathf.Max(0f, currentDayDuration - RunElapsed);
        GameEvents.TimeChanged(timeRemaining);

        // Lista sem Divine God (revisão final, achado 3): sem ninguém pra matar no fim, chegar aos
        // 25:00 vivo é a vitória — senão a run nunca terminaria.
        if (!Schedule.HasFinalBoss && RunElapsed >= currentDayDuration) DayResolver.EndRun(true);

        bool nowIsNight = Schedule.IsNight(RunElapsed);
        if (nowIsNight == IsNight) return;
        IsNight = nowIsNight;
        GameEvents.DayNightPhaseChanged(IsNight);
    }

    // Início da run: relógio zerado e ligado, dia, horda já perseguindo o herói.
    public void StartRun() => StartRun(RunSettings.Load());

    public void StartRun(RunSettings settings)
    {
        DayResolver.ResetForNewRun();
        Settings = settings;
        if (Settings == null) Settings = ScriptableObject.CreateInstance<RunSettings>(); // sem asset: padrões (sem bosses)
        Schedule = new RunSchedule(Settings.BossTiers(), Settings);
        currentDayDuration = Settings.runSeconds;
        RunElapsed = 0f;
        timeRemaining = currentDayDuration;
        FinalBossAlive = false;
        runEnded = false;
        clockStarted = true;
        IsNight = false;
        // O DayNightLightController só escuta o EVENTO — sem isto a luz ficava na noite da run anterior.
        GameEvents.DayNightPhaseChanged(false);
        HeroController.GlobalAggroActive = true;
        GameEvents.TimeChanged(timeRemaining);
        GameEvents.DayStarted(); // "a run começou" — quem já escutava o início do dia continua funcionando
    }

    // Nome antigo, mantido para os callers que ainda não migraram (ShopHandler fica sem uso no fluxo).
    public void ResetForNewDay() => StartRun();

    // Chamado pelo setter de HeroController.GlobalAggroActive (false->true): garantia idempotente.
    public void StartDayClock() => clockStarted = true;

    // Fim da run (vitória ou derrota): o relógio para de vez.
    public void StopClock() => runEnded = true;

#if UNITY_EDITOR
    // Só para verificação no Editor (eval): avança o relógio ignorando pausa/estado do jogo.
    public void DebugAdvance(float seconds)
    {
        if (!IsClockRunning) return;
        Tick(seconds);
    }
#endif
}
