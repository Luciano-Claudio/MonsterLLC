using System.Collections.Generic;
using UnityEngine;

// Stats do Chest Mimic por dia (GDD Deadline 8.1, Seção 8, pendência do Baú) — decisão
// explícita do usuário: "o monstro mais forte do tier do dia" vira um valor MANUAL aqui, nunca
// um código que compara monstros pra descobrir qual é o mais forte. Mesmo padrão/formato de
// DayGoldTableSO — fora de 1..10 usa o dia mais próximo (clamp).
[CreateAssetMenu(fileName = "ChestMimicStatTable", menuName = "MonsterLLC/Economy/Chest Mimic Stat Table")]
public class ChestMimicStatTableSO : ScriptableObject
{
    public List<ChestMimicStatDefinition> days = new()
    {
        new ChestMimicStatDefinition(1, 8, 10),
    };

    public ChestMimicStatDefinition GetDay(int dayNumber)
    {
        if (days == null || days.Count == 0) return null;

        int clamped = Mathf.Clamp(dayNumber, 1, 10);
        foreach (var entry in days)
        {
            if (entry != null && entry.dayNumber == clamped) return entry;
        }

        return days[days.Count - 1];
    }
}

[System.Serializable]
public class ChestMimicStatDefinition
{
    public int dayNumber;
    public float health;
    public float attackDamage;

    public ChestMimicStatDefinition(int dayNumber, float health, float attackDamage)
    {
        this.dayNumber = dayNumber;
        this.health = health;
        this.attackDamage = attackDamage;
    }
}
