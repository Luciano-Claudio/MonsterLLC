using System.Collections.Generic;
using UnityEngine;

// GameObject filho do Mage (girado pelo Mage.cs no Update(), não por si próprio) — o fogo do
// ataque primário. Não viaja (ao contrário de HeroProjectile/RangerArrow): é sempre a mesma
// instância, grudada no Mage e reaproveitada a cada ataque.
//
// D9.1 (2026-11-19, decisão do usuário) — o fogo tem 3 partes no próprio Animator:
// Fire_Start (frames 0-3) -> Fire_Continuous (4-8, loop) -> Fire_End (9-15) -> Idle.
// O Mage acende (StartFire) no frame 1 do attack_continuous do corpo e apaga (EndFire) no
// frame 7 da volta em que a duração acabou. O dano só existe enquanto o fogo está no
// Fire_Continuous, por tick POR MONSTRO (PerTargetTickTracker): quem entra leva dano na hora
// e de novo a cada tickInterval enquanto continuar dentro.
public class MageAttackHitbox : MonoBehaviour
{
    private static readonly int FireContinuousHash = Animator.StringToHash("Fire_Continuous");

    [SerializeField] private Collider2D hitbox;
    private Animator animator;

    private bool fireActive; // entre StartFire() e EndFire() (fim da duração ou ação cortada)
    private System.Func<float> rollTickDamage; // dano de 1 tick (crit rolado por tick) — fornecido pelo Mage
    private readonly PerTargetTickTracker tickTracker = new PerTargetTickTracker(0.25f);
    private readonly List<EnemyController> targets = new();
    // 128 (não 16) — monstro tem 2 Collider2D; 16 saturava com só 8 monstros aglomerados
    // (bug real, 2026-10-26).
    private static readonly Collider2D[] OverlapBuffer = new Collider2D[128];
    private float nextPruneTime;
    private const float PruneInterval = 1f;

    public bool IsFireActive => fireActive;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    // actionSpeedMultiplier vem do Mage (upgrade de velocidade de ação) — Animator próprio,
    // separado do corpo do Mage, então o SetFloat do HeroController não chega aqui sozinho.
    // Idempotente: o frame 1 do attack_continuous dispara de novo a cada volta, mas o fogo só
    // acende uma vez por ataque.
    // Etapa 4 — Mage Immolation: com fireStatusDps, cada tick que acerta também aplica o status Fire
    // (dps do Fire, duração). Nulo = a chama só dá o dano do tick (Phoenix Pact).
    private System.Func<float> fireStatusDps;
    private float fireStatusDuration;
    // Catálogo de cartas — peso de 1 tick dentro do ciclo da animação (intervalo / ciclo): efeitos "por
    // acerto" valem esta fração por tick, e o ciclo inteiro soma 1 acerto.
    private float tickHitWeight = 1f;
    public float TickHitWeight { get => tickHitWeight; set => tickHitWeight = value; }

    public void StartFire(System.Func<float> tickDamage, float tickInterval, float actionSpeedMultiplier,
        System.Func<float> fireStatusDps = null, float fireStatusDuration = 0f)
    {
        if (fireActive) return;
        fireActive = true;
        rollTickDamage = tickDamage;
        this.fireStatusDps = fireStatusDps;
        this.fireStatusDuration = fireStatusDuration;
        tickTracker.SetInterval(tickInterval);
        if (animator == null) return;
        animator.SetFloat("ActionSpeedMultiplier", actionSpeedMultiplier);
        animator.ResetTrigger("FireEndTrigger");
        animator.SetTrigger("FireStartTrigger");
    }

    // Fim normal (duração acabou): toca o Fire_End. O dano para no mesmo instante — só o
    // Fire_Continuous causa dano.
    public void EndFire()
    {
        if (!fireActive) return;
        fireActive = false;
        if (animator == null) return;
        animator.ResetTrigger("FireStartTrigger");
        animator.SetTrigger("FireEndTrigger");
    }

    private void Update()
    {
        if (!fireActive || hitbox == null || !GameplayGate.IsActive) return;
        if (animator != null && animator.GetCurrentAnimatorStateInfo(0).shortNameHash != FireContinuousHash) return;

        int count = hitbox.Overlap(ContactFilter2D.noFilter, OverlapBuffer);
        // Dedup obrigatório — todo monstro tem 2 Collider2D (ver EnemyController.CollectDistinct()).
        EnemyController.CollectDistinct(OverlapBuffer, count, targets);

        float now = Time.time;
        foreach (var enemy in targets)
        {
            if (enemy == null || enemy.IsDead) continue;
            if (!tickTracker.TryHit(enemy.GetInstanceID(), now)) continue;
            ApplyTickTo(enemy);
        }

        if (now >= nextPruneTime)
        {
            nextPruneTime = now + PruneInterval;
            tickTracker.Prune(now);
        }
    }

    // 1 tick da chama num inimigo (extraído do Update pra ser testável fora do Play).
    public void ApplyTickTo(EnemyController enemy)
    {
        if (enemy == null || enemy.IsDead) return;
        // Tick repetido: sem shake/zoom (isDirectPlayerHit false, mesmo critério do pulse
        // do Rogue e das áreas "grounded") — só o filtro de juice muda, o dano não.
        HeroDamage.Direct(enemy, rollTickDamage != null ? rollTickDamage() : 0f, CardAbility.Primary, tickHitWeight, juice: false, flameTick: true); // catálogo de cartas: ponto único
        if (fireStatusDps == null || enemy.IsDead) return;
        var status = enemy.GetComponent<StatusEffectController>();
        if (status != null) status.ApplyStatusEffect(StatusEffectType.Fire, fireStatusDuration, fireStatusDps());
    }
}
