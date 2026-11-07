using System.Collections.Generic;
using UnityEngine;

// Tabela fixa de 10 dias (docs/new/Mudanca_Estrutural_Jogo_Incremental_Deadline_8_1.md, Seção 2 —
// substitui a tabela antiga de 30 dias da doc do Mapa Único) — fica num asset editável, não
// espalhada pelo código. `days` já nasce preenchido com a Seção 2 inteira quando o asset é criado
// pelo menu — editável depois no Inspector pra balanceamento, sem precisar mexer em código. Dias
// 8-10 são 100% de 1 Tier só, de propósito (sem blend de transição — reta final "seca").
[CreateAssetMenu(fileName = "DaySpawnTable", menuName = "MonsterLLC/World Generation/Day Spawn Table")]
public class DaySpawnTableSO : ScriptableObject
{
    public List<DaySpawnDefinition> days = new()
    {
        new DaySpawnDefinition(1,  new float[] { 100, 0, 0, 0, 0, 0, 0, 0, 0, 0 },  new float[] { 80, 20, 0, 0, 0, 0, 0, 0, 0, 0 }),
        new DaySpawnDefinition(2,  new float[] { 20, 80, 0, 0, 0, 0, 0, 0, 0, 0 },  new float[] { 0, 80, 20, 0, 0, 0, 0, 0, 0, 0 }),
        new DaySpawnDefinition(3,  new float[] { 0, 20, 80, 0, 0, 0, 0, 0, 0, 0 },  new float[] { 0, 0, 80, 20, 0, 0, 0, 0, 0, 0 }),
        new DaySpawnDefinition(4,  new float[] { 0, 0, 20, 80, 0, 0, 0, 0, 0, 0 },  new float[] { 0, 0, 0, 80, 20, 0, 0, 0, 0, 0 }),
        new DaySpawnDefinition(5,  new float[] { 0, 0, 0, 20, 80, 0, 0, 0, 0, 0 },  new float[] { 0, 0, 0, 0, 80, 20, 0, 0, 0, 0 }),
        new DaySpawnDefinition(6,  new float[] { 0, 0, 0, 0, 20, 80, 0, 0, 0, 0 },  new float[] { 0, 0, 0, 0, 0, 80, 20, 0, 0, 0 }),
        new DaySpawnDefinition(7,  new float[] { 0, 0, 0, 0, 0, 20, 80, 0, 0, 0 },  new float[] { 0, 0, 0, 0, 0, 0, 80, 20, 0, 0 }),
        new DaySpawnDefinition(8,  new float[] { 0, 0, 0, 0, 0, 0, 0, 100, 0, 0 },  new float[] { 0, 0, 0, 0, 0, 0, 0, 100, 0, 0 }),
        new DaySpawnDefinition(9,  new float[] { 0, 0, 0, 0, 0, 0, 0, 0, 100, 0 },  new float[] { 0, 0, 0, 0, 0, 0, 0, 0, 100, 0 }),
        new DaySpawnDefinition(10, new float[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 100 },  new float[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 100 }),
    };

    // `dayNumber` fora de 1..10 usa o dia mais próximo (clamp) — Dia 10 "pra sempre" depois do
    // fim da tabela (teto fixo da Seção 2, substitui o antigo "Dia 30 pra sempre").
    public DaySpawnDefinition GetDay(int dayNumber)
    {
        if (days == null || days.Count == 0) return null;

        int clamped = Mathf.Clamp(dayNumber, 1, 10);
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
