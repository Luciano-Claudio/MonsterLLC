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
