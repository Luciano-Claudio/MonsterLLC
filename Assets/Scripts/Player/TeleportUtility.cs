using UnityEngine;

// Mira livre de teleporte/dash (Assassin/Mage, Shift) pode apontar pra uma distância que
// atravessa parede, água ou qualquer outro Collider2D sólido — esses deslocamentos setam a
// posição direto (ou uma distância fixa pro projétil de teleporte), sem passar pela física
// normal do Rigidbody2D que já impede isso durante a caminhada comum. ClampDistance() faz um
// Raycast2D na direção pretendida e encurta a distância se achar algo sólido no caminho, pra
// nunca pousar dentro de água/parede/prop.
public static class TeleportUtility
{
    private static readonly RaycastHit2D[] Buffer = new RaycastHit2D[8];
    // useTriggers=false — só Collider2D SÓLIDO bloqueia (água, parede, prop...). A layer "Enemy"
    // sai já no filtro do Raycast (não só no loop): com a horda densa, os 8 primeiros hits
    // poderiam ser todos monstros e esconder a parede logo atrás deles.
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

    private const float SafetyMargin = 0.2f; // 🔢 ajustável — pousa um pouco antes do collider sólido, nunca na borda/dentro dele

    public static float ClampDistance(Vector2 origin, Vector2 direction, float distance, Transform caster)
    {
        if (distance <= 0f) return 0f;

        int count = Physics2D.Raycast(origin, direction, SolidFilter, Buffer, distance);
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        float closestHit = float.MaxValue;
        for (int i = 0; i < count; i++)
        {
            var collider = Buffer[i].collider;
            // Monstro e o próprio herói não bloqueiam (ver TeleportBlockRule).
            if (!TeleportBlockRule.Blocks(collider.gameObject.layer, collider.isTrigger, collider.transform == caster, enemyLayer)) continue;
            if (Buffer[i].distance < closestHit) closestHit = Buffer[i].distance;
        }

        if (closestHit >= distance) return distance; // nada sólido no caminho
        return Mathf.Max(0f, closestHit - SafetyMargin);
    }
}
