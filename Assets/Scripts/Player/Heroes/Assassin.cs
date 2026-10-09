using UnityEngine;

public class Assassin : HeroController
{
    // ===================== Primário — Deadly Dash (GDD Seção 17.9) =====================
    // Redesenho a pedido do usuário: NÃO é mais um dash de verdade (sem translação contínua
    // nem dano ao longo do trajeto). O Assassin fica no lugar o tempo todo — no fim do clipe
    // "Start" ele vira invisível e nasce um projétil (AssassinDashProjectile) que anda pra
    // frente, bate 1x em área (Effect) e volta pro ponto de origem (End); só então o Assassin
    // reaparece, exatamente onde sempre esteve, e toca o clipe de "volta". Deadly Dash normal
    // e Thousand Blades sombrio usam esta MESMA mecânica — só mudam prefab/dano/raio.
    [Header("Ataque primário — Deadly Dash (projétil de ida-e-volta; Assassin não se move de verdade)")]
    [SerializeField] private LayerMask enemyLayerMask;
    [SerializeField] private GameObject dashProjectilePrefab; // precisa ter AssassinDashProjectile
    [SerializeField] private float dashProjectileSpeed = 14f; // 🔢 ajustável — compartilhada com o Thousand Blades
    [SerializeField] private float dashHitRadius = 0.8f; // 🔢 ajustável — raio do trigger do projétil (Effect)
    [SerializeField] private float dashKnockbackForce = 5f; // 🔢 ajustável — compartilhado com o Thousand Blades
    private bool isThousandBladesVariant;
    private bool isDashInvisible; // true do fim do Start ao fim do projétil — ver IsDamageImmune
    private Vector2 dashDirection;
    private SpriteRenderer bodySpriteRenderer;
    private AssassinDashProjectile activeDashProjectile;

    // Guarda contra o disparo duplo do Blend Tree 2D — DashStart/DashEnd (humano) só têm 4
    // poses ORTOGONAIS (N/E/S/W), mas a mira pode ser qualquer uma das 8 direções; mirando
    // numa diagonal, a Blend Tree mistura 2 clipes vizinhos com peso > 0 cada, e os dois
    // disparam o Animation Event no mesmo frame (mesmo bug de sempre — ver Barbarian/
    // EnemyController/Gunslinger). Sem isso, saíam 2 projéteis por dash (bug relatado em
    // teste). ThousandBladesStart/End (sombrio) usam DiagonalAimX/Y com 4 amostras diagonais
    // — sem zona morta, não precisam do guard.
    private int lastDashStartFrame = -1;
    private int lastDashEndFrame = -1;

    // ===================== Thousand Blades — variante sombria do primário (durante a Ultimate) =====================
    [Header("Primário sombrio — Thousand Blades (mesmo mecanismo do Deadly Dash, ativo só na forma sombria)")]
    [SerializeField] private GameObject thousandBladesProjectilePrefab; // precisa ter AssassinDashProjectile
    [SerializeField] private float thousandBladesDamageMultiplier = 2f; // GDD: "2x o dano do Deadly_Dash normal"
    [SerializeField] private float thousandBladesHitRadius = 1f; // 🔢 ajustável — raio do trigger do projétil (Effect)

    // Rede de segurança única — cobre a sequência inteira do primário (Start + projétil + End)
    // e também o teleporte (Shift), mesmo critério compartilhado do Ranger (Attack/Ultimate).
    [SerializeField] private float maxActionDuration = 3f; // 🔢 ajustável
    private float actionElapsed;

    // ===================== Ultimate — forma sombria =====================
    [Header("Ultimate — forma sombria (GDD Seção 17.9)")]
    [SerializeField] private RuntimeAnimatorController shadowAnimatorController; // substitui TODAS as animações durante a forma sombria
    [SerializeField] private float stealthDuration = 10f; // 🔢 GDD não dá número — placeholder ajustável
    private RuntimeAnimatorController humanController;
    private bool isStealthActive; // só true na fase "during" (depois de entrar, antes de sair)
    // Imune a dano nas 2 transições E na forma sombria INTEIRA (decisão do usuário, diferente
    // do Alce do Druid) — isTransformImmune cobre só as transições, isStealthActive cobre o
    // during, ver IsDamageImmune.
    private bool isTransformImmune;
    private float stealthElapsed;
    // Trava de segurança (pedido do usuário) — StealthTransformOutTrigger é AnyState no
    // controller sombrio: disparar ele com um Attack (Thousand Blades) ou Shift (teleporte)
    // em andamento arrancava o Animator do meio da ação (projétil ainda viajando, ou
    // teleporte ainda no "disappear"), deixando flags/visibilidade inconsistentes. Em vez de
    // disparar na hora, só marca a intenção aqui; Update() só chama EndStealthForm() de
    // verdade quando isAttacking voltar a false (ação atual já terminou sozinha).
    private bool stealthEndPending;

    // ===================== Secundária (Shift) — Teleporte =====================
    [Header("Habilidade Secundária (Shift) — Teleporte")]
    [SerializeField] private float teleportMaxRange = 5f; // 🔢 ajustável — teto, não "sempre pula o máximo"

    protected override void Awake()
    {
        base.Awake();
        humanController = animator != null ? animator.runtimeAnimatorController : null;
        bodySpriteRenderer = GetComponent<SpriteRenderer>();
    }

    protected override void Update()
    {
        if (!GameplayGate.IsActive) return;
        base.Update();

        if (isAttacking)
        {
            actionElapsed += Time.deltaTime;
            if (actionElapsed >= maxActionDuration)
            {
                Debug.LogWarning("[Assassin] Animation Event de fim de ação (dash ou teleporte) nunca chegou — forçando fim (verifique o Animator Controller).");
                ForceEndAction();
            }
        }

        if (isStealthActive)
        {
            stealthElapsed += Time.deltaTime;
            if (stealthElapsed >= stealthDuration) stealthEndPending = true;
        }

        // Só sai da forma sombria de verdade quando a ação atual (Attack/Thousand Blades ou
        // Shift/teleporte) já tiver terminado sozinha — nunca no meio dela.
        if (stealthEndPending && !isAttacking)
        {
            stealthEndPending = false;
            EndStealthForm();
        }
    }

    // Limpeza genérica de segurança — cobre tanto o dash/Thousand Blades (destrói o projétil
    // ativo, se houver, e reaparece o Assassin) quanto o teleporte travado (vira no-op nessa
    // parte, já que não há projétil nem invisibilidade nele).
    private void ForceEndAction()
    {
        if (activeDashProjectile != null)
        {
            Destroy(activeDashProjectile.gameObject);
            activeDashProjectile = null;
        }
        if (bodySpriteRenderer != null) bodySpriteRenderer.enabled = true;
        isDashInvisible = false;
        isThousandBladesVariant = false;
        isTransformImmune = false;
        isAttacking = false;
        isUsingSecondaryAbility = false;
    }

    // ===================== Primário =====================

    protected override void PrimaryAttack()
    {
        if (isAttacking) return;

        isAttacking = true;
        actionElapsed = 0f;
        isThousandBladesVariant = isStealthActive;
        dashDirection = AimDirection; // 1 de 8 direções fixas, já travada pela base
        AnimatorTrigger("AttackTrigger"); // Animator decide Deadly_Dash x Thousand_Blades via IsStealthActive
    }

    // Animation Event, no último frame do clipe "Start" (Deadly_Dash_Start ou
    // ITS_Thousand_Blades_Start) — o Assassin vira invisível aqui e nasce o projétil que faz a
    // viagem de ida-e-volta de verdade. A posição do Assassin NUNCA muda — só o projétil anda.
    public void AnimationDashStartEndEvent()
    {
        if (Time.frameCount == lastDashStartFrame) return;
        lastDashStartFrame = Time.frameCount;

        isDashInvisible = true;
        if (bodySpriteRenderer != null) bodySpriteRenderer.enabled = false;

        GameObject prefab = isThousandBladesVariant ? thousandBladesProjectilePrefab : dashProjectilePrefab;
        if (prefab == null) return;

        float damage = isThousandBladesVariant ? stats.damage * thousandBladesDamageMultiplier : stats.damage;
        float radius = isThousandBladesVariant ? thousandBladesHitRadius : dashHitRadius;

        var obj = Instantiate(prefab, transform.position, Quaternion.identity);
        activeDashProjectile = obj.GetComponent<AssassinDashProjectile>();
        if (activeDashProjectile != null)
            activeDashProjectile.Launch(this, dashDirection, damage, dashProjectileSpeed, dashKnockbackForce, radius, enemyLayerMask);
    }

    // Chamado pelo próprio projétil (AssassinDashProjectile.AnimationProjectileReturnedEvent)
    // quando ele termina a viagem de volta — reaparece o Assassin exatamente onde ele sempre
    // esteve e toca o clipe de "volta".
    public void OnDashProjectileReturned()
    {
        activeDashProjectile = null;
        isDashInvisible = false;
        if (bodySpriteRenderer != null) bodySpriteRenderer.enabled = true;
        AnimatorTrigger("AttackReturnTrigger");
    }

    // Animation Event, no último frame do clipe de "volta" (Deadly_Dash_End ou
    // ITS_Thousand_Blades_End) — mesmo método, os dois terminam a ação do mesmo jeito.
    public void AnimationDashEndEvent()
    {
        if (Time.frameCount == lastDashEndFrame) return;
        lastDashEndFrame = Time.frameCount;

        isAttacking = false;
        isThousandBladesVariant = false;
    }

    // Dash/Thousand Blades sem cooldown durante a Ultimate — patch em HeroController.cs.
    protected override bool IsAttackCooldownBypassed() => isStealthActive;

    // ===================== Ultimate — forma sombria =====================

    protected override bool CanUseUltimate() => !isAttacking;

    protected override void UseUltimate()
    {
        isAttacking = true;
        isTransformImmune = true;
        actionElapsed = 0f;

        // Troca o controller inteiro — a animação de entrar na forma sombria já é o estado
        // padrão do controller sombrio (mesmo critério do Alce do Druid).
        if (animator != null) animator.runtimeAnimatorController = shadowAnimatorController;
        RefreshActionSpeedMultiplier(); // trocar de controller reseta os parâmetros pro default dele
    }

    protected override bool IsUltimateActive => isStealthActive;

    protected override void CancelUltimate()
    {
        if (!isStealthActive) return;

        // Mesma trava de segurança do timeout — cancelar manualmente no meio de um Attack/
        // Shift em andamento não pode arrancar o Animator da ação atual (ver stealthEndPending).
        if (isAttacking) { stealthEndPending = true; return; }

        EndStealthForm();
    }

    // Animation Event, no fim do clipe de entrar na forma sombria.
    public void AnimationStealthTransformInEndEvent()
    {
        isTransformImmune = false;
        isStealthActive = true;
        isAttacking = false;
        stealthElapsed = 0f;
        // Mesmo campo estático já usado pela Coruja do Druid / camuflagem do Ranger.
        IsPlayerUntargetable = true;
    }

    // Compartilhado entre cancelamento manual e o teto de stealthDuration.
    private void EndStealthForm()
    {
        isStealthActive = false;
        IsPlayerUntargetable = false;
        isAttacking = true; // bloqueia de novo durante o clipe de voltar ao normal
        isTransformImmune = true;

        // Mesmo custo do cancelamento manual do Alce — zera a Energia da Ultimate.
        stats.energy = 0f;
        GameEvents.EnergyChanged(stats.energy, stats.maxEnergy);

        AnimatorTrigger("StealthTransformOutTrigger");
    }

    // Animation Event, no fim do clipe de voltar ao normal.
    public void AnimationStealthTransformOutEndEvent()
    {
        if (animator != null) animator.runtimeAnimatorController = humanController;
        RefreshActionSpeedMultiplier();
        isTransformImmune = false;
        isAttacking = false;
    }

    // Imune a dano nas 2 transições (isTransformImmune), na forma sombria INTEIRA
    // (isStealthActive, decisão do usuário), durante a duração inteira do teleporte
    // (isUsingSecondaryAbility) e enquanto invisível no dash/Thousand Blades (isDashInvisible,
    // decisão do usuário — narrativamente ele "não está ali" até o projétil voltar).
    protected override bool IsDamageImmune => isTransformImmune || isStealthActive || isUsingSecondaryAbility || isDashInvisible;

    // Mesma exceção do Alce do Druid — morrer durante o stealth não pode deixar
    // IsPlayerUntargetable travado em true pra sempre (cegaria os monstros pro respawn inteiro).
    protected override void OnHeroDeath()
    {
        if (isStealthActive)
        {
            isStealthActive = false;
            IsPlayerUntargetable = false;
        }
        stealthEndPending = false; // sem isso, o respawn (isAttacking volta a false) dispararia um EndStealthForm() póstumo
        ForceEndAction();
    }

    // Mesmo motivo do Druid — se a morte aconteceu na forma sombria, o Animator ainda está no
    // controller sombrio (de propósito, pra tocar o "Die" dele); troca pro humano ANTES do
    // Respawn() da base tentar dar Play("Idle") num controller sem esse estado certo.
    public override void AnimationDieEndEvent()
    {
        if (animator != null && animator.runtimeAnimatorController != humanController)
        {
            animator.runtimeAnimatorController = humanController;
            RefreshActionSpeedMultiplier();
        }
        base.AnimationDieEndEvent();
    }

    // ===================== Secundária (Shift) — Teleporte =====================

    protected override void UseSecondaryAbility()
    {
        isAttacking = true;
        actionElapsed = 0f;

        // Pose travada numa das 4 diagonais (NE/NW/SE/SW, não as 8 completas — GDD) — mesmo
        // critério do RollAimX/Y da Cambalhota / TeleportAimX/Y do Mage. Não influencia a
        // direção real do teleporte (RawAimDirection), só a pose.
        if (animator != null)
        {
            Vector2 pose = DirectionUtility.SnapTo4Diagonals(RawAimDirection);
            animator.SetFloat("TeleportAimX", pose.x);
            animator.SetFloat("TeleportAimY", pose.y);
        }

        AnimatorTrigger("SecondaryAbilityTrigger");
    }

    // Animation Event, no fim do clipe "disappear" — sem fase de voo (diferente do Mage): o
    // teleporte acontece direto aqui, sem objeto intermediário. Distância real respeita um
    // teto (teleportMaxRange), não sempre o máximo — decisão do usuário.
    public void AnimationTeleportDisappearEvent()
    {
        float distance = Mathf.Min(RawAimDistance, teleportMaxRange);
        transform.position += (Vector3)(RawAimDirection * distance);
    }

    // Animation Event, no fim do clipe "appear".
    public void AnimationTeleportAppearEndEvent()
    {
        isAttacking = false;
        isUsingSecondaryAbility = false;
    }

    // Sem override de CancelSecondaryAbility — não cancelável (GDD).
}
