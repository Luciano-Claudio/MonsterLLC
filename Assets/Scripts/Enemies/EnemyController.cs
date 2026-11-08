using System.Collections.Generic;
using UnityEngine;
using Pathfinding;
using Pathfinding.RVO;

public abstract class EnemyController : MonoBehaviour, IDamageable
{
    // Faro de Saque (World Perk, Seção 8) — escrito por WorldPerkEffects.Recompute(), mesmo
    // padrão de HeroController.LootMultiplier/PickupRadius (static, 1 Player só no jogo).
    public static float LootDropChanceBonusPercent = 0f;

    // Proteção central contra dano duplicado em ataques de área (bug real: primário do Mage
    // e o ataque do Pet aplicavam 2x). Todo monstro tem 2 Collider2D no mesmo GameObject — um
    // corpo físico (CapsuleCollider2D, sem trigger) e um trigger genérico maior (CircleCollider2D)
    // — então qualquer Physics2D.OverlapXxx/Collider2D.Overlap() sem filtro encontra o MESMO
    // monstro 2x, um por collider. Resolve o array bruto pra uma lista de EnemyController SEM
    // repetição; todo ataque em área (hero, pet, boss) deve passar os resultados crus por aqui
    // ANTES de aplicar dano, em vez de reimplementar esse dedup a cada script novo.
    public static void CollectDistinct(Collider2D[] results, int count, List<EnemyController> output)
    {
        output.Clear();
        for (int i = 0; i < count; i++)
        {
            if (!results[i].CompareTag("Enemy")) continue;
            var enemy = results[i].GetComponent<EnemyController>();
            if (enemy == null || output.Contains(enemy)) continue;
            output.Add(enemy);
        }
    }

    public EnemyStats stats = new EnemyStats();
    public int energyReward = 20;

    // Por instância (ao contrário de GameEvents.OnHealthChanged, que é global/do herói) — a
    // healthbar de cada monstro escuta só a própria, nunca a de outro monstro vizinho.
    public event System.Action<float, float> OnHealthChanged;

    // Drop table do monstro (GDD Seção 38/39, valores reais já no Bestiário por ficha) —
    // substitui o antigo monsterEssenceDropAmount (1 item fixo). Cada entrada rola sua própria
    // chance de forma independente das demais — um abate pode dropar várias ao mesmo tempo.
    [Header("Loot (GDD Seção 38 — cada entrada rola independente das demais)")]
    public List<LootDropEntry> lootDrops = new();

    // Bestiário (Rat People Royalty) — "summons criados por este boss não geram loot". Quem
    // sumona (ex.: RatPeopleThrowProjectile) seta isso false logo após o Instantiate(); todo
    // monstro "de verdade" (spawn normal via WorldPopulationManager) nunca toca nisso, então o
    // default true cobre 100% do resto do jogo sem precisar de mudança.
    public bool dropsLoot = true;

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

    // Sprint 24 (Boss Spawn System) — detecta a transição true -> false da Camuflagem
    // (mesmo padrão de wasUsingSecondaryAbility em HeroController.Update()), só pra saber o
    // instante exato em que ela ACABA de cair (ver Update()).
    private bool wasPlayerUntargetable;

    private enum AttackAnimState { Idle, Attacking }
    private AttackAnimState attackAnimState = AttackAnimState.Idle;
    private float attackAnimElapsed;
    private bool attackHitFired; // já conectou nesse ciclo de ataque? evita golpe duplicado se o teto de aceleração for atingido depois do Hit Event já ter disparado

    // Golpe duplo (Bestiário Andar 4, Acolyte possuído — chicotada dupla do tentáculo):
    // evento/guard SEPARADO do par AnimationHitEvent/attackHitFired acima — não dá pra só
    // duplicar o AnimationHitEvent no clipe, porque o guard dele existe justamente pra
    // ignorar uma 2ª chamada dentro do mesmo golpe (ver comentário em AnimationHitEvent).
    // Monstro com só 1 golpe por ataque nunca dispara esse evento, então isso fica sempre
    // false/sem uso pra eles.
    private bool attackSecondHitFired;

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

    // Exposição read-only pra subclasse que precisa de um Update() próprio rodando por cima
    // do ciclo de combate padrão (ex.: SpiderQueenController, teia automática em paralelo à
    // perseguição) e precisa parar de agir no instante exato em que o monstro morre, sem
    // esperar o próximo frame do Update() base decidir isso por conta própria.
    protected bool IsDead => isDead;

    private bool dieHandled; // evita destruir/dropar loot duas vezes (evento + timeout de segurança)
    protected Animator animator;
    private SpriteRenderer spriteRenderer;
    private Color spriteOriginalColor;
    private float damageFlashTimer;

    // Achado real (console do usuário, 2026-10-25): Animator.SetFloat num parâmetro que o
    // Controller não declara NÃO é um no-op silencioso — loga "Parameter 'X' does not exist"
    // TODO FRAME (mesmo achado já documentado em PetController.cs). A suposição antiga "todo
    // monstro tem esses parâmetros" está errada: Base_Melee_Ambush/Base_Ranged_Ambush/Lich não
    // têm DiagonalMoveX/Y nem DiagonalAimX/Y, RatPeople não tem AimX/Y, e CardinalAimX/Y só
    // existe nos poucos monstros de ataque ortogonal (Warbreed Phalanx e companhia).
    // Cacheado 1x aqui (nunca escanear animator.parameters todo frame em SetMoveDirection/
    // SetAimDirection).
    private bool hasMoveParam;
    private bool hasDiagonalMoveParam;
    private bool hasAimParam;
    private bool hasDiagonalAimParam;
    private bool hasCardinalAimParam;
    // Mesmo achado, parâmetros diferentes (2026-10-27, Divine God — Animator Controller
    // totalmente customizado, boss híbrido que não usa o fluxo genérico de patrulha/combate,
    // nunca teve IsMoving/InCombat cadastrados): SetMoving()/SetInCombat() chamavam
    // animator.SetBool() sem guarda nenhuma, gerando o mesmo spam de "Parameter does not
    // exist" já corrigido pros outros parâmetros (ver comentário acima).
    private bool hasIsMovingParam;
    private bool hasInCombatParam;
    private bool hasAttackSpeedParam;

    private const float DamageFlashDuration = 0.08f;

    // A* Pathfinding Project (AIPath) — pode não existir em prefabs ainda não migrados/
    // placeholder, por isso "!= null" em todo uso, mesma convenção do animator acima. Quando
    // presente, ele que move o transform de verdade (path + RVO); sem ele, cai no
    // transform.Translate manual de sempre (fallback, ver MoveInDirection).
    protected IAstarAI ai;
    private RVOController rvo; // cacheado em Awake — usado também pelo on/off de Simulation Rings, sem GetComponent repetido

    // Ponto de destino projetado à frente na direção desejada, não um waypoint real — os
    // controllers (Melee/Ranged/Slime/Sapper) recalculam a direção a cada frame (perseguir,
    // fugir, vagar), nunca têm um alvo fixo de verdade, então "destino" pro AIPath é só
    // "continue nessa direção"; o valor só precisa ser longe o bastante pra não bater no
    // próprio ponto atual a cada frame.
    private const float AiDestinationLookahead = 4f;

    // 2026-11-01 — Deadline 8.1, IA de perseguição inteligente (planejamento prévio no chat,
    // não é mais o flanco antigo que só ligava com o player parado e vaga cheia — ver
    // GetSmartApproachPoint). 2 ideias por cima do que foi debatido: leadTime escala com
    // distância/velocidade própria em vez de fixo (longe prevê mais, colado quase não prevê,
    // evita "overshoot" quando já está em cima do player); troca de ângulo só quando a opção
    // nova está CLARAMENTE menos cheia que a comitada (histerese — sem isso, o ângulo
    // escolhido oscilaria a cada reavaliação e o monstro "tremeria" em vez de decidir).
    [Header("Perseguição inteligente (predição + anti-aglomeração local)")]
    [SerializeField] private float leadTimeMax = 1.5f; // 🔢 segundos de antecipação máxima, ajustável
    [SerializeField] private float crowdCheckRadius = 2.5f; // 🔢 raio pra contar "vizinho" perto de um ângulo candidato
    private static readonly float[] ApproachAngleOffsetsDeg = { 0f, 60f, -60f, 120f, -120f };
    private readonly Collider2D[] crowdOverlapBuffer = new Collider2D[16];
    private readonly List<EnemyController> crowdResultsBuffer = new List<EnemyController>(16);
    private float committedApproachAngleDeg;
    private bool approachAngleCommitted;
    private float nextApproachReevalTime;
    private const float ApproachReevalInterval = 0.3f; // mesmo ritmo do RepathInterval abaixo — já é o throttle de decisão do resto do arquivo

    // 2026-11-01 — Deadline 8.1, 2ª rodada de IA (3 ideias pedidas depois do teste do aggro
    // global): corte de frente, coesão de bando e checagem de alcançabilidade. Documentadas
    // cada uma no ponto de uso abaixo, não aqui, pra não duplicar comentário.
    [Header("Corte de frente (fração dos monstros mira NA FRENTE do movimento, não na posição previsão)")]
    [SerializeField] private float frontCutterChance = 0.3f; // 🔢 ajustável
    [SerializeField] private float frontCutterMinDistance = 6f; // 🔢 só compensa reposicionar quando ainda há tempo/espaço
    private bool isFrontCutter;

    [Header("Coesão de bando (mesma espécie, puxa levemente pra perto de quem é igual)")]
    [SerializeField] private float packCohesionRadius = 5f; // 🔢 ajustável
    [SerializeField] private float packCohesionStrength = 0.3f; // 🔢 0 desliga, 1 pesa igual ao próprio approach

    private const float ReachabilityToleranceSqr = 1f; // 🔢 ponto a mais de 1 unidade do nó caminhável mais próximo = fora do grafo

    // Ponto que Melee/Ranged devem perseguir no lugar de player.position cru (ver Move() de
    // cada um). ringRadius = 0 (ex.: Melee bem colado) devolve o ponto previsto puro, sem
    // desvio de ângulo — quem chama decide o quanto de "anel" faz sentido pro próprio alcance.
    protected Vector2 GetSmartApproachPoint(float ringRadius)
    {
        Vector2 playerPos = player.position;
        float distance = Vector2.Distance(transform.position, playerPos);
        float leadTime = Mathf.Clamp(distance / Mathf.Max(stats.moveSpeed, 0.01f), 0f, leadTimeMax)
            * Mathf.Clamp01(stats.predictionSkill);
        Vector2 predicted = playerPos + HeroController.Velocity * leadTime;

        if (ringRadius <= 0f) return predicted;

        if (Time.time >= nextApproachReevalTime)
        {
            nextApproachReevalTime = Time.time + ApproachReevalInterval;

            // Corte de frente: em vez de "de onde eu já estou em relação ao ponto previsto"
            // (comportamento normal), uma fração dos monstros — só os que ainda têm distância
            // de sobra pra valer a pena reposicionar — mira o ângulo da PRÓPRIA velocidade do
            // player, ou seja, tenta chegar pela frente de pra onde ele está correndo, não
            // só seguir por trás.
            Vector2 selfFromPredicted = (Vector2)transform.position - predicted;
            float baseAngleDeg;
            if (isFrontCutter && distance > frontCutterMinDistance && HeroController.Velocity.sqrMagnitude > 0.01f)
                baseAngleDeg = Vector2.SignedAngle(Vector2.right, HeroController.Velocity);
            else
                baseAngleDeg = Vector2.SignedAngle(Vector2.right, selfFromPredicted);

            PickLeastCrowdedAngle(predicted, ringRadius, baseAngleDeg);
        }

        return predicted + AngleToOffset(committedApproachAngleDeg) * ringRadius;
    }

    private void PickLeastCrowdedAngle(Vector2 predicted, float ringRadius, float baseAngleDeg)
    {
        Vector2 committedPoint = predicted + AngleToOffset(committedApproachAngleDeg) * ringRadius;
        bool committedStillValid = approachAngleCommitted && IsReachable(committedPoint);
        int committedCount = committedStillValid ? CountNeighbors(committedPoint) : int.MaxValue;

        int bestCount = int.MaxValue;
        float bestAngle = baseAngleDeg;
        bool foundReachable = false;
        for (int i = 0; i < ApproachAngleOffsetsDeg.Length; i++)
        {
            float candidateDeg = baseAngleDeg + ApproachAngleOffsetsDeg[i];
            Vector2 point = predicted + AngleToOffset(candidateDeg) * ringRadius;
            if (!IsReachable(point)) continue; // ex.: ângulo cairia do outro lado de uma parede — descarta antes de comitar

            int count = CountNeighbors(point);
            if (count < bestCount)
            {
                bestCount = count;
                bestAngle = candidateDeg;
                foundReachable = true;
            }
        }

        if (!foundReachable) return; // todos os candidatos fora do grafo caminhável — mantém o ângulo comitado anterior

        // Histerese: só troca se a melhor opção nova tiver claramente menos gente que a
        // comitada atual (ou se a comitada deixou de ser alcançável) — evita trocar de ângulo
        // a cada 0,3s por 1 vizinho de diferença que vai e volta.
        if (!committedStillValid || bestCount < committedCount - 1)
        {
            committedApproachAngleDeg = bestAngle;
            approachAngleCommitted = true;
        }
    }

    private int CountNeighbors(Vector2 point)
    {
        int hitCount = Physics2D.OverlapCircle(point, crowdCheckRadius, ContactFilter2D.noFilter, crowdOverlapBuffer);
        CollectDistinct(crowdOverlapBuffer, hitCount, crowdResultsBuffer);
        int count = 0;
        for (int i = 0; i < crowdResultsBuffer.Count; i++)
            if (crowdResultsBuffer[i] != this) count++;
        return count;
    }

    // A* Pathfinding Project: GetNearest(position) ignora walkability por padrão (NNConstraint
    // None) — por isso o check explícito de node.Walkable aqui, não só node != null. A
    // tolerância cobre o caso do ponto cair fora do grafo de vez (ex.: do outro lado de uma
    // parede, ou fora dos limites do mapa).
    private static bool IsReachable(Vector2 point)
    {
        if (AstarPath.active == null) return true; // sem grafo carregado (cena de teste sem A* configurado) — não bloqueia nada
        var info = AstarPath.active.GetNearest(point);
        if (info.node == null || !info.node.Walkable) return false;
        return ((Vector2)info.position - point).sqrMagnitude <= ReachabilityToleranceSqr;
    }

    // Coesão de bando: puxa levemente a direção final pra perto do centro de massa de
    // vizinhos da MESMA espécie (mesmo prefab — ver SpeciesKey) dentro de packCohesionRadius.
    // Reaproveita a mesma varredura/dedup de CountNeighbors, com raio e propósito diferentes
    // (aqui é "ficar junto", lá é "evitar lotação"). packCohesionStrength=0 sai antes de
    // qualquer OverlapCircle — desliga de graça, sem custo.
    protected Vector2 ApplyPackCohesion(Vector2 baseDirection)
    {
        if (packCohesionStrength <= 0f) return baseDirection;

        int hitCount = Physics2D.OverlapCircle(transform.position, packCohesionRadius, ContactFilter2D.noFilter, crowdOverlapBuffer);
        CollectDistinct(crowdOverlapBuffer, hitCount, crowdResultsBuffer);

        Vector2 centroid = Vector2.zero;
        int sameSpeciesCount = 0;
        for (int i = 0; i < crowdResultsBuffer.Count; i++)
        {
            var other = crowdResultsBuffer[i];
            if (other == this) continue;
            if (SpeciesKey(other.gameObject) != SpeciesKey(gameObject)) continue;
            centroid += (Vector2)other.transform.position;
            sameSpeciesCount++;
        }

        if (sameSpeciesCount == 0) return baseDirection;
        centroid /= sameSpeciesCount;

        Vector2 towardPack = centroid - (Vector2)transform.position;
        if (towardPack.sqrMagnitude < 0.0001f) return baseDirection;

        return (baseDirection.normalized + towardPack.normalized * packCohesionStrength).normalized;
    }

    // Heurística: o projeto não tem um campo de "espécie" dedicado — Rat/Wolf/Bat etc. são
    // todos a mesma classe (MeleeEnemyController/RangedEnemyController), só prefabs
    // diferentes. O nome do GameObject instanciado é "NomeDoPrefab(Clone)", então tirar o
    // sufixo já separa espécies corretamente sem precisar adicionar um campo novo em todo
    // prefab do Bestiário. Se algum dia um prefab for renomeado sem bater com a espécie
    // real, isso para de funcionar — trocar por um campo explícito nesse caso.
    private static string SpeciesKey(GameObject go)
    {
        const string cloneSuffix = "(Clone)";
        string n = go.name;
        return n.EndsWith(cloneSuffix) ? n.Substring(0, n.Length - cloneSuffix.Length) : n;
    }

    private static Vector2 AngleToOffset(float angleDeg)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
    }

    protected virtual void Awake()
    {
        animator = GetComponent<Animator>(); // pode não existir em prefabs placeholder — sempre checar com "!= null", nunca "?." (ver AnimatorTrigger/SetMoving/SetInCombat/SetMoveDirection)
        hasMoveParam = HasParameter("MoveX");
        hasDiagonalMoveParam = HasParameter("DiagonalMoveX");
        hasAimParam = HasParameter("AimX");
        hasDiagonalAimParam = HasParameter("DiagonalAimX");
        hasCardinalAimParam = HasParameter("CardinalAimX");
        hasIsMovingParam = HasParameter("IsMoving");
        hasInCombatParam = HasParameter("InCombat");
        hasAttackSpeedParam = HasParameter("AttackSpeedMultiplier");
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) spriteOriginalColor = spriteRenderer.color;
        spawnOrigin = transform.position;
        patrolAI = new PatrolAI(minIdleDuration, maxIdleDuration, minWalkDuration, maxWalkDuration);
        attackAnimationCooldown = new AttackCooldown(stats.attackAnimationCooldown);
        SetMoveDirection(Vector2.down); // direção padrão — sem isso, MoveX/MoveY ficam em (0,0) até o primeiro Move(), deixando o Blend Tree de Idle indefinido por alguns frames
        statusEffectController = GetComponent<StatusEffectController>(); // pode não existir em prefabs placeholder
        isFrontCutter = Random.value < frontCutterChance; // sorteado 1x por instância — fixo pela vida do monstro, não reavaliado a cada frame

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
        rvo = GetComponent<RVOController>(); // cacheado (campo, não var local) — Simulation Rings liga/desliga a cada frame, sem GetComponent repetido
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
    // de AnimationDieEndEvent acima). Sem isso, migrar pro AIPath reintroduziria custo de
    // path/RVO rodando durante pausa — desligar os componentes de verdade (não só isStopped)
    // na transição é o que preserva essa garantia.
    //
    // [Limpeza sistema de Floor] "Floor Sleep" (gate extra por FloorActivationCheck/ownerFloor)
    // foi removido — decisão já tomada na Fase A do spawn de monstro do mundo aberto
    // (WorldPopulationManager): todo monstro comum sempre simula em full, sem redução por
    // distância/zona; o despawn por distância (WorldPopulationManager) já limita quantos
    // ficam vivos ao mesmo tempo. Simulation Rings (abaixo) cobre a redução de custo por
    // distância sem despawnar — ver docs/new/Plano_Spawn_Monstros_Mundo_Aberto.md.
    private bool aiSimulating = true;

    [Header("Simulation Rings (otimização de custo por distância do jogador)")]
    [Tooltip("Dentro deste raio do jogador, simulação completa (igual sempre foi) — todo frame. Além dele (mas ainda vivo, dentro do despawn por distância de WorldPopulationManager/WorldBossSpawnManager), a decisão de IA (patrulha/combate/ataque) passa a rodar com frequência reduzida (ver midRingUpdateInterval) e o RVOController é desligado — desvio fino entre monstros só importa aglomerado perto do jogador.")]
    [SerializeField] private float simulationNearRingRadius = 15f; // 🔢 ajustável
    [Tooltip("Com que intervalo (segundos) a IA do monstro é reavaliada quando está fora do anel próximo. AIPath continua se movendo normalmente nesse meio tempo em direção ao último destino setado — só a DECISÃO (pra onde ir, quando atacar) fica mais espaçada, imperceptível a essa distância.")]
    [SerializeField] private float midRingUpdateInterval = 0.3f; // 🔢 ajustável
    private float midRingAccumulator;

    protected virtual void Update()
    {
        bool shouldSimulate = GameplayGate.IsActive;
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

        // Sprint 23 (Cleric) — paralisia da Oração é direto o Efeito WordOfPain estar ativo,
        // sem flag própria: incapacitação total (IA/ataque/movimento travam) exatamente
        // enquanto o Efeito durar, sem risco de desincronizar com o fim dele. Dano/outros
        // Efeitos Nocivos continuam normais (Tick() acima já rodou antes desse return).
        if (statusEffectController != null && statusEffectController.IsEffectActive(StatusEffectType.WordOfPain))
        {
            SetMoving(false); // trava o AIPath enquanto durar, mesmo critério de "parado de propósito"
            return;
        }

        if (player == null) TryFindPlayer();
        if (player == null) return;

        // Simulation Rings — perto do jogador, simula em full (todo frame, igual sempre foi).
        // Mais longe, desliga o RVOController (barato de checar todo frame, mesmo sem
        // throttle) e reduz a FREQUÊNCIA da decisão de IA abaixo (nunca desliga — o monstro
        // continua vivo, patrulhando/perseguindo, só reavaliado com menos frequência).
        //
        // GDD Deadline 8.1, Seção 4 — "Simulation Rings só valem pra quem NÃO tem target":
        // isInCombat=true sempre conta como "near ring", não importa a distância real — um
        // monstro com target não pode perseguir em câmera lenta só porque ficou longe.
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        bool isNearRing = distanceToPlayer <= simulationNearRingRadius || isInCombat;
        if (rvo != null) rvo.enabled = isNearRing;

        float decisionDeltaTime;
        if (isNearRing)
        {
            decisionDeltaTime = Time.deltaTime;
        }
        else
        {
            midRingAccumulator += Time.deltaTime;
            if (midRingAccumulator < midRingUpdateInterval) return; // nem chegou a hora do próximo tick reduzido — dano/knockback/DoT acima já rodaram normalmente mesmo assim
            decisionDeltaTime = midRingAccumulator; // tempo acumulado de verdade, não só o deltaTime deste frame — senão cooldowns/timers internos (ex.: AttackCooldown, PatrolAI) andariam em câmera lenta longe do jogador
            midRingAccumulator = 0f;
        }

        // Camuflagem/stealth do herói (GDD Seção 16/17 — Ranger, futuramente Druid/Assassin) —
        // o monstro comum perde o alvo de verdade (isInCombat=false), não só congela: continua
        // se movendo/tocando a própria animação de patrulha normalmente, só sem saber que o
        // player existe.
        //
        // GDD Deadline 8.1, Seção 4.2 — boss (nunca summon de boss, decisão do usuário) ignora
        // isso por completo pra ESCOLHA DE ALVO: &&!isBoss aqui faz isPlayerUntargetable nunca
        // ser true do ponto de vista de um boss, então ele nunca solta o alvo só por causa de
        // invisibilidade/camuflagem do herói (não remove a imunidade a dano desses estados,
        // só a escolha de alvo — se o boss acertar o jogador por outro meio, o dano continua
        // bloqueado normalmente).
        //
        // Seção 4 — "o target volta sozinho quando a forma normal retorna, sem novo gatilho":
        // qualquer monstro (não só boss) recupera automaticamente se o aggro global já estava
        // ativo ANTES da invisibilidade começar — senão, continua precisando de um gatilho real
        // (proximidade/hit) como qualquer outro monstro dormente, via UpdatePatrol() abaixo.
        bool isPlayerUntargetable = HeroController.IsPlayerUntargetable && !isBoss;
        if (isPlayerUntargetable) isInCombat = false;
        else if (wasPlayerUntargetable && HeroController.GlobalAggroActive) ForceImmediateAggro();
        wasPlayerUntargetable = isPlayerUntargetable;

        if (hasAttackAnimation) attackAnimationCooldown.Tick(decisionDeltaTime);

        if (attackAnimState == AttackAnimState.Attacking)
        {
            // Comprometido com a animação de ataque de verdade — não persegue, não
            // reinicia outro ataque no meio dela.
            attackAnimElapsed += decisionDeltaTime;
            if (attackAnimElapsed >= maxAttackAnimationDuration)
            {
                Debug.LogWarning($"[{GetType().Name}] AnimationAttackEndEvent nunca chegou — forçando fim do ataque (verifique o Animator Controller).");
                EndAttackAnimation();
            }
            return;
        }

        if (!isInCombat) UpdatePatrol(decisionDeltaTime);
        else UpdateCombat(decisionDeltaTime);
    }

    // GDD Seção 22: antes de detectar o jogador, alterna idle/walk aleatoriamente. A
    // detecção troca pra combate imediatamente — quem garante que o "idle" de patrulha
    // não corta visualmente no meio é só o Exit Time da transição no Animator
    // (Idle -> Walk / Idle -> IdleCombat), não um atraso aqui no código. Um atraso
    // code-side chegou a existir aqui, mas usava o timer da fase de patrulha inteira
    // (até maxWalkDuration, vários segundos) em vez do tamanho real do clipe — o monstro
    // ficava "ignorando" o jogador por tempo demais. Removido.
    private void UpdatePatrol(float deltaTime)
    {
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        // !IsPlayerUntargetable aqui é o que faz a camuflagem funcionar de verdade — sem
        // isso, forçar isInCombat=false no Update() não adiantaria nada, porque esse
        // check de distância reativaria o combate sozinho no frame seguinte (o player
        // continua fisicamente perto, só "invisível" pro monstro).
        if (distanceToPlayer <= stats.observationRadius && !HeroController.IsPlayerUntargetable)
        {
            isInCombat = true;
            HeroController.GlobalAggroActive = true; // GDD Deadline 8.1, Seção 4 — 1 detectou, todos perseguem (target global e independente de hit)
            return;
        }

        // Aggro global de verdade (Seção 4, fechado no R4) — o bloco acima liga o flag no 1º
        // monstro que detectar por conta própria (ou TakeDamage, qualquer hit); a partir daí,
        // todo o resto do mundo também persegue, inclusive quem normalmente ficaria dormente
        // esperando (bloco staysDormantUntilDetected abaixo).
        if (HeroController.GlobalAggroActive && !HeroController.IsPlayerUntargetable)
        {
            ForceImmediateAggro();
            return;
        }

        // !IsPlayerUntargetable aqui também — sem isso, um monstro de emboscada volta pra pose
        // dormente SEMPRE que isInCombat vira false, mesmo quando o motivo é só
        // camuflagem/stealth (Ranger/Assassin), não falta de detecção real. Sprint 29 (Assassin):
        // durante stealth, ele deve continuar andando/parado normalmente (patrolAI.Tick logo
        // abaixo), nunca travar na pose estática — mesmo bug já existia pra camuflagem do
        // Ranger, só nunca tinha sido flagrado.
        if (staysDormantUntilDetected && !HeroController.IsPlayerUntargetable)
        {
            // Nunca vaga sozinho — só a pose parada (idle estático) até detectar.
            SetMoving(false);
            SetInCombat(false);
            return;
        }

        patrolAI.Tick(deltaTime, () => spawnOrigin + Random.insideUnitCircle * patrolRadius);

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

        // Fallback sem AIPath (prefab placeholder não migrado) — usa Time.deltaTime puro, não
        // o deltaTime (possivelmente acumulado) do chamador: Simulation Rings no anel médio
        // chamaria isso só 1x a cada midRingUpdateInterval, movendo em saltos em vez de
        // suave. Não é um problema de verdade hoje porque todo monstro real já tem AIPath
        // (só placeholder cai aqui) — documentado, não corrigido, pra não encadear mais uma
        // mudança de assinatura (MoveInDirection) por um caminho sem uso real no momento.
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
        if (rvo != null) rvo.enabled = simulating; // OnEnable/OnDisable do próprio RVOController já tira/põe o agente no RVOSimulator — Simulation Rings pode religar isso em seguida se o monstro estiver no anel médio (ver Update())
    }

    // Emboscada (staysDormantUntilDetected): true depois que o Animator já saiu do estado
    // "Activate" pela 1ª vez — nunca mais reavaliado depois disso (o monstro não volta a
    // dormir, ver UpdateCombat).
    private bool hasActivated;
    private bool activateAimCaptured;
    private Vector2 activateAimSnapshot;

    private void UpdateCombat(float deltaTime)
    {
        // Decisão do usuário (2026-10-26, revertendo a exceção anterior): monstro de
        // emboscada (staysDormantUntilDetected) voltava pra pose dormente sozinho quando o
        // player saía do observationRadius — igual todo resto do jogo, depois de detectar uma
        // vez, persegue pra sempre, sem desistir por distância.
        SetInCombat(true);
        // Direção de movimento != direção de mira: o Ranged foge do player (MoveX/MoveY
        // aponta pra longe dele), mas continua precisando "olhar" pro player enquanto
        // ataca/segura posição — daí o par separado (AimX/AimY), sempre recalculado em
        // direção à posição real do player, independente de Move() estar fugindo,
        // aproximando ou parado.
        Vector2 toPlayer = player.position - transform.position;

        // Bug real reportado pelo usuário (2026-10-26, Andar 5): com a mira recalculada
        // todo frame (padrão normal), um monstro de emboscada com Activate em Blend Tree
        // direcional (ex.: Gargoyle, 4 clipes NE/NW/SE/SW) podia trocar de clipe-filho NO
        // MEIO da própria animação de ativação — cada filho do Blend Tree tem sua própria
        // linha do tempo, então trocar de filho parecia reiniciar a animação do zero antes
        // de pular pro Walk. Trava a mira no valor capturado no instante da detecção
        // enquanto o Animator ainda estiver de fato no estado "Activate" (1x só, depois
        // nunca mais — hasActivated).
        if (staysDormantUntilDetected && !hasActivated)
        {
            if (AnimatorStateCheck.IsInState(animator, "Activate"))
            {
                if (!activateAimCaptured)
                {
                    activateAimSnapshot = toPlayer.normalized;
                    activateAimCaptured = true;
                }
                SetAimDirection(activateAimSnapshot);
            }
            else
            {
                hasActivated = true;
                SetAimDirection(toPlayer.normalized);
            }
        }
        else
        {
            SetAimDirection(toPlayer.normalized);
        }

        Move(deltaTime);
        if (hasAttackAnimation) TryStartAttackAnimation();
    }

    // deltaTime recebido (não Time.deltaTime direto) — Simulation Rings chama isso com o
    // tempo ACUMULADO quando o monstro está fora do anel próximo (reavaliação menos
    // frequente, não desligada); cooldowns/timers internos de cada implementação (ex.:
    // flankAI do Melee, biteCooldown da Spider Queen) precisam desse valor pra não andar em
    // câmera lenta longe do jogador.
    protected abstract void Move(float deltaTime); // implementado por Melee (gruda no contato) e Ranged (mantém alcance, foge se o player chegar perto demais)
    protected abstract void ExecuteAttackHit(); // golpe/disparo real — chamado pelo AnimationHitEvent, no frame exato em que a animação conecta
    protected abstract bool InAttackRange(); // define o alcance que autoriza iniciar a animação de ataque (mesmo raio usado pelo contato/disparo)

    private void TryStartAttackAnimation()
    {
        if (!InAttackRange()) return;
        if (!attackAnimationCooldown.TryConsume()) return;

        attackAnimState = AttackAnimState.Attacking;
        attackAnimElapsed = 0f;
        attackHitFired = false;
        attackSecondHitFired = false;
        attackSpeedMultiplier = 1f;
        // Não confiar no valor default do parâmetro no Animator (Unity cria Float novo
        // com default 0, não 1) — sem isso, o primeiro ataque de cada instância tocaria
        // a 0x de velocidade e travaria parado pra sempre.
        if (animator != null && hasAttackSpeedParam) animator.SetFloat("AttackSpeedMultiplier", 1f);
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

    // 2ª conexão do mesmo golpe (ex.: chicotada dupla do Acolyte possuído) — Animation Event
    // à parte, colocado no 2º frame de conexão real do clipe. Guard próprio (attackSecondHitFired),
    // pelo mesmo motivo do guard de AnimationHitEvent: a Attack é Blend Tree 2D Freeform
    // Directional, então o Animator dispara o evento de todo clipe com peso > 0 na mistura.
    public void AnimationSecondHitEvent()
    {
        if (attackAnimState != AttackAnimState.Attacking) return;
        if (attackSecondHitFired) return;
        attackSecondHitFired = true;
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
        if (animator != null && hasAttackSpeedParam) animator.SetFloat("AttackSpeedMultiplier", 1f);
    }

    // GDD Deadline 8.1, Seção 4.1 — chamado só por WorldBossSpawnManager antes de teleportar um
    // boss que passou de bossDespawnDistance (substitui o antigo Destroy+respawn fresco).
    // Cancela qualquer golpe telegrafado em andamento ANTES do salto de posição — sem isso, o
    // Animator ficaria preso tocando a animação de ataque antiga numa posição que não existe
    // mais. Seguro de chamar fora de hora: EndAttackAnimation() só zera estado, não dispara
    // nenhum efeito colateral de "golpe concluído".
    public void CancelAttackForTeleport()
    {
        if (attackAnimState == AttackAnimState.Attacking) EndAttackAnimation();
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

        if (animator != null && hasAttackSpeedParam) animator.SetFloat("AttackSpeedMultiplier", attackSpeedMultiplier);
    }

    protected void SetMoving(bool isMoving)
    {
        if (animator != null && hasIsMovingParam) animator.SetBool("IsMoving", isMoving);
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
        if (animator != null && hasInCombatParam) animator.SetBool("InCombat", inCombat);
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
    private bool HasParameter(string paramName)
    {
        if (animator == null) return false;
        foreach (var p in animator.parameters)
        {
            if (p.name == paramName) return true;
        }
        return false;
    }

    protected void SetMoveDirection(Vector2 direction)
    {
        if (animator == null) return;
        if (hasMoveParam)
        {
            animator.SetFloat("MoveX", direction.x);
            animator.SetFloat("MoveY", direction.y);
        }

        if (hasDiagonalMoveParam)
        {
            Vector2 diagonal = DirectionUtility.SnapTo4Diagonals(direction);
            animator.SetFloat("DiagonalMoveX", diagonal.x);
            animator.SetFloat("DiagonalMoveY", diagonal.y);
        }
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
        if (hasAimParam)
        {
            animator.SetFloat("AimX", direction.x);
            animator.SetFloat("AimY", direction.y);
        }

        if (hasDiagonalAimParam)
        {
            Vector2 diagonal = DirectionUtility.SnapTo4Diagonals(direction);
            animator.SetFloat("DiagonalAimX", diagonal.x);
            animator.SetFloat("DiagonalAimY", diagonal.y);
        }

        // CardinalAimX/Y — só existe como parâmetro nos controllers de monstro cujo ataque não
        // tem pose diagonal real (ex.: Warbreed Phalanx, controller próprio — ver
        // DirectionUtility.SnapTo4Cardinals).
        if (hasCardinalAimParam)
        {
            Vector2 cardinal = DirectionUtility.SnapTo4Cardinals(direction);
            animator.SetFloat("CardinalAimX", cardinal.x);
            animator.SetFloat("CardinalAimY", cardinal.y);
        }
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

    // `isDirectPlayerHit` (default true, Game Feel) — false só pras fontes de dano repetido/
    // passivo (DoT via StatusEffectController, áreas "grounded"/pulse tick do Mage/Ranger/
    // Rogue, dano de Pet) — HitFeelController usa isso pra só fazer shake/zoom no hit direto
    // do próprio jogador (ex.: impacto de projétil, explosão de habilidade), nunca em tick
    // repetido. Não afeta dano/combate/floating text, só o filtro de juice.
    public virtual void TakeDamage(float amount, bool isDirectPlayerHit = true)
    {
        if (isDead) return;

        // Aggro instantâneo — sofrer dano põe o monstro em combate na hora, independente
        // de distância/observationRadius. Sem isso, ataques à distância (ex.: leque do
        // Ranger) deixavam o player "pokar" um monstro parado sem ele nunca vir de verdade.
        isInCombat = true;

        // Achado real (usuário testando com o Cleric): isInCombat acima só agroa ESTE monstro —
        // o gatilho GLOBAL (Deadline 8.1, Seção 4, "qualquer trigger agrega todos de uma vez")
        // só existia no caminho de detecção por proximidade do Update() abaixo, nunca aqui. Com
        // herói corpo-a-corpo isso passava despercebido (atacar de perto também cai dentro do
        // observationRadius de algum monstro por acidente) — com alcance/AoE que acerta monstro
        // de longe, nada disparava o global. Levar um hit de qualquer herói agora conta como
        // gatilho também, igual já valia pra proximidade.
        HeroController.GlobalAggroActive = true;

        stats.health = HealthSystem.ApplyDamage(stats.health, amount);
        OnHealthChanged?.Invoke(stats.health, stats.maxHealth);
        GameEvents.DamageTaken(GetFloatingTextSpawnPosition(), amount, isPlayer: false, isDirectPlayerHit);

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
        // DamageTrigger é uma transição Any State sem Exit Time (Base_Melee/Base_Ranged) —
        // refirar enquanto JÁ está no estado Damage reinicia o clipe do zero a cada hit, em
        // vez de deixar ele terminar. Dano repetido rápido (ex.: rastro/Fire da Ultimate do
        // Mage) prendia o monstro no Damage pra sempre: MoveInDirection() só move de verdade
        // quando o Animator confirma o estado "Walk", então ele nunca escapava pra sair da
        // área de dano — só acumulava mais hits. Ainda pisca branco em todo hit (feedback
        // visual continua), só não reinicia a pose enquanto já está reagindo a uma anterior.
        else if (AnimatorStateCheck.IsInState(animator, "Damage")) TriggerDamageFlash();
        else AnimatorTrigger("DamageTrigger");
    }

    // Orc Shaman (Bestiário Andar 3) — primeiro caso de cura em monstro (Totem de Heal).
    // Mesmo texto flutuante verde que o herói já usa (GameEvents.OnHealReceived é genérico,
    // só recebe posição/valor, nunca assumiu "sempre é o herói").
    public void Heal(float amount)
    {
        if (isDead) return;

        stats.health = HealthSystem.ApplyHeal(stats.health, amount, stats.maxHealth);
        OnHealthChanged?.Invoke(stats.health, stats.maxHealth);
        GameEvents.HealReceived(GetFloatingTextSpawnPosition(), amount);
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

    // Sprint 24 (Boss Spawn System) — permite ao BossSpawnManager marcar o boss sem
    // depender do Inspector de cada prefab já vir configurado certo.
    public void SetIsBoss(bool value) => isBoss = value;
    public bool IsBoss => isBoss;

    // Regra do usuário: quem spawna decide o container — monstro comum manda pra ENEMYS,
    // boss manda pra BOSSES, independente do que está sendo spawnado (summon, projétil,
    // bomba etc.). Resolve pelo IsBoss REAL da instância que está spawnando, não por
    // suposição de qual andar/ficha do Bestiário ela pertence.
    public static Transform ContainerFor(bool fromBoss) => fromBoss ? BossesContainer : EnemysContainer;

    // Sprint 24 (Boss Spawn System) — decisão do usuário: todo boss nasce já em combate,
    // mesmo longe do jogador, sem fase de patrulha/idle esperando observationRadius
    // detectar (diferente de todo monstro comum). Chamado pelo BossSpawnManager logo após
    // Instantiate(), e de novo internamente quando a Camuflagem cai (ver Update()). Também
    // chamado por WorldPopulationManager.SpawnOne() em retry (GDD Deadline 8.1, Seção 4) e por
    // summons que já nascem "na cara do player" (Lich, Rat People, etc.).
    //
    // Também liga HeroController.GlobalAggroActive — qualquer gatilho forçado de target (boss
    // nascendo, retry, summon) conta como gatilho global de verdade, igual proximidade/hit
    // (Seção 4: "qualquer gatilho... todos os monstros pegam target ao mesmo tempo"); é também
    // o que faz o relógio do dia começar quase na hora num retry, sem precisar repetir esta
    // linha em cada call site.
    public void ForceImmediateAggro()
    {
        isInCombat = true;
        HeroController.GlobalAggroActive = true;
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

        var collider = GetComponent<Collider2D>();
        if (collider != null) collider.enabled = false; // para de bloquear/colidir enquanto o clipe de morte toca

        SetMoving(false); // trava o AIPath (ai.isStopped) — Update() nem chama mais Move()/MoveInDirection depois disso

        // Corrida clássica de Trigger do Animator: qualquer trigger ainda não consumido pelo
        // Animator no instante em que o DieTrigger é setado pode "vencer" a corrida e fazer a
        // Unity descartar o DieTrigger sem nunca seguir a transição de morte — preso no
        // destino do outro trigger pra sempre, só resgatado pelo timeout de segurança
        // (maxDieDuration). Achava que só DamageTrigger entrava nisso (comum em monstros que
        // levam 2-3 hits — cada hit não-letal dispara DamageTrigger, ver TakeDamage()), mas
        // bug real reportado pelo usuário (2026-10-26, intermitente — "às vezes"): o golpe
        // fatal pode acontecer bem no instante em que o PRÓPRIO monstro acabou de setar seu
        // AttackTrigger (ou o trigger de ação de um híbrido — ThrowTrigger do Rat People
        // Royalty, WebTrigger/BiteTrigger da Spider Queen, IceClawsTrigger/ReanimationTrigger
        // do Lich, etc.), ainda não consumido. Em vez de resetar por nome (lista que cresce a
        // cada monstro novo), reseta TODO trigger do Animator genericamente.
        if (animator != null)
        {
            foreach (var param in animator.parameters)
            {
                if (param.type == AnimatorControllerParameterType.Trigger) animator.ResetTrigger(param.name);
            }
        }
        AnimatorTrigger("DieTrigger");
        GameEvents.EnemyKilled(energyReward, isBoss);
    }

    // Chamado por um Animation Event no último frame do clipe "die" — a animação é a
    // fonte de verdade do timing: o GameObject só é destruído e o loot só aparece
    // depois que a morte terminou de tocar por completo (GDD Seção 22).
    // virtual a partir da Sprint 24 — MotherSlimeController precisa rodar código (spawnar
    // os 3 filhotes) no momento exato da morte, antes do Destroy(gameObject) deste método.
    // Nenhum outro monstro sobrescreve isso hoje — zero mudança de comportamento pra eles.
    public virtual void AnimationDieEndEvent()
    {
        if (dieHandled) return; // proteção — evento + timeout de segurança não destroem/dropam duas vezes
        dieHandled = true;

        // Bestiário (Rat People Royalty): "summons criados por este boss não geram loot".
        if (dropsLoot) DropLoot();

        Destroy(gameObject);
    }

    // Cacheado 1x (Resources.Load não é caro, mas também não precisa repetir a cada abate) —
    // prefab em Assets/Resources/Loot/LootDrop.prefab. static: mesmo prefab serve pra qualquer
    // monstro/boss, não precisa de 1 referência serializada em cada um dos 21 prefabs.
    private static GameObject lootDropPrefab;
    private static Transform lootContainer; // GameObject "//LOOT" na cena — criado na hora se ninguém atribuir um aqui, mesmo padrão de GameplayBootstrap.herosContainer

    // GameObject "//ENEMYS" na cena — container único pra TODO spawn de monstro comum,
    // inclusive os que nascem de outro monstro morrendo (Mother Slime, Rat People Royalty,
    // EnemyProjectile) e não só o spawn normal do WorldPopulationManager. Mesmo padrão
    // find-or-create de GameplayBootstrap.herosContainer/lootContainer acima, centralizado
    // aqui pra não duplicar a mesma lógica em cada script que sumona monstro.
    private static Transform enemysContainer;
    public static Transform EnemysContainer
    {
        get
        {
            if (enemysContainer == null)
            {
                var existing = GameObject.Find("//ENEMYS");
                enemysContainer = existing != null ? existing.transform : new GameObject("//ENEMYS").transform;
            }
            return enemysContainer;
        }
    }

    // GameObject "//BOSSES" na cena — mesmo container onde o boss normal já nasce
    // (WorldBossSpawnManager.SpawnOneBoss). Summon de boss nasce aqui também (nasce no mesmo
    // lugar do "pai") — ex.: os Rat People arremessados pela Rat People Royalty (um boss),
    // diferente dos Rat People que nascem de um monstro comum, que vão pra EnemysContainer.
    private static Transform bossesContainer;
    public static Transform BossesContainer
    {
        get
        {
            if (bossesContainer == null)
            {
                var existing = GameObject.Find("//BOSSES");
                bossesContainer = existing != null ? existing.transform : new GameObject("//BOSSES").transform;
            }
            return bossesContainer;
        }
    }

    // GDD Deadline 8.1, Seção 3 — achado real do usuário: GameEvents.ClearAllEnemies() só
    // limpa a lista interna que WorldPopulationManager/WorldBossSpawnManager rastreiam (os
    // monstros que ELES spawnaram) — qualquer outra coisa dentro de ENEMYS/BOSSES nascida por
    // fora desse caminho (ex.: projétil de ataque de monstro, tipo a tocha arremessada do
    // Goblin Raider) nunca entra nessa lista, então nunca era destruída — ficava congelada na
    // cena pra sempre (GameplayGate desliga o próprio Update() dela antes de se autodestruir).
    // Destrói literalmente TUDO dentro dos 2 containers, sem exceção — é reset de dia inteiro
    // (morte ou sucesso), não um despawn seletivo.
    public static void DestroyAllSpawned()
    {
        DestroyAllChildren(EnemysContainer);
        DestroyAllChildren(BossesContainer);
    }

    private static void DestroyAllChildren(Transform container)
    {
        for (int i = container.childCount - 1; i >= 0; i--)
        {
            Destroy(container.GetChild(i).gameObject);
        }
    }

    // "Cada tipo de loot realiza sua própria rolagem de chance, independentemente das demais"
    // (GDD Seção 38) — testa CADA entrada do drop table separadamente, nunca escolhe só 1 item
    // da lista. Um abate pode dropar vários materiais diferentes ao mesmo tempo (ou nenhum, se
    // todas as rolagens falharem — só acontece em entradas com chance < 100%, já que Monster
    // Essence de todo monstro comum é sempre 100% no Bestiário).
    private void DropLoot()
    {
        if (lootDropPrefab == null) lootDropPrefab = Resources.Load<GameObject>("Loot/LootDrop");
        if (lootContainer == null)
        {
            var existing = GameObject.Find("//LOOT");
            lootContainer = existing != null ? existing.transform : new GameObject("//LOOT").transform;
        }

        foreach (var entry in lootDrops)
        {
            if (string.IsNullOrEmpty(entry.itemName)) continue;
            // Faro de Saque (World Perk, Seção 8) — bônus ADITIVO em cima do % próprio de cada
            // entrada (nunca multiplicativo: dobrar uma chance de 100% não faria sentido).
            if (Random.value * 100f >= entry.dropChancePercent + LootDropChanceBonusPercent) continue;

            // Random.Range(int,int) não serve mais — minQuantity/maxQuantity são long (Tier 10
            // do Bestiário já passa de 2,1 bilhões por drop), e Random.Range não tem overload
            // pra long. RollQuantity() abaixo resolve isso sem perder a precisão do float em
            // ranges gigantes.
            long baseQuantity = RollQuantity(entry.minQuantity, entry.maxQuantity);
            // HeroController.LootMultiplier — passiva de sorte do Gunslinger (Sprint 28, GDD
            // Seção 17.8), default 1 = sem efeito em nenhum outro herói. Aplicado em TODO
            // material, não só Monster Essence. Math.Round em double (não Mathf.RoundToInt,
            // que devolve int e reintroduziria o mesmo teto de 2,1 bilhões aqui).
            long quantity = (long)System.Math.Round(baseQuantity * (double)HeroController.LootMultiplier);
            if (quantity <= 0) continue;

            var lootObj = Instantiate(lootDropPrefab, transform.position, Quaternion.identity, lootContainer);
            lootObj.name = $"Loot_{entry.itemName}";
            var drop = lootObj.GetComponent<LootDrop>();
            drop.loot = new LootDefinition { itemName = entry.itemName, quantity = quantity };
        }
    }

    // Substitui Random.Range(int,int) pra ranges que passam do int32 (ver DropLoot acima) —
    // Random.value sozinho (float) já tem mais de 24 bits de mantissa, sobra precisão de
    // sobra pra um roll de loot (ninguém nota a diferença entre cair em 50.000.000.012 ou
    // 50.000.000.780 — o LargeNumberFormatter arredonda pra "50b" de qualquer jeito).
    private static long RollQuantity(long minInclusive, long maxInclusive)
    {
        if (maxInclusive <= minInclusive) return minInclusive;

        double range = (double)(maxInclusive - minInclusive) + 1.0;
        long offset = (long)(Random.value * range);
        if (offset >= range) offset = (long)range - 1; // defesa — Random.value raramente bate exatamente 1.0

        return minInclusive + offset;
    }
}
