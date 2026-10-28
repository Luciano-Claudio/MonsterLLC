using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Time_Background/Time_Bar/Time_Head_Bar/Time_Value — mesma lógica das healthbars (fill +
// head acompanhando), só que escutando GameEvents.OnTimeChanged (DayTimer) em vez de vida.
public class DayTimeBarUI : MonoBehaviour
{
    [SerializeField] private Image timeBar;
    [SerializeField] private RectTransform headBar;
    [SerializeField] private TMP_Text valueLabel;

    [Header("Conteúdo visível dentro do canvas das sprites")]
    [Tooltip("Fração (0-1) de onde começa o conteúdo opaco do Time_Bar dentro do próprio RectTransform. Sprite real: 86px de conteúdo centralizado num canvas de 112px -> 13/112.")]
    [SerializeField] private float contentLeftFraction = 13f / 112f;
    [Tooltip("Fração (0-1) de onde termina o conteúdo opaco do Time_Bar. Sprite real: 99/112.")]
    [SerializeField] private float contentRightFraction = 99f / 112f;

    private void OnEnable() => GameEvents.OnTimeChanged += UpdateTime;
    private void OnDisable() => GameEvents.OnTimeChanged -= UpdateTime;

    private void Start()
    {
        if (DayTimer.Instance != null) UpdateTime(DayTimer.Instance.timeRemaining);
    }

    private void UpdateTime(float timeRemaining)
    {
        float dayDuration = DayTimer.Instance != null ? DayTimer.Instance.CurrentDayDuration : 0f;
        float fillAmount = dayDuration > 0f ? Mathf.Clamp01(timeRemaining / dayDuration) : 0f;
        HealthBarVisuals.Apply(timeBar, headBar, fillAmount, contentLeftFraction, contentRightFraction);

        if (valueLabel != null) valueLabel.text = Mathf.CeilToInt(Mathf.Max(timeRemaining, 0f)).ToString();
    }

    // Boss Signs (BossSignTimelineUI) precisam da mesma posição X que o Head_Bar usaria pra um
    // dado "tempo restante" — reaproveita a mesma fórmula/frações de conteúdo daqui, pra nunca
    // dessincronizar dos dois (ex.: se alguém ajustar contentLeftFraction só aqui).
    public float ComputeXForRemaining(float remaining, float dayDuration)
    {
        float fillAmount = dayDuration > 0f ? Mathf.Clamp01(remaining / dayDuration) : 0f;
        float nativeFillAmount = HealthBarVisuals.NativeFillAmount(fillAmount, contentLeftFraction, contentRightFraction);
        return HealthBarVisuals.FillEdgeX(timeBar, nativeFillAmount);
    }
}
