using UnityEngine;
using TMPro;

public class HealthIndicatorUI : MonoBehaviour
{
    public TMP_Text label;

    private void OnEnable() => GameEvents.OnHealthChanged += UpdateLabel;
    private void OnDisable() => GameEvents.OnHealthChanged -= UpdateLabel;

    private void UpdateLabel(float current, float max) => label.text = $"HP: {current:F0}/{max:F0}";
}
