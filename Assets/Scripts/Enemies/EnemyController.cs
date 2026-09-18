using UnityEngine;

public abstract class EnemyController : MonoBehaviour
{
    public EnemyStats stats = new EnemyStats();
    public int energyReward = 20;
    public int monsterEssenceDropAmount = 1; // quantidade dropada por abate (GDD Seção 38, 🔢 valor de balanceamento pendente)
    public FloorDefinition ownerFloor;

    [Header("Patrulha (GDD Seção 22 — idle/walk aleatório antes de detectar o jogador)")]
    [SerializeField] private float minIdleDuration = 1.5f;
    [SerializeField] private float maxIdleDuration = 3.5f;
    [SerializeField] private float minWalkDuration = 2f;
    [SerializeField] private float maxWalkDuration = 4f;
    [SerializeField] private float patrolRadius = 3f;

    // Emboscada (ex.: Skeletons/Gargoyle do Andar 5): nunca vaga sozinho, fica na pose
    // parada (idle estático, não-direcional) até detectar o jogador — e, ao contrário de
    // todo o resto do jogo, pode "desistir" e voltar a dormir se o jogador se afastar de
    // novo, sem nenhuma animação de transição (o player já não estaria nem olhando pra
    // ele nesse momento).
    [SerializeField] private bool staysDormantUntilDetected = false;

    // Falso pra monstros que nunca têm animação de attack real (ex.: Slimes — só contato,
    // pra sempre). Verdadeiro é o padrão pra todo o resto do Melee/Ranged comum.
    [Header("Animação de ataque (arte real + Animation Event)")]
    [SerializeField] protected bool hasAttackAnimation = true;

    // Rede de segurança — se o Animation Event de fim de ataque nunca disparar (clipe
    // sem o evento configurado, erro de setup), o ataque força o próprio fim depois
    // desse tempo em vez de travar o inimigo pra sempre em "Attacking".
    public float maxAttackAnimationDuration = 5f;

    // Mesma rede de segurança, pro clipe de die.
    public float maxDieDuration = 3f;

    protected Transform player;
    protected PatrolAI patrolAI;
    protected AttackCooldown attackAnimationCooldown;
    protected bool isInCombat;

    // Uma vez que o monstro sofre dano, ele trava em combate pra sempre — nem um monstro
    // de emboscada (staysDormantUntilDetected) pode voltar a dormir depois disso. Sem isso,
    // dava pra ficar atacando um monstro de longe (ex.: leque do Ranger) sem ele nunca vir
    // pro corpo a corpo, já que ataques à distância não entram no observationRadius sozinhos.
    private bool combatLocked;

    private enum AttackAnimState { Idle, Attacking }
    private AttackAnimState attackAnimState = AttackAnimState.Idle;
    private float attackAnimElapsed;
    private bool attackHitFired; // já conectou nesse ciclo de ataque? evita golpe duplicado se o teto de aceleração for atingido depois do Hit Event já ter disparado

    // Regra nova (substitui "dano durante o attack vira só flash, sem mais nada"): cada
    // interrupção acelera a própria animação do golpe em andamento — visualmente fica
    // óbvio que o dano "pegou", sem cancelar o compromisso com o ataque. Acima do teto, o
    // golpe resolve na hora, sem esperar a animação terminar de acelerar.
    [Header("Aceleração do ataque ao ser interrompido (feedback visual de dano)")]
    [SerializeField] private float attackSpeedStepPerHit = 0.5f; // 🔢 ajustável
    [SerializeField] private float maxAttackSpeedMultiplier = 3f; // 🔢 ajustável — acima disso, o golpe sai instantâneo
    private float attackSpeedMultiplier = 1f;

    private Vector2 spawnOrigin;
    private float dieElapsed;
    private bool isDead;
    private bool dieHandled; // evita destruir/dropar loot duas vezes (evento + timeout de segurança)
    protected Animator animator;
    private SpriteRenderer spriteRenderer;
    private Color spriteOriginalColor;
    private float damageFlashTimer;

    private const float DamageFlashDuration = 0.08f;

    protected virtual void Awake()
    {
        animator = GetComponent<Animator>(); // pode não existir em prefabs placeholder — sempre checar com "!= null", nunca "?." (ver AnimatorTrigger/SetMoving/SetInCombat/SetMoveDirection)
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) spriteOriginalColor = spriteRenderer.color;
        spawnOrigin = transform.position;
        patrolAI = new PatrolAI(minIdleDuration, maxIdleDuration, minWalkDuration, maxWalkDuration);
        attackAnimationCooldown = new AttackCooldown(stats.attackAnimationCooldown);
        SetMoveDirection(Vector2.down); // direção padrão — sem isso, MoveX/MoveY ficam em (0,0) até o primeiro Move(), deixando o Blend Tree de Idle indefinido por alguns frames
    }

    // Unity sobrecarrega "==" / "!=" pra detectar objetos destruídos/inexistentes, mas o
    // operador "?." do C# ignora essa sobrecarga e checa a referência crua — num Animator
    // ausente isso deixa passar a chamada e explode um MissingComponentException. Por isso
    // todo acesso ao animator neste arquivo passa por "!= null" explícito, nunca "?.".
    private void AnimatorTrigger(string trigger)
    {
        if (animator != null) animator.SetTrigger(trigger);
    }

    protected virtual void Start()
    {
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;
    }

    protected virtual void Update()
    {
        if (!GameplayGate.IsActive) return;
        if (!FloorActivationCheck.IsActive(ownerFloor, FloorManager.Instance.CurrentFloor)) return;

        UpdateDamageFlash();
        UpdateKnockback();

        if (isDead)
        {
            // O objeto continua existindo (de propósito) enquanto o clipe "die" toca —
            // a destruição real só acontece via AnimationDieEndEvent (Animation Event).
            // Timer manual, não Coroutine: Coroutine não respeita o GameplayGate (decisão
            // da Sprint 13) — continuaria contando durante a pausa.
            dieElapsed += Time.deltaTime;
            if (dieElapsed >= maxDieDuration)
            {
                Debug.LogWarning($"[{GetType().Name}] AnimationDieEndEvent nunca chegou — forçando destruição (verifique o Animator Controller).");
                AnimationDieEndEvent();
            }
            return;
        }

        if (player == null) return;

        if (hasAttackAnimation) attackAnimationCooldown.Tick(Time.deltaTime);

        if (attackAnimState == AttackAnimState.Attacking)
        {
            // Comprometido com a animação de ataque de verdade — não persegue, não
            // reinicia outro ataque no meio dela.
            attackAnimElapsed += Time.deltaTime;
            if (attackAnimElapsed >= maxAttackAnimationDuration)
            {
                Debug.LogWarning($"[{GetType().Name}] AnimationAttackEndEvent nunca chegou — forçando fim do ataque (verifique o Animator Controller).");
                EndAttackAnimation();
            }
            return;
        }

        if (!isInCombat) UpdatePatrol();
        else UpdateCombat();
    }

    // GDD Seção 22: antes de detectar o jogador, alterna idle/walk aleatoriamente. A
    // detecção troca pra combate imediatamente — quem garante que o "idle" de patrulha
    // não corta visualmente no meio é só o Exit Time da transição no Animator
    // (Idle -> Walk / Idle -> IdleCombat), não um atraso aqui no código. Um atraso
    // code-side chegou a existir aqui, mas usava o timer da fase de patrulha inteira
    // (até maxWalkDuration, vários segundos) em vez do tamanho real do clipe — o monstro
    // ficava "ignorando" o jogador por tempo demais. Removido.
    private void UpdatePatrol()
    {
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        if (distanceToPlayer <= stats.observationRadius)
        {
            isInCombat = true;
            return;
        }

        if (staysDormantUntilDetected)
        {
            // Nunca vaga sozinho — só a pose parada (idle estático) até detectar.
            SetMoving(false);
            SetInCombat(false);
            return;
        }

        patrolAI.Tick(Time.deltaTime, () => spawnOrigin + Random.insideUnitCircle * patrolRadius);

        // A fase "Walking" do PatrolAI dura um tempo aleatório fixo, sem saber a
        // distância real até o alvo — se o monstro chega no alvo antes desse tempo
        // acabar, IsMoving precisa cair pra false na hora. Sem isso, o Walk continua
        // tocando parado até o timer da fase estourar, mesmo o monstro já tendo parado.
        Vector2 toTarget = patrolAI.WalkTarget - (Vector2)transform.position;
        bool moving = patrolAI.CurrentPhase == PatrolPhase.Walking && toTarget.sqrMagnitude >= 0.0001f;

        SetMoving(moving);
        SetInCombat(false);
        if (moving)
        {
            toTarget.Normalize();
            MoveInDirection(toTarget);
        }
    }

    // Move o transform de verdade só quando o Animator já entrou no estado "Walk" — nunca
    // no frame em que só a intenção (IsMoving) foi marcada. Sem isso, a entidade desliza
    // pela tela ainda com a pose de Idle enquanto a transição (que tem Exit Time) espera
    // o clipe de patrulha terminar.
    protected void MoveInDirection(Vector2 direction)
    {
        SetMoveDirection(direction);
        if (AnimatorStateCheck.IsInState(animator, "Walk"))
            transform.Translate(direction * stats.moveSpeed * Time.deltaTime);
    }

    private void UpdateCombat()
    {
        // Só monstros de emboscada desistem — o resto do jogo persegue pra sempre depois
        // de detectar (regra padrão, inalterada). Sem animação de transição de volta: se
        // o jogador já saiu do raio de observação, ele nem está olhando pra esse monstro
        // nesse instante — snap direto pra pose parada.
        if (staysDormantUntilDetected && !combatLocked)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, player.position);
            if (distanceToPlayer > stats.observationRadius)
            {
                isInCombat = false;
                SetMoving(false);
                SetInCombat(false);
                return;
            }
        }

        SetInCombat(true);
        // Direção de movimento != direção de mira: o Ranged foge do player (MoveX/MoveY
        // aponta pra longe dele), mas continua precisando "olhar" pro player enquanto
        // ataca/segura posição — daí o par separado (AimX/AimY), sempre recalculado em
        // direção à posição real do player, independente de Move() estar fugindo,
        // aproximando ou parado.
        Vector2 toPlayer = player.position - transform.position;
        SetAimDirection(toPlayer.normalized);
        Move();
        if (hasAttackAnimation) TryStartAttackAnimation();
    }

    protected abstract void Move(); // implementado por Melee (gruda no contato) e Ranged (mantém alcance, foge se o player chegar perto demais)
    protected abstract void ExecuteAttackHit(); // golpe/disparo real — chamado pelo AnimationHitEvent, no frame exato em que a animação conecta
    protected abstract bool InAttackRange(); // define o alcance que autoriza iniciar a animação de ataque (mesmo raio usado pelo contato/disparo)

    private void TryStartAttackAnimation()
    {
        if (!InAttackRange()) return;
        if (!attackAnimationCooldown.TryConsume()) return;

        attackAnimState = AttackAnimState.Attacking;
        attackAnimElapsed = 0f;
        attackHitFired = false;
        attackSpeedMultiplier = 1f;
        // Não confiar no valor default do parâmetro no Animator (Unity cria Float novo
        // com default 0, não 1) — sem isso, o primeiro ataque de cada instância tocaria
        // a 0x de velocidade e travaria parado pra sempre.
        if (animator != null) animator.SetFloat("AttackSpeedMultiplier", 1f);
        AnimatorTrigger("AttackTrigger");
    }

    // Chamado por um Animation Event no frame exato do clipe de ataque em que o golpe
    // conecta (ou o projétil nasce) de verdade — a animação é a fonte de verdade do
    // timing, não um timer.
    public void AnimationHitEvent()
    {
        if (attackAnimState != AttackAnimState.Attacking) return; // proteção — evento chamado fora de hora não faz nada
        // A Attack state é uma Blend Tree 2D Freeform Directional — pra quase qualquer
        // ângulo de mira, 2 clipes diagonais tocam misturados ao mesmo tempo (raramente a
        // mira cai exatamente numa das 4 diagonais puras). Cada clipe carrega seu próprio
        // AnimationHitEvent, e o Animator dispara o evento de TODO clipe com peso > 0 na
        // mistura, não só do dominante — sem essa trava, um golpe só chamava isso 2x (dano
        // dobrado, projétil duplicado sobreposto). attackHitFired já existia mas só era
        // consultado em AccelerateAttack(), nunca aqui.
        if (attackHitFired) return;
        attackHitFired = true;
        ExecuteAttackHit();
    }

    // Chamado por um Animation Event no último frame do clipe de ataque.
    public void AnimationAttackEndEvent()
    {
        if (attackAnimState != AttackAnimState.Attacking) return;
        EndAttackAnimation();
    }

    private void EndAttackAnimation()
    {
        attackAnimState = AttackAnimState.Idle;
        attackSpeedMultiplier = 1f;
        if (animator != null) animator.SetFloat("AttackSpeedMultiplier", 1f);
    }

    // Cada hit recebido durante o próprio golpe acelera a animação em vez de só piscar —
    // GDD/Bestiário (Sprint 16, revisão): "dano não interrompe o ataque" continua valendo,
    // mas precisa de feedback visual real, não silêncio. Passado o teto, resolve na hora.
    private void AccelerateAttack()
    {
        attackSpeedMultiplier += attackSpeedStepPerHit;

        if (attackSpeedMultiplier >= maxAttackSpeedMultiplier)
        {
            if (!attackHitFired) ExecuteAttackHit(); // só golpeia de novo se o Hit Event original ainda não tinha disparado
            EndAttackAnimation();
            return;
        }

        if (animator != null) animator.SetFloat("AttackSpeedMultiplier", attackSpeedMultiplier);
    }

    protected void SetMoving(bool isMoving)
    {
        if (animator != null) animator.SetBool("IsMoving", isMoving);
    }

    // GDD Seção 22: true sempre que o monstro está em combate (perseguindo OU parado
    // esperando o cooldown) — junto com IsMoving, decide se o Animator mostra Walk ou
    // IdleCombat em vez do Idle de patrulha.
    protected void SetInCombat(bool inCombat)
    {
        if (animator != null) animator.SetBool("InCombat", inCombat);
    }

    // Última direção de mira não-nula (rumo ao player) — o GameObject nunca vira, só a
    // sprite muda conforme o Blend Tree, então isso é o único jeito de saber "pra que
    // lado o monstro está olhando" num dado instante (ex.: pra escolher qual dos 4
    // triggers de ataque diagonal checar no golpe real — ver MeleeEnemyController).
    protected Vector2 AimDirection { get; private set; } = Vector2.down;

    // Alimenta MoveX/MoveY — Blend Tree 2D (Freeform Directional) do Walk, direção de
    // movimento crua (pode ser fuga, aproximação, o que for).
    protected void SetMoveDirection(Vector2 direction)
    {
        if (animator == null) return;
        animator.SetFloat("MoveX", direction.x);
        animator.SetFloat("MoveY", direction.y);
    }

    // Alimenta AimX/AimY — Blend Tree 2D (Freeform Directional) do Attack e do
    // IdleCombat, sempre em direção ao player de verdade, independente de pra onde o
    // monstro está se movendo.
    protected void SetAimDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude > 0.0001f) AimDirection = direction;
        if (animator == null) return;
        animator.SetFloat("AimX", direction.x);
        animator.SetFloat("AimY", direction.y);
    }

    [SerializeField] private float floatingTextHeightAdjust = -0.5f; // 🔢 ajuste fino, negativo baixa o texto

    // Topo do sprite, não o pivot bruto — assim o texto flutuante nasce acima da "cabeça"
    // tanto de um Rat pequeno quanto de um Dragon gigante, sem precisar de um offset
    // configurado por monstro. floatingTextHeightAdjust corrige o bounds do sprite (pixel
    // art costuma ter bastante espaço transparente, então o topo "cru" fica alto demais).
    private Vector3 GetFloatingTextSpawnPosition() =>
        spriteRenderer != null
            ? new Vector3(transform.position.x, spriteRenderer.bounds.max.y + floatingTextHeightAdjust, transform.position.z)
            : transform.position;

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        // Aggro instantâneo — sofrer dano põe o monstro em combate na hora, independente
        // de distância/observationRadius. Sem isso, ataques à distância (ex.: leque do
        // Ranger) deixavam o player "pokar" um monstro parado sem ele nunca vir de verdade.
        combatLocked = true;
        isInCombat = true;

        stats.health = HealthSystem.ApplyDamage(stats.health, amount);
        Debug.Log($"[{GetType().Name}] Recebeu {amount} de dano. HP = {stats.health}/{stats.maxHealth}");
        GameEvents.DamageTaken(GetFloatingTextSpawnPosition(), amount);

        if (HealthSystem.IsDead(stats.health))
        {
            Die();
            return;
        }

        // Receber dano != reagir visualmente != interromper uma ação. Comprometido com a
        // animação de ataque de verdade, o dano nunca cancela ela (não existe transição
        // Attack -> Damage) — mas precisa de feedback visual de verdade, não só o flash:
        // cada hit acelera a própria animação do golpe (AccelerateAttack). Fora do ataque,
        // toca a reação normal (DamageTrigger).
        if (attackAnimState == AttackAnimState.Attacking)
        {
            TriggerDamageFlash();
            AccelerateAttack();
        }
        else AnimatorTrigger("DamageTrigger");
    }

    private void TriggerDamageFlash()
    {
        if (spriteRenderer == null) return;
        spriteRenderer.color = Color.white;
        damageFlashTimer = DamageFlashDuration;
    }

    // Timer manual em vez de Coroutine com WaitForSeconds — Coroutine não respeita
    // GameplayGate (decisão da Sprint 13): o flash continuaria contando e revertendo
    // a cor durante a pausa.
    private void UpdateDamageFlash()
    {
        if (damageFlashTimer <= 0f) return;
        damageFlashTimer -= Time.deltaTime;
        if (damageFlashTimer <= 0f && spriteRenderer != null) spriteRenderer.color = spriteOriginalColor;
    }

    // Knockback (heróis, ex.: Barbarian — GDD Seção 17): monstros aqui não usam física
    // (Rigidbody), todo movimento já é por transform.Translate — o empurrão é só mais uma
    // translação, decaindo com o tempo, independente do que a IA/animação estiver fazendo
    // no momento (posição != estado de animação, mesma filosofia do flash de dano acima).
    private Vector2 knockbackVelocity;
    private const float KnockbackDecay = 8f; // 🔢 GDD placeholder — quão rápido o empurrão perde força

    public void ApplyKnockback(Vector2 direction, float force)
    {
        knockbackVelocity = direction.normalized * force;
    }

    private void UpdateKnockback()
    {
        if (knockbackVelocity.sqrMagnitude <= 0.01f) return;
        transform.Translate(knockbackVelocity * Time.deltaTime);
        knockbackVelocity = Vector2.Lerp(knockbackVelocity, Vector2.zero, KnockbackDecay * Time.deltaTime);
    }

    protected virtual void Die()
    {
        isDead = true;
        Debug.Log($"[{GetType().Name}] Morreu — aguardando Animation Event de fim do die...");

        var collider = GetComponent<Collider2D>();
        if (collider != null) collider.enabled = false; // para de bloquear/colidir enquanto o clipe de morte toca

        AnimatorTrigger("DieTrigger");
        GameEvents.EnemyKilled(energyReward);
    }

    // Chamado por um Animation Event no último frame do clipe "die" — a animação é a
    // fonte de verdade do timing: o GameObject só é destruído e o loot só aparece
    // depois que a morte terminou de tocar por completo (GDD Seção 22).
    public void AnimationDieEndEvent()
    {
        if (dieHandled) return; // proteção — evento + timeout de segurança não destroem/dropam duas vezes
        dieHandled = true;

        var lootObj = new GameObject("Loot_MonsterEssence");
        lootObj.transform.position = transform.position;
        var drop = lootObj.AddComponent<LootDrop>();
        drop.loot = new LootDefinition { itemName = "Monster Essence", quantity = monsterEssenceDropAmount };

        Destroy(gameObject);
    }
}
