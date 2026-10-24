using UnityEngine;

// Spectre (Bestiário Andar 5 — exceção pontual, só a superfície de gelo): Melee comum em
// tudo (mesmo critério de hitbox/cooldown do MeleeEnemyController), mas só se o golpe
// conectar de verdade, nasce uma superfície de gelo na posição do próprio attackHitbox que
// acertou — mesmo efeito do Lich (dano + lentidão, ver GroundTargetHazard), só muda onde
// nasce (aqui é o ponto do golpe, lá é embaixo do player) e o gatilho (aqui é golpe corpo a
// corpo conectando, lá é conjuração à distância).
public class SpectreController : MeleeEnemyController
{
    [Header("Superfície de gelo (nasce só se o golpe conectar — mesmo efeito do Lich)")]
    [SerializeField] private GameObject iceSurfacePrefab;

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

        if (iceSurfacePrefab != null)
            Instantiate(iceSurfacePrefab, hitbox.transform.position, Quaternion.identity);
    }
}
