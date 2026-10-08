using UnityEngine;

// Spider Queen (Bestiário — exceção pontual, Melee/Ranged híbrido de verdade). 2 raios de
// ataque independentes, igual todo Melee/Ranged comum já tem o próprio attackRadius: dentro
// de stats.attackRadius (melee) se comporta exatamente como uma Spider comum, ataque corpo a
// corpo real (bite, 4 direções diagonais, hitbox + AnimationBiteEvent); entre o attackRadius e
// o rangedAttackRadius, dispara a teia à distância (attack, 8 direções, projétil real nascido
// via AnimationWebEvent); além do rangedAttackRadius, nenhum dos dois alcança — só persegue.
// Diferente de um RangedEnemyController comum, NUNCA foge: sempre tenta voltar a fechar
// distância pro melee (Move() só aproxima ou fica parada, nunca afasta).
// Não usa o pipeline genérico de Attack/AttackTrigger da base (hasAttackAnimation deve ficar
// FALSE no Inspector) porque esse pipeline só suporta 1 tipo de ataque por monstro — aqui são
// 2, escolhidos por distância a cada frame, cada um com seu próprio trigger/cooldown/evento,
// tratados inteiramente em Move() (mesmo padrão arquitetural do Goblin Sapper/Rat People
// Royalty).
public class SpiderQueenController : EnemyController
{
    [Header("Bite (corpo a corpo — 4 direções diagonais)")]
    [SerializeField] private Collider2D attackHitboxNE;
    [SerializeField] private Collider2D attackHitboxNW;
    [SerializeField] private Collider2D attackHitboxSE;
    [SerializeField] private Collider2D attackHitboxSW;
    [SerializeField] private float biteCooldownDuration = 1.5f; // 🔢 ajustável

    [Header("Teia (à distância — 8 direções)")]
    [SerializeField] private GameObject webProjectilePrefab; // precisa ter SpiderWebProjectile
    [SerializeField] private float webCooldownDuration = 4f; // 🔢 ajustável
    [SerializeField] private float rangedAttackRadius = 6f; // 🔢 ajustável — além disso, nem a teia alcança, só persegue

    private AttackCooldown biteCooldown;
    private AttackCooldown webCooldown;
    private bool isAttacking; // true durante bite OU teia — trava movimento, mesmo critério de "ação real" usado em toda outra classe do projeto

    protected override void Awake()
    {
        base.Awake();
        biteCooldown = new AttackCooldown(biteCooldownDuration);
        webCooldown = new AttackCooldown(webCooldownDuration);
    }

    protected override void Move()
    {
        if (isAttacking)
        {
            SetMoving(false);
            return;
        }

        Vector2 toPlayer = player.position - transform.position;
        float distance = toPlayer.magnitude;

        if (distance <= stats.attackRadius)
        {
            // Colada no player -> idle_combat, tenta mordida pelo cooldown próprio.
            SetMoving(false);
            biteCooldown.Tick(Time.deltaTime);
            if (biteCooldown.TryConsume()) StartBite();
            return;
        }

        // Fora de alcance de mordida -> SEMPRE aproxima (nunca foge, diferente de um Ranged
        // comum). Só tenta a teia se também estiver dentro do alcance dela — além do
        // rangedAttackRadius, nem o Ranged mais comum do jogo alcançaria, então a Spider Queen
        // também não.
        SetMoving(true);
        MoveInDirection(toPlayer.normalized);
        if (distance <= rangedAttackRadius)
        {
            webCooldown.Tick(Time.deltaTime);
            if (webCooldown.TryConsume()) StartWeb();
        }
    }

    private void StartBite()
    {
        isAttacking = true;
        SetMoving(false);
        if (animator != null) animator.SetTrigger("BiteTrigger");
    }

    private void StartWeb()
    {
        isAttacking = true;
        SetMoving(false);
        if (animator != null) animator.SetTrigger("WebTrigger");
    }

    // Animation Event, no frame exato em que a mordida conecta (clipes bite_ne/nw/se/sw).
    public void AnimationBiteEvent()
    {
        var hitbox = GetHitboxForFacing();
        if (hitbox == null) return;

        var heroCollider = player.GetComponent<Collider2D>();
        if (heroCollider == null) return;
        if (!hitbox.IsTouching(heroCollider)) return; // player fora do trigger nesse frame exato -> o golpe erra

        var hero = player.GetComponent<HeroController>();
        if (hero != null) hero.TakeDamage(stats.attackDamage);
    }

    // Animation Event, no fim do clipe de mordida.
    public void AnimationBiteEndEvent()
    {
        isAttacking = false;
    }

    // Animation Event, no frame exato em que a teia nasce (clipes attack_n/ne/e/se/s/sw/w/nw).
    public void AnimationWebEvent()
    {
        if (webProjectilePrefab == null || player == null) return;

        Vector2 dir = ((Vector2)player.position - (Vector2)transform.position).normalized;
        var obj = Instantiate(webProjectilePrefab, transform.position, Quaternion.identity);
        var web = obj.GetComponent<SpiderWebProjectile>();
        if (web != null) web.Launch(dir, ownerFloor);
    }

    // Animation Event, no fim do clipe de teia.
    public void AnimationWebEndEvent()
    {
        isAttacking = false;
    }

    // Classifica AimDirection (sempre em direção ao player) num dos 4 quadrantes diagonais —
    // mesmo critério do MeleeEnemyController, pra escolher a hitbox certa da mordida.
    private Collider2D GetHitboxForFacing()
    {
        bool east = AimDirection.x >= 0f;
        bool north = AimDirection.y >= 0f;

        if (north) return east ? attackHitboxNE : attackHitboxNW;
        return east ? attackHitboxSE : attackHitboxSW;
    }

    // Pipeline genérico de Attack nunca é usado — bite e teia são tratados inteiramente em
    // Move()/StartBite()/StartWeb() acima, igual ao Goblin Sapper/Rat People Royalty.
    protected override bool InAttackRange() => false;
    protected override void ExecuteAttackHit() { }
}
