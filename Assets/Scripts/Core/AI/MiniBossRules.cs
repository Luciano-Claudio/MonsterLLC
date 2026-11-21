using UnityEngine;

// Mini bosses (docs/superpowers/specs/2026-11-21-mini-bosses-design.md): monstros comuns mais
// fortes (ex-bosses) que nascem sozinhos a cada 30-40 s, pelo tier do dia, sem repetir o último.
public static class MiniBossRules
{
    // Índice sorteado de forma uniforme entre os elegíveis, sem o último que nasceu — a menos que
    // ele seja o único elegível (pra o spawn não travar). -1 = nenhum elegível. Não aloca.
    public static int Pick(bool[] eligible, int count, int lastIndex, float random01)
    {
        int total = 0, others = 0;
        for (int i = 0; i < count; i++)
        {
            if (!eligible[i]) continue;
            total++;
            if (i != lastIndex) others++;
        }
        if (total == 0) return -1;

        bool skipLast = others > 0;
        int n = skipLast ? others : total;
        int k = Mathf.Min(n - 1, Mathf.FloorToInt(Mathf.Clamp01(random01) * n));
        for (int i = 0; i < count; i++)
        {
            if (!eligible[i] || (skipLast && i == lastIndex)) continue;
            if (k == 0) return i;
            k--;
        }
        return -1;
    }

    public static float NextInterval(float min, float max, float random01) => Mathf.Lerp(min, max, random01);
}
