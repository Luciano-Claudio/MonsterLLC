using System.Collections.Generic;
using UnityEngine;

// Lich (Bestiário Andar 5) — Ranged especial: sorteia entre 2 conjurações no MESMO
// cooldown (não dispara projétil nenhum, ver RangedEnemyController.ExecuteAttackHit abaixo,
// nunca chamado) — Ice Claws (área no chão, dano + lentidão, ver GroundTargetHazard) ou
// Reanimation (invoca 1 monstro aleatório da lista, perto dele). Reaproveita 100% o
// Move()/kiting do RangedEnemyController quando não está conjurando; durante a conjuração
// fica parado (igual qualquer outro cast do jogo). "Após a morte do Lich, os summons dele
// também morrem" — guarda a lista de quem invocou e mata todos no próprio Die().
public class LichController : RangedEnemyController
{
    [Header("Conjuração (1 cooldown só — sorteia Ice Claws ou Reanimation a cada vez)")]
    [SerializeField] private float attackCooldownDuration = 4f; // 🔢 ajustável

    [Header("Ice Claws — nasce embaixo do player, igual GroundCaster")]
    [SerializeField] private GameObject iceHazardPrefab;

    [Header("Reanimation — sorteia 1 prefab da lista, nasce perto do Lich")]
    [SerializeField] private GameObject[] reanimationPrefabs;
    [SerializeField] private float reanimationSpawnRadius = 2f; // 🔢 ajustável

    private AttackCooldown attackCooldown;
    private bool isCasting;
    private readonly List<EnemyController> summons = new();

    // Bug real (2026-11-15, mesmo achado da Spider Queen/Rat People Royalty): EnemyController.
    // TakeDamage() dispara "DamageTrigger" (Any State, SEM Exit Time) sempre que o boss leva
    // dano fora do pipeline genérico de Attack (que o Lich nunca usa, InAttackRange()=>false
    // abaixo). Se o player acerta bem no meio da conjuração, o Animator salta pro estado
    // Damage ANTES do Animation Event de fim (AnimationCastEndEvent) disparar, e isCasting
    // nunca mais volta a false — Move() trava parado pra sempre. Mesmo failsafe que o pipeline
    // genérico já usa (EnemyController.maxAttackAnimationDuration). Nome diferente de propósito
    // (não "attackAnimElapsed") — a base EnemyController já tem um campo privado com esse nome
    // exato, e Unity não aceita 2 campos com o mesmo nome na cadeia de herança ("serialized
    // multiple times", erro real visto no Console).
    private float customAttackElapsed;

    protected override void Awake()
    {
        base.Awake();
        attackCooldown = new AttackCooldown(attackCooldownDuration);
    }

    protected override void Move(float deltaTime)
    {
        if (isCasting)
        {
            SetMoving(false); // parado conjurando, igual qualquer outro cast do jogo

            // Failsafe (ver comentário na declaração de customAttackElapsed) — sem isso, um hit
            // que interrompe a conjuração no meio prende o Lich parado pra sempre, já que o
            // Animation Event de fim nunca chega.
            customAttackElapsed += deltaTime;
            if (customAttackElapsed >= maxAttackAnimationDuration)
            {
                Debug.LogWarning($"[{GetType().Name}] AnimationCastEndEvent nunca chegou — forçando fim da conjuração (verifique o Animator Controller).");
                isCasting = false;
            }
            return;
        }

        base.Move(deltaTime); // mantém distância do player (kiting padrão do Ranged)

        if (Vector2.Distance(transform.position, player.position) > stats.attackRadius) return;

        attackCooldown.Tick(deltaTime);
        if (attackCooldown.TryConsume()) StartRandomCast();
    }

    private void StartRandomCast()
    {
        isCasting = true;
        customAttackElapsed = 0f;
        SetMoving(false);
        if (animator == null) return;
        if (Random.value < 0.5f) animator.SetTrigger("IceClawsTrigger");
        else animator.SetTrigger("ReanimationTrigger");
    }

    // Animation Event, no frame certo do clipe Spellcast_ice_claws (4 direções).
    public void AnimationIceClawsEvent()
    {
        if (iceHazardPrefab == null || player == null) return;
        Instantiate(iceHazardPrefab, player.position, Quaternion.identity, EnemyController.ContainerFor(IsBoss));
    }

    // Animation Event, no frame certo do clipe Spellcast_reanimation (4 direções).
    public void AnimationReanimationEvent()
    {
        if (reanimationPrefabs == null || reanimationPrefabs.Length == 0) return;

        var prefab = reanimationPrefabs[Random.Range(0, reanimationPrefabs.Length)];
        if (prefab == null) return;

        Vector2 spawnPos = (Vector2)transform.position + Random.insideUnitCircle * reanimationSpawnRadius;
        var obj = Instantiate(prefab, spawnPos, Quaternion.identity, EnemyController.ContainerFor(IsBoss));
        var enemy = obj.GetComponent<EnemyController>();
        if (enemy == null) return;

        enemy.dropsLoot = false; // Bestiário (Rat People Royalty, mesmo critério): summon não dropa loot
        // Pedido do usuário (2026-10-26): o reanimado já nasce sabendo do player, sem esperar
        // observationRadius — mesmo critério de todo summon "que nasce na cara do player" do
        // projeto (Rat People arremessado, forma possuída do Dark Channeler).
        enemy.ForceImmediateAggro();
        summons.Add(enemy);
    }

    // Animation Event, no fim de QUALQUER UM dos 2 clipes de conjuração.
    public void AnimationCastEndEvent()
    {
        isCasting = false;
    }

    protected override void Die()
    {
        foreach (var summon in summons)
        {
            if (summon != null) summon.TakeDamage(float.MaxValue, isDirectPlayerHit: false);
        }
        base.Die();
    }

    // Pipeline genérico de Attack nunca é usado — as 2 conjurações são tratadas inteiramente
    // em Move()/StartRandomCast() acima, cada uma com seu próprio Animation Event.
    protected override bool InAttackRange() => false;
    protected override void ExecuteAttackHit() { }
}
