using UnityEngine;

public static class HealthSystem
{
    public static float ApplyDamage(float current, float damage) => Mathf.Max(current - damage, 0f);
    public static bool IsDead(float current) => current <= 0f;

    // Orc Shaman (Bestiário Andar 3) — primeiro caso de cura em MONSTRO (heróis já curam via
    // HeroController.Heal, caminho separado). Clampa no teto, nunca ultrapassa.
    public static float ApplyHeal(float current, float heal, float max) => Mathf.Min(current + heal, max);
}
