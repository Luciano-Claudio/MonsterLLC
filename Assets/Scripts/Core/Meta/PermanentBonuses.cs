using System.Collections.Generic;
using UnityEngine;

// Loja permanente — bônus que valem na run do herói escolhido, montados no início da run
// (GameplayBootstrap.SpawnHero, antes do herói nascer) com os níveis ATIVOS dele. Lido pelos pontos
// que já existem (CardBonuses.Get, HeroController, HeroLevel, XpGemPool, mundo). Fora da run, neutro.
public static class PermanentBonuses
{
    // Maldição: o que ela dá aos INIMIGOS por nível (o resto usa o valor da melhoria, 0,20).
    public const float CurseEnemyPerLevel = 0.10f; // 🔢

    public static float DamageFactor { get; private set; } = 1f;
    public static float MaxHealthFactor { get; private set; } = 1f;
    public static float ArmorAdd { get; private set; }
    public static float RegenPercentPerSecond { get; private set; } // fração da vida máxima por segundo
    public static float MoveSpeedFactor { get; private set; } = 1f;
    public static float CooldownFactor { get; private set; } = 1f;
    public static float UltimateChargeFactor { get; private set; } = 1f;
    public static float UltimateCostFactor { get; private set; } = 1f;
    public static float MagnetFactor { get; private set; } = 1f;
    public static float XpFactor { get; private set; } = 1f;
    public static float GoldFactor { get; private set; } = 1f;
    public static float SizeFactor { get; private set; } = 1f;
    public static float FlameAreaFactor { get; private set; } = 1f;
    public static float HordeFactor { get; private set; } = 1f;
    public static float EnemyFactor { get; private set; } = 1f;
    public static int ExtraArrows { get; private set; }
    public static int StartRerolls { get; private set; } = 1;
    public static int LuckLevel { get; private set; }
    public static int CurseLevel { get; private set; }
    public static bool HasRevival { get; private set; }

    private static float goldFraction;

    public static void Reset() => BuildFrom(null, null);

    // Monta com os níveis ativos do herói (catálogo + profile.json).
    public static void Build(string heroKey)
    {
        var catalog = PermanentUpgradeCatalog.Load();
        var levels = new Dictionary<PermanentStat, int>();
        var values = new Dictionary<PermanentStat, float>();
        if (catalog != null)
            foreach (var d in catalog.For(heroKey))
            {
                if (d == null) continue;
                levels[d.stat] = PermanentShop.EffectiveLevel(heroKey, d.id);
                values[d.stat] = d.valuePerLevel;
            }
        BuildFrom(levels, values);
    }

    public static void BuildFrom(IReadOnlyDictionary<PermanentStat, int> levels, IReadOnlyDictionary<PermanentStat, float> values)
    {
        int L(PermanentStat s) => levels != null && levels.TryGetValue(s, out int n) ? Mathf.Max(0, n) : 0;
        float V(PermanentStat s) => values != null && values.TryGetValue(s, out float v) ? v : 0f;
        float Up(PermanentStat s) => 1f + V(s) * L(s);
        float Down(PermanentStat s) => Mathf.Max(0f, 1f - V(s) * L(s));

        CurseLevel = L(PermanentStat.Curse);
        // Maldição SOMA com as melhorias do herói (usuário: velocidade no máximo = +15% + 100% = +115%).
        float curseAdd = V(PermanentStat.Curse) * CurseLevel;
        float curse = 1f + curseAdd;

        DamageFactor = Up(PermanentStat.Damage);
        MaxHealthFactor = Up(PermanentStat.MaxHealth) + curseAdd;
        ArmorAdd = V(PermanentStat.Armor) * L(PermanentStat.Armor);
        RegenPercentPerSecond = V(PermanentStat.Regen) * L(PermanentStat.Regen);
        MoveSpeedFactor = Up(PermanentStat.MoveSpeed) + curseAdd;
        CooldownFactor = Down(PermanentStat.Cooldown);
        UltimateChargeFactor = Up(PermanentStat.UltimateCharge);
        UltimateCostFactor = Down(PermanentStat.UltimateCost);
        MagnetFactor = Up(PermanentStat.Magnet);
        XpFactor = Up(PermanentStat.Xp) + curseAdd;
        GoldFactor = Up(PermanentStat.Gold) + curseAdd;
        SizeFactor = Up(PermanentStat.Size);
        FlameAreaFactor = Up(PermanentStat.FlameArea);
        HordeFactor = curse;
        EnemyFactor = 1f + CurseEnemyPerLevel * CurseLevel;
        ExtraArrows = Mathf.RoundToInt(V(PermanentStat.Multishot) * L(PermanentStat.Multishot));
        StartRerolls = 1 + Mathf.RoundToInt(V(PermanentStat.Reroll) * L(PermanentStat.Reroll));
        LuckLevel = L(PermanentStat.Luck);
        HasRevival = L(PermanentStat.Revival) > 0;
        goldFraction = 0f;
    }

    // Gold coletado na run com Greed/Maldição — guarda a fração entre coletas (+10% de 1 gold = 1 a
    // cada 10 moedas).
    public static long ScaleGold(int raw)
    {
        if (raw <= 0) return 0;
        float scaled = raw * GoldFactor + goldFraction;
        long whole = (long)Mathf.Floor(scaled);
        goldFraction = scaled - whole;
        return whole;
    }
}
