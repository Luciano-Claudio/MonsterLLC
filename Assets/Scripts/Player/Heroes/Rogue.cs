using System.Collections.Generic;
using UnityEngine;

public class Rogue : HeroController
{
    // ---------- Ataque primário — Self Area Pulse (GDD Seção 17.5) ----------
    // Redesenho a pedido do usuário: 3 estágios. 1) attack_start (corpo do Rogue, 4 diagonais)
    // — no fim dele, ativa 2) RoguePulseArea (filho FIXO, nunca instanciado — ver comentário no
    // topo do próprio script): fica tocando "Cycle" em loop (igual à Ultimate do Paladin) e
    // dando dano por segundo via o próprio CircleCollider2D (trigger, não mais OverlapCircleAll
    // no próprio Rogue). O Rogue fica LIVRE pra se mover durante essa fase (isAttacking já volta
    // a false no fim do attack_start — decisão explícita do usuário) — como a área é filha dele,
    // ela segue onde ele for. 3) Quando pulseDuration esgota (ver Update()), desativa a área e o
    // Rogue toca attack_end (corpo, 4 diagonais) — só aí trava de novo brevemente.
    [Header("Ataque primário — Self Area Pulse (filho RoguePulseArea cuida do dano/raio real)")]
    [SerializeField] private LayerMask enemyLayerMask; // configurar no Inspector = layer dos monstros (mesmo padrão do Druid) — usado pela Ultimate (bomba), não mais pelo pulso (RoguePulseArea filtra por tag "Enemy")
    [SerializeField] private RoguePulseArea pulseArea; // filho fixo, já no prefab — raio/forma do trigger configurados nele
    [SerializeField] private float pulseDuration = 3f; // 🔢 ajustável — quanto tempo o Cycle do filho fica ativo
    [SerializeField] private float pulseTickInterval = 1f; // 🔢 ajustável — cadência do dano por segundo
    // Upgrade futuro — RoguePulseArea tem um CircleCollider2D DE VERDADE (não um OverlapCircleAll
    // manual, como o anel do Blood Mage), então a própria Unity já escala o trigger real E o
    // visual (SpriteRenderer) sozinha a partir de transform.localScale — não precisa de nenhuma
    // lógica extra dentro do RoguePulseArea.cs, só aplicar o multiplicador aqui antes de ativar.
    [SerializeField] private float pulseSizeMultiplier = 1f; // 🔢 upgradable — até 3x (ver OnValidate)
    private bool isPulseActive;
    private float pulseElapsed;
    // AttackEndTrigger é AnyState no controller — se o pulso esgotar durante a Cambalhota
    // (isAttacking fica true a viagem INTEIRA dela), disparar na hora arrancaria o Animator do
    // meio do Roll. Mesma trava de segurança do stealthEndPending do Assassin: só marca a
    // intenção aqui; Update() só dispara de verdade quando isAttacking voltar a false sozinho.
    private bool attackEndPending;

    // Rede de segurança genérica — mesmo padrão do Barbarian/Ranger/Druid. Cobre as 2 fases
    // travadas do Primário (attack_start/attack_end) e a Ultimate (ações curtas, sem fase
    // "during" nelas mesmas — a fase "during" real, o pulso em si, não trava isAttacking, por
    // isso não precisa de rede de segurança própria: Update() já controla pulseElapsed por
    // código, não por Animation Event). A Cambalhota tem teto próprio (ver
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

#if UNITY_EDITOR
    private void OnValidate()
    {
        pulseSizeMultiplier = Mathf.Clamp(pulseSizeMultiplier, 1f, 3f);
    }
#endif

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

        // Fase "during" do pulso — NÃO trava isAttacking (Rogue fica livre, pedido do usuário),
        // por isso controlada aqui por tempo puro, não por Animation Event/rede de segurança.
        if (isPulseActive)
        {
            pulseElapsed += Time.deltaTime;
            if (pulseElapsed >= pulseDuration) EndPulse();
        }

        // Só dispara o AttackEndTrigger de verdade quando a ação atual (ex.: Cambalhota) já
        // tiver terminado sozinha — nunca no meio dela.
        if (attackEndPending && !isAttacking)
        {
            attackEndPending = false;
            BeginAttackEnd();
        }

        if (isRolling) UpdateRoll();
    }

    // ===================== PRIMÁRIO — SELF AREA PULSE =====================

    protected override void PrimaryAttack()
    {
        if (isAttacking) return;

        isAttacking = true;
        actionElapsed = 0f;
        AnimatorTrigger("AttackTrigger"); // Animator decide AttackStart via AnyState
    }

    // Animation Event, no fim do clipe "attack_start" (4 diagonais) — libera o Rogue pra se
    // mover livremente (decisão do usuário) e ativa a área de dano (filho RoguePulseArea, que
    // segue o Rogue por ser filho de verdade). O dano em si não acontece mais aqui — é tudo
    // dano-por-segundo do próprio filho, enquanto isPulseActive durar.
    public void AnimationAttackStartEndEvent()
    {
        isAttacking = false;

        if (pulseArea != null)
        {
            // Escala o GameObject — RoguePulseArea já lê transform.localScale sozinho (via
            // CircleCollider2D/SpriteRenderer nativos da Unity), então só o multiplicador
            // precisa ser aplicado aqui, 1 único lugar.
            pulseArea.transform.localScale = Vector3.one * pulseSizeMultiplier;
            pulseArea.Activate(stats.damage, pulseTickInterval);
        }
        isPulseActive = true;
        pulseElapsed = 0f;
    }

    // Chamado só por Update() (pulseElapsed >= pulseDuration) — desativa a área na hora (o
    // dano por segundo para imediatamente, não tem motivo pra esperar) e pede o attack_end; se
    // o Rogue estiver ocupado com outra coisa (ex.: Cambalhota), só marca a intenção, ver
    // attackEndPending.
    private void EndPulse()
    {
        isPulseActive = false;
        if (pulseArea != null) pulseArea.Deactivate();

        if (isAttacking) attackEndPending = true;
        else BeginAttackEnd();
    }

    private void BeginAttackEnd()
    {
        isAttacking = true;
        actionElapsed = 0f;
        AnimatorTrigger("AttackEndTrigger"); // Animator decide AttackEnd via AnyState
    }

    // Animation Event, no fim do clipe "attack_end" (4 diagonais) — fecha o primário de verdade.
    public void AnimationAttackEndEvent()
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

        // Sem isso, morrer com o pulso ativo deixava o filho RoguePulseArea tocando dano por
        // segundo pra sempre (órfão, sem ninguém pra chamar Deactivate() depois) — mesmo
        // critério de limpeza do EndRoll acima.
        if (isPulseActive)
        {
            isPulseActive = false;
            if (pulseArea != null) pulseArea.Deactivate();
        }
        attackEndPending = false; // sem isso, o respawn (isAttacking volta a false) dispararia um AttackEndTrigger póstumo
    }

    // Passiva (GDD Seção 17.5) — "4× mais Energia de Ultimate por kill".
    protected override float UltimateEnergyMultiplier => 4f;
}
