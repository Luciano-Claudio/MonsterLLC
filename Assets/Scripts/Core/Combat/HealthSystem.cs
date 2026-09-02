using UnityEngine;

public static class HealthSystem
{
    public static float ApplyDamage(float current, float damage) => Mathf.Max(current - damage, 0f);
    public static bool IsDead(float current) => current <= 0f;
}
