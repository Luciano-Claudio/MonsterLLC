using System.Globalization;

public static class LargeNumberFormatter
{
    private static readonly string[] Suffixes = { "", "k", "m", "b", "t" };

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
