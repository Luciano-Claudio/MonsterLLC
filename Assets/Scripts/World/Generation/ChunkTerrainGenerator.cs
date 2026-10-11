using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// Pinta/limpa o terreno real de cada chunk conforme ele entra/sai do raio de streaming do
// ChunkStreamer (Sprint P1) — Sprint P2 da Pré-Deadline Mundo Único. Modelo final de grids
// aninhados, descoberto durante a própria sprint (ver sprint report): cada profundidade de
// aninhamento (Grama no grid 0, DryDirt no grid 1, DarkSoil dentro do DryDirt no grid 2...)
// é um Tilemap SEPARADO — é assim que um RuleTile comum (This/Not This) já basta: ele nunca
// vê um terreno aninhado mais fundo como vizinho, porque está em outro Tilemap.
// `layerTilemaps[0]` = grid 0 (base), `layerTilemaps[1]` = grid 1, e assim por diante —
// precisa ter pelo menos tantos Tilemaps quanto a maior profundidade de aninhamento que o
// bioma usar.
public class ChunkTerrainGenerator : MonoBehaviour
{
    public WorldGenerationSettings settings;
    public BiomeProfileSO biome;
    public List<Tilemap> layerTilemaps = new();

    // Tilemap própria pra variantes do terreno-base (ex.: HighGrass/ThickGrass) — separada de
    // layerTilemaps porque não é mais uma profundidade de aninhamento, é decoração só de
    // célula "pura" (BiomeProfileSO.baseVariants). Opcional — null = sem variantes.
    public Tilemap baseVariantTilemap;

    public Transform propContainer;

    private readonly HashSet<ChunkCoord> paintedChunks = new();
    private readonly Dictionary<ChunkCoord, List<GameObject>> spawnedProps = new();

    // Fila de 1 chunk por vez, processada pela coroutine ProcessPipeline — espalha até as 4
    // chamadas de SetTilesBlock de 1 ÚNICO chunk (3 camadas + variante) por VÁRIOS frames (1
    // chamada por frame), não só os chunks entre si. O jogador nunca chega perto o bastante,
    // rápido o bastante, pra notar esse atraso (ideia do usuário, Sprint P2) — o chunk sempre
    // termina de pintar bem antes de ficar visível de perto.
    private readonly Queue<ChunkCoord> pendingPaint = new();
    private readonly Queue<ChunkCoord> pendingClear = new();
    private readonly HashSet<ChunkCoord> queuedChunks = new(); // evita enfileirar o mesmo 2x enquanto ele ainda está na fila/sendo processado

    private float seedOffsetX;
    private float seedOffsetY;

    // Buffers reutilizados entre chunks (pintura/limpeza em lote via SetTilesBlock, não 1
    // SetTile por célula — com chunkSize=64, isso é 4096 células; pintar/limpar célula a
    // célula fazia o Tilemap recalcular a malha miles de vezes por chunk e travava o jogo ao
    // cruzar chunk). Alocados 1x em Awake, reaproveitados sempre — zero GC novo por chunk.
    private TileBase[][] layerBuffer;
    private TileBase[] variantBuffer;
    private TileBase[] clearBuffer;
    private TerrainDefinitionSO[] stackBuffer;
    private readonly List<ChunkCoord> toClearBuffer = new();

    private void Awake()
    {
        int seed = settings != null ? settings.worldSeed : 0;
        // Offset derivado da seed, nunca de Random/Time — mesma seed, mesmo deslocamento, sempre.
        seedOffsetX = WorldHash.Hash2DFloat(1, 0, channel: 900, seed: seed) * 10000f;
        seedOffsetY = WorldHash.Hash2DFloat(0, 1, channel: 900, seed: seed) * 10000f;

        int cellCount = settings != null ? settings.chunkSize * settings.chunkSize : 0;
        layerBuffer = new TileBase[layerTilemaps.Count][];
        for (int i = 0; i < layerTilemaps.Count; i++) layerBuffer[i] = new TileBase[cellCount];
        if (baseVariantTilemap != null) variantBuffer = new TileBase[cellCount];
        clearBuffer = new TileBase[cellCount]; // todo null — reaproveitado pra limpar qualquer camada
        stackBuffer = new TerrainDefinitionSO[layerTilemaps.Count];

        StartCoroutine(ProcessPipeline());
    }

    private void Update()
    {
        if (ChunkStreamer.Instance == null || settings == null || biome == null) return;

        var active = ChunkStreamer.Instance.ActiveChunks;

        foreach (var coord in active.Keys)
        {
            if (!paintedChunks.Contains(coord) && !queuedChunks.Contains(coord))
            {
                pendingPaint.Enqueue(coord);
                queuedChunks.Add(coord);
            }
        }

        toClearBuffer.Clear();
        foreach (var coord in paintedChunks)
        {
            if (!active.ContainsKey(coord) && !queuedChunks.Contains(coord)) toClearBuffer.Add(coord);
        }
        foreach (var coord in toClearBuffer)
        {
            pendingClear.Enqueue(coord);
            queuedChunks.Add(coord);
        }
    }

    // Processa 1 chunk de cada vez, do início ao fim, antes de passar pro próximo — os buffers
    // (layerBuffer/variantBuffer/stackBuffer) são reaproveitados entre chunks, então só é
    // seguro ter 1 chunk "em voo" na pipeline por vez (sem concorrência entre coroutines).
    private IEnumerator ProcessPipeline()
    {
        while (true)
        {
            if (pendingPaint.Count > 0)
            {
                yield return StartCoroutine(PaintChunkIncremental(pendingPaint.Dequeue()));
            }
            else if (pendingClear.Count > 0)
            {
                yield return StartCoroutine(ClearChunkIncremental(pendingClear.Dequeue()));
            }
            else
            {
                yield return null; // nada pendente — espera o próximo frame e checa de novo
            }
        }
    }

    private IEnumerator PaintChunkIncremental(ChunkCoord coord)
    {
        int size = settings.chunkSize;
        int originX = coord.X * size;
        int originY = coord.Y * size;

        // Parte CPU-only (sem custo de Tilemap) — calcula a pilha de terreno de cada célula e
        // enche os buffers inteiros de uma vez. O que é espalhado entre frames abaixo é só o
        // SetTilesBlock em si, que é onde o Profiler do usuário realmente mediu o custo
        // (Tilemap.FindAllRefreshPositions, ~5-6ms por chamada).
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int cellX = originX + x;
                int cellY = originY + y;
                int index = y * size + x; // ordem esperada por SetTilesBlock: x varia mais rápido

                int stackDepth = TerrainSelector.ChooseTerrainStackNonAlloc(
                    cellX, cellY, seedOffsetX, seedOffsetY, biome, stackBuffer, settings.safeZoneRadius);

                for (int depth = 0; depth < layerTilemaps.Count; depth++)
                {
                    layerBuffer[depth][index] = depth < stackDepth ? stackBuffer[depth].tile : null;
                }

                if (variantBuffer != null)
                {
                    // Só pinta variante em célula "pura" — pilha de tamanho 1 significa que
                    // nenhum patchTerrain (Dirt/Stone/Água) reivindicou esta célula.
                    var variant = stackDepth == 1
                        ? TerrainSelector.ChooseBaseVariant(cellX, cellY, seedOffsetX, seedOffsetY, biome)
                        : null;
                    variantBuffer[index] = variant != null ? variant.tile : null;
                }
            }
        }

        var bounds = new BoundsInt(originX, originY, 0, size, size, 1);
        for (int depth = 0; depth < layerTilemaps.Count; depth++)
        {
            layerTilemaps[depth].SetTilesBlock(bounds, layerBuffer[depth]);
            yield return null; // 1 SetTilesBlock por frame, não os 4 da camada juntos
        }
        if (baseVariantTilemap != null)
        {
            baseVariantTilemap.SetTilesBlock(bounds, variantBuffer);
            yield return null;
        }

        SpawnProps(coord, originX, originY, size);

        paintedChunks.Add(coord);
        queuedChunks.Remove(coord);
    }

    private void SpawnProps(ChunkCoord coord, int originX, int originY, int size)
    {
        var placedThisChunk = new List<GameObject>();
        float safeZoneRadiusSqr = settings.safeZoneRadius * settings.safeZoneRadius;

        foreach (var prop in biome.props)
        {
            if (prop == null || prop.prefab == null) continue;

            var positions = PropPlacement.GenerateCandidatePositions(
                originX, originY, size, prop.minSpacing, prop.density, prop.jitter, settings.worldSeed);

            foreach (var pos in positions)
            {
                // Nenhum prop na safe zone — é onde NPCs de posição fixa vão ficar, um prop
                // gerado ali podia nascer em cima/perto deles.
                if (settings.safeZoneRadius > 0f && pos.sqrMagnitude <= safeZoneRadiusSqr) continue;

                var instance = Instantiate(prop.prefab, new Vector3(pos.x, pos.y, 0f), Quaternion.identity, propContainer);
                placedThisChunk.Add(instance);
            }
        }

        spawnedProps[coord] = placedThisChunk;
    }

    private IEnumerator ClearChunkIncremental(ChunkCoord coord)
    {
        int size = settings.chunkSize;
        int originX = coord.X * size;
        int originY = coord.Y * size;
        var bounds = new BoundsInt(originX, originY, 0, size, size, 1);

        foreach (var tilemap in layerTilemaps)
        {
            tilemap.SetTilesBlock(bounds, clearBuffer);
            yield return null;
        }
        if (baseVariantTilemap != null)
        {
            baseVariantTilemap.SetTilesBlock(bounds, clearBuffer);
            yield return null;
        }

        if (spawnedProps.TryGetValue(coord, out var props))
        {
            foreach (var instance in props)
            {
                if (instance != null) Destroy(instance);
            }
            spawnedProps.Remove(coord);
        }

        paintedChunks.Remove(coord);
        queuedChunks.Remove(coord);
    }
}
