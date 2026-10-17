using System.Collections.Generic;
using UnityEngine;

// "A tabela não é hardcoded" (docs/new/Mudanca_Estrutural_Mapa_Unico_Spawn_Bosses_ATUALIZADO_
// V2.md, Seção 6.1) — fica num asset editável, não espalhada pelo código. `days` já nasce
// preenchido com a Seção 5 inteira (os 30 dias) quando o asset é criado pelo menu — editável
// depois no Inspector pra balanceamento, sem precisar mexer em código.
[CreateAssetMenu(fileName = "DaySpawnTable", menuName = "MonsterLLC/World Generation/Day Spawn Table")]
public class DaySpawnTableSO : ScriptableObject
{
    public List<DaySpawnDefinition> days = new()
    {
        new DaySpawnDefinition(1,  new float[] { 100, 0, 0, 0, 0, 0, 0, 0, 0, 0 },   new float[] { 70, 30, 0, 0, 0, 0, 0, 0, 0, 0 }),
        new DaySpawnDefinition(2,  new float[] { 70, 30, 0, 0, 0, 0, 0, 0, 0, 0 },   new float[] { 50, 50, 0, 0, 0, 0, 0, 0, 0, 0 }),
        new DaySpawnDefinition(3,  new float[] { 35, 50, 15, 0, 0, 0, 0, 0, 0, 0 },  new float[] { 10, 60, 30, 0, 0, 0, 0, 0, 0, 0 }),
        new DaySpawnDefinition(4,  new float[] { 10, 25, 50, 15, 0, 0, 0, 0, 0, 0 }, new float[] { 10, 15, 50, 25, 0, 0, 0, 0, 0, 0 }),
        new DaySpawnDefinition(5,  new float[] { 10, 25, 30, 35, 0, 0, 0, 0, 0, 0 }, new float[] { 5, 15, 20, 45, 15, 0, 0, 0, 0, 0 }),
        new DaySpawnDefinition(6,  new float[] { 5, 20, 25, 35, 15, 0, 0, 0, 0, 0 }, new float[] { 5, 15, 20, 35, 25, 0, 0, 0, 0, 0 }),
        new DaySpawnDefinition(7,  new float[] { 5, 15, 20, 30, 30, 0, 0, 0, 0, 0 }, new float[] { 5, 10, 15, 30, 40, 0, 0, 0, 0, 0 }),
        new DaySpawnDefinition(8,  new float[] { 5, 10, 15, 30, 40, 0, 0, 0, 0, 0 }, new float[] { 5, 10, 15, 25, 35, 10, 0, 0, 0, 0 }),
        new DaySpawnDefinition(9,  new float[] { 5, 10, 15, 25, 35, 10, 0, 0, 0, 0 }, new float[] { 5, 5, 10, 20, 35, 25, 0, 0, 0, 0 }),
        new DaySpawnDefinition(10, new float[] { 5, 5, 10, 20, 35, 25, 0, 0, 0, 0 }, new float[] { 5, 5, 10, 15, 30, 35, 0, 0, 0, 0 }),
        new DaySpawnDefinition(11, new float[] { 5, 5, 10, 15, 30, 35, 0, 0, 0, 0 }, new float[] { 5, 5, 5, 10, 25, 40, 10, 0, 0, 0 }),
        new DaySpawnDefinition(12, new float[] { 5, 5, 5, 10, 25, 40, 10, 0, 0, 0 }, new float[] { 5, 5, 5, 10, 20, 35, 20, 0, 0, 0 }),
        new DaySpawnDefinition(13, new float[] { 5, 5, 5, 10, 20, 35, 20, 0, 0, 0 }, new float[] { 5, 5, 5, 10, 15, 30, 30, 0, 0, 0 }),
        new DaySpawnDefinition(14, new float[] { 5, 5, 5, 10, 15, 30, 30, 0, 0, 0 }, new float[] { 5, 5, 5, 5, 15, 25, 30, 10, 0, 0 }),
        new DaySpawnDefinition(15, new float[] { 5, 5, 5, 5, 15, 25, 30, 10, 0, 0 }, new float[] { 5, 5, 5, 5, 10, 20, 30, 20, 0, 0 }),
        new DaySpawnDefinition(16, new float[] { 5, 5, 5, 5, 10, 20, 30, 20, 0, 0 }, new float[] { 5, 5, 5, 5, 10, 15, 25, 30, 0, 0 }),
        new DaySpawnDefinition(17, new float[] { 5, 5, 5, 5, 10, 15, 25, 30, 0, 0 }, new float[] { 5, 5, 5, 5, 5, 15, 20, 30, 10, 0 }),
        new DaySpawnDefinition(18, new float[] { 5, 5, 5, 5, 5, 15, 20, 30, 10, 0 }, new float[] { 5, 5, 5, 5, 5, 10, 20, 25, 20, 0 }),
        new DaySpawnDefinition(19, new float[] { 5, 5, 5, 5, 5, 10, 20, 25, 20, 0 }, new float[] { 5, 5, 5, 5, 5, 10, 15, 25, 25, 0 }),
        new DaySpawnDefinition(20, new float[] { 5, 5, 5, 5, 5, 10, 15, 20, 25, 5 }, new float[] { 5, 5, 5, 5, 5, 5, 10, 20, 25, 15 }),
        new DaySpawnDefinition(21, new float[] { 5, 5, 5, 5, 5, 5, 10, 20, 25, 15 }, new float[] { 5, 5, 5, 5, 5, 5, 10, 15, 25, 20 }),
        new DaySpawnDefinition(22, new float[] { 5, 5, 5, 5, 5, 5, 10, 15, 25, 20 }, new float[] { 5, 5, 5, 5, 5, 5, 10, 15, 20, 25 }),
        new DaySpawnDefinition(23, new float[] { 5, 5, 5, 5, 5, 5, 10, 15, 20, 25 }, new float[] { 5, 5, 5, 5, 5, 5, 5, 15, 20, 30 }),
        new DaySpawnDefinition(24, new float[] { 5, 5, 5, 5, 5, 5, 5, 15, 20, 30 }, new float[] { 5, 5, 5, 5, 5, 5, 5, 10, 20, 35 }),
        new DaySpawnDefinition(25, new float[] { 5, 5, 5, 5, 5, 5, 5, 10, 20, 35 }, new float[] { 5, 5, 5, 5, 5, 5, 5, 10, 15, 40 }),
        new DaySpawnDefinition(26, new float[] { 5, 5, 5, 5, 5, 5, 5, 10, 15, 40 }, new float[] { 5, 5, 5, 5, 5, 5, 5, 10, 10, 45 }),
        new DaySpawnDefinition(27, new float[] { 5, 5, 5, 5, 5, 5, 5, 10, 10, 45 }, new float[] { 4, 4, 4, 4, 4, 5, 5, 10, 10, 50 }),
        new DaySpawnDefinition(28, new float[] { 4, 4, 4, 4, 4, 5, 5, 10, 10, 50 }, new float[] { 4, 4, 4, 4, 4, 5, 5, 5, 10, 55 }),
        new DaySpawnDefinition(29, new float[] { 4, 4, 4, 4, 4, 5, 5, 5, 10, 55 }, new float[] { 3, 3, 3, 3, 3, 5, 5, 5, 10, 60 }),
        new DaySpawnDefinition(30, new float[] { 3, 3, 3, 3, 3, 5, 5, 5, 10, 60 }, new float[] { 2, 2, 2, 2, 2, 5, 5, 5, 10, 65 }),
    };

    // `dayNumber` fora de 1..30 usa o dia mais próximo (clamp) — "Dia 30 pra sempre" depois do
    // fim da tabela, confirmado com o usuário (não existe mapa/sprint além do Dia 30 ainda).
    public DaySpawnDefinition GetDay(int dayNumber)
    {
        if (days == null || days.Count == 0) return null;

        int clamped = Mathf.Clamp(dayNumber, 1, 30);
        foreach (var entry in days)
        {
            if (entry != null && entry.dayNumber == clamped) return entry;
        }

        return days[days.Count - 1];
    }

    // "Validação automática... o Editor deve emitir um Warning claro" (Seção 6.2) — confere
    // que cada linha (Dia e Noite) soma exatamente 100, direto ao editar o asset no Inspector.
    private void OnValidate()
    {
        if (days == null) return;

        foreach (var entry in days)
        {
            if (entry == null) continue;
            WarnIfNot100(entry.dayNumber, "Dia", entry.dayWeights);
            WarnIfNot100(entry.dayNumber, "Noite", entry.nightWeights);
        }
    }

    private void WarnIfNot100(int dayNumber, string period, float[] weights)
    {
        if (weights == null || weights.Length != 10)
        {
            Debug.LogWarning($"[DaySpawnTableSO] Dia {dayNumber} ({period}): precisa ter exatamente 10 valores (T1..T10).", this);
            return;
        }

        float sum = 0f;
        foreach (var w in weights) sum += w;

        if (Mathf.Abs(sum - 100f) > 0.01f)
        {
            Debug.LogWarning($"[DaySpawnTableSO] Dia {dayNumber} ({period}): soma {sum}%, deveria ser 100%.", this);
        }
    }
}
