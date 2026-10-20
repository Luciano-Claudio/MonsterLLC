using UnityEngine;
using TMPro;

public class BagIndicatorUI : MonoBehaviour
{
    public TMP_Text label;
    private void OnEnable() => GameEvents.OnBagChanged += UpdateLabel;
    private void OnDisable() => GameEvents.OnBagChanged -= UpdateLabel;
    // Bag.Slots agora é um array fixo de 40 (GDD Seção 37) — Slots.Count sempre é 40, por isso
    // conta quantos dos slots DESTRAVADOS (índice < maxSlots) estão ocupados.
    private void UpdateLabel(Bag bag)
    {
        int used = 0;
        for (int i = 0; i < bag.maxSlots; i++)
            if (!bag.Slots[i].IsEmpty) used++;

        label.text = $"Bag: {used}/{bag.maxSlots} slots";
    }
}
