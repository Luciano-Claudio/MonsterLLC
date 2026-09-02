using UnityEngine;
using TMPro;

public class EnergyIndicatorUI : MonoBehaviour
{
    public TMP_Text label;

    private void OnEnable() => GameEvents.OnEnergyChanged += UpdateLabel;
    private void OnDisable() => GameEvents.OnEnergyChanged -= UpdateLabel;

    private void UpdateLabel(float current, float max) => label.text = $"Energy: {current:F0}/{max:F0}";
}
