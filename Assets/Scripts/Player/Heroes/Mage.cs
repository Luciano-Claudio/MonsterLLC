using UnityEngine;

public class Mage : HeroController
{
    // Ataque primário (GDD Seção 17.3) — GameObject filho, sempre girando pra acompanhar a
    // mira em tempo real (gira aqui no Update(), não dentro do próprio filho), dispara sua
    // própria animação de fogo no Animation Event do cast do Mage.
    [Header("Ataque primário — trigger rotacionando (GDD Seção 17.3)")]
    [SerializeField] private Transform attackHitboxPivot;
    [SerializeField] private MageAttackHitbox attackHitbox;
    [SerializeField] private float attackDamageMultiplier = 1f; // 🔢 ajustável

    // Attack pode acabar sendo Blend Tree (igual Barbarian/Ranger) — cada clipe blendado
    // carrega seu próprio Animation Event, então a trava é obrigatória por precaução (mesmo
    // bug já corrigido em EnemyController/Barbarian/Ranger), mesmo que o Attack do Mage
    // termine sendo um estado único.
    private bool castFireFired;

    [Header("Ultimate — bola de fogo em ângulo livre (GDD Seção 17.3)")]
    [SerializeField] private GameObject fireballPrefab; // precisa ter MageFireball
    [SerializeField] private float ultimateDamageMultiplier = 4f; // 🔢 GDD: "4x o dano do Mage" de impacto
    [SerializeField] private float ultimateTrailDamageMultiplier = 0.5f; // 🔢 GDD: "0,5x o dano do Mage por segundo" no rastro
    [SerializeField] private float fireStatusDamageMultiplier = 0.5f; // 🔢 "metade do dano do Mage por segundo" no status Fire — passível de nerf

    // Habilidade Secundária (Shift) — teleporte na direção da mira (GDD Seção 16/17.3).
    // Não cancelável (sem override de CancelSecondaryAbility). 2 estados no Animator
    // (teleport_start -> teleport_end), cada um Blend Tree 2D Freeform Directional de 8
    // pontos (AimX/AimY, mesma técnica do Attack dos monstros Ranged, não o Blend Tree de
    // 4 diagonais dos outros estados de herói). A transição teleport_start -> teleport_end
    // NÃO é automática (Exit Time) — só acontece quando o MageTeleportProjectile termina de
    // viajar (tempo variável, depende da distância), via TeleportArriveTrigger.
    [Header("Habilidade Secundária (Shift) — teleporte")]
    [SerializeField] private GameObject teleportProjectilePrefab; // precisa ter MageTeleportProjectile
    [SerializeField] private float secondaryAbilityMaxRange = 5f; // 🔢 ajustável
    // Nome diferente do "spriteRenderer" privado do HeroController de propósito — Unity não
    // aceita 2 campos com o mesmo nome numa cadeia de herança, mesmo sendo private em
    // classes diferentes ("The same field name is serialized multiple times").
    private SpriteRenderer mageSpriteRenderer;

    // Pet de início de dia (GDD: "Pets de início de dia (Mage/Blood Mage)") — sumona com
    // animação própria do Mage (duração = a do próprio clipe "SummonPet", não um tempo fixo)
    // e bloqueio de movimento (reaproveita isAttacking, igual toda ação real), não
    // instantaneamente. Ao morrer o herói, o pet retorna junto (OnHeroDeath()).
    [Header("Pet — Phoenix")]
    [SerializeField] private GameObject petPrefab; // precisa ter PetController
    [SerializeField] private Transform petSpawnPoint;
    private PetController currentPet;

    // Rede de segurança — mesmo padrão do Barbarian/Ranger: se o Animation Event de fim nunca
    // disparar, força o fim da ação em vez de travar isAttacking pra sempre. Também cobre a
    // animação de invocar o pet — se o clipe "SummonPet" real acabar sendo mais longo que
    // este valor, aumente-o (ou dê um tratamento próprio, tipo o isTeleporting do Shift).
    [SerializeField] private float maxActionDuration = 3f; // 🔢 ajustável
    private float actionElapsed;
    private bool ultimateFired;

    // true entre AnimationTeleportDisappearEvent() e OnTeleportProjectileArrived() — exclui
    // o teleporte do timer genérico acima: a viagem do projétil tem duração VARIÁVEL
    // (depende da distância até o mouse), podendo passar do teto de 3s pensado pro golpe
    // único do Attack/Ultimate (mesmo motivo do "!isCamouflaged" do Ranger).
    private bool isTeleporting;

    protected override void Awake()
    {
        base.Awake();
        mageSpriteRenderer = GetComponent<SpriteRenderer>();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        GameEvents.OnDayStart += SummonPet;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        GameEvents.OnDayStart -= SummonPet;
    }

    protected override void Update()
    {
        if (!GameplayGate.IsActive) return;

        base.Update();

        // Gira o pivot todo frame acompanhando a mira em tempo real — RawAimDirection já
        // congela sozinho durante isAttacking (HeroController.UpdateAimDirection), então o
        // pivot também para de girar sozinho assim que uma ação começa, sem checagem extra.
        if (attackHitboxPivot != null)
        {
            float angle = Mathf.Atan2(RawAimDirection.y, RawAimDirection.x) * Mathf.Rad2Deg - 90f;
            attackHitboxPivot.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        if (isAttacking && !isTeleporting)
        {
            actionElapsed += Time.deltaTime;
            if (actionElapsed >= maxActionDuration)
            {
                Debug.LogWarning("[Mage] Animation Event de fim de ação nunca chegou — forçando fim (verifique o Animator Controller).");
                isAttacking = false;
            }
        }
    }

    protected override void PrimaryAttack()
    {
        if (isAttacking) return;
        isAttacking = true;
        actionElapsed = 0f;
        castFireFired = false;
        AnimatorTrigger("AttackTrigger");
    }

    // Animation Event, no meio do cast — dispara a animação de fogo do próprio filho, que
    // decide o instante exato do dano na SUA própria animação (ver MageAttackHitbox).
    public void AnimationCastFireEvent()
    {
        if (castFireFired) return;
        castFireFired = true;

        if (attackHitbox != null) attackHitbox.Fire(stats.damage * attackDamageMultiplier);
    }

    // Animation Event, no fim do clipe de cast.
    public void AnimationAttackEndEvent()
    {
        isAttacking = false;
    }

    protected override void UseUltimate()
    {
        // Guarda própria — HeroController não bloqueia Ultimate por isAttacking, então cada
        // herói se protege (mesmo padrão do Barbarian/Ranger).
        if (isAttacking) return;
        isAttacking = true;
        actionElapsed = 0f;
        ultimateFired = false;
        AnimatorTrigger("UltimateTrigger");
    }

    // Animation Event, no frame exato em que a bola de fogo sai. Nasce da posição do próprio
    // Mage — sem pontos fixos de lançamento, já que a mira é livre (mouse), não travada nas
    // 8 direções; trajetória = ângulo livre exato (RawAimDirection) — única exceção do MVP
    // entre os projéteis retos (GDD Seção 17.3).
    public void AnimationUltimateLaunchEvent()
    {
        if (ultimateFired) return;
        ultimateFired = true;

        if (fireballPrefab == null) return;

        Vector3 spawnPos = transform.position;

        float impactDamage = stats.damage * ultimateDamageMultiplier;
        float trailDamagePerTick = stats.damage * ultimateTrailDamageMultiplier;
        float fireStatusDamagePerSecond = stats.damage * fireStatusDamageMultiplier;
        var obj = Instantiate(fireballPrefab, spawnPos, Quaternion.identity);
        var fireball = obj.GetComponent<MageFireball>();
        if (fireball != null) fireball.Launch(RawAimDirection, impactDamage, trailDamagePerTick, fireStatusDamagePerSecond);
    }

    // Animation Event, no fim do clipe de ultimate.
    public void AnimationUltimateEndEvent()
    {
        isAttacking = false;
    }

    // Bloqueia tudo (isAttacking, mesma regra do Ranger) — não cancelável, roda até o fim.
    protected override void UseSecondaryAbility()
    {
        isAttacking = true;
        actionElapsed = 0f;
        // Congela a pose em TeleportAimX/Y — parâmetros PRÓPRIOS do teleporte, separados do
        // AimX/AimY genérico. teleport_start e teleport_end são 2 estados/Blend Trees
        // diferentes; se os dois lessem AimX/AimY direto, nada garante que valem o mesmo nos
        // dois (é só o AimX/AimY "atual" no instante em que o Animator avalia cada um) — com
        // isso aqui, os dois SEMPRE leem o valor travado no instante exato do cast, nunca mudam
        // durante a habilidade inteira (start -> voo -> end).
        if (animator != null)
        {
            animator.SetFloat("TeleportAimX", AimDirection.x);
            animator.SetFloat("TeleportAimY", AimDirection.y);
        }
        AnimatorTrigger("SecondaryAbilityTrigger");
    }

    // Animation Event, no ÚLTIMO frame do clipe "teleport_start" (GDD: "no final dessa
    // animação o player irá desaparecer, vai surgir um projétil na direção que ele mirou").
    // O Mage some (sprite desligada) e o projétil nasce na posição atual, na direção e
    // distância já travadas (RawAimDirection/RawAimDistance congelados desde o início do
    // Shift) — o Mage só reaparece quando OnTeleportProjectileArrived() for chamado.
    public void AnimationTeleportDisappearEvent()
    {
        isTeleporting = true;
        if (mageSpriteRenderer != null) mageSpriteRenderer.enabled = false;

        // Sem prefab configurado, "chega" na hora, na posição atual — sem isso, o Mage
        // ficaria invisível pra sempre (isTeleporting exclui o timer de segurança genérico
        // de propósito, então não existe outro jeito de sair disso sozinho).
        if (teleportProjectilePrefab == null)
        {
            OnTeleportProjectileArrived(transform.position);
            return;
        }

        float distance = Mathf.Min(RawAimDistance, secondaryAbilityMaxRange);
        var obj = Instantiate(teleportProjectilePrefab, transform.position, Quaternion.identity);
        var projectile = obj.GetComponent<MageTeleportProjectile>();
        if (projectile != null) projectile.Launch(RawAimDirection, distance, OnTeleportProjectileArrived);
        else OnTeleportProjectileArrived(transform.position); // prefab sem o script — mesma rede de segurança
    }

    // Callback do MageTeleportProjectile — chamado no instante exato em que ele termina de
    // viajar a distância combinada (GDD: "no final dessa trajetória, o mage irá surgir
    // novamente com a animação teleport_end"). Reaparece na posição real de chegada, não um
    // valor calculado à parte — o projétil É a fonte de verdade de onde o Mage pousa.
    private void OnTeleportProjectileArrived(Vector3 landingPosition)
    {
        isTeleporting = false;
        transform.position = landingPosition;
        if (mageSpriteRenderer != null) mageSpriteRenderer.enabled = true;
        AnimatorTrigger("TeleportArriveTrigger");
    }

    // Animation Event, no fim do clipe "teleport_end".
    public void AnimationTeleportEndEvent()
    {
        isAttacking = false;
        isUsingSecondaryAbility = false;
    }

    // Chamado no Dia 1 e em todo GameEvents.OnDayStart seguinte (fonte única em
    // DayTimer.ResetForNewDay()) — GDD não fala em persistir o pet entre dias, só em
    // sumonar "no início do dia", então cada dia começa com uma Phoenix nova.
    private void SummonPet()
    {
        if (isAttacking) return; // já sumonando ou em outra ação — não deveria acontecer no início do dia, mas evita sobrepor
        isAttacking = true;
        actionElapsed = 0f;
        AnimatorTrigger("SummonPetTrigger");
    }

    // Animation Event, no meio do clipe de invocação do Mage — a
    // animação do HERÓI conjurando, não a "summon" do próprio pet (essa é dele, tocada
    // sozinha quando ele nasce). Se já existir uma Phoenix viva (dia anterior), destrói e
    // sumona de novo.
    public void AnimationSummonPetEvent()
    {
        if (currentPet != null) Destroy(currentPet.gameObject);
        if (petPrefab == null) return;

        Vector3 spawnPos = petSpawnPoint != null ? petSpawnPoint.position : transform.position;
        var obj = Instantiate(petPrefab, spawnPos, Quaternion.identity);
        currentPet = obj.GetComponent<PetController>();
        if (currentPet != null) currentPet.Initialize(transform, petSpawnPoint);
    }

    // Animation Event, no fim do clipe de invocação.
    public void AnimationSummonPetEndEvent()
    {
        isAttacking = false;
    }

    // GDD: "ao morrer o herói, o pet retorna junto na transição" — a Phoenix toca a própria
    // animação de desaparecer (Disappear()), não some instantaneamente. currentPet vira null
    // na hora pra SummonPet() do próximo dia não tentar destruir de novo um objeto que já vai
    // se destruir sozinho no fim da própria animação.
    protected override void OnHeroDeath()
    {
        if (currentPet != null)
        {
            currentPet.Disappear();
            currentPet = null;
        }

        // Morrer no meio do teleporte (entre a sprite sumir e o projétil chegar) não pode
        // deixar o Mage invisível pra sempre no respawn — o próprio HeroController.OnDeath()
        // já toca o DieTrigger, então a sprite precisa estar de volta pra morte aparecer.
        if (isTeleporting)
        {
            isTeleporting = false;
            if (mageSpriteRenderer != null) mageSpriteRenderer.enabled = true;
        }
    }
}
