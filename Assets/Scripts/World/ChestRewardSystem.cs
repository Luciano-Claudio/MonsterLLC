using UnityEngine;

// Fonte única da recompensa de baú (GDD Deadline 8.1, Seção 8) — chamado tanto por
// Chest.Interact() quanto pela morte do Chest Mimic (ChestMimicController.AnimationDieEndEvent),
// pra garantir que os 2 caminhos deem exatamente a mesma recompensa sem duplicar o roll em 2
// lugares. Decisão do usuário (2026-11-08): 50% gold, 50% abre o Chest_Popup (perks/talentos).
public static class ChestRewardSystem
{
    // `collectedObject` (decisão do usuário, 2026-11-08: "o baú só existe até o player
    // coletá-lo") — o GameObject a destruir quando a recompensa for de fato coletada. No
    // caminho gold é destruído já (coleta é instantânea); no caminho popup, ChestPopupUI
    // guarda a referência e destrói só quando o painel fechar (hoje: 1 clique qualquer —
    // placeholder até a escolha real existir). Passa null quando não há objeto de mundo pra
    // destruir (ex.: morte do Chest Mimic, que já se autodestrói via AnimationDieEndEvent,
    // dono do próprio ciclo de vida independente da recompensa).
    public static void GrantReward(GameObject collectedObject = null)
    {
        var settings = WorldChestSpawnManager.Instance != null ? WorldChestSpawnManager.Instance.chestSettings : null;
        if (settings == null)
        {
            Debug.LogWarning("[ChestRewardSystem] ChestSpawnSettings não encontrado (WorldChestSpawnManager.Instance null?) — recompensa ignorada.");
            return;
        }

        if (Random.value * 100f < settings.choicePopupChancePercent)
        {
            ChestPopupUI.Instance?.Show(collectedObject);
            return;
        }

        // Baú Duplo (World Perk, Seção 8, item 13) — bônus ADITIVO em cima da % base do asset.
        long reward = RollGold(settings);
        float doubleChancePercent = settings.doubleRewardChancePercent + WorldPerkEffects.DoubleRewardBonusPercent;
        if (Random.value * 100f < doubleChancePercent) reward += RollGold(settings);

        MainMenuUI.CurrentRun.gold += reward;
        GameEvents.GoldChanged(MainMenuUI.CurrentRun.gold);
        Debug.Log($"[ChestRewardSystem] +{reward} gold.");

        // "Status_Gold" do herói — só neste caminho (gold de verdade), nunca no caminho popup
        // acima (decisão explícita do usuário: "o baú só deve ativar esse status quando vier
        // só gold").
        HeroController.Instance?.ShowGoldStatusFor(settings.goldStatusDuration);

        if (collectedObject != null) Object.Destroy(collectedObject);
    }

    // Mesmo padrão seguro de long aleatório já usado em EnemyController.RollQuantity.
    private static long RollGold(ChestSpawnSettings settings)
    {
        if (settings.maxGoldReward <= settings.minGoldReward) return settings.minGoldReward;

        double range = (double)(settings.maxGoldReward - settings.minGoldReward) + 1.0;
        long offset = (long)(Random.value * range);
        if (offset >= range) offset = (long)range - 1;

        return settings.minGoldReward + offset;
    }
}
