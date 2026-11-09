// Estado/compra de Perks (GDD Deadline 8.1, Secao 6/7.3/8-38, R9) - le/escreve direto em
// RunState.perkRanks (reseta toda run, confirmado com o usuario). Fora de Assets/Scripts/Core/
// de proposito - precisa de MainMenuUI.CurrentRun, e Core nao referencia o assembly padrao onde
// MainMenuUI mora (mesmo motivo de DayStatsTracker/KillGoldReward).
public static class PerkState
{
    public static int GetRank(string perkId)
    {
        var run = MainMenuUI.CurrentRun;
        if (run == null || string.IsNullOrEmpty(perkId)) return 0;

        foreach (var entry in run.perkRanks)
        {
            if (entry.perkId == perkId) return entry.rank;
        }
        return 0;
    }

    public static bool CanAfford(PerkDefinitionSO perk)
    {
        if (perk == null || perk.costPerRank == null) return false;
        var run = MainMenuUI.CurrentRun;
        if (run == null) return false;

        int rank = GetRank(perk.perkId);
        if (rank >= perk.costPerRank.Length) return false; // já no rank máximo

        return run.gold >= perk.costPerRank[rank];
    }

    public static bool IsMaxed(PerkDefinitionSO perk)
    {
        if (perk == null || perk.costPerRank == null) return true;
        return GetRank(perk.perkId) >= perk.costPerRank.Length;
    }

    public static bool TryBuy(PerkDefinitionSO perk)
    {
        if (perk == null || !CanAfford(perk)) return false;

        var run = MainMenuUI.CurrentRun;
        int rank = GetRank(perk.perkId);
        long cost = perk.costPerRank[rank];

        run.gold -= cost;
        SetRank(perk.perkId, rank + 1);
        GameEvents.GoldChanged(run.gold);
        WorldPerkEffects.Recompute();
        // Perks bespoke (Esquerda/Direita/Cima) do herói ATUAL — cada herói sobrescreve
        // RecomputePerkEffects() com os próprios efeitos; default no-op pra quem ainda não tem
        // conteúdo real (R11+). "!= null", nunca "?." — Unity sobrecarrega "==" pra detectar
        // objeto destruído, mas "?." ignora essa sobrecarga (mesma regra de
        // HeroController.AnimatorTrigger). Achado real: comprar perk na Loja depois de morrer,
        // antes de "Start Day", crashava aqui — o herói antigo já foi Destroy()ado e o novo só
        // nasce em GameplayBootstrap.RespawnHeroFresh(), então Instance ainda aponta pro
        // cadáver. Sem efeito nenhum nesse caso (a run não tem herói vivo pra recomputar) — o
        // Awake() do herói novo já recomputa tudo sozinho quando ele nascer.
        if (HeroController.Instance != null) HeroController.Instance.RecomputePerkEffects();
        return true;
    }

    private static void SetRank(string perkId, int rank)
    {
        var run = MainMenuUI.CurrentRun;
        foreach (var entry in run.perkRanks)
        {
            if (entry.perkId == perkId)
            {
                entry.rank = rank;
                return;
            }
        }
        run.perkRanks.Add(new PerkRankEntry { perkId = perkId, rank = rank });
    }
}
