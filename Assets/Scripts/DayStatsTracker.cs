using System.Collections.Generic;

// Dados do Resumo do Dia (Resume_Painel) — GDD Deadline 8.1. Reseta todo dia, retry incluso
// (confirmado com o usuário: é resumo DO DIA, não do run inteiro) via GameEvents.OnDayStart,
// que já dispara tanto no Dia 1 quanto em todo ResetForNewDay() seguinte. Alimentado por
// KillGoldReward (contagem e gold de kill) e Vendor (quantidade/subtotal vendido por item);
// lido por ResumePainelUI quando a Loja abre.
//
// Fora de Assets/Scripts/Core (asmdef próprio) de propósito — precisa de MainMenuUI.CurrentRun
// pra marcar item como conhecido pra sempre (RunState.knownLootItems), e Core não referencia o
// assembly padrão onde MainMenuUI mora (mesmo motivo de KillGoldReward.cs).
public static class DayStatsTracker
{
    public static int CreaturesSlain { get; private set; }
    public static int BossesDefeated { get; private set; }
    public static long GoldFromMonsters { get; private set; }
    public static long GoldFromBosses { get; private set; }

    // Só do que foi vendido NESTE dia — reseta todo dia, diferente de RunState.knownLootItems
    // (revelação de nome, essa sim pra sempre).
    private static readonly Dictionary<string, long> soldQuantityToday = new();
    private static readonly Dictionary<string, long> soldSubtotalToday = new();

    private static bool initialized;

    public static void Init()
    {
        if (initialized) return;
        initialized = true;
        GameEvents.OnEnemyKilled += HandleEnemyKilled;
        GameEvents.OnDayStart += ResetForNewDay;
    }

    private static void HandleEnemyKilled(int energyValue, bool isBoss)
    {
        if (isBoss) BossesDefeated++;
        else CreaturesSlain++;
    }

    // Chamado por KillGoldReward logo depois de calcular o gold de 1 kill — evita duplicar
    // aqui a mesma leitura de DayGoldTableSO.
    public static void RegisterKillGold(long amount, bool isBoss)
    {
        if (isBoss) GoldFromBosses += amount;
        else GoldFromMonsters += amount;
    }

    // Chamado por Vendor.Interact() por item vendido. Marca o item como conhecido pra sempre
    // na 1ª vez que for vendido (RunState.knownLootItems) — confirmado com o usuário: a
    // revelação de nome nunca volta a "???" depois disso, mesmo em dias seguintes.
    public static void RegisterItemSold(string itemName, long quantity, long subtotal)
    {
        soldQuantityToday.TryGetValue(itemName, out long existingQty);
        soldQuantityToday[itemName] = existingQty + quantity;

        soldSubtotalToday.TryGetValue(itemName, out long existingSubtotal);
        soldSubtotalToday[itemName] = existingSubtotal + subtotal;

        if (MainMenuUI.CurrentRun != null && !MainMenuUI.CurrentRun.knownLootItems.Contains(itemName))
            MainMenuUI.CurrentRun.knownLootItems.Add(itemName);
    }

    public static long GetSoldQuantity(string itemName)
    {
        soldQuantityToday.TryGetValue(itemName, out long qty);
        return qty;
    }

    public static long GetSoldSubtotal(string itemName)
    {
        soldSubtotalToday.TryGetValue(itemName, out long subtotal);
        return subtotal;
    }

    public static bool IsItemKnown(string itemName) =>
        MainMenuUI.CurrentRun != null && MainMenuUI.CurrentRun.knownLootItems.Contains(itemName);

    private static void ResetForNewDay()
    {
        CreaturesSlain = 0;
        BossesDefeated = 0;
        GoldFromMonsters = 0;
        GoldFromBosses = 0;
        soldQuantityToday.Clear();
        soldSubtotalToday.Clear();
    }
}
