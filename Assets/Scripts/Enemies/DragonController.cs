using UnityEngine;

// Dragon/Undead Dragon (Bestiário Andar 7 — exceção à regra padrão): Melee comum em
// alcance/triggers (4 circles diagonais, igual a qualquer outro Melee), mas o golpe não é
// um swing único — é uma mordida contínua em 3 fases. attack_start conecta o primeiro
// golpe (Animation Event no ÚLTIMO frame, igual a qualquer Melee — ver
// EnemyController.AnimationHitEvent/ExecuteAttackHit) e liga o dano contínuo; o Animator
// então entra em attack_continuous (loop) por conta própria (Exit Time=1, sem condição) —
// enquanto o player continuar tocando o mesmo trigger, continua mordendo (tick a cada
// continuousDamageTickInterval) e, a cada ciclo do loop, solta fogo no chão na posição do
// próprio trigger ativo (GroundFireHazard, dot em quem passar por cima). No instante em que
// o player sai do trigger (ou morre/fica imune), dispara AttackEndTrigger — o Animator vai
// pra attack_end, cujo 1º frame tem outro Animation Event (AnimationAttackContinuousEndEvent)
// que fecha de vez a contabilidade do ciclo contínuo (o dano em si já parou no Update()
// abaixo, assim que detectou a saída — esse evento só confirma o fim, não é quem decide).
public class DragonController : MeleeEnemyController
{
    [Header("Dano contínuo (mordida) enquanto o player não sair do trigger")]
    [SerializeField] private float continuousDamageTickInterval = 1f; // 🔢 ajustável

    [Header("Fogo no chão (nasce a cada ciclo do attack_continuous)")]
    [SerializeField] private GameObject fireHazardPrefab;

    private AttackCooldown continuousDamageTick;
    private bool isContinuousAttacking;
    private bool attackEndRequested;

    protected override void Awake()
    {
        base.Awake();
        continuousDamageTick = new AttackCooldown(continuousDamageTickInterval);
    }

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

        isContinuousAttacking = true;
        attackEndRequested = false;
        continuousDamageTick.Start(); // próximo tick só depois de 1 intervalo inteiro, não repete esse hit
    }

    protected override void Update()
    {
        base.Update();
        if (!isContinuousAttacking || IsDead || !GameplayGate.IsActive) return;

        var hitbox = GetHitboxForFacing();
        var heroCollider = player != null ? player.GetComponent<Collider2D>() : null;
        var hero = player != null ? player.GetComponent<HeroController>() : null;

        bool stillTouching = hitbox != null && heroCollider != null && hitbox.IsTouching(heroCollider);
        bool heroGone = hero == null || hero.IsCurrentlyDamageImmune;

        if (!stillTouching || heroGone)
        {
            if (!attackEndRequested)
            {
                attackEndRequested = true;
                if (animator != null) animator.SetTrigger("AttackEndTrigger");
            }
            return; // sem mais dano enquanto espera o attack_end confirmar o fim
        }

        continuousDamageTick.Tick(Time.deltaTime);
        if (continuousDamageTick.TryConsume())
            hero.TakeDamage(stats.attackDamage);
    }

    // Animation Event, 1º frame do attack_end — fecha de vez o ciclo contínuo (o Update()
    // acima já tinha parado de aplicar dano desde que detectou a saída do trigger).
    public void AnimationAttackContinuousEndEvent()
    {
        isContinuousAttacking = false;
        attackEndRequested = false;
    }

    // Animation Event dentro do loop do attack_continuous — solta fogo no chão na posição
    // do próprio trigger ativo (mesmo critério de posição já usado no Spectre).
    public void AnimationFireHazardSpawnEvent()
    {
        var hitbox = GetHitboxForFacing();
        if (hitbox == null || fireHazardPrefab == null) return;
        Instantiate(fireHazardPrefab, hitbox.transform.position, Quaternion.identity);
    }
}
