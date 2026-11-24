using System.Collections.Generic;

// Etapa 3 (docs/superpowers/specs/2026-11-24-etapa-3-bau-vendedor-itens-design.md, 5.1) — de onde vem a oferta.
public enum OfferSource { LevelUp, Chest, Vendor }

// Números da oferta (RunSettings): espaços de item, Stat a cada N níveis, chance de Stat no baú.
public struct OfferRules
{
    public int maxItemSlots;
    public int statEveryLevels;
    public float chestStatChance01;

    public static OfferRules From(RunSettings s) => new OfferRules
    {
        maxItemSlots = s != null ? s.maxItemSlots : 6,
        statEveryLevels = s != null ? s.statEveryLevels : 5,
        chestStatChance01 = s != null ? s.chestStatChancePercent / 100f : 0.15f,
    };
}

// Sorteio das cartas de uma oferta: até `count` cartas DISTINTAS, sem as que já estão no nível
// máximo e sem as de outro herói. Devolve menos se não houver cartas suficientes.
// Sem alocação além da lista `result` recebida (os buffers de índices são reaproveitados).
public static class CardRules
{
    private const int AllKinds = ~0;
    private static readonly List<int> eligible = new();
    private static readonly List<int> statEligible = new();

    private static int KindBit(CardKind k) => 1 << (int)k;

    // Etapas 1+2: sorteio de qualquer tipo (mantido para quem não tem fonte).
    public static void Roll(IReadOnlyList<CardDefinition> pool, IReadOnlyDictionary<string, int> owned, string heroId,
        int count, System.Random rng, List<CardDefinition> result)
    {
        result.Clear();
        Collect(pool, owned, heroId, AllKinds, false, eligible);
        TakeRandom(pool, eligible, count, rng, result);
    }

    // Etapa 3 (spec 5.1):
    // - LevelUp em nível múltiplo de statEveryLevels: só Stats (sem Stat possível -> oferta comum);
    // - LevelUp comum: Upgrade + Item, sorteio igual;
    // - Chest: Upgrades; cada carta, com chestStatChance01, vira um Stat distinto;
    // - Vendor: só Itens.
    // Em toda fonte, com maxItemSlots itens já possuídos, item novo não entra.
    public static void RollOffer(OfferSource source, int heroLevel, IReadOnlyList<CardDefinition> pool,
        IReadOnlyDictionary<string, int> owned, string heroId, int count, System.Random rng, OfferRules rules,
        List<CardDefinition> result)
    {
        result.Clear();
        bool itemsFull = CountOwnedItems(pool, owned) >= rules.maxItemSlots;
        switch (source)
        {
            case OfferSource.LevelUp:
                if (rules.statEveryLevels > 0 && heroLevel > 0 && heroLevel % rules.statEveryLevels == 0)
                {
                    Collect(pool, owned, heroId, KindBit(CardKind.Stat), itemsFull, eligible);
                    TakeRandom(pool, eligible, count, rng, result);
                    if (result.Count > 0) return;
                }
                Collect(pool, owned, heroId, KindBit(CardKind.Upgrade) | KindBit(CardKind.Item), itemsFull, eligible);
                TakeRandom(pool, eligible, count, rng, result);
                return;

            case OfferSource.Vendor:
                Collect(pool, owned, heroId, KindBit(CardKind.Item), itemsFull, eligible);
                TakeRandom(pool, eligible, count, rng, result);
                return;

            case OfferSource.Chest:
                Collect(pool, owned, heroId, KindBit(CardKind.Upgrade), itemsFull, eligible);
                TakeRandom(pool, eligible, count, rng, result);
                Collect(pool, owned, heroId, KindBit(CardKind.Stat), itemsFull, statEligible);
                for (int i = 0; i < result.Count && statEligible.Count > 0; i++)
                {
                    if (rng.NextDouble() >= rules.chestStatChance01) continue;
                    int pick = rng.Next(statEligible.Count);
                    result[i] = pool[statEligible[pick]];
                    statEligible.RemoveAt(pick);
                }
                // Upgrades acabaram (todos no máximo): completa com Stats — o baú nunca sai vazio à toa.
                while (result.Count < count && statEligible.Count > 0)
                {
                    int pick = rng.Next(statEligible.Count);
                    result.Add(pool[statEligible[pick]]);
                    statEligible.RemoveAt(pick);
                }
                return;
        }
    }

    // Quantos itens DIFERENTES o herói tem (cada um ocupa 1 dos 6 espaços).
    public static int CountOwnedItems(IReadOnlyList<CardDefinition> pool, IReadOnlyDictionary<string, int> owned)
    {
        int n = 0;
        if (pool == null) return 0;
        for (int i = 0; i < pool.Count; i++)
        {
            var c = pool[i];
            if (c != null && c.kind == CardKind.Item && LevelOf(owned, c) > 0) n++;
        }
        return n;
    }

    public static int LevelOf(IReadOnlyDictionary<string, int> owned, CardDefinition card)
        => card != null && owned != null && card.id != null && owned.TryGetValue(card.id, out int l) ? l : 0;

    private static void Collect(IReadOnlyList<CardDefinition> pool, IReadOnlyDictionary<string, int> owned, string heroId,
        int kindMask, bool itemsFull, List<int> into)
    {
        into.Clear();
        if (pool == null) return;
        for (int i = 0; i < pool.Count; i++)
        {
            var card = pool[i];
            if (card == null) continue;
            if ((kindMask & KindBit(card.kind)) == 0) continue;
            if (!string.IsNullOrEmpty(card.heroId) && card.heroId != heroId) continue;
            int level = LevelOf(owned, card);
            if (level >= card.maxLevel) continue;
            if (itemsFull && card.kind == CardKind.Item && level == 0) continue; // 7º item não cabe
            into.Add(i);
        }
    }

    // Fisher-Yates parcial: só as `count` primeiras posições.
    private static void TakeRandom(IReadOnlyList<CardDefinition> pool, List<int> from, int count, System.Random rng, List<CardDefinition> result)
    {
        int take = System.Math.Min(count, from.Count);
        for (int i = 0; i < take; i++)
        {
            int j = i + rng.Next(from.Count - i);
            (from[i], from[j]) = (from[j], from[i]);
            result.Add(pool[from[i]]);
        }
    }
}
