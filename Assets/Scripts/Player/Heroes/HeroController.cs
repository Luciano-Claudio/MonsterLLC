using UnityEngine;
using EasyTransition;

public abstract class HeroController : MonoBehaviour
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
    // próxima ultimate sozinha. 🔢 2s é chute inicial, ajustável em teste.
    [SerializeField] private float ultimateEnergyLockoutDuration = 2f;
    private float ultimateEnergyLockoutRemaining;

    // Prisão (GDD Seção 33, Efeitos Nocivos — ex.: Freeze) — incapacitação total: sem
    // movimento, sem atacar, sem ultimate, até o efeito acabar. Hook público pro futuro
    // sistema de Efeitos chamar (ainda não existe em código, só a estrutura de herói já
    // fica pronta). Bool, não Trigger — vários efeitos de Prisão podem se sobrepor
    // (Seção 33), quem some por último é quem chama SetTrapped(false) de verdade.
    protected bool isTrapped;

    // GDD Seção 11: "Mira: posição do mouse, resolvida em 8 direções (N, S, L, O, NE, NO, SE, SO)."
    protected Vector2 AimDirection { get; private set; } = Vector2.down;

    // Direção crua do mouse, sem o snap de 8 direções — Animator/animação continuam usando
    // AimDirection (só existem 8 poses), mas alguns heróis podem preferir a direção exata
    // pra mecânicas próprias (ex.: trajetória das flechas do Ranger). Congela junto com
    // AimDirection durante isAttacking/isTrapped, mesmo motivo.
    protected Vector2 RawAimDirection { get; private set; } = Vector2.down;

    protected virtual void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) spriteOriginalColor = spriteRenderer.color;
        attackCooldown = new AttackCooldown(1f / stats.attackSpeed);
        controls = new PlayerControls();
        controls.Gameplay.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        controls.Gameplay.Move.canceled += ctx => moveInput = Vector2.zero;
        controls.Gameplay.Look.performed += ctx => UpdateAimDirection(ctx.ReadValue<Vector2>());
        // Segurar o botão continua atacando sozinho, respeitando o cooldown — mesmo padrão
        // do movimento (bool guardado aqui, lido/consumido todo frame no Update()). O clique
        // único vira só o caso onde attackHeld fica true por 1 frame só.
        controls.Gameplay.Attack.performed += ctx => attackHeld = true;
        controls.Gameplay.Attack.canceled += ctx => attackHeld = false;
        controls.Gameplay.Ultimate.performed += ctx => { if (GameplayGate.IsActive && !isDead && !isTrapped) TryUseUltimate(); };
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

        UpdateDamageFlash();

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

        attackCooldown.Tick(Time.deltaTime);
        // !isAttacking aqui é o que impede um attackSpeed alto de disparar um novo ataque
        // por cima de uma animação ainda tocando (ex.: attackSpeed maior que a duração do
        // próprio golpe) — cooldown cuida do ritmo, isAttacking cuida de nunca sobrepor.
        if (attackHeld && !isAttacking && !isTrapped && attackCooldown.TryConsume()) PrimaryAttack();

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
        RawAimDirection = toMouse.normalized;

        // GDD Seção 16: não existe MoveX/MoveY pro herói (ao contrário dos monstros,
        // Seção 22) — walk/attack/ultimate usam sempre a mira, nunca a direção de
        // movimento. Um único par de parâmetros cobre os 3.
        if (animator != null)
        {
            animator.SetFloat("AimX", AimDirection.x);
            animator.SetFloat("AimY", AimDirection.y);
        }
    }

    private void HandleEnemyKilled(int energyValue)
    {
        // Janela de bloqueio pós-ultimate — sem isso, uma ultimate boa (que geralmente
        // limpa a área ou salva o jogador) já paga a energia da próxima sozinha, virando
        // ultimate infinita. Energia é recurso escasso de propósito.
        if (ultimateEnergyLockoutRemaining > 0f) return;

        stats.energy = EnergySystem.AddEnergy(stats.energy, stats.maxEnergy, energyValue);
        GameEvents.EnergyChanged(stats.energy, stats.maxEnergy);
    }

    private void TryUseUltimate()
    {
        if (!EnergySystem.IsReady(stats.energy, stats.maxEnergy)) return;
        UseUltimate();
        stats.energy = 0f;
        GameEvents.EnergyChanged(stats.energy, stats.maxEnergy);
        ultimateEnergyLockoutRemaining = ultimateEnergyLockoutDuration;
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

    public void TakeDamage(float amount)
    {
        // Guarda contra reentrância: sem isso, dois hits antes do respawn terminar
        // disparam OnDeath() duas vezes (penalidade de -30s duplicada, PlayTransition
        // duplicado — o EasyTransition não suporta duas transições concorrentes).
        if (isDead) return;

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
        if (animator != null)
        {
            animator.SetFloat("DamageSpeedMultiplier", 1f);
            animator.SetBool("IsTrapped", false);
        }
        dieElapsed = 0f;
        dieHandled = false;
        AnimatorTrigger("DieTrigger");
        Debug.Log("[HeroController] Morreu — aguardando Animation Event de fim do die...");

        // 1. Cancela estados temporários — nenhum existe ainda (hook pra ultimates com duração/transformações)

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
    public void AnimationDieEndEvent()
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
}
