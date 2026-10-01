using System.Collections.Generic;
using UnityEngine;

public class Druid : HeroController
{
    // ---------- Ataque primário — vinhas (GDD Seção 17.4) ----------
    [Header("Ataque primário — vinhas")]
    [SerializeField] private GameObject vinePrefab;
    [SerializeField] private int vineCount = 1; // 🔢 teto 15, +1 por tier de arma — mesmo padrão placeholder do arrowCount do Ranger (Sprint 17/18), hook de Tier real ainda não existe
    [SerializeField] private float vineSearchRadius = 15f; // 🔢 ajustável
    [SerializeField] private LayerMask enemyLayerMask; // configurar no Inspector = layer dos monstros
    private bool vineHitFired;

    // Rede de segurança genérica — mesmo padrão do Barbarian/Ranger. Só vale pras janelas
    // curtas (vinha, garra do Alce, start/end da Coruja); "during" do Alce/Coruja tem teto
    // próprio, mais longo, tratado à parte no Update().
    [SerializeField] private float maxActionDuration = 3f; // 🔢 ajustável
    private float actionElapsed;

    // ---------- Ultimate — transformação em Alce (GDD Seção 17.4) ----------
    [Header("Ultimate — Alce")]
    [SerializeField] private RuntimeAnimatorController elkAnimatorController; // substitui TODAS as animações durante a forma Alce
    [SerializeField] private float elkMaxHealthMultiplier = 2f; // 🔢 GDD marca como "TBD" — ajustável em teste
    [SerializeField] private float elkDamageMultiplier = 1.5f; // 🔢 GDD: "mais dano", sem número — ajustável
    [SerializeField] private float elkMoveSpeedMultiplier = 1.3f; // 🔢 GDD: "mais velocidade", sem número — ajustável
    [SerializeField] private float elkFormMaxDuration = 30f; // 🔢 GDD: 30s
    [SerializeField] private float elkKnockbackForce = 4f; // 🔢 ajustável, mesmo padrão do Barbarian
    [SerializeField] private Collider2D elkClawHitboxNE;
    [SerializeField] private Collider2D elkClawHitboxNW;
    [SerializeField] private Collider2D elkClawHitboxSE;
    [SerializeField] private Collider2D elkClawHitboxSW;

    // Corpo do Alce é maior que o humano — o CapsuleCollider2D físico (o mesmo que já existe
    // no Druid, não é um novo componente) muda de tamanho/posição junto com a transformação.
    // Campos próprios em vez de calcular a partir do humano: o corpo do Alce não é
    // necessariamente proporcional (arte própria, silhueta diferente), então cada forma tem
    // seu próprio valor ajustável no Inspector.
    [Header("Collider físico — muda com a forma Alce")]
    [SerializeField] private Vector2 humanColliderOffset;
    [SerializeField] private Vector2 humanColliderSize = new Vector2(1f, 1f); // 🔢 ajustável, tamanho do CapsuleCollider2D na forma humana
    [SerializeField] private Vector2 elkColliderOffset;
    [SerializeField] private Vector2 elkColliderSize = new Vector2(1f, 1f); // 🔢 ajustável, tamanho do CapsuleCollider2D na forma Alce
    // Nome diferente do "bodyCollider" privado do HeroController de propósito — Unity não
    // aceita 2 campos com o mesmo nome numa cadeia de herança, mesmo sendo private em classes
    // diferentes ("The same field name is serialized multiple times").
    private CapsuleCollider2D druidBodyCollider;

    // Os offsets acima (humanColliderOffset/elkColliderOffset) são tunados pra direção E —
    // o Druid tem 2 formas de collider (humana/Alce), então ele não usa o mecanismo genérico
    // da base (AutoFlipBodyCollider = false abaixo), tem o próprio, mas o SINAL que decide
    // E/W é o mesmo da base: DiagonalAimDirection.x (nunca SpriteRenderer.flipX — isso nunca
    // muda em herói nenhum, confirmado lendo os clipes, era a causa real dos projéteis
    // "parando perto, mas não no" Alce). currentBaseOffset guarda a forma ATIVA (humana ou
    // Alce) já sem o sinal do flip, pra reaplicar o espelhamento certo todo frame sem
    // precisar saber em qual forma o Druid está.
    private Vector2 currentBaseOffset;
    private Vector2 currentBaseSize;

    protected override bool AutoFlipBodyCollider => false;

    private RuntimeAnimatorController humanController;
    private bool isElkForm;
    private bool isTransformImmune; // só true durante os clipes de transformar-em/transformar-de-volta
    private float elkFormElapsed;
    private float humanMaxHealth, humanDamage, humanMoveSpeed;
    private bool elkClawHitFired;

    // ---------- Habilidade Secundária (Shift) — transformação em Coruja (GDD Seção 16/17.4) ----------
    [Header("Habilidade Secundária (Shift) — Coruja")]
    [SerializeField] private float owlDuration = 10f; // 🔢 GDD não dá teto — ajustável, mesma ideia do teto de camuflagem do Ranger
    private bool isOwlForm; // só true na fase "during"
    private float owlElapsed;

    protected override void Awake()
    {
        base.Awake();
        humanController = animator != null ? animator.runtimeAnimatorController : null;
        druidBodyCollider = GetComponent<CapsuleCollider2D>();
        ApplyColliderShape(humanColliderOffset, humanColliderSize);
    }

    // offset/size aqui são sempre os valores "E" (DiagonalAimDirection.x >= 0) já
    // configurados no Inspector — o sinal de X correto pra direção atual é resolvido na
    // hora, não guardado.
    private void ApplyColliderShape(Vector2 offset, Vector2 size)
    {
        currentBaseOffset = offset;
        currentBaseSize = size;
        ApplyCurrentColliderShape();
    }

    private void ApplyCurrentColliderShape()
    {
        if (druidBodyCollider == null) return;
        bool facingWest = DiagonalAimDirection.x < 0f;
        druidBodyCollider.offset = new Vector2(facingWest ? -currentBaseOffset.x : currentBaseOffset.x, currentBaseOffset.y);
        druidBodyCollider.size = currentBaseSize;
    }

    protected override void Update()
    {
        if (!GameplayGate.IsActive) return;

        base.Update();
        ApplyCurrentColliderShape(); // mira pode mudar a qualquer frame — reaplica o espelhamento sempre

        // Rede de segurança genérica — suprimida durante as fases "during" longas (cada uma
        // tem o próprio teto de tempo, tratado abaixo), mesmo padrão do
        // "if (isAttacking && !isCamouflaged)" do Ranger.
        if (isAttacking && !isOwlForm && !isElkForm)
        {
            actionElapsed += Time.deltaTime;
            if (actionElapsed >= maxActionDuration)
            {
                Debug.LogWarning("[Druid] Animation Event de fim de ação nunca chegou — forçando fim (verifique o Animator Controller).");
                isAttacking = false;
                isTransformImmune = false; // não deixa imune pra sempre se o evento de transformação falhar
            }
        }

        if (isOwlForm)
        {
            owlElapsed += Time.deltaTime;
            if (owlElapsed >= owlDuration) EndOwlForm();
            UpdateOwlMoveParams();
        }

        if (isElkForm)
        {
            elkFormElapsed += Time.deltaTime;
            if (elkFormElapsed >= elkFormMaxDuration) EndElkForm();
        }
    }

    // ===================== PRIMÁRIO =====================

    protected override void PrimaryAttack()
    {
        if (isAttacking) return;

        if (isElkForm) { StartElkClawAttack(); return; }

        isAttacking = true;
        actionElapsed = 0f;
        vineHitFired = false;
        AnimatorTrigger("AttackTrigger");
    }

    // Animation Event, no frame exato em que as vinhas saem do chão. GDD Seção 17.4/13
    // (Summoned Target Hit): esse evento só SUMONA as vinhas nos alvos escolhidos — o dano de
    // verdade acontece depois, no Animation Event DA PRÓPRIA VINHA (Vine.AnimationVineHitEvent),
    // quando a animação dela chega no frame em que "aperta" o alvo. Duas fases, dois eventos,
    // em dois objetos diferentes — não é um hit instantâneo daqui.
    public void AnimationVineSummonEvent()
    {
        if (vineHitFired) return;
        vineHitFired = true;

        var hits = Physics2D.OverlapCircleAll(transform.position, vineSearchRadius, enemyLayerMask);

        // Não existe registro/enumerador de monstros no projeto (confirmado em EnemyController) —
        // busca própria. Cada EnemyController só entra 1x na lista, então "nunca repete um
        // alvo nesta ativação" (GDD) já sai de graça, sem marcador nenhum.
        var enemies = new List<EnemyController>();
        foreach (var hit in hits)
        {
            var enemy = hit.GetComponent<EnemyController>();
            if (enemy == null || enemies.Contains(enemy)) continue;

            // Trava de segurança — um monstro morto continua existindo (collider incluso, às
            // vezes mais de um por monstro) até o próprio clipe "die" dele terminar de tocar
            // (mesmo critério do Bestiário, Seção 22), então o OverlapCircleAll acima ainda
            // pode pegar um cadáver. Sem isso, a vinha nasce e "aperta" um monstro que já
            // morreu — desperdiça a vinha e fica estranho visualmente.
            if (HealthSystem.IsDead(enemy.stats.health)) continue;

            enemies.Add(enemy);
        }

        enemies.Sort((a, b) =>
            Vector2.Distance(transform.position, a.transform.position)
                .CompareTo(Vector2.Distance(transform.position, b.transform.position)));

        // Compartilhado entre TODAS as vinhas nascidas nesta ativação — cada vinha continua
        // podendo acertar mais de um monstro sozinha (GDD, se estiverem muito próximos), mas
        // nenhum monstro pode ser atingido por 2 vinhas DIFERENTES: sem isso, monstros
        // agrupados tomavam dano dobrado/triplicado (o raio de uma vinha alcançando o alvo da
        // vizinha). Referência única passada por Launch(), não um cooldown por tempo — evita
        // depender de timing (2 vinhas podem disparar no mesmo frame).
        var hitThisActivation = new HashSet<EnemyController>();

        int count = Mathf.Min(vineCount, enemies.Count);
        for (int i = 0; i < count; i++)
        {
            if (vinePrefab == null) continue;

            var vineObj = Instantiate(vinePrefab, enemies[i].transform.position, Quaternion.identity);
            var vine = vineObj.GetComponent<Vine>();
            // SUPOSIÇÃO (Seção 0, item 4) — dano direto = stats.damage, um hit cheio por vinha.
            if (vine != null) vine.Launch(stats.damage, enemyLayerMask, hitThisActivation);
        }
    }

    // Animation Event, no fim do clipe de vinhas.
    public void AnimationVineEndEvent()
    {
        isAttacking = false;
    }

    // ===================== ULTIMATE — ALCE =====================

    // isAttacking/isUsingSecondaryAbility agora são checados por CanUseUltimate()
    // (HeroController), ANTES de gastar energia — sem isso, apertar Ultimate durante a Coruja
    // (Shift) era recusado por dentro de UseUltimate(), mas a energia já tinha sido zerada por
    // quem chamou, um jeito real de perder toda a energia sem nunca virar Alce.
    protected override bool CanUseUltimate() => !isAttacking && !isUsingSecondaryAbility;

    protected override void UseUltimate()
    {
        isAttacking = true;
        isTransformImmune = true;
        elkFormElapsed = 0f;

        humanMaxHealth = stats.maxHealth;
        humanDamage = stats.damage;
        humanMoveSpeed = stats.moveSpeed;

        // Troca o controller inteiro — a animação de transformar-em já é o estado padrão do
        // controller do Alce (configurar no Editor, ver Seção 5), não precisa de Trigger extra.
        if (animator != null) animator.runtimeAnimatorController = elkAnimatorController;
    }

    protected override bool IsUltimateActive => isElkForm;

    protected override void CancelUltimate()
    {
        if (!isElkForm) return;
        EndElkForm();
    }

    // Animation Event, no fim do clipe de transformar-em-Alce.
    public void AnimationElkTransformInEndEvent()
    {
        isTransformImmune = false;
        isElkForm = true;
        isAttacking = false; // libera o ataque — agora vira a garra do Alce, não mais vinha

        stats.maxHealth = humanMaxHealth * elkMaxHealthMultiplier;
        stats.health = stats.maxHealth; // GDD: cura pra 100% do HP máximo do Alce
        stats.damage = humanDamage * elkDamageMultiplier;
        stats.moveSpeed = humanMoveSpeed * elkMoveSpeedMultiplier;
        GameEvents.HealthChanged(stats.health, stats.maxHealth);

        ApplyColliderShape(elkColliderOffset, elkColliderSize);
    }

    private void StartElkClawAttack()
    {
        isAttacking = true;
        actionElapsed = 0f;
        elkClawHitFired = false;
        AnimatorTrigger("AttackTrigger"); // trigger próprio do controller do Alce, mesmo nome, asset diferente
    }

    // Animation Event, no frame exato em que a garra acerta.
    public void AnimationElkClawHitEvent()
    {
        if (elkClawHitFired) return;
        elkClawHitFired = true;

        Collider2D hitbox = GetElkClawHitboxForFacing();
        if (hitbox == null) return;

        var results = new Collider2D[16];
        int count = hitbox.Overlap(ContactFilter2D.noFilter, results);
        for (int i = 0; i < count; i++)
        {
            if (!results[i].CompareTag("Enemy")) continue;
            var enemy = results[i].GetComponent<EnemyController>();
            if (enemy == null) continue;

            enemy.TakeDamage(stats.damage); // já multiplicado (AnimationElkTransformInEndEvent)
            enemy.ApplyKnockback(AimDirection, elkKnockbackForce);
        }
    }

    // Animation Event, no fim do clipe de garra.
    public void AnimationElkClawEndEvent()
    {
        isAttacking = false;
    }

    // Mesmo critério de quadrante do Barbarian.GetHitboxForFacing — duplicado de propósito
    // (é local a cada herói lá também, não é utilitário compartilhado no projeto).
    private Collider2D GetElkClawHitboxForFacing()
    {
        bool east = AimDirection.x >= 0f;
        bool north = AimDirection.y >= 0f;
        if (north) return east ? elkClawHitboxNE : elkClawHitboxNW;
        return east ? elkClawHitboxSE : elkClawHitboxSW;
    }

    // Compartilhado entre o cancelamento manual e o teto de 30s — os dois terminam do mesmo jeito.
    private void EndElkForm()
    {
        isElkForm = false;
        isAttacking = true; // bloqueia de novo durante o clipe de transformar-de-volta
        isTransformImmune = true;

        // GDD: cancelar zera toda a Energia da Ultimate — mesmo custo de deixar o timer
        // acabar (já está em 0 desde a ativação, mas setar explícito documenta a intenção e
        // cobre qualquer fonte futura de recarga de energia durante o "during").
        stats.energy = 0f;
        GameEvents.EnergyChanged(stats.energy, stats.maxEnergy);

        AnimatorTrigger("ElkTransformOutTrigger");
    }

    // Animation Event, no fim do clipe de transformar-de-volta.
    public void AnimationElkTransformOutEndEvent()
    {
        // GDD: HumanCurrentHealth = HumanMaxHealth × (CurrentElkHealth/ElkMaxHealth) — nunca
        // absoluto, nunca cura cheia.
        float ratio = stats.maxHealth > 0f ? stats.health / stats.maxHealth : 0f;

        stats.maxHealth = humanMaxHealth;
        stats.damage = humanDamage;
        stats.moveSpeed = humanMoveSpeed;
        stats.health = stats.maxHealth * ratio;
        GameEvents.HealthChanged(stats.health, stats.maxHealth);

        if (animator != null) animator.runtimeAnimatorController = humanController;
        ApplyColliderShape(humanColliderOffset, humanColliderSize);
        isTransformImmune = false;
        isAttacking = false;
    }

    // isTransformImmune cobre só as 2 animações de transição do Alce (durante o "during" em
    // si o Alce toma dano normal) — isUsingSecondaryAbility cobre a Coruja inteira (start +
    // during + end), pedido explícito do usuário: um Slime de contato ou um golpe que já
    // estava a caminho não pode contar enquanto o Druid está escondido/em transição.
    protected override bool IsDamageImmune => isTransformImmune || isUsingSecondaryAbility;

    // GDD: morrer DURANTE a transformação é a única exceção — pula a conversão proporcional
    // e segue o fluxo universal de morte (Respawn() da base já cura pro stats.maxHealth, que
    // aqui já volta a ser o humano antes disso rodar). Só mexe em stats/flag aqui — a troca do
    // Animator Controller fica pra AnimationDieEndEvent() (ver comentário lá), pra dar tempo
    // do "Die" do Alce (elk_die, reaproveitando por enquanto os frames do die humano) tocar de
    // verdade em vez de sumir sem animação nenhuma.
    protected override void OnHeroDeath()
    {
        if (!isElkForm) return;

        stats.maxHealth = humanMaxHealth;
        stats.damage = humanDamage;
        stats.moveSpeed = humanMoveSpeed;
        isElkForm = false;
    }

    // AnimationDieEndEvent virou virtual na base só pra isso: se a morte aconteceu em forma de
    // Alce, o Animator ainda está no controller do Alce até agora (de propósito — é o que deixa
    // o "Die" dele tocar) — troca pro humano exatamente no instante em que a morte termina de
    // contar (evento real ou timeout de segurança, tanto faz, os dois passam por aqui), antes
    // do Respawn() da base tentar dar Play("Idle") — sem isso ele tentaria tocar "Idle" num
    // controller sem esse estado configurado do jeito certo pro humano.
    public override void AnimationDieEndEvent()
    {
        if (animator != null && animator.runtimeAnimatorController != humanController)
            animator.runtimeAnimatorController = humanController;
        ApplyColliderShape(humanColliderOffset, humanColliderSize);
        base.AnimationDieEndEvent();
    }

    // ===================== SECUNDÁRIA (SHIFT) — CORUJA =====================

    protected override void UseSecondaryAbility()
    {
        if (isElkForm) return; // mutuamente exclusivo com o Alce (Seção 0, item 6)

        // Ao contrário do Alce, aqui isAttacking fica true a viagem INTEIRA (start+during+end)
        // — a Coruja bloqueia SÓ ataque (GDD), então isAttacking nunca vira false até o fim;
        // o movimento continua livre porque o gate de movimento do HeroController.Update() é
        // por nome de estado do Animator, não por isAttacking (ver Seção 0, item 8 / Seção 5).
        isAttacking = true;
        actionElapsed = 0f;

        // Coruja é reposicionamento — sem tirar a colisão com monstro, o Druid continuaria
        // empurrando/travando neles (sem levar dano, mas preso do mesmo jeito), o que anula a
        // função da habilidade. Desliga só a colisão Player×Enemy (não o collider inteiro —
        // isso preservaria escada/parede, que ficam em outra layer), a viagem INTEIRA
        // (start+during+end, mesma janela do IsDamageImmune), religado só no fim de verdade.
        Physics2D.IgnoreLayerCollision(gameObject.layer, LayerMask.NameToLayer("Enemy"), true);

        AnimatorTrigger("OwlTransformStartTrigger");
    }

    // Animation Event no fim do clipe "start" — o Animator já transiciona sozinho pro
    // "during" (Exit Time, sem condição), mesmo padrão do Ranger.
    public void AnimationOwlHiddenEvent()
    {
        isOwlForm = true;
        owlElapsed = 0f;
        IsPlayerUntargetable = true;
    }

    private void UpdateOwlMoveParams()
    {
        if (animator == null) return;
        // A arte da Coruja (owl_fly_idle_ne/nw/se/sw) é diagonal, não cardeal — GDD previa
        // cardeal, mas adaptamos o código pra arte real em vez do contrário (decisão do
        // usuário). Reaproveita o mesmo snap de 4 diagonais que Idle/Walk/Dmg já usam em
        // todo herói (DirectionUtility), em vez de duplicar a lógica aqui.
        Vector2 dir = MoveInput.sqrMagnitude < 0.0001f ? Vector2.zero : DirectionUtility.SnapTo4Diagonals(MoveInput);
        animator.SetFloat("OwlMoveX", dir.x);
        animator.SetFloat("OwlMoveY", dir.y);
    }

    // Shift de novo durante a Coruja — só faz efeito depois que "during" já começou (mesmo
    // critério do Ranger: cancelar no meio do "start" ainda não é caso de uso).
    protected override void CancelSecondaryAbility()
    {
        if (!isOwlForm) return;
        EndOwlForm();
    }

    // Compartilhado entre cancelamento manual e o teto de owlDuration.
    private void EndOwlForm()
    {
        isOwlForm = false;
        IsPlayerUntargetable = false;
        AnimatorTrigger("OwlTransformEndTrigger");
    }

    // Animation Event no fim do clipe "end" (coruja virando herói de novo).
    public void AnimationOwlEndEvent()
    {
        isAttacking = false;
        isUsingSecondaryAbility = false;
        Physics2D.IgnoreLayerCollision(gameObject.layer, LayerMask.NameToLayer("Enemy"), false);
    }
}
