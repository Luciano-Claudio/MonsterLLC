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
}
