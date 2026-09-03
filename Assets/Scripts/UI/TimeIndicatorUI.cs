using UnityEngine;
using TMPro;

public class TimeIndicatorUI : MonoBehaviour
{
    public TMP_Text label;
    private void OnEnable() => GameEvents.OnTimeChanged += UpdateLabel;
    private void OnDisable() => GameEvents.OnTimeChanged -= UpdateLabel;
    private void UpdateLabel(float time) => label.text = $"Time: {time:F0}s";
}
