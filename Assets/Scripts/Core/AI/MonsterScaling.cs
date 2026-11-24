using UnityEngine;

// D9.6a + etapas 1+2 (1,5x por tier a partir da média REAL do T1: 6,9 de vida / 2,3 de dano) — escala dos monstros por tier (docs/superpowers/specs/2026-11-22-d9-6ab-monstros-xp-nivel-design.md, 3.1).
// Ciclo de risco e recompensa: cada tier é bem mais forte, mas paga desproporcionalmente mais XP
// (XP cresce mais rápido que a vida). Valores = melee típico do tier; ranged tem menos vida e mais
// dano; mini boss é um melee bem mais forte. Cada monstro mantém a proporção dele no tier.
public enum MonsterKind { Melee, Ranged, MiniBoss }

public static class MonsterScaling
{
    // Índice = tier (0 sem uso). Padrões = tabela aprovada; o MonsterScalingSettings (Resources)
    // substitui em runtime/no Editor via SetTables (os testes usam os padrões).
    public static float[] TierHealth { get; private set; } = { 0, 6.9f, 10.35f, 15.53f, 23.29f, 34.93f, 52.4f, 78.6f, 117.89f, 176.84f, 265.26f };
    public static float[] TierDamage { get; private set; } = { 0, 2.3f, 3.45f, 5.17f, 7.76f, 11.64f, 17.47f, 26.2f, 39.3f, 58.95f, 88.42f };
    public static int[] TierXp { get; private set; } = { 0, 1, 4, 15, 50, 160, 500, 1500, 4500, 13000, 38000 };

    private static readonly float[] DefaultHealth = { 0, 6.9f, 10.35f, 15.53f, 23.29f, 34.93f, 52.4f, 78.6f, 117.89f, 176.84f, 265.26f };
    private static readonly float[] DefaultDamage = { 0, 2.3f, 3.45f, 5.17f, 7.76f, 11.64f, 17.47f, 26.2f, 39.3f, 58.95f, 88.42f };
    private static readonly int[] DefaultXp = { 0, 1, 4, 15, 50, 160, 500, 1500, 4500, 13000, 38000 };

    // Só aceita tabelas com exatamente os 10 tiers (+ índice 0); fora disso avisa e mantém a atual.
    public static void SetTables(float[] health, float[] damage, int[] xp)
    {
        if (health != null && health.Length == 11) TierHealth = health; else if (health != null) Debug.LogWarning($"[MonsterScaling] tabela de vida com {health.Length - 1} tiers (esperado 10) — ignorada");
        if (damage != null && damage.Length == 11) TierDamage = damage; else if (damage != null) Debug.LogWarning($"[MonsterScaling] tabela de dano com {damage.Length - 1} tiers (esperado 10) — ignorada");
        if (xp != null && xp.Length == 11) TierXp = xp; else if (xp != null) Debug.LogWarning($"[MonsterScaling] tabela de XP com {xp.Length - 1} tiers (esperado 10) — ignorada");
    }

    // Volta aos padrões do código (os testes usam isto: sem domain reload, o asset do boot ficaria).
    public static void ResetTables()
    {
        TierHealth = (float[])DefaultHealth.Clone();
        TierDamage = (float[])DefaultDamage.Clone();
        TierXp = (int[])DefaultXp.Clone();
    }

    // Etapas 1+2 (spec 5.4) — boss = múltiplo do melee típico do tier dele; o Divine God tem mais vida.
    public const float BossHealthFactor = 20f;
    public const float BossDamageFactor = 2f;
    public const float FinalBossHealthFactor = 30f;

    public static float TargetBossHealth(int tier, bool finalBoss, float ratioToTierBossAverage)
        => TierHealth[ClampTier(tier)] * (finalBoss ? FinalBossHealthFactor : BossHealthFactor) * ratioToTierBossAverage;

    public static float TargetBossDamage(int tier, float ratioToTierBossAverage)
        => TierDamage[ClampTier(tier)] * BossDamageFactor * ratioToTierBossAverage;

    public const float RangedHealthFactor = 0.7f;
    public const float RangedDamageFactor = 1.4f;
    public const float MiniBossHealthFactor = 4f;
    public const float MiniBossDamageFactor = 1.5f;
    public const int MiniBossXpFactor = 3;
    public const int BossXpFactor = 10;

    private static int ClampTier(int tier) => Mathf.Clamp(tier, 1, TierHealth.Length - 1);

    public static float TargetHealth(int tier, MonsterKind kind, float ratioToTierAverage)
    {
        float factor = kind == MonsterKind.Ranged ? RangedHealthFactor : kind == MonsterKind.MiniBoss ? MiniBossHealthFactor : 1f;
        return TierHealth[ClampTier(tier)] * factor * ratioToTierAverage;
    }

    public static float TargetDamage(int tier, MonsterKind kind, float ratioToTierAverage)
    {
        float factor = kind == MonsterKind.Ranged ? RangedDamageFactor : kind == MonsterKind.MiniBoss ? MiniBossDamageFactor : 1f;
        return TierDamage[ClampTier(tier)] * factor * ratioToTierAverage;
    }

    // Monstro sem tier conhecido (spawn de teste, summon sem definição) vale como T1.
    public static int XpForKill(int tier, bool isBoss, bool isMiniBoss)
    {
        int xp = tier >= 1 ? TierXp[ClampTier(tier)] : TierXp[1];
        if (isBoss) return xp * BossXpFactor;
        if (isMiniBoss) return xp * MiniBossXpFactor;
        return xp;
    }

    // Velocidade média do tier: sobe um pouco por tier (pedido do usuário, 2026-11-21), de 2,5 no
    // T1 a 4 no T10. Cada monstro mantém a proporção dele (os rápidos continuam rápidos).
    public static float TierSpeed(int tier) => 2.5f + (ClampTier(tier) - 1) * (1.5f / 9f);
}
