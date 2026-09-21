using UnityEngine;

public static class DirectionUtility
{
    // Mesma fórmula do HeroController (Sprint 7) — GDD Seção 11: "resolvida em 8 direções".
    public static Vector2 SnapTo8Directions(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.0001f) return Vector2.zero;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        float snappedAngle = Mathf.Round(angle / 45f) * 45f;
        return new Vector2(Mathf.Cos(snappedAngle * Mathf.Deg2Rad), Mathf.Sin(snappedAngle * Mathf.Deg2Rad));
    }

    private static readonly string[] DirectionNames = { "E", "NE", "N", "NW", "W", "SW", "S", "SE" };

    // Nome do estado de Animator correspondente a uma das 8 direções (GDD Seção 13 —
    // projéteis de herói/monstro com sprite própria por direção, sem Blend Tree: a
    // direção nunca muda depois do disparo, então basta um Animator.Play() por nome uma
    // vez no lançamento). Espera uma direção já resolvida por SnapTo8Directions.
    public static string GetDirectionName(Vector2 snappedDirection) => DirectionNames[GetDirectionIndex(snappedDirection)];

    // Mesma direção, como índice (0-7, mesma ordem de DirectionNames) — usado quando o
    // Animator precisa de um parâmetro Int em vez do nome do estado (ex.: escolher entre
    // estados soltos de ataque direcional via transição, sem Blend Tree — ver Ranger).
    public static int GetDirectionIndex(Vector2 snappedDirection)
    {
        if (snappedDirection.sqrMagnitude < 0.0001f) return 6; // S, mesmo fallback de GetDirectionName

        float angle = Mathf.Atan2(snappedDirection.y, snappedDirection.x) * Mathf.Rad2Deg;
        if (angle < 0f) angle += 360f;

        return Mathf.RoundToInt(angle / 45f) % 8;
    }

    // Inverso de GetDirectionIndex — dado o índice (0-7, mesma ordem de DirectionNames:
    // E, NE, N, NW, W, SW, S, SE), devolve o vetor unitário daquela direção fixa. Usado
    // quando a direção nasce de uma das 8 direções fixas conhecidas (ex.: cada faca da
    // Ultimate do Ranger), não de mira real.
    public static Vector2 DirectionFromIndex(int index)
    {
        float angle = index * 45f * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
    }

    // Direção crua encaixada só nas 4 diagonais (NE/NW/SE/SW) — NUNCA cardeal pura. Usada
    // pelos Blend Trees que só têm pose desenhada pras diagonais (Idle/Walk/Damage/SummonPet
    // — e hoje todo estado do Barbarian/Ranger). SnapTo8Directions sozinho pode devolver um
    // cardeal puro (N/E/S/W); num Blend Tree que só tem as 4 diagonais como amostra, isso cai
    // numa "zona morta" de 45° em volta de cada eixo cardeal, onde as 2 diagonais vizinhas
    // ficam EXATAMENTE equidistantes — o cálculo de peso do Unity fica instável bem no meio
    // dessa zona (ruído mínimo decide pra qual lado pende), o que parece um "flip" aleatório
    // perto do eixo. Aqui não existe zona ambígua: cada metade do plano (dividida só pelo
    // sinal de X e de Y, uma linha sem largura) sempre resolve pra exatamente 1 diagonal.
    public static Vector2 SnapTo4Diagonals(Vector2 direction)
    {
        float x = direction.x >= 0f ? 0.70710678f : -0.70710678f;
        float y = direction.y >= 0f ? 0.70710678f : -0.70710678f;
        return new Vector2(x, y);
    }
}
