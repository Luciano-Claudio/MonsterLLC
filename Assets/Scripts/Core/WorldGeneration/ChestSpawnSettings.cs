using UnityEngine;

// Config do baú no mundo aberto (posição/anel e chance de mímico). Etapa 3: QUANTOS baús por run
// ficam no RunSettings (chestMin/MaxCount); a recompensa é a oferta de cartas (ChestRewardSystem).
[CreateAssetMenu(fileName = "ChestSpawnSettings", menuName = "MonsterLLC/World Generation/Chest Spawn Settings")]
public class ChestSpawnSettings : ScriptableObject
{
    [Range(0f, 100f)]
    [Tooltip("Chance de cada baú de ser, na verdade, um Chest Mimic disfarçado em vez do Chest normal.")]
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
}
