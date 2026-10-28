using UnityEngine;

// Clock > Rotation_Position > Clock_Hand. Rotation_Position gira no Z conforme o tempo do dia
// decorrido (0° no início do dia, -360° completos no fim). Clock_Alert pulsa 1x quando vira
// noite (metade do dia, via DayTimer.IsNight) e pulsa 1x por segundo nos últimos 5 segundos.
public class DayClockUI : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private RectTransform rotationPosition;

    private static readonly int AlertTrigger = Animator.StringToHash("AlertTrigger");

    private int lastFinalSecondAlerted = -1;

    private void OnEnable()
    {
        GameEvents.OnTimeChanged += UpdateClock;
        GameEvents.OnDayNightPhaseChanged += HandleDayNightPhaseChanged;
        GameEvents.OnDayStart += HandleDayStart;
    }

    private void OnDisable()
    {
        GameEvents.OnTimeChanged -= UpdateClock;
        GameEvents.OnDayNightPhaseChanged -= HandleDayNightPhaseChanged;
        GameEvents.OnDayStart -= HandleDayStart;
    }

    private void HandleDayStart() => lastFinalSecondAlerted = -1;

    private void HandleDayNightPhaseChanged(bool isNight)
    {
        if (isNight) animator.SetTrigger(AlertTrigger);
    }

    private void UpdateClock(float timeRemaining)
    {
        float dayDuration = DayTimer.Instance != null ? DayTimer.Instance.CurrentDayDuration : 0f;
        if (dayDuration > 0f)
        {
            float elapsedFraction = 1f - Mathf.Clamp01(timeRemaining / dayDuration);
            rotationPosition.localEulerAngles = new Vector3(0f, 0f, -360f * elapsedFraction);
        }

        // Últimos 5 segundos — pulsa 1x a cada mudança de segundo inteiro (5, 4, 3, 2, 1).
        if (timeRemaining <= 5f && timeRemaining > 0f)
        {
            int currentSecond = Mathf.CeilToInt(timeRemaining);
            if (currentSecond != lastFinalSecondAlerted)
            {
                lastFinalSecondAlerted = currentSecond;
                animator.SetTrigger(AlertTrigger);
            }
        }
    }
}
