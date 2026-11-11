using System.Collections.Generic;

// Estado/compra de Perks (GDD Deadline 8.1, Secao 6/7.3/8-38, R9) - le/escreve direto em
// RunState.perkRanks (reseta toda run, confirmado com o usuario). Fora de Assets/Scripts/Core/
// de proposito - precisa de MainMenuUI.CurrentRun, e Core nao referencia o assembly padrao onde
// MainMenuUI mora (mesmo motivo de DayStatsTracker/KillGoldReward).
public static class PerkState
{
    // Botão "Redefine" (Perk_Painel) — Ctrl+Z de 1 compra por clique, só dentro da visita ATUAL
    // à Loja (pedido do usuário: nunca alcança uma visita anterior). purchaseHistory zera toda
    // vez que a Loja abre — ver ShopUIController.HandleStateChanged, único ponto que já sabe
    // exatamente quando isso acontece.
    private static readonly List<PerkDefinitionSO> purchaseHistory = new();
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

        // Pré-requisito entre perks (ex.: Ranger "Imortal em Combate" exige "Veterano de
        // Guerra" já comprado) — trava a compra em si, não só o efeito; o jogador nem consegue
        // pagar por este perk antes do pré-requisito ter rank >= 1.
        if (!string.IsNullOrEmpty(perk.prerequisitePerkId) && GetRank(perk.prerequisitePerkId) < 1)
            return false;

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
        purchaseHistory.Add(perk);
        GameEvents.GoldChanged(run.gold);
        RecomputeAllEffects();
        return true;
    }

    // Botão "Redefine" — desfaz a ÚLTIMA compra desta visita à Loja (estorna o gold pago
    // naquele rank, volta o rank pra como estava) e a tira da pilha, então clicar de novo
    // desfaz a compra anterior a ela, e assim por diante — nunca passa do início desta visita
    // (purchaseHistory só tem o que foi comprado DEPOIS que a Loja abriu agora).
    public static bool UndoLastPurchase()
    {
        if (purchaseHistory.Count == 0) return false;

        var perk = purchaseHistory[^1];
        purchaseHistory.RemoveAt(purchaseHistory.Count - 1);

        int rank = GetRank(perk.perkId);
        if (rank <= 0) return false; // segurança — não deveria acontecer

        var run = MainMenuUI.CurrentRun;
        long refund = perk.costPerRank[rank - 1]; // o custo PAGO pra chegar nesse rank
        run.gold += refund;
        SetRank(perk.perkId, rank - 1);

        GameEvents.GoldChanged(run.gold);
        RecomputeAllEffects();
        return true;
    }

    // Chamado sempre que a Loja abre (ShopUIController.HandleStateChanged) — garante que
    // "Redefine" nunca alcança uma compra de uma visita anterior à Loja.
    public static void ClearPurchaseHistory() => purchaseHistory.Clear();

    // Compartilhado por TryBuy/UndoLastPurchase — qualquer mudança de rank precisa recomputar
    // os 2 agregadores (Mundo + bespoke do herói atual). "!= null", nunca "?." — Unity
    // sobrecarrega "==" pra detectar objeto destruído, mas "?." ignora essa sobrecarga (mesma
    // regra de HeroController.AnimatorTrigger). Achado real: comprar perk na Loja depois de
    // morrer, antes de "Start Day", crashava aqui — o herói antigo já foi Destroy()ado e o novo
    // só nasce em GameplayBootstrap.RespawnHeroFresh(), então Instance ainda aponta pro
    // cadáver. Sem efeito nenhum nesse caso (a run não tem herói vivo pra recomputar) — o
    // Awake() do herói novo já recomputa tudo sozinho quando ele nascer.
    private static void RecomputeAllEffects()
    {
        WorldPerkEffects.Recompute();
        if (HeroController.Instance != null) HeroController.Instance.RecomputePerkEffects();
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
