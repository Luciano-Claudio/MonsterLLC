using System.Collections.Generic;
using UnityEngine;

public class AttackBudgetManager : MonoBehaviour
{
    public static AttackBudgetManager Instance { get; private set; }

    public int meleeBudget = 3; // 🔢 GDD Seção 14 — placeholder, balanceamento real pendente
    public int rangedBudget = 2;

    // GDD Seção 24 (Combat Scope): "Attack Budget considera apenas as ameaças do Floor
    // atual — os budgets Melee/Ranged não se tornam um pool global". Um tracker por Floor,
    // criado sob demanda, para que um slot preso num Floor dormindo (inimigo congelado em
    // pleno ataque) nunca roube capacidade do Floor atualmente ativo.
    private readonly Dictionary<FloorDefinition, AttackBudgetTracker> trackers = new();

    private void Awake() => Instance = this;

    private AttackBudgetTracker GetTracker(FloorDefinition floor)
    {
        if (!trackers.TryGetValue(floor, out var tracker))
        {
            tracker = new AttackBudgetTracker(meleeBudget, rangedBudget);
            trackers[floor] = tracker;
        }
        return tracker;
    }

    public bool TryReserveSlot(FloorDefinition floor, AttackType type) => GetTracker(floor).TryReserve(type);
    public void ReleaseSlot(FloorDefinition floor, AttackType type) => GetTracker(floor).Release(type);
    public int MeleeInUse(FloorDefinition floor) => GetTracker(floor).MeleeInUse;
    public int RangedInUse(FloorDefinition floor) => GetTracker(floor).RangedInUse;
}
