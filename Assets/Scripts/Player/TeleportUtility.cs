using UnityEngine;

// Mira livre de teleporte (Assassin/Mage, Shift). Esses deslocamentos setam a posição direto (ou uma
// distância fixa pro projétil de teleporte), sem a física normal do Rigidbody2D. Desde 2026-11-27 o
// teleporte atravessa obstáculos (ex.: um lago), mas nunca pousa dentro de um (ResolveDistance).
public static class TeleportUtility
{
    // Só Collider2D SÓLIDO conta (água, parede, prop...). A layer "Enemy" sai já no filtro (não só no
    // loop): com a horda densa, os primeiros resultados poderiam ser todos monstros.
    private static ContactFilter2D solidFilter;
    private static bool solidFilterReady;

    private static ContactFilter2D SolidFilter
    {
        get
        {
            if (!solidFilterReady)
            {
                solidFilter = new ContactFilter2D();
                int enemyLayer = LayerMask.NameToLayer("Enemy");
                if (enemyLayer >= 0) solidFilter.SetLayerMask(~(1 << enemyLayer));
                solidFilterReady = true;
            }
            return solidFilter;
        }
    }

    private static readonly Collider2D[] Overlaps = new Collider2D[16];
    private const float HeroRadius = 0.3f; // 🔢 tamanho do corpo do herói na checagem do ponto de pouso

    // Regra do usuário (2026-11-27, ver TeleportRules): atravessa obstáculos, mas nunca pousa dentro de um.
    // Mirou dentro de um obstáculo: outro lado se o alcance der, senão a borda de cá.
    public static float ResolveDistance(Vector2 origin, Vector2 direction, float aimDistance, float maxRange, Transform caster)
    {
        Vector2 dir = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.right;
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        return TeleportRules.ResolveDistance(aimDistance, maxRange, t => IsFree(origin + dir * t, caster, enemyLayer));
    }

    // Livre = nenhum collider SÓLIDO de cenário no círculo do corpo do herói (monstro, trigger e o próprio
    // herói não contam — TeleportBlockRule).
    public static bool IsFree(Vector2 point, Transform caster, int enemyLayer)
    {
        int count = Physics2D.OverlapCircle(point, HeroRadius, SolidFilter, Overlaps);
        for (int i = 0; i < count; i++)
        {
            var collider = Overlaps[i];
            if (collider == null) continue;
            bool isCaster = caster != null && (collider.transform == caster || collider.transform.IsChildOf(caster));
            if (TeleportBlockRule.Blocks(collider.gameObject.layer, collider.isTrigger, isCaster, enemyLayer)) return false;
        }
        return true;
    }
}
