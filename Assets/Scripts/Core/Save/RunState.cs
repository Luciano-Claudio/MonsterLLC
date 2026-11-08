[System.Serializable]
public class RunState
{
    public string mode = "Standard";
    public string hero = "Barbarian";
    public string map = "Tower";
    public int day = 1;
    public long gold = 0;

    // Stub — nao e o sistema real de Weapon Tier (15 tiers, Deadline 9, Sprint 33).
    // So o suficiente pra provar que uma compra na Loja sobrevive ao Save/Load.
    public string weaponTier = "Basic";

    // Sprint P10 (migracao do mundo unico) — antes vinha fixo de WorldGenerationSettings.worldSeed
    // (mesmo mundo em TODA run). Gerado em RunCreation.CreateNewRun, nunca aqui (RunState so
    // guarda estado, nao decide valores). Save antigo sem este campo carrega 0 — mesmo valor
    // fixo que o asset sempre teve, entao nao quebra nada ja salvo.
    public int worldSeed;

    // Tier Suppression (doc de design, Secao 12/13) — quantos Tiers suprimidos, sempre
    // sequencial a partir do T1 (T1, depois T2, depois T3...), nunca escolha livre, entao um
    // unico int basta: suppressedTierCount=2 significa T1 e T2 fora da pool de spawn. 0 = nada
    // suprimido ainda (default de save novo/antigo). Stub de loja em ShopHandler.
    // BuyNextTierSuppression.
    public int suppressedTierCount;

    // Resumo do Dia (DayStatsTracker/Vendor.Interact) — itens cujo nome já foi revelado (1ª
    // venda) viram conhecidos PRA SEMPRE no resto da run, mesmo em dias seguintes sem vender de
    // novo aquele item específico (confirmado com o usuário). Lista de nome exato (LootPricingSettings.itemName),
    // não índice — mais fácil de ler/debugar, e sobrevive a qualquer reordenação futura da
    // tabela de preços sem quebrar o save.
    public System.Collections.Generic.List<string> knownLootItems = new();

    // Infra de Perks (GDD Deadline 8.1, Seção 6/7.3/8-38, R9) — rank investido por perk, reseta
    // toda run (confirmado com o usuário, mesmo critério do resto da economia). Lista de
    // (perkId, rank), não Dictionary direto — JsonUtility não serializa Dictionary nativo (mesmo
    // motivo de knownLootItems ser List em vez de HashSet).
    public System.Collections.Generic.List<PerkRankEntry> perkRanks = new();
}

[System.Serializable]
public class PerkRankEntry
{
    public string perkId;
    public int rank;
}
