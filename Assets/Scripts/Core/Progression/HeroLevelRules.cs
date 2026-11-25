using UnityEngine;

// Etapas 1+2 (docs/superpowers/specs/2026-11-24-etapas-1-2-run-25min-cartas-design.md, 6.2) — nível
// do herói numa run de 25 min. Subir de nível NÃO dá status: só abre a tela de 3 cartas.
// Tabela do usuário (2026-11-25): só múltiplos de 5, cada nível mais caro que o anterior.
// - 1 a 5 fixos (5, 10, 20, 30, 40) — começo fácil, coletar gemas é difícil.
// - Daí em diante, o maior entre a curva antiga (15 × L^0,6, +12% por nível depois do 40)
//   arredondada pra cima no múltiplo de 5, e o nível anterior + 5. Até o 44 vira +5 por nível.
// - Nível máximo 60. Total até o 40: 4.440 XP; até o 60: 16.375.
public static class HeroLevelRules
{
    public const int MaxLevel = 60;

    // XP pra subir DO nível (índice + 1) pro seguinte. 59 valores (1→2 até 59→60). Editável. 🔢
    private static readonly long[] XpTable =
    {
        5, 10, 20, 30, 40, 45, 50, 55, 60, 65, // 1-10
        70, 75, 80, 85, 90, 95, 100, 105, 110, 115, // 11-20
        120, 125, 130, 135, 140, 145, 150, 155, 160, 165, // 21-30
        170, 175, 180, 185, 190, 195, 200, 205, 210, 215, // 31-40
        220, 225, 230, 235, 260, 295, 335, 380, 430, 490, // 41-50
        555, 630, 710, 805, 910, 1030, 1165, 1320, 1495, // 51-59
    };

    public static long XpToNext(int level)
    {
        if (level < 1 || level >= MaxLevel) return long.MaxValue;
        return XpTable[level - 1];
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
