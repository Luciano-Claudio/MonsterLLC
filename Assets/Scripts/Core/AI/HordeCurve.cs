using UnityEngine;

// Etapas 1+2 (docs/superpowers/specs/2026-11-24-etapas-1-2-run-25min-cartas-design.md, 7) — curva da
// horda de 25 min: meta de monstros COMUNS vivos em função do segundo da run (tempo real). Pedido do
// usuário (2026-11-27): de dia sobe aos poucos 10 -> 25 (min 1) -> 50 (min 2) -> 100 (min 3) -> 150 (min 4)
// e fica em 150; à noite cresce conforme a noite (1ª +10%, 2ª +25%, 3ª +50%, da 4ª em diante o dobro),
// sempre limitado ao teto de desempenho (300, medido no D9.3).
[System.Serializable]
public class HordeCurveSettings
{
    [Tooltip("Meta de monstros vivos DE DIA por segundo da run (linear entre as chaves).")]
    public AnimationCurve dayTargetBySecond = LinearCurve(
        new Vector2(0f, 10f), new Vector2(60f, 25f), new Vector2(120f, 50f), new Vector2(180f, 100f),
        new Vector2(240f, 150f), new Vector2(1500f, 150f)); // 🔢

    [Tooltip("À noite a meta é a do dia vezes isto, por noite (1ª, 2ª, 3ª...). Depois da última, repete a última.")]
    public float[] nightMultipliers = { 1.10f, 1.25f, 1.50f, 2f }; // 🔢

    [Tooltip("Limite absoluto de monstros comuns vivos (desempenho, medido no D9.3).")]
    public int hardCap = 300; // 🔢

    public float NightMultiplier(int nightNumber)
    {
        if (nightNumber <= 0 || nightMultipliers == null || nightMultipliers.Length == 0) return 1f;
        return nightMultipliers[Mathf.Min(nightNumber, nightMultipliers.Length) - 1];
    }

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

    // nightNumber: 0 = dia; 1 = 1ª noite... (RunSchedule.NightNumber). extraMultiplier: Maldição da loja
    // permanente (+20% por nível). Sempre abaixo do hardCap.
    public int TargetAlive(float runSeconds, int nightNumber, float extraMultiplier = 1f)
    {
        float target = dayTargetBySecond != null ? dayTargetBySecond.Evaluate(runSeconds) : 0f;
        target *= NightMultiplier(nightNumber);
        target *= Mathf.Max(0f, extraMultiplier);
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
