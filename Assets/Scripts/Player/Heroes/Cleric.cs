using System.Collections.Generic;
using UnityEngine;

public class Cleric : HeroController
{
    // ---------- Ataque primário — Projétil Homing (GDD Seção 17.6) ----------
    [Header("Ataque primário — Projétil Homing")]
    [SerializeField] private GameObject projectilePrefab; // precisa ter ClericProjectile
    [SerializeField] private float attackRadius = 8f; // 🔢 GDD não dá número — conceito novo, nenhum outro herói tem
    [SerializeField] private LayerMask enemyLayerMask; // configurar no Inspector = layer dos monstros

    // Teste do usuário — quantos projéteis o Primário lança por ativação, cada um perseguindo
    // um monstro distinto (os N mais próximos dentro do raio). Começa em 1 (igual à GDD hoje);
    // upgrades futuros da loja podem subir esse número — é só pra validar se vale a pena antes
    // de desenhar a carta de verdade.
    [SerializeField] private int projectileCount = 1; // 🔢 teste
    // Controlador decide TUDO sobre o projétil e repassa pro prefab em cada Launch() — ver
    // comentário no topo do ClericProjectile.cs.
    [SerializeField] private float projectileSpeed = 7f; // 🔢 ajustável — GDD: "upgrades da loja aumentam a velocidade"
    [SerializeField] private float projectileMaxDistance = 12f; // 🔢 ajustável
    [SerializeField] private float projectileKnockbackForce = 2f; // 🔢 ajustável — Cleric não é um herói de empurrão forte
    private readonly List<EnemyController> pendingTargets = new List<EnemyController>();
    private readonly List<EnemyController> prayerTargets = new List<EnemyController>();

    // Rede de segurança genérica — mesmo padrão do resto do elenco.
    [SerializeField] private float maxActionDuration = 3f; // 🔢 ajustável
    private float actionElapsed;

    // ---------- Ultimate — Oração (GDD Seção 17.6) ----------
    // Correção do usuário sobre o texto original da GDD: NÃO é global (Floor inteiro) — é no
    // raio de visão do Cleric, mesma categoria de área que todo outro herói usa.
    [Header("Ultimate — Oração")]
    [SerializeField] private float prayerRadius = 6f; // 🔢 ajustável — "raio de visão do Cleric"
    [SerializeField] private float prayerDuration = 4f; // 🔢 ajustável — duração do Efeito WordOfPain
    // Decisão do usuário: o tick da Oração é SEMPRE metade do dano normal do Cleric, não um
    // valor próprio — ex.: Cleric com 100 de dano aplica 50 por tick. Por isso parte direto de
    // stats.damage, não de um campo fixo tipo prayerTickDamage; e NÃO leva o multiplicador da
    // passiva (ClericDamageMultiplier) — esse só vale pro hit do projétil (ver comentário da
    // passiva abaixo).
    [SerializeField] private float prayerTickDamageMultiplier = 0.5f; // 🔢 ajustável — fração do dano normal aplicada por tick

    [Header("Debug — só pra visualização em Editor, não afeta gameplay")]
    [SerializeField] private bool showAttackRadiusGizmo = false;
    [SerializeField] private float attackRadiusGizmoOffsetY = 0f;
    [SerializeField] private bool showPrayerGizmo = false;
    [SerializeField] private float prayerGizmoOffsetY = 0f;

    private Vector3 AttackRadiusCenter => transform.position + Vector3.up * attackRadiusGizmoOffsetY;
    private Vector3 PrayerCenter => transform.position + Vector3.up * prayerGizmoOffsetY;

    // ---------- Habilidade Secundária (Shift) — Reza / Cura Ativa (GDD Seção 16/17.6) ----------
    // Correção do usuário: a cura total vem em 4 ONDAS (1/4 cada), disparadas pelos 4
    // Animation Events do próprio clipe do ClericShiftEffect (filho dedicado, efeito visual
    // próprio) — não mais 1 evento único no corpo do Cleric. Cada onda também pisca o status
    // "Heal" (StatusEffectController.FlashStatus) por cima de qualquer Efeito Nocivo ativo.
    [Header("Habilidade Secundária (Shift) — Cura Ativa")]
    [SerializeField] private float healPercentOfMaxHealth = 0.25f; // 🔢 GDD: "% da própria Vida Máxima" — TOTAL das 4 ondas somadas
    [SerializeField] private float healFlashDuration = 0.5f; // 🔢 quanto tempo o ícone "Heal" fica por cima do Efeito Nocivo ativo, por onda
    [SerializeField] private ClericShiftEffect shiftEffect; // filho dedicado — Animator próprio, clipe HealStatusCleric

    // ---------- Passiva (GDD Seção 17.6) ----------
    // "Monstros sofrem 2× de dano do Cleric" — aplicado só no hit do projétil (Primário).
    // NÃO se aplica ao tick da Oração (ver prayerTickDamageMultiplier acima) — decisão
    // explícita do usuário, o tick tem a própria regra fixa (metade do dano normal), que já
    // substitui qualquer multiplicador daqui. Não precisa de hook na base — nenhum outro
    // herói tem esse conceito, e o dano do Cleric só é aplicado dentro desta classe e do
    // ClericProjectile.
    private const float ClericDamageMultiplier = 2f;

    protected override void Update()
    {
        if (!GameplayGate.IsActive) return;
        base.Update();

        // Mesma rede de segurança genérica do resto do elenco — se o Animation Event de fim
        // nunca chegar (clipe sem o evento configurado), a ação não trava o Cleric pra sempre.
        if (isAttacking)
        {
            actionElapsed += Time.deltaTime;
            if (actionElapsed >= maxActionDuration)
            {
                Debug.LogWarning("[Cleric] Animation Event de fim de ação nunca chegou — forçando fim (verifique o Animator Controller).");
                isAttacking = false;
            }
        }
    }

    // ===================== PRIMÁRIO — PROJÉTIL HOMING =====================

    // GDD: única exceção do MVP a "todo herói sempre pode tentar atacar" — sem monstro no
    // raio, o clique simplesmente não faz nada, nem toca animação. O cooldown universal já
    // foi consumido pela base antes de chegar aqui (HeroController.Update()) mesmo nesse
    // caso — aceito de propósito (decisão da sprint, zero risco pros outros 9 heróis; mudar
    // isso exigiria mexer no gate compartilhado por todo herói).
    // Hook da base (HeroController) — chamado ANTES do cooldown ser consumido. Já popula
    // pendingTargets aqui mesmo (reaproveitado por PrimaryAttack() logo em seguida, no mesmo
    // frame): sem monstro no raio, devolve false e a base nem chama TryConsume(), então o
    // clique no vazio não gasta o cooldown do Cleric.
    protected override bool ShouldConsumeCooldownOnAttack()
    {
        FindNearestEnemiesInRadius(projectileCount, pendingTargets);
        return pendingTargets.Count > 0;
    }

    protected override void PrimaryAttack()
    {
        if (isAttacking) return;
        if (pendingTargets.Count == 0) return; // exceção do MVP — sem alvo, não acontece nada (já checado por ShouldConsumeCooldownOnAttack)

        isAttacking = true;
        actionElapsed = 0f;
        AnimatorTrigger("AttackTrigger");
    }

    // Os N monstros vivos mais próximos e distintos dentro do raio (N = projectileCount) —
    // mesmo critério de seleção das vinhas do Druid, adaptado pra 1 projétil por alvo em vez
    // de vários por alvo.
    private void FindNearestEnemiesInRadius(int count, List<EnemyController> results)
    {
        results.Clear();

        var hits = Physics2D.OverlapCircleAll(AttackRadiusCenter, attackRadius, enemyLayerMask);
        var candidates = new List<EnemyController>(hits.Length);
        foreach (var hit in hits)
        {
            var enemy = hit.GetComponent<EnemyController>();
            // Mesma trava do Druid (AnimationVineSummonEvent) — um monstro pode ter mais de um
            // collider na layer Enemy, e sem o Contains() ele entraria 2x na lista, roubando a
            // vaga do segundo monstro mais próximo (2 projéteis no mesmo alvo em vez de 1 cada).
            if (enemy == null || candidates.Contains(enemy)) continue;
            if (HealthSystem.IsDead(enemy.stats.health)) continue;

            candidates.Add(enemy);
        }

        candidates.Sort((a, b) => DistanceSqrTo(a).CompareTo(DistanceSqrTo(b)));
        for (int i = 0; i < candidates.Count && i < count; i++) results.Add(candidates[i]);
    }

    private float DistanceSqrTo(EnemyController enemy) =>
        ((Vector2)enemy.transform.position - (Vector2)transform.position).sqrMagnitude;

    // Animation Event, no frame exato em que os projéteis são lançados — 1 por alvo em
    // pendingTargets (ver projectileCount).
    public void AnimationProjectileLaunchEvent()
    {
        if (projectilePrefab == null || pendingTargets.Count == 0) { isAttacking = false; return; }

        foreach (var target in pendingTargets)
        {
            if (target == null) continue; // pode ter morrido entre o clique e o Animation Event

            var projObj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
            var proj = projObj.GetComponent<ClericProjectile>();
            if (proj != null) proj.Launch(target, stats.damage * ClericDamageMultiplier, enemyLayerMask, projectileSpeed, projectileMaxDistance, projectileKnockbackForce);
        }
        pendingTargets.Clear();
    }

    // Animation Event, no fim do clipe de lançamento — os projéteis já estão voando sozinhos
    // (GameObjects independentes), o Cleric recupera o controle na hora (mesmo padrão da
    // bomba do Rogue).
    public void AnimationProjectileEndEvent()
    {
        isAttacking = false;
    }

    // ===================== ULTIMATE — ORAÇÃO =====================

    protected override bool CanUseUltimate() => !isAttacking;

    protected override void UseUltimate()
    {
        isAttacking = true;
        actionElapsed = 0f;
        AnimatorTrigger("PrayerTrigger");
    }

    // Animation Event, no frame exato em que a Oração "dispara" — área no raio de visão do
    // Cleric (correção do usuário: a GDD original dizia "todos os monstros em campo", mas o
    // efeito real é por raio, mesma categoria de toda outra área do projeto). A Oração É o
    // Efeito Nocivo WordOfPain (StatusEffectController) — paraliza (EnemyController consulta
    // IsEffectActive direto) e aplica DoT, os dois ao mesmo tempo, sem GameObject bespoke por
    // alvo.
    public void AnimationPrayerCastEvent()
    {
        var hits = Physics2D.OverlapCircleAll(PrayerCenter, prayerRadius, enemyLayerMask);
        // Dedup obrigatório — mesmo bug do primário do Mage (ver EnemyController.CollectDistinct()).
        // Sem isso, um monstro com 2 Collider2D reaplicava WordOfPain 2x no mesmo cast (não
        // dobra dano por tick, mas reinicia a duração à toa e desperdiça trabalho).
        EnemyController.CollectDistinct(hits, hits.Length, prayerTargets);
        foreach (var enemy in prayerTargets)
        {
            if (HealthSystem.IsDead(enemy.stats.health)) continue;

            var statusEffects = enemy.GetComponent<StatusEffectController>();
            if (statusEffects != null) statusEffects.ApplyStatusEffect(StatusEffectType.WordOfPain, prayerDuration, stats.damage * prayerTickDamageMultiplier);
        }
    }

    // Animation Event, no fim do clipe da Oração — o Efeito WordOfPain já está rodando
    // sozinho em cada monstro atingido (StatusEffectController.Tick(), chamado pelo Update()
    // de cada um deles), o Cleric recupera o controle na hora, independente de quanto tempo o
    // Efeito ainda tem pra durar.
    public void AnimationPrayerEndEvent()
    {
        isAttacking = false;
    }

    // ===================== SECUNDÁRIA (SHIFT) — REZA / CURA ATIVA =====================

    protected override void UseSecondaryAbility()
    {
        isAttacking = true; // bloqueia primário/ultimate durante a reza inteira
        actionElapsed = 0f;

        // Correção: a GDD original dizia "4 direções (N/E/S/W)" cardeal, mas a arte real da
        // Reza é diagonal (NE/NW/SE/SW) — mesmo caso da Cambalhota do Rogue (Sprint 22).
        // RawAimDirection já está fresco aqui: TryUseSecondaryAbility() (base) força a
        // releitura da mira antes de chamar este método.
        Vector2 prayDirection = DirectionUtility.SnapTo4Diagonals(RawAimDirection);
        if (animator != null)
        {
            animator.SetFloat("PrayDirX", prayDirection.x);
            animator.SetFloat("PrayDirY", prayDirection.y);
        }

        AnimatorTrigger("HealTrigger");

        // Toca o efeito visual dedicado (filho ClericShiftEffect) — ele mesmo chama
        // ApplyHealWave() 4x via Animation Event no próprio clipe (ver Seção "Habilidade
        // Secundária" acima). Timeline independente do corpo do Cleric (shift_ne/nw/se/sw).
        if (shiftEffect != null) shiftEffect.Play();
    }

    // Chamado pelo ClericShiftEffect (Animation Event no clipe dele, 4x por ativação) — cada
    // onda cura 1/4 do total (healPercentOfMaxHealth é a soma das 4) e pisca o status
    // "Heal" por cima de qualquer Efeito Nocivo ativo, que volta sozinho ao normal depois.
    public void ApplyHealWave()
    {
        float healAmount = stats.maxHealth * healPercentOfMaxHealth * 0.25f;
        Heal(healAmount);

        var statusEffects = GetComponent<StatusEffectController>();
        if (statusEffects != null) statusEffects.FlashStatus(StatusEffectType.Heal, healFlashDuration);
    }

    // Animation Event, no fim do clipe de reza do CORPO do Cleric (shift_ne/nw/se/sw) — não
    // tem relação com o clipe do ClericShiftEffect, que roda numa timeline própria e pode
    // terminar antes ou depois disso. GDD: "Não cancelável" — CancelSecondaryAbility()
    // default (no-op, herdado sem override) já cobre isso.
    public void AnimationHealEndEvent()
    {
        isAttacking = false;
        isUsingSecondaryAbility = false; // arma o cooldown no próximo Update() da base
    }

    private void OnDrawGizmosSelected()
    {
        if (showAttackRadiusGizmo)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(AttackRadiusCenter, attackRadius);
        }

        if (showPrayerGizmo)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(PrayerCenter, prayerRadius);
        }
    }
}
