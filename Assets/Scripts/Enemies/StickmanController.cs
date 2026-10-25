using UnityEngine;

// Stickman (Bestiário Andar 9, boss): Melee comum em tudo (4 triggers direcionais, cooldown,
// Animation Event do golpe), mais um 5º trigger — uma área ao redor dele mesmo, que também
// dá dano no mesmo golpe. 2 Animation Events no clipe de ataque: o 1º é o AnimationHitEvent
// normal (golpe direcional, ver EnemyController/MeleeEnemyController), o 2º é
// AnimationAreaHitEvent (novo, só aqui) — dano em área, sem precisar o player estar
// exatamente no trigger direcional, só dentro do raio.
public class StickmanController : MeleeEnemyController
{
    [Header("Dano em área (5º trigger, acontece junto do golpe direcional)")]
    [SerializeField] private CircleCollider2D areaHitbox;

    // Animation Event, 2º evento do clipe de ataque (depois do golpe direcional normal).
    public void AnimationAreaHitEvent()
    {
        if (areaHitbox == null) return;

        var heroCollider = player.GetComponent<Collider2D>();
        if (heroCollider == null) return;
        if (!areaHitbox.IsTouching(heroCollider)) return;

        var hero = player.GetComponent<HeroController>();
        if (hero != null) hero.TakeDamage(stats.attackDamage);
    }
}
