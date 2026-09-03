using UnityEngine;
using TMPro;

public class AttackBudgetIndicatorUI : MonoBehaviour
{
    public TMP_Text label;

    private void Update()
    {
        if (AttackBudgetManager.Instance == null) return;
        label.text = $"Melee: {AttackBudgetManager.Instance.MeleeInUse}/{AttackBudgetManager.Instance.meleeBudget} | " +
                      $"Ranged: {AttackBudgetManager.Instance.RangedInUse}/{AttackBudgetManager.Instance.rangedBudget}";
    }
}
