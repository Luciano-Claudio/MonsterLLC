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
    // Controlador decide TUDO sobre a vinha e repassa pro prefab em cada Launch() — ver
    // comentário no topo do Vine.cs.
    [SerializeField] private float vineLifetime = 1.5f; // 🔢 ajustável
    [SerializeField] private float vineHitRadius = 0.5f; // 🔢 ajustável — o "circle trigger pequeno" da GDD Seção 17.4
    private bool vineHitFired;

    // Rede de segurança genérica — mesmo padrão do Barbarian/Ranger. Só vale pras janelas
    // curtas (vinha, garra do Alce, start/end da Coruja); "during" do Alce/Coruja tem teto
    // próprio, mais longo, tratado à parte no Update().
    [SerializeField] private float maxActionDuration = 3f; // 🔢 ajustável
    private float actionElapsed;

    // ---------- Variante (D9.1, 2026-11-19, decisão do usuário) — 2 heróis em 1 ----------
    // Floresta (isFeral false): humano — vinhas / Coruja / Leaves_Spell (veneno na tela).
    // Feral (isFeral true): urso (Alce no código) PERMANENTE — garra / Rugido / Cabeçada + Ground Crack.
    // A escolha da variante (futuro, dentro do jogo, com o dia rolando) só seta o bool: a
    // mudança é notada sozinha (mesmo critério do PetSummoner) e o Druid se transforma assim que
    // ficar livre. Depois disso, a transformação toca de novo no início de todo dia.
    [Header("Variante (Floresta x Feral)")]
    [SerializeField] private bool isFeral = false; // false até o sistema de variantes existir
    private bool lastIsFeral;
    private bool feralTransformPending;
    public bool IsFeral
    {
        get => isFeral;
        set => isFeral = value;
    }

    // ---------- Forma de urso (Alce no código/sprites) — Feral ----------
    [Header("Forma de urso (Alce) — Feral")]
    [SerializeField] private RuntimeAnimatorController elkAnimatorController; // substitui TODAS as animações durante a forma Alce
    [SerializeField] private float elkMaxHealthMultiplier = 2f; // 🔢 GDD marca como "TBD" — ajustável em teste
    [SerializeField] private float elkDamageMultiplier = 1.5f; // 🔢 GDD: "mais dano", sem número — ajustável
    [SerializeField] private float elkMoveSpeedMultiplier = 1.3f; // 🔢 GDD: "mais velocidade", sem número — ajustável
    [SerializeField] private float elkKnockbackForce = 4f; // 🔢 ajustável, mesmo padrão do Barbarian
    // Upgrade futuro — escala o transform.localScale INTEIRO durante a forma Alce (visual +
    // CapsuleCollider2D, que é definido em espaço LOCAL e já escala junto automaticamente, +
    // as 4 hitboxes de garra, que são filhas e também seguem a escala do pai). Só vale durante
    // o Alce: volta pra 1 (humano) em AnimationElkTransformOutEndEvent/AnimationDieEndEvent.
    [SerializeField] private float elkSizeMultiplier = 1f; // 🔢 upgradable — até 3x (ver OnValidate)
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
    private float humanMaxHealth, humanDamage, humanMoveSpeed;
    private bool elkClawHitFired;
    private readonly List<EnemyController> elkClawTargets = new();

    // ---------- Habilidade Secundária (Shift) — transformação em Coruja (GDD Seção 16/17.4) ----------
    [Header("Habilidade Secundária (Shift) — Coruja")]
    [SerializeField] private float owlDuration = 10f; // 🔢 GDD não dá teto — ajustável, mesma ideia do teto de camuflagem do Ranger
    private bool isOwlForm; // só true na fase "during"
    private float owlElapsed;

    // ---------- ULTIMATE do Feral — Cabeçada (era o Shift do Alce até o D9.1) ----------
    // Pose trava numa das 4 diagonais (DiagonalAimX/Y, já travado pela base antes de chamar
    // este método), mas a trajetória real da carga segue a mira livre (RawAimDirection), mesmo
    // critério da Cambalhota do Rogue. No fim, deixa um Ground Crack no chão (ver Dr4).
    [Header("Ultimate (Feral) — Cabeçada: impulso forte + freada + dano em área")]
    [SerializeField] private CircleCollider2D elkHeadbuttHitbox; // trigger circular dedicado — ver AnimationElkHeadbuttHitEvent
    [SerializeField] private float elkHeadbuttLaunchSpeed = 32f; // 🔢 ajustável — rápido o bastante pra cobrir elkHeadbuttMaxRange inteiro antes do frame de freio da animação
    // Rework (2026-10-30, pedido do usuário) — não é mais distância fixa: vai até a posição do
    // mouse (RawAimDistance), com este teto — mesmo critério do teleporte do Assassin/Mage. Mouse
    // perto = o Alce para logo ali; mouse longe = vai até o teto na direção dele.
    [SerializeField] private float elkHeadbuttMaxRange = 12f; // 🔢 ajustável
    [SerializeField] private float elkHeadbuttDamageMultiplier = 2f; // 🔢 ajustável
    [SerializeField] private float elkHeadbuttKnockbackForce = 8f; // 🔢 ajustável
    private bool isHeadbuttCharging; // true do Launch até chegar no destino (ou até o Stop, rede de segurança) — Update() reaplica a velocidade todo frame (base já zera por não estar em "Walk")
    private Vector2 headbuttDirection;
    private Vector3 headbuttTargetPosition;
    private bool elkHeadbuttHitFired;
    private readonly List<EnemyController> elkHeadbuttTargets = new();

    // ---------- Ícones alternativos (Primary_Attack/Shift) durante a forma Alce — Skills HUD ----------
    // ---------- SHIFT do Feral — Rugido (D9.1) ----------
    // Mesmo papel do Shift do Barbarian, só que defensivo: trava o movimento só durante a
    // animação (estado "Roar" não é andável) e depois o urso recebe menos dano por roarDuration.
    [Header("Shift (Feral) — Rugido: menos dano recebido por um tempo")]
    [SerializeField] private float roarDamageReductionPercent = 50f; // 🔢 ajustável
    [SerializeField] private float roarDuration = 5f; // 🔢 ajustável — conta desde o clique
    private float roarRemaining;

    // ---------- ULTIMATE do Feral — Ground Crack no fim da Cabeçada (D9.1) ----------
    // Mesmo GroundCrack do Barbarian (área de dano por segundo por um tempo), com a sprite
    // radial do Druid (prefab variante Druid_Ground_Crack — as 4 direções tocam o mesmo clipe).
    [Header("Ultimate (Feral) — Ground Crack no fim da Cabeçada")]
    [SerializeField] private GameObject groundCrackPrefab; // precisa ter GroundCrack
    [SerializeField] private float groundCrackDamagePercent = 50f; // 🔢 % do dano do urso, por segundo
    [SerializeField] private float groundCrackDuration = 3f; // 🔢 ajustável
    [SerializeField] private float groundCrackTickInterval = 0.5f; // 🔢 ajustável
    [SerializeField] private float groundCrackSizeMultiplier = 1f; // 🔢 ajustável

    // ---------- ULTIMATE da Floresta — Leaves_Spell (D9.1) ----------
    // Mesmo papel da Oração do Cleric, mas em TODO monstro visível na câmera: o corpo toca a
    // animação das vinhas (clipes próprios ult_leaves_*, sem o evento de vinha), as folhas
    // tocam 1x em cada monstro (FlashStatus LeavesSpell) e ele fica envenenado (Poison, que
    // assume o visual em loop quando o flash termina).
    [Header("Ultimate (Floresta) — Leaves_Spell: veneno em todo monstro na tela")]
    [SerializeField] private float leavesPoisonDuration = 4f; // 🔢 ajustável
    [SerializeField] private float leavesPoisonDamagePercent = 50f; // 🔢 % do dano do Druid, por segundo
    [SerializeField] private float leavesFlashDuration = 1.25f; // = clipe LeavesSpell (15 frames @12fps)
    private bool leavesSpellFired;
    private static readonly Collider2D[] ScreenOverlapBuffer = new Collider2D[512];
    private readonly List<EnemyController> leavesTargets = new();

    [Header("Ícones alternativos (Skills HUD) — forma de urso (Feral)")]
    [SerializeField] private Sprite elkClawIcon;     // slot 0 (Primary_Attack)
    [SerializeField] private Sprite elkRoarIcon;     // slot 1 (Shift) — Rugido
    [SerializeField] private Sprite elkHeadbuttIcon; // slot 2 (Ultimate) — Cabeçada

#if UNITY_EDITOR
    private void OnValidate()
    {
        elkSizeMultiplier = Mathf.Clamp(elkSizeMultiplier, 1f, 3f);
    }
#endif

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

        UpdateFeralVariant();

        // Rede de segurança genérica — suprimida só durante a Coruja (fase "during" longa, com
        // teto próprio abaixo), mesmo padrão do "if (isAttacking && !isCamouflaged)" do Ranger.
        // A forma de urso agora é permanente (Feral), então as ações dela (garra, Rugido,
        // Cabeçada, transformação) são todas curtas e passam pela rede normal.
        if (isAttacking && !isOwlForm)
        {
            actionElapsed += Time.deltaTime;
            if (actionElapsed >= maxActionDuration)
            {
                Debug.LogWarning("[Druid] Animation Event de fim de ação nunca chegou — forçando fim (verifique o Animator Controller).");
                isAttacking = false;
                isAttackResolving = false;
                isTransformImmune = false; // não deixa imune pra sempre se o evento de transformação falhar
            }
        }

        if (isOwlForm)
        {
            owlElapsed += Time.deltaTime;
            if (owlElapsed >= owlDuration) EndOwlForm();
            UpdateOwlMoveParams();
        }

        if (isHeadbuttCharging) UpdateHeadbuttCharge();
        UpdateRoar();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        GameEvents.OnDayStart += HandleDayStart;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        GameEvents.OnDayStart -= HandleDayStart;
    }

    // ===================== VARIANTE (Floresta x Feral) =====================

    // Mudança no bool notada sozinha (escolha da variante dentro do jogo, ou Inspector em Play
    // Mode). Virou Feral: transforma assim que o Druid ficar livre (nunca corta ação). Deixou de
    // ser Feral: volta a humano com a animação de transformar-de-volta.
    private void UpdateFeralVariant()
    {
        if (isFeral != lastIsFeral)
        {
            lastIsFeral = isFeral;
            if (isFeral) feralTransformPending = true;
            else
            {
                feralTransformPending = false;
                if (isElkForm && !isAttacking) EndElkForm();
            }
        }

        if (!feralTransformPending || isElkForm) return;
        if (isAttacking || isUsingSecondaryAbility || isTransformImmune || HealthSystem.IsDead(stats.health)) return;
        StartFeralTransform();
    }

    // Início de todo dia (decisão do usuário): o Feral toca a transformação de novo. Volta a
    // humano na hora (sem animação) e reentra — assim os stats de humano são relidos (upgrades
    // entre dias) antes de aplicar os multiplicadores do urso.
    private void HandleDayStart()
    {
        if (!isFeral) return;
        if (isElkForm) RestoreHumanForm(keepHealthRatio: false);
        feralTransformPending = true;
    }

    // Era o UseUltimate antigo (transformação com duração) — agora só a entrada na forma
    // permanente do Feral. O clipe de transformar-em é o estado padrão do controller do urso,
    // então trocar o controller já toca a animação (AnimationElkTransformInEndEvent no fim).
    private void StartFeralTransform()
    {
        feralTransformPending = false;
        isAttacking = true;
        actionElapsed = 0f;
        isTransformImmune = true;

        humanMaxHealth = stats.maxHealth;
        humanDamage = stats.damage;
        humanMoveSpeed = stats.moveSpeed;

        if (animator != null) animator.runtimeAnimatorController = elkAnimatorController;
        RefreshActionSpeedMultiplier(); // trocar de controller reseta os parâmetros pro default dele
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

            var vineObj = Instantiate(vinePrefab, enemies[i].transform.position, Quaternion.identity, HeroController.HerosContainer);
            var vine = vineObj.GetComponent<Vine>();
            // SUPOSIÇÃO (Seção 0, item 4) — dano direto = stats.damage, um hit cheio por vinha.
            if (vine != null) vine.Launch(stats.damage, enemyLayerMask, hitThisActivation, vineLifetime, vineHitRadius);
        }
    }

    // Animation Event, no fim do clipe de vinhas — só aqui o cooldown real começa a contar
    // (Seção 53 do GDD).
    public void AnimationVineEndEvent()
    {
        isAttacking = false;
        isAttackResolving = false;
    }

    // ===================== ULTIMATE =====================

    // Checado ANTES de gastar energia (HeroController) — sem isso, apertar Ultimate durante a
    // Coruja/transformação era recusado por dentro, mas a energia já tinha sido zerada.
    // isUsingSecondaryAbility só bloqueia na Coruja (Floresta) — o buff do Rugido (Feral) dura
    // 5s com isUsingSecondaryAbility ligado e não pode travar a Cabeçada.
    protected override bool CanUseUltimate() => !isAttacking && !isTransformImmune && !(isUsingSecondaryAbility && !isElkForm);

    // D9.1 — Feral (urso): Cabeçada + Ground Crack. Floresta (humano): Leaves_Spell (Dr5).
    // A transformação em urso NÃO é mais a Ultimate (virou a variante Feral, permanente).
    protected override void UseUltimate()
    {
        if (isElkForm) { StartHeadbutt(); return; }
        StartLeavesSpell();
    }

    // ---------- Floresta: Leaves_Spell ----------

    private void StartLeavesSpell()
    {
        isAttacking = true;
        actionElapsed = 0f;
        leavesSpellFired = false;
        AnimatorTrigger("UltimateTrigger"); // AnyState -> LeavesSpellCast (clipes ult_leaves_*)
    }

    // Animation Event, no frame em que as vinhas sairiam do chão (frame 3 do ult_leaves_*).
    public void AnimationLeavesSpellEvent()
    {
        if (leavesSpellFired) return;
        leavesSpellFired = true;

        var cam = Camera.main;
        if (cam == null) return;
        Vector2 min = cam.ViewportToWorldPoint(new Vector3(0f, 0f, 0f));
        Vector2 max = cam.ViewportToWorldPoint(new Vector3(1f, 1f, 0f));

        var filter = new ContactFilter2D { useTriggers = true };
        filter.SetLayerMask(enemyLayerMask);
        int count = Physics2D.OverlapArea(min, max, filter, ScreenOverlapBuffer);
        // Dedup obrigatório — todo monstro tem 2 Collider2D (ver EnemyController.CollectDistinct()).
        EnemyController.CollectDistinct(ScreenOverlapBuffer, count, leavesTargets);

        float poisonDps = stats.damage * leavesPoisonDamagePercent / 100f;
        foreach (var enemy in leavesTargets)
        {
            if (enemy == null || enemy.IsDead) continue;
            var statusEffects = enemy.GetComponent<StatusEffectController>();
            if (statusEffects == null) continue;
            // Ordem importa: o Poison primeiro (ApplyStatusEffect já troca o visual pro Poison),
            // o flash das folhas por cima — quando o flash acaba, o Tick volta pro Poison em loop.
            statusEffects.ApplyStatusEffect(StatusEffectType.Poison, leavesPoisonDuration, poisonDps);
            statusEffects.FlashStatus(StatusEffectType.LeavesSpell, leavesFlashDuration);
        }
    }

    // Animation Event, no último frame do ult_leaves_*.
    public void AnimationLeavesSpellEndEvent()
    {
        isAttacking = false;
    }

    // ---------- Feral: Ground Crack no fim da Cabeçada ----------

    private void SpawnUltimateGroundCrack()
    {
        if (groundCrackPrefab == null) return;
        var crackObj = Instantiate(groundCrackPrefab, transform.position, Quaternion.identity, HeroController.HerosContainer);
        var crack = crackObj.GetComponent<GroundCrack>();
        // Direção irrelevante: o override do Druid toca o mesmo clipe radial nas 4.
        if (crack != null) crack.Initialize(GroundCrack.Direction.SE, groundCrackSizeMultiplier, stats.damage * groundCrackDamagePercent / 100f, groundCrackTickInterval);
        Destroy(crackObj, groundCrackDuration);
    }

    // ---------- Feral: Rugido (Shift) ----------

    private void StartRoar()
    {
        isAttacking = true; // trava ataque/movimento só durante a animação (AnimationRoarEndEvent)
        actionElapsed = 0f;
        roarRemaining = roarDuration;
        AnimatorTrigger("SecondaryAbilityTrigger"); // AnyState -> Roar (controller do urso)
    }

    // Animation Event, último frame do roar_* — libera o urso; a redução de dano segue até
    // roarRemaining acabar (UpdateRoar), e só aí o cooldown do Shift começa a contar.
    public void AnimationRoarEndEvent()
    {
        isAttacking = false;
    }

    private void UpdateRoar()
    {
        if (roarRemaining <= 0f) return;
        roarRemaining -= Time.deltaTime;
        if (roarRemaining <= 0f) isUsingSecondaryAbility = false;
    }

    protected override float GetConditionalDamageReductionPercent() =>
        roarRemaining > 0f ? roarDamageReductionPercent : 0f;

    // Animation Event, no fim do clipe de transformar-em-Alce.
    public void AnimationElkTransformInEndEvent()
    {
        isTransformImmune = false;
        isElkForm = true;
        isAttacking = false; // libera o ataque — agora vira a garra do Alce, não mais vinha

        // healthBefore ANTES de tocar em maxHealth — o Floating Combat Text de cura precisa do
        // quanto a vida subiu de verdade (ex.: 12/32 -> 76/76 mostra "+64"), não um valor
        // calculado a partir do novo teto. Não usa o Heal() genérico da base porque aqui
        // maxHealth muda no mesmo instante (vira o teto do Alce), não um alvo fixo.
        float healthBefore = stats.health;
        stats.maxHealth = humanMaxHealth * elkMaxHealthMultiplier;
        stats.health = stats.maxHealth; // GDD: cura pra 100% do HP máximo do Alce
        stats.damage = humanDamage * elkDamageMultiplier;
        stats.moveSpeed = humanMoveSpeed * elkMoveSpeedMultiplier;
        GameEvents.HealthChanged(stats.health, stats.maxHealth);

        float healedAmount = stats.health - healthBefore;
        if (healedAmount > 0f) GameEvents.HealReceived(GetFloatingTextSpawnPosition(), healedAmount);

        ApplyColliderShape(elkColliderOffset, elkColliderSize);
        transform.localScale = Vector3.one * elkSizeMultiplier;

        ApplyElkActionIcons();
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

        // 128 (não 16) — monstro tem 2 Collider2D, 16 saturava com só 8 monstros aglomerados
        // perto do player, deixando o resto fora do hit sem erro nenhum (bug real, 2026-10-26).
        var results = new Collider2D[128];
        int count = hitbox.Overlap(ContactFilter2D.noFilter, results);
        // Dedup obrigatório — mesmo bug do primário do Mage (ver EnemyController.CollectDistinct()).
        EnemyController.CollectDistinct(results, count, elkClawTargets);
        foreach (var enemy in elkClawTargets)
        {
            enemy.TakeDamage(stats.damage); // já multiplicado (AnimationElkTransformInEndEvent)
            enemy.ApplyKnockback(AimDirection, elkKnockbackForce);
        }
    }

    // Animation Event, no fim do clipe de garra — é o Primário em forma Alce (mesmo gate/
    // cooldown da vinha, só muda a animação), então o cooldown real também só começa a
    // contar aqui (Seção 53 do GDD).
    public void AnimationElkClawEndEvent()
    {
        isAttacking = false;
        isAttackResolving = false;
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

    // Só quando a variante deixa de ser Feral (isFeral false com o urso ativo) — toca o clipe de
    // transformar-de-volta.
    private void EndElkForm()
    {
        isElkForm = false;
        isAttacking = true; // bloqueia de novo durante o clipe de transformar-de-volta
        isTransformImmune = true;
        AnimatorTrigger("ElkTransformOutTrigger");
    }

    // Animation Event, no fim do clipe de transformar-de-volta.
    public void AnimationElkTransformOutEndEvent()
    {
        RestoreHumanForm(keepHealthRatio: true);
        isTransformImmune = false;
        isAttacking = false;
    }

    // Volta stats/controller/collider/tamanho/ícones pro humano. keepHealthRatio (GDD):
    // HumanCurrentHealth = HumanMaxHealth × (CurrentElkHealth/ElkMaxHealth) — nunca cura cheia.
    // false = início do dia: a transformação logo em seguida cura pra 100% do urso de qualquer jeito.
    private void RestoreHumanForm(bool keepHealthRatio)
    {
        float ratio = stats.maxHealth > 0f ? stats.health / stats.maxHealth : 0f;

        stats.maxHealth = humanMaxHealth;
        stats.damage = humanDamage;
        stats.moveSpeed = humanMoveSpeed;
        stats.health = keepHealthRatio ? stats.maxHealth * ratio : Mathf.Min(stats.health, stats.maxHealth);
        GameEvents.HealthChanged(stats.health, stats.maxHealth);

        isElkForm = false;
        if (animator != null && animator.runtimeAnimatorController != humanController)
        {
            animator.runtimeAnimatorController = humanController;
            RefreshActionSpeedMultiplier(); // trocar de controller reseta os parâmetros pro default dele
        }
        ApplyColliderShape(humanColliderOffset, humanColliderSize);
        transform.localScale = Vector3.one; // volta ao tamanho humano normal — size do urso só vale nele
        ClearElkActionIcons();
    }

    // isTransformImmune cobre só as 2 animações de transição do Alce (durante o "during" em
    // si o Alce toma dano normal) — isUsingSecondaryAbility cobre a Coruja inteira (start +
    // during + end), pedido explícito do usuário: um Slime de contato ou um golpe que já
    // estava a caminho não pode contar enquanto o Druid está escondido/em transição.
    // D9.1: a imunidade do Shift vale só pra Coruja (Floresta). O Shift do urso (Rugido) usa
    // isUsingSecondaryAbility pro buff inteiro e dá REDUÇÃO de dano, não imunidade.
    protected override bool IsDamageImmune => isTransformImmune || (isUsingSecondaryAbility && !isElkForm);

    // GDD: morrer DURANTE a transformação é a única exceção — pula a conversão proporcional e
    // segue o fluxo universal de morte (stats.maxHealth já volta a ser o humano aqui, embora a
    // instância seja destruída logo depois — não reaproveitada). Só mexe em stats/flag aqui — a
    // troca do Animator Controller fica pra AnimationDieEndEvent() (ver comentário lá), pra dar
    // tempo do "Die" do Alce (elk_die, reaproveitando por enquanto os frames do die humano)
    // tocar de verdade em vez de sumir sem animação nenhuma.
    protected override void OnHeroDeath()
    {
        roarRemaining = 0f;
        feralTransformPending = false;
        if (!isElkForm) return;

        // Mesmo critério do EndRoll do Rogue — morrer no meio da cabeçada não pode deixar o
        // Druid pra sempre sem colisão com monstro depois do respawn.
        if (isHeadbuttCharging)
        {
            isHeadbuttCharging = false;
            Physics2D.IgnoreLayerCollision(gameObject.layer, LayerMask.NameToLayer("Enemy"), false);
        }
        ClearElkActionIcons();

        stats.maxHealth = humanMaxHealth;
        stats.damage = humanDamage;
        stats.moveSpeed = humanMoveSpeed;
        isElkForm = false;
    }

    // AnimationDieEndEvent virou virtual na base só pra isso: se a morte aconteceu em forma de
    // Alce, o Animator ainda está no controller do Alce até agora (de propósito — é o que deixa
    // o "Die" dele tocar) — troca pro humano exatamente no instante em que a morte termina de
    // contar (evento real ou timeout de segurança, tanto faz, os dois passam por aqui), antes
    // de base.AnimationDieEndEvent() destruir o objeto.
    public override void AnimationDieEndEvent()
    {
        if (animator != null && animator.runtimeAnimatorController != humanController)
        {
            animator.runtimeAnimatorController = humanController;
            RefreshActionSpeedMultiplier(); // trocar de controller reseta os parâmetros pro default dele
        }
        ApplyColliderShape(humanColliderOffset, humanColliderSize);
        transform.localScale = Vector3.one; // mesmo critério — se morreu em forma Alce, volta ao tamanho humano aqui
        base.AnimationDieEndEvent();
    }

    // ===================== SECUNDÁRIA (SHIFT) — CORUJA =====================

    protected override void UseSecondaryAbility()
    {
        if (isElkForm) { StartRoar(); return; } // Feral (urso): Rugido — a Coruja é só da Floresta

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

    // ===================== SECUNDÁRIA (SHIFT) EM FORMA ALCE — CABEÇADA =====================

    private void StartHeadbutt()
    {
        isAttacking = true; // trava primário/ultimate a sequência inteira, mesmo critério da Coruja
        actionElapsed = 0f;
        elkHeadbuttHitFired = false;

        // Trajetória real livre (mira exata) — só a pose (DiagonalAimX/Y) trava numa das 4
        // diagonais, e essa já está travada pela base (TryUseSecondaryAbility força a
        // releitura da mira antes de chamar este método) — mesmo critério da Cambalhota do
        // Rogue, sem precisar setar nenhum parâmetro de Animator manual aqui (igual o próprio
        // StartElkClawAttack já faz).
        headbuttDirection = RawAimDirection;

        // Vai até onde o mouse estiver, com teto em elkHeadbuttMaxRange — mesmo critério do
        // teleporte do Assassin/Mage (Mathf.Min(RawAimDistance, teto)).
        float distance = Mathf.Min(RawAimDistance, elkHeadbuttMaxRange);
        headbuttTargetPosition = transform.position + (Vector3)(headbuttDirection * distance);

        AnimatorTrigger("UltimateTrigger"); // D9.1: Cabeçada virou a Ultimate do Feral (era o Shift do Alce)
    }

    // Animation Event — só LIGA a carga; o movimento de verdade (e a parada exatamente no
    // destino) é todo feito em UpdateHeadbuttCharge(), chamado todo frame enquanto
    // isHeadbuttCharging. Ignora colisão com monstro durante a carga inteira (atravessa, mesmo
    // critério da Coruja/Cambalhota) — parede continua bloqueando normal.
    public void AnimationElkHeadbuttLaunchEvent()
    {
        isHeadbuttCharging = true;
        Physics2D.IgnoreLayerCollision(gameObject.layer, LayerMask.NameToLayer("Enemy"), true);
    }

    // Rede de segurança — NÃO é mais quem decide a parada normal (isso já aconteceu em
    // UpdateHeadbuttCharge no instante exato em que o Alce chegou no destino, podendo ser bem
    // antes deste frame se o mouse estava próximo). Só age se, por algum motivo, a carga ainda
    // não tiver chegado (ex.: parede no meio do caminho) — aí força a parada aqui mesmo, no
    // meio do clipe (não é o último frame).
    public void AnimationElkHeadbuttStopEvent()
    {
        if (!isHeadbuttCharging) return;
        isHeadbuttCharging = false;
        if (rb != null) rb.linearVelocity = Vector2.zero;
        Physics2D.IgnoreLayerCollision(gameObject.layer, LayerMask.NameToLayer("Enemy"), false);
    }

    // Animation Event, no MESMO frame do Stop (evento separado de propósito — mesmo critério
    // do rework do Gunslinger) — dano e knockback em área pelo CircleCollider2D dedicado.
    public void AnimationElkHeadbuttHitEvent()
    {
        if (elkHeadbuttHitFired) return;
        elkHeadbuttHitFired = true;

        if (elkHeadbuttHitbox == null) return;

        // 128 (não 16) — mesmo bug real já corrigido no golpe do Barbarian/garra do Alce
        // (monstro aglomerado tem 2 Collider2D cada, 16 satura rápido).
        var results = new Collider2D[128];
        int count = elkHeadbuttHitbox.Overlap(ContactFilter2D.noFilter, results);
        EnemyController.CollectDistinct(results, count, elkHeadbuttTargets);
        foreach (var enemy in elkHeadbuttTargets)
        {
            enemy.TakeDamage(stats.damage * elkHeadbuttDamageMultiplier); // já é o dano do Alce (multiplicado em AnimationElkTransformInEndEvent)
            Vector2 direction = ((Vector2)enemy.transform.position - (Vector2)transform.position).normalized;
            enemy.ApplyKnockback(direction, elkHeadbuttKnockbackForce);
        }
    }

    // Animation Event, no último frame do clipe de verdade — fecha a ação e deixa o Ground
    // Crack no chão (ver SpawnUltimateGroundCrack, Dr4).
    public void AnimationElkHeadbuttEndEvent()
    {
        isAttacking = false;
        SpawnUltimateGroundCrack();
    }

    // Move em direção a headbuttTargetPosition todo frame (base.Update() já zera
    // rb.linearVelocity neste mesmo frame por não estar em "Walk", mesmo critério do
    // UpdateRoll do Rogue) e para EXATAMENTE nele assim que chegar — pode ser bem antes do
    // AnimationElkHeadbuttStopEvent se o mouse estava próximo (ele "fica ali" esperando o
    // frame de dano), ou ser interrompido mais tarde pela rede de segurança se algo bloquear
    // o caminho (ex.: parede).
    private void UpdateHeadbuttCharge()
    {
        if (rb == null) { isHeadbuttCharging = false; return; }

        Vector2 toTarget = (Vector2)headbuttTargetPosition - rb.position;
        float step = elkHeadbuttLaunchSpeed * Time.deltaTime;

        if (toTarget.magnitude <= step)
        {
            rb.position = headbuttTargetPosition;
            rb.linearVelocity = Vector2.zero;
            isHeadbuttCharging = false;
            Physics2D.IgnoreLayerCollision(gameObject.layer, LayerMask.NameToLayer("Enemy"), false);
            return;
        }

        rb.linearVelocity = headbuttDirection * elkHeadbuttLaunchSpeed;
    }

    // ===================== Ícones alternativos (Skills HUD) durante a forma Alce =====================

    private void ApplyElkActionIcons()
    {
        GameEvents.ActionIconOverrideChanged(0, elkClawIcon);
        GameEvents.ActionIconOverrideChanged(1, elkRoarIcon);
        GameEvents.ActionIconOverrideChanged(2, elkHeadbuttIcon);
    }

    private void ClearElkActionIcons()
    {
        GameEvents.ActionIconOverrideChanged(0, null);
        GameEvents.ActionIconOverrideChanged(1, null);
        GameEvents.ActionIconOverrideChanged(2, null);
    }
}
