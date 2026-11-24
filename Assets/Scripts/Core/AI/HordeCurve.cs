using UnityEngine;

// Etapas 1+2 (docs/superpowers/specs/2026-11-24-etapas-1-2-run-25min-cartas-design.md, 7) — curva da
// horda de 25 min: meta de monstros COMUNS vivos em função do segundo da run (tempo real). Pedido do
// usuário: começa baixo e sobe (~50 no minuto 1, ~150 no minuto 3, de dia); à noite o DOBRO, sempre
// limitado ao teto de desempenho (300, medido no D9.3). Os picos periódicos saíram: a noite é o pico.
[System.Serializable]
public class HordeCurveSettings
{
    [Tooltip("Meta de monstros vivos DE DIA por segundo da run (linear entre as chaves).")]
    public AnimationCurve dayTargetBySecond = LinearCurve(
        new Vector2(0f, 10f), new Vector2(60f, 50f), new Vector2(180f, 150f), new Vector2(1500f, 150f)); // 🔢

    [Tooltip("À noite a meta é a do dia vezes isto.")]
    public float nightMultiplier = 2f; // 🔢

    [Tooltip("Limite absoluto de monstros comuns vivos (desempenho, medido no D9.3).")]
    public int hardCap = 300; // 🔢

    // Curva com tangentes lineares (a subida não "estaciona" em cada chave, como faria com inclinação zero).
    public static AnimationCurve LinearCurve(params Vector2[] points)
    {
        var keys = new Keyframe[points.Length];
        for (int i = 0; i < points.Length; i++)
        {
            float inSlope = i > 0 ? (points[i].y - points[i - 1].y) / (points[i].x - points[i - 1].x) : 0f;
            float outSlope = i < points.Length - 1 ? (points[i + 1].y - points[i].y) / (points[i + 1].x - points[i].x) : 0f;
            if (i == 0) inSlope = outSlope;
            if (i == points.Length - 1) outSlope = inSlope;
            keys[i] = new Keyframe(points[i].x, points[i].y, inSlope, outSlope);
        }
        return new AnimationCurve(keys);
    }

    public int TargetAlive(float runSeconds, bool isNight)
    {
        float target = dayTargetBySecond != null ? dayTargetBySecond.Evaluate(runSeconds) : 0f;
        if (isNight) target *= nightMultiplier;
        return Mathf.Min(hardCap, Mathf.RoundToInt(target));
    }
}

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
