using UnityEngine;

// Teleporte de mira livre (Mage e Assassin) — regra do usuário (2026-11-27): o teleporte ATRAVESSA os
// obstáculos do caminho (ex.: um lago), mas a posição final NUNCA fica dentro de um collider:
// - ponto mirado livre: pousa lá;
// - ponto mirado dentro de um obstáculo: o 1º ponto livre DEPOIS dele, se o alcance chegar;
// - se não chegar: o 1º ponto livre ANTES dele (a borda de cá, sem atravessar).
// isFree(t) diz se a posição a t de distância (na direção da mira) está livre — quem chama decide como.
public static class TeleportRules
{
    public const float Step = 0.1f; // 🔢 precisão da busca do ponto livre

    public static float ResolveDistance(float aimDistance, float maxRange, System.Func<float, bool> isFree, float step = Step)
    {
        float max = Mathf.Max(0f, maxRange);
        float d = Mathf.Clamp(aimDistance, 0f, max);
        if (isFree(d)) return d;
        for (float t = d + step; t <= max + 1e-4f; t += step)
            if (isFree(Mathf.Min(t, max))) return Mathf.Min(t, max);
        for (float t = d - step; t > 0f; t -= step)
            if (isFree(t)) return t;
        return 0f;
    }
}
