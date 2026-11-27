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
    // O ponto de PARTIDA não é checado de propósito: a regra antiga soltava um raio a partir do pé do herói,
    // e com o pé sobre a borda da água/pedra/tronco (Physics2D.queriesStartInColliders ligado) o raio
    // acertava na distância 0 — o teleporte ficava no mesmo lugar "às vezes" (bug antigo, 2026-11-27).
    public static float ResolveDistance(Vector2 origin, Vector2 direction, float aimDistance, float maxRange, Transform caster)
    {
        Vector2 dir = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.right;
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        // O pouso checa o CORPO real do herói (o collider sólido dele fica acima do pé), não só o pé.
        Vector2 bodyOffset = Vector2.zero, bodySize = Vector2.zero;
        var body = BodyOf(caster);
        if (body != null)
        {
            var bounds = body.bounds;
            bodyOffset = (Vector2)(bounds.center - caster.position);
            bodySize = (Vector2)bounds.size * 0.9f; // um pouco menor: encostar não é estar dentro
        }
        float distance = TeleportRules.ResolveDistance(aimDistance, maxRange,
            t => IsFree(origin + dir * t, caster, enemyLayer, bodyOffset, bodySize));
        if (distance < 0.05f && Mathf.Min(aimDistance, maxRange) > 0.5f)
            Debug.LogWarning($"[Teleporte] Quase sem sair do lugar: origem={origin} direção={dir} mira={aimDistance:0.##} " +
                $"alcance={maxRange:0.##} resultado={distance:0.##} corpo={(body != null ? body.name : "nenhum")} — mande este aviso pro dev.");
        return distance;
    }

    private static Collider2D BodyOf(Transform caster)
    {
        if (caster == null) return null;
        foreach (var c in caster.GetComponents<Collider2D>())
            if (c != null && c.enabled && !c.isTrigger) return c;
        return null;
    }

    // Livre = nenhum collider SÓLIDO de cenário onde o corpo do herói ficaria (monstro, trigger e o próprio
    // herói não contam — TeleportBlockRule). Sem collider de corpo: um círculo no pé.
    public static bool IsFree(Vector2 point, Transform caster, int enemyLayer) => IsFree(point, caster, enemyLayer, Vector2.zero, Vector2.zero);

    private static bool IsFree(Vector2 point, Transform caster, int enemyLayer, Vector2 bodyOffset, Vector2 bodySize)
    {
        int count = bodySize.x > 0f && bodySize.y > 0f
            ? Physics2D.OverlapBox(point + bodyOffset, bodySize, 0f, SolidFilter, Overlaps)
            : Physics2D.OverlapCircle(point, HeroRadius, SolidFilter, Overlaps);
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
