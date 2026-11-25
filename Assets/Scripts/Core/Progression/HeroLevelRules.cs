using UnityEngine;

// Etapas 1+2 (docs/superpowers/specs/2026-11-24-etapas-1-2-run-25min-cartas-design.md, 6.2) — nível
// do herói numa run de 25 min. Subir de nível NÃO dá status: só abre a tela de 3 cartas.
// - Nível máximo 60. 1º nível em 5 XP (pedido do usuário, 2026-11-25 — 15 era alto demais); do 2º
//   em diante a curva continua a mesma (15 × L^0,6).
// - Até o 40 a curva sobe devagar (L^0,6): o 40 tem que ser bem possível até o minuto 25.
// - Depois do 40 cada nível fica 12% mais caro que o anterior: o 60 é só pra quem quebra o jogo.
public static class HeroLevelRules
{
    public const int MaxLevel = 60;
    public const float BaseXp = 15f; // 🔢
    public const long FirstLevelXp = 5; // 🔢
    public const float Exponent = 0.6f; // 🔢
    public const int LateLevel = 40; // 🔢
    public const float LateGrowth = 1.12f; // 🔢

    public static long XpToNext(int level)
    {
        if (level < 1 || level >= MaxLevel) return long.MaxValue;
        if (level == 1) return FirstLevelXp;
        double xp = BaseXp * System.Math.Pow(level, Exponent);
        if (level >= LateLevel) xp *= System.Math.Pow(LateGrowth, level - LateLevel);
        return (long)System.Math.Round(xp);
    }

    public struct XpResult
    {
        public int level;
        public long xpIntoLevel;
        public int levelsGained;
    }

    // Aplica um ganho de XP: sobe quantos níveis couberem. No nível máximo a barra fica cheia e o
    // resto se perde.
    public static XpResult AddXp(int level, long xpIntoLevel, long xpGained)
    {
        var r = new XpResult { level = Mathf.Max(1, level), xpIntoLevel = System.Math.Max(0, xpIntoLevel) };
        long remaining = System.Math.Max(0, xpGained);
        while (remaining > 0 && r.level < MaxLevel)
        {
            long need = XpToNext(r.level) - r.xpIntoLevel;
            if (remaining < need) { r.xpIntoLevel += remaining; break; }
            remaining -= need;
            r.level++;
            r.xpIntoLevel = 0;
            r.levelsGained++;
        }
        return r;
    }
}
