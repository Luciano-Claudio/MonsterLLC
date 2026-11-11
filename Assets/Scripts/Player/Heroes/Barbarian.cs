using System.Collections.Generic;
using UnityEngine;

public class Barbarian : HeroController
{
    // 4 triggers fixos (um por diagonal) — mesmo padrão do golpe direcional do Bestiário
    // (MeleeEnemyController.GetHitboxForFacing, Seção 22): o GameObject nunca vira, só a
    // animação muda, então o trigger certo depende de onde a mira apontava quando o golpe
    // começou (congelada em AimDirection enquanto isAttacking, ver HeroController).
    [Header("Ataque primário — golpe no chão (GDD Seção 17.1)")]
    [SerializeField] private Collider2D attackHitboxNE;
    [SerializeField] private Collider2D attackHitboxNW;
    [SerializeField] private Collider2D attackHitboxSE;
    [SerializeField] private Collider2D attackHitboxSW;
    [SerializeField] private GameObject groundCrackPrefab;
    [SerializeField] private float groundCrackDuration = 3f; // 🔢 ajustável
    [SerializeField] private float groundCrackDamagePerSecond = 5f; // 🔢 ajustável — perks de Esquerda mexem aqui, nunca no prefab
    [SerializeField] private float groundCrackTickInterval = 0.5f; // 🔢 ajustável
    [SerializeField] private float knockbackForce = 4f; // 🔢 ajustável

    // Perks de Esquerda (GDD Seção 9, R10 — infra construída agora, valores neutros, nenhum
    // perk escrevendo ainda; isso é conteúdo da próxima fase).
    [Header("Perks de Esquerda — infra (default neutro)")]
    [SerializeField] private float groundCrackAreaMultiplier = 1f; // #15-17 "Área do Ground Crack" — INDEPENDENTE do Corpo (sizeMultiplier)
    [SerializeField] private float furiaDiagonalBonusPerExtraTarget = 0f; // #25 — % de dano extra por ALVO ADICIONAL distinto no mesmo golpe, por rank (reinterpretação: o golpe já acerta todos que estão na hitbox de uma vez, então "cada hitbox que acerta alvo diferente" = cada alvo extra no mesmo swing)
    [SerializeField] private float sangramentoPerfuranteDamagePerSecond = 0f; // #26 "Sangramento Perfurante" (redesign — era "Golpe Perfurante", decisão do usuário: trocou perfuração física por um Bleeding real, mesmo StatusEffectController que Ranger/Mage/Cleric já usam)
    [SerializeField] private float sangramentoPerfuranteDuration = 3f; // 🔢 ajustável
    [SerializeField] private float groundCrackShiftBonusPercent = 0f; // #27 "Fenda Enfurecida" — dano do Ground Crack +% enquanto o Shift estiver ativo
    [SerializeField] private float groundCrackHpLostBonusPerStep = 0f; // #28 "Fenda Selvagem" — dano do Ground Crack +X% por step de HP perdido (mesmos steps da passiva)
    [SerializeField] private bool groundCrackPermanent = false; // #29 "Fenda Permanente" — rank 0/1
    [SerializeField] private float groundCrackPermanentLeaveRadius = 3f; // 🔢 ajustável — distância do ponto de origem que conta como "se afastou"
    [SerializeField] private bool ecoDoImpactoEnabled = false; // #30 "Eco do Impacto" — rank 0/1
    [SerializeField] private float ecoDoImpactoSizeMultiplier = 0.5f; // 🔢 ajustável — "menor" que o crack original

    private readonly List<GroundCrackTracker> activePermanentCracks = new();
    private readonly List<EnemyController> echoTrackedEnemies = new();

    private struct GroundCrackTracker
    {
        public GameObject Instance;
        public Vector2 OriginPosition;
    }

    [Header("Ultimate — salto + N projéteis retos")]
    [SerializeField] private GameObject ultimateProjectilePrefab; // precisa ter HeroProjectile
    [SerializeField] private float ultimateDamageMultiplier = 2f; // 🔢 GDD: "2x o dano atual da arma"
    // Controlador decide TUDO sobre o projétil e repassa pro prefab em cada Launch() — ver
    // comentário no topo do HeroProjectile.cs.
    [SerializeField] private float projectileSpeed = 10f; // 🔢 ajustável
    [SerializeField] private float projectileMaxDistance = 8f; // 🔢 ajustável
    [SerializeField] private float projectileKnockbackForce = 4f; // 🔢 ajustável

    // Perks de Cima (GDD Seção 11, R10 — mesmo critério: infra agora, valores neutros).
    [Header("Perks de Cima — infra (default neutro)")]
    [SerializeField] private int ultimateProjectileCount = 8; // #21 "Chuva de Lanças" — 8 (base) a 14
    [SerializeField] private float ultimateChargeRequiredPercent = 100f; // #15-17 — % da energia máxima real necessária (100 = sem efeito)
    [SerializeField] private float ultimateEnergyGainBonusPercent = 0f; // #18-20 — soma em UltimateEnergyMultiplier (override abaixo)
    [SerializeField] private float ultimateExplosionRadius = 0f; // #23 "Explosão de Impacto"
    [SerializeField] private float ultimateExplosionDamagePercent = 0f; // #23
    [SerializeField] private float ultimateGroundCrackTrailChance = 0f; // #24 "Trilha de Fúria" — chance por tick de viagem
    [SerializeField] private float ultimateHealPercentPerHit = 0f; // #26 "Renovação de Batalha"
    [SerializeField] private float ultimateLandingAreaRadius = 0f; // #25 "Pouso Sísmico"
    [SerializeField] private float ultimateLandingAreaDamagePercent = 0f; // #25 — % do dano base (reserve)
    [SerializeField] private float ultimateRepeatVolleyChance = 0f; // #27 "Fúria Repetida"
    [SerializeField] private float ultimateAutoFireInterval = 0f; // #28 "Fúria Autônoma" — 0 = desligado
    [SerializeField] private bool ultimateEveryFifthUseDoubled = false; // #29 "Avatar da Destruição"
    [SerializeField] private bool ultimateFragmentOnHit = false; // #30 "Fragmentação Sem Fim"
    [SerializeField] private int ultimateFragmentMaxDepth = 0; // #30 — 0 = desligado
    [SerializeField] private float ultimateFragmentDamageDecayPercent = 50f; // 🔢 ajustável

    private float ultimateAutoFireElapsed;
    private int ultimateUseCount;

    // Habilidade Secundária (Shift) — buff temporário de dano e velocidade (GDD Seção 16/17.1,
    // Sprint 18→19). 1 animação só, sem direções, sem fases (igual "die") — não bloqueia
    // ataque/ultimate/movimento, e não é cancelável: roda até o fim sozinha.
    [Header("Habilidade Secundária (Shift) — dano e velocidade temporários")]
    // Balanceamento (2026-11-11, pedido do usuário): nerf de 2x pra 1.5x — combinado com o
    // teto de +100% dos perks de Dano no Shift (RecomputeDireita), o total nunca passa de 3x
    // (era até 23.5x antes, Shift virava "tecla de matar tudo" sem desafio nenhum).
    [SerializeField] private float secondaryAbilityDamageMultiplier = 1.5f; // 🔢 ajustável
    [SerializeField] private float secondaryAbilitySpeedMultiplier = 1.5f; // 🔢 ajustável
    [SerializeField] private float secondaryAbilityDuration = 5f; // 🔢 ajustável
    private float secondaryAbilityRemaining;
    private float baseMoveSpeed;

    // Perks de Direita (GDD Seção 10, R10 — mesmo critério: infra agora, valores neutros).
    [Header("Perks de Direita — infra (default neutro)")]
    [SerializeField] private float passiveDamagePerStepBonus = 0f; // #17-19 — ABSOLUTO, soma ao 0.25/step base
    [SerializeField] private int passiveDamageStepCapBonus = 0; // #20 "Limite Expandido" — soma ao teto de 8 steps
    [SerializeField] private float shiftDamageReductionPercent = 0f; // #21 "Fúria Protetora"
    [SerializeField] private float shiftLifestealPercent = 0f; // #22 "Sede de Sangue"
    [SerializeField] private float shiftCooldownReductionOnKillPercent = 0f; // #23 "Fôlego Recuperado"
    [SerializeField] private float shiftShieldPercentOfMaxHealth = 0f; // #24 "Último Fôlego"
    [SerializeField] private float shiftRegenPercentPerSecond = 0f; // #25 "Renovação Selvagem"
    // Mesmo acumulador do regen de Mundo (HeroController.UpdateHpRegen) — 1 Floating Text por
    // segundo com o total curado no intervalo, nunca 1 por frame.
    private float shiftRegenTextAccumulator;
    private float shiftRegenTextElapsed;
    [SerializeField] private float lowHpShiftCooldownReductionPercent = 0f; // #26 "Instinto de Sobrevivência"
    [SerializeField] private float lowHpShiftCooldownThresholdPercent = 30f; // 🔢 ajustável — "abaixo de 30% HP"
    [SerializeField] private float shiftMaxHealthBonusPercent = 0f; // #27 "Vontade Inquebrável"
    [SerializeField] private float furiaFinalInvulnDuration = 0f; // #28 "Fúria Final" — 0 = desligado
    [SerializeField] private float furiaFinalHpThresholdPercent = 20f; // 🔢 ajustável — "abaixo de 20% HP"
    [SerializeField] private bool coracaoSelvagemEnabled = false; // #29 — rank 0/1
    [SerializeField] private float coracaoSelvagemHpThresholdPercent = 10f; // 🔢 ajustável
    [SerializeField] private bool avatarDaFuriaUncapSteps = false; // #30 — rank 0/1, sem efeito fora do Shift

    private float furiaFinalInvulnRemaining;
    private bool wasBelowCoracaoSelvagemThreshold;

    // Base vinda dos ranks de cooldown (Direita #13-16, escrito pela futura recompute de perks
    // — fica 1f/sem efeito até lá) — composto por MULTIPLICAÇÃO com o condicional do #26 em
    // UpdateLowHpShiftCooldownReduction, nunca sobrescrito por ele (senão o #26 apagaria o
    // desconto base dos outros perks de cooldown sempre que o HP subisse de novo).
    private float secondaryCooldownBaseMultiplier = 1f;

    // Rede de segurança — se o Animation Event de fim (attack ou ultimate) nunca disparar,
    // força o fim da ação em vez de travar o herói pra sempre em "isAttacking" (mesmo
    // padrão do EnemyController pro attack/die).
    [SerializeField] private float maxActionDuration = 3f; // 🔢 ajustável
    private float actionElapsed;

    // Upgrade futuro — Barbarian literalmente cresce (escala o Transform inteiro, sprite +
    // colliders + hitboxes de ataque incluídos) até 3x o tamanho original. 1 = tamanho normal.
    [SerializeField] private float sizeMultiplier = 1f; // 🔢 upgradable — até 3x (ver OnValidate)

    // Attack e Ultimate são Blend Tree 2D Freeform Directional — pra quase qualquer ângulo
    // de mira, 2 clipes diagonais tocam misturados ao mesmo tempo, e cada clipe carrega seu
    // próprio Animation Event de hit. O Animator dispara o evento de TODO clipe com peso > 0
    // na mistura, não só do dominante — sem essa trava, um golpe/ultimate só chamava o
    // Hit/Land Event 2x (dano dobrado no golpe, 8 projéteis da ultimate saindo em dobro).
    // Mesma causa raiz já corrigida em EnemyController.AnimationHitEvent().
    private bool attackHitFired;
    private bool ultimateLandFired;
    private readonly List<EnemyController> hitTargets = new();

    private static readonly Vector2[] EightDirections =
    {
        Vector2.up,                                   // N
        new Vector2(0.7071f, 0.7071f),                 // NE
        Vector2.right,                                 // E
        new Vector2(0.7071f, -0.7071f),                // SE
        Vector2.down,                                  // S
        new Vector2(-0.7071f, -0.7071f),                // SW
        Vector2.left,                                  // W
        new Vector2(-0.7071f, 0.7071f),                // NW
    };

#if UNITY_EDITOR
    private void OnValidate()
    {
        sizeMultiplier = Mathf.Clamp(sizeMultiplier, 1f, 3f);
    }
#endif

    // Valores "de kit" capturados 1x, ANTES de qualquer RecomputePerkEffects() rodar — os
    // campos originais (knockbackForce, sizeMultiplier, etc.) continuam sendo o que o usuário
    // configura no Inspector, mas passam a ser SOBRESCRITOS a cada recompute com
    // base * (1 + bônus%). Sem isso, cada compra de perk multiplicaria em cima do valor JÁ
    // multiplicado da vez anterior, compondo errado (ex.: 2 ranks de +10% virariam +21%, não
    // +20%).
    private float baseKnockbackForce;
    private float baseSizeMultiplier;
    private float baseGroundCrackDuration;
    private float baseActionSpeedMultiplier;
    private float baseSecondaryAbilitySpeedMultiplier;
    private float baseSecondaryAbilityDuration;
    private float baseProjectileSpeed;
    private float baseProjectileMaxDistance;
    private float baseProjectileKnockbackForce;

    protected override void Awake()
    {
        base.Awake();
        baseMoveSpeed = stats.moveSpeed;

        baseKnockbackForce = knockbackForce;
        baseSizeMultiplier = sizeMultiplier;
        baseGroundCrackDuration = groundCrackDuration;
        baseActionSpeedMultiplier = ActionSpeedMultiplier;
        baseSecondaryAbilitySpeedMultiplier = secondaryAbilitySpeedMultiplier;
        baseSecondaryAbilityDuration = secondaryAbilityDuration;
        baseProjectileSpeed = projectileSpeed;
        baseProjectileMaxDistance = projectileMaxDistance;
        baseProjectileKnockbackForce = projectileKnockbackForce;

        transform.localScale = Vector3.one * sizeMultiplier;

        // Aplica de cara qualquer rank já comprado/salvo (Continue Game, ou reabrir a Loja
        // depois de já ter comprado algo nesta run) — mesmo motivo de WorldPerkEffects.Recompute()
        // ser chamado em StartRun/ContinueGame.
        RecomputePerkEffects();
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
                Debug.LogWarning("[Barbarian] Animation Event de fim de ação nunca chegou — forçando fim (verifique o Animator Controller).");
                isAttacking = false;
                isAttackResolving = false;
            }
        }

        if (secondaryAbilityRemaining > 0f)
        {
            secondaryAbilityRemaining -= Time.deltaTime;

            // #25 "Renovação Selvagem" — regen contínuo, só enquanto o Shift está ativo.
            // Acumula e mostra 1 Floating Text por segundo (mesmo critério do regen de Mundo em
            // HeroController.UpdateHpRegen) — nunca 1 por frame (bugado/lag) nem nenhum (sem
            // feedback nenhum).
            if (shiftRegenPercentPerSecond > 0f)
            {
                float shiftHealed = Heal(EffectiveMaxHealth * shiftRegenPercentPerSecond / 100f * Time.deltaTime, showFloatingText: false);
                shiftRegenTextAccumulator += shiftHealed;
            }

            shiftRegenTextElapsed += Time.deltaTime;
            if (shiftRegenTextElapsed >= 1f)
            {
                shiftRegenTextElapsed = 0f;
                if (shiftRegenTextAccumulator > 0f)
                    GameEvents.HealReceived(GetFloatingTextSpawnPosition(), shiftRegenTextAccumulator);
                shiftRegenTextAccumulator = 0f;
            }

            if (secondaryAbilityRemaining <= 0f)
            {
                stats.moveSpeed = baseMoveSpeed;
                isUsingSecondaryAbility = false;
            }
        }

        if (furiaFinalInvulnRemaining > 0f) furiaFinalInvulnRemaining -= Time.deltaTime;

        UpdateLowHpShiftCooldownReduction();
        UpdateCoracaoSelvagem();
        UpdatePermanentGroundCracks();
        UpdateEchoTrackedEnemies();
        UpdateUltimateAutoFire();
    }

    // #26 "Instinto de Sobrevivência" — enquanto o HP estiver abaixo do limiar, o cooldown do
    // Shift fica reduzido; volta ao normal assim que o HP sobe de novo (não é um gatilho de
    // borda único, é uma condição contínua, por isso recalcula o multiplicador todo frame).
    private void UpdateLowHpShiftCooldownReduction()
    {
        float conditionalMultiplier = 1f;
        if (lowHpShiftCooldownReductionPercent > 0f)
        {
            bool belowThreshold = stats.health / EffectiveMaxHealth * 100f < lowHpShiftCooldownThresholdPercent;
            if (belowThreshold) conditionalMultiplier = 1f - Mathf.Clamp01(lowHpShiftCooldownReductionPercent / 100f);
        }
        SetSecondaryCooldownMultiplier(secondaryCooldownBaseMultiplier * conditionalMultiplier);
    }

    // #29 "Coração Selvagem" — ao contrário do #26, este é um gatilho de BORDA: dispara 1x
    // exatamente no instante em que o HP cruza pra baixo do limiar, não enquanto estiver lá.
    private void UpdateCoracaoSelvagem()
    {
        if (!coracaoSelvagemEnabled) return;

        bool belowThreshold = stats.health / EffectiveMaxHealth * 100f < coracaoSelvagemHpThresholdPercent;
        if (belowThreshold && !wasBelowCoracaoSelvagemThreshold) ResetSecondaryCooldownRemaining();
        wasBelowCoracaoSelvagemThreshold = belowThreshold;
    }

    // #29 "Fenda Permanente" — substitui o Destroy(crackObj, groundCrackDuration) normal por
    // uma checagem contínua de distância; só entra na lista quando groundCrackPermanent=true no
    // momento do spawn (ver AnimationAttackHitEvent).
    private void UpdatePermanentGroundCracks()
    {
        for (int i = activePermanentCracks.Count - 1; i >= 0; i--)
        {
            var tracker = activePermanentCracks[i];
            if (tracker.Instance == null) { activePermanentCracks.RemoveAt(i); continue; }

            if (Vector2.Distance(transform.position, tracker.OriginPosition) > groundCrackPermanentLeaveRadius)
            {
                Destroy(tracker.Instance);
                activePermanentCracks.RemoveAt(i);
            }
        }
    }

    // #30 "Eco do Impacto" — monitora os inimigos empurrados pelo golpe até o knockback deles
    // terminar de verdade (EnemyController.IsBeingKnockedBack virar false), e só então cria o
    // 2º crack, no ponto onde cada um parou.
    private void UpdateEchoTrackedEnemies()
    {
        for (int i = echoTrackedEnemies.Count - 1; i >= 0; i--)
        {
            var enemy = echoTrackedEnemies[i];
            if (enemy == null) { echoTrackedEnemies.RemoveAt(i); continue; }
            if (enemy.IsBeingKnockedBack) continue;

            SpawnGroundCrackAt(enemy.transform.position, GroundCrack.Direction.SE, ecoDoImpactoSizeMultiplier, permanent: false);
            echoTrackedEnemies.RemoveAt(i);
        }
    }

    // #28 "Fúria Autônoma" — dispara a Ultimate sozinha a cada N segundos, ALÉM do uso manual
    // normal (não consome energia, não passa pelo gate de energia — de propósito, GDD: "além
    // do uso manual"). ultimateAutoFireInterval <= 0 (default) = desligado, custo zero no
    // Update() além da comparação.
    private void UpdateUltimateAutoFire()
    {
        if (ultimateAutoFireInterval <= 0f || isAttacking || isTrapped) return;

        ultimateAutoFireElapsed += Time.deltaTime;
        if (ultimateAutoFireElapsed < ultimateAutoFireInterval) return;
        ultimateAutoFireElapsed = 0f;

        UseUltimate();
    }

    protected override void PrimaryAttack()
    {
        if (isAttacking) return;
        isAttacking = true;
        actionElapsed = 0f;
        attackHitFired = false;
        AnimatorTrigger("AttackTrigger");
    }

    // Animation Event, no frame exato em que a espada toca o chão.
    public void AnimationAttackHitEvent()
    {
        if (attackHitFired) return;
        attackHitFired = true;

        var (hitbox, facingDirection) = GetHitboxForFacing();
        if (hitbox == null) return;

        float critMultiplier = HeroController.RollCritMultiplier(HeroController.PrimaryCritChance);
        float damage = stats.damage * GetPassiveDamageMultiplier() * GetSecondaryAbilityDamageMultiplier()
            * HeroController.PrimaryDamageMultiplier * HeroController.CentralDamageMultiplier * critMultiplier;

        // 128 (não 16) — bug real reportado pelo usuário (2026-10-26): com muito monstro
        // aglomerado perto do player (vários já "parados" no próprio slot de ataque, viram
        // obstáculo RVO pros outros), o golpe deixava de acertar alguns mesmo claramente
        // dentro do hitbox. Causa: Overlap() só devolve até results.Length colliders — com
        // 2 Collider2D por monstro, 16 satura com só 8 monstros sobrepostos, e o resto
        // simplesmente nunca entra no array (sem erro, sem aviso). 128 cobre 64 monstros de
        // verdade, bem acima de qualquer aglomerado plausível numa hitbox de golpe.
        var results = new Collider2D[128];
        int count = hitbox.Overlap(ContactFilter2D.noFilter, results);
        // Dedup obrigatório — todo monstro tem 2 Collider2D no mesmo GameObject (corpo +
        // trigger genérico), então Overlap() sem isso acertava o mesmo monstro 2x (mesmo bug
        // real já corrigido no primário do Mage — ver EnemyController.CollectDistinct()).
        EnemyController.CollectDistinct(results, count, hitTargets);

        // #25 "Fúria Diagonal" — reinterpretação (ver comentário no campo): cada alvo ADICIONAL
        // distinto atingido no mesmo golpe soma dano a esse golpe inteiro, por rank.
        if (furiaDiagonalBonusPerExtraTarget > 0f && hitTargets.Count > 1)
            damage *= 1f + (hitTargets.Count - 1) * furiaDiagonalBonusPerExtraTarget / 100f;

        float totalDamageDealt = 0f;
        foreach (var enemy in hitTargets)
        {
            enemy.TakeDamage(damage);
            enemy.ApplyKnockback(AimDirection, knockbackForce);
            totalDamageDealt += damage;

            // #26 "Sangramento Perfurante" — mesmo StatusEffectController/Bleeding que Ranger
            // já usa (ver RangerKnife.ApplyBleeding), reaplicar só reseta duração/dano, nunca
            // empilha 2 instâncias no mesmo monstro.
            if (sangramentoPerfuranteDamagePerSecond > 0f)
            {
                var statusEffects = enemy.GetComponent<StatusEffectController>();
                if (statusEffects != null)
                    statusEffects.ApplyStatusEffect(StatusEffectType.Bleeding, sangramentoPerfuranteDuration, sangramentoPerfuranteDamagePerSecond);
            }

            // #30 "Eco do Impacto" — só passa a monitorar o inimigo se o rank estiver comprado.
            if (ecoDoImpactoEnabled) echoTrackedEnemies.Add(enemy);
        }

        // #22 "Sede de Sangue" (Direita) — cura % do dano causado como HP, só durante o Shift.
        if (secondaryAbilityRemaining > 0f && shiftLifestealPercent > 0f)
            Heal(totalDamageDealt * shiftLifestealPercent / 100f);

        if (groundCrackPrefab != null)
        {
            // Pedido do usuário (2026-11-11): o dano base do Ground Crack passa a ser SEMPRE
            // metade do dano do golpe que o gerou ("damage", já calculado acima com toda a
            // família "Dano"/Central/crítico desse hit específico) — nunca mais um número fixo
            // desconectado do personagem, resolvendo de vez o problema de "1 de dano pra
            // sempre" mesmo com os 2 perks dedicados comprados. Mathf.Max garante piso de 1.
            // #27 "Fenda Enfurecida" (dano +% durante o Shift) e #28 "Fenda Selvagem" (dano +X%
            // por step de HP perdido) continuam multiplicando ESSE valor, nunca substituindo.
            float crackDamage = Mathf.Max(1f, damage * 0.5f);
            if (secondaryAbilityRemaining > 0f && groundCrackShiftBonusPercent > 0f)
                crackDamage *= 1f + groundCrackShiftBonusPercent / 100f;
            if (groundCrackHpLostBonusPerStep > 0f)
                crackDamage *= 1f + GetPassiveDamageSteps() * groundCrackHpLostBonusPerStep / 100f;

            // Sempre no pé do Barbarian (transform.position — pivot do sprite já fica na base),
            // nunca no centro da hitbox diagonal (que fica deslocada pra longe dele). Só a
            // ANIMAÇÃO muda conforme a direção do golpe, não a posição de nascimento.
            SpawnGroundCrackAtWithDamage(transform.position, facingDirection, crackDamage, groundCrackPermanent);
        }
    }

    // Extraído de AnimationAttackHitEvent (dano configurável) pra ser reaproveitado por #30
    // "Eco do Impacto" (SpawnGroundCrackAt, abaixo, com o dano/tick BASE, sem os bônus
    // condicionais do golpe original — o eco é sempre "o crack normal", nunca turbinado 2x).
    private void SpawnGroundCrackAtWithDamage(Vector2 position, GroundCrack.Direction direction, float damagePerSecond, bool permanent)
    {
        var crackObj = Instantiate(groundCrackPrefab, position, Quaternion.identity, HeroController.HerosContainer);
        var crack = crackObj.GetComponent<GroundCrack>();
        if (crack != null) crack.Initialize(direction, sizeMultiplier, damagePerSecond, groundCrackTickInterval, groundCrackAreaMultiplier);

        if (permanent) activePermanentCracks.Add(new GroundCrackTracker { Instance = crackObj, OriginPosition = position });
        else Destroy(crackObj, groundCrackDuration);
    }

    private void SpawnGroundCrackAt(Vector2 position, GroundCrack.Direction direction, float sizeOverride, bool permanent)
    {
        if (groundCrackPrefab == null) return;

        var crackObj = Instantiate(groundCrackPrefab, position, Quaternion.identity, HeroController.HerosContainer);
        var crack = crackObj.GetComponent<GroundCrack>();
        if (crack != null) crack.Initialize(direction, sizeOverride, groundCrackDamagePerSecond, groundCrackTickInterval);

        if (permanent) activePermanentCracks.Add(new GroundCrackTracker { Instance = crackObj, OriginPosition = position });
        else Destroy(crackObj, groundCrackDuration);
    }

    // Animation Event, no fim do clipe de ataque — só aqui o cooldown real começa a contar
    // (ver AttackCooldown.Start()/HeroController.Update(), Seção 53 do GDD).
    public void AnimationAttackEndEvent()
    {
        isAttacking = false;
        isAttackResolving = false;
    }

    // isAttacking agora é checado por CanUseUltimate() (HeroController), antes de gastar
    // energia — não precisa mais desse "return" aqui dentro, virado tarde demais pra impedir
    // o gasto.
    protected override bool CanUseUltimate() => !isAttacking;

    // #18-20 "Instinto de Poder"/"Sede de Poder"/"Fome de Destruição" — multiplicador de ganho
    // de energia só da Ultimate, já era um ponto de override pronto (Sprint 22, Rogue).
    protected override float UltimateEnergyMultiplier => 1f + ultimateEnergyGainBonusPercent / 100f;

    // #15-17 "Canalização Rápida"/"Fúria Contida"/"Explosão Iminente" — % da energia máxima
    // real necessária pra liberar a Ultimate (100 = sem efeito, igual hoje).
    protected override float UltimateChargeRequiredMultiplier => ultimateChargeRequiredPercent / 100f;

    protected override void UseUltimate()
    {
        isAttacking = true;
        actionElapsed = 0f;
        ultimateLandFired = false;
        ultimateUseCount++;
        AnimatorTrigger("UltimateTrigger");
    }

    // Animation Event, no frame exato em que o Barbarian cai no chão.
    public void AnimationUltimateLandEvent()
    {
        if (ultimateLandFired) return;
        ultimateLandFired = true;

        if (ultimateProjectilePrefab == null) return;

        float critMultiplier = HeroController.RollCritMultiplier(HeroController.UltimateCritChance);
        float reserve = stats.damage * GetPassiveDamageMultiplier() * GetSecondaryAbilityDamageMultiplier()
            * ultimateDamageMultiplier * HeroController.UltimateDamageMultiplier * HeroController.CentralDamageMultiplier * critMultiplier;

        // #29 "Avatar da Destruição" — a cada 5 usos, o 5º vem com o dobro de dano e projéteis.
        bool isDoubledUse = ultimateEveryFifthUseDoubled && ultimateUseCount % 5 == 0;
        if (isDoubledUse) reserve *= 2f;

        FireUltimateVolley(reserve, isDoubledUse ? ultimateProjectileCount * 2 : ultimateProjectileCount);

        // #27 "Fúria Repetida" — chance de uma 2ª rodada completa, automática, na mesma posição.
        if (ultimateRepeatVolleyChance > 0f && Random.value < ultimateRepeatVolleyChance / 100f)
            FireUltimateVolley(reserve, ultimateProjectileCount);

        // #25 "Pouso Sísmico" — dano em área no ponto de aterrissagem, independente dos projéteis.
        if (ultimateLandingAreaRadius > 0f && ultimateLandingAreaDamagePercent > 0f)
        {
            float landingDamage = reserve * ultimateLandingAreaDamagePercent / 100f;
            var results = new Collider2D[64];
            int count = Physics2D.OverlapCircle(transform.position, ultimateLandingAreaRadius, ContactFilter2D.noFilter, results);
            var landingHits = new List<EnemyController>();
            EnemyController.CollectDistinct(results, count, landingHits);
            foreach (var enemy in landingHits) enemy.TakeDamage(landingDamage);
        }
    }

    // Extraído pra ser reaproveitado por #27 (2ª rodada) e #28 (disparo automático) sem
    // duplicar a lógica de spawn — count já vem PRONTO (dobrado ou não pelo #29).
    private void FireUltimateVolley(float reserve, int count)
    {
        foreach (Vector2 dir in GetUltimateDirections(count))
        {
            var obj = Instantiate(ultimateProjectilePrefab, transform.position, Quaternion.identity, HeroController.HerosContainer);
            // Mesmo multiplicador do próprio corpo — Barbarian maior upgrada joga projéteis maiores junto.
            obj.transform.localScale = Vector3.one * sizeMultiplier;
            var projectile = obj.GetComponent<HeroProjectile>();
            if (projectile == null) continue;

            projectile.Launch(dir, reserve, projectileSpeed, projectileMaxDistance, projectileKnockbackForce);
            projectile.SetOwner(this);
            if (ultimateExplosionRadius > 0f) projectile.SetExplosion(ultimateExplosionRadius, ultimateExplosionDamagePercent);
            if (ultimateHealPercentPerHit > 0f) projectile.SetHealOnHit(ultimateHealPercentPerHit);
            if (ultimateGroundCrackTrailChance > 0f && groundCrackPrefab != null)
                projectile.SetGroundCrackTrail(groundCrackPrefab, ultimateGroundCrackTrailChance / 100f, groundCrackDamagePerSecond, groundCrackTickInterval, groundCrackDuration);
            if (ultimateFragmentOnHit && ultimateFragmentMaxDepth > 0)
                projectile.SetFragmentation(true, ultimateFragmentMaxDepth, ultimateFragmentDamageDecayPercent);
        }
    }

    // #21 "Chuva de Lanças" — 8 direções (base) continuam vindo do array fixo original, sem
    // mudar 1 bit de comportamento quando count==8 (default); qualquer outro N gera direções
    // uniformemente espaçadas (não precisam bater com os 8 estados nomeados do Animator — a
    // ANIMAÇÃO do projétil é só estética, o movimento real usa o vetor exato).
    private static Vector2[] GetUltimateDirections(int count)
    {
        if (count == 8) return EightDirections;

        var dirs = new Vector2[count];
        float angleStep = 360f / count;
        for (int i = 0; i < count; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            dirs[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }
        return dirs;
    }

    // Animation Event, no fim do clipe de ultimate.
    public void AnimationUltimateEndEvent()
    {
        isAttacking = false;
    }

    // Não seta isAttacking — de propósito: essa habilidade não bloqueia nada, ataque/
    // ultimate/movimento continuam funcionando normalmente durante o buff. Não cancelável
    // (sem override de CancelSecondaryAbility) — roda até secondaryAbilityDuration acabar.
    protected override void UseSecondaryAbility()
    {
        secondaryAbilityRemaining = secondaryAbilityDuration;
        stats.moveSpeed = baseMoveSpeed * secondaryAbilitySpeedMultiplier;
        AnimatorTrigger("SecondaryAbilityTrigger");

        // #24 "Último Fôlego" — escudo ao ativar o Shift, % do HP máximo efetivo.
        if (shiftShieldPercentOfMaxHealth > 0f) AddShield(EffectiveMaxHealth * shiftShieldPercentOfMaxHealth / 100f);

        // #28 "Fúria Final" — invuln total ao ativar o Shift COM HP baixo, por rank.
        if (furiaFinalInvulnDuration > 0f && stats.health / EffectiveMaxHealth * 100f < furiaFinalHpThresholdPercent)
            furiaFinalInvulnRemaining = furiaFinalInvulnDuration;
    }

    protected override bool IsDamageImmune => furiaFinalInvulnRemaining > 0f;

    // #21 "Fúria Protetora" — dano recebido -%, só durante o Shift.
    protected override float GetConditionalDamageReductionPercent() =>
        secondaryAbilityRemaining > 0f ? shiftDamageReductionPercent : 0f;

    // #27 "Vontade Inquebrável" — HP máximo temporário +%, só durante o Shift. Composto na
    // própria EffectiveMaxHealth (HeroController) — reverte sozinho quando o Shift acaba, sem
    // precisar de nenhum clamp manual aqui (Heal()/TakeDamage() já clampam em EffectiveMaxHealth
    // todo frame, então o HP atual nunca fica "acima do máximo" por mais de 1 frame).
    protected override float GetConditionalMaxHealthBonusPercent() =>
        secondaryAbilityRemaining > 0f ? shiftMaxHealthBonusPercent : 0f;

    // ShiftDamageMultiplier existe desde o R9 (par simétrico de Primary/UltimateDamageMultiplier)
    // mas nunca tinha sido lido em lugar nenhum — gap real encontrado agora, ao ligar os perks
    // de Direita #1-4 ("Dano do Shift"), que escrevem exatamente nele (ver RecomputePerkEffects).
    private float GetSecondaryAbilityDamageMultiplier() =>
        secondaryAbilityRemaining > 0f ? secondaryAbilityDamageMultiplier * HeroController.ShiftDamageMultiplier : 1f;

    // Classifica AimDirection (congelada em isAttacking) num dos 4 quadrantes diagonais —
    // mesmo critério que o Blend Tree 2D do Animator usa pra escolher entre atk_ne/nw/se/sw.
    // Devolve também a direção (não só o hitbox) — GroundCrack precisa saber qual das 4
    // animações (NE/NW/SE/SW) tocar.
    private (Collider2D hitbox, GroundCrack.Direction direction) GetHitboxForFacing()
    {
        bool east = AimDirection.x >= 0f;
        bool north = AimDirection.y >= 0f;

        if (north) return east ? (attackHitboxNE, GroundCrack.Direction.NE) : (attackHitboxNW, GroundCrack.Direction.NW);
        return east ? (attackHitboxSE, GroundCrack.Direction.SE) : (attackHitboxSW, GroundCrack.Direction.SW);
    }

    // GDD Seção 17.1 — passiva: a cada 10% de Vida Máxima perdida, +25% de dano, até +200%
    // (3x total) ao perder 80% ou mais. passiveDamageStepCapBonus (#20) soma ao teto normal;
    // avatarDaFuriaUncapSteps (#30) remove o teto por completo, só durante o Shift.
    private int GetPassiveDamageSteps()
    {
        float hpLostPercent = (1f - stats.health / stats.maxHealth) * 100f;
        int steps = Mathf.FloorToInt(hpLostPercent / 10f);

        if (avatarDaFuriaUncapSteps && secondaryAbilityRemaining > 0f) return Mathf.Max(0, steps);
        return Mathf.Clamp(steps, 0, 8 + passiveDamageStepCapBonus);
    }

    // passiveDamagePerStepBonus (#17-19) é ABSOLUTO — soma direto ao 0.25f base, não é um % dele
    // (GDD: "+5%/step ADICIONAL por rank", não "+5% do step base").
    private float GetPassiveDamageMultiplier() => 1f + GetPassiveDamageSteps() * (0.25f + passiveDamagePerStepBonus);

    protected override void OnEnable()
    {
        base.OnEnable();
        GameEvents.OnEnemyKilled += HandleEnemyKilledForShiftCooldown;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        GameEvents.OnEnemyKilled -= HandleEnemyKilledForShiftCooldown;
    }

    // #23 "Fôlego Recuperado" — matar um monstro durante o Shift reduz o cooldown restante
    // dele por %. Assinatura igual à de HeroController.HandleEnemyKilled (mesmo evento),
    // inscrição PRÓPRIA em vez de tornar o método da base virtual — não risca nada ali.
    private void HandleEnemyKilledForShiftCooldown(int energyValue, bool isBoss)
    {
        if (secondaryAbilityRemaining <= 0f || shiftCooldownReductionOnKillPercent <= 0f) return;
        ReduceSecondaryCooldownRemaining(secondaryAbilityDuration * shiftCooldownReductionOnKillPercent / 100f);
    }

    // Agregador único dos 90 perks bespoke do Barbarian (Esquerda/Direita/Cima, GDD Seções
    // 9/10/11) — chamado 1x no Awake() (aplica ranks já salvos) e a cada PerkState.TryBuy()
    // (qualquer árvore, não só a de Mundo). Mesmo critério de WorldPerkEffects.Recompute():
    // recomputa TUDO de uma vez a partir dos ranks atuais, nunca soma incremental em cima do
    // valor antigo.
    //
    // 🔢 Vários perks pedem um número/%/rank que o GDD não especifica (ver cada comentário
    // "PLACEHOLDER" abaixo) — inventei um valor plausível, sinalizado, até confirmação real.
    public override void RecomputePerkEffects()
    {
        RecomputeEsquerda();
        RecomputeDireita();
        RecomputeCima();
    }

    private void RecomputeEsquerda()
    {
        // Balanceamento (2026-11-10): 5x o original — era 8/15/20/30/50.
        //
        // Balanceamento (2026-11-11, pedido do usuário): Fúria Definitiva virou 1->5 ranks e é
        // quem carrega o "teto" de dano novo (igual Vitalidade Suprema do lado da vida) — tabela
        // escalando, não rank*X, pra chegar no ~1,2 trilhão de dano calibrado contra os
        // monstros Tier9/10 (ver chat 2026-11-11). Força Bruta ganhou 2 ranks novos (3->5) mas
        // manteve o mesmo 100%/rank — rank*X já escala sozinho, sem precisar de tabela.
        float furiaDefinitivaRank = PerkState.GetRank(BarbarianPerkIds.EsquerdaFuriaDefinitiva);
        float[] furiaDefinitivaPercent = { 0f, 250f, 2500f, 125000f, 25000000f, 30000000000000f };
        float danoPercent = PerkState.GetRank(BarbarianPerkIds.EsquerdaGolpePesado) * 40f
            + PerkState.GetRank(BarbarianPerkIds.EsquerdaFuriaDoImpacto) * 75f
            + PerkState.GetRank(BarbarianPerkIds.EsquerdaForcaBruta) * 100f
            + PerkState.GetRank(BarbarianPerkIds.EsquerdaDevastacao) * 150f
            + furiaDefinitivaPercent[Mathf.Clamp((int)furiaDefinitivaRank, 0, 5)];
        HeroController.PrimaryDamageMultiplier = 1f + danoPercent / 100f;

        float knockbackPercent = PerkState.GetRank(BarbarianPerkIds.EsquerdaEmpurrao) * 10f
            + PerkState.GetRank(BarbarianPerkIds.EsquerdaImpactoSismico) * 20f
            + PerkState.GetRank(BarbarianPerkIds.EsquerdaOndaDeChoque) * 30f;
        knockbackForce = baseKnockbackForce * (1f + knockbackPercent / 100f);

        float corpoPercent = PerkState.GetRank(BarbarianPerkIds.EsquerdaCorpoAvantajado) * 5f
            + PerkState.GetRank(BarbarianPerkIds.EsquerdaCrescimentoBrutal) * 10f
            + PerkState.GetRank(BarbarianPerkIds.EsquerdaCorpoDeTita) * 15f;
        sizeMultiplier = baseSizeMultiplier * (1f + corpoPercent / 100f);
        transform.localScale = Vector3.one * sizeMultiplier;

        float duracaoPercent = PerkState.GetRank(BarbarianPerkIds.EsquerdaFendaDuradoura) * 10f
            + PerkState.GetRank(BarbarianPerkIds.EsquerdaRachaduraProfunda) * 20f
            + PerkState.GetRank(BarbarianPerkIds.EsquerdaAbismo) * 30f;
        groundCrackDuration = baseGroundCrackDuration * (1f + duracaoPercent / 100f);

        float areaPercent = PerkState.GetRank(BarbarianPerkIds.EsquerdaFendaLarga) * 8f
            + PerkState.GetRank(BarbarianPerkIds.EsquerdaTerremoto) * 15f
            + PerkState.GetRank(BarbarianPerkIds.EsquerdaCataclismo) * 20f;
        groundCrackAreaMultiplier = 1f + areaPercent / 100f;

        float animSpeedPercent = PerkState.GetRank(BarbarianPerkIds.EsquerdaGolpeRapido) * 10f
            + PerkState.GetRank(BarbarianPerkIds.EsquerdaFrenesi) * 15f
            + PerkState.GetRank(BarbarianPerkIds.EsquerdaVelocidadeBrutal) * 20f;
        SetActionSpeedMultiplier(baseActionSpeedMultiplier * (1f + animSpeedPercent / 100f));

        float cooldownPercent = PerkState.GetRank(BarbarianPerkIds.EsquerdaReflexosDeCombate) * 5f
            + PerkState.GetRank(BarbarianPerkIds.EsquerdaInstintoDeBatalha) * 8f;
        SetPrimaryCooldownMultiplier(1f - Mathf.Clamp01(cooldownPercent / 100f));

        float critPercent = PerkState.GetRank(BarbarianPerkIds.EsquerdaGolpeCerteiro) * 5f
            + PerkState.GetRank(BarbarianPerkIds.EsquerdaPrecisaoSelvagem) * 8f;
        HeroController.PrimaryCritChance = Mathf.Clamp01(critPercent / 100f);

        // Balanceamento (2026-11-10): dano direto/indireto — 5x o original (era 10f).
        furiaDiagonalBonusPerExtraTarget = PerkState.GetRank(BarbarianPerkIds.EsquerdaFuriaDiagonal) * 50f;

        // 🔢 PLACEHOLDER — GDD só descreve "aplica Sangramento", nunca deu dano/rank nem duração.
        // Balanceamento (2026-11-10): 5x o original (era 2f).
        int sangramentoRank = PerkState.GetRank(BarbarianPerkIds.EsquerdaSangramentoPerfurante);
        sangramentoPerfuranteDamagePerSecond = sangramentoRank * 10f;
        sangramentoPerfuranteDuration = 3f;

        // Balanceamento (2026-11-10): 5x o original (era 15f).
        groundCrackShiftBonusPercent = PerkState.GetRank(BarbarianPerkIds.EsquerdaFendaEnfurecida) * 75f;

        // GDD: "+10/15/20% por step, CONFORME O RANK" — tabela por rank, não rank*X.
        // Balanceamento (2026-11-10): 5x o original (era 0/10/15/20).
        int fendaSelvagemRank = PerkState.GetRank(BarbarianPerkIds.EsquerdaFendaSelvagem);
        float[] fendaSelvagemPerStep = { 0f, 50f, 75f, 100f };
        groundCrackHpLostBonusPerStep = fendaSelvagemPerStep[Mathf.Clamp(fendaSelvagemRank, 0, 3)];

        groundCrackPermanent = PerkState.GetRank(BarbarianPerkIds.EsquerdaFendaPermanente) >= 1;
        ecoDoImpactoEnabled = PerkState.GetRank(BarbarianPerkIds.EsquerdaEcoDoImpacto) >= 1;
    }

    private void RecomputeDireita()
    {
        // Balanceamento (2026-11-11, pedido do usuário): nerf geral do Shift — esses 4 perks
        // somados agora têm teto de +100% (era até +1075%), pra combinar com o
        // secondaryAbilityDamageMultiplier=1.5 (nerfado de 2) e fechar um teto TOTAL de 3x no
        // Shift (era até 23.5x — "tecla de matar tudo" sem desafio nenhum).
        float shiftDanoPercent = PerkState.GetRank(BarbarianPerkIds.DireitaFuriaCrescente) * 5f
            + PerkState.GetRank(BarbarianPerkIds.DireitaImpetoSelvagem) * 7f
            + PerkState.GetRank(BarbarianPerkIds.DireitaAvalancheDeGolpes) * 9f
            + PerkState.GetRank(BarbarianPerkIds.DireitaFuriaSemFim) * 14f;
        HeroController.ShiftDamageMultiplier = 1f + shiftDanoPercent / 100f;

        // Velocidade de movimento SAI da escala de buff (2026-11-10, pedido do usuário: "tá
        // roubado o movespeed") — volta pro valor original (8/12/16/20), nunca mais escala
        // junto com dano/vida/gold.
        float shiftSpeedPercent = PerkState.GetRank(BarbarianPerkIds.DireitaPassosRapidos) * 8f
            + PerkState.GetRank(BarbarianPerkIds.DireitaRajada) * 12f
            + PerkState.GetRank(BarbarianPerkIds.DireitaVendaval) * 16f
            + PerkState.GetRank(BarbarianPerkIds.DireitaVelocidadeDaTempestade) * 20f;
        secondaryAbilitySpeedMultiplier = baseSecondaryAbilitySpeedMultiplier * (1f + shiftSpeedPercent / 100f);

        float shiftDurationPercent = PerkState.GetRank(BarbarianPerkIds.DireitaFolegoExtra) * 10f
            + PerkState.GetRank(BarbarianPerkIds.DireitaResistencia) * 15f
            + PerkState.GetRank(BarbarianPerkIds.DireitaPerseveranca) * 20f
            + PerkState.GetRank(BarbarianPerkIds.DireitaVontadeDeFerro) * 30f;
        secondaryAbilityDuration = baseSecondaryAbilityDuration * (1f + shiftDurationPercent / 100f);

        float shiftCooldownPercent = PerkState.GetRank(BarbarianPerkIds.DireitaRecuperacaoRapida) * 5f
            + PerkState.GetRank(BarbarianPerkIds.DireitaFolegoRenovado) * 8f
            + PerkState.GetRank(BarbarianPerkIds.DireitaInstintoAgil) * 10f
            + PerkState.GetRank(BarbarianPerkIds.DireitaReflexoDeBatalha) * 15f;
        secondaryCooldownBaseMultiplier = 1f - Mathf.Clamp01(shiftCooldownPercent / 100f);

        // Balanceamento (2026-11-10): 5x o original — era 0.05/0.08/0.10.
        passiveDamagePerStepBonus = PerkState.GetRank(BarbarianPerkIds.DireitaSangueFervente) * 0.25f
            + PerkState.GetRank(BarbarianPerkIds.DireitaInstintoBerserker) * 0.40f
            + PerkState.GetRank(BarbarianPerkIds.DireitaFuriaSemLimites) * 0.50f;
        passiveDamageStepCapBonus = PerkState.GetRank(BarbarianPerkIds.DireitaLimiteExpandido);

        shiftDamageReductionPercent = PerkState.GetRank(BarbarianPerkIds.DireitaFuriaProtetora) * 8f;
        shiftLifestealPercent = PerkState.GetRank(BarbarianPerkIds.DireitaSedeDeSangue) * 50f; // era 10f (5x)
        shiftCooldownReductionOnKillPercent = PerkState.GetRank(BarbarianPerkIds.DireitaFolegoRecuperado) * 5f;

        // GDD: "10%/20% do HP máximo POR RANK" — tabela por rank (2 ranks), não rank*X.
        int ultimoFolegoRank = PerkState.GetRank(BarbarianPerkIds.DireitaUltimoFolego);
        float[] ultimoFolegoPercent = { 0f, 10f, 20f };
        shiftShieldPercentOfMaxHealth = ultimoFolegoPercent[Mathf.Clamp(ultimoFolegoRank, 0, 2)];

        // 🔢 PLACEHOLDER — GDD só diz "%/segundo por rank", nunca deu o número.
        // Balanceamento (2026-11-10): 5x o original — era 2f.
        shiftRegenPercentPerSecond = PerkState.GetRank(BarbarianPerkIds.DireitaRenovacaoSelvagem) * 10f;

        lowHpShiftCooldownReductionPercent = PerkState.GetRank(BarbarianPerkIds.DireitaInstintoDeSobrevivencia) * 50f;
        shiftMaxHealthBonusPercent = PerkState.GetRank(BarbarianPerkIds.DireitaVontadeInquebravel) * 25f; // era 5f (5x)

        // GDD: "1s/2s POR RANK" — tabela por rank (2 ranks).
        int furiaFinalRank = PerkState.GetRank(BarbarianPerkIds.DireitaFuriaFinal);
        float[] furiaFinalDuration = { 0f, 1f, 2f };
        furiaFinalInvulnDuration = furiaFinalDuration[Mathf.Clamp(furiaFinalRank, 0, 2)];

        coracaoSelvagemEnabled = PerkState.GetRank(BarbarianPerkIds.DireitaCoracaoSelvagem) >= 1;
        avatarDaFuriaUncapSteps = PerkState.GetRank(BarbarianPerkIds.DireitaAvatarDaFuria) >= 1;
    }

    private void RecomputeCima()
    {
        // Balanceamento (2026-11-10): 5x o original — era 10/15/20/30/40.
        //
        // Balanceamento (2026-11-11, pedido do usuário): Juízo Final virou 1->5 ranks, mesmo
        // tratamento de Fúria Definitiva (tabela escalando, proporção 0,8x dela — era 200:250
        // no design original). Poder Esmagador ganhou 2 ranks novos (3->5), mesmo 100%/rank.
        float juizoFinalRank = PerkState.GetRank(BarbarianPerkIds.CimaJuizoFinal);
        float[] juizoFinalPercent = { 0f, 200f, 2000f, 100000f, 20000000f, 24000000000000f };
        float ultDanoPercent = PerkState.GetRank(BarbarianPerkIds.CimaGolpeDivino) * 50f
            + PerkState.GetRank(BarbarianPerkIds.CimaIraAncestral) * 75f
            + PerkState.GetRank(BarbarianPerkIds.CimaPoderEsmagador) * 100f
            + PerkState.GetRank(BarbarianPerkIds.CimaApocalipsePessoal) * 150f
            + juizoFinalPercent[Mathf.Clamp((int)juizoFinalRank, 0, 5)];
        HeroController.UltimateDamageMultiplier = 1f + ultDanoPercent / 100f;

        float ultSpeedPercent = PerkState.GetRank(BarbarianPerkIds.CimaProjetilVeloz) * 10f
            + PerkState.GetRank(BarbarianPerkIds.CimaLancaRelampago) * 15f
            + PerkState.GetRank(BarbarianPerkIds.CimaVelocidadeAbsoluta) * 20f;
        projectileSpeed = baseProjectileSpeed * (1f + ultSpeedPercent / 100f);

        float ultRangePercent = PerkState.GetRank(BarbarianPerkIds.CimaAlcanceAmpliado) * 10f
            + PerkState.GetRank(BarbarianPerkIds.CimaLongoAlcance) * 15f
            + PerkState.GetRank(BarbarianPerkIds.CimaHorizonteDeGuerra) * 20f;
        projectileMaxDistance = baseProjectileMaxDistance * (1f + ultRangePercent / 100f);

        float ultKnockbackPercent = PerkState.GetRank(BarbarianPerkIds.CimaImpactoDevastador) * 10f
            + PerkState.GetRank(BarbarianPerkIds.CimaOndaExpansiva) * 15f
            + PerkState.GetRank(BarbarianPerkIds.CimaForcaAbsoluta) * 20f;
        projectileKnockbackForce = baseProjectileKnockbackForce * (1f + ultKnockbackPercent / 100f);

        float chargeReductionPercent = PerkState.GetRank(BarbarianPerkIds.CimaCanalizacaoRapida) * 5f
            + PerkState.GetRank(BarbarianPerkIds.CimaFuriaContida) * 8f
            + PerkState.GetRank(BarbarianPerkIds.CimaExplosaoIminente) * 10f;
        ultimateChargeRequiredPercent = 100f - Mathf.Clamp(chargeReductionPercent, 0f, 100f);

        float energyGainPercent = PerkState.GetRank(BarbarianPerkIds.CimaInstintoDePoder) * 10f
            + PerkState.GetRank(BarbarianPerkIds.CimaSedeDePoder) * 15f
            + PerkState.GetRank(BarbarianPerkIds.CimaFomeDeDestruicao) * 20f;
        ultimateEnergyGainBonusPercent = energyGainPercent;

        ultimateProjectileCount = 8 + PerkState.GetRank(BarbarianPerkIds.CimaChuvaDeLancas) * 2;

        float ultCritPercent = PerkState.GetRank(BarbarianPerkIds.CimaGolpeCriticoCelestial) * 5f;
        HeroController.UltimateCritChance = Mathf.Clamp01(ultCritPercent / 100f);

        // 🔢 PLACEHOLDER — GDD não dá o raio nem o %/rank exatos da explosão. Raio fica como
        // está (área, fora da escala de buff); dano 5x o original (2026-11-10, era 15f).
        int explosionRank = PerkState.GetRank(BarbarianPerkIds.CimaExplosaoDeImpacto);
        ultimateExplosionRadius = explosionRank >= 1 ? 2f : 0f;
        ultimateExplosionDamagePercent = explosionRank * 75f;

        // 🔢 PLACEHOLDER — GDD não dá a chance/rank exata.
        ultimateGroundCrackTrailChance = PerkState.GetRank(BarbarianPerkIds.CimaTrilhaDeFuria) * 10f;

        // 🔢 PLACEHOLDER — GDD não dá o raio nem o %/rank exatos do pouso. Raio fica como está
        // (área, fora da escala de buff); dano 5x o original (2026-11-10, era 20f).
        int landingRank = PerkState.GetRank(BarbarianPerkIds.CimaPousoSismico);
        ultimateLandingAreaRadius = landingRank >= 1 ? 2.5f : 0f;
        ultimateLandingAreaDamagePercent = landingRank * 100f;

        // 🔢 PLACEHOLDER — GDD não dá o %/rank exato de cura.
        // Balanceamento (2026-11-10): 5x o original — era 5f.
        ultimateHealPercentPerHit = PerkState.GetRank(BarbarianPerkIds.CimaRenovacaoDeBatalha) * 25f;

        // 🔢 PLACEHOLDER — GDD não dá a chance/rank exata.
        ultimateRepeatVolleyChance = PerkState.GetRank(BarbarianPerkIds.CimaFuriaRepetida) * 15f;

        // 🔢 PLACEHOLDER — GDD não dá o intervalo exato ("a cada N segundos fixos").
        ultimateAutoFireInterval = PerkState.GetRank(BarbarianPerkIds.CimaFuriaAutonoma) >= 1 ? 8f : 0f;

        ultimateEveryFifthUseDoubled = PerkState.GetRank(BarbarianPerkIds.CimaAvatarDaDestruicao) >= 1;

        // 🔢 PLACEHOLDER — GDD não dá a profundidade máxima de fragmentação.
        bool fragmentEnabled = PerkState.GetRank(BarbarianPerkIds.CimaFragmentacaoSemFim) >= 1;
        ultimateFragmentOnHit = fragmentEnabled;
        ultimateFragmentMaxDepth = fragmentEnabled ? 3 : 0;
    }
}
