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
