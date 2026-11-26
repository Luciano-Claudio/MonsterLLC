using System.Globalization;

public static class LargeNumberFormatter
{
    private static readonly string[] Suffixes = { "", "k", "m", "b", "t" };

    // Vida atual: arredonda pra CIMA (vivo com 0,4 mostra "1"; "0" só quando morreu de verdade —
    // HealthSystem.IsDead é <= 0). Bug real 2026-11-26: "0" de vida e o herói ainda usava o Shift.
    public static string FormatHealth(double value)
    {
        if (value <= 0) return "0";
        return value < 1000 ? System.Math.Ceiling(value).ToString("0", CultureInfo.InvariantCulture) : Format(value);
    }

    public static string Format(double value)
    {
        if (value < 1000) return value.ToString("0", CultureInfo.InvariantCulture);

        int suffixIndex = 0;
        double reduced = value;

        while (reduced >= 1000 && suffixIndex < Suffixes.Length - 1)
        {
            reduced /= 1000;
            suffixIndex++;
        }

        return reduced.ToString("0.#", CultureInfo.InvariantCulture) + Suffixes[suffixIndex];
    }
}
