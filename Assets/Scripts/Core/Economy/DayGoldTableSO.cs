using System.Collections.Generic;
using UnityEngine;

// Tabela fixa de 10 dias (GDD Deadline 8.1, Seção 3.1) — gold fixo por kill de monstro comum e
// de boss, crescendo junto com o Tier de ameaça do dia (ver DaySpawnTableSO). Dia 1 fixado pelo
// usuário (1/20); dia 2 também (100/500, usado como referência de "ordem de grandeza" pro
// resto); dias 3-10 interpolados por mim numa escala ~4-5x/dia, chegando na casa dos milhões no
// Dia 10 — valor de balanceamento solto de propósito (usuário: "ainda não entendo bem a economia
// do jogo, buffa ou nerfa depois"), editável no Inspector sem mexer em código.
[CreateAssetMenu(fileName = "DayGoldTable", menuName = "MonsterLLC/Economy/Day Gold Table")]
public class DayGoldTableSO : ScriptableObject
{
    public List<DayGoldDefinition> days = new()
    {
        new DayGoldDefinition(1, 1, 20),
        new DayGoldDefinition(2, 100, 500),
        new DayGoldDefinition(3, 500, 2_500),
        new DayGoldDefinition(4, 2_000, 10_000),
        new DayGoldDefinition(5, 10_000, 50_000),
        new DayGoldDefinition(6, 50_000, 250_000),
        new DayGoldDefinition(7, 200_000, 1_000_000),
        new DayGoldDefinition(8, 1_000_000, 5_000_000),
        new DayGoldDefinition(9, 4_000_000, 20_000_000),
        new DayGoldDefinition(10, 15_000_000, 75_000_000),
    };

    // Mesmo critério de DaySpawnTableSO.GetDay — fora de 1..10 usa o dia mais próximo (clamp).
    public DayGoldDefinition GetDay(int dayNumber)
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
