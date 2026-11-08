using UnityEngine;

// Config central do spawn de Baú no mundo aberto (GDD Deadline 8.1, Seção 8) — paralelo a
// BossSpawnSettings/MonsterSpawnSettings, mas pros parâmetros próprios do Baú. Decisão do
// usuário: o Baú NÃO tem uma quantidade fixa/por-dia — é uma % de "o dia tem baú ou não",
// rolada 1x por dia (pode não ter baú nenhum). Essa % é de propósito baixa e vai crescer via
// World Perks (Seção 8, pendência "aumentar a % de surgir baú") — o hook em si (bônus do
// WorldPerkEffects somado aqui) só entra quando os Perks existirem de verdade; por enquanto
// chestSpawnChancePercent é só a base fixa configurável no Inspector.
[CreateAssetMenu(fileName = "ChestSpawnSettings", menuName = "MonsterLLC/World Generation/Chest Spawn Settings")]
public class ChestSpawnSettings : ScriptableObject
{
    [Range(0f, 100f)]
    [Tooltip("% de chance de nascer 1 baú no dia (rolado 1x só, no início do dia — nunca mais de 1 baú por dia nesta fase). Decisão do usuário: baús são fortes de propósito, não triviais — a base é baixa e cresce via World Perks depois.")]
    public float chestSpawnChancePercent = 15f;

    [Range(0f, 100f)]
    [Tooltip("Rolado individualmente pro baú do dia, se ele nascer: chance de ser, na verdade, um Chest Mimic disfarçado em vez do Chest normal.")]
    public float mimicChancePercent = 20f;

    [Tooltip("Anel de spawn ao redor do jogador pro Baú, em CHUNKS (não em unidades de mundo) — decisão do usuário: baú precisa forçar exploração, nunca nascer perto. Convertido pra unidades de mundo multiplicando por WorldGenerationSettings.chunkSize (ex.: minChunksAway=2 com chunkSize=64 = raio mínimo real de 128).")]
    public int minChunksAway = 2;
    public int maxChunksAway = 4;

    [Tooltip("Somado a WorldGenerationSettings.safeZoneRadius pra decidir a distância mínima de spawn — mesmo conceito do monstro comum/Boss.")]
    public float spawnSafetyPadding = 0f;

    [Tooltip("Tentativas de amostragem de posição válida (fora da Safe Zone + nó caminhável) antes de desistir e não nascer baú nenhum hoje.")]
    public int maxSpawnPositionAttempts = 20;

    [Header("Distância física de obstáculo (só pro spawn, nunca muda o grafo A*)")]
    [Tooltip("Mesmo mecanismo do monstro comum/Boss (WorldSpawnUtility) — raio de sondagem física pra não nascer colado numa borda.")]
    public float spawnObstacleClearanceRadius = 0.6f;
    public LayerMask obstacleLayerMask;

    [Header("Recompensa (ChestRewardSystem — fonte única, abrir o Chest OU matar o Chest Mimic dão a MESMA recompensa)")]
    [Range(0f, 100f)]
    [Tooltip("Decisão do usuário (2026-11-08): 50% gold, 50% abre o Chest_Popup (perks/talentos).")]
    public float choicePopupChancePercent = 50f;

    public long minGoldReward = 50;
    public long maxGoldReward = 200;

    [Range(0f, 100f)]
    [Tooltip("Chance de dar 2 recompensas em vez de 1 — placeholder até WorldPerkEffects.ChestDoubleChancePercent existir (perk \"Baú Duplo\").")]
    public float doubleRewardChancePercent = 0f;

    [Tooltip("Quanto tempo o \"Status_Gold\" do herói fica ativo quando o baú dá gold (decisão do usuário, 2026-11-08: NUNCA no caminho popup — só quando vem gold de verdade).")]
    public float goldStatusDuration = 3f;
}
