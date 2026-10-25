using UnityEngine;

// Divine God (Bestiário Andar 10, boss): o mais simples de todos — zero direção em qualquer
// animação (idle/attack/cast/dmg/die são clipes únicos, sem Blend Tree nenhuma), por isso
// estende EnemyController direto, igual ao Slug/Ambuster. É Melee/Ranged híbrido: persegue
// direto o player (sem flanco, sem kiting) e, ao atacar, o Animator escolhe sozinho entre o
// golpe melee e o cast ranged pelo bool InMeleeRange (dentro de meleeRange = melee, fora =
// cast) — nunca os dois ao mesmo tempo, mesmo cooldown único de ataque de sempre.
//
// O idle dele já É a própria animação de movimento (confirmado no GDD) — por isso o
// Controller só tem um estado "Idle" cobrindo Idle/Walk/IdleCombat ao mesmo tempo (sem
// transição nenhuma entre eles), e MoveInDirection precisa do walkStateName "Idle" (não o
// "Walk" padrão) pra liberar o movimento de verdade.
//
// Cast ranged (5 raios em sequência): o estado CastRay toca um clipe em LOOP
// (Divine_Wrath_Or_Summon) — o Animator não sai dele sozinho, é o código que conta até
// raysPerCast e manda o CastEndTrigger. Cada raio nasce como objeto à parte (DivineRayStrike),
// não uma animação — mesma ideia de objeto independente já usada no Ambuster/Mage, só que
// aqui ele fica parado (não viaja, só espera a própria animação de queda terminar pra checar
// dano, telegraph confirmado com o usuário).
public class DivineGodController : EnemyController
{
    [Header("Faixas de ataque (dentro = melee, fora = ranged)")]
    [SerializeField] private float meleeRange = 2f; // 🔢 ajustável

    [Header("Melee (\"explosão\" do GDD — círculo centrado nele mesmo)")]
    [SerializeField] private Collider2D meleeHitbox;

    [Header("Ranged (5 raios em sequência, 1 por loop do cast)")]
    [SerializeField] private GameObject rayStrikePrefab;
    [SerializeField] private int raysPerCast = 5;
    [SerializeField] private float rayScatterRadius = 2.5f; // 🔢 ajustável — espalhamento ao redor da posição atual do player

    [Header("Dano de contato passivo (camada extra, independente do ciclo acima)")]
    [SerializeField] private float contactDamageCooldown = 1.5f; // 🔢 ajustável

    private int raysFiredThisCast;

    protected override void Awake()
    {
        base.Awake();

        var contactDamage = GetComponent<EnemyContactDamage>();
        if (contactDamage != null) contactDamage.Initialize(stats.attackDamage, contactDamageCooldown);
    }

    protected override void Move(float deltaTime)
    {
        Vector2 toPlayer = player.position - transform.position;
        float distance = toPlayer.magnitude;

        if (animator != null) animator.SetBool("InMeleeRange", distance <= meleeRange);

        if (distance > meleeRange * 0.8f)
        {
            SetMoving(true);
            MoveInDirection(toPlayer.normalized, walkStateName: "Idle");
        }
        else
        {
            SetMoving(false);
        }
    }

    protected override bool InAttackRange() =>
        Vector2.Distance(transform.position, player.position) <= stats.attackRadius;

    // Golpe melee (AnimationHitEvent, 1 trigger só, sem direção).
    protected override void ExecuteAttackHit()
    {
        if (meleeHitbox == null) return;
        var heroCollider = player.GetComponent<Collider2D>();
        if (heroCollider == null) return;
        if (!meleeHitbox.IsTouching(heroCollider)) return;

        var hero = player.GetComponent<HeroController>();
        if (hero != null) hero.TakeDamage(stats.attackDamage);
    }

    // Animation Event, 1x por loop do clipe de cast — dispara raysPerCast vezes em sequência
    // (o clipe fica em loop livre, é esta contagem que decide quando sair dele de verdade).
    public void AnimationCastRayEvent()
    {
        raysFiredThisCast++;

        if (player != null && rayStrikePrefab != null)
        {
            // Posição fresca a cada chamada (não um snapshot único no início do ataque) —
            // cada um dos 5 raios usa a posição ATUAL do player naquele instante + espalhamento.
            Vector2 target = (Vector2)player.position + Random.insideUnitCircle * rayScatterRadius;
            var obj = Instantiate(rayStrikePrefab, target, Quaternion.identity);
            var ray = obj.GetComponent<DivineRayStrike>();
            if (ray != null) ray.Initialize(stats.attackDamage, player);
        }

        if (raysFiredThisCast >= raysPerCast)
        {
            raysFiredThisCast = 0;
            if (animator != null) animator.SetTrigger("CastEndTrigger");
            AnimationAttackEndEvent(); // reaproveita o fim de ataque padrão (libera cooldown etc.)
        }
    }
}
