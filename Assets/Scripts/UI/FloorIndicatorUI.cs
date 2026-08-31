using UnityEngine;
using TMPro;

public class FloorIndicatorUI : MonoBehaviour
{
    public TMP_Text label;

    private void OnEnable() => GameEvents.OnFloorChanged += UpdateLabel;
    private void OnDisable() => GameEvents.OnFloorChanged -= UpdateLabel;

    private void UpdateLabel(FloorDefinition floor) => label.text = $"Floor: {floor.floorName}";
}
