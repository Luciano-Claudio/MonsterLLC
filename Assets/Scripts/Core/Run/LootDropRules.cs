using UnityEngine;

// Tipos de loot do chão (XpGemPool).
public enum LootKind { Xp, Gold, GoldBag, StatItem, RerollDice }

// Loja permanente (docs/superpowers/specs/2026-11-26-loja-permanente-design.md) — chances dos loots novos.
// Stat Item (só boss): 10% + 10% por nível de Luck (até 50%). Reroll_Dice: 1% em monstro comum e 5% em
// mini boss/boss, e cada nível de Luck soma 25% da chance base (Luck 4 = o dobro).
public static class LootDropRules
{
    public static float StatItemChance(int luck, float baseChance = 0.10f, float perLuck = 0.10f) =>
        Mathf.Clamp01(baseChance + perLuck * Mathf.Max(0, luck));

    // Luck: +10% por nível de chance de 1 baú extra e (rolado à parte) de 1 vendedor extra na run.
    public static float ExtraRewardChance(int luck, float perLuck = 0.10f) => Mathf.Clamp01(perLuck * Mathf.Max(0, luck));
    public static bool RollsExtraReward(int luck, double roll) => (float)roll < ExtraRewardChance(luck); // em float: 0,40f > 0,40 em double

    public static float DiceChance(bool bossOrMiniBoss, int luck, float commonChance = 0.01f, float bossChance = 0.05f, float perLuck = 0.25f) =>
        Mathf.Clamp01((bossOrMiniBoss ? bossChance : commonChance) * (1f + perLuck * Mathf.Max(0, luck)));
}
