using System.Collections.Generic;
using UnityEngine;

// Fundação de streaming de chunks (Sprint P1 — Pré-Deadline Mundo Único). Decide QUAIS
// chunks ficam ativos ao redor de um alvo (normalmente o jogador), de forma determinística
// por WorldSeed + coordenada — nunca por ordem de chamada. Ainda NÃO gera terreno real
// (isso é a Sprint P2); cada chunk, por enquanto, só carrega um ChunkRuntimeData com os
// valores de hash determinísticos da própria célula, prova de que a Fundação funciona antes
// de qualquer Tilemap/autotile entrar em cena.
public class ChunkStreamer : MonoBehaviour
{
    public static ChunkStreamer Instance { get; private set; }

    public WorldGenerationSettings settings;
    public Transform streamingTarget;

    private readonly Dictionary<ChunkCoord, ChunkRuntimeData> activeChunks = new();

    public IReadOnlyDictionary<ChunkCoord, ChunkRuntimeData> ActiveChunks => activeChunks;

    private void Awake() => Instance = this;

    private void Update()
    {
        if (streamingTarget == null || settings == null) return;
        RefreshAroundTarget(streamingTarget.position);
    }

    // Recalcula o conjunto de chunks que deveriam estar ativos em torno de `worldPosition` e
    // aplica a diferença (ativa os que faltam, desativa os que saíram do raio). Chamado todo
    // frame pelo Update, mas também exposto pra uso direto em teste/edição.
    public void RefreshAroundTarget(Vector2 worldPosition)
    {
        var center = ChunkCoord.FromWorldPosition(worldPosition, settings.chunkSize);
        var desired = new HashSet<ChunkCoord>();

        int radius = settings.streamingRadiusInChunks;
        for (int dx = -radius; dx <= radius; dx++)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                desired.Add(new ChunkCoord(center.X + dx, center.Y + dy));
            }
        }

        foreach (var coord in desired)
        {
            if (!activeChunks.ContainsKey(coord))
                activeChunks[coord] = GenerateChunkData(coord);
        }

        var toRemove = new List<ChunkCoord>();
        foreach (var coord in activeChunks.Keys)
        {
            if (!desired.Contains(coord)) toRemove.Add(coord);
        }
        foreach (var coord in toRemove) activeChunks.Remove(coord);
    }

    // Gera (ou consulta, se já existir) os dados determinísticos de 1 chunk. Nunca depende de
    // quais outros chunks já foram gerados nem da ordem em que foi chamado — só de
    // WorldSeed + coordenada.
    public ChunkRuntimeData GenerateChunkData(ChunkCoord coord)
    {
        return new ChunkRuntimeData(coord, settings.worldSeed);
    }

    // Consulta um canal de hash de um chunk vizinho mesmo que ele não esteja ativo agora —
    // é o "halo"/margem de amostragem (Doc Seção 12/36): vizinhança sempre pode ser
    // consultada, mesmo descarregada, porque é 100% derivada de (WorldSeed, coordenada).
    public ChunkRuntimeData PeekChunkData(ChunkCoord coord)
    {
        return activeChunks.TryGetValue(coord, out var data) ? data : GenerateChunkData(coord);
    }

    // Verificação visual manual (Scene view, sem Play Mode necessário pra desenhar o grid):
    // cada chunk ativo vira um quadrado colorido pelo próprio DebugSeedValue — se a mesma
    // seed/posição sempre pintar a mesma cor, qualquer que seja a ordem em que o jogador
    // visitou os chunks, a Fundação está correta.
    private void OnDrawGizmos()
    {
        if (settings == null) return;
        foreach (var pair in activeChunks)
        {
            var coord = pair.Key;
            var color = ColorFromDebugSeedValue(pair.Value.DebugSeedValue);
            var center = new Vector3(
                (coord.X + 0.5f) * settings.chunkSize,
                (coord.Y + 0.5f) * settings.chunkSize,
                0f);
            Gizmos.color = color;
            Gizmos.DrawWireCube(center, new Vector3(settings.chunkSize, settings.chunkSize, 0f));
        }
    }

    private static Color ColorFromDebugSeedValue(uint value)
    {
        float r = ((value >> 0) & 0xFF) / 255f;
        float g = ((value >> 8) & 0xFF) / 255f;
        float b = ((value >> 16) & 0xFF) / 255f;
        return new Color(r, g, b);
    }
}
