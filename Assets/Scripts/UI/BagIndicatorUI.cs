using UnityEngine;
using TMPro;

public class BagIndicatorUI : MonoBehaviour
{
    public TMP_Text label;
    private void OnEnable() => GameEvents.OnBagChanged += UpdateLabel;
    private void OnDisable() => GameEvents.OnBagChanged -= UpdateLabel;
    private void UpdateLabel(Bag bag) => label.text = $"Bag: {bag.Slots.Count}/{bag.maxSlots} slots";
}
