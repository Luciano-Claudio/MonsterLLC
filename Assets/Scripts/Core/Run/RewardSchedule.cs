// Etapa 3 (docs/superpowers/specs/2026-11-24-etapa-3-bau-vendedor-itens-design.md, seção 6) — em que
// segundos da run o baú (ou o vendedor) aparece. Sorteia N entre mín/máx, divide [quiet, run) em N
// faixas iguais e põe 1 aparição num segundo aleatório de cada faixa: espalhado, nunca no começo.
public static class RewardSchedule
{
    public static float[] Build(int minCount, int maxCount, float runSeconds, float quietSeconds, System.Random rng)
    {
        if (maxCount < minCount) maxCount = minCount;
        int count = minCount <= 0 && maxCount <= 0 ? 0 : rng.Next(System.Math.Max(0, minCount), maxCount + 1);
        float span = runSeconds - quietSeconds;
        if (count <= 0 || span <= 0f) return new float[0];

        var times = new float[count];
        float width = span / count;
        for (int i = 0; i < count; i++)
        {
            float start = quietSeconds + i * width;
            // [start, start + width): o fim da faixa fica de fora (vira o começo da próxima).
            times[i] = start + (float)rng.NextDouble() * width * 0.999f;
        }
        return times;
    }
}
