using UnityEngine;

public abstract class HeroController : MonoBehaviour, IDamageable
{
    // Mesmo problema/mesma solução de EnemyController.CollectDistinct, só que pro lado do
    // herói: desde que o herói ganhou um 2º Collider2D (ProjectileHitbox, trigger, além do
    // CapsuleCollider2D físico — achado real, 2026-10-25: Physics2D.OverlapCircle sem filtro
    // agora acha o MESMO herói 2x, um por collider, e todo script que fazia
    // "for each resultado tagged Player -> TakeDamage" dentro do loop estava dando dano
    // duplo). Só existe 1 Player no jogo, então não precisa de lista — devolve o primeiro
    // HeroController achado (ou null) e quem chamou já sabe que é seguro aplicar dano/efeito
    // só 1 vez. Todo ataque em área de monstro/boss deve passar os resultados crus por aqui
    // ANTES de aplicar dano, em vez de reimplementar esse dedup a cada script novo.
    public static HeroController CollectDistinctPlayer(Collider2D[] results, int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (!results[i].CompareTag("Player")) continue;
            var hero = results[i].GetComponent<HeroController>();
            if (hero != null) return hero;
        }
        return null;
    }

    // Só existe 1 Player no jogo (mesmo critério de CollectDistinctPlayer acima) — Instance
    // existe só pra sistemas estáticos (ChestRewardSystem, VendorCart) acharem o herói atual
    // sem precisar de FindObjectOfType nem de referência arrastada manualmente.
    public static HeroController Instance { get; private set; }

    // World Perks (GDD Deadline 8.1, Seção 8) — todos escritos de uma vez só por
    // WorldPerkEffects.Recompute(), nunca lidos de PerkState diretamente daqui (mesmo critério
    // de LootMultiplier/PickupRadius já existentes). Default sempre neutro (1/0/false) — sem
    // Perk nenhum comprado, nada muda pra nenhum herói.
    public static float PickupRadiusMultiplier = 1f;
    public static float MoveSpeedMultiplier = 1f;
    public static float MaxHealthMultiplier = 1f;
    public static float DamageReductionPercent = 0f;
    public static float HpRegenPercentPerSecond = 0f;
    public static bool SegundaChanceUnlocked = false;

    // Perk Central "Despertar do Poder" (5g, rank único) — escrito por WorldPerkEffects.Recompute()
    // junto dos outros multiplicadores estáticos, mesmo critério. Multiplica POR CIMA de
    // MaxHealthMultiplier/PrimaryDamageMultiplier/UltimateDamageMultiplier, nunca substitui (os
    // efeitos se acumulam) — composto em EffectiveMaxHealth aqui, e em cada fórmula de dano de
    // cada herói (ver Barbarian.AnimationAttackHitEvent/AnimationUltimateLandEvent).
    public static float CentralDamageMultiplier = 1f;
    public static float CentralHealthMultiplier = 1f;

    // Infra pros 90 perks bespoke (Esquerda/Direita/Cima) do Barbarian (e futuros heróis) —
    // construída agora, com default neutro, SEM nenhum perk escrevendo nela ainda (isso é
    // conteúdo, próxima fase). Chance de crítico é por ÁRVORE (Esquerda ≠ Cima), nunca um campo
    // só — senão um perk de uma árvore vazaria pra outra (mesma regra de
    // Primary/Ultimate/ShiftDamageMultiplier acima).
    public static float PrimaryCritChance = 0f;
    public static float UltimateCritChance = 0f;

    // Dano x2 em crítico (GDD: "dano x2"), nunca outro valor — centralizado aqui pra não
    // duplicar o literal em cada herói que rolar crítico.
    public static float RollCritMultiplier(float critChance) => Random.value < critChance ? 2f : 1f;

    public HeroStats stats = new HeroStats();
    protected PlayerControls controls;
    protected Animator animator; // pode não existir em prefabs placeholder — sempre checar com "!= null", nunca "?." (mesma regra do EnemyController)
    private SpriteRenderer spriteRenderer;
    private Color spriteOriginalColor;
    private float damageFlashTimer;
    private const float DamageFlashDuration = 0.08f;
    private Vector2 moveInput;
    private bool isDead;

    // "Status_Gold" (GDD Deadline 8.1, Seção 8) — GameObject com Animator em loop já existente
    // em todo prefab de herói, achado por nome (igual ChestPopupUI faz com "Chest_Popup" —
    // busca recursiva em vez de transform.Find direto, pra não depender de profundidade exata
    // na hierarquia). Ativo enquanto goldStatusTimer > 0 (baú deu gold, decisão do usuário:
    // "só quando vier só gold", nunca no caminho popup) OU vendorGoldStatusActive (Vendor:
    // ativo o tempo inteiro que os itens ainda estão voando pro carrinho — ver VendorCart).
    private GameObject statusGoldIndicator;
    private float goldStatusTimer;
    private bool vendorGoldStatusActive;

    // Segunda Chance (World Perk, Seção 8, item 30) — 1x por dia, reseta em OnDayStart (mesmo
    // gatilho que WorldBossSpawnManager/WorldChestSpawnManager já usam pra "virou dia novo").
    private bool segundaChanceUsedToday;

    // Escudo genérico (Direita #24 do Barbarian, "Último Fôlego" — reaproveitável por qualquer
    // herói futuro com perk parecido) — absorve dano ANTES do HP, até esvaziar. AddShield() usa
    // Mathf.Max (não soma) — mesmo critério de ShowGoldStatusFor: reativar o mesmo perk não
    // deve "empilhar" escudo, só garantir o valor configurado.
    private float shieldAmount;
    public float ShieldAmount => shieldAmount;
    public void AddShield(float amount) => shieldAmount = Mathf.Max(shieldAmount, amount);

    // stats.maxHealth é o valor BASE do herói (bespoke por herói, nunca mutado aqui) — o bônus
    // de World Perk (Vigor/Fortitude/Pulso Vital/Vitalidade Suprema) é um multiplicador lido
    // em cima, igual PickupRadiusMultiplier/MoveSpeedMultiplier. Todo clamp/UI de vida usa ISTO,
    // nunca stats.maxHealth direto (ver TakeDamage/Heal/Start).
    public float EffectiveMaxHealth => stats.maxHealth * MaxHealthMultiplier * CentralHealthMultiplier
        * (1f + GetConditionalMaxHealthBonusPercent() / 100f);

    // Hook pra bônus de HP máximo CONDICIONAL (ex.: Direita #27 do Barbarian, "Vontade
    // Inquebrável" — HP máximo temporário enquanto o Shift estiver ativo). Default 0 — sem
    // efeito em nenhum herói que não sobrescrever isso. Cada herói decide a própria condição
    // (isUsingSecondaryAbility, etc.); aqui só o ponto de leitura central.
    protected virtual float GetConditionalMaxHealthBonusPercent() => 0f;

    // Hook pra redução de dano CONDICIONAL (ex.: Direita #21 do Barbarian, "Fúria Protetora" —
    // dano recebido -% enquanto o Shift estiver ativo). Default 0 — nunca interfere com
    // DamageReductionPercent (World Perks, incondicional), os dois se acumulam.
    protected virtual float GetConditionalDamageReductionPercent() => 0f;
    private float EffectiveMoveSpeed => stats.moveSpeed * MoveSpeedMultiplier;

    // Efeitos Nocivos de dano-ao-longo-do-tempo (GDD Seção 33) — componente compartilhado
    // com EnemyController (ver StatusEffectController.cs). Trapped continua à parte, é
    // incapacitação via SetTrapped(), não dano ao longo do tempo.
    private StatusEffectController statusEffectController;

    // Cooldown do ataque primário, compartilhado por todo herói — GDD: attackSpeed é
    // "ataques por segundo", então o cooldown em si é o inverso (attackSpeed=2 -> ataca a
    // cada 0.5s). Sem isso, segurar o botão vira uma metralhadora: nenhum Melee sobrevive
    // tempo suficiente pra chegar perto, principalmente com knockback aplicado a cada hit.
    private AttackCooldown attackCooldown;
    private bool attackHeld;

    // Rede de segurança — se o Animation Event de fim do "die" nunca disparar (clipe sem o
    // evento configurado), força o respawn depois desse tempo em vez de travar o herói pra
    // sempre "morto" (mesmo padrão do EnemyController — maxDieDuration/dieElapsed).
    public float maxDieDuration = 3f;
    private float dieElapsed;
    private bool dieHandled; // evita chamar AnimationDieEndEvent() duas vezes (evento + timeout de segurança)

    // Heróis com ataque real via Animation Event (ex.: Barbarian) marcam isso true/false ao
    // redor da própria janela de ataque — receber dano != interromper uma ação (mesma regra
    // do Bestiário, Seção 22/12): comprometido com o golpe, o dano vira só um flash leve em
    // vez de trocar de estado no Animator (não existe transição Attack -> Damage).
    protected bool isAttacking;

    // Rework 2026-10-30 (Seção 53 do GDD, pendência "Início do cooldown de habilidade") —
    // dedicado só ao Ataque Primário, nunca reaproveitado por Ultimate/Shift (ao contrário de
    // isAttacking, que é um "ocupado" compartilhado por qualquer ação do herói). Fica true do
    // clique até o fim de verdade da animação de ataque primário (o hero decide onde, chamando
    // isAttackResolving = false no próprio Animation Event de fim — ver cada HeroXxx.cs), e só
    // nesse instante o cooldown real começa a contar (ver Update()), mesmo critério que o
    // isUsingSecondaryAbility já usava pro Shift. Herói com fase "during" que libera isAttacking
    // no meio (ex.: Rogue) mantém isAttackResolving true até o fim de VERDADE, não até o meio.
    protected bool isAttackResolving;
    private bool wasAttackResolving; // detecta a transição true -> false no Update(), ver comentário lá

    // Movimento só é permitido durante "Walk" (Seção 16) — sem isso, tomar dano de várias
    // fontes seguidas prende o herói na animação de "Damage" indefinidamente (cada hit
    // reativa o Trigger antes do anterior terminar), impedindo ele de sair da posição e
    // causando mais dano ainda. Mesma solução do Attack dos monstros: cada hit recebido
    // enquanto já está reagindo acelera a própria animação, até um teto onde ela nem chega
    // a tocar — devolve o controle pro jogador na hora.
    private bool isReactingToDamage;
    private float damageSpeedMultiplier = 1f;
    [SerializeField] private float damageSpeedStepPerHit = 0.5f; // 🔢 ajustável
    [SerializeField] private float maxDamageSpeedMultiplier = 3f; // 🔢 ajustável — acima disso, pula a animação de dano

    // Timer, não checagem de estado do Animator — SetTrigger() arma na hora, mas o Animator
    // só processa a transição na própria passada de update dele, depois do Update() dos
    // scripts. Se o hit chega via física (antes do Update() do próprio frame), checar
    // IsInState(animator, "Damage") aqui ainda vê o estado antigo e reseta cedo demais,
    // fazendo o próximo hit reiniciar a reação em vez de acelerar (nunca acumula, nunca
    // atinge o teto, e o Trigger antigo fica armado até disparar sozinho na saída seguinte).
    [SerializeField] private float damageReactionDuration = 0.5f; // 🔢 duração normal (1x) do clipe de dano
    private float damageReactionElapsed;

    // Upgrade futuro — acelera a PLAYBACK de Attack/Ultimate (e, em alguns heróis, Shift que
    // causa dano/cura — ex.: shield bash do Paladin, cura do Cleric, buff do Barbarian; NÃO
    // Shift de sobrevivência/mobilidade como Cambalhota do Rogue, teleporte do Mage,
    // camuflagem do Ranger, Coruja do Druid). Parâmetro do Animator ("ActionSpeedMultiplier"),
    // ligado como Speed Parameter só nos estados certos no Animator Controller de cada herói
    // — não afeta Walk/Idle/Damage/Die, mesmo critério do DamageSpeedMultiplier acima. 1 =
    // velocidade normal.
    [SerializeField] private float actionSpeedMultiplier = 1f; // 🔢 upgradable no futuro

    // Energia é recurso escasso de propósito — sem essa janela, matar vários monstros com
    // a própria ultimate (comum, já que ela costuma limpar a área) já reabasteceria a
    // próxima ultimate sozinha. 🔢 2s é chute inicial, ajustável em teste. Cobre só o
    // impacto inicial (explosão/projétil) — farmar energia com a área PERSISTENTE que a
    // própria ultimate deixa no chão depois (Mage/Ranger, Seção 13) é intencional, não
    // precisa de proteção nenhuma (decisão explícita do usuário, Sprint 19).
    [SerializeField] private float ultimateEnergyLockoutDuration = 2f;
    private float ultimateEnergyLockoutRemaining;

    // Prisão (GDD Seção 33, Efeitos Nocivos — ex.: Freeze) — incapacitação total: sem
    // movimento, sem atacar, sem ultimate, até o efeito acabar. Hook público pro futuro
    // sistema de Efeitos chamar (ainda não existe em código, só a estrutura de herói já
    // fica pronta). Bool, não Trigger — vários efeitos de Prisão podem se sobrepor
    // (Seção 33), quem some por último é quem chama SetTrapped(false) de verdade.
    protected bool isTrapped;

    // Habilidade Secundária (Shift, GDD Seção 16) — terceira ação de todo herói, cooldown
    // próprio mais alto que o do primário. Só habilidades que transformam/incapacitam o
    // herói são canceláveis (Shift de novo no meio dela) — CancelSecondaryAbility() default
    // não faz nada, então heróis sem cancelamento (a maioria) simplesmente ignoram o clique
    // repetido, sem precisar checar nada a mais.
    [SerializeField] private float secondaryAbilityCooldownDuration = 8f; // 🔢 maior que o do primário, ajustável por herói
    private AttackCooldown secondaryAbilityCooldown;
    protected bool isUsingSecondaryAbility;

    // Exposição controlada dos 2 AttackCooldown privados pra perks bespoke de cooldown (Esquerda
    // #21-22/Direita #13-16/#23/#26/#29 do Barbarian) — nenhum herói precisa saber que existe um
    // AttackCooldown por baixo, só que pode ajustar/reduzir/zerar o próprio cooldown.
    protected void SetPrimaryCooldownMultiplier(float multiplier) => attackCooldown.DurationMultiplier = multiplier;
    protected void SetSecondaryCooldownMultiplier(float multiplier) => secondaryAbilityCooldown.DurationMultiplier = multiplier;
    protected void ReduceSecondaryCooldownRemaining(float amount) => secondaryAbilityCooldown.ReduceTimeRemaining(amount);
    protected void ResetSecondaryCooldownRemaining() => secondaryAbilityCooldown.ResetTimeRemaining();
    private bool wasUsingSecondaryAbility; // detecta a transição true -> false no Update(), ver comentário lá

    // Teia da Spider Queen (Efeito Ice) — detecta a transição pra chamar SetTrapped() só nas
    // bordas, ver Update().
    private bool wasIceActive;

    // Flag simples (não por-instância — só existe 1 herói jogável por vez) pra habilidades
    // tipo camuflagem/stealth: enquanto true, EnemyController trata o player como
    // inexistente (GDD Seção 16/17 — Ranger, e futuramente Druid/Assassin, reaproveitam).
    public static bool IsPlayerUntargetable;

    // GDD Deadline 8.1, Seção 4 — "target é global e independente de hit": qualquer gatilho
    // (proximidade, hit, retry já nascendo agressivo) liga isto, e todo o resto do mundo passa
    // a perseguir ao mesmo tempo (ver EnemyController.UpdatePatrol). Resetado em
    // DayTimer.ResetForNewDay() (todo dia novo, retry incluso, começa sem target global) — um
    // RETRY específico volta a ligar quase na hora porque WorldPopulationManager.SpawnOne()
    // força ForceImmediateAggro() no spawn durante retry (ver lá), não porque este flag nunca
    // resetou. Setter customizado: a transição false->true é o próprio "1º gatilho de target
    // do dia" que a Seção 4 pede pra destravar o relógio (DayTimer.StartDayClock()).
    private static bool globalAggroActive;
    public static bool GlobalAggroActive
    {
        get => globalAggroActive;
        set
        {
            if (value && !globalAggroActive) DayTimer.Instance?.StartDayClock();
            globalAggroActive = value;
        }
    }

    // Achado real do usuário (GDD Deadline 8.1, Seção 3): DayResolver.ResolvePlayerDeath()
    // limpa monstro/projétil/loot NA HORA da morte (não espera a animação), mas o GameState só
    // muda pra Results/Shop no FIM da animação (AnimationDieEndEvent) — nesse meio-tempo
    // GameplayGate.IsActive continua true (CurrentState ainda é Gameplay), e
    // WorldPopulationManager/WorldBossSpawnManager continuavam rodando normalmente, vendo a
    // população zerada e reabastecendo tudo de novo ainda durante a animação (os monstros
    // novos ficavam vivos até dentro da Loja). Não deu pra resolver só com GameplayGate — ele
    // também gate(ia) o próprio Update() do HeroController, incluindo o timeout de segurança
    // do "die" (ver linha ~372), que PRECISA continuar rodando enquanto isDead. Por isso é um
    // flag separado, checado só pelos managers de população, não pelo GameplayGate em si.
    public static bool IsDying;

    // 2026-11-01 — Deadline 8.1, IA de perseguição inteligente (planejamento prévio no chat).
    // Velocidade real do herói, pro monstro prever ONDE ele vai estar em vez de só perseguir
    // onde ele ESTÁ agora (EnemyController.GetSmartApproachPoint). Static pelo mesmo motivo de
    // IsPlayerUntargetable/PickupRadius (só existe 1 herói jogável por vez). Atualizado no
    // mesmo lugar onde rb.linearVelocity é escrito de verdade (ver Update()).
    public static Vector2 Velocity;

    // GameObject "//HEROS" na cena — mesmo container onde o próprio herói nasce
    // (GameplayBootstrap.SpawnHero). Pets (Mage/Blood Mage) nascem aqui também: nascem no
    // mesmo lugar do "pai", mesmo padrão find-or-create do EnemyController.EnemysContainer.
    private static Transform herosContainer;
    public static Transform HerosContainer
    {
        get
        {
            if (herosContainer == null)
            {
                var existing = GameObject.Find("//HEROS");
                herosContainer = existing != null ? existing.transform : new GameObject("//HEROS").transform;
            }
            return herosContainer;
        }
    }

    // Sprint 28 (Gunslinger) — passiva de sorte/loot (GDD Seção 17.8): multiplica a
    // quantidade de Monster Essence dropada por qualquer abate, não só os do próprio
    // Gunslinger. Static pelo mesmo motivo de IsPlayerUntargetable (só existe 1 herói
    // jogável por vez); lido em EnemyController.AnimationDieEndEvent(). Default 1 = sem
    // efeito em nenhum herói existente.
    public static float LootMultiplier = 1f;

    // Sprint 32 — GDD Seção 37 (Pickup Radius). Static pelo mesmo motivo de LootMultiplier (só
    // existe 1 herói jogável por vez). Upgrade "Increase Pickup Radius" (Bonuses, Seção 41,
    // ainda não implementado) só precisa mudar este valor — o collider físico que de fato
    // detecta o loot (pickupRadius, abaixo — HeroPickupRadius, num GameObject filho) lê ele
    // todo frame.
    public static float PickupRadius = 1f;

    // 2026-10-31 — Deadline 8.1 (redesign incremental): nerf de propósito na Energia de
    // Ultimate recebida por kill, pra abrir espaço pra um perk de Baixo ("mundo") ou de Cima
    // (ultimate específica) que devolva esse valor aos poucos. Static pelo mesmo motivo de
    // LootMultiplier/PickupRadius (só existe 1 herói jogável por vez). Multiplica POR CIMA de
    // UltimateEnergyMultiplier (passiva por-herói, ex.: 4× do Rogue) em HandleEnemyKilled —
    // os dois efeitos se acumulam, não se substituem. Default 0.1 = herói começa recebendo só
    // 10% da Energia que o monstro largaria num mundo sem este nerf (hoje 20 por kill — ver
    // EnemyController.energyReward — vira 2 de verdade sem nenhum perk comprado).
    public static float EnergyGainMultiplier = 0.1f;

    // GDD Deadline 8.1, Seção 7.3 (R9) — Perks de Esquerda/Cima/Direita precisam ser 100%
    // independentes entre si. Antes deste rework, Ultimate/Shift calculavam dano como
    // multiplicador de stats.damage — o MESMO campo que o Ataque Primário usa como base — então
    // um perk de Esquerda que bufasse dano através dele vazaria de graça pra Ultimate/Shift.
    // Cada herói agora multiplica o dano de CADA habilidade por UM destes 3, nunca por
    // stats.damage sozinho nem pelos outros 2 — assim um perk de uma árvore só pode escrever no
    // multiplicador da própria árvore. Static pelo mesmo motivo de LootMultiplier/PickupRadius/
    // EnergyGainMultiplier (só existe 1 herói jogável por vez). Default 1 = sem perk comprado
    // ainda (nenhum perk escreve nestes 3 ainda — isso é conteúdo, R11+; aqui só o rework de
    // arquitetura, pra não vazar quando os perks de verdade começarem a escrever neles).
    public static float PrimaryDamageMultiplier = 1f;
    public static float UltimateDamageMultiplier = 1f;
    public static float ShiftDamageMultiplier = 1f;

    // Exposto no Inspector só pra ajustar o valor BASE acima sem precisar editar código — campo
    // comum de instância (não static), igual qualquer outro 🔢 pendente de balanceamento
    // (ex.: energyReward do EnemyController). Copiado pro PickupRadius static em Awake(); como
    // só 1 herói existe por vez, mudar isso num prefab específico é o jeito de testar valores.
    [SerializeField] private float basePickupRadius = 1f;

    // GDD Seção 11: "Mira: posição do mouse, resolvida em 8 direções (N, S, L, O, NE, NO, SE, SO)."
    protected Vector2 AimDirection { get; private set; } = Vector2.down;

    // Direção crua do mouse, sem o snap de 8 direções — Animator/animação continuam usando
    // AimDirection (só existem 8 poses), mas alguns heróis podem preferir a direção exata
    // pra mecânicas próprias (ex.: trajetória das flechas do Ranger). Congela junto com
    // AimDirection durante isAttacking/isTrapped, mesmo motivo.
    protected Vector2 RawAimDirection { get; private set; } = Vector2.down;

    // Distância crua (não normalizada) até o mouse no instante da mira — o teleporte do Mage
    // (GDD Seção 16/17.3) precisa saber A DISTÂNCIA, não só a direção, pra respeitar um
    // alcance máximo. Congela junto com RawAimDirection pelo mesmo motivo (isAttacking/
    // isTrapped), de graça, por reaproveitar o mesmo "if" de UpdateAimDirection.
    protected float RawAimDistance { get; private set; }

    // Mesma mira, só que travada nas 4 diagonais (nunca cardeal pura) — alimenta
    // DiagonalAimX/Y, usado pelos Blend Trees que só têm pose desenhada pras diagonais
    // (Idle/Walk/Damage/SummonPet). AimDirection sozinho pode devolver um cardeal puro
    // (N/E/S/W), o que nesses Blend Trees cai numa zona onde as 2 diagonais vizinhas ficam
    // exatamente equidistantes — ver DirectionUtility.SnapTo4Diagonals.
    protected Vector2 DiagonalAimDirection { get; private set; } = new Vector2(-0.70710678f, -0.70710678f);

    // Sprint 21 (Druid) — exposição read-only do vetor de movimento cru, mesmo padrão de
    // AimDirection. GDD Seção 16 diz "não existe MoveX/MoveY pro herói" de propósito, mas a
    // Coruja do Druid anda olhando pras 4 direções cardeais de pra-onde-o-jogador-está-andando,
    // não pra mira do mouse — só esse caso quebra a regra geral.
    protected Vector2 MoveInput => moveInput;

    // Collider físico (CapsuleCollider2D) — offset.x precisa espelhar conforme a direção da
    // mira, mesmo motivo do FlippedColliderOffsetX.cs do Bestiário, só que o sinal usado lá
    // (SpriteRenderer.flipX) nunca muda em herói NENHUM: a arte de cada herói é sempre um
    // clipe dedicado por diagonal (idle_ne/nw/se/sw etc.), nunca um clipe espelhado via
    // flipX. O sinal real de "olhando pra E ou pra W" é DiagonalAimDirection.x (o mesmo que
    // já alimenta o Blend Tree). offset.x já configurado no prefab é assumido tunado pra E
    // (DiagonalAimDirection.x >= 0) — nunca precisa de 2 campos, só espelha (× -1) pra W.
    private CapsuleCollider2D bodyCollider;
    private float bodyColliderOffsetXFacingEast;

    // Vive num GameObject FILHO dedicado (ver HeroPickupRadius) — NUNCA no mesmo GameObject do
    // corpo. Motivo real (achado em playtest): Unity dispara OnTriggerStay2D quando QUALQUER
    // collider do GameObject é trigger, mesmo o sólido do corpo — compartilhar o objeto faria o
    // corpo (bem maior) coletar loot sozinho, ignorando o raio configurado.
    private HeroPickupRadius pickupRadius;

    // [Sprint P3] Movimento deliberado passou de transform.Translate pra rb.linearVelocity —
    // Translate ignora Collider2D sólido (o herói atravessava props sólidos, ex.: pedra/
    // árvore, achado do usuário criando os prefabs de props); rb.linearVelocity é resolvido
    // pelo motor de física de verdade, então colisão sólida passa a bloquear o herói de
    // graça. O Rigidbody2D já existe no prefab (Dynamic, Interpolate ligado pro fix de
    // jitter do Pixel Perfect/Cinemachine) só pra absorver o empurrão do RVO dos monstros
    // (Linear Damping) — reaproveitado aqui, não é um componente novo. Fallback pra Translate
    // só existe pra não quebrar um prefab placeholder antigo sem Rigidbody2D configurado.
    // Protected (não private) — Rogue.UpdateRoll() (Cambalhota) reaproveita o mesmo campo,
    // mesmo motivo (atravessava props sólidos rolando por cima).
    protected Rigidbody2D rb;

    // Druid sobrescreve pra false — ele já cuida do próprio collider físico (tem 2 formas,
    // humana e Alce, cada uma com offset/size próprios) e reaplica esse mesmo espelhamento
    // por conta própria em cima disso. Deixar os dois mecanismos ativos ao mesmo tempo faria
    // o daqui sobrescrever com um offset "humano" errado por cima do que o Druid acabou de
    // aplicar pra forma Alce.
    protected virtual bool AutoFlipBodyCollider => true;

    protected virtual void Awake()
    {
        Instance = this;

        foreach (var t in GetComponentsInChildren<Transform>(true))
        {
            if (t.name != "Status_Gold") continue;
            statusGoldIndicator = t.gameObject;
            break;
        }
        if (statusGoldIndicator != null) statusGoldIndicator.SetActive(false);

        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) spriteOriginalColor = spriteRenderer.color;
        bodyCollider = GetComponent<CapsuleCollider2D>();
        if (bodyCollider != null) bodyColliderOffsetXFacingEast = bodyCollider.offset.x;
        rb = GetComponent<Rigidbody2D>();

        PickupRadius = basePickupRadius;
        var pickupRadiusObj = new GameObject("PickupRadius");
        pickupRadiusObj.transform.SetParent(transform, false);
        pickupRadius = pickupRadiusObj.AddComponent<HeroPickupRadius>();
        pickupRadius.SetRadius(PickupRadius);

        attackCooldown = new AttackCooldown(1f / stats.attackSpeed);
        secondaryAbilityCooldown = new AttackCooldown(secondaryAbilityCooldownDuration);
        statusEffectController = GetComponent<StatusEffectController>(); // pode não existir em prefabs placeholder
        controls = new PlayerControls();
        controls.Gameplay.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        controls.Gameplay.Move.canceled += ctx => moveInput = Vector2.zero;
        // Não usa .performed aqui de propósito — esse evento só dispara quando o mouse se
        // MOVE, e enquanto isAttacking/isTrapped a mira é ignorada (não fica em fila). Se o
        // jogador mexe o mouse durante uma ação longa (ex.: o teleporte do Mage) e para de
        // mexer antes dela acabar, nenhum evento novo chega pra "acordar" a mira depois —
        // ela ficava presa na direção antiga até o próximo movimento. Lendo a posição atual
        // todo frame no Update() (ver UpdateAimDirection ali embaixo) resolve isso de vez:
        // a mira sempre reflete onde o mouse ESTÁ, não só o último evento de movimento.
        // Segurar o botão continua atacando sozinho, respeitando o cooldown — mesmo padrão
        // do movimento (bool guardado aqui, lido/consumido todo frame no Update()). O clique
        // único vira só o caso onde attackHeld fica true por 1 frame só.
        controls.Gameplay.Attack.performed += ctx => attackHeld = true;
        controls.Gameplay.Attack.canceled += ctx => attackHeld = false;
        controls.Gameplay.Ultimate.performed += ctx => { if (GameplayGate.IsActive && !isDead && !isTrapped) TryUseUltimate(); };
        // Shift: se já está usando, tenta cancelar (só faz efeito em habilidade cancelável —
        // CancelSecondaryAbility() default não faz nada); senão, tenta usar (respeitando
        // cooldown próprio, maior que o do primário).
        controls.Gameplay.SecondaryAbility.performed += ctx =>
        {
            if (!GameplayGate.IsActive || isDead || isTrapped) return;
            if (isUsingSecondaryAbility) CancelSecondaryAbility();
            else TryUseSecondaryAbility();
        };

        RefreshActionSpeedMultiplier();
    }

    // Trocar o runtimeAnimatorController inteiro (ex.: Druid virando Alce) reseta os
    // parâmetros pros defaults do controller NOVO — reaplica o valor de upgrade sempre que
    // isso acontecer (cada herói que troca de controller em runtime chama isto de novo logo
    // depois da troca).
    protected void RefreshActionSpeedMultiplier()
    {
        if (animator != null) animator.SetFloat("ActionSpeedMultiplier", actionSpeedMultiplier);
    }

    // Exposto só pra heróis com Animator FILHO independente do próprio (ex.: MageAttackHitbox)
    // repassarem o mesmo multiplicador — SetFloat não propaga entre Animators diferentes.
    protected float ActionSpeedMultiplier => actionSpeedMultiplier;

    // Pros perks de velocidade de animação (ex.: Esquerda #18-20 do Barbarian) recomputarem o
    // valor sem precisar duplicar o SetFloat do Animator aqui — RefreshActionSpeedMultiplier()
    // já faz isso.
    protected void SetActionSpeedMultiplier(float multiplier)
    {
        actionSpeedMultiplier = multiplier;
        RefreshActionSpeedMultiplier();
    }

    protected virtual void Start()
    {
        // Achado real do usuário: perk de "+%HP máximo" só mexia no TETO (EffectiveMaxHealth) —
        // stats.health (vida ATUAL) é o valor fixo serializado no prefab, nunca sincronizado
        // com o novo máximo, então o jogador nascia com a vida de sempre e só "via" o bônus se
        // regenerasse/curasse até lá. Start() roda depois de TODO Awake() (inclusive
        // RecomputePerkEffects() do herói específico, chamado no fim do Awake() de cada
        // subclasse), então aqui EffectiveMaxHealth já reflete World + Central + bônus próprios
        // do herói — nasce sempre com a vida ATUAL = vida MÁXIMA efetiva daquele momento. Só
        // acontece 1x aqui (spawn); comprar perk DEPOIS de nascido não deve curar de graça — só
        // aumenta o teto, como já fazia.
        stats.health = EffectiveMaxHealth;

        GameEvents.HealthChanged(stats.health, EffectiveMaxHealth);
        GameEvents.EnergyChanged(stats.energy, stats.maxEnergy);
    }

    protected virtual void OnEnable()
    {
        controls.Enable();
        GameEvents.OnEnemyKilled += HandleEnemyKilled;
        GameEvents.OnDayStart += HandleDayStartForSegundaChance;
    }

    protected virtual void OnDisable()
    {
        controls.Disable();
        GameEvents.OnEnemyKilled -= HandleEnemyKilled;
        GameEvents.OnDayStart -= HandleDayStartForSegundaChance;
    }

    private void HandleDayStartForSegundaChance() => segundaChanceUsedToday = false;

    protected virtual void Update()
    {
        // Reflete qualquer upgrade de Pickup Radius no collider de verdade, todo frame — mais
        // simples que um setter dedicado, já que PickupRadius é static (qualquer sistema pode
        // mudar o valor direto, mesmo critério do LootMultiplier). PickupRadiusMultiplier
        // (World Perks) multiplica POR CIMA do próprio basePickupRadius do herói, nunca
        // substitui — cada herói mantém seu valor bespoke.
        PickupRadius = basePickupRadius * PickupRadiusMultiplier;
        if (pickupRadius != null) pickupRadius.SetRadius(PickupRadius);

        if (!GameplayGate.IsActive) return;

        // Detecta o instante em que a Habilidade Secundária termina de verdade (qualquer
        // herói, qualquer motivo — timeout ou cancelamento manual, os dois convergem pro
        // mesmo "isUsingSecondaryAbility = false" no Animation Event de fim de cada um) e só
        // AÍ arma o cooldown. Cooldown é "tempo de uso", não "tempo desde o clique" — sem
        // isso, o cooldown corria por baixo dos panos enquanto o jogador ainda estava
        // transformado/escondido, deixando o tempo de espera real mais curto que o
        // configurado. Checado antes do "if (isDead)" de propósito: morrer no meio da
        // habilidade também zera isUsingSecondaryAbility (OnDeath), e não queremos perder essa
        // transição só porque o resto do Update() retorna cedo nesse frame.
        if (wasUsingSecondaryAbility && !isUsingSecondaryAbility) secondaryAbilityCooldown.Start();
        wasUsingSecondaryAbility = isUsingSecondaryAbility;

        if (wasAttackResolving && !isAttackResolving) attackCooldown.Start();
        wasAttackResolving = isAttackResolving;

        UpdateDamageFlash();
        UpdateGoldStatus();
        if (!isDead) UpdateHpRegen();
        UpdateAimDirection(controls.Gameplay.Look.ReadValue<Vector2>());
        if (AutoFlipBodyCollider) ApplyBodyColliderFlip();

        if (isDead)
        {
            // O herói continua existindo (de propósito) enquanto o clipe "die" toca — a
            // transição de volta pro andar inicial só acontece via AnimationDieEndEvent
            // (Animation Event), com o mesmo timeout de segurança do EnemyController caso
            // o evento não esteja configurado.
            dieElapsed += Time.deltaTime;
            if (dieElapsed >= maxDieDuration)
            {
                Debug.LogWarning($"[{GetType().Name}] AnimationDieEndEvent nunca chegou — forçando respawn (verifique o Animator Controller).");
                AnimationDieEndEvent();
            }
            return;
        }

        if (statusEffectController != null)
        {
            statusEffectController.Tick(Time.deltaTime);

            // Teia da Spider Queen (GDD Bestiário) — "gruda no jogador e o prende". O Efeito
            // Ice em si não causa dano (ApplyStatusEffect chamado com damagePerSecond: 0, só
            // pra mostrar o ícone — ainda sem arte própria, decisão do usuário: clonar o
            // visual de outro Efeito por enquanto), a incapacitação de verdade é o
            // isTrapped/SetTrapped() já existente (o comentário de SetTrapped já antecipava
            // isso: "ex.: Freeze"). Detecta a transição (mesmo padrão de wasPlayerUntargetable
            // em EnemyController) pra chamar SetTrapped() só 1x em cada borda, nunca todo
            // frame — StatusEffectController.Tick() já remove o Efeito sozinho quando a
            // duração acaba, então IsEffectActive virar false destrava automaticamente, sem
            // precisar de um timer próprio aqui.
            bool iceActive = statusEffectController.IsEffectActive(StatusEffectType.Ice);
            if (iceActive != wasIceActive) SetTrapped(iceActive);
            wasIceActive = iceActive;
        }

        // Terminou sozinho (sem ter atingido o teto de aceleração) -- reseta pro próximo
        // hit começar do zero, não escalado. Escala pelo próprio multiplicador: acelerado
        // 2x, a janela "reagindo" também encolhe pela metade, pra bater com o que a
        // animação realmente está mostrando na tela.
        if (isReactingToDamage)
        {
            damageReactionElapsed += Time.deltaTime * damageSpeedMultiplier;
            if (damageReactionElapsed >= damageReactionDuration)
            {
                isReactingToDamage = false;
                damageSpeedMultiplier = 1f;
                if (animator != null) animator.SetFloat("DamageSpeedMultiplier", 1f);
            }
        }

        if (ultimateEnergyLockoutRemaining > 0f) ultimateEnergyLockoutRemaining -= Time.deltaTime;

        secondaryAbilityCooldown.Tick(Time.deltaTime);
        attackCooldown.Tick(Time.deltaTime);
        GameEvents.PrimaryAttackCooldownChanged(attackCooldown.TimeRemaining, attackCooldown.Duration);
        GameEvents.SecondaryAbilityCooldownChanged(secondaryAbilityCooldown.TimeRemaining, secondaryAbilityCooldown.Duration);
        GameEvents.PrimaryAttackActiveChanged(isAttackResolving);
        GameEvents.SecondaryAbilityActiveChanged(isUsingSecondaryAbility);
        // Heróis cuja Ultimate tem duração própria (Alce do Druid, forma sombria do Assassin)
        // já sobrescrevem IsUltimateActive pra cancelamento — reaproveitado aqui pra UI
        // (Ultimate_Active fica aceso o tempo todo que ela estiver em uso, não só quando a
        // energia enche). Sem efeito em heróis sem Ultimate com duração (default false).
        GameEvents.UltimateActiveChanged(IsUltimateActive);
        // !isAttacking aqui é o que impede um attackSpeed alto de disparar um novo ataque
        // por cima de uma animação ainda tocando (ex.: attackSpeed maior que a duração do
        // próprio golpe) — cooldown cuida do ritmo, isAttacking cuida de nunca sobrepor.
        // ShouldConsumeCooldownOnAttack() vem ANTES do TryConsume() de propósito — sem isso,
        // o Cleric (primeiro herói cujo clique pode "não fazer nada" por falta de alvo no
        // raio) gastava o cooldown inteiro num clique que nem chegou a lançar o projétil.
        if (attackHeld) TryPrimaryAttack();

        bool wantsToMove = moveInput.sqrMagnitude > 0.0001f;
        if (animator != null) animator.SetBool("IsMoving", wantsToMove);

        // GDD Seção 16: o herói só se move de verdade enquanto o Animator confirma que já
        // está no estado "Walk" — qualquer outra animação (attack, ultimate, summon etc.)
        // bloqueia o movimento até terminar. Sem Animator ainda wireado (heróis não
        // migrados), AnimatorStateCheck libera sempre, então nada muda pra eles.
        bool canMove = wantsToMove && AnimatorStateCheck.IsInState(animator, "Walk");
        if (rb != null)
        {
            // rb.linearVelocity (não Translate) — resolvido pelo motor de física de verdade,
            // então Collider2D sólido (ex.: props, Sprint P3) bloqueia o herói de verdade em
            // vez de ser atravessado. Precisa zerar explicitamente quando canMove é falso —
            // diferente de Translate (que simplesmente não faz nada), velocity persiste entre
            // frames até ser trocada.
            rb.linearVelocity = canMove ? moveInput * EffectiveMoveSpeed : Vector2.zero;
            Velocity = rb.linearVelocity;
        }
        else if (canMove)
        {
            transform.Translate(moveInput * EffectiveMoveSpeed * Time.deltaTime);
            Velocity = moveInput * EffectiveMoveSpeed;
        }
        else
        {
            Velocity = Vector2.zero;
        }
    }

    private void UpdateAimDirection(Vector2 screenPosition)
    {
        // Trava a mira durante o golpe/ultimate — mesma regra do Bestiário (Seção 22): a
        // direção de um ataque real não muda no meio da própria animação. Preso (Seção 33)
        // pelo mesmo motivo: incapacitado não vira nem olhando — AimX/AimY ficam
        // congelados na direção de quando o Trapped começou.
        if (isAttacking || isTrapped) return;
        if (Camera.main == null) return;

        Vector3 worldPoint = Camera.main.ScreenToWorldPoint(
            new Vector3(screenPosition.x, screenPosition.y, -Camera.main.transform.position.z));
        Vector2 toMouse = (Vector2)worldPoint - (Vector2)transform.position;
        if (toMouse.sqrMagnitude < 0.0001f) return;

        AimDirection = DirectionUtility.SnapTo8Directions(toMouse);
        DiagonalAimDirection = DirectionUtility.SnapTo4Diagonals(toMouse);
        RawAimDirection = toMouse.normalized;
        RawAimDistance = toMouse.magnitude;

        // GDD Seção 16: não existe MoveX/MoveY pro herói (ao contrário dos monstros,
        // Seção 22) — walk/attack/ultimate usam sempre a mira, nunca a direção de
        // movimento. Um único par de parâmetros cobre os 3.
        if (animator != null)
        {
            animator.SetFloat("AimX", AimDirection.x);
            animator.SetFloat("AimY", AimDirection.y);
            // Par separado, só pros Blend Trees de 4 pontos (ver DiagonalAimDirection) —
            // não substitui AimX/AimY, que continuam alimentando os Blend Trees de 8 pontos
            // (Attack/Ultimate/Teleport do Mage, que têm pose real pra cardeal).
            animator.SetFloat("DiagonalAimX", DiagonalAimDirection.x);
            animator.SetFloat("DiagonalAimY", DiagonalAimDirection.y);
        }
    }

    // Espelha offset.x do collider físico conforme DiagonalAimDirection.x — ver comentário
    // do campo bodyColliderOffsetXFacingEast. Roda toda vez que a mira for recalculada
    // (mesmo Update() de UpdateAimDirection), não precisa de LateUpdate separado porque o
    // Collider2D não depende de pose do Animator, só do valor lógico da mira.
    private void ApplyBodyColliderFlip()
    {
        if (bodyCollider == null) return;
        var offset = bodyCollider.offset;
        offset.x = DiagonalAimDirection.x < 0f ? -bodyColliderOffsetXFacingEast : bodyColliderOffsetXFacingEast;
        bodyCollider.offset = offset;
    }

    // Sprint 22 (Rogue) — multiplicador de Energia de Ultimate ganho por kill. Default 1f,
    // sem efeito em nenhum herói existente. Só o Rogue sobrescreve (passiva: "4× mais Energia
    // de Ultimate por kill").
    protected virtual float UltimateEnergyMultiplier => 1f;

    // Cima #15-17 do Barbarian ("Canalização Rápida"/"Fúria Contida"/"Explosão Iminente" —
    // carga necessária -%/rank). Multiplica o THRESHOLD de "pronto pra usar", nunca o
    // stats.maxEnergy real (que também é o teto de EnergySystem.AddEnergy) — default 1 = sem
    // efeito em nenhum herói que não sobrescrever. Valores < 1 deixam a Ultimate pronta ANTES
    // da barra visual chegar a 100% — ajuste de UI fica pra quando o perk for ligado de verdade.
    protected virtual float UltimateChargeRequiredMultiplier => 1f;

    // Chamado por PerkState.TryBuy() (qualquer compra, de qualquer árvore) e pelo próprio herói
    // no Awake() (pra aplicar ranks já salvos/comprados antes dele existir). Default no-op —
    // só heróis com perks bespoke próprios (Esquerda/Direita/Cima) sobrescrevem isso; Perks de
    // Mundo continuam 100% à parte, em WorldPerkEffects.Recompute().
    public virtual void RecomputePerkEffects() { }

    // Rework 2026-10-30 (Druid — Alce) — heróis cuja Ultimate tem uma forma/duração própria
    // podem bloquear o próprio ganho de Energia enquanto ela estiver ativa (senão o jogador
    // sairia da forma já com a próxima Ultimate pronta de graça, só de matar monstro durante
    // ela). Default true, sem efeito em nenhum herói que não sobrescrever isso.
    protected virtual bool CanGainUltimateEnergy => true;

    private void HandleEnemyKilled(int energyValue, bool isBoss)
    {
        // Janela de bloqueio pós-ultimate — sem isso, uma ultimate boa (que geralmente
        // limpa a área ou salva o jogador) já paga a energia da próxima sozinha, virando
        // ultimate infinita. Energia é recurso escasso de propósito.
        if (ultimateEnergyLockoutRemaining > 0f) return;
        if (!CanGainUltimateEnergy) return;

        int adjustedEnergy = Mathf.RoundToInt(energyValue * UltimateEnergyMultiplier * EnergyGainMultiplier);
        stats.energy = EnergySystem.AddEnergy(stats.energy, stats.maxEnergy, adjustedEnergy);
        GameEvents.EnergyChanged(stats.energy, stats.maxEnergy);
    }

    // Extraído do Update() (que só chama isto enquanto attackHeld) pra também poder ser
    // chamado 1x só por um clique de UI (Right_Button_Mouse em Primary_Attack) — mesmos gates,
    // mesmo consumo de cooldown, sem duplicar a lógica.
    public bool TryPrimaryAttack()
    {
        // TryConsume() virou IsReady (só checa, não arma mais o timer na hora do clique) —
        // o timer real só começa em Update(), quando isAttackResolving volta a false de
        // verdade (ver comentário no campo e AttackCooldown.Start()).
        if (!(!isAttacking && !isTrapped && ShouldConsumeCooldownOnAttack() && (IsAttackCooldownBypassed() || attackCooldown.IsReady)))
            return false;

        isAttackResolving = true;
        PrimaryAttack();
        return true;
    }

    public void TryUseUltimate()
    {
        // Sprint 21 (Druid) — só o Alce tem Ultimate com duração até hoje, então só ele
        // sobrescreve IsUltimateActive/CancelUltimate (default false/no-op, sem efeito nos
        // outros heróis). Checa ANTES do gate de energia: a energia já está zerada desde a
        // ativação (ver embaixo), então o gate bloquearia o próprio cancelamento se viesse depois.
        if (IsUltimateActive) { CancelUltimate(); return; }
        // Checa ANTES de gastar energia — sem isso, um herói que recusa a Ultimate por dentro
        // (ex.: Coruja do Druid, isAttacking de qualquer herói) ainda perdia a energia toda,
        // já que UseUltimate() é void e o "return" cedo dele é invisível pra quem chamou.
        if (!CanUseUltimate()) return;
        if (!EnergySystem.IsReady(stats.energy, stats.maxEnergy * UltimateChargeRequiredMultiplier)) return;
        // Releitura forçada antes de congelar — o Input System processa este clique ANTES
        // do Update() deste frame, então sem isso a mira congelaria com o valor do frame
        // ANTERIOR (podendo estar bem perto do herói se o mouse só chegou na posição final
        // no exato frame do clique), não com a posição real do mouse agora.
        UpdateAimDirection(controls.Gameplay.Look.ReadValue<Vector2>());
        UseUltimate();
        stats.energy = 0f;
        GameEvents.EnergyChanged(stats.energy, stats.maxEnergy);
        ultimateEnergyLockoutRemaining = ultimateEnergyLockoutDuration;
    }

    public void TryUseSecondaryAbility()
    {
        // Mesma trava que PrimaryAttack()/UseUltimate() já tinham e a Habilidade Secundária
        // não tinha — sem isso, apertar Shift em cima do fim de outra ação (Attack, Ultimate,
        // SummonPet) ainda inicia a habilidade, só que UpdateAimDirection() logo abaixo
        // também é bloqueada pela mesma isAttacking (é a mesma trava, ver o "if" dela), então
        // a releitura forçada vira no-op silencioso e a habilidade acaba usando a mira
        // CONGELADA da ação anterior (podia estar longe da mira real, ex.: o "micro-teleport"
        // do Mage e a pose errada no teleport_end — causa raiz encontrada em produção).
        if (isAttacking) return;
        // Só checa (IsReady), não consome mais aqui — o timer é armado depois, no Update(),
        // quando a habilidade termina de verdade (ver comentário lá e em AttackCooldown.Start()).
        if (!secondaryAbilityCooldown.IsReady) return;
        UpdateAimDirection(controls.Gameplay.Look.ReadValue<Vector2>());
        isUsingSecondaryAbility = true;
        UseSecondaryAbility();
    }

    // Chamado pelo sistema de Efeitos Nocivos quando uma Prisão (ex.: teia da Spider Queen,
    // via Efeito Ice — ver Update()) começa/termina no herói — GDD Seção 33. Interrompe
    // qualquer ação em andamento na hora (igual o Die já faz hoje) e reseta as flags internas
    // de Attack/Damage, senão o jogador ficaria incapaz de agir pra sempre depois que a
    // Prisão passasse.
    public void SetTrapped(bool trapped)
    {
        isTrapped = trapped;
        if (trapped)
        {
            isAttacking = false;
            isAttackResolving = false;
            isReactingToDamage = false;
            damageSpeedMultiplier = 1f;
            if (animator != null) animator.SetFloat("DamageSpeedMultiplier", 1f);
        }
        if (animator != null) animator.SetBool("IsTrapped", trapped);
    }

    [SerializeField] private float floatingTextHeightAdjust = -0.5f; // 🔢 ajuste fino, negativo baixa o texto

    // Topo do sprite, não o pivot bruto — mesmo critério do EnemyController, pra não
    // precisar configurar um offset por herói. floatingTextHeightAdjust corrige o bounds
    // do sprite (pixel art costuma ter espaço transparente, topo "cru" fica alto demais).
    protected Vector3 GetFloatingTextSpawnPosition() =>
        spriteRenderer != null
            ? new Vector3(transform.position.x, spriteRenderer.bounds.max.y + floatingTextHeightAdjust, transform.position.z)
            : transform.position;

    // Exposto publicamente (IsDamageImmune em si é protected, cada herói sobrescreve) pra
    // quem aplica dano de fora (ex.: EnemyProjectile) saber se o TakeDamage() abaixo vai ser
    // um no-op ANTES de decidir aplicar um Efeito Nocivo (ex.: Fire) junto — sem isso, o
    // Efeito Nocivo "colava" mesmo com o dano bloqueado pela imunidade (Cambalhota do Rogue,
    // Owl do Druid, etc.), já que TakeDamage() não devolvia nenhum sinal de "bloqueei".
    public bool IsCurrentlyDamageImmune => isDead || IsDamageImmune;

    // `isDirectPlayerHit` não é usado aqui (só EnemyController se importa) — existe só pra
    // bater com a assinatura de IDamageable (ver comentário lá).
    public virtual void TakeDamage(float amount, bool isDirectPlayerHit = true)
    {
        // Guarda contra reentrância: sem isso, dois hits antes do respawn terminar
        // disparam OnDeath() duas vezes (penalidade de -30s duplicada, PlayTransition
        // duplicado — o EasyTransition não suporta duas transições concorrentes).
        if (isDead) return;
        if (IsDamageImmune) return;

        // Pele Grossa/Couraça/Casco de Ferro (World Perks) — % de redução, aplicada ANTES do
        // ApplyDamage (nunca depois: reduzir o dano JÁ aplicado deixaria o herói com vida "de
        // volta", o que pareceria cura, não redução). GetConditionalDamageReductionPercent
        // (ex.: Fúria Protetora do Barbarian, só durante o Shift) se acumula com a de Mundo,
        // nunca substitui — por isso os 2 "if" separados, não um só somado antes do clamp.
        if (DamageReductionPercent > 0f) amount *= 1f - Mathf.Clamp01(DamageReductionPercent / 100f);
        float conditionalReduction = GetConditionalDamageReductionPercent();
        if (conditionalReduction > 0f) amount *= 1f - Mathf.Clamp01(conditionalReduction / 100f);

        // Escudo (ex.: Último Fôlego do Barbarian) absorve ANTES do HP — mesma ordem do resto
        // (redução de % primeiro, escudo absorve o que sobrar, só então o HP perde o resto).
        if (shieldAmount > 0f)
        {
            float absorbed = Mathf.Min(shieldAmount, amount);
            shieldAmount -= absorbed;
            amount -= absorbed;
        }

        stats.health = HealthSystem.ApplyDamage(stats.health, amount);

        // Segunda Chance (World Perk, Seção 8, item 30) — 1x por dia, sobrevive com 1 HP a um
        // hit que mataria. Checado AQUI (depois do ApplyDamage, antes do IsDead abaixo) pra
        // nunca interferir com hits não-fatais, só o golpe que realmente zeraria a vida.
        if (SegundaChanceUnlocked && !segundaChanceUsedToday && HealthSystem.IsDead(stats.health))
        {
            segundaChanceUsedToday = true;
            stats.health = 1f;
        }

        GameEvents.HealthChanged(stats.health, EffectiveMaxHealth);
        // isDirectPlayerHit não importa aqui (isPlayer=true) — valor fixo, nunca lido pelo HitFeelController nesse caso.
        GameEvents.DamageTaken(GetFloatingTextSpawnPosition(), amount, isPlayer: true, isDirectPlayerHit: true);

        // Golpe fatal vai direto pro DieTrigger — não dispara Damage no mesmo frame que
        // já vai morrer.
        if (HealthSystem.IsDead(stats.health)) OnDeath();
        // isTrapped aqui também: sem Trapped -> Damage no grafo, um DamageTrigger disparado
        // preso ficaria armado e só dispararia (fora de hora) quando o Trapped terminasse.
        else if (isAttacking || isTrapped) TriggerDamageFlash();
        else if (isReactingToDamage) AccelerateDamageReaction();
        else StartDamageReaction();
    }

    // Wrapper público só pro HeroProjectile (Cima #26 do Barbarian, "Renovação de Batalha") —
    // Heal() em si fica protected de propósito (só o próprio herói decide curar a si mesmo);
    // o projétil não é um HeroController, precisa deste ponto de entrada dedicado.
    public void HealFromProjectile(float amount) => Heal(amount);

    // Centraliza toda cura de qualquer herói (Cleric.ApplyHealWave, regeneração do Ranger,
    // etc.) — clamp em maxHealth, GameEvents.HealthChanged e o Floating Combat Text de cura
    // (verde, com "+") num só lugar, em vez de cada herói duplicar essa lógica.
    // showFloatingText=false pra regen PASSIVO/contínuo (tick todo frame, ver UpdateHpRegen() e
    // o regen do Shift do Barbarian) — achado real do usuário: cada frame curava uma fração de
    // HP (ex. 0,016 de 1/segundo), sempre "> 0f", então o texto flutuante disparava a CADA
    // FRAME (60x/s), cada um arredondado pra exibir "+0" (visualmente parecia bugado/não
    // curava nada) e ainda lagava o jogo de tanto spawnar objeto. Cura de verdade (Cleric,
    // lifesteal, poção) continua com texto — só o drip passivo fica silencioso (a barra de HP
    // já mostra a cura subindo suave, não precisa do texto por frame).
    protected void Heal(float amount, bool showFloatingText = true)
    {
        float healthBefore = stats.health;
        stats.health = Mathf.Min(EffectiveMaxHealth, stats.health + amount);
        GameEvents.HealthChanged(stats.health, EffectiveMaxHealth);

        // Mostra só o que realmente curou — evita "+50" quando faltava 3 de vida.
        float actualHealed = stats.health - healthBefore;
        if (showFloatingText && actualHealed > 0f) GameEvents.HealReceived(GetFloatingTextSpawnPosition(), actualHealed);
    }

    private void StartDamageReaction()
    {
        isReactingToDamage = true;
        damageSpeedMultiplier = 1f;
        damageReactionElapsed = 0f;
        // Não confiar no valor default do parâmetro no Animator (Unity cria Float novo com
        // default 0, não 1 — mesmo bug já visto no Bestiário) antes de disparar o Trigger.
        if (animator != null) animator.SetFloat("DamageSpeedMultiplier", 1f);
        AnimatorTrigger("DamageTrigger");
    }

    // Cada hit recebido enquanto ainda está reagindo ao anterior acelera a animação de
    // dano em vez de simplesmente reiniciá-la ou ignorá-la — passado o teto, sai da reação
    // na marra (Play direto), sem esperar o Animator: a prioridade aqui é devolver o
    // controle de movimento pro jogador o quanto antes.
    private void AccelerateDamageReaction()
    {
        damageSpeedMultiplier += damageSpeedStepPerHit;

        if (damageSpeedMultiplier >= maxDamageSpeedMultiplier)
        {
            isReactingToDamage = false;
            damageSpeedMultiplier = 1f;
            if (animator != null)
            {
                animator.SetFloat("DamageSpeedMultiplier", 1f);
                bool wantsToMove = moveInput.sqrMagnitude > 0.0001f;
                animator.Play(wantsToMove ? "Walk" : "Idle", 0, 0f);
            }
            return;
        }

        if (animator != null) animator.SetFloat("DamageSpeedMultiplier", damageSpeedMultiplier);
    }

    // Unity sobrecarrega "==" / "!=" pra detectar objetos destruídos/inexistentes, mas o
    // operador "?." do C# ignora essa sobrecarga — todo acesso ao animator passa por
    // "!= null" explícito, nunca "?." (mesma regra do EnemyController).
    protected void AnimatorTrigger(string trigger)
    {
        if (animator != null) animator.SetTrigger(trigger);
    }

    // Baú deu gold (ChestRewardSystem — nunca no caminho popup, decisão explícita do usuário).
    // Mathf.Max, não atribuição direta: se já estava tocando (ex.: 2 baús muito próximos, raro
    // nesta fase mas possível) não reinicia o timer pra um valor menor.
    public void ShowGoldStatusFor(float duration) => goldStatusTimer = Mathf.Max(goldStatusTimer, duration);

    // Chamado por VendorCart.Update() todo frame, refletindo se ainda tem item voando/
    // enfileirado pro carrinho — nenhum timer próprio aqui, a duração É literalmente "enquanto
    // isso for true".
    public void SetVendorGoldStatusActive(bool active) => vendorGoldStatusActive = active;

    private void UpdateGoldStatus()
    {
        if (goldStatusTimer > 0f) goldStatusTimer -= Time.deltaTime;

        bool shouldShow = goldStatusTimer > 0f || vendorGoldStatusActive;
        if (statusGoldIndicator != null && statusGoldIndicator.activeSelf != shouldShow)
            statusGoldIndicator.SetActive(shouldShow);
    }

    // Recuperação/Metabolismo Acelerado/Fôlego Eterno (World Perks) — % do HP MÁXIMO efetivo
    // por segundo, contínuo (não em degraus). Heal() já cuida do clamp/evento/Floating Text.
    private void UpdateHpRegen()
    {
        if (HpRegenPercentPerSecond <= 0f) return;
        Heal(EffectiveMaxHealth * HpRegenPercentPerSecond / 100f * Time.deltaTime, showFloatingText: false);
    }

    private void TriggerDamageFlash()
    {
        if (spriteRenderer == null) return;
        spriteRenderer.color = Color.white;
        damageFlashTimer = DamageFlashDuration;
    }

    // Timer manual em vez de Coroutine — Coroutine não respeita GameplayGate (Sprint 13):
    // o flash continuaria contando e revertendo a cor durante a pausa.
    private void UpdateDamageFlash()
    {
        if (damageFlashTimer <= 0f) return;
        damageFlashTimer -= Time.deltaTime;
        if (damageFlashTimer <= 0f && spriteRenderer != null) spriteRenderer.color = spriteOriginalColor;
    }

    // GDD Seção 11 — "Morte: ordem de eventos". Os passos 1-4 são só estado/dado, acontecem
    // na hora; o passo 5 (transição + teleporte) é visual e só acontece quando o clipe "die"
    // termina de tocar de verdade — ver AnimationDieEndEvent().
    private void OnDeath()
    {
        isDead = true;
        IsDying = true; // trava WorldPopulationManager/WorldBossSpawnManager até o herói novo nascer (GameplayBootstrap.SpawnHero)
        isAttacking = false; // sem isso, um herói morto no meio de uma ação renasceria sem poder agir de novo (PrimaryAttack/UseUltimate ignoram clique enquanto isAttacking)
        isAttackResolving = false;
        isReactingToDamage = false;
        damageSpeedMultiplier = 1f;
        isTrapped = false; // mesmo motivo — morrer preso não pode renascer incapaz de agir
        isUsingSecondaryAbility = false;
        IsPlayerUntargetable = false; // sem isso, morrer camuflado deixaria os monstros cegos pro respawn inteiro
        Velocity = Vector2.zero; // sem isso, o herói morto em movimento deixaria todo monstro prevendo um ponto fantasma até o respawn mexer de novo
        if (animator != null)
        {
            animator.SetFloat("DamageSpeedMultiplier", 1f);
            animator.SetBool("IsTrapped", false);
        }
        dieElapsed = 0f;
        dieHandled = false;

        // Efeito Nocivo ativo (ex.: Fire) não pode sobreviver congelado até o respawn — sem
        // isso, Update() parava de chamar Tick() (guarda de isDead acima) e o efeito voltava
        // sozinho depois do respawn, como se nunca tivesse morrido (bug real).
        if (statusEffectController != null) statusEffectController.ClearAllEffects();

        // 1. Cancela estados temporários — hook pra ultimates com duração/transformações e
        // pra pets de kit (GDD: "ao morrer o herói, o pet retorna junto na transição"). Roda
        // ANTES do DieTrigger de propósito: o Alce do Druid troca o runtimeAnimatorController
        // de volta pro humano aqui dentro — se o Trigger disparasse antes, pegaria o
        // controller errado (sem estado "Die"), viraria no-op, e a troca de controller logo
        // em seguida resetaria o Trigger já "gasto" sem nunca tocar animação de morte nenhuma.
        OnHeroDeath();

        AnimatorTrigger("DieTrigger");
        Debug.Log("[HeroController] Morreu — aguardando Animation Event de fim do die...");

        // 2. Destrói loot carregado
        BagController.Instance.Bag.Clear();
        GameEvents.BagChanged(BagController.Instance.Bag);

        // 3. Zera Energia da Ultimate
        stats.energy = 0f;
        GameEvents.EnergyChanged(stats.energy, stats.maxEnergy);

        // 4. Reseta o mundo (monstro/projétil/loot somem na hora) — de propósito IMEDIATO, não
        // espera a animação de morte terminar (pedido do usuário: monstro/projétil continuando
        // em cena enquanto o "Die" toca ficava estranho). NÃO abre a Loja ainda — isso é
        // DayResolver.OpenResultsAfterDeath(), chamado só depois que a animação termina de
        // verdade (AnimationDieEndEvent) — o jogador precisa VER o "Die" primeiro.
        DayResolver.ResolvePlayerDeath();
    }

    // Chamado por um Animation Event no último frame do clipe "die" (mesmo padrão do
    // EnemyController) — a animação é a fonte de verdade do timing.
    //
    // GDD Deadline 8.1, Seção 3 (revisado 2x — achados reais do usuário):
    // 1ª revisão: antes, isto reposicionava a MESMA instância de volta na origem com HP cheio
    // ("Respawn"). Agora só destrói — a instância que volta da Loja é sempre 100% nova
    // (GameplayBootstrap.RespawnHeroFresh(), chamado por ShopHandler.ProceedFromShop()), nunca
    // a mesma reaproveitada — evita valores só aplicados no Instantiate/Start (ex.: size)
    // ficarem presos no estado antigo até um New Game do zero.
    // 2ª revisão: o reset do MUNDO (DayResolver.ResolvePlayerDeath()) é imediato, em OnDeath()
    // (passo 4) — só a troca de GameState pra abrir a Loja (OpenResultsAfterDeath) mora aqui,
    // depois que a animação termina de verdade (evento real ou timeout de segurança, tanto faz).
    public virtual void AnimationDieEndEvent()
    {
        if (dieHandled) return; // proteção — evento + timeout de segurança não chamam Destroy() duas vezes
        dieHandled = true;

        DayResolver.OpenResultsAfterDeath();
        Destroy(gameObject);
    }

    protected abstract void PrimaryAttack();
    protected abstract void UseUltimate();
    protected abstract void UseSecondaryAbility();

    // Default true — cada herói que tem alguma janela em que a própria Ultimate não pode
    // rodar (isAttacking de qualquer um, isUsingSecondaryAbility do Druid enquanto Coruja)
    // sobrescreve isso, em vez de só dar "return" por dentro de UseUltimate(). TryUseUltimate()
    // checa ANTES de gastar energia — um "return" só de dentro de UseUltimate() não seria
    // visto por quem chamou, e a energia seria gasta de qualquer jeito.
    protected virtual bool CanUseUltimate() => true;

    // Default true — todo herói existente sempre "faz alguma coisa" ao atacar (projétil,
    // golpe etc.), então o clique sempre gasta o cooldown. Só o Cleric sobrescreve: sem
    // monstro no raio de ataque, PrimaryAttack() não faz nada (exceção de MVP, Sprint 23),
    // e sem esse hook o cooldown seria gasto à toa, deixando o Cleric "travado" por até 1
    // cooldown inteiro assim que um monstro aparecesse no raio logo depois do clique.
    protected virtual bool ShouldConsumeCooldownOnAttack() => true;

    // Default false — Sprint 29 (Assassin): permite pular o cooldown do primário INTEIRO
    // durante a forma sombria (Thousand Blades encadeia sem espera nenhuma), não só "não
    // consome num clique que não fez nada" (isso já é ShouldConsumeCooldownOnAttack). Sem
    // efeito em nenhum outro herói — !isAttacking no Update() já impede 2 ações ao mesmo tempo,
    // então isto só remove a ESPERA entre uma ação e a próxima, nunca a sobreposição.
    protected virtual bool IsAttackCooldownBypassed() => false;

    // Default false — só heróis cuja Habilidade Secundária esconde/transforma o jogador de
    // verdade (camuflagem do Ranger, teleporte do Mage, Coruja do Druid) sobrescrevem isso.
    // Cobre um caso que IsPlayerUntargetable sozinho não cobre: um Slime de dano por contato
    // não "escolhe" o jogador como alvo, só bate por estar encostado fisicamente — sem essa
    // trava direto no TakeDamage(), o dano de contato (ou de um golpe que já estava a
    // caminho) ainda contava mesmo com o jogador "escondido"/em transição.
    protected virtual bool IsDamageImmune => false;

    // Default no-op — só heróis com habilidade cancelável (ex.: camuflagem do Ranger)
    // sobrescrevem isso. Sem override, apertar Shift de novo em habilidade não-cancelável
    // simplesmente não faz nada (não reinicia, não interrompe).
    protected virtual void CancelSecondaryAbility() { }

    // Sprint 21 (Druid) — par simétrico a CancelSecondaryAbility, só que pra Ultimate. Default
    // false/no-op — só o Alce (única Ultimate com duração até hoje) sobrescreve. A GDD pede
    // "cancelamento manual" (apertar o botão de Ultimate de novo enquanto ativa cancela), mesma
    // UX que Shift já usa pra Habilidade Secundária.
    protected virtual bool IsUltimateActive => false;

    protected virtual void CancelUltimate() { }

    // Chamado no início de OnDeath(), antes de qualquer outra limpeza. Default no-op — só
    // heróis com estado que precisa ser desfeito na morte (ex.: pet do Mage/Blood Mage)
    // sobrescrevem isso.
    protected virtual void OnHeroDeath() { }

    // Gizmo de debug (Scene, objeto selecionado) — mesmo padrão do showAttackRadiusGizmo do
    // EnemyController. virtual (não private) pra heróis que já têm o próprio OnDrawGizmosSelected
    // (ex.: Cleric) poderem chamar base.OnDrawGizmosSelected() e somar os dois.
    [SerializeField] private bool showPickupRadiusGizmo = false;

    protected virtual void OnDrawGizmosSelected()
    {
        if (!showPickupRadiusGizmo) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, basePickupRadius);
    }
}
