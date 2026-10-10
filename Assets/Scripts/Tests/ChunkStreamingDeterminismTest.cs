using UnityEngine;

// Teste manual (Sprint P1 — Pré-Deadline Mundo Único): confirma em Play Mode que o
// ChunkStreamer produz exatamente o mesmo ChunkRuntimeData pra uma coordenada, independente
// da ordem em que os chunks em volta dela foram carregados — o Exit Criteria central da
// sprint ("mesma seed/coordenada produz o mesmo chunk em qualquer ordem de carregamento").
public class ChunkStreamingDeterminismTest : MonoBehaviour
{
    public WorldGenerationSettings settings;
    public ChunkCoordInput targetChunk = new ChunkCoordInput { x = 3, y = -2 };

    [System.Serializable]
    public struct ChunkCoordInput
    {
        public int x;
        public int y;
    }

    [ContextMenu("Compare Chunk Data — Direct vs. After Generating Neighbors First")]
    public void CompareOrderIndependence()
    {
        var coord = new ChunkCoord(targetChunk.x, targetChunk.y);

        // Caminho A: gera o chunk-alvo direto, sem vizinhos antes.
        var streamerA = new GameObject("TempStreamerA").AddComponent<ChunkStreamer>();
        streamerA.settings = settings;
        var dataA = streamerA.GenerateChunkData(coord);

        // Caminho B: gera os 8 vizinhos primeiro, em ordem "errada" de propósito, só depois o alvo.
        var streamerB = new GameObject("TempStreamerB").AddComponent<ChunkStreamer>();
        streamerB.settings = settings;
        for (int dy = 1; dy >= -1; dy--)
        {
            for (int dx = 1; dx >= -1; dx--)
            {
                if (dx == 0 && dy == 0) continue;
                streamerB.GenerateChunkData(new ChunkCoord(coord.X + dx, coord.Y + dy));
            }
        }
        var dataB = streamerB.GenerateChunkData(coord);

        bool match = dataA.DebugSeedValue == dataB.DebugSeedValue;
        Debug.Log(match
            ? $"[ChunkStreamingDeterminismTest] OK — chunk {coord} produziu o mesmo DebugSeedValue ({dataA.DebugSeedValue}) nos 2 caminhos."
            : $"[ChunkStreamingDeterminismTest] FALHA — chunk {coord} produziu valores diferentes: A={dataA.DebugSeedValue}, B={dataB.DebugSeedValue}.");

        DestroyImmediate(streamerA.gameObject);
        DestroyImmediate(streamerB.gameObject);
    }
}
