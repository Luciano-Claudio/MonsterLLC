using UnityEngine;
using TMPro;

public class AttackBudgetIndicatorUI : MonoBehaviour
{
    public TMP_Text label;

    private void Update()
    {
        if (AttackBudgetManager.Instance == null || FloorManager.Instance == null) return;

        var floor = FloorManager.Instance.CurrentFloor;
        label.text = $"Melee: {AttackBudgetManager.Instance.MeleeInUse(floor)}/{AttackBudgetManager.Instance.meleeBudget} | " +
                      $"Ranged: {AttackBudgetManager.Instance.RangedInUse(floor)}/{AttackBudgetManager.Instance.rangedBudget}";
    }
}
