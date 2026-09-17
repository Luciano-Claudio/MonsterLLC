using UnityEngine;

// Exceção ao Ranged comum (Skeleton Mage / Zombie Mage — GDD/Bestiary Andar 5): em vez de
// disparar um projétil vivo em direção ao player, conjura uma área de dano no chão sob a
// posição atual dele. Reaproveita 100% de Move()/InAttackRange() do RangedEnemyController
// (mesma lógica de manter distância) — só ExecuteAttackHit() muda.
public class GroundCasterEnemyController : RangedEnemyController
{
    public GameObject groundHazardPrefab;

    protected override void ExecuteAttackHit()
    {
        if (groundHazardPrefab == null)
        {
            Debug.LogWarning("[GroundCasterEnemyController] Sem groundHazardPrefab — dano aplicado direto como fallback.");
            var heroFallback = player.GetComponent<HeroController>();
            if (heroFallback != null) heroFallback.TakeDamage(stats.attackDamage);
            return;
        }

        // Nasce na posição atual do player no instante exato do Animation Event de cast —
        // não segue ele depois, e não é mirado (não é um projétil).
        var hazardObj = Instantiate(groundHazardPrefab, player.position, Quaternion.identity);
        var hazard = hazardObj.GetComponent<GroundTargetHazard>() ?? hazardObj.AddComponent<GroundTargetHazard>();
        hazard.damage = stats.attackDamage;
        hazard.ownerFloor = ownerFloor;
    }
}
