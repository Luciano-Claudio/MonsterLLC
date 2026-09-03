using UnityEngine;
using TMPro;

public class GoldIndicatorUI : MonoBehaviour
{
    public TMP_Text label;
    private void OnEnable() => GameEvents.OnGoldChanged += UpdateLabel;
    private void OnDisable() => GameEvents.OnGoldChanged -= UpdateLabel;
    private void UpdateLabel(long gold) => label.text = $"Gold: {LargeNumberFormatter.Format(gold)}";
}
