using System;

// 1 linha da "TABELA BASE COMPLETA — DIAS 1 A 30" (docs/new/Mudanca_Estrutural_Mapa_Unico_
// Spawn_Bosses_ATUALIZADO_V2.md, Seção 5) — % de spawn de cada Tier de monstro (T1..T10,
// índice 0..9), separado por período Dia/Noite. Cada array de 10 posições deve somar 100
// (não 1.0 — é % literal, igual a tabela do doc). Fonte de verdade pra composição de Tiers,
// reaproveitada tanto pelo spawn de monstro (fora de escopo aqui) quanto pelo bioma de
// terreno via EffectiveSpawnDay (WorldTierSelector).
[Serializable]
public class DaySpawnDefinition
{
    public int dayNumber;
    public float[] dayWeights = new float[10];
    public float[] nightWeights = new float[10];

    public DaySpawnDefinition() { }

    public DaySpawnDefinition(int dayNumber, float[] dayWeights, float[] nightWeights)
    {
        this.dayNumber = dayNumber;
        this.dayWeights = dayWeights;
        this.nightWeights = nightWeights;
    }
}
