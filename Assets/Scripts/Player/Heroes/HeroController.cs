using UnityEngine;
using EasyTransition;

public abstract class HeroController : MonoBehaviour, IDamageable
{
    public HeroStats stats = new HeroStats();
    public TransitionSettings respawnTransition;
    protected PlayerControls controls;
    protected Animator animator; // pode não existir em prefabs placeholder — sempre checar com "!= null", nunca "?." (mesma regra do EnemyController)
    private SpriteRenderer spriteRenderer;
    private Color spriteOriginalColor;
    private float damageFlashTimer;
    private const float DamageFlashDuration = 0.08f;
    private Vector2 moveInput;
    private bool isDead;

    // Efeitos Nocivos de dano-ao-longo-do-tempo (GDD Seção 33) — componente compartilhado
    // com EnemyController (ver StatusEffectController.cs). Trapped continua à parte, é
    // incapacitação via SetTrapped(), não dano ao longo do tempo.
    private StatusEffectController statusEffectController;

    // Cooldown do ataque primário, compartilhado por todo herói — GDD: attackSpeed é
    // "ataques por segundo", então o cooldown em si é o inverso (attackSpeed=2 -> ataca a
    // cada 0.5s). Sem isso, segurar o botão vira uma metralhadora: nenhum Melee sobrevive
    // tempo suficiente pra chegar perto, principalmente com knockback aplicado a cada hit.
    private AttackCooldown attackCooldown;
    private bool attackHeld;

    // Rede de segurança — se o Animation Event de fim do "die" nunca disparar (clipe sem o
    // evento configurado), força o respawn depois desse tempo em vez de travar o herói pra
    // sempre "morto" (mesmo padrão do EnemyController — maxDieDuration/dieElapsed).
    public float maxDieDuration = 3f;
    private float dieElapsed;
    private bool dieHandled; // evita chamar Respawn() duas vezes (evento + timeout de segurança)

    // Heróis com ataque real via Animation Event (ex.: Barbarian) marcam isso true/false ao
    // redor da própria janela de ataque — receber dano != interromper uma ação (mesma regra
    // do Bestiário, Seção 22/12): comprometido com o golpe, o dano vira só um flash leve em
    // vez de trocar de estado no Animator (não existe transição Attack -> Damage).
    protected bool isAttacking;

    // Movimento só é permitido durante "Walk" (Seção 16) — sem isso, tomar dano de várias
    // fontes seguidas prende o herói na animação de "Damage" indefinidamente (cada hit
    // reativa o Trigger antes do anterior terminar), impedindo ele de sair da posição e
    // causando mais dano ainda. Mesma solução do Attack dos monstros: cada hit recebido
    // enquanto já está reagindo acelera a própria animação, até um teto onde ela nem chega
    // a tocar — devolve o controle pro jogador na hora.
    private bool isReactingToDamage;
    private float damageSpeedMultiplier = 1f;
    [SerializeField] private float damageSpeedStepPerHit = 0.5f; // 🔢 ajustável
    [SerializeField] private float maxDamageSpeedMultiplier = 3f; // 🔢 ajustável — acima disso, pula a animação de dano

    // Timer, não checagem de estado do Animator — SetTrigger() arma na hora, mas o Animator
    // só processa a transição na própria passada de update dele, depois do Update() dos
    // scripts. Se o hit chega via física (antes do Update() do próprio frame), checar
    // IsInState(animator, "Damage") aqui ainda vê o estado antigo e reseta cedo demais,
    // fazendo o próximo hit reiniciar a reação em vez de acelerar (nunca acumula, nunca
    // atinge o teto, e o Trigger antigo fica armado até disparar sozinho na saída seguinte).
    [SerializeField] private float damageReactionDuration = 0.5f; // 🔢 duração normal (1x) do clipe de dano
    private float damageReactionElapsed;

    // Energia é recurso escasso de propósito — sem essa janela, matar vários monstros com
    // a própria ultimate (comum, já que ela costuma limpar a área) já reabasteceria a
    // próxima ultimate sozinha. 🔢 2s é chute inicial, ajustável em teste. Cobre só o
    // impacto inicial (explosão/projétil) — farmar energia com a área PERSISTENTE que a
    // própria ultimate deixa no chão depois (Mage/Ranger, Seção 13) é intencional, não
    // precisa de proteção nenhuma (decisão explícita do usuário, Sprint 19).
    [SerializeField] private float ultimateEnergyLockoutDuration = 2f;
    private float ultimateEnergyLockoutRemaining;

    // Prisão (GDD Seção 33, Efeitos Nocivos — ex.: Freeze) — incapacitação total: sem
    // movimento, sem atacar, sem ultimate, até o efeito acabar. Hook público pro futuro
    // sistema de Efeitos chamar (ainda não existe em código, só a estrutura de herói já
    // fica pronta). Bool, não Trigger — vários efeitos de Prisão podem se sobrepor
    // (Seção 33), quem some por último é quem chama SetTrapped(false) de verdade.
    protected bool isTrapped;

    // Habilidade Secundária (Shift, GDD Seção 16) — terceira ação de todo herói, cooldown
    // próprio mais alto que o do primário. Só habilidades que transformam/incapacitam o
    // herói são canceláveis (Shift de novo no meio dela) — CancelSecondaryAbility() default
    // não faz nada, então heróis sem cancelamento (a maioria) simplesmente ignoram o clique
    // repetido, sem precisar checar nada a mais.
    [SerializeField] private float secondaryAbilityCooldownDuration = 8f; // 🔢 maior que o do primário, ajustável por herói
    private AttackCooldown secondaryAbilityCooldown;
    protected bool isUsingSecondaryAbility;
    private bool wasUsingSecondaryAbility; // detecta a transição true -> false no Update(), ver comentário lá

    // Flag simples (não por-instância — só existe 1 herói jogável por vez) pra habilidades
    // tipo camuflagem/stealth: enquanto true, EnemyController trata o player como
    // inexistente (GDD Seção 16/17 — Ranger, e futuramente Druid/Assassin, reaproveitam).
    public static bool IsPlayerUntargetable;

    // GDD Seção 11: "Mira: posição do mouse, resolvida em 8 direções (N, S, L, O, NE, NO, SE, SO)."
    protected Vector2 AimDirection { get; private set; } = Vector2.down;

    // Direção crua do mouse, sem o snap de 8 direções — Animator/animação continuam usando
    // AimDirection (só existem 8 poses), mas alguns heróis podem preferir a direção exata
    // pra mecânicas próprias (ex.: trajetória das flechas do Ranger). Congela junto com
    // AimDirection durante isAttacking/isTrapped, mesmo motivo.
    protected Vector2 RawAimDirection { get; private set; } = Vector2.down;

    // Distância crua (não normalizada) até o mouse no instante da mira — o teleporte do Mage
    // (GDD Seção 16/17.3) precisa saber A DISTÂNCIA, não só a direção, pra respeitar um
    // alcance máximo. Congela junto com RawAimDirection pelo mesmo motivo (isAttacking/
    // isTrapped), de graça, por reaproveitar o mesmo "if" de UpdateAimDirection.
    protected float RawAimDistance { get; private set; }

    // Mesma mira, só que travada nas 4 diagonais (nunca cardeal pura) — alimenta
    // DiagonalAimX/Y, usado pelos Blend Trees que só têm pose desenhada pras diagonais
    // (Idle/Walk/Damage/SummonPet). AimDirection sozinho pode devolver um cardeal puro
    // (N/E/S/W), o que nesses Blend Trees cai numa zona onde as 2 diagonais vizinhas ficam
    // exatamente equidistantes — ver DirectionUtility.SnapTo4Diagonals.
    protected Vector2 DiagonalAimDirection { get; private set; } = new Vector2(-0.70710678f, -0.70710678f);

    // Sprint 21 (Druid) — exposição read-only do vetor de movimento cru, mesmo padrão de
    // AimDirection. GDD Seção 16 diz "não existe MoveX/MoveY pro herói" de propósito, mas a
    // Coruja do Druid anda olhando pras 4 direções cardeais de pra-onde-o-jogador-está-andando,
    // não pra mira do mouse — só esse caso quebra a regra geral.
    protected Vector2 MoveInput => moveInput;

    // Collider físico (CapsuleCollider2D) — offset.x precisa espelhar conforme a direção da
    // mira, mesmo motivo do FlippedColliderOffsetX.cs do Bestiário, só que o sinal usado lá
    // (SpriteRenderer.flipX) nunca muda em herói NENHUM: a arte de cada herói é sempre um
    // clipe dedicado por diagonal (idle_ne/nw/se/sw etc.), nunca um clipe espelhado via
    // flipX. O sinal real de "olhando pra E ou pra W" é DiagonalAimDirection.x (o mesmo que
    // já alimenta o Blend Tree). offset.x já configurado no prefab é assumido tunado pra E
    // (DiagonalAimDirection.x >= 0) — nunca precisa de 2 campos, só espelha (× -1) pra W.
    private CapsuleCollider2D bodyCollider;
    private float bodyColliderOffsetXFacingEast;

    // Druid sobrescreve pra false — ele já cuida do próprio collider físico (tem 2 formas,
    // humana e Alce, cada uma com offset/size próprios) e reaplica esse mesmo espelhamento
    // por conta própria em cima disso. Deixar os dois mecanismos ativos ao mesmo tempo faria
    // o daqui sobrescrever com um offset "humano" errado por cima do que o Druid acabou de
    // aplicar pra forma Alce.
    protected virtual bool AutoFlipBodyCollider => true;

    protected virtual void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) spriteOriginalColor = spriteRenderer.color;
        bodyCollider = GetComponent<CapsuleCollider2D>();
        if (bodyCollider != null) bodyColliderOffsetXFacingEast = bodyCollider.offset.x;
        attackCooldown = new AttackCooldown(1f / stats.attackSpeed);
        secondaryAbilityCooldown = new AttackCooldown(secondaryAbilityCooldownDuration);
        statusEffectController = GetComponent<StatusEffectController>(); // pode não existir em prefabs placeholder
        controls = new PlayerControls();
        controls.Gameplay.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        controls.Gameplay.Move.canceled += ctx => moveInput = Vector2.zero;
        // Não usa .performed aqui de propósito — esse evento só dispara quando o mouse se
        // MOVE, e enquanto isAttacking/isTrapped a mira é ignorada (não fica em fila). Se o
        // jogador mexe o mouse durante uma ação longa (ex.: o teleporte do Mage) e para de
        // mexer antes dela acabar, nenhum evento novo chega pra "acordar" a mira depois —
        // ela ficava presa na direção antiga até o próximo movimento. Lendo a posição atual
        // todo frame no Update() (ver UpdateAimDirection ali embaixo) resolve isso de vez:
        // a mira sempre reflete onde o mouse ESTÁ, não só o último evento de movimento.
        // Segurar o botão continua atacando sozinho, respeitando o cooldown — mesmo padrão
        // do movimento (bool guardado aqui, lido/consumido todo frame no Update()). O clique
        // único vira só o caso onde attackHeld fica true por 1 frame só.
        controls.Gameplay.Attack.performed += ctx => attackHeld = true;
        controls.Gameplay.Attack.canceled += ctx => attackHeld = false;
        controls.Gameplay.Ultimate.performed += ctx => { if (GameplayGate.IsActive && !isDead && !isTrapped) TryUseUltimate(); };
        // Shift: se já está usando, tenta cancelar (só faz efeito em habilidade cancelável —
        // CancelSecondaryAbility() default não faz nada); senão, tenta usar (respeitando
        // cooldown próprio, maior que o do primário).
        controls.Gameplay.SecondaryAbility.performed += ctx =>
        {
            if (!GameplayGate.IsActive || isDead || isTrapped) return;
            if (isUsingSecondaryAbility) CancelSecondaryAbility();
            else TryUseSecondaryAbility();
        };
    }

    protected virtual void Start()
    {
        GameEvents.HealthChanged(stats.health, stats.maxHealth);
        GameEvents.EnergyChanged(stats.energy, stats.maxEnergy);
    }

    protected virtual void OnEnable()
    {
        controls.Enable();
        GameEvents.OnEnemyKilled += HandleEnemyKilled;
    }

    protected virtual void OnDisable()
    {
        controls.Disable();
        GameEvents.OnEnemyKilled -= HandleEnemyKilled;
    }

    protected virtual void Update()
    {
        if (!GameplayGate.IsActive) return;

        // Detecta o instante em que a Habilidade Secundária termina de verdade (qualquer
        // herói, qualquer motivo — timeout ou cancelamento manual, os dois convergem pro
        // mesmo "isUsingSecondaryAbility = false" no Animation Event de fim de cada um) e só
        // AÍ arma o cooldown. Cooldown é "tempo de uso", não "tempo desde o clique" — sem
        // isso, o cooldown corria por baixo dos panos enquanto o jogador ainda estava
        // transformado/escondido, deixando o tempo de espera real mais curto que o
        // configurado. Checado antes do "if (isDead)" de propósito: morrer no meio da
        // habilidade também zera isUsingSecondaryAbility (OnDeath), e não queremos perder essa
        // transição só porque o resto do Update() retorna cedo nesse frame.
        if (wasUsingSecondaryAbility && !isUsingSecondaryAbility) secondaryAbilityCooldown.Start();
        wasUsingSecondaryAbility = isUsingSecondaryAbility;

        UpdateDamageFlash();
        UpdateAimDirection(controls.Gameplay.Look.ReadValue<Vector2>());
        if (AutoFlipBodyCollider) ApplyBodyColliderFlip();

        if (isDead)
        {
            // O herói continua existindo (de propósito) enquanto o clipe "die" toca — a
            // transição de volta pro andar inicial só acontece via AnimationDieEndEvent
            // (Animation Event), com o mesmo timeout de segurança do EnemyController caso
            // o evento não esteja configurado.
            dieElapsed += Time.deltaTime;
            if (dieElapsed >= maxDieDuration)
            {
                Debug.LogWarning($"[{GetType().Name}] AnimationDieEndEvent nunca chegou — forçando respawn (verifique o Animator Controller).");
                AnimationDieEndEvent();
            }
            return;
        }

        if (statusEffectController != null) statusEffectController.Tick(Time.deltaTime);

        // Terminou sozinho (sem ter atingido o teto de aceleração) -- reseta pro próximo
        // hit começar do zero, não escalado. Escala pelo próprio multiplicador: acelerado
        // 2x, a janela "reagindo" também encolhe pela metade, pra bater com o que a
        // animação realmente está mostrando na tela.
        if (isReactingToDamage)
        {
            damageReactionElapsed += Time.deltaTime * damageSpeedMultiplier;
            if (damageReactionElapsed >= damageReactionDuration)
            {
                isReactingToDamage = false;
                damageSpeedMultiplier = 1f;
                if (animator != null) animator.SetFloat("DamageSpeedMultiplier", 1f);
            }
        }

        if (ultimateEnergyLockoutRemaining > 0f) ultimateEnergyLockoutRemaining -= Time.deltaTime;

        secondaryAbilityCooldown.Tick(Time.deltaTime);
        attackCooldown.Tick(Time.deltaTime);
        // !isAttacking aqui é o que impede um attackSpeed alto de disparar um novo ataque
        // por cima de uma animação ainda tocando (ex.: attackSpeed maior que a duração do
        // próprio golpe) — cooldown cuida do ritmo, isAttacking cuida de nunca sobrepor.
        // ShouldConsumeCooldownOnAttack() vem ANTES do TryConsume() de propósito — sem isso,
        // o Cleric (primeiro herói cujo clique pode "não fazer nada" por falta de alvo no
        // raio) gastava o cooldown inteiro num clique que nem chegou a lançar o projétil.
        if (attackHeld && !isAttacking && !isTrapped && ShouldConsumeCooldownOnAttack() && attackCooldown.TryConsume())
            PrimaryAttack();

        bool wantsToMove = moveInput.sqrMagnitude > 0.0001f;
        if (animator != null) animator.SetBool("IsMoving", wantsToMove);

        // GDD Seção 16: o herói só se move de verdade enquanto o Animator confirma que já
        // está no estado "Walk" — qualquer outra animação (attack, ultimate, summon etc.)
        // bloqueia o movimento até terminar. Sem Animator ainda wireado (heróis não
        // migrados), AnimatorStateCheck libera sempre, então nada muda pra eles.
        if (wantsToMove && AnimatorStateCheck.IsInState(animator, "Walk"))
            transform.Translate(moveInput * stats.moveSpeed * Time.deltaTime);
    }

    private void UpdateAimDirection(Vector2 screenPosition)
    {
        // Trava a mira durante o golpe/ultimate — mesma regra do Bestiário (Seção 22): a
        // direção de um ataque real não muda no meio da própria animação. Preso (Seção 33)
        // pelo mesmo motivo: incapacitado não vira nem olhando — AimX/AimY ficam
        // congelados na direção de quando o Trapped começou.
        if (isAttacking || isTrapped) return;
        if (Camera.main == null) return;

        Vector3 worldPoint = Camera.main.ScreenToWorldPoint(
            new Vector3(screenPosition.x, screenPosition.y, -Camera.main.transform.position.z));
        Vector2 toMouse = (Vector2)worldPoint - (Vector2)transform.position;
        if (toMouse.sqrMagnitude < 0.0001f) return;

        AimDirection = DirectionUtility.SnapTo8Directions(toMouse);
        DiagonalAimDirection = DirectionUtility.SnapTo4Diagonals(toMouse);
        RawAimDirection = toMouse.normalized;
        RawAimDistance = toMouse.magnitude;

        // GDD Seção 16: não existe MoveX/MoveY pro herói (ao contrário dos monstros,
        // Seção 22) — walk/attack/ultimate usam sempre a mira, nunca a direção de
        // movimento. Um único par de parâmetros cobre os 3.
        if (animator != null)
        {
            animator.SetFloat("AimX", AimDirection.x);
            animator.SetFloat("AimY", AimDirection.y);
            // Par separado, só pros Blend Trees de 4 pontos (ver DiagonalAimDirection) —
            // não substitui AimX/AimY, que continuam alimentando os Blend Trees de 8 pontos
            // (Attack/Ultimate/Teleport do Mage, que têm pose real pra cardeal).
            animator.SetFloat("DiagonalAimX", DiagonalAimDirection.x);
            animator.SetFloat("DiagonalAimY", DiagonalAimDirection.y);
        }
    }

    // Espelha offset.x do collider físico conforme DiagonalAimDirection.x — ver comentário
    // do campo bodyColliderOffsetXFacingEast. Roda toda vez que a mira for recalculada
    // (mesmo Update() de UpdateAimDirection), não precisa de LateUpdate separado porque o
    // Collider2D não depende de pose do Animator, só do valor lógico da mira.
    private void ApplyBodyColliderFlip()
    {
        if (bodyCollider == null) return;
        var offset = bodyCollider.offset;
        offset.x = DiagonalAimDirection.x < 0f ? -bodyColliderOffsetXFacingEast : bodyColliderOffsetXFacingEast;
        bodyCollider.offset = offset;
    }

    // Sprint 22 (Rogue) — multiplicador de Energia de Ultimate ganho por kill. Default 1f,
    // sem efeito em nenhum herói existente. Só o Rogue sobrescreve (passiva: "4× mais Energia
    // de Ultimate por kill").
    protected virtual float UltimateEnergyMultiplier => 1f;

    private void HandleEnemyKilled(int energyValue)
    {
        // Janela de bloqueio pós-ultimate — sem isso, uma ultimate boa (que geralmente
        // limpa a área ou salva o jogador) já paga a energia da próxima sozinha, virando
        // ultimate infinita. Energia é recurso escasso de propósito.
        if (ultimateEnergyLockoutRemaining > 0f) return;

        int adjustedEnergy = Mathf.RoundToInt(energyValue * UltimateEnergyMultiplier);
        stats.energy = EnergySystem.AddEnergy(stats.energy, stats.maxEnergy, adjustedEnergy);
        GameEvents.EnergyChanged(stats.energy, stats.maxEnergy);
    }

    private void TryUseUltimate()
    {
        // Sprint 21 (Druid) — só o Alce tem Ultimate com duração até hoje, então só ele
        // sobrescreve IsUltimateActive/CancelUltimate (default false/no-op, sem efeito nos
        // outros heróis). Checa ANTES do gate de energia: a energia já está zerada desde a
        // ativação (ver embaixo), então o gate bloquearia o próprio cancelamento se viesse depois.
        if (IsUltimateActive) { CancelUltimate(); return; }
        // Checa ANTES de gastar energia — sem isso, um herói que recusa a Ultimate por dentro
        // (ex.: Coruja do Druid, isAttacking de qualquer herói) ainda perdia a energia toda,
        // já que UseUltimate() é void e o "return" cedo dele é invisível pra quem chamou.
        if (!CanUseUltimate()) return;
        if (!EnergySystem.IsReady(stats.energy, stats.maxEnergy)) return;
        // Releitura forçada antes de congelar — o Input System processa este clique ANTES
        // do Update() deste frame, então sem isso a mira congelaria com o valor do frame
        // ANTERIOR (podendo estar bem perto do herói se o mouse só chegou na posição final
        // no exato frame do clique), não com a posição real do mouse agora.
        UpdateAimDirection(controls.Gameplay.Look.ReadValue<Vector2>());
        UseUltimate();
        stats.energy = 0f;
        GameEvents.EnergyChanged(stats.energy, stats.maxEnergy);
        ultimateEnergyLockoutRemaining = ultimateEnergyLockoutDuration;
    }

    private void TryUseSecondaryAbility()
    {
        // Mesma trava que PrimaryAttack()/UseUltimate() já tinham e a Habilidade Secundária
        // não tinha — sem isso, apertar Shift em cima do fim de outra ação (Attack, Ultimate,
        // SummonPet) ainda inicia a habilidade, só que UpdateAimDirection() logo abaixo
        // também é bloqueada pela mesma isAttacking (é a mesma trava, ver o "if" dela), então
        // a releitura forçada vira no-op silencioso e a habilidade acaba usando a mira
        // CONGELADA da ação anterior (podia estar longe da mira real, ex.: o "micro-teleport"
        // do Mage e a pose errada no teleport_end — causa raiz encontrada em produção).
        if (isAttacking) return;
        // Só checa (IsReady), não consome mais aqui — o timer é armado depois, no Update(),
        // quando a habilidade termina de verdade (ver comentário lá e em AttackCooldown.Start()).
        if (!secondaryAbilityCooldown.IsReady) return;
        UpdateAimDirection(controls.Gameplay.Look.ReadValue<Vector2>());
        isUsingSecondaryAbility = true;
        UseSecondaryAbility();
    }

    // Chamado pelo (futuro) sistema de Efeitos Nocivos quando uma Prisão (ex.: Freeze)
    // começa/termina no herói — GDD Seção 33. Interrompe qualquer ação em andamento na
    // hora (igual o Die já faz hoje) e reseta as flags internas de Attack/Damage, senão o
    // jogador ficaria incapaz de agir pra sempre depois que a Prisão passasse.
    public void SetTrapped(bool trapped)
    {
        isTrapped = trapped;
        if (trapped)
        {
            isAttacking = false;
            isReactingToDamage = false;
            damageSpeedMultiplier = 1f;
            if (animator != null) animator.SetFloat("DamageSpeedMultiplier", 1f);
        }
        if (animator != null) animator.SetBool("IsTrapped", trapped);
    }

    [SerializeField] private float floatingTextHeightAdjust = -0.5f; // 🔢 ajuste fino, negativo baixa o texto

    // Topo do sprite, não o pivot bruto — mesmo critério do EnemyController, pra não
    // precisar configurar um offset por herói. floatingTextHeightAdjust corrige o bounds
    // do sprite (pixel art costuma ter espaço transparente, topo "cru" fica alto demais).
    private Vector3 GetFloatingTextSpawnPosition() =>
        spriteRenderer != null
            ? new Vector3(transform.position.x, spriteRenderer.bounds.max.y + floatingTextHeightAdjust, transform.position.z)
            : transform.position;

    // Exposto publicamente (IsDamageImmune em si é protected, cada herói sobrescreve) pra
    // quem aplica dano de fora (ex.: EnemyProjectile) saber se o TakeDamage() abaixo vai ser
    // um no-op ANTES de decidir aplicar um Efeito Nocivo (ex.: Fire) junto — sem isso, o
    // Efeito Nocivo "colava" mesmo com o dano bloqueado pela imunidade (Cambalhota do Rogue,
    // Owl do Druid, etc.), já que TakeDamage() não devolvia nenhum sinal de "bloqueei".
    public bool IsCurrentlyDamageImmune => isDead || IsDamageImmune;

    public virtual void TakeDamage(float amount)
    {
        // Guarda contra reentrância: sem isso, dois hits antes do respawn terminar
        // disparam OnDeath() duas vezes (penalidade de -30s duplicada, PlayTransition
        // duplicado — o EasyTransition não suporta duas transições concorrentes).
        if (isDead) return;
        if (IsDamageImmune) return;

        stats.health = HealthSystem.ApplyDamage(stats.health, amount);
        GameEvents.HealthChanged(stats.health, stats.maxHealth);
        GameEvents.DamageTaken(GetFloatingTextSpawnPosition(), amount);

        // Golpe fatal vai direto pro DieTrigger — não dispara Damage no mesmo frame que
        // já vai morrer.
        if (HealthSystem.IsDead(stats.health)) OnDeath();
        // isTrapped aqui também: sem Trapped -> Damage no grafo, um DamageTrigger disparado
        // preso ficaria armado e só dispararia (fora de hora) quando o Trapped terminasse.
        else if (isAttacking || isTrapped) TriggerDamageFlash();
        else if (isReactingToDamage) AccelerateDamageReaction();
        else StartDamageReaction();
    }

    private void StartDamageReaction()
    {
        isReactingToDamage = true;
        damageSpeedMultiplier = 1f;
        damageReactionElapsed = 0f;
        // Não confiar no valor default do parâmetro no Animator (Unity cria Float novo com
        // default 0, não 1 — mesmo bug já visto no Bestiário) antes de disparar o Trigger.
        if (animator != null) animator.SetFloat("DamageSpeedMultiplier", 1f);
        AnimatorTrigger("DamageTrigger");
    }

    // Cada hit recebido enquanto ainda está reagindo ao anterior acelera a animação de
    // dano em vez de simplesmente reiniciá-la ou ignorá-la — passado o teto, sai da reação
    // na marra (Play direto), sem esperar o Animator: a prioridade aqui é devolver o
    // controle de movimento pro jogador o quanto antes.
    private void AccelerateDamageReaction()
    {
        damageSpeedMultiplier += damageSpeedStepPerHit;

        if (damageSpeedMultiplier >= maxDamageSpeedMultiplier)
        {
            isReactingToDamage = false;
            damageSpeedMultiplier = 1f;
            if (animator != null)
            {
                animator.SetFloat("DamageSpeedMultiplier", 1f);
                bool wantsToMove = moveInput.sqrMagnitude > 0.0001f;
                animator.Play(wantsToMove ? "Walk" : "Idle", 0, 0f);
            }
            return;
        }

        if (animator != null) animator.SetFloat("DamageSpeedMultiplier", damageSpeedMultiplier);
    }

    // Unity sobrecarrega "==" / "!=" pra detectar objetos destruídos/inexistentes, mas o
    // operador "?." do C# ignora essa sobrecarga — todo acesso ao animator passa por
    // "!= null" explícito, nunca "?." (mesma regra do EnemyController).
    protected void AnimatorTrigger(string trigger)
    {
        if (animator != null) animator.SetTrigger(trigger);
    }

    private void TriggerDamageFlash()
    {
        if (spriteRenderer == null) return;
        spriteRenderer.color = Color.white;
        damageFlashTimer = DamageFlashDuration;
    }

    // Timer manual em vez de Coroutine — Coroutine não respeita GameplayGate (Sprint 13):
    // o flash continuaria contando e revertendo a cor durante a pausa.
    private void UpdateDamageFlash()
    {
        if (damageFlashTimer <= 0f) return;
        damageFlashTimer -= Time.deltaTime;
        if (damageFlashTimer <= 0f && spriteRenderer != null) spriteRenderer.color = spriteOriginalColor;
    }

    // GDD Seção 11 — "Morte: ordem de eventos". Os passos 1-4 são só estado/dado, acontecem
    // na hora; o passo 5 (transição + teleporte) é visual e só acontece quando o clipe "die"
    // termina de tocar de verdade — ver AnimationDieEndEvent().
    private void OnDeath()
    {
        isDead = true;
        isAttacking = false; // sem isso, um herói morto no meio de uma ação renasceria sem poder agir de novo (PrimaryAttack/UseUltimate ignoram clique enquanto isAttacking)
        isReactingToDamage = false;
        damageSpeedMultiplier = 1f;
        isTrapped = false; // mesmo motivo — morrer preso não pode renascer incapaz de agir
        isUsingSecondaryAbility = false;
        IsPlayerUntargetable = false; // sem isso, morrer camuflado deixaria os monstros cegos pro respawn inteiro
        if (animator != null)
        {
            animator.SetFloat("DamageSpeedMultiplier", 1f);
            animator.SetBool("IsTrapped", false);
        }
        dieElapsed = 0f;
        dieHandled = false;

        // 1. Cancela estados temporários — hook pra ultimates com duração/transformações e
        // pra pets de kit (GDD: "ao morrer o herói, o pet retorna junto na transição"). Roda
        // ANTES do DieTrigger de propósito: o Alce do Druid troca o runtimeAnimatorController
        // de volta pro humano aqui dentro — se o Trigger disparasse antes, pegaria o
        // controller errado (sem estado "Die"), viraria no-op, e a troca de controller logo
        // em seguida resetaria o Trigger já "gasto" sem nunca tocar animação de morte nenhuma.
        OnHeroDeath();

        AnimatorTrigger("DieTrigger");
        Debug.Log("[HeroController] Morreu — aguardando Animation Event de fim do die...");

        // 2. Destrói loot carregado
        BagController.Instance.Bag.Clear();
        GameEvents.BagChanged(BagController.Instance.Bag);

        // 3. Zera Energia da Ultimate
        stats.energy = 0f;
        GameEvents.EnergyChanged(stats.energy, stats.maxEnergy);

        // 4. Penalidade de -30s no timer do dia
        DayTimer.Instance.ApplyPenalty(30f);
    }

    // Chamado por um Animation Event no último frame do clipe "die" (mesmo padrão do
    // EnemyController) — a animação é a fonte de verdade do timing: o jogador só volta pro
    // andar inicial depois que a morte terminou de tocar por completo.
    public virtual void AnimationDieEndEvent()
    {
        if (dieHandled) return; // proteção — evento + timeout de segurança não chamam Respawn() duas vezes
        dieHandled = true;

        // 5. Respawn no térreo com HP cheio — sempre, mesmo se a penalidade do passo 4 (em
        // OnDeath()) tiver zerado o dia (DayResolver já resolveu Results/Game Over em
        // paralelo; o herói precisa terminar com HP/posição sãos para o próximo dia).
        Respawn();
    }

    private void Respawn()
    {
        // Teleporte acontece dentro do callback, no momento em que a transição
        // já cobriu a tela por completo — o jogador nunca vê o salto de posição.
        TransitionHelper.PlayTransition(respawnTransition, () =>
        {
            var ground = FloorRegistry.Instance.Floors.Find(f => f.originalFloorIdentity == 0);
            if (ground != null)
            {
                transform.position = ground.transform.position;
                FloorManager.Instance.SetCurrentFloor(ground);
            }

            stats.health = stats.maxHealth;
            GameEvents.HealthChanged(stats.health, stats.maxHealth);
            isDead = false;

            // Sem isso, o Animator ficaria preso no estado "Die" pra sempre — diferente do
            // monstro (que é destruído ao fim do clipe), o herói continua existindo e
            // precisa voltar pro Idle na marra (Play direto, não dá pra confiar num
            // trigger/transição pra sair de um estado terminal).
            if (animator != null) animator.Play("Idle", 0, 0f);

            Debug.Log("[HeroController] Respawn no térreo com HP cheio.");
        });
    }

    protected abstract void PrimaryAttack();
    protected abstract void UseUltimate();
    protected abstract void UseSecondaryAbility();

    // Default true — cada herói que tem alguma janela em que a própria Ultimate não pode
    // rodar (isAttacking de qualquer um, isUsingSecondaryAbility do Druid enquanto Coruja)
    // sobrescreve isso, em vez de só dar "return" por dentro de UseUltimate(). TryUseUltimate()
    // checa ANTES de gastar energia — um "return" só de dentro de UseUltimate() não seria
    // visto por quem chamou, e a energia seria gasta de qualquer jeito.
    protected virtual bool CanUseUltimate() => true;

    // Default true — todo herói existente sempre "faz alguma coisa" ao atacar (projétil,
    // golpe etc.), então o clique sempre gasta o cooldown. Só o Cleric sobrescreve: sem
    // monstro no raio de ataque, PrimaryAttack() não faz nada (exceção de MVP, Sprint 23),
    // e sem esse hook o cooldown seria gasto à toa, deixando o Cleric "travado" por até 1
    // cooldown inteiro assim que um monstro aparecesse no raio logo depois do clique.
    protected virtual bool ShouldConsumeCooldownOnAttack() => true;

    // Default false — só heróis cuja Habilidade Secundária esconde/transforma o jogador de
    // verdade (camuflagem do Ranger, teleporte do Mage, Coruja do Druid) sobrescrevem isso.
    // Cobre um caso que IsPlayerUntargetable sozinho não cobre: um Slime de dano por contato
    // não "escolhe" o jogador como alvo, só bate por estar encostado fisicamente — sem essa
    // trava direto no TakeDamage(), o dano de contato (ou de um golpe que já estava a
    // caminho) ainda contava mesmo com o jogador "escondido"/em transição.
    protected virtual bool IsDamageImmune => false;

    // Default no-op — só heróis com habilidade cancelável (ex.: camuflagem do Ranger)
    // sobrescrevem isso. Sem override, apertar Shift de novo em habilidade não-cancelável
    // simplesmente não faz nada (não reinicia, não interrompe).
    protected virtual void CancelSecondaryAbility() { }

    // Sprint 21 (Druid) — par simétrico a CancelSecondaryAbility, só que pra Ultimate. Default
    // false/no-op — só o Alce (única Ultimate com duração até hoje) sobrescreve. A GDD pede
    // "cancelamento manual" (apertar o botão de Ultimate de novo enquanto ativa cancela), mesma
    // UX que Shift já usa pra Habilidade Secundária.
    protected virtual bool IsUltimateActive => false;

    protected virtual void CancelUltimate() { }

    // Chamado no início de OnDeath(), antes de qualquer outra limpeza. Default no-op — só
    // heróis com estado que precisa ser desfeito na morte (ex.: pet do Mage/Blood Mage)
    // sobrescrevem isso.
    protected virtual void OnHeroDeath() { }
}
