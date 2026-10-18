using UnityEngine;

// Decide, de forma pura e determinística, qual dos 6 GRUPOS de terreno (Tier1, Tier2-3,
// Tier4-5, Tier6-8, Tier9, Tier10 — índice 0..5) está ativo numa célula global. Baseado
// inteiramente no mecanismo já documentado (docs/new/Mudanca_Estrutural_Mapa_Unico_Spawn_
// Bosses_ATUALIZADO_V2.md, Seções 5 e 8): a mesma tabela de % de spawn por Tier/Dia, e o
// mesmo EffectiveSpawnDay (distância da safe zone = "dias futuros" simulados espacialmente).
// Terreno usa SEMPRE o período "Dia" da tabela (decisão do usuário — bioma não deve oscilar
// a cada ciclo dia/noite, só avançar com o tempo/distância de verdade).
public static class WorldTierSelector
{
    public const int TierGroupCount = 6;

    // Abaixo de 20% de presença, o grupo simplesmente não aparece no mapa (decisão do
    // usuário) — o que sobra é redistribuído proporcionalmente entre os grupos que ficaram
    // ativos, pra sempre fechar 100% (sem "buraco" no mapa).
    public const float MinGroupPercent = 0.20f;

    // Cada grupo de terreno cobre um intervalo de índices T1..T10 (0-based: T1=índice 0).
    // Tier6-8 cobre 3 tiers de monstro (T6,T7,T8) — a % do grupo é a do tier MAIS FORTE entre
    // os que ele cobre, não a soma deles (decisão explícita do usuário: cada tier de monstro
    // compete isoladamente pelos 20%/dominância, nunca somado com os outros do mesmo grupo —
    // senão um grupo de 3 tiers fracos venceria artificialmente um grupo de 1 tier forte).
    private static readonly (int startTierIndex, int count)[] TierGroupRanges =
    {
        (0, 1), // Tier1       = T1
        (1, 2), // Tier2-3     = T2, T3
        (3, 2), // Tier4-5     = T4, T5
        (5, 3), // Tier6-8     = T6, T7, T8
        (8, 1), // Tier9       = T9
        (9, 1), // Tier10      = T10
    };

    // EffectiveSpawnDay (Seção 8.2): DistanceDayOffset = floor(DistanceBeyondSafeZone /
    // DistancePerSpawnDay); EffectiveSpawnDay = clamp(ActualDay + DistanceDayOffset, 1, 30).
    // A Safe Zone não muda pesos (Seção 6.3) — só desloca de onde a distância começa a contar.
    public static int ComputeEffectiveSpawnDay(
        float distanceFromCenter, float safeZoneRadius, float distancePerSpawnDay, int actualDay)
    {
        float distanceBeyondSafeZone = Mathf.Max(0f, distanceFromCenter - safeZoneRadius);

        int distanceDayOffset = distancePerSpawnDay > 0f
            ? Mathf.FloorToInt(distanceBeyondSafeZone / distancePerSpawnDay)
            : 0;

        return Mathf.Clamp(actualDay + distanceDayOffset, 1, 30);
    }

    // % finais dos 6 grupos (soma sempre 1 = 100%, já com o piso de 20% aplicado e
    // redistribuído). `dayDef.dayWeights` está em % literal (0-100), devolve fração (0-1).
    public static float[] ComputeTierGroupPercents(DaySpawnDefinition dayDef)
    {
        var percents = new float[TierGroupCount];
        ComputeTierGroupPercentsNonAlloc(dayDef, percents);
        return percents;
    }

    // Mesma lógica, sem alocar array novo — escreve em `buffer` (reaproveitado pelo
    // chamador). Existe pelo mesmo motivo de `TerrainSelector.ChooseTerrainStackNonAlloc`:
    // isso roda 1x por célula do mundo (até milhares de vezes por chunk), e a versão
    // alocando já causou engasgo de GC uma vez nesta sprint — não repetir o erro aqui.
    // `buffer` precisa ter pelo menos `TierGroupCount` posições.
    public static void ComputeTierGroupPercentsNonAlloc(DaySpawnDefinition dayDef, float[] buffer)
    {
        for (int g = 0; g < TierGroupCount; g++) buffer[g] = 0f;
        if (dayDef == null || dayDef.dayWeights == null) return;

        for (int g = 0; g < TierGroupCount; g++)
        {
            var (startTierIndex, count) = TierGroupRanges[g];
            float strongest = 0f;
            for (int t = startTierIndex; t < startTierIndex + count && t < dayDef.dayWeights.Length; t++)
            {
                if (dayDef.dayWeights[t] > strongest) strongest = dayDef.dayWeights[t];
            }
            buffer[g] = strongest / 100f;
        }

        float activeSum = 0f;
        for (int g = 0; g < TierGroupCount; g++)
        {
            if (buffer[g] < MinGroupPercent) buffer[g] = 0f;
            else activeSum += buffer[g];
        }

        if (activeSum > 0f)
        {
            for (int g = 0; g < TierGroupCount; g++)
            {
                if (buffer[g] > 0f) buffer[g] /= activeSum;
            }
        }
    }

    // Escolhe o grupo ativo numa célula por amostragem cumulativa (tipo roleta) sobre 1 único
    // valor de ruído coerente — ao contrário do TerrainMask (testes independentes por entrada,
    // que podem deixar "buraco" se nada bater), aqui cada célula SEMPRE pertence a exatamente
    // 1 grupo, nunca fica sem terreno algum. `groupPercents` precisa somar 1 (ver
    // ComputeTierGroupPercents).
    public static int ChooseActiveTierGroup(
        int cellX, int cellY, float seedOffsetX, float seedOffsetY, float noiseScale, float[] groupPercents)
    {
        float n = Mathf.PerlinNoise((cellX + seedOffsetX) * noiseScale, (cellY + seedOffsetY) * noiseScale);

        float cumulative = 0f;
        for (int g = 0; g < TierGroupCount; g++)
        {
            cumulative += groupPercents[g];
            if (n < cumulative) return g;
        }

        // Sobra de arredondamento de float (soma ligeiramente < 1) — cai no último grupo ativo.
        for (int g = TierGroupCount - 1; g >= 0; g--)
        {
            if (groupPercents[g] > 0f) return g;
        }

        return 0;
    }

    // 10 Tiers de monstro (T1..T10, índice 0-based) — igual DaySpawnDefinition.dayWeights/
    // nightWeights. Diferente de ComputeTierGroupPercentsNonAlloc (6 grupos de TERRENO, com
    // "mais forte vence" + piso de 20%): spawn de monstro comum (Fase A, docs/new/Mudanca_
    // Estrutural_Mapa_Unico_Spawn_Bosses_ATUALIZADO_V2.md, Seção 16) é só normalização
    // DIRETA dos 10 pesos — decisão do usuário: o piso de 20% foi pensado especificamente
    // pra grupo de terreno (evitar grupo fraco de vários tiers vencer artificialmente grupo
    // forte de 1 tier só); não se aplica do mesmo jeito a 10 tiers individuais. Bate com a
    // regra de Tier Suppression do doc (remover peso = renormalizar proporcional entre os
    // que sobraram, sem piso nenhum).
    public const int MonsterTierCount = 10;

    // `buffer` precisa ter pelo menos MonsterTierCount posições. `suppressedTiers`
    // (opcional, null = nenhum suprimido) zera o peso de qualquer tier marcado true —
    // Tier Suppression (compra de remoção de camada, upgrade sequencial P1→P2→...).
    // `isNight` (default false) troca `dayWeights` por `nightWeights` — usado só pelo spawn de
    // monstro/boss (Ciclo Dia/Noite real), NUNCA pelo terreno/bioma
    // (ComputeTierGroupPercentsNonAlloc não tem esse parâmetro de propósito — decisão do
    // usuário: bioma não deve oscilar a cada troca de fase, só avançar com dia/distância).
    public static void ComputeTierPercentsNonAlloc(DaySpawnDefinition dayDef, float[] buffer, bool[] suppressedTiers = null, bool isNight = false)
    {
        for (int t = 0; t < MonsterTierCount; t++) buffer[t] = 0f;
        var weights = isNight ? dayDef?.nightWeights : dayDef?.dayWeights;
        if (weights == null) return;

        float sum = 0f;
        for (int t = 0; t < MonsterTierCount && t < weights.Length; t++)
        {
            if (suppressedTiers != null && t < suppressedTiers.Length && suppressedTiers[t]) continue;
            buffer[t] = weights[t] / 100f;
            sum += buffer[t];
        }

        if (sum > 0f)
        {
            for (int t = 0; t < MonsterTierCount; t++) buffer[t] /= sum;
        }
    }

    // Roleta cumulativa igual ChooseActiveTierGroup, mas pros 10 tiers individuais
    // ("Roll Tier", doc Seção 16) — recebe um valor já sorteado (Random.value) em vez de
    // ruído Perlin por célula: spawn de monstro é um evento pontual no tempo, não precisa
    // de coerência espacial célula a célula como o terreno. `tierPercents` precisa somar 1
    // (ver ComputeTierPercentsNonAlloc). Devolve índice 0-based (T1=0..T10=9).
    public static int RollMonsterTier(float roll01, float[] tierPercents)
    {
        float cumulative = 0f;
        for (int t = 0; t < MonsterTierCount; t++)
        {
            cumulative += tierPercents[t];
            if (roll01 < cumulative) return t;
        }

        // Sobra de arredondamento — cai no último tier ativo.
        for (int t = MonsterTierCount - 1; t >= 0; t--)
        {
            if (tierPercents[t] > 0f) return t;
        }

        return -1; // nenhum tier com peso > 0 — chamador precisa checar (ex.: roster vazio)
    }
}
