using UnityEngine;

public class MeleeEnemyController : EnemyController
{
    // 4 clipes de ataque diagonais (NE/NW/SE/SW), cada um com seu próprio trigger filho
    // fixo naquela direção — o GameObject nunca vira, só a animação muda, então o trigger
    // certo depende de pra que lado o monstro estava olhando quando o ataque começou.
    // Ficam sem uso se hasAttackAnimation for falso (ex.: Slimes). Decisão do usuário: Melee
    // não usa mais "attack radius" (distância) pra decidir quando parar/atacar — qualquer um
    // dos 4 tocando o player já libera (ver IsPlayerTouchingAnyHitbox), medido direto contra
    // o collider de verdade em vez de Vector2.Distance <= stats.attackRadius (bug recorrente:
    // RVO/colisão entre vários monstros às vezes nunca deixava fechar a distância exata).
    [Header("Triggers de ataque (um por direção diagonal)")]
    // protected (não mais private) — PossessableMeleeEnemyController (Bestiário Andar 4)
    // precisa de um 2º conjunto pra forma possuída (corpo maior) e sobrescreve
    // IsPlayerTouchingAnyHitbox()/GetHitboxForFacing() pra escolher qual conjunto usar.
    [SerializeField] protected Collider2D attackHitboxNE;
    [SerializeField] protected Collider2D attackHitboxNW;
    [SerializeField] protected Collider2D attackHitboxSE;
    [SerializeField] protected Collider2D attackHitboxSW;

    // Exceção (Bestiário Andar 3, Warbreed Phalanx — lança) — arte de ataque sem pose
    // diagonal real, só ortogonal (N/S/E/W); precisa de Animator Controller próprio (não um
    // AnimatorOverrideController do Base_Melee), com a Blend Tree de Attack reposicionada nos
    // 4 cardeais e alimentada por CardinalAimX/Y em vez de DiagonalAimX/Y (ver
    // EnemyController.SetAimDirection/DirectionUtility.SnapTo4Cardinals). Os 4 campos acima
    // continuam sendo os únicos 4 triggers — aqui só mudam de NOME CONCEITUAL (NE guarda o
    // trigger de South, NW o de North, SE o de East, SW o de West — mapeamento confirmado
    // pelo usuário comparando a arte real), e GetHitboxForFacing() escolhe entre eles pelo
    // eixo dominante em vez de quadrante diagonal.
    [SerializeField] private bool orthogonalAttack = false;

    // Enquanto a "vaga" perto do player estiver cheia (MeleeAttackSlotManager), o monstro
    // não fecha pra tentar tocar os triggers de ataque — fica flanqueando no flankRadius,
    // alternando andar na borda do círculo e idle_combat. Reaproveita o PatrolAI (mesma
    // lógica de alternância com timers aleatórios) só que com o alvo sempre recalculado num
    // ponto do anel, não num ponto fixo de patrulha.
    [Header("Flanco (quando a vaga perto do player está cheia)")]
    [SerializeField] private float minFlankIdleDuration = 1f;
    [SerializeField] private float maxFlankIdleDuration = 2.5f;
    [SerializeField] private float minFlankWalkDuration = 1f;
    [SerializeField] private float maxFlankWalkDuration = 2f;
    [SerializeField] private float flankArcDegrees = 60f; // o quanto o próximo ponto do anel pode variar em relação ao atual

    private PatrolAI flankAI;
    private bool hasAttackSlot;
    private float flankAngle;
    private bool flankAngleInitialized;

    // Bug real reportado pelo usuário (2026-10-26): monstro possuído (Bestiário Andar 4) ficava
    // preso em "Walk_Possession" sem nunca se mover de verdade — MoveInDirection() só libera o
    // AIPath quando o Animator já confirmou ter entrado no estado de nome walkStateName (ver
    // comentário lá), que aqui sempre era "Walk" (o default do parâmetro), nunca existente pra
    // quem está possuído (o estado real se chama "Walk_Possession"). Virtual pra
    // PossessableMeleeEnemyController sobrescrever — mesmo padrão já usado pelo Goblin Sapper
    // (Walk/Walk_Bomb via IsArmed).
    protected virtual string WalkStateName => "Walk";

    // Geral pra todo Melee agora, configurado no MeleeAttackSlotManager (não mais por
    // monstro em EnemyStats) — mesmo lugar que já decide quantos cabem perto do player,
    // faz sentido o raio do anel de flanco morar junto. Fallback só pro caso raro do
    // manager não existir na cena ainda (mesmo padrão null-safe do resto da classe).
    private float FlankRadius => MeleeAttackSlotManager.Instance != null ? MeleeAttackSlotManager.Instance.FlankRadius : 3f;

    protected override void Awake()
    {
        base.Awake();
        flankAI = new PatrolAI(minFlankIdleDuration, maxFlankIdleDuration, minFlankWalkDuration, maxFlankWalkDuration);
    }

    protected override void Move(float deltaTime)
    {
        Vector2 toPlayer = player.position - transform.position;
        float distance = toPlayer.magnitude;

        if (IsPlayerTouchingAnyHitbox())
        {
            SetMoving(false); // colado no player -> idle_combat, enquanto os cooldowns não liberam
            return;
        }

        // O player pode se afastar depois do monstro já ter conseguido vaga — sem isso,
        // quem pegou vaga primeiro ficaria com ela pra sempre, perseguindo o player pelo
        // mapa inteiro, enquanto outros monstros de verdade perto agora não conseguem
        // nenhuma. Libera assim que cair fora do próprio flankRadius.
        if (hasAttackSlot && distance > FlankRadius)
        {
            if (MeleeAttackSlotManager.Instance != null) MeleeAttackSlotManager.Instance.ReleaseSlot();
            hasAttackSlot = false;
        }

        if (hasAttackSlot || distance > FlankRadius)
        {
            // já garantiu vaga, ou ainda está longe demais até pra flanquear -> aproxima normalmente
            SetMoving(true);
            MoveInDirection(toPlayer.normalized, WalkStateName);
            return;
        }

        // chegou no anel de flanco sem vaga garantida — tenta pegar uma agora
        if (MeleeAttackSlotManager.Instance == null || MeleeAttackSlotManager.Instance.TryReserveSlot())
        {
            hasAttackSlot = true;
            SetMoving(true);
            MoveInDirection(toPlayer.normalized, WalkStateName);
            return;
        }

        UpdateFlanking(toPlayer, deltaTime);
    }

    private void UpdateFlanking(Vector2 toPlayer, float deltaTime)
    {
        if (!flankAngleInitialized)
        {
            // começa a orbitar a partir de onde já estava em relação ao player, sem pular
            flankAngle = Mathf.Atan2(-toPlayer.y, -toPlayer.x);
            flankAngleInitialized = true;
        }

        flankAI.Tick(deltaTime, PickNextFlankPoint);

        bool orbiting = flankAI.CurrentPhase == PatrolPhase.Walking;
        SetMoving(orbiting);
        if (orbiting) MoveInDirection((flankAI.WalkTarget - (Vector2)transform.position).normalized, WalkStateName);

        // tenta de novo a cada frame enquanto flanqueia — barato (comparação de int) e
        // garante que a vaga seja ocupada assim que alguém morrer/liberar, sem atraso.
        if (MeleeAttackSlotManager.Instance == null || MeleeAttackSlotManager.Instance.TryReserveSlot())
            hasAttackSlot = true;
    }

    private Vector2 PickNextFlankPoint()
    {
        float arcRad = flankArcDegrees * Mathf.Deg2Rad;
        flankAngle += Random.Range(-arcRad, arcRad);
        Vector2 offset = new Vector2(Mathf.Cos(flankAngle), Mathf.Sin(flankAngle)) * FlankRadius;
        return (Vector2)player.position + offset;
    }

    // Decisão do usuário (bug recorrente: RVO/colisão entre vários monstros às vezes nunca
    // deixava fechar EXATAMENTE a distância de stats.attackRadius, monstro perseguia pra
    // sempre sem nunca atacar) — "attack radius" deixou de existir pra Melee. Em vez de medir
    // distância, testa contato real contra os 4 triggers de ataque: qualquer um dos 4 já
    // libera (não precisa ser o da direção que está olhando) — o golpe de verdade
    // (ExecuteAttackHit) continua testando só o trigger de GetHitboxForFacing(), no frame
    // exato do Animation Hit Event.
    protected virtual bool IsPlayerTouchingAnyHitbox()
    {
        var heroCollider = player.GetComponent<Collider2D>();
        if (heroCollider == null) return false;

        if (attackHitboxNE != null && attackHitboxNE.IsTouching(heroCollider)) return true;
        if (attackHitboxNW != null && attackHitboxNW.IsTouching(heroCollider)) return true;
        if (attackHitboxSE != null && attackHitboxSE.IsTouching(heroCollider)) return true;
        if (attackHitboxSW != null && attackHitboxSW.IsTouching(heroCollider)) return true;
        return false;
    }

    protected override bool InAttackRange() => IsPlayerTouchingAnyHitbox();

    protected override void ExecuteAttackHit()
    {
        var hitbox = GetHitboxForFacing();
        if (hitbox == null) return;

        var heroCollider = player.GetComponent<Collider2D>();
        if (heroCollider == null) return;
        if (!hitbox.IsTouching(heroCollider)) return; // player fora do trigger nesse frame exato -> o golpe erra

        var hero = player.GetComponent<HeroController>();
        if (hero == null) return;

        Debug.Log($"[MeleeEnemyController] Golpe real conecta: {stats.attackDamage}.");
        hero.TakeDamage(stats.attackDamage);
    }

    // Classifica AimDirection (sempre em direção ao player) num dos 4 quadrantes
    // diagonais — mesmo critério que o Blend Tree 2D Freeform Directional usa pra
    // escolher entre atk_ne/nw/se/sw.
    protected virtual Collider2D GetHitboxForFacing()
    {
        if (orthogonalAttack)
        {
            // Eixo dominante (não quadrante) — mesmo critério de DirectionUtility.
            // SnapTo4Cardinals, só que escolhendo um Collider2D em vez de um Vector2.
            // Mapeamento conceitual (ver comentário no campo orthogonalAttack):
            // NE=South, NW=North, SE=East, SW=West.
            bool horizontalDominant = Mathf.Abs(AimDirection.x) >= Mathf.Abs(AimDirection.y);
            if (horizontalDominant) return AimDirection.x >= 0f ? attackHitboxSE : attackHitboxSW;
            return AimDirection.y >= 0f ? attackHitboxNW : attackHitboxNE;
        }

        bool east = AimDirection.x >= 0f;
        bool north = AimDirection.y >= 0f;

        if (north) return east ? attackHitboxNE : attackHitboxNW;
        return east ? attackHitboxSE : attackHitboxSW;
    }

    protected override void Die()
    {
        if (hasAttackSlot && MeleeAttackSlotManager.Instance != null)
            MeleeAttackSlotManager.Instance.ReleaseSlot();

        base.Die();
    }
}
