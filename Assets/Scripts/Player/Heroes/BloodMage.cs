using System.Collections.Generic;
using UnityEngine;

public class BloodMage : HeroController
{
    // ===================== Primário — projétil reto com reserva de dano (GDD Seção 17.10/13) =====================
    [Header("Ataque primário — projétil reto com perfuração (leque igual ao martelo do Paladin)")]
    [SerializeField] private GameObject projectilePrefab; // precisa ter BloodMageProjectile
    [SerializeField] private LayerMask enemyLayerMask;
    [SerializeField] private float projectileSpeed = 10f; // 🔢 ajustável
    [SerializeField] private float projectileMaxDistance = 8f; // 🔢 ajustável
    // Reserva de dano — quantos "hits cheios" o projétil aguenta antes de se esgotar. Padrão
    // 1 = cada projétil vale exatamente 1 hit (vida útil = o próprio dano, não perfura de
    // verdade) — pedido explícito do usuário, que achou o 3 original fazendo o projétil
    // atravessar monstros demais antes de impactar. Upgrade futuro de perfuração real sobe
    // esse número, não muda a lógica.
    [SerializeField] private float damageReserveMultiplier = 1f; // 🔢 ajustável
    // Upgrade futuro — mesma lógica do hammerCount do Paladin: a partir de 2, os projéteis
    // extras abrem em leque (±15°, ±30°...), cada um com reserva CHEIA (sem dividir entre
    // eles, mesmo critério dos martelos/flechas/balas de outros heróis).
    [SerializeField] private int projectileCount = 1; // 🔢 upgradable — 1 a 5 (ver OnValidate)
    private readonly List<float> projectileAngles = new();
    private bool projectileLaunchFired;

    // Rede de segurança genérica — mesmo padrão de todo herói.
    [SerializeField] private float maxActionDuration = 3f; // 🔢 ajustável
    private float actionElapsed;

    // ===================== Ultimate — onda de choque em anel (GDD Seção 17.10) =====================
    [Header("Ultimate — anel de dano em 3 estágios")]
    [SerializeField] private BloodMageShockwave shockwavePrefab; // filho dedicado, já no prefab
    [SerializeField] private float shockwaveDamageMultiplier = 5f; // GDD: "5x o dano" por estágio
    [SerializeField] private float shockwaveStage1Size = 2f; // 🔢 GDD: diâmetro 2 (círculo, não caixa)
    [SerializeField] private float shockwaveStage2Size = 4f; // 🔢 GDD: diâmetro 4
    [SerializeField] private float shockwaveStage3Size = 8f; // 🔢 GDD: diâmetro 8
    // Upgrade futuro — mesmo critério do ultimateSizeMultiplier do Mage: escala os 3 estágios
    // E o GameObject inteiro (visual), valor FINAL calculado aqui antes de entrar em Activate()
    // (BloodMageShockwave continua "burro", só recebe números já prontos).
    [SerializeField] private float shockwaveSizeMultiplier = 1f; // 🔢 upgradable — até 3x (ver OnValidate)
    private bool shockwaveSpawnFired;

    // ===================== Secundária (Shift) — extração de sangue =====================
    // Trava o movimento SÓ durante a animação de "sugar" (channel). Depois que o Animation
    // Event dispara (nasce a(s) orb(s) de sangue), o Blood Mage já pode se mover livremente —
    // a cura acontece quando cada orb chega nele, de forma totalmente independente (ver
    // OnBloodOrbArrived). Mesmo critério de seleção de alvo da vinha do Druid: pega até
    // extractTargetCount inimigos vivos mais próximos numa área, nunca repete.
    [Header("Habilidade Secundária (Shift) — extração de sangue (multi-alvo, igual à vinha do Druid)")]
    [SerializeField] private float extractSearchRadius = 15f; // 🔢 ajustável, mesmo critério da vinha do Druid
    [SerializeField] private int extractTargetCount = 1; // 🔢 upgradable — 1 a 15 (ver OnValidate), mesmo critério do vineCount
    [SerializeField] private GameObject bloodOrbPrefab; // precisa ter BloodOrb
    [SerializeField] private float extractDamageMultiplier = 1f; // 🔢 ajustável — GDD não dá número
    // Cura = dano causado NAQUELE alvo × este multiplicador (pedido do usuário: por padrão a
    // cura precisa ser EXATAMENTE o dano causado, não um número fixo desconectado do dano real).
    [SerializeField] private float extractHealMultiplier = 1f; // 🔢 ajustável
    [SerializeField] private float orbSpeed = 10f; // 🔢 ajustável
    // Status visual "Drain" (StatusEffectController.FlashStatus) — 1x só, sem dano/duração
    // própria (o dano aqui é sempre aplicado direto via TakeDamage, independente do flash).
    // Cancela Fire/Bleeding/etc. na tela do alvo, mas nunca escapa WordOfPain (guarda já fica
    // dentro do próprio FlashStatus).
    [SerializeField] private float drainFlashDuration = 0.5f; // 🔢 ajustável
    // Janela de debounce (pedido do usuário) — se outra orb chegar enquanto o Blood Mage ainda
    // está no meio da animação de "consumir sangue" (consume_blood) OU nos poucos milissegundos
    // logo depois dela acabar, NÃO toca outra animação — só cura silenciosamente. Sem isso, 2+
    // orbs chegando quase juntas prendiam o jogador numa sequência de poses de consumo, parado
    // tempo demais. O guard em si é só "!isAttacking" (ver OnBloodOrbArrived) — essa janela
    // cobre só a brecha de alguns frames IMEDIATAMENTE depois do isAttacking voltar a false.
    [SerializeField] private float consumeBloodCooldown = 0.15f; // 🔢 ajustável
    private float consumeCooldownRemaining;
    private bool extractHitFired;
    private readonly List<EnemyController> extractTargets = new();

    // ===================== Passiva — pet Elemental de Sangue (idêntico à Phoenix do Mage) =====================
    [Header("Pet — Elemental de Sangue")]
    [SerializeField] private GameObject petPrefab; // precisa ter PetController
    [SerializeField] private Transform petSpawnPoint;
    [SerializeField] private float petSizeMultiplier = 1f; // 🔢 upgradable — sem teto definido ainda, mesmo critério do Mage
    [SerializeField] private float petActionSpeedMultiplier = 1f; // 🔢 upgradable — sem teto definido ainda
    private PetController currentPet;

#if UNITY_EDITOR
    private void OnValidate()
    {
        projectileCount = Mathf.Clamp(projectileCount, 1, 5);
        extractTargetCount = Mathf.Clamp(extractTargetCount, 1, 15);
        shockwaveSizeMultiplier = Mathf.Clamp(shockwaveSizeMultiplier, 1f, 3f);
    }
#endif

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

        if (isAttacking)
        {
            actionElapsed += Time.deltaTime;
            if (actionElapsed >= maxActionDuration)
            {
                Debug.LogWarning("[BloodMage] Animation Event de fim de ação nunca chegou — forçando fim (verifique o Animator Controller).");
                isAttacking = false;
                isUsingSecondaryAbility = false;
            }
        }

        if (consumeCooldownRemaining > 0f) consumeCooldownRemaining -= Time.deltaTime;
    }

    // ===================== Primário =====================

    protected override void PrimaryAttack()
    {
        if (isAttacking) return;
        isAttacking = true;
        actionElapsed = 0f;
        projectileLaunchFired = false;
        AnimatorTrigger("AttackTrigger");
    }

    // Animation Event, no frame exato em que o(s) projétil(eis) são lançados — direção base =
    // RawAimDirection (ângulo livre exato do mouse, não mais travado nas 8 direções: o
    // projétil já rotaciona o próprio sprite pra acompanhar qualquer ângulo — mesmo critério
    // do MageFireball/RangerArrow/PaladinHammer), com leque angular se projectileCount > 1
    // (mesma lógica do martelo do Paladin — GetFanAngles/RotateDegrees). A pose do CORPO
    // continua travada nas 8 direções via Blend Tree (AimX/AimY) — só a trajetória real do
    // projétil segue o mouse.
    public void AnimationProjectileLaunchEvent()
    {
        if (projectileLaunchFired) return;
        projectileLaunchFired = true;

        if (projectilePrefab == null) return;

        float reserve = stats.damage * damageReserveMultiplier;
        GetFanAngles(projectileCount, RawAimDirection, projectileAngles);
        foreach (float angle in projectileAngles)
        {
            Vector2 dir = RotateDegrees(RawAimDirection, angle);
            var obj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
            var projectile = obj.GetComponent<BloodMageProjectile>();
            // Reserva CHEIA por projétil, sem dividir entre eles — mesmo critério do martelo
            // do Paladin/flechas do Ranger/balas do Gunslinger.
            if (projectile != null) projectile.Launch(dir, stats.damage, reserve, enemyLayerMask, projectileSpeed, projectileMaxDistance);
        }
    }

    // Leque de ângulos a partir da direção base — idêntico ao GetHammerAngles do Paladin. 1 =
    // só 0° (reto). A partir de 2, abre em pares simétricos (±15°, ±30°...); contagem par
    // sobra 1 projétil "solteiro" nesse par, resolvido pra cima ou pra baixo conforme o
    // quadrante da direção base (y>=0 → cima, y<0 → baixo).
    private static void GetFanAngles(int count, Vector2 baseDirection, List<float> results)
    {
        results.Clear();
        results.Add(0f);

        int fullPairs = (count - 1) / 2;
        for (int tier = 1; tier <= fullPairs; tier++)
        {
            float tierAngle = tier * 15f;
            results.Add(tierAngle);
            results.Add(-tierAngle);
        }

        if (count % 2 == 0)
        {
            float extraAngle = (fullPairs + 1) * 15f;
            results.Add(baseDirection.y >= 0f ? extraAngle : -extraAngle);
        }
    }

    private static Vector2 RotateDegrees(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }

    // Animation Event, no fim do clipe de ataque.
    public void AnimationAttackEndEvent()
    {
        isAttacking = false;
    }

    // ===================== Ultimate =====================

    protected override bool CanUseUltimate() => !isAttacking;

    protected override void UseUltimate()
    {
        isAttacking = true;
        actionElapsed = 0f;
        shockwaveSpawnFired = false;
        AnimatorTrigger("UltimateTrigger");
    }

    // Animation Event, no frame exato da queda do salto — nasce o anel, centrado na posição
    // atual. O salto é só visual (puramente de animação) — o Blood Mage NUNCA se desloca de
    // verdade, então "a posição de antes de saltar" e "a de pousar" são sempre a mesma.
    public void AnimationShockwaveSpawnEvent()
    {
        if (shockwaveSpawnFired) return;
        shockwaveSpawnFired = true;

        if (shockwavePrefab == null) return;

        float damagePerStage = stats.damage * shockwaveDamageMultiplier;
        var wave = Instantiate(shockwavePrefab, transform.position, Quaternion.identity);
        // Escala o GameObject — BloodMageShockwave já lê transform.localScale sozinho pra
        // calcular o raio real (dano) e o gizmo, então os 3 tamanhos base passam sem
        // pré-multiplicar (1 único lugar aplicando o multiplicador, não 2 desalinháveis).
        wave.transform.localScale = Vector3.one * shockwaveSizeMultiplier;
        wave.Activate(damagePerStage, shockwaveStage1Size, shockwaveStage2Size, shockwaveStage3Size, enemyLayerMask);
    }

    // Animation Event, no fim do clipe de Ultimate (o anel já está rodando sozinho, GameObject
    // independente — mesmo critério de qualquer Ultimate que nasce um filho e devolve o
    // controle antes dele terminar, ex.: a bomba do Rogue).
    public void AnimationUltimateEndEvent()
    {
        isAttacking = false;
    }

    // ===================== Secundária (Shift) — extração de sangue =====================

    protected override void UseSecondaryAbility()
    {
        isAttacking = true; // channel sem movimento — só durante a animação de sugar
        actionElapsed = 0f;
        extractHitFired = false;

        // Pose travada numa das 4 diagonais — mesmo critério do RollAimX/Y da Cambalhota / da
        // reza do Cleric.
        if (animator != null)
        {
            Vector2 pose = DirectionUtility.SnapTo4Diagonals(RawAimDirection);
            animator.SetFloat("ExtractAimX", pose.x);
            animator.SetFloat("ExtractAimY", pose.y);
        }

        AnimatorTrigger("SecondaryAbilityTrigger");
    }

    // Animation Event, no fim da reza — acerta até extractTargetCount monstros vivos mais
    // próximos (nunca repete, mesmo critério da vinha do Druid) e solta 1 orb de sangue por
    // alvo até o Blood Mage. A PARTIR DESTE EVENTO ele já pode se mover livremente — a cura só
    // acontece quando cada orb chegar, de forma independente (ver AnimationExtractBloodEndEvent
    // logo abaixo, que já libera o movimento sem esperar as orbs).
    public void AnimationExtractBloodEvent()
    {
        if (extractHitFired) return;
        extractHitFired = true;

        FindNearestLivingEnemies(extractTargetCount, extractTargets);
        if (bloodOrbPrefab == null) return;

        float damage = stats.damage * extractDamageMultiplier;
        foreach (var target in extractTargets)
        {
            // Status visual primeiro (1x só, por cima de qualquer outro Efeito do alvo), dano
            // e nascimento da orb na sequência — tudo no mesmo frame, mesmo critério de
            // qualquer outro Animation Event de impacto do projeto.
            var statusEffect = target.GetComponent<StatusEffectController>();
            if (statusEffect != null) statusEffect.FlashStatus(StatusEffectType.Drain, drainFlashDuration);

            // Dano REAL causado (nunca passa da vida que o alvo tinha — mesmo critério do
            // damageReserve do RangerArrow) — a cura tem que bater com o que a vida do monstro
            // realmente desceu, não com o dano nominal (pedido explícito do usuário).
            float actualDamage = Mathf.Min(damage, target.stats.health);
            target.TakeDamage(actualDamage);
            float healAmount = actualDamage * extractHealMultiplier;

            var obj = Instantiate(bloodOrbPrefab, target.transform.position, Quaternion.identity);
            var orb = obj.GetComponent<BloodOrb>();
            if (orb != null) orb.Launch(transform, orbSpeed, healAmount, OnBloodOrbArrived);
        }
    }

    // Mesmo critério de busca/seleção da vinha do Druid (AnimationVineSummonEvent) — busca por
    // área, descarta mortos (collider de cadáver ainda existe até o próprio "die" terminar),
    // ordena por distância e pega os N mais próximos.
    private void FindNearestLivingEnemies(int count, List<EnemyController> results)
    {
        results.Clear();

        var hits = Physics2D.OverlapCircleAll(transform.position, extractSearchRadius, enemyLayerMask);
        var enemies = new List<EnemyController>();
        foreach (var hit in hits)
        {
            var enemy = hit.GetComponent<EnemyController>();
            if (enemy == null || enemies.Contains(enemy)) continue;
            if (HealthSystem.IsDead(enemy.stats.health)) continue;
            enemies.Add(enemy);
        }

        enemies.Sort((a, b) =>
            Vector2.Distance(transform.position, a.transform.position)
                .CompareTo(Vector2.Distance(transform.position, b.transform.position)));

        int take = Mathf.Min(count, enemies.Count);
        for (int i = 0; i < take; i++) results.Add(enemies[i]);
    }

    // Callback do BloodOrb — chamado no instante exato em que CADA orb chega no Blood Mage
    // (posição ATUAL dele, não a de quando foi lançada — o jogador pode ter se movido livremente
    // durante a viagem). A cura em si é SEMPRE aplicada, mas a animação de "consumir sangue"
    // (consume_blood) só toca se o Blood Mage não estiver ocupado com nenhuma outra coisa
    // agora (isAttacking cobre tanto "já consumindo outra orb" quanto "no meio de qualquer
    // outra ação" — nunca interrompe nada) nem dentro da janela de debounce pós-consumo (ver
    // consumeCooldownRemaining). Sem isso, 2+ orbs chegando quase juntas prendiam o jogador
    // numa sequência de animações (pedido do usuário pra evitar).
    private void OnBloodOrbArrived(float healAmount)
    {
        Heal(healAmount);

        if (isAttacking || consumeCooldownRemaining > 0f) return;

        isAttacking = true;
        AnimatorTrigger("ConsumeBloodTrigger");
    }

    // Animation Event, no fim do clipe "consume_blood".
    public void AnimationConsumeBloodEndEvent()
    {
        isAttacking = false;
        consumeCooldownRemaining = consumeBloodCooldown;
    }

    // Animation Event, no fim do clipe de reza — libera o movimento aqui, SEM esperar nenhuma
    // orb chegar (elas continuam viajando/curando de forma independente depois disso).
    public void AnimationExtractBloodEndEvent()
    {
        isAttacking = false;
        isUsingSecondaryAbility = false;
    }

    // Sem override de CancelSecondaryAbility — não cancelável (GDD).

    // ===================== Passiva — pet Elemental de Sangue =====================

    // Cópia 1:1 do SummonPet/AnimationSummonPetEvent/AnimationSummonPetEndEvent do Mage —
    // mesmo PetController, mesma API (spawn-lock, multiplicador de tamanho e de velocidade de
    // ataque já embutidos em PetController.Initialize).
    private void SummonPet()
    {
        if (isAttacking) return;
        isAttacking = true;
        actionElapsed = 0f;
        AnimatorTrigger("SummonPetTrigger");
    }

    public void AnimationSummonPetEvent()
    {
        if (currentPet != null) Destroy(currentPet.gameObject);
        if (petPrefab == null) return;

        Vector3 spawnPos = petSpawnPoint != null ? petSpawnPoint.position : transform.position;
        var obj = Instantiate(petPrefab, spawnPos, Quaternion.identity);
        currentPet = obj.GetComponent<PetController>();
        if (currentPet != null) currentPet.Initialize(transform, petSpawnPoint, petSizeMultiplier, petActionSpeedMultiplier);
    }

    public void AnimationSummonPetEndEvent()
    {
        isAttacking = false;
    }

    // GDD (Seção 52, "Pet retorna junto") — o pet sobrevive à morte do herói e se teleporta
    // sozinho pro térreo (PetController.HandleFloorChanged(), já inscrito em
    // GameEvents.OnFloorChanged) — nenhuma limpeza extra necessária aqui, mesmo critério do Mage.
}
