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

    // Nome do estado de Animator correspondente a uma das 8 direções (GDD Seção 13 —
    // projéteis de herói/monstro com sprite própria por direção, sem Blend Tree: a
    // direção nunca muda depois do disparo, então basta um Animator.Play() por nome uma
    // vez no lançamento). Espera uma direção já resolvida por SnapTo8Directions.
    public static string GetDirectionName(Vector2 snappedDirection)
    {
        if (snappedDirection.sqrMagnitude < 0.0001f) return "S";

        float angle = Mathf.Atan2(snappedDirection.y, snappedDirection.x) * Mathf.Rad2Deg;
        if (angle < 0f) angle += 360f;

        string[] names = { "E", "NE", "N", "NW", "W", "SW", "S", "SE" };
        int index = Mathf.RoundToInt(angle / 45f) % 8;
        return names[index];
    }
}
