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
    // useTriggers=false, useLayerMask=false (defaults) — qualquer Collider2D SÓLIDO (nunca um
    // trigger de monstro/pickup) bloqueia, de qualquer layer (água, parede, prop...).
    private static readonly ContactFilter2D SolidFilter = new ContactFilter2D();

    private const float SafetyMargin = 0.2f; // 🔢 ajustável — pousa um pouco antes do collider sólido, nunca na borda/dentro dele

    public static float ClampDistance(Vector2 origin, Vector2 direction, float distance, Transform caster)
    {
        if (distance <= 0f) return 0f;

        int count = Physics2D.Raycast(origin, direction, SolidFilter, Buffer, distance);
        float closestHit = float.MaxValue;
        for (int i = 0; i < count; i++)
        {
            if (Buffer[i].collider.transform == caster) continue; // ignora o próprio collider do herói
            if (Buffer[i].distance < closestHit) closestHit = Buffer[i].distance;
        }

        if (closestHit >= distance) return distance; // nada sólido no caminho
        return Mathf.Max(0f, closestHit - SafetyMargin);
    }
}
