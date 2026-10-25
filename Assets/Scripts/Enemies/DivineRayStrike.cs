using UnityEngine;

// Raio do Divine God (Bestiário Andar 10) — nasce parado na posição calculada por
// DivineGodController.AnimationCastRayEvent() e fica "fixo ali" (nunca persegue o player).
// Telegraph confirmado com o usuário: o trigger só é checado DEPOIS que a própria animação de
// queda (ray_land, Divine_Wrath_Spell) termina de tocar — dá tempo real de desviar enquanto
// ela joga — não há dano nenhum no instante em que o objeto nasce.
public class DivineRayStrike : MonoBehaviour
{
    [SerializeField] private Collider2D hitbox; // trigger, ajustável no Editor (gizmo)

    private float damage;
    private Transform player;

    public void Initialize(float rayDamage, Transform playerTransform)
    {
        damage = rayDamage;
        player = playerTransform;
    }

    // Animation Event, último frame do ray_land.
    public void AnimationRayLandEvent()
    {
        if (hitbox != null && player != null)
        {
            var heroCollider = player.GetComponent<Collider2D>();
            if (heroCollider != null && hitbox.IsTouching(heroCollider))
            {
                var hero = player.GetComponent<HeroController>();
                if (hero != null) hero.TakeDamage(damage);
            }
        }

        Destroy(gameObject);
    }
}
