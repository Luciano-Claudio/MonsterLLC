using UnityEngine;

// D9.4 — curva da horda (docs/superpowers/specs/2026-11-20-d9-4-horda-cerco-design.md, 4.1-4.4).
// A meta é a quantidade de monstros COMUNS vivos ao redor do herói, em função do TEMPO REAL do
// dia (não do cronômetro: o cronômetro pausa com boss vivo, a horda não — quem demora pra matar
// boss enfrenta mais monstros) e do dia da run (cada dia tem começo e teto próprios).

[System.Serializable]
public struct DayPopulation
{
    public int start;   // 🔢 meta no início do dia
    public int ceiling; // 🔢 meta depois de secondsToCeiling
}

[System.Serializable]
public class HordeCurveSettings
{
    [Tooltip("Forma da subida, compartilhada por todos os dias: tempo normalizado (0 = início do dia, 1 = secondsToCeiling) -> fração entre o começo e o teto do dia.")]
    public AnimationCurve shape = new AnimationCurve(
        new Keyframe(0f, 0f), new Keyframe(0.125f, 0.0357f), new Keyframe(0.25f, 0.1429f),
        new Keyframe(0.5f, 0.3571f), new Keyframe(0.75f, 0.6429f), new Keyframe(1f, 1f));

    [Tooltip("Segundos de tempo real até a meta chegar no teto do dia. Depois disso fica no teto.")]
    public float secondsToCeiling = 240f; // 🔢

    [Tooltip("Começo e teto por dia (índice 0 = Dia 1). Dia acima do tamanho da lista usa a última linha.")]
    public DayPopulation[] days =
    {
        new DayPopulation { start = 10, ceiling = 150 },
        new DayPopulation { start = 15, ceiling = 180 },
        new DayPopulation { start = 25, ceiling = 210 },
        new DayPopulation { start = 30, ceiling = 240 },
        new DayPopulation { start = 40, ceiling = 270 },
        new DayPopulation { start = 50, ceiling = 300 },
        new DayPopulation { start = 50, ceiling = 300 },
        new DayPopulation { start = 50, ceiling = 300 },
        new DayPopulation { start = 50, ceiling = 300 },
        new DayPopulation { start = 50, ceiling = 300 },
    };

    [Tooltip("De quanto em quanto tempo (s) vem um pico. O primeiro é em spikeInterval.")]
    public float spikeInterval = 60f; // 🔢
    [Tooltip("Duração (s) de cada pico.")]
    public float spikeDuration = 15f; // 🔢
    [Tooltip("Multiplicador da meta durante o pico.")]
    public float spikeMultiplier = 1.5f; // 🔢
    [Tooltip("Limite absoluto de monstros comuns vivos (desempenho, medido no D9.3).")]
    public int hardCap = 300; // 🔢

    public int TargetAlive(float elapsedSeconds, int day)
    {
        if (days == null || days.Length == 0) return 0;
        var row = days[Mathf.Clamp(day, 1, days.Length) - 1];

        float t = secondsToCeiling > 0f ? Mathf.Clamp01(elapsedSeconds / secondsToCeiling) : 1f;
        float target = row.start + (row.ceiling - row.start) * shape.Evaluate(t);

        if (spikeInterval > 0f && elapsedSeconds >= spikeInterval && (elapsedSeconds % spikeInterval) < spikeDuration)
            target *= spikeMultiplier;

        return Mathf.Min(hardCap, Mathf.RoundToInt(target));
    }
}

// D9.4 — monstros que atiram de longe ficam fora dos primeiros segundos, só no Dia 1.
public static class RangedGate
{
    public static bool Blocks(int day, float elapsedSeconds, float unlockSeconds) =>
        day == 1 && elapsedSeconds < unlockSeconds;
}

// D9.4 — relógio da horda: tempo real do dia. Zera no início do dia e só anda com o jogo ativo
// (nunca durante loading, pausa ou animação de morte — quem chama decide o "active").
public class HordeClock
{
    public float Elapsed { get; private set; }

    public void Reset(float startOffsetSeconds) => Elapsed = Mathf.Max(0f, startOffsetSeconds);

    public void Tick(float deltaTime, bool active)
    {
        if (active) Elapsed += deltaTime;
    }
}
