using UnityEngine;

// Teste alternativo ao leque angular (FanSpread) pro Ranger: em vez de várias flechas
// saindo em ângulos diferentes, todas viajam na MESMA direção (paralelas), só nascem
// deslocadas formando uma cunha em "V" (tipo bando de pássaros voando) — flechas de dentro
// nascem mais à frente, flechas de fora nascem mais atrás e mais afastadas lateralmente.
// Retorna deslocamentos locais (x = pra frente na direção da mira, y = lateral/perpendicular)
// relativos ao ponto de nascimento normal (deslocamento 0,0 = onde nasceria com 1 flecha só).
public static class ArrowFormation
{
    public static Vector2[] GetOffsets(int count, float lateralStep, float forwardStep)
    {
        if (count <= 1) return new[] { Vector2.zero };

        bool hasCenter = count % 2 != 0;
        int pairCount = hasCenter ? (count - 1) / 2 : count / 2;
        int maxRank = pairCount;

        var offsets = new Vector2[count];
        int index = 0;

        if (hasCenter)
            offsets[index++] = new Vector2(maxRank * forwardStep, 0f);

        // Quantidade ímpar tem flecha central (lateral 0), então cada rank fica a
        // rank*lateralStep dela — todo vão vizinho já sai igual a lateralStep. Quantidade
        // par não tem centro, então o par mais interno precisa nascer só a MEIO passo do
        // meio (rank - 0.5) — sem isso, o vão entre as duas flechas centrais dobrava de
        // tamanho (virava 2×lateralStep) em vez de ficar igual aos outros vãos.
        for (int rank = 1; rank <= pairCount; rank++)
        {
            float forward = (maxRank - rank) * forwardStep;
            float lateral = (hasCenter ? rank : rank - 0.5f) * lateralStep;
            offsets[index++] = new Vector2(forward, lateral);
            offsets[index++] = new Vector2(forward, -lateral);
        }

        return offsets;
    }
}
