using System.Collections.Generic;

// Etapa 3 (docs/superpowers/specs/2026-11-24-etapa-3-bau-vendedor-itens-design.md, 5.1) — de onde vem a oferta.
public enum OfferSource { LevelUp, Chest, Vendor }

// Números da oferta (RunSettings): espaços de item, Stat a cada N níveis, chance de Stat no baú.
public struct OfferRules
{
    public int maxItemSlots;
    public int statEveryLevels;
    public float chestStatChance01;
    // Etapa 4: level up comum — chance de cada carta ser Upgrade, e no máximo N itens por oferta.
    public float levelUpUpgradeChance01;
    public int maxItemsPerOffer;
    // Etapa 4: atributos (Stats) — no máximo N, e não sobem de nível.
    public int maxStatSlots;
    // Pedido do usuário (2026-11-25): cartas sempre presentes no 1º level up, e peso dos itens já possuídos.
    public IReadOnlyList<CardDefinition> firstLevelUpCards;
    public float ownedItemWeight;
    // Pedido do usuário (2026-11-25): upgrades prioritários (primário, dano e vida gerais) pesam mais,
    // mais ainda no começo — cai em linha reta de Early (até FadeStart) a Late (a partir de FadeEnd).
    public float priorityUpgradeWeightEarly;
    public float priorityUpgradeWeightLate;
    public int priorityFadeStartLevel;
    public int priorityFadeEndLevel;

    public static OfferRules From(RunSettings s) => new OfferRules
    {
        maxItemSlots = s != null ? s.maxItemSlots : 6,
        statEveryLevels = s != null ? s.statEveryLevels : 5,
        chestStatChance01 = s != null ? s.chestStatChancePercent / 100f : 0.15f,
        levelUpUpgradeChance01 = s != null ? s.levelUpUpgradeChancePercent / 100f : 0.6f,
        maxItemsPerOffer = s != null ? s.maxItemsPerOffer : 2,
        maxStatSlots = s != null ? s.maxStatSlots : 6,
        firstLevelUpCards = s != null ? s.firstLevelUpCards : null,
        ownedItemWeight = s != null ? s.ownedItemWeight : 3f,
        priorityUpgradeWeightEarly = s != null ? s.priorityUpgradeWeightEarly : 2.5f,
        priorityUpgradeWeightLate = s != null ? s.priorityUpgradeWeightLate : 1.5f,
        priorityFadeStartLevel = s != null ? s.priorityFadeStartLevel : 5,
        priorityFadeEndLevel = s != null ? s.priorityFadeEndLevel : 20,
    };
}

// Sorteio das cartas de uma oferta: até `count` cartas DISTINTAS, sem as que já estão no nível
// máximo e sem as de outro herói. Devolve menos se não houver cartas suficientes.
// Sem alocação além da lista `result` recebida (os buffers de índices são reaproveitados).
public static class CardRules
{
    private const int AllKinds = ~0;
    public const int FirstLevelUpLevel = 2; // o 1º level up leva o herói ao nível 2
    private static readonly List<int> eligible = new();
    private static readonly List<int> statEligible = new();
    private static readonly List<int> itemEligible = new();

    private static int KindBit(CardKind k) => 1 << (int)k;

    // Etapas 1+2: sorteio de qualquer tipo (mantido para quem não tem fonte).
    public static void Roll(IReadOnlyList<CardDefinition> pool, IReadOnlyDictionary<string, int> owned, string heroId,
        SpecTag heroTags, int count, System.Random rng, List<CardDefinition> result)
    {
        result.Clear();
        Collect(pool, owned, heroId, heroTags, AllKinds, Caps.None, eligible);
        TakeRandom(pool, eligible, count, rng, result);
    }

    // Etapa 3 (spec 5.1) + etapa 4:
    // - LevelUp em nível múltiplo de statEveryLevels: só Stats (sem Stat possível -> oferta comum);
    // - LevelUp comum: carta a carta, Upgrade com levelUpUpgradeChance01, senão Item; no máximo
    //   maxItemsPerOffer itens (o excedente vira Upgrade). Sem Upgrade -> Item -> Stat;
    // - Chest: Upgrades; cada carta, com chestStatChance01, vira um Stat distinto;
    // - Vendor: só Itens.
    // Em toda fonte: carta com requiredTags que a especialização não tem fica de fora; com
    // maxItemSlots itens já possuídos, item novo não entra; com maxStatSlots Stats, Stat nenhum entra.
    public static void RollOffer(OfferSource source, int heroLevel, IReadOnlyList<CardDefinition> pool,
        IReadOnlyDictionary<string, int> owned, string heroId, SpecTag heroTags, int count, System.Random rng, OfferRules rules,
        List<CardDefinition> result)
    {
        result.Clear();
        var caps = Caps.None;
        if (CountOwned(pool, owned, CardKind.Item) >= rules.maxItemSlots) caps |= Caps.ItemsFull;
        if (rules.maxStatSlots > 0 && CountOwned(pool, owned, CardKind.Stat) >= rules.maxStatSlots) caps |= Caps.StatsFull;
        switch (source)
        {
            case OfferSource.LevelUp:
                if (rules.statEveryLevels > 0 && heroLevel > 0 && heroLevel % rules.statEveryLevels == 0)
                {
                    Collect(pool, owned, heroId, heroTags, KindBit(CardKind.Stat), caps, eligible);
                    TakeRandom(pool, eligible, count, rng, result);
                    if (result.Count > 0) return;
                }
                RollLevelUpMix(pool, owned, heroId, heroTags, caps, heroLevel, count, rng, rules, result);
                return;

            case OfferSource.Vendor:
                Collect(pool, owned, heroId, heroTags, KindBit(CardKind.Item), caps, eligible);
                while (result.Count < count && eligible.Count > 0) result.Add(TakeItem(pool, eligible, owned, rules.ownedItemWeight, rng));
                return;

            case OfferSource.Chest:
                Collect(pool, owned, heroId, heroTags, KindBit(CardKind.Upgrade), caps, eligible);
                TakeRandom(pool, eligible, count, rng, result);
                Collect(pool, owned, heroId, heroTags, KindBit(CardKind.Stat), caps, statEligible);
                for (int i = 0; i < result.Count && statEligible.Count > 0; i++)
                {
                    if (rng.NextDouble() >= rules.chestStatChance01) continue;
                    result[i] = TakeOne(pool, statEligible, rng);
                }
                // Upgrades acabaram (todos no máximo): completa com Stats — o baú nunca sai vazio à toa.
                while (result.Count < count && statEligible.Count > 0) result.Add(TakeOne(pool, statEligible, rng));
                return;
        }
    }

    // Level up comum (etapa 4): cada carta é Upgrade com a chance, senão Item (até o limite por oferta).
    // Os 3 buffers encolhem a cada pick, então as cartas nunca se repetem.
    private static void RollLevelUpMix(IReadOnlyList<CardDefinition> pool, IReadOnlyDictionary<string, int> owned,
        string heroId, SpecTag heroTags, Caps caps, int heroLevel, int count, System.Random rng, OfferRules rules, List<CardDefinition> result)
    {
        Collect(pool, owned, heroId, heroTags, KindBit(CardKind.Upgrade), caps, eligible);
        Collect(pool, owned, heroId, heroTags, KindBit(CardKind.Item), caps, itemEligible);
        Collect(pool, owned, heroId, heroTags, KindBit(CardKind.Stat), caps, statEligible);
        // 1º level up: as cartas garantidas entram primeiro (se forem elegíveis) e saem do sorteio.
        if (heroLevel == FirstLevelUpLevel && rules.firstLevelUpCards != null)
            for (int g = 0; g < rules.firstLevelUpCards.Count && result.Count < count; g++)
            {
                var card = rules.firstLevelUpCards[g];
                if (RemoveCard(pool, eligible, card) || RemoveCard(pool, itemEligible, card) || RemoveCard(pool, statEligible, card))
                    result.Add(card);
            }
        int items = 0;
        while (result.Count < count)
        {
            bool itemRoom = items < rules.maxItemsPerOffer && itemEligible.Count > 0;
            bool wantUpgrade = rng.NextDouble() < rules.levelUpUpgradeChance01;
            if ((wantUpgrade || !itemRoom) && eligible.Count > 0) result.Add(TakeUpgrade(pool, eligible, PriorityUpgradeWeight(heroLevel, rules), rng));
            else if (itemRoom) { result.Add(TakeItem(pool, itemEligible, owned, rules.ownedItemWeight, rng)); items++; }
            else if (statEligible.Count > 0) result.Add(TakeOne(pool, statEligible, rng));
            else return;
        }
    }

    private static bool RemoveCard(IReadOnlyList<CardDefinition> pool, List<int> from, CardDefinition card)
    {
        if (card == null) return false;
        for (int i = 0; i < from.Count; i++)
            if (pool[from[i]] == card) { from[i] = from[from.Count - 1]; from.RemoveAt(from.Count - 1); return true; }
        return false;
    }

    // Upgrade prioritário: do ataque primário, ou o dano/vida gerais (habilidade None).
    public static bool IsPriorityUpgrade(CardDefinition c)
        => c != null && c.kind == CardKind.Upgrade
           && (c.ability == CardAbility.Primary
               || (c.ability == CardAbility.None && (c.stat == CardStat.Damage || c.stat == CardStat.MaxHealth)));

    // Peso dos prioritários no nível do herói: Early até FadeStart, Late a partir de FadeEnd, reta no meio.
    public static float PriorityUpgradeWeight(int heroLevel, OfferRules rules)
    {
        if (rules.priorityUpgradeWeightEarly <= 0f && rules.priorityUpgradeWeightLate <= 0f) return 1f;
        if (heroLevel <= rules.priorityFadeStartLevel || rules.priorityFadeEndLevel <= rules.priorityFadeStartLevel)
            return heroLevel <= rules.priorityFadeStartLevel ? rules.priorityUpgradeWeightEarly : rules.priorityUpgradeWeightLate;
        if (heroLevel >= rules.priorityFadeEndLevel) return rules.priorityUpgradeWeightLate;
        float t = (heroLevel - rules.priorityFadeStartLevel) / (float)(rules.priorityFadeEndLevel - rules.priorityFadeStartLevel);
        return rules.priorityUpgradeWeightEarly + (rules.priorityUpgradeWeightLate - rules.priorityUpgradeWeightEarly) * t;
    }

    private static CardDefinition TakeUpgrade(IReadOnlyList<CardDefinition> pool, List<int> from, float priorityWeight, System.Random rng)
    {
        if (priorityWeight <= 1f) return TakeOne(pool, from, rng);
        double total = 0;
        for (int i = 0; i < from.Count; i++) total += IsPriorityUpgrade(pool[from[i]]) ? priorityWeight : 1f;
        double roll = rng.NextDouble() * total;
        int pick = from.Count - 1;
        for (int i = 0; i < from.Count; i++)
        {
            roll -= IsPriorityUpgrade(pool[from[i]]) ? priorityWeight : 1f;
            if (roll < 0) { pick = i; break; }
        }
        var card = pool[from[pick]];
        from[pick] = from[from.Count - 1];
        from.RemoveAt(from.Count - 1);
        return card;
    }

    // Item com peso: os que o herói já tem pesam `ownedWeight` (os novos pesam 1). Peso <= 1 = sorteio igual.
    private static CardDefinition TakeItem(IReadOnlyList<CardDefinition> pool, List<int> from,
        IReadOnlyDictionary<string, int> owned, float ownedWeight, System.Random rng)
    {
        if (ownedWeight <= 1f) return TakeOne(pool, from, rng);
        double total = 0;
        for (int i = 0; i < from.Count; i++) total += LevelOf(owned, pool[from[i]]) > 0 ? ownedWeight : 1f;
        double roll = rng.NextDouble() * total;
        int pick = from.Count - 1;
        for (int i = 0; i < from.Count; i++)
        {
            roll -= LevelOf(owned, pool[from[i]]) > 0 ? ownedWeight : 1f;
            if (roll < 0) { pick = i; break; }
        }
        var card = pool[from[pick]];
        from[pick] = from[from.Count - 1];
        from.RemoveAt(from.Count - 1);
        return card;
    }

    private static CardDefinition TakeOne(IReadOnlyList<CardDefinition> pool, List<int> from, System.Random rng)
    {
        int pick = rng.Next(from.Count);
        var card = pool[from[pick]];
        from[pick] = from[from.Count - 1];
        from.RemoveAt(from.Count - 1);
        return card;
    }

    // Quantos itens DIFERENTES o herói tem (cada um ocupa 1 dos 6 espaços).
    public static int CountOwnedItems(IReadOnlyList<CardDefinition> pool, IReadOnlyDictionary<string, int> owned)
        => CountOwned(pool, owned, CardKind.Item);

    // Quantas cartas DIFERENTES daquele tipo o herói tem.
    public static int CountOwned(IReadOnlyList<CardDefinition> pool, IReadOnlyDictionary<string, int> owned, CardKind kind)
    {
        int n = 0;
        if (pool == null) return 0;
        for (int i = 0; i < pool.Count; i++)
        {
            var c = pool[i];
            if (c != null && c.kind == kind && LevelOf(owned, c) > 0) n++;
        }
        return n;
    }

    [System.Flags]
    private enum Caps { None = 0, ItemsFull = 1, StatsFull = 2 }

    public static int LevelOf(IReadOnlyDictionary<string, int> owned, CardDefinition card)
        => card != null && owned != null && card.id != null && owned.TryGetValue(card.id, out int l) ? l : 0;

    private static void Collect(IReadOnlyList<CardDefinition> pool, IReadOnlyDictionary<string, int> owned, string heroId,
        SpecTag heroTags, int kindMask, Caps caps, List<int> into)
    {
        into.Clear();
        if (pool == null) return;
        for (int i = 0; i < pool.Count; i++)
        {
            var card = pool[i];
            if (card == null) continue;
            if ((kindMask & KindBit(card.kind)) == 0) continue;
            if (!string.IsNullOrEmpty(card.heroId) && card.heroId != heroId) continue;
            if (card.requiredTags != SpecTag.None && (card.requiredTags & heroTags) == 0) continue; // etapa 4
            int level = LevelOf(owned, card);
            if (level >= card.maxLevel) continue;
            if ((caps & Caps.ItemsFull) != 0 && card.kind == CardKind.Item && level == 0) continue; // 7º item não cabe
            if ((caps & Caps.StatsFull) != 0 && card.kind == CardKind.Stat && level == 0) continue; // 7º Stat não cabe
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
