using System.Collections.Generic;
using UnityEngine;

// Etapas 1+2 — configuração da run de 25 min (docs/superpowers/specs/2026-11-24-etapas-1-2-run-25min-cartas-design.md, 5.x).
// Fica em Resources/RunSettings.asset. A ordem dos bosses é do usuário: cada espaço = 1 minuto,
// o Divine God sempre no último. Todos os números 🔢 são ajustáveis aqui, nunca no código.
[CreateAssetMenu(fileName = "RunSettings", menuName = "MonsterLLC/Run Settings")]
public class RunSettings : ScriptableObject
{
    [Header("Relógio e bosses")]
    [Tooltip("Ordem dos bosses (1 espaço por minuto). O Divine God (tier 10) tem que ser o último.")]
    public List<BossDefinitionSO> bossOrder = new();
    public float runSeconds = 1500f; // 🔢 25 min
    public float bossIntervalSeconds = 60f; // 🔢

    [Header("Dia e noite")]
    public float daySeconds = 60f; // 🔢
    public float nightSeconds = 30f; // 🔢
    [Tooltip("Velocidade dos monstros (e bosses) à noite.")]
    public float nightSpeedMultiplier = 1.15f; // 🔢

    [Header("Enrage do boss")]
    public float enrageSpeedAfter = 60f; // 🔢 segundos vivo
    public float enrageSpeedMultiplier = 1.5f; // 🔢
    public float enrageDamageAfter = 120f; // 🔢 segundos vivo
    public float enrageDamageMultiplier = 1.5f; // 🔢
    public float enrageDamageStepSeconds = 30f; // 🔢
    public float enrageDamageStepPercent = 25f; // 🔢

    [Header("Troca de tier e mini boss")]
    [Tooltip("Segundos para os monstros do tier seguinte irem de 0% a 100% da horda.")]
    public float tierRampSeconds = 90f; // 🔢
    public float miniBossMinDelay = 15f; // 🔢 segundos depois do boss
    public float miniBossMaxDelay = 45f; // 🔢

    [Header("Gemas de XP (etapa 2)")]
    [Tooltip("Sprite da gema no chão.")]
    public Sprite gemSprite;
    public float gemScale = 0.5f; // 🔢
    [Tooltip("Limite de gemas no chão (desempenho). Passou disso, uma gema existente absorve o XP da nova.")]
    public int maxGems = 400; // 🔢
    [Tooltip("Raio do ímã (unidades) antes das cartas de ímã.")]
    public float gemMagnetRadius = 2.5f; // 🔢
    public float gemFlySpeed = 12f; // 🔢 unidades/s
    public int miniBossGems = 5; // 🔢
    public int bossGems = 20; // 🔢

    [Header("Cartas: itens e Stats (etapa 3)")]
    [Tooltip("Espaços de item. Cheio: só sobem de nível os itens que o herói já tem.")]
    public int maxItemSlots = 6; // 🔢
    [Tooltip("A cada N níveis, a oferta de level up é de Stats.")]
    public int statEveryLevels = 5; // 🔢
    [Tooltip("Chance (%) de cada carta do baú virar um Stat.")]
    public float chestStatChancePercent = 15f; // 🔢

    [Header("Cartas: oferta de level up e atributos (etapa 4)")]
    [Tooltip("Chance (%) de cada carta do level up comum ser Upgrade (senão, Item).")]
    public float levelUpUpgradeChancePercent = 60f; // 🔢
    [Tooltip("No máximo N itens por oferta de level up; o excedente vira Upgrade.")]
    public int maxItemsPerOffer = 2; // 🔢
    [Tooltip("Atributos (Stats) que o herói pode ter. Cheio: Stat nenhum aparece mais.")]
    public int maxStatSlots = 6; // 🔢
    [Tooltip("Cartas que SEMPRE aparecem no 1º level up (pedido do usuário: dano e vida gerais).")]
    public List<CardDefinition> firstLevelUpCards = new();
    [Tooltip("Peso dos itens que o herói JÁ TEM no sorteio de item (level up e vendedor). 1 = mesmo peso dos novos.")]
    public float ownedItemWeight = 3f; // 🔢
    [Tooltip("Peso dos upgrades prioritários (primário, dano geral, vida geral) no sorteio de Upgrade do level up, até priorityFadeStartLevel.")]
    public float priorityUpgradeWeightEarly = 2.5f; // 🔢
    [Tooltip("Peso dos upgrades prioritários a partir de priorityFadeEndLevel (entre os dois níveis, cai em linha reta).")]
    public float priorityUpgradeWeightLate = 1.5f; // 🔢
    public int priorityFadeStartLevel = 5; // 🔢
    public int priorityFadeEndLevel = 20; // 🔢

    [Header("Baú e vendedor no mundo (etapa 3)")]
    [Tooltip("Aparições de baú por run (sorteado entre mín e máx, espalhado em faixas iguais).")]
    public int chestMinCount = 4; // 🔢
    public int chestMaxCount = 5; // 🔢
    [Tooltip("Aparições do vendedor por run.")]
    public int vendorMinCount = 6; // 🔢
    public int vendorMaxCount = 8; // 🔢
    [Tooltip("Segundos do começo da run sem baú nem vendedor.")]
    public float rewardQuietSeconds = 60f; // 🔢
    [Tooltip("Prefab do vendedor (criado no mundo em runtime).")]
    public GameObject vendorPrefab;

    [Header("Efeitos de itens e Stats (etapa 3)")]
    [Tooltip("Teto da redução de dano somada (perks + Iron Plate + Iron Will), em %.")]
    public float damageReductionCapPercent = 75f; // 🔢
    [Tooltip("Blast Charm: raio da explosão (unidades).")]
    public float blastRadius = 1.5f; // 🔢
    [Tooltip("Blast Charm: dano da explosão em % do dano do ataque primário do herói.")]
    public float blastDamagePercent = 100f; // 🔢

    // Catálogo de cartas (docs/superpowers/specs/2026-11-25-catalogo-cartas-design.md) — valores
    // secundários dos efeitos. Os valores por nível ficam nas próprias cartas.
    [Header("Catálogo de cartas — dano e crítico")]
    public float baseCritChancePercent = 5f; // 🔢 crítico base de todo ataque direto (decisão do usuário)
    public float executeThreshold = 0.30f; // 🔢 Executioner's Mark
    public float elementalSupremacyBonus = 0.25f; // 🔢
    public float finalJudgmentBonus = 0.30f; // 🔢
    public float trophyPerBoss = 0.05f; // 🔢
    public float trophyCap = 0.50f; // 🔢
    public float focusPerHit = 0.05f; // 🔢 Predator's Focus
    public float focusCap = 0.50f; // 🔢
    public int endlessKillsPerStep = 30; // 🔢 Endless Hunger
    public float endlessPerStep = 0.01f; // 🔢
    public float endlessCap = 0.25f; // 🔢
    public float finalStandThreshold = 0.30f; // 🔢
    public float finalStandDamage = 0.20f; // 🔢
    public float finalStandReduction = 0.20f; // 🔢
    public float feverPerStack = 0.02f; // 🔢 Critical Fever
    public int feverMaxStacks = 10; // 🔢
    public float feverDuration = 4f; // 🔢
    public float bloodFrenzyThreshold = 0.40f; // 🔢
    public float bloodFrenzyBonus = 0.25f; // 🔢 dano e velocidade
    public float bloodFrenzyDuration = 4f; // 🔢
    public int echoFangEvery = 4; // 🔢
    public float critSecondImpactPercent = 0.50f; // 🔢 Shattered Fortune
    public float overkillPercent = 0.50f; // 🔢
    public float overkillRange = 3f; // 🔢
    public int crowdBreakerTargets = 3; // 🔢
    public float crowdBreakerBonus = 0.40f; // 🔢
    public float relentlessMoveSeconds = 2f; // 🔢 Relentless Momentum
    public float relentlessDamage = 0.50f; // 🔢
    public float relentlessArea = 0.30f; // 🔢
    public float relentlessCastingPerHit = 0.1f; // 🔢 segundos
    public float relentlessCastingPerSecond = 0.5f; // 🔢 teto por segundo

    [Header("Catálogo de cartas — DOT")]
    public int dotExplosionEveryTicks = 8; // 🔢 Affliction Heart
    public float dotExplosionRadius = 1.5f; // 🔢
    public float plagueRadius = 2.5f; // 🔢 Plague Herald
    public int plagueTargets = 2; // 🔢
    public float deathsEchoPercent = 0.50f; // 🔢
    public float deathsEchoRadius = 1.5f; // 🔢
    public int afflictionHarvestEvery = 20; // 🔢 ticks
    public float afflictionHarvestEnergyPercent = 2f; // 🔢 % da energia máxima

    [Header("Catálogo de cartas — defesa, cura e movimento")]
    public float wellspringCooldown = 10f; // 🔢
    public float thornCooldown = 0.5f; // 🔢 Thornwake Armor
    public int thornBaseCount = 2; // 🔢 + nível
    public float thornScale = 0.5f; // 🔢 escala do projétil (Thunder Blade) usado como espinho
    public float lastEmberCooldown = 300f; // 🔢 5 minutos
    public float lastEmberInvulnerable = 2f; // 🔢
    public float graveboundRadius = 2.5f; // 🔢
    public int graveboundMaxEnemies = 10; // 🔢
    public float killHealCapPerSecond = 0.05f; // 🔢 Reaper's Feast (fração da vida máxima)
    public int titanKillsPerStep = 25; // 🔢 Titan's Tribute
    public float titanPerStep = 0.01f; // 🔢
    public float titanCap = 0.50f; // 🔢
    public float survivorPerBoss = 0.05f; // 🔢 Survivor's Legacy
    public float survivorCap = 0.50f; // 🔢
    public float goldenMomentumBonus = 0.20f; // 🔢
    public float goldenMomentumDuration = 2f; // 🔢
    public float dangerousXpBonus = 0.30f; // 🔢 Dangerous Bargain
    public float dangerousDamageTaken = 0.15f; // 🔢
    public float scarPerStack = 0.03f; // 🔢 Scars of Battle
    public int scarMaxStacks = 5; // 🔢
    public float scarDuration = 4f; // 🔢
    public int unbrokenKills = 15; // 🔢
    public float unbrokenShield = 0.20f; // 🔢 fração da vida máxima
    public float unbrokenDuration = 8f; // 🔢
    public float adrenalineThreshold = 0.25f; // 🔢
    public float adrenalineBonus = 0.30f; // 🔢
    public float adrenalineDuration = 3f; // 🔢
    public float adrenalineCooldown = 10f; // 🔢
    public float warriorsHeal = 0.25f; // 🔢 Warrior's Feast
    public float warriorsShield = 0.15f; // 🔢
    public float warriorsDuration = 10f; // 🔢
    public float movingFortressSeconds = 1.5f; // 🔢
    public float movingFortressReduction = 0.15f; // 🔢
    public float spiritLinkEnergyPercent = 5f; // 🔢 % da energia máxima

    [Header("Catálogo de cartas — projéteis (Ranger)")]
    public float homingBaseRadius = 3f; // 🔢 Predator's Eye (+1 por nível)
    public float shardDamagePercent = 0.40f; // 🔢 Shattered Prism
    public float ricochetRange = 4f; // 🔢 Ricochet Fate
    public float ricochetDamagePercent = 0.60f; // 🔢

    [Header("Sistemas fora da run (etapa 3 religa baú/vendedor dando cartas)")]
    [Tooltip("Gold por kill/sobrevivência dentro da run. Não existe mais gold na run (etapas 1+2).")]
    public bool runGoldEnabled = false;
    [Tooltip("Monstros dropando loot (materiais) pra Bag.")]
    public bool lootEnabled = false;
    [Tooltip("Baús e Chest Mimic nascendo no mundo.")]
    public bool chestsEnabled = false;
    [Tooltip("Vendedor no mundo.")]
    public bool vendorEnabled = false;
    [Tooltip("Fase 1: o mundo inteiro usa um único bioma (o do T1), sem trocar por distância/tier.")]
    public bool singleBiome = true;

    // Asset carregado uma vez (lido por kill/morte/célula de chunk — nunca Resources.Load por chamada).
    // Sem asset (cena de teste antiga): os padrões do código (tudo desligado, bioma único).
    private static RunSettings current;
    public static RunSettings Current
    {
        get
        {
            if (current == null) current = Load();
            if (current == null) current = CreateInstance<RunSettings>();
            return current;
        }
    }

    public static bool Flag(System.Func<RunSettings, bool> read) => read(Current);

    public static RunSettings Load() => Resources.Load<RunSettings>("RunSettings");

    // Tier de cada espaço (0 = espaço vazio).
    public int[] BossTiers()
    {
        var tiers = new int[bossOrder.Count];
        for (int i = 0; i < tiers.Length; i++) tiers[i] = bossOrder[i] != null ? bossOrder[i].tier : 0;
        return tiers;
    }

    public string[] Validate()
    {
        var warnings = new List<string>();
        if (bossOrder.Count != 25) warnings.Add($"A ordem dos bosses tem {bossOrder.Count} espaços (esperado 25).");
        for (int i = 0; i < bossOrder.Count; i++)
            if (bossOrder[i] == null) warnings.Add($"Espaço {i + 1} da ordem dos bosses está vazio (esse minuto fica sem boss).");
            else if (bossOrder[i].prefab == null || bossOrder[i].prefab.GetComponent("EnemyController") == null)
                warnings.Add($"Espaço {i + 1} ({bossOrder[i].name}): prefab sem EnemyController (esse minuto fica sem boss).");
        if (bossOrder.Count > 0)
        {
            var last = bossOrder[bossOrder.Count - 1];
            if (last == null || last.tier != 10) warnings.Add("O último espaço tem que ser o Divine God (tier 10).");
        }
        return warnings.ToArray();
    }

    private void OnValidate()
    {
        foreach (var w in Validate()) Debug.LogWarning("[RunSettings] " + w, this);
    }
}
