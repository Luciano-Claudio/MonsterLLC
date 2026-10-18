using UnityEngine;
using UnityEngine.Rendering.Universal;

// Visual do Ciclo Dia/Noite real (docs/new/Mudanca_Estrutural_Mapa_Unico_Spawn_Bosses_
// ATUALIZADO_V2.md, Seção 25) — crossfade suave de cor/intensidade de um Light2D
// Global na troca de fase (GameEvents.OnDayNightPhaseChanged), nunca instantâneo ("a
// transição precisa ser visualmente perceptível", doc Seção 3). Só cuida do visual — a troca
// de verdade na composição de monstro/boss é feita direto em DayTimer.IsNight
// (WorldPopulationManager/WorldBossSpawnManager), sem depender deste script.
public class DayNightLightController : MonoBehaviour
{
    public Light2D globalLight;

    [Header("Dia")]
    public Color dayColor = Color.white;
    public float dayIntensity = 1f;

    [Header("Noite")]
    public Color nightColor = new Color(0.35f, 0.4f, 0.65f);
    public float nightIntensity = 0.45f;

    [Tooltip("Duração do crossfade, em segundos, no INSTANTE da troca de fase — não é a duração da fase inteira (Dia/Noite continuam 50/50 da duração do dia, ver DayTimer), só a transição visual.")]
    public float transitionDuration = 5f;

    private float transitionTimer;
    private bool transitioning;
    private Color fromColor;
    private float fromIntensity;
    private Color toColor;
    private float toIntensity;

    private void OnEnable() => GameEvents.OnDayNightPhaseChanged += HandlePhaseChanged;
    private void OnDisable() => GameEvents.OnDayNightPhaseChanged -= HandlePhaseChanged;

    private void Start()
    {
        // Estado inicial instantâneo (sem animar do zero) — usa a fase que o DayTimer já
        // estiver quando este script acorda (ex.: cena recarregada no meio do dia).
        ApplyImmediate(DayTimer.Instance != null && DayTimer.Instance.IsNight);
    }

    private void HandlePhaseChanged(bool isNight)
    {
        if (globalLight == null) return;

        fromColor = globalLight.color;
        fromIntensity = globalLight.intensity;
        toColor = isNight ? nightColor : dayColor;
        toIntensity = isNight ? nightIntensity : dayIntensity;
        transitionTimer = 0f;
        transitioning = true;
    }

    private void Update()
    {
        // Pausa deste jogo é só uma flag (TimeManager.IsPaused), Time.timeScale continua 1 —
        // sem este gate o crossfade continuaria rodando com o menu de pausa/loja aberto.
        if (!GameplayGate.IsActive || !transitioning || globalLight == null) return;

        transitionTimer += Time.deltaTime;
        float t = transitionDuration > 0f ? Mathf.Clamp01(transitionTimer / transitionDuration) : 1f;
        float smooth = t * t * (3f - 2f * t); // smoothstep — sem "degrau" perceptível no início/fim do crossfade

        globalLight.color = Color.Lerp(fromColor, toColor, smooth);
        globalLight.intensity = Mathf.Lerp(fromIntensity, toIntensity, smooth);

        if (t >= 1f) transitioning = false;
    }

    private void ApplyImmediate(bool isNight)
    {
        if (globalLight == null) return;
        globalLight.color = isNight ? nightColor : dayColor;
        globalLight.intensity = isNight ? nightIntensity : dayIntensity;
    }
}
