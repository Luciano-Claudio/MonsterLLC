using UnityEngine;

public class Ranger : HeroController
{
    [SerializeField] private GameObject arrowPrefab;
    [SerializeField] private int arrowCount = 1; // 🔢 teto de 5 + hook de carta chegam na Sprint 18
    [SerializeField] private float totalSpreadDegrees = 30f; // GDD Seção 17.2

    protected override void PrimaryAttack()
    {
        // Cooldown (attackSpeed) e "segurar o botão continua atacando" já são resolvidos
        // pela base — aqui só entra a lógica específica do leque de flechas.
        Vector2[] directions = FanSpread.GetDirections(AimDirection, arrowCount, totalSpreadDegrees);
        foreach (var dir in directions)
        {
            var arrowObj = Instantiate(arrowPrefab, transform.position, Quaternion.identity);
            arrowObj.GetComponent<RangerArrow>().Launch(dir, stats.damage);
        }
    }

    protected override void UseUltimate()
    {
        Debug.Log("[Ranger] Ultimate (facas persistentes) ainda não implementada — Sprint 18.");
    }

    // Sem Card Framework ainda (Sprint 35) — incremento manual só pra provar o leque nesta sprint.
    [ContextMenu("Debug: +1 Flecha")]
    private void DebugIncreaseArrowCount()
    {
        arrowCount++;
        Debug.Log($"[Ranger] arrowCount = {arrowCount}");
    }
}
