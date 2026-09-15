using UnityEngine;

public class MeleeEnemyController : EnemyController
{
    // 4 clipes de ataque diagonais (NE/NW/SE/SW), cada um com seu próprio trigger filho
    // fixo naquela direção — o GameObject nunca vira, só a animação muda, então o trigger
    // certo depende de pra que lado o monstro estava olhando quando o ataque começou.
    // Ficam sem uso se hasAttackAnimation for falso (ex.: Slimes).
    [Header("Triggers de ataque (um por direção diagonal)")]
    [SerializeField] private Collider2D attackHitboxNE;
    [SerializeField] private Collider2D attackHitboxNW;
    [SerializeField] private Collider2D attackHitboxSE;
    [SerializeField] private Collider2D attackHitboxSW;

    protected override void Move()
    {
        Vector2 toPlayer = player.position - transform.position;
        if (toPlayer.magnitude > stats.attackRadius)
        {
            SetMoving(true);
            MoveInDirection(toPlayer.normalized);
        }
        else
        {
            SetMoving(false); // colado no player -> idle_combat, enquanto os cooldowns não liberam
        }
    }

    protected override bool InAttackRange() =>
        Vector2.Distance(transform.position, player.position) <= stats.attackRadius;

    protected override void ExecuteAttackHit()
    {
        var hitbox = GetHitboxForFacing();
        if (hitbox == null) return;

        var heroCollider = player.GetComponent<Collider2D>();
        if (heroCollider == null) return;
        if (!hitbox.IsTouching(heroCollider)) return; // player fora do trigger nesse frame exato -> o golpe erra

        var hero = player.GetComponent<HeroController>();
        if (hero == null) return;

        Debug.Log($"[MeleeEnemyController] Golpe real conecta: {stats.attackDamage}.");
        hero.TakeDamage(stats.attackDamage);
    }

    // Classifica AimDirection (sempre em direção ao player) num dos 4 quadrantes
    // diagonais — mesmo critério que o Blend Tree 2D Freeform Directional usa pra
    // escolher entre atk_ne/nw/se/sw.
    private Collider2D GetHitboxForFacing()
    {
        bool east = AimDirection.x >= 0f;
        bool north = AimDirection.y >= 0f;

        if (north) return east ? attackHitboxNE : attackHitboxNW;
        return east ? attackHitboxSE : attackHitboxSW;
    }
}
