using System.Collections.Generic;
using System.Linq;

public static class StairRouting
{
    // Próxima Active Floor Position acima da atual, ou null se já for o topo ativo.
    public static int? GetNextPosition(IEnumerable<int> activePositions, int currentPosition)
    {
        var ordered = activePositions.Distinct().OrderBy(p => p).ToList();
        int index = ordered.IndexOf(currentPosition);
        if (index == -1 || index == ordered.Count - 1) return null;
        return ordered[index + 1];
    }

    // Active Floor Position anterior à atual, ou null se já for o térreo.
    public static int? GetPreviousPosition(IEnumerable<int> activePositions, int currentPosition)
    {
        var ordered = activePositions.Distinct().OrderBy(p => p).ToList();
        int index = ordered.IndexOf(currentPosition);
        if (index <= 0) return null;
        return ordered[index - 1];
    }
}
