using System.Collections.Generic;
using UnityEngine;

// Catálogo de cartas (docs/superpowers/specs/2026-11-25-catalogo-cartas-design.md) — cartas de projétil
// do Ranger (flechas e facas): Gravity Core (tamanho), Windpiercer (velocidade e alcance), Predator's Eye
// (persegue), Shattered Prism (estilhaços) e Ricochet Fate (1 salto). Estilhaços e ricochetes nascem como
// "filhos" e nunca geram outros filhos.
public static class ProjectileCards
{
    private static readonly List<EnemyController> nearby = new();
    public const float HomingTurnDegreesPerSecond = 360f; // 🔢

    private static float S(CardStat stat) => HeroController.Cards.Sum(CardAbility.None, stat);

    public static float SizeFactor => 1f + S(CardStat.ProjectileSize);
    public static float SpeedFactor => 1f + S(CardStat.ProjectileSpeed);
    public static float SplitChance => S(CardStat.ProjectileSplit);
    public static bool Ricochet => S(CardStat.RicochetFate) > 0f;

    // Raio de busca do Predator's Eye: base + 1 por nível (o valor da carta é 1 por nível). 0 = sem carta.
    public static float HomingRadius
    {
        get
        {
            float level = S(CardStat.ProjectileHoming);
            return level > 0f ? RunSettings.Current.homingBaseRadius + level : 0f;
        }
    }

    // Vira a direção na direção do alvo (guardado em `target`; procura outro quando ele morre).
    public static Vector2 Steer(Vector2 position, Vector2 direction, ref EnemyController target, float dt)
    {
        float radius = HomingRadius;
        if (radius <= 0f) return direction;
        if (target == null || target.IsDead || ((Vector2)target.transform.position - position).sqrMagnitude > radius * radius)
        {
            CardEffectsUtil.Nearby(position, radius, null, nearby);
            target = nearby.Count > 0 ? nearby[0] : null;
        }
        if (target == null) return direction;
        Vector2 wanted = ((Vector2)target.transform.position - position).normalized;
        float maxRadians = HomingTurnDegreesPerSecond * Mathf.Deg2Rad * dt;
        return Vector3.RotateTowards(direction, wanted, maxRadians, 0f).normalized;
    }

    // Inimigo mais perto pro ricochete (fora o atingido).
    public static EnemyController RicochetTarget(Vector2 from, EnemyController hit)
    {
        CardEffectsUtil.Nearby(from, RunSettings.Current.ricochetRange, hit, nearby);
        return nearby.Count > 0 ? nearby[0] : null;
    }

    public static Vector2 Rotate(Vector2 v, float degrees)
    {
        float r = degrees * Mathf.Deg2Rad, c = Mathf.Cos(r), s = Mathf.Sin(r);
        return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
    }
}
