using UnityEngine;

public static class EnergySystem
{
    public static float AddEnergy(float current, float max, float amount) =>
        Mathf.Min(current + amount, max);

    public static bool IsReady(float current, float max) => current >= max;
}
