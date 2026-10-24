using UnityEngine;

// Undead Dragon (Bestiário Andar 7): golpe normal de Melee comum (1 trigger só conectando,
// igual a qualquer outro Melee — nenhum prefab nasce, nenhuma área no chão), mas o golpe
// também aplica lentidão (StatusEffectType.Ice, mesmo efeito já usado no Lich/Spectre —
// ApplyStatusEffect direto, sem dano por tick: o dano real já é o hit normal acima).
public class UndeadDragonController : MeleeEnemyController
{
    [Header("Lentidão (aplicada direto no golpe, sem nenhum prefab)")]
    [SerializeField] private float slowDuration = 3f; // 🔢 ajustável

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

        if (!hero.IsCurrentlyDamageImmune)
        {
            var statusController = hero.GetComponent<StatusEffectController>();
            if (statusController != null)
                statusController.ApplyStatusEffect(StatusEffectType.Ice, slowDuration, 0f);
        }
    }
}
