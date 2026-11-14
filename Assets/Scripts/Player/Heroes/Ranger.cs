using UnityEngine;

public class Ranger : HeroController
{
    [SerializeField] private GameObject arrowPrefab;
    [SerializeField] private int arrowCount = 1; // 🔢 teto de 15, +1 por tier de arma — hook de Tier real chega na Sprint 33
    [SerializeField] private float attackDamageMultiplier = 2f; // 🔢 GDD Seção 17.2: dano total do golpe = stats.damage × isso

    // Formação em cunha (V) — todas as flechas viajam paralelas na mesma direção (a mira),
    // só nascem deslocadas: flechas de dentro nascem mais à frente, as de fora mais atrás e
    // mais afastadas lateralmente (mesma lógica de bando de pássaros voando).
    [SerializeField] private float arrowLateralStep = 0.5f; // 🔢 espaço entre flechas vizinhas — flecha tem 3px (~0.375u a 8 PPU), ajustável
    [SerializeField] private float arrowForwardStep = 0.3f; // 🔢 quanto cada rank nasce mais à frente, ajustável
    // Controlador decide TUDO sobre a flecha e repassa pro prefab em cada Launch() — ver
    // comentário no topo do RangerArrow.cs.
    [SerializeField] private float arrowSpeed = 10f; // 🔢 ajustável
    [SerializeField] private float arrowLifetime = 2f; // 🔢 alcance efetivo da flecha — placeholder de balanceamento
    [SerializeField] private float arrowKnockbackForce = 4f; // 🔢 ajustável — GDD Seção 17.2

    // Rede de segurança — mesmo padrão do Barbarian: se o Animation Event de fim nunca
    // disparar, força o fim da ação em vez de travar isAttacking pra sempre.
    [SerializeField] private float maxActionDuration = 3f; // 🔢 ajustável
    private float actionElapsed;

    // Attack é uma Blend Tree 2D Freeform Directional (8 pontos, N/NE/E/SE/S/SW/W/NW) —
    // igual o Attack do Barbarian, mesmo mecanismo. Isso mistura 2 clipes ao mesmo tempo pra
    // quase qualquer ângulo, e cada clipe carrega seu próprio Animation Event — por isso a
    // trava aqui é OBRIGATÓRIA (mesma correção aplicada em Barbarian.cs e
    // EnemyController.AnimationHitEvent()), não apenas defensiva.
    private bool shotFired;

    [Header("Ultimate — 8 facas nas 8 direções fixas (GDD Seção 17.2)")]
    [SerializeField] private GameObject knifePrefab; // precisa ter RangerKnife
    [SerializeField] private float ultimateDamageMultiplier = 2f; // 🔢 GDD: "2x o dano do Ranger" em voo, 1x no chão
    // Efeito Nocivo Bleeding (Sprint 19) — tanto o hit em voo quanto o tick no chão aplicam,
    // além do dano normal: metade do dano do Ranger por segundo, por 5s (ver RangerKnife).
    [SerializeField] private float bleedDamageMultiplier = 0.5f; // 🔢 "metade do dano do Ranger por segundo" — passível de nerf
    // Controlador decide TUDO sobre a faca e repassa pro prefab em cada Launch() — ver
    // comentário no topo do RangerKnife.cs.
    [SerializeField] private float knifeFlightSpeed = 12f; // 🔢 ajustável
    [SerializeField] private float knifeMaxDistance = 6f; // 🔢 alcance em voo
    [SerializeField] private float knifeFlightKnockbackForce = 4f; // 🔢 ajustável
    [SerializeField] private float knifeGroundedDuration = 30f; // GDD: 30s
    [SerializeField] private float knifeGroundedTickInterval = 0.5f; // 🔢 cadência do "dano contínuo" — GDD não especifica
    [SerializeField] private float knifeGroundedRadius = 0.6f; // 🔢 raio da área depois de pousar, maior que o collider de voo
    [SerializeField] private float knifeGroundedKnockbackForce = 2f; // 🔢 ajustável
    [SerializeField] private float knifeBleedDuration = 5f; // 🔢 passível de nerf/buff

    // A Ultimate também é Blend Tree 2D Freeform Directional (4 pontos diagonais, igual
    // Idle/Walk/Damage) — mesmo risco de evento duplicado do Attack, só que aqui cada uma
    // das 8 facas tem seu PRÓPRIO Animation Event/método (pedido explícito do usuário, não
    // um método único parametrizado), então a trava precisa ser por direção: um clique só
    // pode disparar o evento de uma direção 2x sem afetar as outras 7.
    private readonly bool[] knifeThrown = new bool[8];

    // Habilidade Secundária (Shift) — camuflagem (GDD Seção 16/17.2, Sprint 18→19). 3 fases,
    // cada uma 1 estado só sem direção (igual "die"): start (se escondendo) -> during
    // (escondido, teto de tempo — ver secondaryAbilityMaxDuration) -> end (surgindo).
    // Bloqueia tudo (isAttacking reaproveitado — mesmo gate que já bloqueia
    // PrimaryAttack/UseUltimate, e o movimento fica preso porque nenhum dos 3 estados é
    // "Walk"). Cancelável: Shift de novo adianta pra "end" antes do teto de tempo acabar.
    [Header("Habilidade Secundária (Shift) — camuflagem")]
    // % da Vida Máxima curada por tick, não valor fixo — com tick a cada 1s e teto de 5s na
    // fase "during", 10%/s dá um teto de cura total de 50% se o jogador ficar camuflado o
    // tempo inteiro (10% × 5 ticks) — o cap emerge do próprio tick rate × duração, não
    // precisa de uma variável de teto separada.
    [SerializeField] private float secondaryAbilityHealPercentPerTick = 0.1f; // 🔢 ajustável
    [SerializeField] private float secondaryAbilityHealTickInterval = 1f; // 🔢 ajustável
    [SerializeField] private float secondaryAbilityMaxDuration = 5f; // 🔢 teto de tempo camuflado, passível de nerf/buff — cancelável antes disso
    private AttackCooldown secondaryAbilityHealTick;
    private bool isCamouflaged; // só true durante a fase "during" — controla o tick de cura
    private float camouflageElapsed;

    // #25 "Veterano de Guerra" (GDD Seção 13) — troca a camuflagem por um modo visível/
    // vulnerável: sem animação de disfarce, sem bloquear isAttacking (Primary Attack/
    // Ultimate/movimento continuam livres), sem IsPlayerUntargetable/IsDamageImmune (o
    // "custo real" trocado) — só os efeitos de cura/etc. já comprados na árvore continuam
    // rodando, no mesmo timing de sempre. Duração cai 30% (compensação do próprio perk).
    // Decisão de design do GDD (Seção 13): "árvore única, sem fork" — não existem 2 trilhas
    // separadas, é uma progressão só, e #25 troca o MODO no topo, mantendo #1-24 intactos.
    private bool veteranoDeGuerraEnabled;

    // true só enquanto a camuflagem CLÁSSICA (fases "escondendo"/"reaparecendo") está usando
    // isAttacking pra bloquear tudo — nunca true no modo Veterano, onde isAttacking fica
    // livre pro Primary Attack/Ultimate normais disparar WHILE isCamouflaged também é true.
    // Sem este flag dedicado, a rede de segurança genérica do Attack/Ultimate em Update()
    // (que hoje usa "!isCamouflaged") ficaria cega pra qualquer ataque disparado durante um
    // Shift no modo Veterano, porque isCamouflaged fica true nesse modo também.
    private bool isAttackingForCamouflage;

    // #26 "Postura de Combate" + #30 "Imortal em Combate" — dano recebido -%, só enquanto o
    // Shift estiver ativo (os 2 somam, igual o padrão shiftDamageReductionPercent do
    // Barbarian). #30 exige #25 comprado (PerkDefinitionSO.prerequisitePerkId, checado em
    // PerkState.CanAfford() — não precisa ser checado de novo aqui).
    private float shiftDamageReductionPercent;

    // #27 "Fúria Curativa" — dano causado +%, só enquanto o Shift estiver ativo. Funciona nos
    // 2 modos, mas só é visível de verdade no modo Veterano — no modo clássico o Ranger não
    // ataca durante o disfarce, então nunca há dano causado pra bonificar.
    private float shiftDamageDealtBonusPercent;

    // #28 "Vigor de Batalha" — cura adicional (já em fração de Vida Máxima/tick, não %
    // inteiro) somada à cura base (secondaryAbilityHealPercentPerTick) — funciona nos 2 modos.
    private float shiftExtraHealPercentPerTick;

    public override string CentralPerkId => RangerPerkIds.Central;

    // Valores de design (Inspector) capturados 1x em Awake() — RecomputeEsquerda/Cima()
    // escalam a PARTIR destes, nunca do valor já escalado (senão cada compra de perk
    // composria em cima da última, igual o bug que já corrigimos no Barbarian).
    private float baseArrowSpeed, baseArrowLifetime, baseArrowKnockbackForce;
    private int baseArrowCount;
    private float baseKnifeFlightSpeed, baseKnifeMaxDistance, baseKnifeFlightKnockbackForce;
    private float baseKnifeGroundedDuration, baseKnifeGroundedRadius;
    private float baseBleedDamageMultiplier, baseKnifeBleedDuration;
    private float baseSecondaryAbilityHealPercentPerTick, baseSecondaryAbilityHealTickInterval;

    // Direita #13 "Emboscada Perfeita" — crítico garantido (x2) no 1º disparo após sair da
    // camuflagem, igual #26 "Ataque Furtivo" mas forçado, não um %/rank.
    private bool emboscadaPerfeitaEnabled;
    private bool emboscadaPerfeitaPending;

    // #14/#22 — redução de dano CONDICIONAL por limiar de HP (somam com
    // shiftDamageReductionPercent em GetConditionalDamageReductionPercent).
    private float lowHpDamageReductionPercent; // #14, abaixo de 20% HP
    private float criticalHpDamageReductionPercent; // #22, abaixo de 10% HP, binário

    // #16 "Véu Prolongado" — imunidade continua por X segundos depois de sair da camuflagem
    // CLÁSSICA (curto de propósito, GDD).
    private float postCamouflageImmunityDuration;
    private float postCamouflageImmunityRemaining;

    // #17 "Sombra Vingativa" — ao reaparecer, IsPlayerUntargetable continua true por mais X
    // segundos antes de soltar de vez (sem isso, EnemyController.ForceImmediateAggro() re-
    // adquire o alvo no exato frame em que a camuflagem acaba, porque GlobalAggroActive
    // continua true — ver comentário em EnemyController.Update()).
    private float untargetableGraceDuration;
    private float untargetableGraceRemaining;

    // #18 "Instinto de Sobrevivência" — cooldown -50%/rank SÓ abaixo de 30% HP, por cima do
    // -%/rank incondicional de #8-11 (baseCooldownReductionPercent) — precisa reavaliar a
    // cada frame (SetSecondaryCooldownMultiplier é um setter, não reativo, ao contrário de
    // GetConditionalDamageReductionPercent/GetConditionalMaxHealthBonusPercent, que são lidos
    // na hora certa pelo próprio HeroController).
    private float baseCooldownReductionPercent;
    private float lowHpCooldownReductionPercent;

    // #19 "Fôlego Sombrio" — HP máximo temporário +%/rank por alguns segundos ao sair da
    // camuflagem (não cura o atual, só abre um colchão) — GDD não dá o número de segundos.
    private float postCamouflageMaxHealthBonusPercent;
    private float postCamouflageMaxHealthRemaining;
    private const float PostCamouflageMaxHealthDuration = 5f; // 🔢 GDD: "alguns segundos", sem número exato

    // #21 "Instinto Predador" — regen passivo fora do disfarce (inclui tanto parado quanto
    // em combate normal, desde que não esteja escondido) — acumulador pra não repetir o bug
    // de floating text por frame já corrigido no Barbarian/HeroController.
    private float outOfCombatRegenPercentPerSecond;
    private float predatorRegenAccumulator, predatorRegenElapsed;

    // #12 "Disfarce Perfeito" — aura de lentidão em monstros próximos, só no modo CLÁSSICO
    // (categoria "armadilha do disfarce" do GDD — o Ranger é uma pedra/objeto parado; no modo
    // Veterano ele não é mais isso). Reaplicada todo frame via EnemyController.
    // ApplyExternalSlow() — decai sozinha se sair de perto, nunca precisa de "remover".
    private float perfectDisguiseSlowMultiplier = 1f; // 1 = sem efeito; ex. 0.9 = -10%
    private const float PerfectDisguiseRadius = 2.5f; // 🔢 GDD não dá o raio exato
    private static readonly Collider2D[] PerfectDisguiseHits = new Collider2D[16];

    // #15 "Retorno Sombrio" — reembolsa %/rank do cooldown gasto ao CANCELAR manualmente
    // (nunca no timeout). O cooldown real só começa quando isUsingSecondaryAbility vira
    // false de verdade (base HeroController.Update(), ver comentário em Update() abaixo) —
    // por isso isto é um "pedido pendente", consumido assim que essa transição acontecer,
    // nunca direto em CancelSecondaryAbility().
    private float cancelCooldownRefundPercent;
    private float pendingCooldownRefundPercent;

    // #20 "Reflexos nas Sombras" — fases de entrar/sair da camuflagem CLÁSSICA mais rápidas,
    // via Animator.speed (só durante essas 2 fases — nunca fica setado fora delas). Reset
    // garantido em 3 pontos: os 2 Animation Events de fim de fase normais, e OnHeroDeath()
    // como rede de segurança (se a morte interromper no meio da transição).
    private float transitionSpeedMultiplier = 1f;

    // #23 "Armadilha Camuflada" — reflete dano em quem tocar o disfarce sem saber (só modo
    // CLÁSSICO, mesma categoria "armadilha" do #12). dedup por HashSet (não TakeDamage() —
    // ele não repassa quem atacou, então a defesa é detectar o CONTATO físico direto no
    // Ranger, igual o EnemyContactDamage.cs já faz do lado do monstro).
    private bool armadilhaCamufladaEnabled;
    private readonly System.Collections.Generic.HashSet<EnemyController> armadilhaCamufladaHit = new();

    // Esquerda — "Dano" (Mira Afiada...Disparo Supremo) soma aqui, consumido direto em
    // AnimationShootEvent() (e também em OnTriggerEnter2D(), ver #23 mais abaixo).
    private float primaryDamagePercent;

    // #26 "Ataque Furtivo" — bônus só no PRIMEIRO disparo depois de sair da camuflagem
    // (qualquer um dos 2 modos). stealthAttackBonusPercent é o valor do perk; o flag abaixo
    // é consumido (1x só) no próximo AnimationShootEvent.
    private float stealthAttackBonusPercent;
    private bool stealthAttackBonusPending;

    // #27 "Tiro à Distância" — dano +%/rank conforme a distância percorrida, até o alcance
    // máximo (repassado pra RangerArrow calcular na hora do hit, onde a distância real
    // percorrida já é conhecida).
    private float distanceDamageBonusPercent;

    // #28 "Flechas Sangrentas" — chance/rank de aplicar Bleeding por flecha (repassado pra
    // RangerArrow rolar no hit).
    private float arrowBleedChancePercent;

    // #29/#30 — gates da Esquerda, repassados pra RangerArrow decidir o comportamento no
    // hit/fim de vida (ver RangerArrow.Launch()).
    private bool boomerangArrowEnabled;
    private bool infinitePierceEnabled;

    // Cima — "Dano em voo" (Lâmina Certeira...Lâmina Suprema) alimenta
    // HeroController.UltimateDamageMultiplier (mesmo static usado pelo Barbarian - genérico,
    // não exclusivo de 1 herói). "Dano no chão" não tem multiplicador próprio — vira sempre
    // metade do dano em voo (pedido do usuário, mesmo padrão do Ground Crack do Barbarian:
    // nunca mais um número fixo desconectado do personagem).
    private float bleedDamagePercent; // "Dano de sangramento" soma aqui

    // #6-8 "Facas Fincadas"/"Lâminas Cravadas"/"Armadilha Mortal" — multiplicam o dano no
    // chão JÁ calculado (metade do dano em voo), nunca o substituem (igual Fenda Enfurecida/
    // Selvagem em cima do Ground Crack do Barbarian).
    private float groundDamageBonusMultiplier = 1f;

    // #29 "Segunda Rajada" — 2ª rodada automática de 8 facas, 1s depois da primeira.
    private bool segundaRajadaEnabled;
    private bool segundaRajadaPending;
    private float segundaRajadaElapsed;
    private const float SegundaRajadaDelay = 1f;

    // #30 "Chuva de Lâminas" — cada lançamento solta 2 facas (±11,25°) em vez de 1.
    private bool chuvaDeLaminasEnabled;
    private const float ChuvaDeLaminasAngleOffset = 11.25f;

    protected override void Awake()
    {
        base.Awake();
        baseArrowSpeed = arrowSpeed;
        baseArrowLifetime = arrowLifetime;
        baseArrowKnockbackForce = arrowKnockbackForce;
        baseArrowCount = arrowCount;
        baseKnifeFlightSpeed = knifeFlightSpeed;
        baseKnifeMaxDistance = knifeMaxDistance;
        baseKnifeFlightKnockbackForce = knifeFlightKnockbackForce;
        baseKnifeGroundedDuration = knifeGroundedDuration;
        baseKnifeGroundedRadius = knifeGroundedRadius;
        baseBleedDamageMultiplier = bleedDamageMultiplier;
        baseKnifeBleedDuration = knifeBleedDuration;
        baseSecondaryAbilityHealPercentPerTick = secondaryAbilityHealPercentPerTick;
        baseSecondaryAbilityHealTickInterval = secondaryAbilityHealTickInterval;

        // Aplica de cara qualquer rank já comprado/salvo (Continue Game, ou reabrir a Loja
        // depois de já ter comprado algo nesta run) — mesmo motivo/mesmo padrão de
        // Barbarian.Awake(). Faltava aqui: sem isso, os 90 perks bespoke do Ranger (dano, vida,
        // flechas etc.) não reaplicavam ao nascer de novo (respawn/load).
        RecomputePerkEffects();
    }

    protected override void Update()
    {
        if (!GameplayGate.IsActive) return;

        base.Update();

        // !isAttackingForCamouflage aqui é essencial: a camuflagem CLÁSSICA tem seu próprio
        // teto de tempo (secondaryAbilityMaxDuration, maior que maxActionDuration) e sua
        // própria rede de segurança (camouflageElapsed, logo abaixo) — não pode usar a
        // mesma rede genérica do Attack/Ultimate, senão seria cortada cedo demais
        // (maxActionDuration é calibrado pro tiro único do primário, bem mais curto). Não dá
        // pra usar "!isCamouflaged" aqui (como era antes) porque no modo Veterano (#25)
        // isCamouflaged também fica true, só que isAttacking nesse caso é de um Primary
        // Attack/Ultimate normal disparado em paralelo — esses SIM precisam da rede genérica.
        if (isAttacking && !isAttackingForCamouflage)
        {
            actionElapsed += Time.deltaTime;
            if (actionElapsed >= maxActionDuration)
            {
                Debug.LogWarning("[Ranger] Animation Event de fim de ataque nunca chegou — forçando fim (verifique o Animator Controller).");
                isAttacking = false;
                isAttackResolving = false;
            }
        }

        if (isCamouflaged)
        {
            secondaryAbilityHealTick.Tick(Time.deltaTime);
            if (secondaryAbilityHealTick.TryConsume())
            {
                Heal(stats.maxHealth * (secondaryAbilityHealPercentPerTick + shiftExtraHealPercentPerTick));
            }

            camouflageElapsed += Time.deltaTime;
            // #25 "Veterano de Guerra" — a janela de efeitos dura 30% menos nesse modo.
            float maxDuration = veteranoDeGuerraEnabled ? secondaryAbilityMaxDuration * 0.7f : secondaryAbilityMaxDuration;
            if (camouflageElapsed >= maxDuration) EndCamouflage();
        }

        // #29 "Segunda Rajada" — dispara a 2ª rodada de 8 facas sozinha, sem novo input nem
        // nova animação (GDD: "automática, 1s depois da primeira").
        if (segundaRajadaPending)
        {
            segundaRajadaElapsed += Time.deltaTime;
            if (segundaRajadaElapsed >= SegundaRajadaDelay)
            {
                segundaRajadaPending = false;
                // knifeThrown trava 1 disparo por direção por rodada (evita duplicar via
                // Blend Tree) — tem que resetar pra essa 2ª rodada poder lançar de novo.
                for (int i = 0; i < knifeThrown.Length; i++) knifeThrown[i] = false;
                for (int i = 0; i < 8; i++) ThrowKnife(i);
            }
        }

        // #16/#17/#19 — janelas temporárias que decaem sozinhas (nunca negativas; não
        // precisam de evento, só contagem regressiva simples).
        if (postCamouflageImmunityRemaining > 0f) postCamouflageImmunityRemaining -= Time.deltaTime;
        if (postCamouflageMaxHealthRemaining > 0f) postCamouflageMaxHealthRemaining -= Time.deltaTime;
        if (untargetableGraceRemaining > 0f)
        {
            untargetableGraceRemaining -= Time.deltaTime;
            if (untargetableGraceRemaining <= 0f) IsPlayerUntargetable = false;
        }

        // #18 "Instinto de Sobrevivência" — reavaliado todo frame (SetSecondaryCooldownMultiplier
        // é um setter, não um hook reativo como GetConditionalDamageReductionPercent) porque
        // depende do HP ATUAL, que muda a qualquer momento fora de uma compra de perk.
        float hpFraction = stats.maxHealth > 0f ? stats.health / stats.maxHealth : 1f;
        float effectiveCooldownReduction = baseCooldownReductionPercent + (hpFraction < 0.3f ? lowHpCooldownReductionPercent : 0f);
        SetSecondaryCooldownMultiplier(1f - Mathf.Clamp01(effectiveCooldownReduction / 100f));

        // #21 "Instinto Predador" — regen passivo fora do disfarce (acumulador pra não
        // repetir o bug de floating text por frame, mesmo critério já corrigido no
        // Barbarian/HeroController.UpdateHpRegen()).
        if (!isCamouflaged && outOfCombatRegenPercentPerSecond > 0f)
        {
            float healed = Heal(stats.maxHealth * outOfCombatRegenPercentPerSecond / 100f * Time.deltaTime, showFloatingText: false);
            predatorRegenAccumulator += healed;
            predatorRegenElapsed += Time.deltaTime;
            if (predatorRegenElapsed >= 1f)
            {
                predatorRegenElapsed = 0f;
                if (predatorRegenAccumulator > 0f) GameEvents.HealReceived(GetFloatingTextSpawnPosition(), predatorRegenAccumulator);
                predatorRegenAccumulator = 0f;
            }
        }

        // #12 "Disfarce Perfeito" — só no modo clássico (o Ranger precisa estar
        // literalmente "parado fingindo ser objeto" pra fazer sentido como armadilha).
        // Reaplica o debuff em todo monstro na área TODO frame — ApplyExternalSlow() decai
        // sozinho, então sair do raio = voltar ao normal sem precisar de lógica de saída.
        if (isCamouflaged && !veteranoDeGuerraEnabled && perfectDisguiseSlowMultiplier < 1f)
        {
            int hitCount = Physics2D.OverlapCircle(transform.position, PerfectDisguiseRadius, ContactFilter2D.noFilter, PerfectDisguiseHits);
            for (int i = 0; i < hitCount; i++)
            {
                if (!PerfectDisguiseHits[i].CompareTag("Enemy")) continue;
                var enemy = PerfectDisguiseHits[i].GetComponent<EnemyController>();
                if (enemy != null) enemy.ApplyExternalSlow(perfectDisguiseSlowMultiplier, 0.2f);
            }
        }

        // #15 "Retorno Sombrio" — só aplica o reembolso QUANDO o cooldown real de verdade
        // começa a contar (base.Update(), chamado no topo deste método, já rodou ESTE mesmo
        // frame) — nunca direto em CancelSecondaryAbility(), porque no modo clássico o
        // cooldown só começa quando a animação de "reaparecendo" termina de tocar, não no
        // instante do cancelamento.
        if (pendingCooldownRefundPercent > 0f && !isUsingSecondaryAbility)
        {
            ReduceSecondaryCooldownRemaining(SecondaryAbilityCooldownDuration * pendingCooldownRefundPercent / 100f);
            pendingCooldownRefundPercent = 0f;
        }
    }

    protected override void PrimaryAttack()
    {
        if (isAttacking) return;
        isAttacking = true;
        actionElapsed = 0f;
        shotFired = false;
        AnimatorTrigger("AttackTrigger");
    }

    // Animation Event, no frame exato em que o Ranger solta as flechas.
    public void AnimationShootEvent()
    {
        if (shotFired) return;
        shotFired = true;

        // Direção crua do mouse (não travada nas 8 direções) — só a pose do personagem e a
        // sprite da flecha (RangerArrow.Launch escolhe o sub-sprite mais próximo) ficam
        // presas nas 8 poses possíveis; a trajetória de voo em si mira exato.
        Vector2 forward = RawAimDirection;
        Vector2 lateral = new Vector2(-forward.y, forward.x);

        // #26 "Ataque Furtivo" / Direita #13 "Emboscada Perfeita" — bônus/crítico garantido
        // só no 1º disparo depois de sair da camuflagem, consumidos aqui (no disparo em si,
        // não no hit — "primeiro disparo" é sobre a AÇÃO).
        float stealthBonus = stealthAttackBonusPending ? stealthAttackBonusPercent / 100f : 0f;
        stealthAttackBonusPending = false;
        float guaranteedCritMultiplier = emboscadaPerfeitaPending ? 2f : 1f;
        emboscadaPerfeitaPending = false;

        // Balanceamento (pedido do usuário): dividir o dano do golpe entre as flechas estava
        // quebrando o Ranger (mais flechas = cada uma mais fraca, sem ganho real) — GDD Seção
        // 17.2 original foi revertido. Cada flecha agora carrega o dano CHEIO do golpe como
        // sua própria reserva de perfuração, não mais uma fração. Esquerda "Dano" (#1-5) +
        // #26 + Central entram todos aqui, mesma fórmula do Barbarian.
        float reservePerArrow = stats.damage * attackDamageMultiplier * (1f + primaryDamagePercent / 100f)
            * (1f + stealthBonus) * guaranteedCritMultiplier * HeroController.CentralDamageMultiplier * GetShiftDamageDealtMultiplier();

        Vector2[] offsets = ArrowFormation.GetOffsets(arrowCount, arrowLateralStep, arrowForwardStep);
        foreach (var offset in offsets)
        {
            Vector3 spawnPos = transform.position + (Vector3)(forward * offset.x + lateral * offset.y);
            var arrowObj = Instantiate(arrowPrefab, spawnPos, Quaternion.identity, HeroController.HerosContainer);
            // #28 "Flechas Sangrentas" é "mesmo efeito da Ultimate" (GDD) — reaproveita a
            // magnitude/duração do bleed das facas (bleedDamageMultiplier/knifeBleedDuration),
            // só a CHANCE por flecha é própria desta família.
            arrowObj.GetComponent<RangerArrow>().Launch(forward, reservePerArrow, arrowSpeed, arrowLifetime, arrowKnockbackForce,
                distanceDamageBonusPercent, arrowBleedChancePercent, stats.damage * bleedDamageMultiplier, knifeBleedDuration,
                boomerangArrowEnabled, infinitePierceEnabled);
        }
    }

    // Animation Event, no fim do clipe de ataque — só aqui o cooldown real começa a contar
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
        for (int i = 0; i < knifeThrown.Length; i++) knifeThrown[i] = false;
        AnimatorTrigger("UltimateTrigger");

        // #29 "Segunda Rajada" — agenda a 2ª rodada automática (ver Update()).
        if (segundaRajadaEnabled)
        {
            segundaRajadaPending = true;
            segundaRajadaElapsed = 0f;
        }
    }

    // 8 Animation Events, um por direção fixa (E/NE/N/NW/W/SW/S/SE, mesma ordem de
    // DirectionUtility) — método próprio por direção em vez de 1 método parametrizado,
    // pra ligar cada um no frame exato do giro em que aquela direção "atira" de verdade.
    public void AnimationThrowKnife_E() => ThrowKnife(0);
    public void AnimationThrowKnife_NE() => ThrowKnife(1);
    public void AnimationThrowKnife_N() => ThrowKnife(2);
    public void AnimationThrowKnife_NW() => ThrowKnife(3);
    public void AnimationThrowKnife_W() => ThrowKnife(4);
    public void AnimationThrowKnife_SW() => ThrowKnife(5);
    public void AnimationThrowKnife_S() => ThrowKnife(6);
    public void AnimationThrowKnife_SE() => ThrowKnife(7);

    private void ThrowKnife(int directionIndex)
    {
        if (knifeThrown[directionIndex]) return;
        knifeThrown[directionIndex] = true;

        Vector2 dir = DirectionUtility.DirectionFromIndex(directionIndex);
        float shiftMultiplier = GetShiftDamageDealtMultiplier();
        // Cima "Dano em voo" (#1-5) + Central entram aqui, mesma fórmula do Barbarian.
        float flightDamage = stats.damage * ultimateDamageMultiplier * HeroController.UltimateDamageMultiplier
            * HeroController.CentralDamageMultiplier * shiftMultiplier;
        // "Dano no chão" (pedido do usuário, 2026-11-11) nunca mais um número fixo
        // desconectado do personagem — sempre metade do dano em voo, piso 1 (mesma solução
        // do Ground Crack do Barbarian). #6-8 multiplicam ESSE valor por cima.
        float groundedDamage = Mathf.Max(1f, flightDamage * 0.5f) * groundDamageBonusMultiplier;
        float bleedDamagePerSecond = stats.damage * bleedDamageMultiplier * shiftMultiplier;

        if (chuvaDeLaminasEnabled)
        {
            // #30 "Chuva de Lâminas" — 2 facas por lançamento, pequeno desvio angular entre
            // elas (a animação continua sendo a do estado mais próximo, só a trajetória real
            // de voo diverge — mesmo critério de RawAimDirection na RangerArrow).
            LaunchKnife(RotateDirection(dir, ChuvaDeLaminasAngleOffset), flightDamage, groundedDamage, bleedDamagePerSecond);
            LaunchKnife(RotateDirection(dir, -ChuvaDeLaminasAngleOffset), flightDamage, groundedDamage, bleedDamagePerSecond);
        }
        else
        {
            LaunchKnife(dir, flightDamage, groundedDamage, bleedDamagePerSecond);
        }
    }

    private void LaunchKnife(Vector2 dir, float flightDamage, float groundedDamage, float bleedDamagePerSecond)
    {
        var knifeObj = Instantiate(knifePrefab, transform.position, Quaternion.identity, HeroController.HerosContainer);
        knifeObj.GetComponent<RangerKnife>().Launch(dir, flightDamage, groundedDamage, bleedDamagePerSecond,
            knifeFlightSpeed, knifeMaxDistance, knifeFlightKnockbackForce, knifeGroundedDuration, knifeGroundedTickInterval, knifeGroundedRadius, knifeGroundedKnockbackForce, knifeBleedDuration);
    }

    private static Vector2 RotateDirection(Vector2 dir, float angleDeg)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad), sin = Mathf.Sin(rad);
        return new Vector2(dir.x * cos - dir.y * sin, dir.x * sin + dir.y * cos);
    }

    // Animation Event, no fim do clipe da Ultimate.
    public void AnimationUltimateEndEvent()
    {
        isAttacking = false;
    }

    protected override void UseSecondaryAbility()
    {
        // #25 "Veterano de Guerra" — modo visível: sem animação, sem bloquear isAttacking
        // (Primary Attack/Ultimate/movimento continuam livres), sem IsPlayerUntargetable —
        // só os efeitos de cura/etc. já comprados na árvore rodando, no mesmo timing de
        // sempre (BeginCamouflageEffects() é a MESMA lógica que o modo clássico só dispara
        // via Animation Event, ver AnimationCamouflageHiddenEvent()).
        if (veteranoDeGuerraEnabled)
        {
            BeginCamouflageEffects();
            return;
        }

        // isAttacking bloqueia PrimaryAttack/UseUltimate (mesma trava que eles já usam) e,
        // como nenhum dos 3 estados da camuflagem é "Walk", o movimento já fica preso
        // sozinho (regra geral do HeroController, Seção 16) — não precisa de flag nova.
        isAttacking = true;
        isAttackingForCamouflage = true;
        actionElapsed = 0f;
        // #20 "Reflexos nas Sombras" — acelera só a fase "escondendo" (Animator.speed é
        // GLOBAL, então isto nunca fica setado fora das 2 fases de transição — ver os 2
        // pontos de reset: AnimationCamouflageHiddenEvent() logo abaixo e OnHeroDeath()).
        if (animator != null) animator.speed = transitionSpeedMultiplier;
        AnimatorTrigger("SecondaryAbilityTrigger");
    }

    // Animation Event, no fim do clipe de "se escondendo" — o Animator já transiciona
    // sozinho pro estado "during" (Exit Time, sem condição), esse evento só sincroniza a
    // parte de código (começa a cura, avisa os monstros que o player sumiu). Só existe no
    // modo clássico (sem #25) — o modo Veterano chama BeginCamouflageEffects() direto em
    // UseSecondaryAbility(), sem esperar clipe nenhum.
    public void AnimationCamouflageHiddenEvent()
    {
        BeginCamouflageEffects();
        IsPlayerUntargetable = true; // só no modo clássico (único que dispara este evento)
        // #20 "Reflexos nas Sombras" — fim da fase "escondendo", volta a velocidade normal
        // (a fase "during" nunca precisa de velocidade alterada).
        if (animator != null) animator.speed = 1f;
        armadilhaCamufladaHit.Clear(); // #23 — nova janela de disfarce, pode refletir de novo nos mesmos monstros
    }

    // Compartilhado pelos 2 modos — liga o tick de cura e o timer da janela de efeitos. O
    // que muda entre os modos é só QUEM chama isto (Animation Event vs. direto em
    // UseSecondaryAbility()) e se IsPlayerUntargetable/IsDamageImmune também ligam.
    private void BeginCamouflageEffects()
    {
        isCamouflaged = true;
        camouflageElapsed = 0f;
        secondaryAbilityHealTick = new AttackCooldown(secondaryAbilityHealTickInterval);
    }

    // Imune a dano a viagem INTEIRA da camuflagem CLÁSSICA (start + during + end), não só o
    // "during" — um Slime de contato ou um golpe que já estava a caminho não pode contar
    // enquanto o Ranger está se escondendo/escondido/reaparecendo. #25 "Veterano de Guerra"
    // remove essa imunidade por completo (GDD Seção 13: "o custo real" trocado por poder
    // continuar lutando) — #26/#30 (redução de dano recebido) existem justamente pra
    // compensar essa vulnerabilidade nova, não pra substituir a imunidade de antes.
    // #16 "Véu Prolongado" soma uma janela extra de imunidade depois do modo clássico acabar
    // (postCamouflageImmunityRemaining), fora da viagem normal da camuflagem.
    protected override bool IsDamageImmune =>
        (isUsingSecondaryAbility && !veteranoDeGuerraEnabled) || postCamouflageImmunityRemaining > 0f;

    // #26 "Postura de Combate" + #30 "Imortal em Combate" (Shift ativo) + #14/#22 (limiar de
    // HP baixo/crítico, incondicional do Shift) — todos somam.
    protected override float GetConditionalDamageReductionPercent()
    {
        float percent = isUsingSecondaryAbility ? shiftDamageReductionPercent : 0f;
        float hpFraction = stats.maxHealth > 0f ? stats.health / stats.maxHealth : 1f;
        if (hpFraction < 0.2f) percent += lowHpDamageReductionPercent; // #14
        if (hpFraction < 0.1f) percent += criticalHpDamageReductionPercent; // #22
        return percent;
    }

    // #19 "Fôlego Sombrio" — HP máximo temporário, só durante a janela pós-camuflagem.
    protected override float GetConditionalMaxHealthBonusPercent() =>
        postCamouflageMaxHealthRemaining > 0f ? postCamouflageMaxHealthBonusPercent : 0f;

    // #27 "Fúria Curativa" — dano causado +%, só enquanto o Shift estiver ativo.
    private float GetShiftDamageDealtMultiplier() =>
        isUsingSecondaryAbility ? 1f + shiftDamageDealtBonusPercent / 100f : 1f;

    // Shift de novo durante a camuflagem — só faz efeito depois que a janela de efeitos
    // realmente começou (cancelar no meio da animação de esconder, no modo clássico, ainda
    // não existe como caso de uso — pré-existente, não mudou aqui).
    protected override void CancelSecondaryAbility()
    {
        if (!isCamouflaged) return;
        // #15 "Retorno Sombrio" — só arma o pedido de reembolso aqui (cancelamento MANUAL,
        // nunca no timeout automático de Update()) — consumido mais tarde, no frame exato em
        // que o cooldown de verdade começa a contar (ver Update()).
        if (cancelCooldownRefundPercent > 0f) pendingCooldownRefundPercent = cancelCooldownRefundPercent;
        EndCamouflage();
    }

    // Compartilhado entre o cancelamento manual (Shift de novo) e o teto de tempo
    // (secondaryAbilityMaxDuration, já reduzido 30% no modo Veterano — estourado) — os dois
    // terminam a janela de efeitos do mesmo jeito.
    private void EndCamouflage()
    {
        isCamouflaged = false;
        // #26 "Ataque Furtivo" / #13 "Emboscada Perfeita" — armam o bônus do próximo disparo,
        // nos 2 modos (clássico ou Veterano) — "sair da camuflagem" vale pros 2, é a mesma
        // janela de efeitos.
        stealthAttackBonusPending = true;
        if (emboscadaPerfeitaEnabled) emboscadaPerfeitaPending = true;

        // #16/#17/#19 só fazem sentido no modo CLÁSSICO (onde existe disfarce/untargetable
        // de verdade pra estender) — no modo Veterano não há nada pra prolongar, solta
        // IsPlayerUntargetable na hora (nem chegou a ligar nesse modo, mas reforça por
        // segurança).
        if (veteranoDeGuerraEnabled)
        {
            IsPlayerUntargetable = false;
            // Sem clipe de "reaparecendo" nesse modo — fecha a janela direto, sem animação
            // (o Animation Event AnimationCamouflageEndEvent() nunca dispararia pra avisar).
            isUsingSecondaryAbility = false;
            return;
        }

        if (postCamouflageImmunityDuration > 0f) postCamouflageImmunityRemaining = postCamouflageImmunityDuration;
        if (postCamouflageMaxHealthBonusPercent > 0f) postCamouflageMaxHealthRemaining = PostCamouflageMaxHealthDuration;
        // #17 "Sombra Vingativa" — mantém IsPlayerUntargetable=true por mais X segundos em
        // vez de soltar na hora (ver o decaimento em Update()); sem isso,
        // EnemyController.ForceImmediateAggro() readquire o alvo no mesmo frame.
        if (untargetableGraceDuration > 0f) untargetableGraceRemaining = untargetableGraceDuration;
        else IsPlayerUntargetable = false;

        // #20 "Reflexos nas Sombras" — acelera a fase "reaparecendo" (reset no Animation
        // Event logo abaixo).
        if (animator != null) animator.speed = transitionSpeedMultiplier;
        AnimatorTrigger("SecondaryAbilityEndTrigger");
    }

    // Animation Event, no fim do clipe de "reaparecendo" — só existe no modo clássico.
    public void AnimationCamouflageEndEvent()
    {
        isAttacking = false;
        isAttackingForCamouflage = false;
        isUsingSecondaryAbility = false;
        if (animator != null) animator.speed = 1f; // #20 — fim da fase acelerada
    }

    // #20 "Reflexos nas Sombras" — rede de segurança: se a morte interromper o Ranger NO
    // MEIO de uma fase acelerada (animator.speed != 1), sem isso o respawn nasceria com
    // TODAS as animações (não só a camuflagem) rodando na velocidade errada pra sempre.
    protected override void OnHeroDeath()
    {
        base.OnHeroDeath();
        if (animator != null) animator.speed = 1f;
    }

    // Tabela compartilhada pelos 2 capstones de dano (Disparo Supremo/Lâmina Suprema) — mesmo
    // formato/escala usados em Fúria Definitiva/Juízo Final do Barbarian (rank1 = valor
    // original do GDD, depois saltos crescentes até ~1,875×10^13% no rank 5), calibrados pra
    // chegar no mesmo ~1,2 trilhão de dano-alvo contra os monstros Tier9/10 (ver chat
    // 2026-11-11) com a família inteira (#1-5) maxada + Central.
    private static readonly float[] DamageCapstonePercent = { 0f, 40f, 400f, 20000f, 4000000f, 18750000000000f };

    protected override float UltimateChargeRequiredMultiplier => ultimateChargeRequiredPercent / 100f;
    private float ultimateChargeRequiredPercent = 100f;

    // Cima #27 "Instinto de Caça" (incondicional) + Direita #24 "Instinto Caçador" (só
    // enquanto o Shift estiver em cooldown) — campos SEPARADOS de propósito: RecomputeCima()/
    // RecomputeDireita() rodam em sequência dentro do mesmo RecomputePerkEffects(), e se os
    // 2 escrevessem no MESMO campo, o que roda depois sobrescreveria o outro (bug real que
    // cheguei a introduzir e corrigir ainda nesta sessão). Combinados só aqui, na leitura.
    protected override float UltimateEnergyMultiplier =>
        1f + (ultimateEnergyGainBonusPercent + (!IsSecondaryAbilityCooldownReady ? direitaEnergyGainBonusPercent : 0f)) / 100f;
    private float ultimateEnergyGainBonusPercent;
    private float direitaEnergyGainBonusPercent;

    // Chamado no fim de Awake() (HeroController) e por PerkState a cada compra/undo.
    public override void RecomputePerkEffects()
    {
        RecomputeEsquerda();
        RecomputeDireita();
        RecomputeCima();
    }

    private void RecomputeEsquerda()
    {
        // "Dano" (#1-5) — #5 "Disparo Supremo" é o capstone (tabela, não rank*X).
        float disparoSupremoRank = PerkState.GetRank(RangerPerkIds.EsquerdaDisparoSupremo);
        primaryDamagePercent = PerkState.GetRank(RangerPerkIds.EsquerdaMiraAfiada) * 8f
            + PerkState.GetRank(RangerPerkIds.EsquerdaPrecisaoMortal) * 15f
            + PerkState.GetRank(RangerPerkIds.EsquerdaTiroCerteiro) * 20f
            + PerkState.GetRank(RangerPerkIds.EsquerdaChuvaMortal) * 30f
            + DamageCapstonePercent[Mathf.Clamp((int)disparoSupremoRank, 0, 5)];

        float speedPercent = PerkState.GetRank(RangerPerkIds.EsquerdaFlechaVeloz) * 10f
            + PerkState.GetRank(RangerPerkIds.EsquerdaVentoCortante) * 15f
            + PerkState.GetRank(RangerPerkIds.EsquerdaVelocidadeSonica) * 20f;
        arrowSpeed = baseArrowSpeed * (1f + speedPercent / 100f);

        float rangePercent = PerkState.GetRank(RangerPerkIds.EsquerdaAlcanceEstendido) * 10f
            + PerkState.GetRank(RangerPerkIds.EsquerdaVistaDeAguia) * 15f
            + PerkState.GetRank(RangerPerkIds.EsquerdaHorizonteInfinito) * 20f;
        arrowLifetime = baseArrowLifetime * (1f + rangePercent / 100f);

        float knockbackPercent = PerkState.GetRank(RangerPerkIds.EsquerdaImpactoPerfurante) * 10f
            + PerkState.GetRank(RangerPerkIds.EsquerdaForcaDoDisparo) * 15f
            + PerkState.GetRank(RangerPerkIds.EsquerdaOndaDeChoque) * 20f;
        arrowKnockbackForce = baseArrowKnockbackForce * (1f + knockbackPercent / 100f);

        int extraArrows = PerkState.GetRank(RangerPerkIds.EsquerdaCarcazCheio)
            + PerkState.GetRank(RangerPerkIds.EsquerdaChuvaDeFlechas)
            + PerkState.GetRank(RangerPerkIds.EsquerdaTempestadeDeSetas);
        arrowCount = baseArrowCount + extraArrows;

        float cooldownPercent = PerkState.GetRank(RangerPerkIds.EsquerdaMaosRapidas) * 5f
            + PerkState.GetRank(RangerPerkIds.EsquerdaReflexoDeCacador) * 8f
            + PerkState.GetRank(RangerPerkIds.EsquerdaInstintoAgil) * 10f;
        SetPrimaryCooldownMultiplier(1f - Mathf.Clamp01(cooldownPercent / 100f));

        float animSpeedPercent = PerkState.GetRank(RangerPerkIds.EsquerdaDisparoRapido) * 10f
            + PerkState.GetRank(RangerPerkIds.EsquerdaFluidezDeCombate) * 15f
            + PerkState.GetRank(RangerPerkIds.EsquerdaPrecisaoVeloz) * 20f;
        SetActionSpeedMultiplier(1f + animSpeedPercent / 100f);

        float critPercent = PerkState.GetRank(RangerPerkIds.EsquerdaGolpeVital) * 5f
            + PerkState.GetRank(RangerPerkIds.EsquerdaOlhoDeFalcao) * 8f;
        HeroController.PrimaryCritChance = Mathf.Clamp01(critPercent / 100f);

        stealthAttackBonusPercent = PerkState.GetRank(RangerPerkIds.EsquerdaAtaqueFurtivo) * 20f;
        distanceDamageBonusPercent = PerkState.GetRank(RangerPerkIds.EsquerdaTiroADistancia) * 20f;
        // 🔢 PLACEHOLDER — GDD (#28) só diz "%/rank" sem valor exato, mesmo critério usado
        // nos placeholders do Barbarian/Direita do Ranger.
        arrowBleedChancePercent = PerkState.GetRank(RangerPerkIds.EsquerdaFlechasSangrentas) * 15f;
        boomerangArrowEnabled = PerkState.GetRank(RangerPerkIds.EsquerdaFlechaBumerangue) >= 1;
        infinitePierceEnabled = PerkState.GetRank(RangerPerkIds.EsquerdaEnxamePerfurante) >= 1;
    }

    private void RecomputeCima()
    {
        // "Dano em voo" (#1-5) — #5 "Lâmina Suprema" é o capstone, mesma tabela de Disparo
        // Supremo (famílias idênticas: 8/15/20/30/40%).
        float laminaSupremaRank = PerkState.GetRank(RangerPerkIds.CimaLaminaSuprema);
        float ultDanoPercent = PerkState.GetRank(RangerPerkIds.CimaLaminaCerteira) * 8f
            + PerkState.GetRank(RangerPerkIds.CimaCorteProfundo) * 15f
            + PerkState.GetRank(RangerPerkIds.CimaFuriaDasLaminas) * 20f
            + PerkState.GetRank(RangerPerkIds.CimaTempestadeDeAco) * 30f
            + DamageCapstonePercent[Mathf.Clamp((int)laminaSupremaRank, 0, 5)];
        HeroController.UltimateDamageMultiplier = 1f + ultDanoPercent / 100f;

        // "Dano no chão" (#6-8) — multiplica a metade-do-dano-em-voo já calculada em
        // ThrowKnife(), nunca a substitui (pedido do usuário, 2026-11-11).
        float groundPercent = PerkState.GetRank(RangerPerkIds.CimaFacasFincadas) * 8f
            + PerkState.GetRank(RangerPerkIds.CimaLaminasCravadas) * 15f
            + PerkState.GetRank(RangerPerkIds.CimaArmadilhaMortal) * 20f;
        groundDamageBonusMultiplier = 1f + groundPercent / 100f;

        float bleedPercent = PerkState.GetRank(RangerPerkIds.CimaSangramentoIntenso) * 10f
            + PerkState.GetRank(RangerPerkIds.CimaHemorragia) * 15f
            + PerkState.GetRank(RangerPerkIds.CimaFeridaFatal) * 20f;
        bleedDamageMultiplier = baseBleedDamageMultiplier * (1f + bleedPercent / 100f);

        float flightSpeedPercent = PerkState.GetRank(RangerPerkIds.CimaVooVeloz) * 10f
            + PerkState.GetRank(RangerPerkIds.CimaLaminaRelampago) * 15f
            + PerkState.GetRank(RangerPerkIds.CimaVelocidadeMortal) * 20f;
        knifeFlightSpeed = baseKnifeFlightSpeed * (1f + flightSpeedPercent / 100f);

        float flightRangePercent = PerkState.GetRank(RangerPerkIds.CimaAlcanceDaLamina) * 10f
            + PerkState.GetRank(RangerPerkIds.CimaLongoVoo) * 15f
            + PerkState.GetRank(RangerPerkIds.CimaHorizonteDeAco) * 20f;
        knifeMaxDistance = baseKnifeMaxDistance * (1f + flightRangePercent / 100f);

        float flightKnockbackPercent = PerkState.GetRank(RangerPerkIds.CimaImpactoDaLamina) * 10f
            + PerkState.GetRank(RangerPerkIds.CimaForcaCortante) * 15f;
        knifeFlightKnockbackForce = baseKnifeFlightKnockbackForce * (1f + flightKnockbackPercent / 100f);

        float groundedDurationPercent = PerkState.GetRank(RangerPerkIds.CimaArmadilhaDuradoura) * 10f
            + PerkState.GetRank(RangerPerkIds.CimaLaminaPersistente) * 15f
            + PerkState.GetRank(RangerPerkIds.CimaCampoMinado) * 20f;
        knifeGroundedDuration = baseKnifeGroundedDuration * (1f + groundedDurationPercent / 100f);

        float groundedRadiusPercent = PerkState.GetRank(RangerPerkIds.CimaAreaAmpliada) * 10f
            + PerkState.GetRank(RangerPerkIds.CimaZonaDePerigo) * 15f;
        knifeGroundedRadius = baseKnifeGroundedRadius * (1f + groundedRadiusPercent / 100f);

        float bleedDurationPercent = PerkState.GetRank(RangerPerkIds.CimaVenenoProlongado) * 15f;
        knifeBleedDuration = baseKnifeBleedDuration * (1f + bleedDurationPercent / 100f);

        float chargeReductionPercent = PerkState.GetRank(RangerPerkIds.CimaCanalizacaoRapida) * 5f;
        ultimateChargeRequiredPercent = 100f - Mathf.Clamp(chargeReductionPercent, 0f, 100f);

        ultimateEnergyGainBonusPercent = PerkState.GetRank(RangerPerkIds.CimaInstintoDeCaca) * 15f;

        float ultCritPercent = PerkState.GetRank(RangerPerkIds.CimaGolpeCriticoSombrio) * 5f;
        HeroController.UltimateCritChance = Mathf.Clamp01(ultCritPercent / 100f);

        segundaRajadaEnabled = PerkState.GetRank(RangerPerkIds.CimaSegundaRajada) >= 1;
        chuvaDeLaminasEnabled = PerkState.GetRank(RangerPerkIds.CimaChuvaDeLaminas) >= 1;
    }

    private void RecomputeDireita()
    {
        // "Cura por tick" (#1-4) — soma em PONTOS percentuais direto na fração de cura/tick
        // (ex.: rank5 de Regeneração Furtiva sozinho = +10 pontos em cima dos 10% base = 20%
        // por tick) — não é um multiplicador do valor, é aditivo, consistente com a escala
        // dos números do GDD (2-5%) serem comparáveis à base (10%).
        float healBonusPoints = PerkState.GetRank(RangerPerkIds.DireitaRegeneracaoFurtiva) * 2f
            + PerkState.GetRank(RangerPerkIds.DireitaCuraSilenciosa) * 3f
            + PerkState.GetRank(RangerPerkIds.DireitaRenovacaoNaSombra) * 4f
            + PerkState.GetRank(RangerPerkIds.DireitaVidaOculta) * 5f;
        secondaryAbilityHealPercentPerTick = baseSecondaryAbilityHealPercentPerTick + healBonusPoints / 100f;

        // "Frequência da cura" (#5-7) — reduz o intervalo entre ticks.
        float tickIntervalReductionPercent = PerkState.GetRank(RangerPerkIds.DireitaPulsoVital) * 10f
            + PerkState.GetRank(RangerPerkIds.DireitaFluxoConstante) * 15f
            + PerkState.GetRank(RangerPerkIds.DireitaRitmoDaSombra) * 20f;
        secondaryAbilityHealTickInterval = baseSecondaryAbilityHealTickInterval * (1f - Mathf.Clamp01(tickIntervalReductionPercent / 100f));

        // "Cooldown" (#8-11) + #29 "Reflexos de Veterano" (achado faltando numa auditoria de
        // descrição, 2026-11-12 — nunca tinha entrado nesta soma) — incondicional; combinado
        // com #18 (condicional, HP baixo) todo frame em Update(), não aqui
        // (SetSecondaryCooldownMultiplier não é reativo).
        baseCooldownReductionPercent = PerkState.GetRank(RangerPerkIds.DireitaReflexoFurtivo) * 5f
            + PerkState.GetRank(RangerPerkIds.DireitaInstintoSilencioso) * 8f
            + PerkState.GetRank(RangerPerkIds.DireitaSombraVeloz) * 10f
            + PerkState.GetRank(RangerPerkIds.DireitaDissipacaoRapida) * 15f
            + PerkState.GetRank(RangerPerkIds.DireitaReflexosDeVeterano) * 20f;

        // #12 "Disfarce Perfeito" — 🔢 PLACEHOLDER, GDD não dá o %/rank exato.
        float disfarcePerfeitoPercent = PerkState.GetRank(RangerPerkIds.DireitaDisfarcePerfeito) * 10f;
        perfectDisguiseSlowMultiplier = 1f - Mathf.Clamp01(disfarcePerfeitoPercent / 100f);

        // #13 "Emboscada Perfeita" — binário.
        emboscadaPerfeitaEnabled = PerkState.GetRank(RangerPerkIds.DireitaEmboscadaPerfeita) >= 1;

        // #14 "Reflexos de Emergência" — redução condicional (abaixo de 20% HP).
        lowHpDamageReductionPercent = PerkState.GetRank(RangerPerkIds.DireitaReflexosDeEmergencia) * 10f;

        // #15 "Retorno Sombrio" — 🔢 PLACEHOLDER, GDD não dá o %/rank exato.
        cancelCooldownRefundPercent = PerkState.GetRank(RangerPerkIds.DireitaRetornoSombrio) * 15f;

        // #16 "Véu Prolongado" — 0.5/1/1.5s por rank (tabela, não rank*X).
        int veuProlongadoRank = PerkState.GetRank(RangerPerkIds.DireitaVeuProlongado);
        float[] veuProlongadoDuration = { 0f, 0.5f, 1f, 1.5f };
        postCamouflageImmunityDuration = veuProlongadoDuration[Mathf.Clamp(veuProlongadoRank, 0, 3)];

        // #17 "Sombra Vingativa" — segundos/rank de perda de alvo ao reaparecer.
        // 🔢 PLACEHOLDER — GDD não dá o número exato de segundos/rank.
        untargetableGraceDuration = PerkState.GetRank(RangerPerkIds.DireitaSombraVingativa) * 1f;

        // #18 "Instinto de Sobrevivência" — cooldown -50%/rank condicional (abaixo de 30% HP).
        lowHpCooldownReductionPercent = PerkState.GetRank(RangerPerkIds.DireitaInstintoDeSobrevivencia) * 50f;

        // #19 "Fôlego Sombrio" — HP máximo temporário +%/rank.
        // 🔢 PLACEHOLDER — GDD não dá o %/rank exato.
        postCamouflageMaxHealthBonusPercent = PerkState.GetRank(RangerPerkIds.DireitaFolegoSombrio) * 10f;

        // #20 "Reflexos nas Sombras" — 🔢 PLACEHOLDER, GDD não dá o %/rank exato.
        float reflexosNasSombrasPercent = PerkState.GetRank(RangerPerkIds.DireitaReflexosNasSombras) * 10f;
        transitionSpeedMultiplier = 1f + reflexosNasSombrasPercent / 100f;

        // #21 "Instinto Predador" — regen passivo fora do disfarce.
        // 🔢 PLACEHOLDER — GDD não dá o %/rank exato.
        outOfCombatRegenPercentPerSecond = PerkState.GetRank(RangerPerkIds.DireitaInstintoPredador) * 1f;

        // #22 "Última Linha de Defesa" — binário, -40% abaixo de 10% HP.
        criticalHpDamageReductionPercent = PerkState.GetRank(RangerPerkIds.DireitaUltimaLinhaDeDefesa) >= 1 ? 40f : 0f;

        // #23 "Armadilha Camuflada" — binário.
        armadilhaCamufladaEnabled = PerkState.GetRank(RangerPerkIds.DireitaArmadilhaCamuflada) >= 1;

        // #24 "Instinto Caçador" — ganho de energia da Ultimate +%/rank, enquanto a
        // camuflagem estiver em cooldown (condição avaliada na leitura, em
        // UltimateEnergyMultiplier — ver comentário lá).
        direitaEnergyGainBonusPercent = PerkState.GetRank(RangerPerkIds.DireitaInstintoCacador) * 10f;

        veteranoDeGuerraEnabled = PerkState.GetRank(RangerPerkIds.DireitaVeteranoDeGuerra) >= 1;

        // #26/#27/#28 — 🔢 PLACEHOLDER, GDD só diz "%/rank" sem valor exato (mesmo critério
        // de vários casos do Barbarian). #30 "Imortal em Combate" é 1 rank, "drasticamente" —
        // valor grande (60%) escolhido na mesma linha.
        shiftDamageReductionPercent = PerkState.GetRank(RangerPerkIds.DireitaPosturaDeCombate) * 10f
            + (PerkState.GetRank(RangerPerkIds.DireitaImortalEmCombate) >= 1 ? 60f : 0f);
        shiftDamageDealtBonusPercent = PerkState.GetRank(RangerPerkIds.DireitaFuriaCurativa) * 15f;
        shiftExtraHealPercentPerTick = PerkState.GetRank(RangerPerkIds.DireitaVigorDeBatalha) * 5f / 100f;
    }

    // Sem Card Framework ainda (Sprint 35) — incremento manual só pra provar o leque nesta sprint.
    [ContextMenu("Debug: +1 Flecha")]
    private void DebugIncreaseArrowCount()
    {
        arrowCount++;
        Debug.Log($"[Ranger] arrowCount = {arrowCount}");
    }

    // #23 "Armadilha Camuflada" — monstro que TOCA o disfarce (dano de contato, ex.: Slime,
    // via EnemyContactDamage.cs do lado dele) reflete o dano nele mesmo. TakeDamage() não
    // repassa quem atacou, então detecta o contato físico direto AQUI, no Ranger (mesmo
    // padrão de trigger que EnemyContactDamage.cs já usa do lado do monstro). Só modo
    // CLÁSSICO (categoria "armadilha do disfarce" — no modo Veterano o Ranger não é mais um
    // objeto parado pra "tocar sem saber"). Dedup por monstro por janela de disfarce
    // (armadilhaCamufladaHit, limpo em AnimationCamouflageHiddenEvent) — nunca pune o mesmo
    // monstro 2x na mesma vez que ele fica "grudado" no Ranger.
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!armadilhaCamufladaEnabled || !isCamouflaged || veteranoDeGuerraEnabled) return;
        if (!other.CompareTag("Enemy")) return;
        var enemy = other.GetComponent<EnemyController>();
        if (enemy == null || !armadilhaCamufladaHit.Add(enemy)) return;

        // Reflete o equivalente a 1 golpe primário completo — GDD não dá o valor exato
        // ("reflete dano nele mesmo", sem número), escolhido pra doer de verdade sem ser um
        // insta-kill, consistente com o resto do kit.
        float reflectDamage = stats.damage * attackDamageMultiplier * (1f + primaryDamagePercent / 100f) * HeroController.CentralDamageMultiplier;
        enemy.TakeDamage(reflectDamage, isDirectPlayerHit: false);
    }
}
