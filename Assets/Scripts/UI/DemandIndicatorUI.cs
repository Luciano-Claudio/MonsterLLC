using UnityEngine;
using TMPro;

public class DemandIndicatorUI : MonoBehaviour
{
    public TMP_Text label;
    private void OnEnable() => GameEvents.OnDemandChanged += UpdateLabel;
    private void OnDisable() => GameEvents.OnDemandChanged -= UpdateLabel;
    private void UpdateLabel(long sold, long target) => label.text = $"Demand: {sold}/{target} Monster Essence";
}
