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
    // Upgrade futuro — aumenta a ÁREA do fogo (escala o GameObject do próprio
    // MageAttackHitbox, Collider2D incluso) até 2x o tamanho original. 1 = tamanho normal.
    [SerializeField] private float attackHitboxSizeMultiplier = 1f; // 🔢 upgradable — até 2x (ver OnValidate)

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
    // Controlador decide TUDO sobre a bola de fogo e repassa pro prefab em cada Launch() —
    // ver comentário no topo do MageFireball.cs.
    [SerializeField] private float fireballFlightSpeed = 8f; // 🔢 ajustável
    [SerializeField] private float fireballMaxTravelDistance = 4f; // 🔢 "distância curta" — ajustável
    [SerializeField] private float fireballExplosionRadius = 1.2f; // 🔢 ajustável
    [SerializeField] private float fireballGroundedDuration = 30f; // GDD: 30s
    [SerializeField] private float fireballGroundedTickInterval = 1f; // 🔢 ajustável
    [SerializeField] private float fireballGroundedRadius = 1.5f; // 🔢 raio da área depois da explosão, maior que o de impacto
    [SerializeField] private float fireballFireStatusDuration = 3f; // 🔢 passível de nerf/buff
    // Upgrade futuro — aumenta o tamanho da bola de fogo (escala o GameObject inteiro: sprite
    // + collider de voo) E a área real de impacto/rastro no chão (fireballExplosionRadius/
    // fireballGroundedRadius acima já escalam junto dentro do MageFireball, puxando pelo
    // transform.localScale — ver comentário lá). 1 = tamanho normal, até 3x.
    [SerializeField] private float ultimateSizeMultiplier = 1f; // 🔢 upgradable — até 3x (ver OnValidate)

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
    [SerializeField] private float teleportProjectileSpeed = 12f; // 🔢 ajustável — velocidade do trajeto visual
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
    // Upgrade futuro — tamanho e velocidade de ataque do pet, decididos aqui e repassados
    // em Initialize() (mesmo critério do resto do herói: controlador decide, prefab só recebe).
    [SerializeField] private float petSizeMultiplier = 1f; // 🔢 upgradable — sem teto definido ainda
    [SerializeField] private float petActionSpeedMultiplier = 1f; // 🔢 upgradable — sem teto definido ainda
    private PetController currentPet;
    // Direita #29 "Segunda Fênix" (perk) — 2º Phoenix simultâneo, metade do dano do primeiro.
    private PetController secondPet;

    // Rede de segurança — mesmo padrão do Barbarian/Ranger: se o Animation Event de fim nunca
    // disparar, força o fim da ação em vez de travar isAttacking pra sempre. Também cobre a
    // animação de invocar o pet — se o clipe "SummonPet" real acabar sendo mais longo que
    // este valor, aumente-o (ou dê um tratamento próprio, tipo o isTeleporting do Shift).
    [SerializeField] private float maxActionDuration = 3f; // 🔢 ajustável
    private float actionElapsed;
    private bool ultimateFired;
    private int ultimateUseCount; // Cima #30 Trindade Flamejante — conta a cada UseUltimate(), nunca reseta (a cada 3 usos, "a cada 3", nunca "por dia")
    private bool trindadeFlamejanteEnabled;
    private const float TrindadeFlamejanteSpreadAngleDegrees = 12f; // "ângulos próximos" — 3 projéteis a +-12° do mouse

    // true entre AnimationTeleportDisappearEvent() e OnTeleportProjectileArrived() — exclui
    // o teleporte do timer genérico acima: a viagem do projétil tem duração VARIÁVEL
    // (depende da distância até o mouse), podendo passar do teto de 3s pensado pro golpe
    // único do Attack/Ultimate (mesmo motivo do "!isCamouflaged" do Ranger).
    private bool isTeleporting;

    // ===================== Perks (GDD do Mage) =====================
    // Mesmo critério de Barbarian/Ranger: CentralPerkId por-herói (WorldPerkEffects.Recompute
    // lê isto via HeroController.Instance), valores "de kit" capturados 1x em Awake() ANTES
    // de qualquer RecomputePerkEffects() rodar (senão cada compra multiplicaria em cima do
    // valor JÁ multiplicado da vez anterior, compondo errado), e RecomputePerkEffects()
    // chamado 1x no fim do Awake() (aplica ranks já salvos) e a cada PerkState.TryBuy().
    public override string CentralPerkId => MagePerkIds.Central;

    private float baseAttackDamageMultiplier;
    private float baseAttackHitboxSizeMultiplier;
    private float baseUltimateDamageMultiplier;
    private float baseUltimateTrailDamageMultiplier;
    private float baseFireStatusDamageMultiplier;
    private float baseFireballFlightSpeed;
    private float baseFireballMaxTravelDistance;
    private float baseFireballExplosionRadius;
    private float baseFireballGroundedDuration;
    private float baseFireballGroundedRadius;
    private float baseFireballFireStatusDuration;
    private float baseUltimateSizeMultiplier;
    private float baseSecondaryAbilityMaxRange;
    private float baseTeleportProjectileSpeed;
    private float basePetSizeMultiplier;
    private float basePetActionSpeedMultiplier;

    // ---- Esquerda (Ataque Primário) ----
    private float residualBurnChancePercent; // #20 Queimadura Residual
    private float primaryGroundTrailDamagePercent; // #21 Rastro Flamejante
    private float doubleBurstChancePercent; // #22 Rajada Dupla
    private float contagiousFireChancePercent; // #23 Fogo Contagiante
    private float flamingEchoChancePercent; // #24 Eco Flamejante
    private float explosiveImpactPercent; // #25 Impacto Explosivo
    private float arcaneBreathEnergyPercent; // #26 Fôlego Arcano
    private float vengefulFlameDamagePercent; // #27 Chama Vingativa
    private bool vengefulFlameNextHitPending;
    private float lifestealPercent; // #11 Chama Vital (ex-Muralha de Chamas)
    private bool eternalFlameEnabled; // #29 Chama Eterna
    private bool isChannelingEternalFlame;
    private bool supernovaEnabled; // #30 Supernova
    private int supernovaCastCounter;
    private float supernovaAccumulatedDamage;
    private float contagiousFireTickTimer;
    private MageFireball activeGroundFirePrimaryPatch; // slot anti-empilhamento do #21

    // ---- Direita (Sobrevivência) ----
    private float residualFlameDamagePercent; // #25 Chama Residual (teleporte)
    private float explosiveLandingPercent; // #26 Pouso Explosivo
    private float vengefulPhoenixDamagePercent; // #27 Fênix Vingativa
    private bool vengefulPhoenixPending;
    private float petDamageMultiplier = 1f;
    private float petCooldownReductionMultiplier = 1f;
    private float petChaseRadiusMultiplier = 1f;
    private bool secondPhoenixEnabled; // #29 Segunda Fênix
    private bool instantTeleportEnabled; // #30 Teleporte Instantâneo
    private MageFireball activeGroundFireTeleportPatch; // slot anti-empilhamento do #25

    // ---- Cima (Ultimate) ----
    private float ultimateGroundTrailDamagePercent; // família #5-7 "Trail damage" (voo)
    private MageFireball activeUltimateFireball;
    private MageFireball activeGroundFireUltimateTrailPatch; // slot anti-empilhamento da família #5-7
    private float ultimateTrailSpawnTimer;
    private const float UltimateTrailSpawnInterval = 0.5f; // 🔢 a cada quanto tempo de voo tenta deixar 1 trilha

#if UNITY_EDITOR
    private void OnValidate()
    {
        attackHitboxSizeMultiplier = Mathf.Clamp(attackHitboxSizeMultiplier, 1f, 3f);
        ultimateSizeMultiplier = Mathf.Clamp(ultimateSizeMultiplier, 1f, 3f);
        petSizeMultiplier = Mathf.Max(1f, petSizeMultiplier);
        petActionSpeedMultiplier = Mathf.Max(1f, petActionSpeedMultiplier);
    }
#endif

    protected override void Awake()
    {
        base.Awake();
        mageSpriteRenderer = GetComponent<SpriteRenderer>();

        baseAttackDamageMultiplier = attackDamageMultiplier;
        baseAttackHitboxSizeMultiplier = attackHitboxSizeMultiplier;
        baseUltimateDamageMultiplier = ultimateDamageMultiplier;
        baseUltimateTrailDamageMultiplier = ultimateTrailDamageMultiplier;
        baseFireStatusDamageMultiplier = fireStatusDamageMultiplier;
        baseFireballFlightSpeed = fireballFlightSpeed;
        baseFireballMaxTravelDistance = fireballMaxTravelDistance;
        baseFireballExplosionRadius = fireballExplosionRadius;
        baseFireballGroundedDuration = fireballGroundedDuration;
        baseFireballGroundedRadius = fireballGroundedRadius;
        baseFireballFireStatusDuration = fireballFireStatusDuration;
        baseUltimateSizeMultiplier = ultimateSizeMultiplier;
        baseSecondaryAbilityMaxRange = secondaryAbilityMaxRange;
        baseTeleportProjectileSpeed = teleportProjectileSpeed;
        basePetSizeMultiplier = petSizeMultiplier;
        basePetActionSpeedMultiplier = petActionSpeedMultiplier;

        if (attackHitbox != null) attackHitbox.transform.localScale = Vector3.one * attackHitboxSizeMultiplier;

        // Aplica de cara qualquer rank já comprado/salvo (Continue Game, ou reabrir a Loja
        // depois de já ter comprado algo nesta run) — mesmo motivo/mesmo padrão do Barbarian/
        // Ranger.
        RecomputePerkEffects();
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
                isAttackResolving = false;
            }
        }

        UpdateUltimateFlyingTrail();
        UpdateContagiousFire();
        UpdateEternalFlameChannel();
    }

    protected override void PrimaryAttack()
    {
        if (isAttacking) return;
        isAttacking = true;
        actionElapsed = 0f;
        castFireFired = false;

        // #29 "Chama Eterna" — entra no estado dedicado (Attack_Start -> Attack_Continuous em
        // loop -> Attack_End), em vez do Attack normal. isChannelingEternalFlame é checado no
        // Update() pra saber a hora de soltar o EternalFlameEndTrigger (quando o jogador
        // soltar o botão) — ver UpdateEternalFlameChannel().
        if (eternalFlameEnabled)
        {
            isChannelingEternalFlame = true;
            AnimatorTrigger("EternalFlameTrigger");
        }
        else
        {
            AnimatorTrigger("AttackTrigger");
        }
    }

    // Chama Eterna — enquanto estiver canalizando, solta o EternalFlameEndTrigger assim que o
    // jogador soltar o botão (Attack_Continuous está num self-loop no Animator, sem condição
    // de saída própria — só esse trigger, disparado de AnyState, tira ele de lá).
    private void UpdateEternalFlameChannel()
    {
        if (!isChannelingEternalFlame) return;
        if (IsAttackHeld) return;

        isChannelingEternalFlame = false;
        AnimatorTrigger("EternalFlameEndTrigger");
    }

    // Animation Event, no meio do cast — dispara a animação de fogo do próprio filho, que
    // decide o instante exato do dano na SUA própria animação (ver MageAttackHitbox).
    public void AnimationCastFireEvent()
    {
        if (castFireFired) return;
        castFireFired = true;
        DealPrimaryHit();
    }

    // Animation Event do clipe attack_continuous_XX — 1 por repetição do loop (Chama Eterna).
    // Sem guarda de "já disparou" de propósito: cada volta do loop é uma entrada NOVA no
    // estado (ver self-transition Attack_Continuous -> Attack_Continuous no Animator), então
    // o evento só dispara 1x por volta de qualquer jeito.
    public void AnimationEternalFlameHitEvent() => DealPrimaryHit();

    private void DealPrimaryHit()
    {
        float critMultiplier = HeroController.RollCritMultiplier(HeroController.PrimaryCritChance);
        // Bug real reportado pelo usuário: perk Central comprado, vida máx. subiu (EffectiveMaxHealth
        // já é genérico em HeroController), mas o DANO ficou igual — Barbarian/Ranger multiplicam
        // HeroController.CentralDamageMultiplier em toda fórmula de dano própria, o Mage nunca tinha
        // isso em lugar nenhum (nem aqui, nem na Ultimate, nem nas trilhas de fogo).
        float damage = stats.damage * attackDamageMultiplier * HeroController.CentralDamageMultiplier * critMultiplier;

        // #27 "Chama Vingativa" — bônus consumido aqui (no disparo em si, não no hit), mesmo
        // critério do "Ataque Furtivo" do Ranger.
        if (vengefulFlameNextHitPending)
        {
            damage *= 1f + vengefulFlameDamagePercent / 100f;
            vengefulFlameNextHitPending = false;
        }

        if (attackHitbox != null)
            attackHitbox.Fire(damage, ActionSpeedMultiplier, residualBurnChancePercent, damage, fireballFireStatusDuration, explosiveImpactPercent, flamingEchoChancePercent);

        // #11 "Chama Vital" — cura % do dano do golpe, aplicado no disparo (mesma granularidade
        // de Fôlego Arcano logo abaixo — não depende de quantos inimigos o hitbox realmente
        // acertou, é sobre a AÇÃO em si).
        if (lifestealPercent > 0f) Heal(damage * lifestealPercent / 100f);

        // #26 "Fôlego Arcano" — ganho de Energia por hit, independente de abate.
        GainEnergyPercentOfMax(arcaneBreathEnergyPercent);

        // #22 "Rajada Dupla" — 2º hit manual, mesmo instante (ver MageAttackHitbox.FireDirectHit).
        if (doubleBurstChancePercent > 0f && Random.Range(0f, 100f) < doubleBurstChancePercent)
            attackHitbox?.FireDirectHit();

        // #21 "Rastro Flamejante" — trilha no chão no ponto do hit, 1 instância por vez (ver
        // TrySpawnGroundFirePatch).
        if (primaryGroundTrailDamagePercent > 0f)
        {
            float trailDps = stats.damage * HeroController.CentralDamageMultiplier * primaryGroundTrailDamagePercent / 100f;
            TrySpawnGroundFirePatch(() => activeGroundFirePrimaryPatch, v => activeGroundFirePrimaryPatch = v, attackHitboxPivot != null ? attackHitboxPivot.position : transform.position, trailDps, 0.5f);
        }

        // #30 "Supernova" — acumula o dano de cada cast; a cada 10, explode o acumulado dos 9
        // anteriores numa área ao redor do Mage.
        if (supernovaEnabled)
        {
            supernovaCastCounter++;
            if (supernovaCastCounter >= 10)
            {
                supernovaCastCounter = 0;
                var results = Physics2D.OverlapCircleAll(transform.position, 3f);
                var targets = new System.Collections.Generic.List<EnemyController>();
                EnemyController.CollectDistinct(results, results.Length, targets);
                foreach (var enemy in targets) enemy.TakeDamage(supernovaAccumulatedDamage, isDirectPlayerHit: false);
                supernovaAccumulatedDamage = 0f;
            }
            else
            {
                supernovaAccumulatedDamage += damage;
            }
        }
    }

    // Animation Event, no fim do clipe de cast — só aqui o cooldown real começa a contar
    // (Seção 53 do GDD).
    public void AnimationAttackEndEvent()
    {
        isAttacking = false;
        isAttackResolving = false;
    }

    // isAttacking agora é checado por CanUseUltimate() (HeroController), antes de gastar
    // energia — não precisa mais desse "return" aqui dentro, virado tarde demais pra impedir
    // o gasto.
    protected override bool CanUseUltimate() => !isAttacking;

    protected override void UseUltimate()
    {
        isAttacking = true;
        actionElapsed = 0f;
        ultimateFired = false;
        ultimateUseCount++;
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

        float ultimateCritMultiplier = HeroController.RollCritMultiplier(HeroController.UltimateCritChance);
        float impactDamage = stats.damage * ultimateDamageMultiplier * HeroController.CentralDamageMultiplier * ultimateCritMultiplier;
        float trailDamagePerTick = stats.damage * ultimateTrailDamageMultiplier * HeroController.CentralDamageMultiplier;
        float fireStatusDamagePerSecond = stats.damage * fireStatusDamageMultiplier * HeroController.CentralDamageMultiplier;
        // Explosão e área no chão escalam junto com o tamanho da bola de fogo — valor FINAL
        // (já multiplicado) calculado aqui, não dentro do MageFireball (ver comentário lá
        // sobre circleCollider.radius precisar compensar a escala do GameObject).
        float explosionRadius = fireballExplosionRadius * ultimateSizeMultiplier;
        float groundedRadius = fireballGroundedRadius * ultimateSizeMultiplier;

        // Cima #30 "Trindade Flamejante" — a cada 3 usos da Ultimate (nunca reseta por dia),
        // sai 1 projétil extra pra cada lado (ângulos próximos), TODOS com dano cheio (nenhuma
        // divisão entre eles — "full damage" na descrição do perk).
        if (trindadeFlamejanteEnabled && ultimateUseCount % 3 == 0)
        {
            SpawnUltimateFireball(RotateDirection(RawAimDirection, -TrindadeFlamejanteSpreadAngleDegrees),
                impactDamage, trailDamagePerTick, fireStatusDamagePerSecond, explosionRadius, groundedRadius, trackForTrail: false);
            SpawnUltimateFireball(RawAimDirection,
                impactDamage, trailDamagePerTick, fireStatusDamagePerSecond, explosionRadius, groundedRadius, trackForTrail: true);
            SpawnUltimateFireball(RotateDirection(RawAimDirection, TrindadeFlamejanteSpreadAngleDegrees),
                impactDamage, trailDamagePerTick, fireStatusDamagePerSecond, explosionRadius, groundedRadius, trackForTrail: false);
        }
        else
        {
            SpawnUltimateFireball(RawAimDirection,
                impactDamage, trailDamagePerTick, fireStatusDamagePerSecond, explosionRadius, groundedRadius, trackForTrail: true);
        }
    }

    private void SpawnUltimateFireball(Vector2 direction, float impactDamage, float trailDamagePerTick, float fireStatusDamagePerSecond,
        float explosionRadius, float groundedRadius, bool trackForTrail)
    {
        var obj = Instantiate(fireballPrefab, transform.position, Quaternion.identity, HeroController.HerosContainer);
        obj.transform.localScale = Vector3.one * ultimateSizeMultiplier;
        var fireball = obj.GetComponent<MageFireball>();
        if (fireball == null) return;

        fireball.Launch(direction, impactDamage, trailDamagePerTick, fireStatusDamagePerSecond,
            fireballFlightSpeed, fireballMaxTravelDistance, explosionRadius, fireballGroundedDuration, fireballGroundedTickInterval, groundedRadius, fireballFireStatusDuration);

        if (trackForTrail)
        {
            // Cima #5-7 "Trail damage" — 1 trilha ativa por vez (mesmo critério de sempre);
            // com Trindade Flamejante, só a bola central (ângulo original) é rastreada.
            activeUltimateFireball = fireball;
            ultimateTrailSpawnTimer = 0f;
        }
    }

    private static Vector2 RotateDirection(Vector2 direction, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(direction.x * cos - direction.y * sin, direction.x * sin + direction.y * cos);
    }

    // Enquanto a bola de fogo da Ultimate estiver voando, deixa mini trilhas de fogo pelo
    // caminho (Cima #5-7, família "Trail damage") — 1 trilha ativa por vez (mesmo critério de
    // TrySpawnGroundFirePatch), a próxima só nasce quando a anterior acabar.
    private void UpdateUltimateFlyingTrail()
    {
        if (activeUltimateFireball == null) return;
        if (!activeUltimateFireball.IsFlying)
        {
            activeUltimateFireball = null;
            return;
        }
        if (ultimateGroundTrailDamagePercent <= 0f) return;

        ultimateTrailSpawnTimer += Time.deltaTime;
        if (ultimateTrailSpawnTimer < UltimateTrailSpawnInterval) return;
        ultimateTrailSpawnTimer = 0f;

        float trailDps = stats.damage * HeroController.CentralDamageMultiplier * 0.5f * (1f + ultimateGroundTrailDamagePercent / 100f);
        TrySpawnGroundFirePatch(() => activeGroundFireUltimateTrailPatch, v => activeGroundFireUltimateTrailPatch = v, activeUltimateFireball.transform.position, trailDps, 0.5f * ultimateSizeMultiplier);
    }

    // Animation Event, no fim do clipe de ultimate.
    public void AnimationUltimateEndEvent()
    {
        isAttacking = false;
    }

    // Imune a dano a viagem INTEIRA do teleporte (teleport_start + voo do projétil +
    // teleport_end), não só enquanto isTeleporting — um Slime de contato ou um golpe que já
    // estava a caminho não pode contar enquanto o Mage está sumindo/viajando/reaparecendo.
    protected override bool IsDamageImmune => isUsingSecondaryAbility;

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

        // Direita #25 "Chama Residual" — deixa 1 trilha de fogo no ponto de ORIGEM do
        // teleporte, mesmo critério anti-empilhamento de TrySpawnGroundFirePatch.
        if (residualFlameDamagePercent > 0f)
        {
            float trailDps = stats.damage * HeroController.CentralDamageMultiplier * 0.5f * (1f + residualFlameDamagePercent / 100f);
            TrySpawnGroundFirePatch(() => activeGroundFireTeleportPatch, v => activeGroundFireTeleportPatch = v, transform.position, trailDps, 0.5f);
        }

        // Direita #30 "Teleporte Instantâneo" — pula a viagem do projétil inteira, vai direto
        // pro ponto de chegada (ainda assim respeitando TeleportUtility.ClampDistance, pra
        // nunca atravessar parede/água).
        if (instantTeleportEnabled)
        {
            float instantDistance = Mathf.Min(RawAimDistance, secondaryAbilityMaxRange);
            instantDistance = TeleportUtility.ClampDistance(transform.position, RawAimDirection, instantDistance, transform);
            OnTeleportProjectileArrived(transform.position + (Vector3)(RawAimDirection.normalized * instantDistance));
            return;
        }

        // Sem prefab configurado, "chega" na hora, na posição atual — sem isso, o Mage
        // ficaria invisível pra sempre (isTeleporting exclui o timer de segurança genérico
        // de propósito, então não existe outro jeito de sair disso sozinho).
        if (teleportProjectilePrefab == null)
        {
            OnTeleportProjectileArrived(transform.position);
            return;
        }

        float distance = Mathf.Min(RawAimDistance, secondaryAbilityMaxRange);
        // Nunca pousa dentro de água/parede/prop (Collider2D sólido) — o projétil não checa
        // colisão nenhuma durante o voo (ver MageTeleportProjectile), só reporta onde parou.
        distance = TeleportUtility.ClampDistance(transform.position, RawAimDirection, distance, transform);
        var obj = Instantiate(teleportProjectilePrefab, transform.position, Quaternion.identity, HeroController.HerosContainer);
        var projectile = obj.GetComponent<MageTeleportProjectile>();
        if (projectile != null) projectile.Launch(RawAimDirection, distance, OnTeleportProjectileArrived, teleportProjectileSpeed);
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

        // Direita #26 "Pouso Explosivo" — dano de área no ponto de chegada.
        if (explosiveLandingPercent > 0f)
        {
            float landingDamage = stats.damage * attackDamageMultiplier * HeroController.CentralDamageMultiplier * explosiveLandingPercent / 100f;
            var results = Physics2D.OverlapCircleAll(landingPosition, 1.5f);
            var targets = new System.Collections.Generic.List<EnemyController>();
            EnemyController.CollectDistinct(results, results.Length, targets);
            foreach (var enemy in targets) enemy.TakeDamage(landingDamage, isDirectPlayerHit: false);
        }

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
        if (secondPet != null) Destroy(secondPet.gameObject);
        if (petPrefab == null) return;

        Vector3 spawnPos = petSpawnPoint != null ? petSpawnPoint.position : transform.position;
        var obj = Instantiate(petPrefab, spawnPos, Quaternion.identity, HeroController.HerosContainer);
        currentPet = obj.GetComponent<PetController>();
        if (currentPet != null)
            currentPet.Initialize(transform, petSpawnPoint, petSizeMultiplier, petActionSpeedMultiplier, petDamageMultiplier, petCooldownReductionMultiplier, petChaseRadiusMultiplier);

        // Direita #29 "Segunda Fênix" — 2º Phoenix simultâneo, metade do dano do 1º (mesmos
        // tamanho/velocidade/cooldown/alcance — só o dano cai pela metade, GDD: "metade dos
        // stats" interpretado como metade do DANO, o que de fato importa pro DPS do pet).
        if (secondPhoenixEnabled)
        {
            var secondObj = Instantiate(petPrefab, spawnPos, Quaternion.identity, HeroController.HerosContainer);
            secondPet = secondObj.GetComponent<PetController>();
            if (secondPet != null)
                secondPet.Initialize(transform, petSpawnPoint, petSizeMultiplier, petActionSpeedMultiplier, petDamageMultiplier * 0.5f, petCooldownReductionMultiplier, petChaseRadiusMultiplier);
        }
    }

    // Animation Event, no fim do clipe de invocação.
    public void AnimationSummonPetEndEvent()
    {
        isAttacking = false;
    }

    // #27 "Chama Vingativa" / Direita #27 "Fênix Vingativa" — consumidos aqui: levar dano
    // arma o bônus do PRÓXIMO hit do primário e dá um boost temporário ao(s) pet(s) vivo(s).
    // base.TakeDamage() primeiro (mesmo critério de qualquer override — ver Ranger), só reage
    // de verdade se o herói continuar vivo depois (sem bônus "póstumo").
    public override void TakeDamage(float amount, bool isDirectPlayerHit = true)
    {
        base.TakeDamage(amount, isDirectPlayerHit);
        if (HealthSystem.IsDead(stats.health)) return;

        if (vengefulFlameDamagePercent > 0f) vengefulFlameNextHitPending = true;

        if (vengefulPhoenixDamagePercent > 0f)
        {
            float duration = 5f; // 🔢 ajustável — "por um tempo curto" (GDD)
            currentPet?.ApplyTemporaryDamageBonus(1f + vengefulPhoenixDamagePercent / 100f, duration);
            secondPet?.ApplyTemporaryDamageBonus(1f + vengefulPhoenixDamagePercent / 100f, duration);
        }
    }

    // Não chama pet.Disappear() aqui de propósito — não precisa. GDD Deadline 8.1, Seção 3: o
    // herói morto é destruído e reinstanciado por GameplayBootstrap.RespawnHeroFresh(), que já
    // chama PetController.DestroyAll() antes de summonar o pet do herói novo. currentPet desta
    // instância (prestes a ser destruída junto) não precisa de nenhuma limpeza própria.
    protected override void OnHeroDeath()
    {
        // Morrer no meio do teleporte (entre a sprite sumir e o projétil chegar) não pode
        // deixar o Mage invisível pra sempre no respawn — o próprio HeroController.OnDeath()
        // já toca o DieTrigger, então a sprite precisa estar de volta pra morte aparecer.
        if (isTeleporting)
        {
            isTeleporting = false;
            if (mageSpriteRenderer != null) mageSpriteRenderer.enabled = true;
        }
    }

    // ===================== Fogo Contagiante (Esquerda #23, perk) =====================
    // Periódico (não por hit) — a cada 1s, pega 1 inimigo em chamas aleatório e tenta
    // espalhar o status pros vizinhos próximos, por chance. Custo de performance aceitável:
    // só roda enquanto o perk estiver comprado (contagiousFireChancePercent > 0) e 1x/segundo,
    // nunca por frame.
    private const float ContagiousFireTickInterval = 1f;
    private const float ContagiousFireSpreadRadius = 2.5f;

    private void UpdateContagiousFire()
    {
        if (contagiousFireChancePercent <= 0f) return;

        contagiousFireTickTimer += Time.deltaTime;
        if (contagiousFireTickTimer < ContagiousFireTickInterval) return;
        contagiousFireTickTimer = 0f;

        var allEnemies = FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
        foreach (var source in allEnemies)
        {
            if (source == null) continue;
            var sourceStatus = source.GetComponent<StatusEffectController>();
            if (sourceStatus == null || !sourceStatus.IsEffectActive(StatusEffectType.Fire)) continue;
            if (Random.Range(0f, 100f) >= contagiousFireChancePercent) continue;

            var nearby = Physics2D.OverlapCircleAll(source.transform.position, ContagiousFireSpreadRadius);
            var targets = new System.Collections.Generic.List<EnemyController>();
            EnemyController.CollectDistinct(nearby, nearby.Length, targets);
            foreach (var target in targets)
            {
                if (target == source) continue;
                var targetStatus = target.GetComponent<StatusEffectController>();
                if (targetStatus == null || targetStatus.IsEffectActive(StatusEffectType.Fire)) continue;
                targetStatus.ApplyStatusEffect(StatusEffectType.Fire, fireballFireStatusDuration, stats.damage * fireStatusDamageMultiplier * HeroController.CentralDamageMultiplier);
                break; // 1 alvo novo por fonte por tick, pra não virar reação em cadeia instantânea
            }
        }
    }

    // ===================== Trilhas de fogo — helper compartilhado =====================
    // Usado por Esquerda #21 (Rastro Flamejante), Direita #25 (Chama Residual) e Cima #5-7
    // (Trail damage, no caminho da bola de fogo da Ultimate) — mesmo prefab da Ultimate
    // (fireballPrefab), só que "nasce" já na fase Grounded (MageFireball.LaunchGroundedOnly),
    // em 0.5x de escala (pedido do usuário: "vai ser sumonado várias vezes, então o size tem
    // que ser menor"). Nunca mais de 1 trilha ativa por FONTE ao mesmo tempo (slot guardado
    // em `activeSlot`) — a próxima só nasce quando a atual tiver desaparecido de vez
    // (MageFireball.OnDisappeared), exatamente o cuidado anti-spawn-infinito pedido.
    // getSlot/setSlot em vez de "ref MageFireball" — parâmetro ref não pode ser capturado
    // dentro da lambda de OnDisappeared (CS1628), então o slot é acessado por um par de
    // delegates (cada chamador passa o campo certo via lambda, ver as 3 chamadas acima).
    private void TrySpawnGroundFirePatch(System.Func<MageFireball> getSlot, System.Action<MageFireball> setSlot, Vector3 position, float damagePerSecond, float scale)
    {
        if (getSlot() != null) return;
        if (fireballPrefab == null) return;

        var obj = Instantiate(fireballPrefab, position, Quaternion.identity, HeroController.HerosContainer);
        obj.transform.localScale = Vector3.one * scale;
        var patch = obj.GetComponent<MageFireball>();
        if (patch == null) { Destroy(obj); return; }

        patch.LaunchGroundedOnly(damagePerSecond, damagePerSecond, fireballGroundedDuration, fireballGroundedTickInterval, fireballGroundedRadius * scale, fireballFireStatusDuration);

        setSlot(patch);
        // Variável local capturada pela lambda — precisa ser a MESMA referência que
        // guardamos no slot, não o campo (que pode já ter mudado quando o callback chegar se
        // ele for reatribuído por outra chamada no meio do caminho). Como só entra aqui com o
        // slot vazio, não há ambiguidade: limpa o slot sempre que ESTA patch específica sumir.
        var spawnedPatch = patch;
        patch.OnDisappeared += () =>
        {
            if (getSlot() == spawnedPatch) setSlot(null);
        };
    }

    // ===================== Recompute (perks) =====================
    public override void RecomputePerkEffects()
    {
        RecomputeEsquerda();
        RecomputeDireita();
        RecomputeCima();
    }

    private void RecomputeEsquerda()
    {
        float damagePercent = PerkState.GetRank(MagePerkIds.EsquerdaChamaArdente) * 8f
            + PerkState.GetRank(MagePerkIds.EsquerdaFogoCrescente) * 15f
            + PerkState.GetRank(MagePerkIds.EsquerdaInfernoPessoal) * 20f
            + PerkState.GetRank(MagePerkIds.EsquerdaCombustaoTotal) * 30f
            + PerkState.GetRank(MagePerkIds.EsquerdaFuriaDasChamas) * 40f
            + PerkState.GetRank(MagePerkIds.EsquerdaChamaAbsoluta) * 25f;
        attackDamageMultiplier = baseAttackDamageMultiplier * (1f + damagePercent / 100f);

        // Pedido do usuário (2026-11-13): não existe "crescer só largura" separado de "crescer
        // só alcance" — só existe UM crescimento, igual o tamanho do pet e da Ultimate (escala
        // as 3 dimensões juntas, nunca x e y separados). As 2 famílias que antes controlavam
        // eixos diferentes (Alcance Ardente/Chama Longa/Fogo Expansivo e Leque de Fogo/Arco
        // Flamejante) agora alimentam o MESMO multiplicador uniforme — mesmo critério de "Dano
        // do Primário" logo acima, que também soma mais de 3 perks num único stat.
        //
        // Rebalanceamento (2026-11-13, pedido do usuário): com as 2 famílias juntas, o máximo
        // batia +310% (4.1x) — mas o teto real do campo é 3x (ver Clamp abaixo), sobrando perk
        // comprado à toa. Muralha de Chamas (o 6º perk, que controlava largura) saiu da soma e
        // virou "Chama Vital" (vida roubada, ver lifestealPercent) — os 5 que restaram foram
        // rebalanceados pra somar EXATAMENTE 200% no rank máximo: 2×(8%×5 ranks) + 2×(12%×3
        // ranks) + 1×(16%×3 ranks, o único capstone que sobrou) = 80+72+48 = 200%.
        float sizePercent = PerkState.GetRank(MagePerkIds.EsquerdaAlcanceArdente) * 8f
            + PerkState.GetRank(MagePerkIds.EsquerdaChamaLonga) * 12f
            + PerkState.GetRank(MagePerkIds.EsquerdaFogoExpansivo) * 16f
            + PerkState.GetRank(MagePerkIds.EsquerdaLequeDeFogo) * 8f
            + PerkState.GetRank(MagePerkIds.EsquerdaArcoFlamejante) * 12f;
        attackHitboxSizeMultiplier = Mathf.Clamp(baseAttackHitboxSizeMultiplier * (1f + sizePercent / 100f), 1f, 3f);
        if (attackHitbox != null) attackHitbox.transform.localScale = Vector3.one * attackHitboxSizeMultiplier;

        // #11 "Chama Vital" (ex-Muralha de Chamas) — vida roubada no ataque primário, 5%/rank.
        lifestealPercent = PerkState.GetRank(MagePerkIds.EsquerdaChamaVital) * 5f;

        float cooldownPercent = PerkState.GetRank(MagePerkIds.EsquerdaConjuracaoRapida) * 5f
            + PerkState.GetRank(MagePerkIds.EsquerdaFluxoArcano) * 8f
            + PerkState.GetRank(MagePerkIds.EsquerdaDominioDoFogo) * 10f;
        SetPrimaryCooldownMultiplier(1f - Mathf.Clamp01(cooldownPercent / 100f));

        float animSpeedPercent = PerkState.GetRank(MagePerkIds.EsquerdaGestoRapido) * 10f
            + PerkState.GetRank(MagePerkIds.EsquerdaFluidezArcana) * 15f
            + PerkState.GetRank(MagePerkIds.EsquerdaPrecisaoFlamejante) * 20f;
        SetActionSpeedMultiplier(1f + animSpeedPercent / 100f);

        float critPercent = PerkState.GetRank(MagePerkIds.EsquerdaFaiscaCritica) * 5f
            + PerkState.GetRank(MagePerkIds.EsquerdaChamaPerfeita) * 8f;
        HeroController.PrimaryCritChance = Mathf.Clamp01(critPercent / 100f);

        residualBurnChancePercent = PerkState.GetRank(MagePerkIds.EsquerdaQueimaduraResidual) * 10f;
        primaryGroundTrailDamagePercent = PerkState.GetRank(MagePerkIds.EsquerdaRastroFlamejante) * 10f;
        doubleBurstChancePercent = PerkState.GetRank(MagePerkIds.EsquerdaRajadaDupla) * 15f;
        contagiousFireChancePercent = PerkState.GetRank(MagePerkIds.EsquerdaFogoContagiante) * 10f;
        flamingEchoChancePercent = PerkState.GetRank(MagePerkIds.EsquerdaEcoFlamejante) * 15f;
        explosiveImpactPercent = PerkState.GetRank(MagePerkIds.EsquerdaImpactoExplosivo) * 20f;
        arcaneBreathEnergyPercent = PerkState.GetRank(MagePerkIds.EsquerdaFolegoArcano) * 5f;
        vengefulFlameDamagePercent = PerkState.GetRank(MagePerkIds.EsquerdaChamaVingativa) * 25f;
        eternalFlameEnabled = PerkState.GetRank(MagePerkIds.EsquerdaChamaEterna) > 0;
        supernovaEnabled = PerkState.GetRank(MagePerkIds.EsquerdaSupernova) > 0;
    }

    private void RecomputeDireita()
    {
        float teleportRangePercent = PerkState.GetRank(MagePerkIds.DireitaSaltoArcano) * 10f
            + PerkState.GetRank(MagePerkIds.DireitaPortalExpandido) * 15f
            + PerkState.GetRank(MagePerkIds.DireitaDistanciaArcana) * 20f
            + PerkState.GetRank(MagePerkIds.DireitaHorizonteMistico) * 30f;
        secondaryAbilityMaxRange = baseSecondaryAbilityMaxRange * (1f + teleportRangePercent / 100f);

        float teleportSpeedPercent = PerkState.GetRank(MagePerkIds.DireitaViagemRapida) * 10f
            + PerkState.GetRank(MagePerkIds.DireitaFluxoInstantaneo) * 15f
            + PerkState.GetRank(MagePerkIds.DireitaVelocidadeArcana) * 20f;
        teleportProjectileSpeed = baseTeleportProjectileSpeed * (1f + teleportSpeedPercent / 100f);

        float shiftCooldownPercent = PerkState.GetRank(MagePerkIds.DireitaRecargaMistica) * 5f
            + PerkState.GetRank(MagePerkIds.DireitaCanalAberto) * 8f
            + PerkState.GetRank(MagePerkIds.DireitaDominioArcano) * 10f;
        SetSecondaryCooldownMultiplier(1f - Mathf.Clamp01(shiftCooldownPercent / 100f));

        float petDamagePercent = PerkState.GetRank(MagePerkIds.DireitaBicoFlamejante) * 10f
            + PerkState.GetRank(MagePerkIds.DireitaGarrasArdentes) * 15f
            + PerkState.GetRank(MagePerkIds.DireitaFuriaDaFenix) * 20f
            + PerkState.GetRank(MagePerkIds.DireitaChamaImortal) * 30f
            + PerkState.GetRank(MagePerkIds.DireitaAvatarDaFenix) * 40f;
        petDamageMultiplier = 1f + petDamagePercent / 100f;

        float petSpeedPercent = PerkState.GetRank(MagePerkIds.DireitaReflexosDaFenix) * 10f
            + PerkState.GetRank(MagePerkIds.DireitaInstintoRapido) * 15f
            + PerkState.GetRank(MagePerkIds.DireitaFuriaInstantanea) * 20f;
        petActionSpeedMultiplier = basePetActionSpeedMultiplier * (1f + petSpeedPercent / 100f);

        float petSizePercent = PerkState.GetRank(MagePerkIds.DireitaFenixMaior) * 5f
            + PerkState.GetRank(MagePerkIds.DireitaFenixGigante) * 10f
            + PerkState.GetRank(MagePerkIds.DireitaFenixColossal) * 15f;
        petSizeMultiplier = basePetSizeMultiplier * (1f + petSizePercent / 100f);

        float petCooldownPercent = PerkState.GetRank(MagePerkIds.DireitaGarrasAgeis) * 10f
            + PerkState.GetRank(MagePerkIds.DireitaFuriaSemPausa) * 15f;
        petCooldownReductionMultiplier = 1f - Mathf.Clamp01(petCooldownPercent / 100f);

        float chaseRadiusPercent = PerkState.GetRank(MagePerkIds.DireitaVinculoEstendido) * 15f;
        petChaseRadiusMultiplier = 1f + chaseRadiusPercent / 100f;

        residualFlameDamagePercent = PerkState.GetRank(MagePerkIds.DireitaChamaResidual) * 10f;
        explosiveLandingPercent = PerkState.GetRank(MagePerkIds.DireitaPousoExplosivo) * 20f;
        vengefulPhoenixDamagePercent = PerkState.GetRank(MagePerkIds.DireitaFenixVingativa) * 25f;
        secondPhoenixEnabled = PerkState.GetRank(MagePerkIds.DireitaSegundaFenix) > 0;
        instantTeleportEnabled = PerkState.GetRank(MagePerkIds.DireitaTeleporteInstantaneo) > 0;

        // Direita #28 "Renascimento" — redesenhado 2026-11-13 (pedido do usuário: a versão
        // antiga, "cura %/rank no respawn em cima de nascer com metade da vida", confundia com
        // o Segunda Chance de Mundo e foi odiada no teste). Virou 1 rank único: ignora 1 morte
        // por dia, igual ao Segunda Chance de Mundo, só que exclusivo do Mage — ver
        // HeroController.HasExclusiveSecondChance (flag própria, empilha com o de Mundo).
        magePhoenixSecondChanceEnabled = PerkState.GetRank(MagePerkIds.DireitaRenascimento) > 0;
    }

    private bool magePhoenixSecondChanceEnabled;
    protected override bool HasExclusiveSecondChance => magePhoenixSecondChanceEnabled;

    private void RecomputeCima()
    {
        float impactDamagePercent = PerkState.GetRank(MagePerkIds.CimaExplosaoArdente) * 8f
            + PerkState.GetRank(MagePerkIds.CimaBolaDeFogoMaior) * 15f
            + PerkState.GetRank(MagePerkIds.CimaInfernoAbsoluto) * 20f
            + PerkState.GetRank(MagePerkIds.CimaCataclismoArcano) * 30f;
        ultimateDamageMultiplier = baseUltimateDamageMultiplier * (1f + impactDamagePercent / 100f);

        float trailDamagePercent = PerkState.GetRank(MagePerkIds.CimaRastroArdente) * 10f
            + PerkState.GetRank(MagePerkIds.CimaChamasPersistentes) * 15f
            + PerkState.GetRank(MagePerkIds.CimaFogoEterno) * 20f;
        ultimateGroundTrailDamagePercent = trailDamagePercent;

        float trailDurationPercent = PerkState.GetRank(MagePerkIds.CimaQueimaduraArcana) * 10f
            + PerkState.GetRank(MagePerkIds.CimaCombustaoInterna) * 15f
            + PerkState.GetRank(MagePerkIds.CimaFogoConsumidor) * 20f;
        fireballGroundedDuration = baseFireballGroundedDuration * (1f + trailDurationPercent / 100f);

        float trailAreaPercent = PerkState.GetRank(MagePerkIds.CimaQueimaduraProlongada) * 15f
            + PerkState.GetRank(MagePerkIds.CimaChamaDuradoura) * 20f;
        fireballGroundedRadius = baseFireballGroundedRadius * (1f + trailAreaPercent / 100f);

        float flightSpeedPercent = PerkState.GetRank(MagePerkIds.CimaVooFlamejante) * 10f
            + PerkState.GetRank(MagePerkIds.CimaProjetilArcano) * 15f
            + PerkState.GetRank(MagePerkIds.CimaVelocidadeInfernal) * 20f;
        fireballFlightSpeed = baseFireballFlightSpeed * (1f + flightSpeedPercent / 100f);

        float maxRangePercent = PerkState.GetRank(MagePerkIds.CimaAlcanceFlamejante) * 10f
            + PerkState.GetRank(MagePerkIds.CimaTrajetoriaLonga) * 15f
            + PerkState.GetRank(MagePerkIds.CimaHorizonteEmChamas) * 20f;
        fireballMaxTravelDistance = baseFireballMaxTravelDistance * (1f + maxRangePercent / 100f);

        float explosionRadiusPercent = PerkState.GetRank(MagePerkIds.CimaBolaMaior) * 5f
            + PerkState.GetRank(MagePerkIds.CimaEsferaFlamejante) * 10f
            + PerkState.GetRank(MagePerkIds.CimaSolPessoal) * 15f;
        fireballExplosionRadius = baseFireballExplosionRadius * (1f + explosionRadiusPercent / 100f);

        float groundFireDamagePercent = PerkState.GetRank(MagePerkIds.CimaChaoEmChamas) * 10f
            + PerkState.GetRank(MagePerkIds.CimaFogoQueNaoApaga) * 15f
            + PerkState.GetRank(MagePerkIds.CimaTerraArrasada) * 20f;
        fireStatusDamageMultiplier = baseFireStatusDamageMultiplier * (1f + groundFireDamagePercent / 100f);

        float chargeRequiredPercent = PerkState.GetRank(MagePerkIds.CimaConjuracaoSuprema) * 5f
            + PerkState.GetRank(MagePerkIds.CimaFluxoExplosivo) * 8f;
        cimaChargeRequiredMultiplier = 1f - Mathf.Clamp01(chargeRequiredPercent / 100f);

        float energyGainPercent = PerkState.GetRank(MagePerkIds.CimaInstintoArcano) * 10f
            + PerkState.GetRank(MagePerkIds.CimaFomeArcana) * 15f;
        cimaEnergyGainBonusPercent = energyGainPercent;

        float ultimateCritPercent = PerkState.GetRank(MagePerkIds.CimaFaiscaDevastadora) * 5f;
        HeroController.UltimateCritChance = Mathf.Clamp01(ultimateCritPercent / 100f);

        // Cima #30 "Trindade Flamejante" — a cada 3 usos da Ultimate, os 3 projéteis saem juntos
        // (ultimateUseCount incrementado em UseUltimate(), checado em AnimationUltimateLaunchEvent).
        trindadeFlamejanteEnabled = PerkState.GetRank(MagePerkIds.CimaTrindadeFlamejante) > 0;
    }

    // UltimateChargeRequiredMultiplier/UltimateEnergyMultiplier já são hooks genéricos do
    // HeroController (mesmo padrão usado pelo Ranger pros perks #15-17/#24-27 equivalentes) —
    // campos próprios aqui só pra não brigar com o resto da classe caso algum dia o Mage
    // ganhe uma 2ª fonte de cada um (nenhuma hoje).
    private float cimaChargeRequiredMultiplier = 1f;
    private float cimaEnergyGainBonusPercent;
    protected override float UltimateChargeRequiredMultiplier => cimaChargeRequiredMultiplier;
    protected override float UltimateEnergyMultiplier => 1f + cimaEnergyGainBonusPercent / 100f;
}
