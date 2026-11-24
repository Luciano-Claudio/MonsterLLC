using System.Collections.Generic;

// Etapas 1+2 (spec 6.3) — sorteio das cartas de uma oferta: até `count` cartas DISTINTAS, sem as que
// já estão no nível máximo e sem as de outro herói. Devolve menos se não houver cartas suficientes.
// Sem alocação além da lista `result` recebida (o buffer de índices é reaproveitado).
public static class CardRules
{
    private static readonly List<int> eligible = new();

    public static void Roll(IReadOnlyList<CardDefinition> pool, IReadOnlyDictionary<string, int> owned, string heroId,
        int count, System.Random rng, List<CardDefinition> result)
    {
        result.Clear();
        eligible.Clear();
        if (pool == null) return;

        for (int i = 0; i < pool.Count; i++)
        {
            var card = pool[i];
            if (card == null) continue;
            if (!string.IsNullOrEmpty(card.heroId) && card.heroId != heroId) continue;
            int level = owned != null && card.id != null && owned.TryGetValue(card.id, out int l) ? l : 0;
            if (level >= card.maxLevel) continue;
            eligible.Add(i);
        }

        // Fisher-Yates parcial: só as `count` primeiras posições.
        int take = System.Math.Min(count, eligible.Count);
        for (int i = 0; i < take; i++)
        {
            int j = i + rng.Next(eligible.Count - i);
            (eligible[i], eligible[j]) = (eligible[j], eligible[i]);
            result.Add(pool[eligible[i]]);
        }
    }

    public static int LevelOf(IReadOnlyDictionary<string, int> owned, CardDefinition card)
        => card != null && owned != null && card.id != null && owned.TryGetValue(card.id, out int l) ? l : 0;
}
