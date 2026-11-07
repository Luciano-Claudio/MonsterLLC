// Economia de Combate (GDD Deadline 8.1, Seção 3.1) — gold fixo por kill (monstro comum ou
// boss), crescendo por dia via DayGoldTableSO (injetada por SystemsBootstrap, mesmo critério de
// toda outra tabela de dia do projeto — vem do Inspector, nunca Resources.Load).
// GoldBonusMultiplier é o gancho pra bônus futuro de Perk/Talento (a infra de Perk/Talento ainda
// não existe — R9/R10): multiplica POR CIMA do valor da tabela, mesmo padrão de
// HeroController.EnergyGainMultiplier (perks acumulam, nunca substituem a base).
//
// Fora de Assets/Scripts/Core (asmdef próprio) de propósito — Core não referencia o assembly
// padrão onde MainMenuUI mora (mesmo motivo documentado antes nesta classe).
public static class KillGoldReward
{
    public static float GoldBonusMultiplier = 1f;

    private static DayGoldTableSO table;
    private static bool initialized;

    public static void Init(DayGoldTableSO goldTable)
    {
        if (initialized) return;
        initialized = true;
        table = goldTable;
        GameEvents.OnEnemyKilled += HandleEnemyKilled;
    }

    private static void HandleEnemyKilled(int energyValue, bool isBoss)
    {
        if (table == null) return;

        var dayDef = table.GetDay(MainMenuUI.CurrentRun?.day ?? 1);
        if (dayDef == null) return;

        long baseGold = isBoss ? dayDef.bossGold : dayDef.killGold;
        long finalGold = (long)(baseGold * GoldBonusMultiplier);

        MainMenuUI.CurrentRun.gold += finalGold;
        GameEvents.GoldChanged(MainMenuUI.CurrentRun.gold);
    }
}
