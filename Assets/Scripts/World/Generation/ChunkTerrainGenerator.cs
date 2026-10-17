using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// Pinta/limpa o terreno real de cada chunk conforme ele entra/sai do raio de streaming do
// ChunkStreamer (Sprint P1) — Sprint P2 (grids aninhados por profundidade) + Sprint P3
// (mundo multi-tier, "mundo vivo", docs/new/Mudanca_Estrutural_Mapa_Unico_Spawn_Bosses_
// ATUALIZADO_V2.md).
//
// Modelo final: 2 eixos de camadas independentes.
// 1) TIER (qual bioma está "ativo" numa célula — Tier1, Tier2-3, Tier4-5, Tier6-8, Tier9,
//    Tier10) — decidido por WorldTierSelector (EffectiveSpawnDay + % de spawn da tabela real
//    + ruído). Cada tier tem seu PRÓPRIO Tilemap de base (`tierBaseTilemaps[0..5]` =
//    Layer0..Layer5 da cena), com Sorting Order crescente. O tier ATIVO pinta seu próprio
//    terreno-base; TODO tier anterior (índice menor) também pinta seu terreno-base puro na
//    MESMA célula, por baixo (achado do usuário, Sprint P3: os tiles principais de bioma têm
//    bordas com fundo transparente — refletem o que está embaixo — então sem esse "underlay"
//    a transição entre tiers cria vazios). Tier posterior ao ativo fica nulo ali.
// 2) PROFUNDIDADE de aninhamento DENTRO do tier ativo (Dirt/Pedra/Água sobre a grama daquele
//    tier, igual Sprint P2) — usa 2 Tilemaps COMPARTILHADOS entre todos os tiers
//    (`patchesTilemap`/`nestedTilemap`, Layer_Patches/Layer_Nested) porque só 1 tier está
//    ativo por célula, nunca disputam a mesma célula entre si. `baseVariantTilemap`
//    (Layer_GrassVariants) é igualmente compartilhado, mesma lógica da Sprint P2. Nenhum dos
//    3 é pintado numa célula de FRONTEIRA entre 2 tiers (`IsTierBorderCell`) — outro achado do
//    usuário: patch/nested/variante foram desenhados assumindo vizinhos do mesmo bioma pai, e
//    ficam estranhos bem na transição entre tiers diferentes.
public class ChunkTerrainGenerator : MonoBehaviour
{
    public WorldGenerationSettings settings;

    [Tooltip("1 BiomeProfileSO por grupo de tier, na ordem: [0]=Tier1, [1]=Tier2-3, [2]=Tier4-5, [3]=Tier6-8, [4]=Tier9, [5]=Tier10. Enquanto um slot estiver vazio, aquele grupo cai de volta pro Tier1 (tierBiomes[0]) pra não deixar buraco no mapa.")]
    public BiomeProfileSO[] tierBiomes = new BiomeProfileSO[WorldTierSelector.TierGroupCount];

    [Tooltip("1 Tilemap por grupo de tier (Layer0..Layer5 da cena, Sorting Order -5..0), mesma ordem de tierBiomes.")]
    public Tilemap[] tierBaseTilemaps = new Tilemap[WorldTierSelector.TierGroupCount];

    [Tooltip("Layer_Patches da cena (Sorting Order 2) — compartilhado entre todos os tiers, pinta o patch de profundidade 1 (Dirt/Pedra/Água) do tier ATIVO naquela célula.")]
    public Tilemap patchesTilemap;

    [Tooltip("Layer_Nested da cena (Sorting Order 3) — compartilhado, profundidade 2 (ex.: DarkSoil dentro de Dirt) do tier ativo naquela célula.")]
    public Tilemap nestedTilemap;

    [Tooltip("Layer_GrassVariants da cena (Sorting Order 1) — compartilhado, variantes de terreno-base (BiomeProfileSO.baseVariants) do tier ativo. Opcional — null = sem variantes.")]
    public Tilemap baseVariantTilemap;

    public Transform propContainer;

    [Header("Debug / Teste (Sprint P3)")]
    [Tooltip("Se marcado, ignora o Dia real da run (MainMenuUI.CurrentRun.day) e usa testDayOverride no lugar — só pra testar a geração de qualquer dia sem precisar jogar até lá.")]
    public bool useTestDayOverride;
    [Range(1, 30)]
    public int testDayOverride = 1;

    private readonly HashSet<ChunkCoord> paintedChunks = new();
    private readonly Dictionary<ChunkCoord, List<GameObject>> spawnedProps = new();

    // Acumulado por chunk em SpawnProps — reaproveitado entre chamadas, limpo no início de
    // cada uma (ver TooCloseToPlacedProp).
    private readonly List<(Vector2 pos, Vector2 halfExtent)> placedPropFootprintsThisChunk = new();

    // Fila de 1 chunk por vez, processada pela coroutine ProcessPipeline — espalha as chamadas
    // de SetTilesBlock de 1 ÚNICO chunk por VÁRIOS frames (1 chamada por frame), não só os
    // chunks entre si. O jogador nunca chega perto o bastante, rápido o bastante, pra notar
    // esse atraso (ideia do usuário, Sprint P2) — o chunk sempre termina de pintar bem antes
    // de ficar visível de perto.
    private readonly Queue<ChunkCoord> pendingPaint = new();
    private readonly Queue<ChunkCoord> pendingClear = new();
    private readonly HashSet<ChunkCoord> queuedChunks = new(); // evita enfileirar o mesmo 2x enquanto ele ainda está na fila/sendo processado

    private float seedOffsetX;
    private float seedOffsetY;

    // Buffers reutilizados entre chunks (pintura/limpeza em lote via SetTilesBlock, não 1
    // SetTile por célula — zero alocação nova por célula/chunk, lição da Sprint P2).
    private TileBase[][] tierBaseBuffer; // 1 por grupo de tier
    private TileBase[] patchesBuffer;
    private TileBase[] nestedBuffer;
    private TileBase[] variantBuffer;
    private TileBase[] clearBuffer; // todo null — reaproveitado pra limpar qualquer Tilemap
    private TerrainDefinitionSO[] stackBuffer; // [0]=base do tier ativo, [1]=patch, [2]=nested
    private TerrainDefinitionSO[] propFootprintStackBuffer; // mesma forma de stackBuffer, uso exclusivo de CanPlacePropAt (nunca ao mesmo tempo que o outro — pintura de célula termina antes de SpawnProps rodar)
    private float[] tierGroupPercentsBuffer;
    private readonly List<ChunkCoord> toClearBuffer = new();

    private int ActualDay => useTestDayOverride ? testDayOverride : (MainMenuUI.CurrentRun?.day ?? 1);

    private void Awake()
    {
        int seed = settings != null ? settings.worldSeed : 0;
        // Offset derivado da seed, nunca de Random/Time — mesma seed, mesmo deslocamento, sempre.
        seedOffsetX = WorldHash.Hash2DFloat(1, 0, channel: 900, seed: seed) * 10000f;
        seedOffsetY = WorldHash.Hash2DFloat(0, 1, channel: 900, seed: seed) * 10000f;

        int cellCount = settings != null ? settings.chunkSize * settings.chunkSize : 0;
        tierBaseBuffer = new TileBase[WorldTierSelector.TierGroupCount][];
        for (int g = 0; g < WorldTierSelector.TierGroupCount; g++) tierBaseBuffer[g] = new TileBase[cellCount];
        patchesBuffer = new TileBase[cellCount];
        nestedBuffer = new TileBase[cellCount];
        if (baseVariantTilemap != null) variantBuffer = new TileBase[cellCount];
        clearBuffer = new TileBase[cellCount];
        stackBuffer = new TerrainDefinitionSO[3];
        propFootprintStackBuffer = new TerrainDefinitionSO[3];
        tierGroupPercentsBuffer = new float[WorldTierSelector.TierGroupCount];

        StartCoroutine(ProcessPipeline());
    }

    private void Update()
    {
        if (ChunkStreamer.Instance == null || settings == null) return;

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

    // Chunks que já passaram por QUALQUER etapa de um regenerate anterior — usado só pra
    // limpar tudo que pode ter sobrado de um clique interrompido (ver bug abaixo). Reaproveitado
    // entre chamadas, nunca alocado de novo.
    private readonly HashSet<ChunkCoord> regenerateClearScratch = new();

    // 🔴 DEBUG (Sprint P3) — limpa o mundo inteiro já pintado e manda repintar tudo com o Dia
    // atual (real ou testDayOverride). Clica com o botão direito no componente no Inspector
    // (Play Mode) pra rodar. Não precisa reiniciar o jogo pra testar um dia diferente.
    //
    // [CORRIGIDO] Bug real relatado pelo usuário: clicar de novo RÁPIDO (antes do repaint
    // anterior terminar) deixava terreno de dias antigos preso no mapa por vários dias de
    // teste seguintes. Causa: `PaintChunkIncremental` é uma coroutine que espalha os até 9
    // `SetTilesBlock` (6 tiers + patches + nested + variant) por VÁRIOS FRAMES, e só marcava o
    // chunk como pintado (`paintedChunks.Add`) no fim. `StopAllCoroutines()` no clique seguinte
    // interrompia esse chunk no MEIO do repaint — ele nunca entrava em `paintedChunks`, então o
    // próximo clique não limpava ele antes de repintar, deixando uma mistura de tiles de 2+ dias
    // diferentes na mesma célula até alguma vez o pipeline completo rodar sem ser interrompido.
    // Fix: o botão agora limpa TUDO que pode ter sobrado (pintado, na fila, ou ativo no
    // ChunkStreamer) e repinta tudo de forma SÍNCRONA (`PaintChunkImmediate`, sem yield) — o
    // método inteiro termina atômico, nunca fica "pela metade" nem é interrompido por outro clique.
    [ContextMenu("Regenerar Mundo (Day Override)")]
    public void RegenerateWorld()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[ChunkTerrainGenerator] \"Regenerar Mundo\" só funciona em Play Mode.");
            return;
        }

        StopAllCoroutines();
        pendingPaint.Clear();
        pendingClear.Clear();

        // Limpa TUDO que pode ter sobrado de um clique anterior interrompido — não só
        // `paintedChunks` (que só reflete chunks que terminaram de pintar), mas também qualquer
        // coisa que estava na fila ou ativa no streamer, mesmo que nunca tenha sido marcada como
        // pintada.
        regenerateClearScratch.Clear();
        foreach (var coord in paintedChunks) regenerateClearScratch.Add(coord);
        foreach (var coord in queuedChunks) regenerateClearScratch.Add(coord);
        if (ChunkStreamer.Instance != null)
        {
            foreach (var coord in ChunkStreamer.Instance.ActiveChunks.Keys) regenerateClearScratch.Add(coord);
        }
        foreach (var coord in regenerateClearScratch)
        {
            ClearChunkImmediate(coord);
        }

        paintedChunks.Clear();
        queuedChunks.Clear();

        // Repintura 100% síncrona (sem coroutine) — termina inteira antes do método retornar,
        // então um clique rápido demais no botão nunca mais interrompe no meio.
        if (ChunkStreamer.Instance != null)
        {
            foreach (var coord in ChunkStreamer.Instance.ActiveChunks.Keys)
            {
                PaintChunkImmediate(coord);
            }
        }

        StartCoroutine(ProcessPipeline()); // volta a processar streaming normal (novos chunks futuros)

        Debug.Log($"[ChunkTerrainGenerator] Mundo regenerado — Dia {ActualDay}{(useTestDayOverride ? " (override)" : "")}.");
    }

    // Processa 1 chunk de cada vez, do início ao fim, antes de passar pro próximo — os buffers
    // são reaproveitados entre chunks, então só é seguro ter 1 chunk "em voo" na pipeline por
    // vez (sem concorrência entre coroutines).
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

    // Qual grupo de tier (0..5) está ativo na célula global (cellX,cellY), pro Dia informado.
    // Reaproveitado tanto pela pintura de terreno quanto pelo spawn de props (cada um precisa
    // saber de qual tier puxar os assets certos naquela posição específica).
    private int ActiveTierGroupAt(int cellX, int cellY, int actualDay)
    {
        float distanceFromCenter = Mathf.Sqrt((float)cellX * cellX + (float)cellY * cellY);
        int effectiveDay = WorldTierSelector.ComputeEffectiveSpawnDay(
            distanceFromCenter, settings.safeZoneRadius, settings.distancePerSpawnDay, actualDay);

        var dayDef = settings.daySpawnTable != null ? settings.daySpawnTable.GetDay(effectiveDay) : null;
        WorldTierSelector.ComputeTierGroupPercentsNonAlloc(dayDef, tierGroupPercentsBuffer);

        return WorldTierSelector.ChooseActiveTierGroup(
            cellX, cellY, seedOffsetX, seedOffsetY, settings.tierBlendNoiseScale, tierGroupPercentsBuffer);
    }

    // tierBiomes[group] pode estar vazio enquanto o usuário ainda não criou o BiomeProfileSO
    // daquele tier — cai pro Tier1 (sempre deve existir) pra nunca deixar célula sem terreno.
    private BiomeProfileSO ResolveBiome(int tierGroup)
    {
        var biome = tierBiomes[tierGroup];
        return biome != null ? biome : tierBiomes[0];
    }

    // Vizinhança de Moore (8 direções) — se QUALQUER vizinho pertence a um tier diferente
    // desta célula, ela está na fronteira entre 2 biomas (achado do usuário, Sprint P3: patch/
    // nested/variante foram desenhados assumindo vizinhos do MESMO bioma — colocar um deles
    // bem na fronteira com outro tier fica estranho na maioria das vezes). Célula de fronteira
    // sempre pinta só o terreno-base puro, nunca patch/nested/variante.
    private bool IsTierBorderCell(int cellX, int cellY, int actualDay, int activeGroup)
    {
        for (int dy = -1; dy <= 1; dy++)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                if (dx == 0 && dy == 0) continue;
                if (ActiveTierGroupAt(cellX + dx, cellY + dy, actualDay) != activeGroup) return true;
            }
        }

        return false;
    }

    // Parte CPU-only (sem custo de Tilemap) — calcula tier ativo + pilha de terreno de cada
    // célula do chunk e enche os buffers inteiros de uma vez. Compartilhado entre a pintura
    // incremental (espalhada por frames, streaming normal) e a pintura imediata (debug,
    // `RegenerateWorld`) — a única diferença entre as duas é COMO os buffers resultantes são
    // aplicados nos Tilemaps (`SetTilesBlock` espalhado vs tudo de uma vez).
    private void ComputeChunkBuffers(ChunkCoord coord)
    {
        int size = settings.chunkSize;
        int originX = coord.X * size;
        int originY = coord.Y * size;
        int actualDay = ActualDay;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int cellX = originX + x;
                int cellY = originY + y;
                int index = y * size + x; // ordem esperada por SetTilesBlock: x varia mais rápido

                int activeGroup = ActiveTierGroupAt(cellX, cellY, actualDay);
                var activeBiome = ResolveBiome(activeGroup);
                bool isTierBorder = IsTierBorderCell(cellX, cellY, actualDay, activeGroup);

                int stackDepth;
                if (isTierBorder)
                {
                    // Fronteira entre 2 tiers — só terreno-base puro, nunca patch/nested (achado
                    // do usuário: a maioria dos patches foi desenhada só pro próprio bioma pai).
                    stackBuffer[0] = activeBiome.baseTerrain;
                    stackDepth = 1;
                }
                else
                {
                    stackDepth = TerrainSelector.ChooseTerrainStackNonAlloc(
                        cellX, cellY, seedOffsetX, seedOffsetY, activeBiome, stackBuffer, settings.safeZoneRadius);
                }

                // Tier ativo pinta seu próprio terreno-base; todo tier ANTERIOR (g < activeGroup)
                // também pinta seu terreno-base puro na mesma célula, "por baixo" — achado do
                // usuário: os tiles principais de bioma têm bordas com fundo transparente (eles
                // refletem o que está embaixo), então sem isso a transição criaria vazios. Tier
                // seguinte (g > activeGroup) fica nulo — não faz sentido mostrar um bioma "futuro".
                for (int g = 0; g < WorldTierSelector.TierGroupCount; g++)
                {
                    if (g == activeGroup)
                    {
                        tierBaseBuffer[g][index] = stackBuffer[0].tile;
                    }
                    else if (g < activeGroup)
                    {
                        var underlayBiome = ResolveBiome(g);
                        tierBaseBuffer[g][index] = underlayBiome.baseTerrain != null ? underlayBiome.baseTerrain.tile : null;
                    }
                    else
                    {
                        tierBaseBuffer[g][index] = null;
                    }
                }

                patchesBuffer[index] = stackDepth > 1 ? stackBuffer[1].tile : null;
                nestedBuffer[index] = stackDepth > 2 ? stackBuffer[2].tile : null;

                if (variantBuffer != null)
                {
                    // Só pinta variante em célula "pura" (pilha de tamanho 1 = nenhum
                    // patchTerrain reivindicou a célula) e fora da fronteira entre tiers.
                    var variant = (!isTierBorder && stackDepth == 1)
                        ? TerrainSelector.ChooseBaseVariant(cellX, cellY, seedOffsetX, seedOffsetY, activeBiome)
                        : null;
                    variantBuffer[index] = variant != null ? variant.tile : null;
                }
            }
        }
    }

    private IEnumerator PaintChunkIncremental(ChunkCoord coord)
    {
        int size = settings.chunkSize;
        int originX = coord.X * size;
        int originY = coord.Y * size;
        int actualDay = ActualDay;

        ComputeChunkBuffers(coord);

        var bounds = new BoundsInt(originX, originY, 0, size, size, 1);
        for (int g = 0; g < WorldTierSelector.TierGroupCount; g++)
        {
            if (tierBaseTilemaps[g] == null) continue;
            tierBaseTilemaps[g].SetTilesBlock(bounds, tierBaseBuffer[g]);
            yield return null; // 1 SetTilesBlock por frame
        }
        if (patchesTilemap != null)
        {
            patchesTilemap.SetTilesBlock(bounds, patchesBuffer);
            yield return null;
        }
        if (nestedTilemap != null)
        {
            nestedTilemap.SetTilesBlock(bounds, nestedBuffer);
            yield return null;
        }
        if (baseVariantTilemap != null)
        {
            baseVariantTilemap.SetTilesBlock(bounds, variantBuffer);
            yield return null;
        }

        SpawnProps(coord, originX, originY, size, actualDay);

        paintedChunks.Add(coord);
        queuedChunks.Remove(coord);
    }

    // Igual a `PaintChunkIncremental`, mas TUDO de uma vez, sem yield — só pro botão de debug
    // `RegenerateWorld`. Ali um pequeno congelamento de frame é aceitável (não é streaming de
    // gameplay real) e, em troca, o método nunca fica pela metade se o usuário clicar de novo
    // rápido (bug real corrigido nesta rodada — ver comentário em `RegenerateWorld`).
    private void PaintChunkImmediate(ChunkCoord coord)
    {
        int size = settings.chunkSize;
        int originX = coord.X * size;
        int originY = coord.Y * size;
        int actualDay = ActualDay;

        ComputeChunkBuffers(coord);

        var bounds = new BoundsInt(originX, originY, 0, size, size, 1);
        for (int g = 0; g < WorldTierSelector.TierGroupCount; g++)
        {
            if (tierBaseTilemaps[g] == null) continue;
            tierBaseTilemaps[g].SetTilesBlock(bounds, tierBaseBuffer[g]);
        }
        if (patchesTilemap != null) patchesTilemap.SetTilesBlock(bounds, patchesBuffer);
        if (nestedTilemap != null) nestedTilemap.SetTilesBlock(bounds, nestedBuffer);
        if (baseVariantTilemap != null) baseVariantTilemap.SetTilesBlock(bounds, variantBuffer);

        SpawnProps(coord, originX, originY, size, actualDay);

        paintedChunks.Add(coord);
    }

    // Checa TODA célula do footprint do prop (não só o ponto onde ele nasce) contra o tier
    // ativo, os terrenos proibidos e a safe zone (achado do usuário: props grandes, ex.
    // Volcano, não podem só checar o centro — o resto do sprite podia ficar pendurado fora do
    // terreno certo, ou meio-dentro da safe zone mesmo com o ponto central fora dela).
    // footprintSize em CÉLULAS, centrado em `pos`.
    private bool CanPlacePropAt(Vector2 pos, EnvironmentPropSO prop, int tierGroup, int actualDay)
    {
        float halfX = prop.footprintSize.x * 0.5f;
        float halfY = prop.footprintSize.y * 0.5f;
        int minX = Mathf.FloorToInt(pos.x - halfX);
        int maxX = Mathf.FloorToInt(pos.x + halfX);
        int minY = Mathf.FloorToInt(pos.y - halfY);
        int maxY = Mathf.FloorToInt(pos.y + halfY);

        var biome = ResolveBiome(tierGroup);
        float safeZoneRadiusSqr = settings.safeZoneRadius * settings.safeZoneRadius;

        for (int cx = minX; cx <= maxX; cx++)
        {
            for (int cy = minY; cy <= maxY; cy++)
            {
                // Nenhum prop com collider pode ter QUALQUER parte do footprint dentro da safe
                // zone (não só o ponto central) — props sem collider (canSpawnInSafeZone=true)
                // ficam livres dessa checagem.
                if (!prop.canSpawnInSafeZone && settings.safeZoneRadius > 0f
                    && (float)cx * cx + (float)cy * cy <= safeZoneRadiusSqr)
                {
                    return false;
                }

                // Chunks ficam na fronteira entre 2+ tiers — um prop do Tier4-5 não pode
                // aparecer (nem parcialmente) numa célula que, naquele ponto, já é outro tier.
                if (ActiveTierGroupAt(cx, cy, actualDay) != tierGroup) return false;

                if (prop.forbiddenTerrains != null && prop.forbiddenTerrains.Count > 0)
                {
                    int depth = TerrainSelector.ChooseTerrainStackNonAlloc(
                        cx, cy, seedOffsetX, seedOffsetY, biome, propFootprintStackBuffer, settings.safeZoneRadius);
                    var surfaceTerrain = propFootprintStackBuffer[depth - 1]; // terreno de cima de verdade (o que está pintado na tela ali)
                    if (prop.forbiddenTerrains.Contains(surfaceTerrain)) return false;

                    // avoidTerrainEdges: além da própria célula, os 8 vizinhos TAMBÉM não podem
                    // ter superfície proibida — exclui a borda do terreno permitido (ex.: Bubble
                    // só no meio da lava, nunca onde ela encosta em Ashes/Obsidian).
                    if (prop.avoidTerrainEdges)
                    {
                        for (int ndx = -1; ndx <= 1; ndx++)
                        {
                            for (int ndy = -1; ndy <= 1; ndy++)
                            {
                                if (ndx == 0 && ndy == 0) continue;

                                int ndepth = TerrainSelector.ChooseTerrainStackNonAlloc(
                                    cx + ndx, cy + ndy, seedOffsetX, seedOffsetY, biome, propFootprintStackBuffer, settings.safeZoneRadius);
                                var neighborSurface = propFootprintStackBuffer[ndepth - 1];
                                if (prop.forbiddenTerrains.Contains(neighborSurface)) return false;
                            }
                        }
                    }
                }
            }
        }

        return true;
    }

    // Checa se `pos` (com half-extent `halfExtent`, metade do footprintSize) fica muito perto
    // de QUALQUER prop já aceito neste chunk, de QUALQUER EnvironmentPropSO — não só do mesmo
    // tipo (achado do usuário: cacto nascendo em cima de pedra, 2 props diferentes). AABB de
    // cada um expandida pela margem settings.minPropSpacing; "muito perto" = as 2 AABBs
    // expandidas se sobrepõem.
    private bool TooCloseToPlacedProp(Vector2 pos, Vector2 halfExtent)
    {
        float margin = settings.minPropSpacing;
        foreach (var (otherPos, otherHalf) in placedPropFootprintsThisChunk)
        {
            float sumHalfX = halfExtent.x + otherHalf.x + margin;
            float sumHalfY = halfExtent.y + otherHalf.y + margin;
            if (Mathf.Abs(pos.x - otherPos.x) < sumHalfX && Mathf.Abs(pos.y - otherPos.y) < sumHalfY)
            {
                return true;
            }
        }

        return false;
    }

    private void SpawnProps(ChunkCoord coord, int originX, int originY, int size, int actualDay)
    {
        var placedThisChunk = new List<GameObject>();
        placedPropFootprintsThisChunk.Clear();

        for (int g = 0; g < WorldTierSelector.TierGroupCount; g++)
        {
            var tierBiome = tierBiomes[g];
            if (tierBiome == null) continue;

            foreach (var prop in tierBiome.props)
            {
                if (prop == null || prop.prefab == null) continue;

                // Salt por identidade do prop (ver PropPlacement) — sem isso, 2 props com o
                // mesmo minSpacing sorteiam a MESMA posição na mesma célula (bug real
                // encontrado pelo usuário: várias árvores empilhadas no mesmo ponto).
                int propSalt = WorldHash.HashString(prop.id);
                var positions = PropPlacement.GenerateCandidatePositions(
                    originX, originY, size, prop.minSpacing, prop.density, prop.jitter, settings.worldSeed, propSalt);

                Vector2 halfExtent = prop.footprintSize * 0.5f;

                foreach (var pos in positions)
                {
                    // Safe zone, terreno proibido e tier ativo são checados juntos, célula a
                    // célula do footprint real do prop (ver CanPlacePropAt).
                    if (!CanPlacePropAt(pos, prop, g, actualDay)) continue;

                    // Espaçamento mínimo global entre QUALQUER par de props (pedido explícito
                    // do usuário, >= 1 tile por padrão) — considera a área de cada um, não só
                    // o ponto central.
                    if (TooCloseToPlacedProp(pos, halfExtent)) continue;

                    var instance = Instantiate(prop.prefab, new Vector3(pos.x, pos.y, 0f), Quaternion.identity, propContainer);
                    placedThisChunk.Add(instance);
                    placedPropFootprintsThisChunk.Add((pos, halfExtent));
                }
            }
        }

        spawnedProps[coord] = placedThisChunk;
    }

    private IEnumerator ClearChunkIncremental(ChunkCoord coord)
    {
        var bounds = ChunkBounds(coord);

        foreach (var tilemap in tierBaseTilemaps)
        {
            if (tilemap == null) continue;
            tilemap.SetTilesBlock(bounds, clearBuffer);
            yield return null;
        }
        if (patchesTilemap != null)
        {
            patchesTilemap.SetTilesBlock(bounds, clearBuffer);
            yield return null;
        }
        if (nestedTilemap != null)
        {
            nestedTilemap.SetTilesBlock(bounds, clearBuffer);
            yield return null;
        }
        if (baseVariantTilemap != null)
        {
            baseVariantTilemap.SetTilesBlock(bounds, clearBuffer);
            yield return null;
        }

        DestroySpawnedProps(coord);

        paintedChunks.Remove(coord);
        queuedChunks.Remove(coord);
    }

    // Versão síncrona de ClearChunkIncremental, só pro botão de debug RegenerateWorld — ali
    // precisamos limpar TODOS os chunks já pintados de uma vez, antes de recomeçar a fila,
    // sem esperar frame a frame (isso só roda sob demanda no Editor, não todo frame do jogo).
    private void ClearChunkImmediate(ChunkCoord coord)
    {
        var bounds = ChunkBounds(coord);

        foreach (var tilemap in tierBaseTilemaps)
        {
            if (tilemap != null) tilemap.SetTilesBlock(bounds, clearBuffer);
        }
        if (patchesTilemap != null) patchesTilemap.SetTilesBlock(bounds, clearBuffer);
        if (nestedTilemap != null) nestedTilemap.SetTilesBlock(bounds, clearBuffer);
        if (baseVariantTilemap != null) baseVariantTilemap.SetTilesBlock(bounds, clearBuffer);

        DestroySpawnedProps(coord);
    }

    private BoundsInt ChunkBounds(ChunkCoord coord)
    {
        int size = settings.chunkSize;
        return new BoundsInt(coord.X * size, coord.Y * size, 0, size, size, 1);
    }

    private void DestroySpawnedProps(ChunkCoord coord)
    {
        if (!spawnedProps.TryGetValue(coord, out var props)) return;

        foreach (var instance in props)
        {
            if (instance != null) Destroy(instance);
        }
        spawnedProps.Remove(coord);
    }
}
