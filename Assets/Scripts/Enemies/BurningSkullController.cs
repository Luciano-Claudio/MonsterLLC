using UnityEngine;

// Burning Skull (Bestiário Andar 5) — kamikaze: nunca ataca de verdade, a aproximação em
// si é a ameaça. Ao tocar o player, vai direto pra Die (explode); se for morto por dano
// antes de chegar perto, também vai direto pra Die — sem golpe real, sem pipeline de
// Attack/AttackTrigger (hasAttackAnimation deve ficar FALSE no Inspector).
public class BurningSkullController : EnemyController
{
    [Header("Contato (toque = Die instantâneo, sem golpe real)")]
    [SerializeField] private Collider2D contactHitbox;

    [Header("Área de explosão (filho — BurningSkullExplosionArea)")]
    [SerializeField] private BurningSkullExplosionArea explosionArea;

    protected override void Move(float deltaTime)
    {
        if (IsPlayerTouchingContact())
        {
            SetMoving(false);
            Die();
            return;
        }

        Vector2 toPlayer = player.position - transform.position;
        SetMoving(true);
        MoveInDirection(toPlayer.normalized);
    }

    private bool IsPlayerTouchingContact()
    {
        if (contactHitbox == null) return false;
        var heroCollider = player.GetComponent<Collider2D>();
        return heroCollider != null && contactHitbox.IsTouching(heroCollider);
    }

    // Pipeline genérico de Attack nunca é usado — a única "ação" deste monstro é Move()
    // decidir ir pra Die acima.
    protected override bool InAttackRange() => false;
    protected override void ExecuteAttackHit() { }

    // Animation Event, no início do clipe Die — dispara o crescimento visual da área
    // vermelha (ver BurningSkullExplosionArea.BeginGrowth).
    public void AnimationExplosionGrowStartEvent()
    {
        if (explosionArea != null) explosionArea.BeginGrowth();
    }

    // Animation Event, no frame exato da explosão — dano real em quem estiver dentro do
    // raio. Lê o raio de BurningSkullExplosionArea.FinalRadius (fonte única — visual e
    // dano real nunca ficam dessincronizados).
    public void AnimationExplosionHitEvent()
    {
        if (player == null) return;

        float radius = explosionArea != null ? explosionArea.FinalRadius : 0f;
        if (Vector2.Distance(transform.position, player.position) > radius) return;

        var hero = player.GetComponent<HeroController>();
        if (hero != null) hero.TakeDamage(stats.attackDamage);
    }
}
