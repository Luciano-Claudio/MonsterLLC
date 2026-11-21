using UnityEngine;

// D9.4 (pedido do usuário, 2026-11-21) — aviso de explosão em 2 círculos: a área completa, fraca,
// e um círculo mais forte que cresce do centro até preencher a área no instante da explosão.
// Progress = fração desse crescimento (0 = acabou de começar, 1 = explode agora).
public static class TelegraphRules
{
    public static float Progress(float elapsedSeconds, float explodeAtSeconds) =>
        explodeAtSeconds <= 0f ? 1f : Mathf.Clamp01(elapsedSeconds / explodeAtSeconds);
}
