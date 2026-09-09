using UnityEngine;

public abstract class EnemyController : MonoBehaviour
{
    public EnemyStats stats = new EnemyStats();
    public int energyReward = 20;
    public int monsterEssenceDropAmount = 1; // quantidade dropada por abate (GDD Seção 38, 🔢 valor de balanceamento pendente)
    public FloorDefinition ownerFloor;

    // Rede de segurança — se o Animation Event de fim de ataque nunca disparar (clipe
    // sem o evento configurado, erro de setup), o ataque força o próprio fim depois
    // desse tempo em vez de travar o inimigo pra sempre em "Attacking".
    public float maxAttackDuration = 5f;

    // Mesma rede de segurança, pro clipe de die — se o Animation Event de fim nunca
    // disparar, força a destruição depois desse tempo em vez de deixar o cadáver
    // parado em cena pra sempre.
    public float maxDieDuration = 3f;

    protected Transform player;
    protected Vector2 lockedTargetPosition; // travada no início do ataque (GDD Seção 22) — não atualiza até o hit
    private float lastAttackTime = -999f;
    private float attackElapsed;
    private float dieElapsed;
    private bool isDead;
    private bool dieHandled; // evita destruir/dropar loot duas vezes (evento + timeout de segurança)
    protected Animator animator;
    private SpriteRenderer spriteRenderer;
    private Color spriteOriginalColor;
    private float damageFlashTimer;

    private const float DamageFlashDuration = 0.08f;

    private enum AttackState { Idle, Attacking }
    private AttackState attackState = AttackState.Idle;

    protected virtual void Awake()
    {
        animator = GetComponent<Animator>(); // pode não existir em prefabs placeholder — sempre checar com "!= null", nunca "?." (ver AnimatorTrigger/SetMoving)
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) spriteOriginalColor = spriteRenderer.color;
        SetMoveDirection(Vector2.down); // direção padrão — sem isso, MoveX/MoveY ficam em (0,0) até o primeiro Move()/ataque, deixando o Blend Tree de Idle indefinido por alguns frames
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

        if (isDead)
        {
            // O objeto continua existindo (de propósito) enquanto o clipe "die" toca —
            // a destruição real só acontece via AnimationDieEndEvent (Animation Event).
            dieElapsed += Time.deltaTime;
            if (dieElapsed >= maxDieDuration)
            {
                Debug.LogWarning($"[{GetType().Name}] AnimationDieEndEvent nunca chegou — forçando destruição (verifique o Animator Controller).");
                AnimationDieEndEvent();
            }
            return;
        }

        if (player == null) return;

        if (attackState != AttackState.Idle)
        {
            attackElapsed += Time.deltaTime;
            if (attackElapsed >= maxAttackDuration)
            {
                Debug.LogWarning($"[{GetType().Name}] AnimationAttackEndEvent nunca chegou — forçando fim do ataque (verifique o Animator Controller).");
                EndAttack();
            }
            return; // travado durante o próprio ataque — não persegue nem re-ataca no meio da animação
        }

        float distance = Vector2.Distance(transform.position, player.position);

        if (distance <= stats.attackRadius)
        {
            SetMoving(false);
            TryStartAttack();
        }
        else if (distance <= stats.observationRadius)
        {
            SetMoving(true);
            Move();
        }
        else
        {
            SetMoving(false);
        }
    }

    protected abstract void Move();
    protected abstract void ExecuteHit(); // dano de verdade acontece aqui — chamado pelo AnimationHitEvent, no frame exato em que a animação conecta
    protected abstract AttackType AttackType { get; }
    protected virtual void OnAttackStarted() { } // hook pra setup extra no início do ataque (ex.: direção do Rat People)

    protected void SetMoving(bool isMoving)
    {
        if (animator != null) animator.SetBool("IsMoving", isMoving);
    }

    // Alimenta um Blend Tree 2D (Freeform Directional) de walk/attack/damage — direção
    // crua, sem arredondar pra 8 direções, pra deixar a interpolação do Blend Tree suave.
    // Funciona igual com monstros de 4 direções (só diagonais) ou 8.
    protected void SetMoveDirection(Vector2 direction)
    {
        if (animator == null) return;
        animator.SetFloat("MoveX", direction.x);
        animator.SetFloat("MoveY", direction.y);
    }

    private void TryStartAttack()
    {
        if (Time.time - lastAttackTime < stats.attackCooldown) return;
        if (!AttackBudgetManager.Instance.TryReserveSlot(ownerFloor, AttackType)) return; // sem slot — fica esperando, tenta de novo no próximo frame
        lastAttackTime = Time.time;
        lockedTargetPosition = player.position; // GDD Seção 22: alvo trava no início da animação, não continua atualizando até o impacto
        attackState = AttackState.Attacking;
        attackElapsed = 0f;

        SetMoveDirection((lockedTargetPosition - (Vector2)transform.position).normalized);

        AnimatorTrigger("AttackTrigger");
        OnAttackStarted();
        Debug.Log($"[{GetType().Name}] Ataque iniciado — aguardando Animation Event...");
    }

    // Chamado por um Animation Event no frame exato do clipe de ataque em que o golpe
    // conecta de verdade — a animação é a fonte de verdade do timing, não um timer.
    public void AnimationHitEvent()
    {
        if (attackState != AttackState.Attacking) return; // proteção — evento chamado fora de hora não faz nada
        ExecuteHit();
    }

    // Chamado por um Animation Event no último frame do clipe de ataque.
    public void AnimationAttackEndEvent()
    {
        if (attackState != AttackState.Attacking) return;
        EndAttack();
    }

    private void EndAttack()
    {
        attackState = AttackState.Idle;
        AttackBudgetManager.Instance.ReleaseSlot(ownerFloor, AttackType);
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;
        stats.health = HealthSystem.ApplyDamage(stats.health, amount);
        Debug.Log($"[{GetType().Name}] Recebeu {amount} de dano. HP = {stats.health}/{stats.maxHealth}");

        if (HealthSystem.IsDead(stats.health))
        {
            Die();
            return;
        }

        // Receber dano != reagir visualmente != interromper uma ação. Comprometido com
        // um ataque, o dano nunca cancela a animação — só um flash leve, sem trocar de
        // estado no Animator. Fora de ataque, toca a reação normal (estado Damage).
        if (attackState == AttackState.Attacking) TriggerDamageFlash();
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
    // a cor durante a pausa, já que o projeto pausa por flag manual, não Time.timeScale.
    private void UpdateDamageFlash()
    {
        if (damageFlashTimer <= 0f) return;
        damageFlashTimer -= Time.deltaTime;
        if (damageFlashTimer <= 0f && spriteRenderer != null) spriteRenderer.color = spriteOriginalColor;
    }

    protected virtual void Die()
    {
        isDead = true;
        Debug.Log($"[{GetType().Name}] Morreu — aguardando Animation Event de fim do die...");
        AnimatorTrigger("DieTrigger");

        // Morreu no meio do próprio ataque — sem isso o slot do AttackBudgetManager
        // nunca seria liberado (vazamento permanente).
        if (attackState != AttackState.Idle) AttackBudgetManager.Instance.ReleaseSlot(ownerFloor, AttackType);

        GameEvents.EnemyKilled(energyReward);
    }

    // Chamado por um Animation Event no último frame do clipe "die" — a animação é a
    // fonte de verdade do timing, igual ataque: o GameObject só é destruído e o loot só
    // aparece depois que a morte terminou de tocar por completo (GDD Seção 22).
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
