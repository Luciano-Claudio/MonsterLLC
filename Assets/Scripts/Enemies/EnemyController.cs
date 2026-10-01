using UnityEngine;
using Pathfinding;
using Pathfinding.RVO;

public abstract class EnemyController : MonoBehaviour, IDamageable
{
    public EnemyStats stats = new EnemyStats();
    public int energyReward = 20;
    public int monsterEssenceDropAmount = 1; // quantidade dropada por abate (GDD Seção 38, 🔢 valor de balanceamento pendente)
    public FloorDefinition ownerFloor;

    // Bosses são imunes a knockback (decisão do usuário, Sprint 18→19) — sem isso, ataques
    // com empurrão forte (ex.: golpe do Barbarian) tirariam o boss da própria arena/posição
    // de telegraph, além de trivializar mecânicas de boss pensadas em torno de posição fixa.
    [SerializeField] private bool isBoss = false;

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

    [Header("Debug — só pra visualização em Editor, não afeta gameplay")]
    [SerializeField] private bool showAttackRadiusGizmo = false;
    [SerializeField] private float attackRadiusGizmoOffsetY = 0f; // sobe o centro do gizmo em relação ao pivô — pivô cru costuma ficar nos pés, atrapalha julgar o alcance visualmente

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

    // A* Pathfinding Project (AIPath) — pode não existir em prefabs ainda não migrados/
    // placeholder, por isso "!= null" em todo uso, mesma convenção do animator acima. Quando
    // presente, ele que move o transform de verdade (path + RVO); sem ele, cai no
    // transform.Translate manual de sempre (fallback, ver MoveInDirection).
    protected IAstarAI ai;

    // Ponto de destino projetado à frente na direção desejada, não um waypoint real — os
    // controllers (Melee/Ranged/Slime/Sapper) recalculam a direção a cada frame (perseguir,
    // fugir, vagar), nunca têm um alvo fixo de verdade, então "destino" pro AIPath é só
    // "continue nessa direção"; o valor só precisa ser longe o bastante pra não bater no
    // próprio ponto atual a cada frame.
    private const float AiDestinationLookahead = 4f;

    protected virtual void Awake()
    {
        animator = GetComponent<Animator>(); // pode não existir em prefabs placeholder — sempre checar com "!= null", nunca "?." (ver AnimatorTrigger/SetMoving/SetInCombat/SetMoveDirection)
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) spriteOriginalColor = spriteRenderer.color;
        spawnOrigin = transform.position;
        patrolAI = new PatrolAI(minIdleDuration, maxIdleDuration, minWalkDuration, maxWalkDuration);
        attackAnimationCooldown = new AttackCooldown(stats.attackAnimationCooldown);
        SetMoveDirection(Vector2.down); // direção padrão — sem isso, MoveX/MoveY ficam em (0,0) até o primeiro Move(), deixando o Blend Tree de Idle indefinido por alguns frames
        statusEffectController = GetComponent<StatusEffectController>(); // pode não existir em prefabs placeholder

        ai = GetComponent<IAstarAI>();
        if (ai != null)
        {
            ai.maxSpeed = stats.moveSpeed; // fonte única de verdade continua sendo EnemyStats, não o Inspector do AIPath
            ai.updateRotation = false; // sprite 2D nunca gira o GameObject — só o Blend Tree troca a pose (mesma regra do herói)

            // true — já tentei false (deixar o AIPath só calcular, mover o transform na mão em
            // MoveInDirection), mas isso exigia sincronizar a posição interna dele todo frame
            // via Teleport(), e Teleport() reseta o histórico de movimento (prevPosition1/2 e a
            // velocidade do RVO) a cada chamada — resultado: velocity ficava travado em zero
            // pra sempre, monstro completamente parado. updatePosition=true é o jeito como a
            // biblioteca foi desenhada pra ser usada; o empurrão físico no player que isso causa
            // (via rigid2D.MovePosition) é resolvido à parte, com Linear Damping no Rigidbody2D
            // do herói (qualquer velocidade injetada pela física decai rápido, sem deslizar) —
            // não brigando com o sistema de posição interno do AIPath nem desligando colisão.
            ai.updatePosition = true;
            ai.isStopped = true; // só libera quando MoveInDirection/SetMoving mandar de verdade

            // canSearch nessa versão do pacote é derivado de autoRepath.mode, cujo default é
            // "Never" — sem isso, o AIPath nunca busca path nenhum (hasPath/velocity ficam
            // zerados pra sempre, mesmo com destination setado certinho e a animação de Walk
            // tocando normal, já que animação e path são coisas independentes). Isso não dá
            // pra configurar direto no YAML do prefab (é uma property calculada, não um campo
            // simples), então tem que ser em código.
            ai.canSearch = true;
        }

        // Regra de prioridade RVO (decisão do usuário): quem dá mais dano tem prioridade maior
        // no desvio — os outros cedem espaço pra ele chegar perto do player, em vez de todo
        // mundo competir igual. Função própria (não depende do resto do elenco atual), então
        // continua correta conforme novos monstros forem adicionados no futuro, sem precisar
        // reajustar os já existentes. Casos como o Goblin Sapper (dano real é a bomba, não o
        // stats.attackDamage base) sobrescrevem isso depois de base.Awake() — ver
        // GoblinSapperController.
        var rvo = GetComponent<RVOController>();
        if (rvo != null) rvo.priority = DamageToRvoPriority(stats.attackDamage);
    }

    private const float RvoPriorityReferenceDamage = 25f; // 🔢 dano "alto" de referência — igual/acima disso, satura em prioridade máxima
    private const float RvoPriorityFloor = 0.2f; // 🔢 nem o monstro mais fraco cede espaço pra todo mundo sempre

    protected static float DamageToRvoPriority(float damage) =>
        Mathf.Lerp(RvoPriorityFloor, 1f, Mathf.Clamp01(damage / RvoPriorityReferenceDamage));

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
        TryFindPlayer();
    }

    // Busca "lazy" — antes o Player já existia na cena quando todo monstro rodava Start(), uma
    // tentativa só bastava. Agora que o herói só nasce quando o MainMenuUI instancia ele (depois
    // do Start() de todo monstro já ter rodado), sem retry a referência ficava null pra sempre e
    // o monstro nunca detectava ninguém. Chamado de novo em Update() enquanto ainda não achou.
    private void TryFindPlayer()
    {
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;
    }

    // AIPath/RVOController têm Update/FixedUpdate próprios, fora do controle desse Update()
    // — GameplayGate não usa Time.timeScale (por isso Coroutine não respeita ele, comentário
    // de AnimationDieEndEvent acima), e o Floor Sleep sempre foi "custo zero" pro monstro
    // dormente. Sem isso, migrar pro AIPath reintroduziria custo de path/RVO rodando durante
    // pausa e em Floors dormentes — desligar os componentes de verdade (não só isStopped)
    // nas transições é o que preserva as duas garantias.
    private bool aiSimulating = true;

    protected virtual void Update()
    {
        bool shouldSimulate = GameplayGate.IsActive && FloorActivationCheck.IsActive(ownerFloor, FloorManager.Instance.CurrentFloor);
        if (shouldSimulate != aiSimulating)
        {
            aiSimulating = shouldSimulate;
            SetAiSimulating(shouldSimulate);
        }
        if (!shouldSimulate) return;

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

        if (statusEffectController != null) statusEffectController.Tick(Time.deltaTime);

        if (player == null) TryFindPlayer();
        if (player == null) return;

        // Camuflagem/stealth do herói (GDD Seção 16/17 — Ranger, futuramente Druid/Assassin)
        // — o monstro perde o alvo de verdade (isInCombat=false), não só congela: continua
        // se movendo/tocando a própria animação de patrulha normalmente, só sem saber que o
        // player existe. Precisa redetectar via observationRadius depois que acabar (ver
        // guarda em UpdatePatrol) — não retoma perseguição sozinho quando a camuflagem cai.
        if (HeroController.IsPlayerUntargetable) isInCombat = false;

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
        // !IsPlayerUntargetable aqui é o que faz a camuflagem funcionar de verdade — sem
        // isso, forçar isInCombat=false no Update() não adiantaria nada, porque esse
        // check de distância reativaria o combate sozinho no frame seguinte (o player
        // continua fisicamente perto, só "invisível" pro monstro).
        if (distanceToPlayer <= stats.observationRadius && !HeroController.IsPlayerUntargetable)
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
    // walkStateName existe pra monstros com mais de um estado de "andando" (ex.: Goblin
    // Sapper — "Walk"/"Walk_Bomb" dependendo de IsArmed) — sem isso, essa checagem (o
    // Animator já entrou de fato no estado de Walk, não só a intenção foi marcada) só
    // reconheceria o nome padrão "Walk", travando o monstro na pose andando sem nunca se
    // mover de verdade.
    protected void MoveInDirection(Vector2 direction, string walkStateName = "Walk")
    {
        SetMoveDirection(direction);
        bool canWalk = AnimatorStateCheck.IsInState(animator, walkStateName);

        if (ai != null)
        {
            ai.isStopped = !canWalk;
            if (canWalk)
            {
                ai.destination = transform.position + (Vector3)(direction * AiDestinationLookahead);

                // Chamada direta, não depende do sistema automático de repath (autoRepath) —
                // esse objeto é uma classe aninhada do AIPath que não sobrevive corretamente
                // sendo configurada via YAML de prefab escrito à mão (fica num estado zerado/
                // inválido); SearchPath() é a API pública direta, documentada, sem essa
                // dependência. Throttle manual pra não buscar path todo frame.
                if (Time.time >= nextRepathTime)
                {
                    ai.SearchPath();
                    nextRepathTime = Time.time + RepathInterval;
                }

                // updatePosition=true (Awake) — o próprio AIPath move o transform/Rigidbody2D
                // a cada frame, já com path + desvio do RVO resolvidos.
            }
            return;
        }

        if (canWalk) transform.Translate(direction * stats.moveSpeed * Time.deltaTime);
    }

    private float nextRepathTime;
    private const float RepathInterval = 0.3f; // 🔢 com que frequência recalcula o path — ajustável

    private void SetAiSimulating(bool simulating)
    {
        if (ai == null) return;
        if (!simulating) ai.isStopped = true; // trava o destino também, pra não retomar andando um frame antes do resto acordar
        var aiBehaviour = ai as Behaviour;
        if (aiBehaviour != null) aiBehaviour.enabled = simulating;
        var rvo = GetComponent<RVOController>();
        if (rvo != null) rvo.enabled = simulating; // OnEnable/OnDisable do próprio RVOController já tira/põe o agente no RVOSimulator
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
        // Cobre todo estado "parado de propósito" que nunca chama MoveInDirection de novo
        // depois (colado em attackRadius, emboscada dormente, etc.) — sem isso o AIPath
        // ficaria tentando alcançar o último destino projetado pra sempre, mesmo com o
        // Blend Tree já em Idle/IdleCombat.
        if (ai != null && !isMoving) ai.isStopped = true;
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
    // movimento crua (pode ser fuga, aproximação, o que for). Também alimenta
    // DiagonalMoveX/DiagonalMoveY (DirectionUtility.SnapTo4Diagonals) — todo Blend Tree de
    // Idle/Walk/Damage do Base_Melee/Base_Ranged só tem pose desenhada pras 4 diagonais
    // (sem cardeal real), então usa o par diagonal pra evitar a mesma instabilidade de
    // "flip" perto dos eixos cardeais já corrigida do lado do herói (regra de arquitetura,
    // GDD) — MoveX/MoveY continuam sendo alimentados normalmente, caso algum Blend Tree
    // futuro tenha pose cardeal real e precise do valor cru.
    protected void SetMoveDirection(Vector2 direction)
    {
        if (animator == null) return;
        animator.SetFloat("MoveX", direction.x);
        animator.SetFloat("MoveY", direction.y);

        Vector2 diagonal = DirectionUtility.SnapTo4Diagonals(direction);
        animator.SetFloat("DiagonalMoveX", diagonal.x);
        animator.SetFloat("DiagonalMoveY", diagonal.y);
    }

    // Alimenta AimX/AimY — Blend Tree 2D (Freeform Directional) do Attack e do
    // IdleCombat, sempre em direção ao player de verdade, independente de pra onde o
    // monstro está se movendo. Também alimenta DiagonalAimX/DiagonalAimY, mesmo motivo do
    // DiagonalMoveX/Y acima — usado pelo Attack/IdleCombat de todo Melee (só 4 diagonais,
    // nunca tem pose cardeal real) e pelo IdleCombat de Ranged (o Attack de Ranged tem
    // pose real de 8 direções, então esse continua em AimX/AimY puro).
    protected void SetAimDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude > 0.0001f) AimDirection = direction;
        if (animator == null) return;
        animator.SetFloat("AimX", direction.x);
        animator.SetFloat("AimY", direction.y);

        Vector2 diagonal = DirectionUtility.SnapTo4Diagonals(direction);
        animator.SetFloat("DiagonalAimX", diagonal.x);
        animator.SetFloat("DiagonalAimY", diagonal.y);
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

    // Só pra visualização em Editor (criação/ajuste de monstro) — não roda em build, não afeta
    // gameplay. virtual pra GoblinSapperController (e qualquer outro com gizmo próprio)
    // conseguir somar o próprio desenho por cima via base.OnDrawGizmosSelected().
    protected virtual void OnDrawGizmosSelected()
    {
        if (!showAttackRadiusGizmo) return;
        Vector3 center = transform.position + new Vector3(0, attackRadiusGizmoOffsetY, 0);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(center, stats.attackRadius);
    }

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
        if (isBoss) return;
        knockbackVelocity = direction.normalized * force;
    }

    private void UpdateKnockback()
    {
        if (knockbackVelocity.sqrMagnitude <= 0.01f) return;
        transform.Translate(knockbackVelocity * Time.deltaTime);
        knockbackVelocity = Vector2.Lerp(knockbackVelocity, Vector2.zero, KnockbackDecay * Time.deltaTime);
    }

    // Efeitos Nocivos de dano-ao-longo-do-tempo (GDD Seção 33) — componente compartilhado
    // com HeroController (ver StatusEffectController.cs), não duplicado aqui. Trapped
    // (Seção 33 também) continua à parte, é incapacitação via HeroController.SetTrapped(),
    // não dano ao longo do tempo.
    private StatusEffectController statusEffectController;

    protected virtual void Die()
    {
        isDead = true;
        Debug.Log($"[{GetType().Name}] Morreu — aguardando Animation Event de fim do die...");

        var collider = GetComponent<Collider2D>();
        if (collider != null) collider.enabled = false; // para de bloquear/colidir enquanto o clipe de morte toca

        SetMoving(false); // trava o AIPath (ai.isStopped) — Update() nem chama mais Move()/MoveInDirection depois disso

        // Corrida clássica de Trigger do Animator: se um DamageTrigger de um hit anterior ainda
        // não foi consumido pelo Animator quando o DieTrigger é setado logo em seguida (comum
        // em monstros que levam 2-3 hits pra morrer — cada hit não-letal dispara DamageTrigger,
        // ver TakeDamage()), o Unity pode processar só o DamageTrigger e descartar o DieTrigger
        // sem nunca seguir a transição de morte — o monstro fica preso no destino do Damage
        // (IdleCombat, já que InCombat continua true) pra sempre, só resgatado pelo timeout de
        // segurança (maxDieDuration). AttackTrigger não entra aqui: TakeDamage() nunca o seta
        // (só o início do próprio golpe do monstro faz isso, em outro lugar), então não existe
        // cenário real de corrida com ele nesse ponto.
        if (animator != null) animator.ResetTrigger("DamageTrigger");
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
