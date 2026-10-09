using System.Collections.Generic;
using UnityEngine;

public class Rogue : HeroController
{
    // ---------- Ataque primário — Self Area Pulse (GDD Seção 17.5) ----------
    [Header("Ataque primário — Self Area Pulse")]
    [SerializeField] private float pulseRadius = 2f; // 🔢 GDD não dá número — ajustável
    [SerializeField] private LayerMask enemyLayerMask; // configurar no Inspector = layer dos monstros (mesmo padrão do Druid)
    [SerializeField] private float pulseKnockbackForce = 4f; // 🔢 mesmo valor/padrão do knockbackForce do Barbarian — mesma categoria "Self Area Pulse"
    private bool pulseHitFired;
    private readonly List<EnemyController> pulseTargets = new();

    // Debug — mesmo padrão da Vine/RogueBomb/EnemyController: NÃO é um trigger de verdade, é
    // só pra visualizar/posicionar o raio do OverlapCircleAll no Editor sem precisar rodar em
    // Play Mode. O dano continua filtrado por enemyLayerMask acima (só a layer de monstro
    // entra no cálculo, nunca "qualquer collider").
    [Header("Debug — só pra visualização em Editor, não afeta gameplay")]
    [SerializeField] private bool showPulseGizmo = false;
    [SerializeField] private float pulseGizmoOffsetY = 0f;

    private Vector3 PulseCenter => transform.position + Vector3.up * pulseGizmoOffsetY;

    // Rede de segurança genérica — mesmo padrão do Barbarian/Ranger/Druid. Cobre Primário e
    // Ultimate (ações curtas, sem fase "during"); a Cambalhota tem teto próprio (ver
    // rollMaxSafetyDuration), suprimido daqui.
    [SerializeField] private float maxActionDuration = 3f; // 🔢 ajustável
    private float actionElapsed;

    // ---------- Ultimate — Bomba (GDD Seção 17.5) ----------
    [Header("Ultimate — Bomba")]
    [SerializeField] private GameObject bombPrefab; // precisa ter RogueBomb
    [SerializeField] private float bombDamageMultiplier = 4f; // GDD: "4× o dano"
    // Controlador decide TUDO sobre a bomba e repassa pro prefab em cada Launch() — ver
    // comentário no topo do RogueBomb.cs.
    [SerializeField] private float bombSpeed = 8f; // 🔢 ajustável
    [SerializeField] private float bombMaxDistance = 10f; // 🔢 ajustável — não escala por Tier de Arma ainda (mesmo placeholder do vineCount/arrowCount)
    [SerializeField] private float bombExplosionRadius = 2.5f; // 🔢 ajustável — maior que o Pulso, é a Ultimate

    // ---------- Habilidade Secundária (Shift) — Cambalhota (GDD Seção 16/17.5) ----------
    // Movimento livre (segue a mira de verdade, RawAimDirection) — decisão explícita do
    // usuário: a arte tem 4 poses fixas, mas em DIAGONAL (NE/NW/SE/SW — correção Sprint 22,
    // visual real funciona melhor assim que o N/E/S/W originalmente previsto no GDD), e a
    // TRAJETÓRIA não precisa ficar presa a elas. RollAimX/Y (abaixo) é só o par travado em
    // diagonal (SnapTo4Diagonals, mesma fórmula do Idle/Walk/Dmg) pra escolher qual das 4
    // poses tocar, igual o TeleportAimX/Y do Mage — não influencia o deslocamento em si.
    [Header("Habilidade Secundária (Shift) — Cambalhota")]
    [SerializeField] private float rollSpeed = 12f; // 🔢 ajustável — mais rápido que moveSpeed normal, é um dash
    [SerializeField] private float rollKnockbackForce = 5f; // 🔢 ajustável, mesma escala do elkKnockbackForce do Druid
    [SerializeField] private float rollHitRadius = 0.8f; // 🔢 ajustável — raio de detecção de monstros ao longo do trajeto
    [SerializeField] private float rollMaxSafetyDuration = 1f; // 🔢 só rede de segurança — a duração real é o clipe "Roll"
    private bool isRolling;
    private float rollSafetyElapsed;
    private Vector2 rollDirection;
    private readonly HashSet<EnemyController> rolledEnemiesThisActivation = new HashSet<EnemyController>();

    protected override void Update()
    {
        if (!GameplayGate.IsActive) return;

        base.Update();

        // Suprimido durante a Cambalhota — ela tem o próprio teto de segurança (ver
        // UpdateRoll), mesmo raciocínio do "if (isAttacking && !isOwlForm && !isElkForm)" do Druid.
        if (isAttacking && !isRolling)
        {
            actionElapsed += Time.deltaTime;
            if (actionElapsed >= maxActionDuration)
            {
                Debug.LogWarning("[Rogue] Animation Event de fim de ação nunca chegou — forçando fim (verifique o Animator Controller).");
                isAttacking = false;
            }
        }

        if (isRolling) UpdateRoll();
    }

    // ===================== PRIMÁRIO — SELF AREA PULSE =====================

    protected override void PrimaryAttack()
    {
        if (isAttacking) return;

        isAttacking = true;
        actionElapsed = 0f;
        pulseHitFired = false;
        AnimatorTrigger("AttackTrigger");
    }

    // Animation Event, no frame exato em que o pulso se expande — dispara 1x (GDD: "1x por
    // Animation Event"), independente de quantos clipes diagonais estejam misturados no Blend
    // Tree (mesma proteção attackHitFired que Barbarian/Druid/EnemyController já usam: o
    // Animator dispara o evento de todo clipe com peso > 0 na mistura, não só o dominante).
    public void AnimationPulseHitEvent()
    {
        if (pulseHitFired) return;
        pulseHitFired = true;

        var hits = Physics2D.OverlapCircleAll(PulseCenter, pulseRadius, enemyLayerMask);
        // Dedup obrigatório — todo monstro tem 2 Collider2D no mesmo GameObject (corpo +
        // trigger genérico), então OverlapCircleAll sem isso acertava o mesmo monstro 2x
        // (mesmo bug real já corrigido no primário do Mage — ver EnemyController.CollectDistinct()).
        // TakeDamage() já é seguro contra corpo já morto, sem checagem extra aqui.
        EnemyController.CollectDistinct(hits, hits.Length, pulseTargets);
        foreach (var enemy in pulseTargets)
        {
            enemy.TakeDamage(stats.damage);
            enemy.ApplyKnockback(AimDirection, pulseKnockbackForce);
        }
    }

    // Animation Event, no fim do clipe do pulso.
    public void AnimationPulseEndEvent()
    {
        isAttacking = false;
    }

    // ===================== ULTIMATE — BOMBA =====================

    // Sem isso, dava pra disparar a bomba no meio do Pulso ou da Cambalhota.
    protected override bool CanUseUltimate() => !isAttacking;

    protected override void UseUltimate()
    {
        isAttacking = true;
        actionElapsed = 0f;
        AnimatorTrigger("UltimateTrigger");
    }

    // Animation Event, no frame exato em que a bomba é lançada — nasce na posição do Rogue e
    // viaja na direção REAL da mira (RawAimDirection, não o AimDirection snapado em 8 — mesmo
    // motivo do Ranger/Mage usarem a direção crua pra trajetória, só a pose do Animator usa o
    // valor snapado). RawAimDirection já está fresco aqui: TryUseUltimate() (base) força a
    // releitura da mira antes de chamar UseUltimate().
    public void AnimationBombLaunchEvent()
    {
        if (bombPrefab == null) return;

        var bombObj = Instantiate(bombPrefab, transform.position, Quaternion.identity);
        var bomb = bombObj.GetComponent<RogueBomb>();
        if (bomb != null) bomb.Launch(RawAimDirection, stats.damage * bombDamageMultiplier, enemyLayerMask, bombSpeed, bombMaxDistance, bombExplosionRadius);
    }

    // Animation Event, no fim do clipe de lançar a bomba — a bomba já está viajando sozinha
    // (GameObject independente, mesmo padrão do GoblinSapperBomb/EnemyProjectile); o Rogue
    // recupera o controle assim que a animação de LANÇAR termina, não quando a bomba explode
    // (os dois são objetos/timelines diferentes, igual o Ranger não espera a flecha aterrissar).
    public void AnimationBombEndEvent()
    {
        isAttacking = false;
    }

    // ===================== SECUNDÁRIA (SHIFT) — CAMBALHOTA =====================

    protected override void UseSecondaryAbility()
    {
        // Bloqueia primário/ultimate durante a Cambalhota inteira — mesmo padrão da Coruja do
        // Druid ("isAttacking fica true a viagem inteira").
        isAttacking = true;
        isRolling = true;
        rollSafetyElapsed = 0f;
        rolledEnemiesThisActivation.Clear();

        // Movimento livre — decisão explícita do usuário (Sprint 22): a Cambalhota segue a
        // mira de verdade, não trava numa das 4 diagonais/cardeais. RawAimDirection já reflete
        // a releitura forçada que TryUseSecondaryAbility() (base) faz antes de chamar este
        // método.
        rollDirection = RawAimDirection;

        // Sem colisão física com monstro durante a Cambalhota inteira (não só imune a dano) —
        // mesmo padrão da Coruja do Druid: IgnoreLayerCollision em vez de desligar o collider
        // inteiro, pra não perder colisão com escada/parede (outra layer). Religado só no fim
        // de verdade, em EndRoll().
        Physics2D.IgnoreLayerCollision(gameObject.layer, LayerMask.NameToLayer("Enemy"), true);

        // Pose do Animator, sim, trava numa das 4 diagonais — é tudo que a arte tem (correção
        // Sprint 22, ver comentário no topo da classe). Par próprio (RollAimX/Y), congelado só
        // nesse instante, mesmo critério do TeleportAimX/Y do Mage — não influencia
        // rollDirection acima.
        if (animator != null)
        {
            Vector2 pose = DirectionUtility.SnapTo4Diagonals(rollDirection);
            animator.SetFloat("RollAimX", pose.x);
            animator.SetFloat("RollAimY", pose.y);
        }

        AnimatorTrigger("RollTrigger");
    }

    private void UpdateRoll()
    {
        // Só desloca de verdade quando o Animator já confirmou o estado "Roll" — nunca no
        // frame em que só a intenção foi marcada (mesmo critério do "Walk" na base e do
        // "Walk_Bomb" do Goblin Sapper).
        bool canRoll = AnimatorStateCheck.IsInState(animator, "Roll");
        if (canRoll)
        {
            transform.Translate(rollDirection * rollSpeed * Time.deltaTime);

            // Empurra (sem dano) cada monstro no caminho 1x só por ativação; sem o HashSet, um
            // monstro parado no trajeto levaria um knockback por frame.
            var hits = Physics2D.OverlapCircleAll(transform.position, rollHitRadius, enemyLayerMask);
            foreach (var hit in hits)
            {
                var enemy = hit.GetComponent<EnemyController>();
                if (enemy == null || rolledEnemiesThisActivation.Contains(enemy)) continue;
                rolledEnemiesThisActivation.Add(enemy);
                enemy.ApplyKnockback(rollDirection, rollKnockbackForce);
            }
        }

        // Rede de segurança — se AnimationRollEndEvent nunca chegar, força o fim depois desse
        // tempo (mesmo padrão de maxDieDuration/owlDuration/elkFormMaxDuration).
        rollSafetyElapsed += Time.deltaTime;
        if (rollSafetyElapsed >= rollMaxSafetyDuration)
        {
            Debug.LogWarning("[Rogue] AnimationRollEndEvent nunca chegou — forçando fim (verifique o Animator Controller).");
            EndRoll();
        }
    }

    // Animation Event, no último frame do clipe "Roll" — fonte de verdade do fim da Cambalhota.
    // rollMaxSafetyDuration acima só existe pro caso desse evento faltar.
    public void AnimationRollEndEvent()
    {
        EndRoll();
    }

    private void EndRoll()
    {
        if (!isRolling) return; // evita chamar 2x (evento real + timeout de segurança)
        isRolling = false;
        isAttacking = false;
        isUsingSecondaryAbility = false; // arma o cooldown no próximo Update() da base
        Physics2D.IgnoreLayerCollision(gameObject.layer, LayerMask.NameToLayer("Enemy"), false);
    }

    // IsDamageImmune vale desde o instante em que isRolling vira true (UseSecondaryAbility),
    // não só a partir de algum Animation Event — a Cambalhota é imune a dano a viagem
    // inteira, por isso TakeDamage() nem chega a tirar vida do Rogue enquanto ela dura (ver
    // HeroController.TakeDamage: "if (IsDamageImmune) return;" acontece ANTES de qualquer
    // outra coisa). Isso também é por que a morte "durante a Cambalhota" não acontece pela
    // via normal de dano hoje — só é alcançável por uma fonte futura que ignore essa trava.
    protected override bool IsDamageImmune => isRolling;

    // Mesmo precedente do Alce do Druid (isElkForm) — sem isso, morrer no meio da Cambalhota
    // deixaria isRolling travado em true pra sempre. Chama EndRoll() (não só "isRolling =
    // false" direto) porque o estado da Cambalhota não é só a flag: IgnoreLayerCollision
    // ficou ligado lá em UseSecondaryAbility() e só EndRoll() desliga de volta — sem isso, o
    // Rogue morreria sem colisão física com monstro nenhum e continuaria assim pra sempre
    // depois do respawn (a flag reseta, a layer collision não). O Animator não precisa de
    // ajuda aqui: DieTrigger já é uma transição Any State (sem Exit Time) no Rogue.controller,
    // então interrompe o clipe "Roll" na hora, de qualquer estado, sem configuração extra.
    protected override void OnHeroDeath()
    {
        EndRoll();
    }

    // Passiva (GDD Seção 17.5) — "4× mais Energia de Ultimate por kill".
    protected override float UltimateEnergyMultiplier => 4f;

    private void OnDrawGizmosSelected()
    {
        if (!showPulseGizmo) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(PulseCenter, pulseRadius);
    }
}
