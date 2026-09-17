using UnityEngine;

// GDD Seção 17.2 (Multi-Straight/leque): 2 flechas = 30° entre si, 3 = 15° entre si — mesmo
// total de spread de ponta a ponta, não um incremento por flecha.
public static class FanSpread
{
    // Distribui `count` direções simetricamente ao redor de `baseDirection`, cobrindo
    // `totalSpreadDegrees` no total entre a primeira e a última. 1 direção = baseDirection
    // sem alteração nenhuma.
    public static Vector2[] GetDirections(Vector2 baseDirection, int count, float totalSpreadDegrees)
    {
        if (count <= 1) return new[] { baseDirection };

        var directions = new Vector2[count];
        float step = totalSpreadDegrees / (count - 1);
        float startAngle = -totalSpreadDegrees / 2f;

        for (int i = 0; i < count; i++)
            directions[i] = Rotate(baseDirection, startAngle + step * i);

        return directions;
    }

    private static Vector2 Rotate(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad), sin = Mathf.Sin(rad);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }
}
