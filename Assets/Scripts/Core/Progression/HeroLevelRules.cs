using UnityEngine;

// D9.6b — nível do herói (docs/superpowers/specs/2026-11-22-d9-6ab-monstros-xp-nivel-design.md, 3.2–3.4).
// O nível tem que se SENTIR (decisão do usuário): +40% de dano e +30% de vida por nível, com piso
// de +1 de dano e +5 de vida pra nenhum nível passar em branco. Escala menos que os monstros de
// propósito — a build (itens + cartas) fecha a diferença.
public static class HeroLevelRules
{
    public const int MaxLevel = 20;
    public const float DamageGrowth = 1.40f;
    public const float HealthGrowth = 1.30f;
    public const float DamageFloor = 1f;
    public const float HealthFloor = 5f;

    // XP pra subir do nível (índice) pro seguinte. Índice 0 sem uso. ~100 kills do tier do dia por
    // nível; no tier anterior, 3–4× mais (é o que empurra o jogador a subir de tier).
    private static readonly long[] XpTable =
    {
        0, 40, 260, 520, 1000, 1400, 3300, 4600, 10000, 15500, 33500,
        46000, 104000, 136000, 294000, 426000, 874000, 1216000, 2530000, 3570000,
    };

    public static float DamageAt(float baseDamage, int level)
    {
        float d = baseDamage;
        for (int l = 2; l <= Mathf.Min(level, MaxLevel); l++) d = Mathf.Max(d * DamageGrowth, d + DamageFloor);
        return d;
    }

    public static float HealthAt(float baseHealth, int level)
    {
        float h = baseHealth;
        for (int l = 2; l <= Mathf.Min(level, MaxLevel); l++) h = Mathf.Max(h * HealthGrowth, h + HealthFloor);
        return h;
    }

    public static long XpToNext(int level) =>
        level >= 1 && level < MaxLevel ? XpTable[level] : long.MaxValue;

    // Cap do dia = o nível que fecha aquele dia (2·dia) + 1 de folga. Segura o "voltar o dia" (não
    // dá pra passar do nível do dia farmando tier baixo).
    public static int LevelCapForDay(int day) => Mathf.Min(MaxLevel, 2 * Mathf.Max(1, day) + 1);

    public struct XpResult
    {
        public int level;
        public long xpIntoLevel;
        public long goldGained;
        public long goldConvertedToday;
        public int levelsGained;
    }

    // Aplica um ganho de XP: sobe quantos níveis couberem até o cap do dia. No cap, a barra enche
    // até o ponto de subir (sobe quando o próximo dia liberar) e só o que passar disso vira gold,
    // 1 por XP, até goldDailyLimit por dia; o resto se perde.
    public static XpResult AddXp(int level, long xpIntoLevel, long xpGained, int day, long goldConvertedToday, int goldDailyLimit)
    {
        var r = new XpResult { level = level, xpIntoLevel = xpIntoLevel, goldConvertedToday = goldConvertedToday };
        int cap = LevelCapForDay(day);
        long remaining = System.Math.Max(0, xpGained);

        while (remaining > 0)
        {
            if (r.level >= cap || r.level >= MaxLevel)
            {
                if (r.level < MaxLevel)
                {
                    long space = System.Math.Max(0, XpToNext(r.level) - 1 - r.xpIntoLevel);
                    long fill = System.Math.Min(space, remaining);
                    r.xpIntoLevel += fill;
                    remaining -= fill;
                }
                long room = System.Math.Max(0, goldDailyLimit - r.goldConvertedToday);
                long gold = System.Math.Min(room, remaining);
                r.goldGained += gold;
                r.goldConvertedToday += gold;
                break;
            }

            long need = XpToNext(r.level) - r.xpIntoLevel;
            if (remaining >= need)
            {
                remaining -= need;
                r.level++;
                r.xpIntoLevel = 0;
                r.levelsGained++;
            }
            else
            {
                r.xpIntoLevel += remaining;
                remaining = 0;
            }
        }
        return r;
    }
}
