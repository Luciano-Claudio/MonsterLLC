using UnityEngine;

// Ancient Danger Leader (Bestiário Andar 6 — exceção pontual, só o impacto): Melee comum em
// tudo (mesmo critério de hitbox/cooldown do MeleeEnemyController), mas só se o golpe
// conectar de verdade, nasce um efeito de impacto na posição do próprio attackHitbox que
// acertou — mesmo gatilho/posição do Spectre, mas SEM efeito de status: o prefab aqui só dá
// dano (ver GroundTargetHazard com appliesStatusEffect desligado), sem lentidão nem nada.
public class AncientDangerLeaderController : MeleeEnemyController
{
    [Header("Efeito de impacto (nasce só se o golpe conectar — só dano, sem efeito de status)")]
    [SerializeField] private GameObject impactEffectPrefab;

    protected override void ExecuteAttackHit()
    {
        var hitbox = GetHitboxForFacing();
        if (hitbox == null) return;

        var heroCollider = player.GetComponent<Collider2D>();
        if (heroCollider == null) return;
        if (!hitbox.IsTouching(heroCollider)) return; // player fora do trigger nesse frame exato -> o golpe erra

        var hero = player.GetComponent<HeroController>();
        if (hero == null) return;

        hero.TakeDamage(stats.attackDamage);

        if (impactEffectPrefab != null)
            Instantiate(impactEffectPrefab, hitbox.transform.position, Quaternion.identity);
    }
}
