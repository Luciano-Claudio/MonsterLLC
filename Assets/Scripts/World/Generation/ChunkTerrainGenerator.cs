using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Pathfinding;

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

    [Tooltip("Tilemap dedicado SÓ a colisão (sem TilemapRenderer, invisível) — recebe uma cópia muda de qualquer célula cuja pilha de terreno ativa (base/patch/nested) tenha algum TerrainDefinitionSO.blocksMovement=true (água/void/lava/rainbow). Precisa de TilemapCollider2D + CompositeCollider2D nele, na layer Collision (ver AstarPath Obstacle Layer Mask). Opcional — null = terreno nunca bloqueia (só props continuam bloqueando).")]
    public Tilemap hazardCollisionTilemap;

    [Tooltip("Tile simples (NÃO o RuleTile original do terreno) usado em toda célula marcada como hazard — precisa ter colliderType=Grid (bloco sólido fixo por célula, sem regra de vizinhança nenhuma). Achado em teste: reusar o RuleTile original aqui gerava collider inconsistente, porque ele tentava casar vizinhança nesse layer isolado (sem o resto do contexto do bioma) e quase nunca batia com nenhuma regra dele.")]
    public TileBase hazardMarkerTile;

    public Transform propContainer;

    [Header("Debug / Teste (Sprint P3)")]
    [Tooltip("Se marcado, ignora o Dia real da run (MainMenuUI.CurrentRun.day) e usa testDayOverride no lugar — só pra testar a geração de qualquer dia sem precisar jogar até lá.")]
    public bool useTestDayOverride;
    [Range(1, 30)]
    public int testDayOverride = 1;

    [Tooltip("Se marcado, ignora a seed real da run (RunState.worldSeed, sorteada em RunCreation.CreateNewRun) e usa settings.worldSeed no lugar — só pra testar a mesma seed sempre, sem depender de qual run começou.")]
    public bool useTestWorldSeedOverride;

    [Header("Performance / Streaming (Sprint P4)")]
    [Tooltip("Orçamento de tempo real (milissegundos) de trabalho por frame durante o streaming NORMAL (chunk entrando no raio enquanto o jogador anda) — passado esse tempo, devolve o frame e continua no próximo. Início de jogo/Continuar e o botão \"Regenerar Mundo\" NÃO usam isso — continuam síncronos/rápidos de propósito (eventos pontuais, não streaming contínuo). docs/new/Plano_Performance_Streaming_Chunks.md, item 4.")]
    public float streamingMsBudgetPerFrame = 2f;

    [Tooltip("Distância mínima (unidades de mundo) que o alvo de streaming precisa ter se movido desde o frame anterior pra contar como \"andando numa direção\" e disparar a pré-busca preditiva (item 6) — abaixo disso, considera parado/girando no lugar e não antecipa nada.")]
    public float predictiveMinMoveDelta = 0.01f;

    [Header("Pathfinding A* (Sprint P4, prep pro spawn de monstro)")]
    [Tooltip("Nome EXATO do GridGraph configurado no painel do AstarPath pro mundo aberto (1 grafo só, agora de tamanho MODERADO — ver openWorldGraphMover abaixo — Scan on Startup DESLIGADO — este script faz o Scan inicial paceado e depois só sincroniza incrementalmente). NUNCA os GridGraphs antigos de Floor (sistema de dungeon legado, Sprint 20) — esses continuam intocados.")]
    public string openWorldGraphName = "OpenWorld";

    [Tooltip("Componente ProceduralGraphMover (pacote A* Pathfinding Project) que mantém o GridGraph OpenWorld deslizando em volta do herói — achado do usuário: um grafo de tamanho fixo (mesmo no máximo de 1024 nós) tem um teto que um jogador consegue alcançar andando por tempo suficiente (ex.: com a duração do dia subindo de 100 pra 300s). Opcional — null = grafo continua fixo na origem (comportamento antigo), só funciona bem dentro do raio que o tamanho fixo cobrir.")]
    public ProceduralGraphMover openWorldGraphMover;

    private GridGraph openWorldGraph;
    private bool openWorldGraphResolved;

    private readonly HashSet<ChunkCoord> paintedChunks = new();
    private readonly Dictionary<ChunkCoord, List<GameObject>> spawnedProps = new();

    // [Perf, item 5] Chunks que já saíram do raio de streaming pelo menos 1x — ficam com os
    // tiles do Tilemap intactos (não custam nada relevante fora de tela, Unity já faz culling)
    // e os props só OCULTOS (SetActive(false), nunca Destroy). Volta pro raio → só reativa os
    // props, sem repintar nada. Sem limite/eviction — reset "de verdade" (Destroy + repintura
    // completa) só acontece no botão "Regenerar Mundo" (serve de fronteira de dia pra teste até
    // a Loja real existir — ver docs/new/Plano_Performance_Streaming_Chunks.md).
    private readonly HashSet<ChunkCoord> hiddenChunks = new();

    // Acumulado por chunk em SpawnProps — reaproveitado entre chamadas, limpo no início de
    // cada uma (ver TooCloseToPlacedProp).
    private readonly List<(Vector2 pos, Vector2 halfExtent)> placedPropFootprintsThisChunk = new();

    // Fila de 1 chunk por vez, processada pela coroutine ProcessPipeline — espalha as chamadas
    // de SetTilesBlock de 1 ÚNICO chunk por VÁRIOS frames (1 chamada por frame), não só os
    // chunks entre si. O jogador nunca chega perto o bastante, rápido o bastante, pra notar
    // esse atraso (ideia do usuário, Sprint P2) — o chunk sempre termina de pintar bem antes
    // de ficar visível de perto.
    private readonly Queue<ChunkCoord> pendingPaint = new();
    private readonly HashSet<ChunkCoord> queuedChunks = new(); // evita enfileirar o mesmo 2x enquanto ele ainda está na fila/sendo processado

    private float seedOffsetX;
    private float seedOffsetY;

    // Buffers reutilizados entre chunks (pintura/limpeza em lote via SetTilesBlock, não 1
    // SetTile por célula — zero alocação nova por célula/chunk, lição da Sprint P2).
    private TileBase[][] tierBaseBuffer; // 1 por grupo de tier
    private TileBase[] patchesBuffer;
    private TileBase[] nestedBuffer;
    private TileBase[] variantBuffer;
    private TileBase[] hazardBuffer; // espelho mudo (hazardCollisionTilemap) das células com TerrainDefinitionSO.blocksMovement=true

    // Cache do CompositeCollider2D no mesmo GameObject do hazardCollisionTilemap — resolvido
    // 1x (lazy), reaproveitado em todo chunk. Usado por ForceHazardColliderRegeneration.
    private CompositeCollider2D hazardCollisionComposite;
    private bool hazardCollisionCompositeResolved;
    private TileBase[] clearBuffer; // todo null — reaproveitado pra limpar qualquer Tilemap
    private TerrainDefinitionSO[] stackBuffer; // [0]=base do tier ativo, [1]=patch, [2]=nested
    private TerrainDefinitionSO[] propFootprintStackBuffer; // mesma forma de stackBuffer, uso exclusivo de CanPlacePropAt (nunca ao mesmo tempo que o outro — pintura de célula termina antes de SpawnProps rodar)
    private float[] tierGroupPercentsBuffer;
    private readonly List<ChunkCoord> toClearBuffer = new();

    // [Perf] Tier ativo de cada célula do chunk + 1 célula de margem (halo), calculado 1x por
    // chunk em ComputeTierGroupHalo — ComputeChunkBuffers/IsTierBorderCellLocal só LEEM daqui
    // em vez de chamar ActiveTierGroupAt de novo pra cada vizinho (antes: até 9x por célula,
    // ~37 mil chamadas por chunk — causa raiz do lag relatado pelo usuário mesmo depois de
    // cortar 87,5% da densidade de prop, ver docs/new/Plano_Performance_Streaming_Chunks.md).
    // Tamanho (size+2)², margem de 1 célula pra IsTierBorderCellLocal alcançar os vizinhos da
    // borda do chunk sem sair do array.
    private int[] tierGroupHaloBuffer;

    // [Perf] Cache do EffectiveSpawnDay dentro da varredura do halo — ActiveTierGroupAtCached
    // só recalcula dayDef/ComputeTierGroupPercentsNonAlloc (a parte cara: scan de 30 dias +
    // loop de 6 grupos) quando o EffectiveSpawnDay muda de verdade. Dentro de 1 chunk de 64
    // células, isso quase sempre é o MESMO valor em toda célula (só muda perto de uma fronteira
    // exata de distancePerSpawnDay) — na prática reduz as até ~37 mil repetições desse cálculo
    // pra 1 ou 2 por chunk. int.MinValue garante que a 1ª célula de cada chunk sempre recalcula.
    private int haloEffectiveDayCache = int.MinValue;

    // [Perf] Quais dos 6 grupos de tier aparecem em QUALQUER célula do chunk atual — computado
    // junto com o halo, reaproveitado por SpawnProps pra pular a lista de props de um tier
    // inteira (sem gerar grade de candidatos cara) quando aquele tier nem existe no chunk.
    private bool[] tierPresentInChunkBuffer;

    // [Perf, item 6] Posição do alvo de streaming no frame anterior — usada só pra calcular a
    // direção de movimento (pré-busca preditiva). `hasLastStreamingTargetPos` é falso até o
    // 1º frame com alvo válido, pra não tratar "nunca medido" como "moveu de (0,0)".
    private Vector2 lastStreamingTargetPos;
    private bool hasLastStreamingTargetPos;

    private int ActualDay => useTestDayOverride ? testDayOverride : (MainMenuUI.CurrentRun?.day ?? 1);

    // Tier Suppression (resto do P10) — decisão do usuário: suprimir um Tier remove também o
    // bioma visual do terreno, não só o monstro/boss (WorldPopulationManager/
    // WorldBossSpawnManager já respeitavam isso, o terreno não respeitava até agora).
    private int SuppressedTierCount => MainMenuUI.CurrentRun?.suppressedTierCount ?? 0;

    // RunState.worldSeed é a fonte de verdade (Sprint P10) — settings.worldSeed vira só o
    // fallback de teste (useTestWorldSeedOverride) ou pra quando não existe run nenhuma ainda
    // (ex.: Editor fora do Play mode).
    private int EffectiveWorldSeed
    {
        get
        {
            if (!useTestWorldSeedOverride && MainMenuUI.CurrentRun != null) return MainMenuUI.CurrentRun.worldSeed;
            return settings != null ? settings.worldSeed : 0;
        }
    }

    // Achado real (timing): Awake() roda assim que a cena carrega, ANTES do jogador clicar
    // "Novo Jogo"/"Continuar" no MainMenuUI — nesse instante MainMenuUI.CurrentRun ainda é
    // null, então EffectiveWorldSeed cairia sempre no fallback (settings.worldSeed) mesmo
    // numa run real. Na prática isso é inofensivo aqui (nenhum chunk é pintado antes do herói
    // nascer — ChunkStreamer.RefreshAroundTarget só roda dentro de HandleHeroSpawned, então a
    // fila do ProcessPipeline iniciado abaixo fica vazia até lá), mas o offset precisa ser
    // RECALCULADO em HandleHeroSpawned (ver lá), já com a seed certa da run, antes do primeiro
    // chunk pintar de verdade.
    private void RecomputeSeedOffsets()
    {
        int seed = EffectiveWorldSeed;
        // Offset derivado da seed, nunca de Random/Time — mesma seed, mesmo deslocamento, sempre.
        seedOffsetX = WorldHash.Hash2DFloat(1, 0, channel: 900, seed: seed) * 10000f;
        seedOffsetY = WorldHash.Hash2DFloat(0, 1, channel: 900, seed: seed) * 10000f;
    }

    private void Awake()
    {
        RecomputeSeedOffsets();

        int cellCount = settings != null ? settings.chunkSize * settings.chunkSize : 0;
        tierBaseBuffer = new TileBase[WorldTierSelector.TierGroupCount][];
        for (int g = 0; g < WorldTierSelector.TierGroupCount; g++) tierBaseBuffer[g] = new TileBase[cellCount];
        patchesBuffer = new TileBase[cellCount];
        nestedBuffer = new TileBase[cellCount];
        if (baseVariantTilemap != null) variantBuffer = new TileBase[cellCount];
        if (hazardCollisionTilemap != null) hazardBuffer = new TileBase[cellCount];
        clearBuffer = new TileBase[cellCount];
        stackBuffer = new TerrainDefinitionSO[3];
        propFootprintStackBuffer = new TerrainDefinitionSO[3];
        tierGroupPercentsBuffer = new float[WorldTierSelector.TierGroupCount];

        int haloSize = settings != null ? settings.chunkSize + 2 : 0;
        tierGroupHaloBuffer = new int[haloSize * haloSize];
        tierPresentInChunkBuffer = new bool[WorldTierSelector.TierGroupCount];

        StartCoroutine(ProcessPipeline());
    }

    // Start() (não Awake()) de propósito — garante que TODO Awake() da cena já rodou,
    // inclusive o do AstarPath (que só preenche o `AstarPath.active` estático no próprio
    // Awake dele). Ordem de execução de script nunca foi definida entre os dois componentes;
    // depender de Start() em vez de Awake() evita essa corrida sem precisar configurar Script
    // Execution Order.
    private void Start()
    {
        StartCoroutine(ScanOpenWorldGraphAsync());
    }

    private void OnEnable() => GameEvents.OnHeroSpawned += HandleHeroSpawned;
    private void OnDisable() => GameEvents.OnHeroSpawned -= HandleHeroSpawned;

    // Resolve e cacheia o GridGraph do mundo aberto por NOME (1x) — nunca os GridGraphs
    // antigos de Floor (sistema de dungeon legado da Sprint 20, ainda presentes no AstarPath
    // da cena/`FloorSpawnUtility`, intocados). Se o grafo não existir (ainda não criado no
    // painel do AstarPath, ou nome digitado errado), avisa 1x e desiste silenciosamente nas
    // chamadas seguintes — pathfinding simplesmente não acompanha o terreno até isso ser
    // configurado, mas o resto do jogo (geração de terreno/props) continua funcionando normal.
    // Exposto estático pra qualquer sistema fora deste script (ex.: WorldSpawnUtility, Fase A
    // do spawn de monstro) reaproveitar o MESMO grafo já resolvido, sem duplicar a busca por
    // nome nem arriscar divergir se `openWorldGraphName` for alterado só aqui. Só 1
    // ChunkTerrainGenerator existe por cena (mesmo padrão de singleton informal já usado por
    // ChunkStreamer.Instance) — populado assim que ResolveOpenWorldGraph() rodar com sucesso
    // (já acontece cedo, dentro de Start()/ScanOpenWorldGraphAsync()).
    public static GridGraph OpenWorldGraph { get; private set; }

    private GridGraph ResolveOpenWorldGraph()
    {
        if (openWorldGraphResolved) return openWorldGraph;
        openWorldGraphResolved = true;

        if (AstarPath.active == null) return null;

        openWorldGraph = AstarPath.active.data.FindGraph(g => g.name == openWorldGraphName) as GridGraph;
        if (openWorldGraph == null)
        {
            Debug.LogWarning($"[ChunkTerrainGenerator] GridGraph '{openWorldGraphName}' não encontrado no AstarPath — configure 1 Grid Graph com esse nome exato no painel do AstarPath (Scan on Startup desligado). Até lá, o pathfinding não vai reconhecer terreno/props do mundo aberto.");
        }
        OpenWorldGraph = openWorldGraph;
        return openWorldGraph;
    }

    // [P4 prep] Scan inicial do GridGraph do mundo aberto — 1x por sessão, PACEADO por frame
    // (ScanAsync, não Scan() síncrono) porque o grafo tem tamanho fixo generoso (bem maior que
    // 1 chunk) e um Scan cheio de uma vez congelaria o início da run. Depois desse único scan,
    // NUNCA mais um Scan() completo — só UpdateGraphs incremental por chunk
    // (SyncPathfindingForChunk), conforme docs/new/Mudanca_Estrutural_Geracao_Procedural_
    // Evolucao_Mapa_30_Dias_V2_ILUSTRADO.md, item 18 ("atualização incremental de A*/GridGraph,
    // evitando scan completo por frame"). Roda independente do herói já ter nascido — o grafo
    // é ancorado na origem do mundo, não no jogador (ver decisão de escopo: tamanho fixo
    // generoso, sem recentralizar). As áreas ainda não pintadas simplesmente escaneiam como
    // "andáveis" por padrão (sem collider nenhum ainda) e corrigem sozinhas conforme cada
    // chunk termina de pintar.
    // Achado em teste real (usuário): o chunk CENTRAL (pintado sincronamente assim que o herói
    // nasce, ver HandleHeroSpawned) era o ÚNICO que nunca sincronizava colisão de água/lava
    // direito — porque o Scan inicial acima (360 mil nós, paceado) ainda podia estar em
    // andamento no exato momento em que o herói nasce, e um UpdateGraphs contra um grafo que
    // ainda não terminou de ser criado falha silenciosamente pra aquela região. Todo chunk
    // seguinte (jogador andando) já acontece bem depois do Scan ter terminado — só o 1º sofria.
    private bool openWorldGraphScanComplete;

    private IEnumerator ScanOpenWorldGraphAsync()
    {
        var graph = ResolveOpenWorldGraph();
        if (graph == null)
        {
            openWorldGraphScanComplete = true; // libera quem estiver esperando (SyncPathfindingForChunkDelayed) mesmo sem grafo, pra nunca travar pra sempre
            yield break;
        }

        foreach (var _ in AstarPath.active.ScanAsync(graph))
        {
            yield return null;
        }

        openWorldGraphScanComplete = true;
    }

    // Rescan INCREMENTAL (só a área deste chunk, nunca o grafo inteiro) do GridGraph do mundo
    // aberto — chamado sempre que o terreno/props de 1 chunk muda de estado (pintura inicial,
    // esconder ou reativar props). UpdateGraphs só recalcula a caminhabilidade dos nós que já
    // existem dentro do bounds (o Scan inicial acima já criou todos os nós do grafo inteiro de
    // uma vez) — por isso é barato o bastante pra rodar no mesmo ritmo do streaming normal,
    // sem reintroduzir nenhum congelamento. `GraphMask.FromGraph` restringe a atualização só a
    // ESTE grafo, nunca toca nos GridGraphs antigos de Floor.
    private void SyncPathfindingForChunk(ChunkCoord coord)
    {
        var graph = ResolveOpenWorldGraph();
        if (graph == null) return;

        StartCoroutine(SyncPathfindingForChunkDelayed(coord, graph));
    }

    // Espera o Scan inicial do grafo inteiro terminar (ver ScanOpenWorldGraphAsync) antes de
    // qualquer UpdateGraphs — era a causa real do chunk central nunca sincronizar. Também
    // espera fim de frame + 1 FixedUpdate (o TilemapCollider2D só processa os tiles novos do
    // SetTilesBlock depois disso) antes de forçar o CompositeCollider2D a regenerar o shape e
    // só então perguntar pro grafo.
    private IEnumerator SyncPathfindingForChunkDelayed(ChunkCoord coord, GridGraph graph)
    {
        while (!openWorldGraphScanComplete) yield return null;

        // Achado real do usuário: com o gizmo "Show Graphs" do AstarPath ligado (que deixa o
        // FPS bem mais baixo no Editor), o chunk central sincronizava certo; desligado (FPS
        // normal), falhava. Isso prova que a espera precisa ser por TEMPO REAL, não por
        // quantidade de frames — WaitForEndOfFrame + 1 WaitForFixedUpdate (tentativa anterior)
        // é tempo real curto demais com o jogo rodando liso. Os chunks normais (pintados
        // andando, via fila paceada) nunca tinham esse problema porque o próprio pacing já
        // espalha a pintura por vários frames reais antes da sincronização rodar — só o chunk
        // central, pintado inteiro num frame único (PaintChunkImmediate, ao herói nascer), não
        // tinha essa folga de graça.
        yield return new WaitForSeconds(0.5f);

        ForceHazardColliderRegeneration();

        int size = settings.chunkSize;
        var origin = coord.ToWorldOrigin(size);

        // Achado real (usuário): a margem extra sozinha (item anterior) só "consertou" a BORDA
        // do chunk central — de graça, por causa do vazamento da margem dos chunks VIZINHOS pra
        // dentro dele. O interior continuava quebrado porque o update do PRÓPRIO chunk central
        // nunca funcionava — e ele é o ÚNICO cujo centro de consulta cai EXATAMENTE em cima do
        // pivô (0,0) do GridGraph (mesmo ponto que o Center dele usa). Isso aponta pra um caso
        // de borda de arredondamento dentro do código fechado do pacote, bem nesse ponto exato.
        // Em vez de tentar caçar essa linha (não dá pra editar o pacote), desloca o CENTRO da
        // consulta por um valor pequeno e não-redondo (nunca exatamente 0,0) — sai desse ponto
        // especial pra qualquer chunk, sem mudar a área coberta de verdade (a margem generosa
        // abaixo garante que o chunk inteiro continua dentro do bounds mesmo deslocado).
        const float pivotEscapeOffset = 0.37f;
        const float boundsPadding = 8f;
        var center = new Vector3(
            origin.x + size * 0.5f + pivotEscapeOffset,
            origin.y + size * 0.5f + pivotEscapeOffset,
            0f);
        var guo = new GraphUpdateObject(new Bounds(center, new Vector3(size + boundsPadding, size + boundsPadding, 4f)));
        guo.graphMask = GraphMask.FromGraph(graph);
        AstarPath.active.UpdateGraphs(guo);
    }

    // Resolve (1x, lazy) e força o CompositeCollider2D do hazardCollisionTilemap a regenerar o
    // shape AGORA, de forma síncrona de verdade — GenerateGeometry() é a API feita exatamente
    // pra isso, em vez de confiar em quando o Unity decide processar a regeneração automática.
    private void ForceHazardColliderRegeneration()
    {
        if (hazardCollisionTilemap == null) return;

        if (!hazardCollisionCompositeResolved)
        {
            hazardCollisionCompositeResolved = true;
            hazardCollisionComposite = hazardCollisionTilemap.GetComponent<CompositeCollider2D>();
            if (hazardCollisionComposite == null)
            {
                Debug.LogWarning("[ChunkTerrainGenerator] hazardCollisionTilemap não tem CompositeCollider2D no mesmo GameObject — colisão de água/lava/void/rainbow pode ficar com atraso ou nunca sincronizar com o A*.");
            }
        }

        if (hazardCollisionComposite != null) hazardCollisionComposite.GenerateGeometry();
    }

    // [Perf, Sprint P4 — 2ª rodada, ideia do usuário] Em vez de pintar TODOS os ~25 chunks
    // iniciais de forma síncrona (1ª versão deste fix — ainda causava um congelamento notável
    // no início da run), pinta só o chunk que CONTÉM o herói (instantâneo, 1 chunk) e solta ele
    // pra andar na hora — os outros 24 entram na fila JÁ PACEADA do streaming normal (item 4),
    // mas em ORDEM DE ESPIRAL a partir do centro (não a ordem crua/arbitrária de iteração do
    // Dictionary) — do anel mais próximo pro mais distante, cada anel em sentido horário
    // começando pelo Norte. Combinado com a centralização da origem (ChunkCoord.ToWorldOrigin),
    // o herói nasce exatamente no centro geométrico da malha — o resultado é que, no momento
    // em que ele já consegue sair do próprio chunk andando, o anel imediatamente ao redor já
    // teve TEMPO de sobra pra terminar (paceado a poucos ms por frame), sem nenhum
    // congelamento perceptível.
    private void HandleHeroSpawned(Transform hero)
    {
        if (ChunkStreamer.Instance == null || settings == null) return;

        // Recalcula AQUI (não só em Awake) — MainMenuUI.CurrentRun só fica disponível depois
        // que o jogador clica "Novo Jogo"/"Continuar", bem depois de Awake() já ter rodado.
        // Este é o ponto certo: nenhum chunk foi pintado ainda (ver RecomputeSeedOffsets).
        RecomputeSeedOffsets();

        // ProceduralGraphMover.target não dá pra pré-configurar no Editor (o herói só existe a
        // partir do Instantiate em runtime) — seta aqui, mesmo padrão de evento já usado pro
        // resto do streaming. O próprio componente já lida com "grafo ainda não escaneado"
        // sozinho (só começa a mover quando graph.isScanned virar true), sem precisar esperar
        // nada explicitamente aqui.
        if (openWorldGraphMover != null) openWorldGraphMover.target = hero;

        // Garante ActiveChunks populado JÁ — não depende de quem rodou Update() primeiro neste
        // frame nem da ordem de subscription entre ChunkStreamer e este script no mesmo evento.
        ChunkStreamer.Instance.RefreshAroundTarget(hero.position);

        var center = ChunkCoord.FromWorldPosition(hero.position, settings.chunkSize);
        if (!paintedChunks.Contains(center)) PaintChunkImmediate(center);

        EnqueueSpiralAroundCenter(center, settings.streamingRadiusInChunks);
    }

    // Enfileira (fila normal, paceada) os chunks de cada anel ao redor de `center`, do raio 1
    // até `radius`, cada anel em sentido horário a partir do Norte — ver RingClockwiseFromNorth.
    // `ChunkStreamer.ActiveChunks.ContainsKey` é só uma trava de segurança (hoje a malha de
    // anéis bate exatamente com o quadrado que RefreshAroundTarget já ativou, mas não custa
    // nada confirmar em vez de assumir silenciosamente que os 2 nunca vão dessincronizar).
    private void EnqueueSpiralAroundCenter(ChunkCoord center, int radius)
    {
        var active = ChunkStreamer.Instance.ActiveChunks;

        for (int r = 1; r <= radius; r++)
        {
            foreach (var coord in RingClockwiseFromNorth(center, r))
            {
                if (!active.ContainsKey(coord)) continue;
                if (paintedChunks.Contains(coord) || queuedChunks.Contains(coord)) continue;
                pendingPaint.Enqueue(coord);
                queuedChunks.Add(coord);
            }
        }
    }

    // Percorre o perímetro do anel de distância de Chebyshev `r` ao redor de `center`, em
    // sentido horário, começando pela célula exatamente ao Norte (+Y) — ordem pedida pelo
    // usuário (N, NE, E, SE, S, SW, W, NW pro anel 1, mesmo padrão pros anéis maiores). 8r
    // coordenadas no total, sem repetir nenhuma.
    private static IEnumerable<ChunkCoord> RingClockwiseFromNorth(ChunkCoord center, int r)
    {
        int x = center.X;
        int y = center.Y;

        // Norte -> logo antes do Nordeste.
        for (int i = 0; i < r; i++) yield return new ChunkCoord(x + i, y + r);
        // Nordeste -> logo antes do Sudeste (desce pela borda direita).
        for (int i = 0; i < 2 * r; i++) yield return new ChunkCoord(x + r, y + r - i);
        // Sudeste -> logo antes do Sudoeste (vai pra esquerda pela borda de baixo).
        for (int i = 0; i < 2 * r; i++) yield return new ChunkCoord(x + r - i, y - r);
        // Sudoeste -> logo antes do Noroeste (sobe pela borda esquerda).
        for (int i = 0; i < 2 * r; i++) yield return new ChunkCoord(x - r, y - r + i);
        // Noroeste -> fecha o anel bem antes do Norte inicial (sem repeti-lo).
        for (int i = 0; i < r; i++) yield return new ChunkCoord(x - r + i, y + r);
    }

    // Pinta (síncrono, sem fila/pacing) todo chunk que o ChunkStreamer já considera ativo e
    // ainda não está pintado — usado só pelo botão de debug "Regenerar Mundo", que precisa
    // continuar 100% atômico (ver comentário em RegenerateWorld) — NÃO usado mais no início de
    // run normal (ver HandleHeroSpawned acima, que agora só pinta 1 chunk na hora e paceia o
    // resto).
    private void PaintAllActiveChunksImmediate()
    {
        if (ChunkStreamer.Instance == null) return;

        foreach (var coord in ChunkStreamer.Instance.ActiveChunks.Keys)
        {
            if (paintedChunks.Contains(coord)) continue;
            PaintChunkImmediate(coord);
        }
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
            else if (hiddenChunks.Remove(coord))
            {
                // [Perf, item 5] Já pintado, só estava oculto (saiu do raio antes) — reativa os
                // props na hora, sem repintar nada (Tilemap nunca foi limpo).
                ShowChunkProps(coord);
            }
        }

        EnqueuePredictiveChunk();

        // [Perf, item 5] Chunk pintado que saiu do raio vira OCULTO, nunca mais limpo/destruído
        // aqui — só o botão "Regenerar Mundo" faz reset de verdade (ver RegenerateWorld).
        toClearBuffer.Clear();
        foreach (var coord in paintedChunks)
        {
            if (!active.ContainsKey(coord) && !hiddenChunks.Contains(coord)) toClearBuffer.Add(coord);
        }
        foreach (var coord in toClearBuffer)
        {
            HideChunkProps(coord);
            hiddenChunks.Add(coord);
        }
    }

    // [Perf, item 6] Antecipa a entrada na fila de pintura do chunk 1 posição ALÉM do raio de
    // streaming, na direção que o alvo está se movendo — dá a folga que o pacing (item 4)
    // precisa pra terminar de pintar antes do jogador chegar de verdade. Reaproveita
    // DirectionUtility.SnapTo8Directions (mesma lógica já usada pra mira/direção de herói, só
    // pra decidir qual dos 8 vizinhos antecipar). Direção instável (parado, girando no lugar)
    // simplesmente não antecipa nada — a entrada normal do raio ainda cobre esses casos.
    private void EnqueuePredictiveChunk()
    {
        var target = ChunkStreamer.Instance.streamingTarget;
        if (target == null)
        {
            hasLastStreamingTargetPos = false;
            return;
        }

        Vector2 currentPos = target.position;
        if (!hasLastStreamingTargetPos)
        {
            lastStreamingTargetPos = currentPos;
            hasLastStreamingTargetPos = true;
            return;
        }

        Vector2 delta = currentPos - lastStreamingTargetPos;
        lastStreamingTargetPos = currentPos;

        if (delta.magnitude < predictiveMinMoveDelta) return; // parado/quase parado — não antecipa

        Vector2 snapped = DirectionUtility.SnapTo8Directions(delta);
        var centerChunk = ChunkCoord.FromWorldPosition(currentPos, settings.chunkSize);
        int reachX = settings.streamingRadiusInChunks + 1;
        int reachY = settings.streamingRadiusInChunks + 1;

        var predictCoord = new ChunkCoord(
            centerChunk.X + Mathf.RoundToInt(snapped.x) * reachX,
            centerChunk.Y + Mathf.RoundToInt(snapped.y) * reachY);

        if (!paintedChunks.Contains(predictCoord) && !queuedChunks.Contains(predictCoord))
        {
            pendingPaint.Enqueue(predictCoord);
            queuedChunks.Add(predictCoord);
        }
    }

    private void HideChunkProps(ChunkCoord coord)
    {
        if (!spawnedProps.TryGetValue(coord, out var props)) return;
        foreach (var instance in props)
        {
            if (instance != null) instance.SetActive(false);
        }
        SyncPathfindingForChunk(coord); // colliders de prop acabaram de desativar — grafo precisa saber
    }

    private void ShowChunkProps(ChunkCoord coord)
    {
        if (!spawnedProps.TryGetValue(coord, out var props)) return;
        foreach (var instance in props)
        {
            if (instance != null) instance.SetActive(true);
        }
        SyncPathfindingForChunk(coord); // colliders de prop acabaram de reativar — grafo precisa saber
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
    // Diagnóstico direto (Play Mode, clique direito no componente) — verifica a posição EXATA
    // de onde o herói está parado agora: qual chunk é, se já foi pintado/sincronizado, e o que
    // o grafo A* realmente acha daquele ponto específico (Walkable ou não). Mais confiável que
    // uma amostra de grade (CENSO), que pode "errar" uma poça pequena por sorte sem ser bug.
    [ContextMenu("Debug: Checar posição do herói no grafo A*")]
    public void DebugCheckHeroPositionInGraph()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[PathSync-Debug] Só funciona em Play Mode.");
            return;
        }

        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
        {
            Debug.LogWarning("[PathSync-Debug] Nenhum GameObject com tag \"Player\" encontrado — o herói já nasceu?");
            return;
        }

        Vector3 pos = playerObj.transform.position;
        var coord = ChunkCoord.FromWorldPosition(pos, settings.chunkSize);
        bool painted = paintedChunks.Contains(coord);
        bool queued = queuedChunks.Contains(coord);

        var graph = ResolveOpenWorldGraph();
        if (graph == null)
        {
            Debug.LogWarning($"[PathSync-Debug] herói em {pos}, chunk={coord}, painted={painted}, queued={queued} — grafo OpenWorld não resolvido.");
            return;
        }

        var constraint = NearestNodeConstraint.None;
        constraint.graphMask = GraphMask.FromGraph(graph);
        var info = AstarPath.active.GetNearest(pos, constraint);

        if (info.node == null)
        {
            Debug.LogWarning($"[PathSync-Debug] herói em {pos}, chunk={coord}, painted={painted}, queued={queued} — NENHUM nó encontrado nessa posição (fora do grafo?).");
            return;
        }

        Debug.Log($"[PathSync-Debug] herói em {pos}, chunk={coord}, painted={painted}, queued={queued}, nó mais próximo em {(Vector3)info.node.position}, Walkable={info.node.Walkable}, distância nó-herói={Vector3.Distance(pos, (Vector3)info.node.position):F2}");
    }

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

        // População inteira renasce do zero sob as regras atuais (ex.: Tier Suppression comprada
        // nesta sessão) — nunca é morte (WorldPopulationManager/WorldBossSpawnManager escutam e
        // só dão Destroy(), sem loot/kill).
        GameEvents.ClearAllEnemies();

        // Limpa TUDO que pode ter sobrado de um clique anterior interrompido — não só
        // `paintedChunks` (que só reflete chunks que terminaram de pintar, inclusive os
        // OCULTOS — item 5 nunca os remove desse set), mas também qualquer coisa que estava na
        // fila ou ativa no streamer, mesmo que nunca tenha sido marcada como pintada. Isso é a
        // fronteira de "dia" pra teste (ver item 5/Plano de Performance) — destrói até os props
        // ocultos (DestroySpawnedProps não liga pra SetActive, destrói de qualquer jeito).
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
        hiddenChunks.Clear();

        // Repintura 100% síncrona (sem coroutine) — termina inteira antes do método retornar,
        // então um clique rápido demais no botão nunca mais interrompe no meio.
        PaintAllActiveChunksImmediate();

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
        WorldTierSelector.ComputeTierGroupPercentsNonAlloc(dayDef, tierGroupPercentsBuffer, SuppressedTierCount);

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

    // Igual a ActiveTierGroupAt, mas só pra uso DENTRO de ComputeTierGroupHalo (varredura
    // sequencial de 1 chunk) — reaproveita tierGroupPercentsBuffer entre células enquanto o
    // EffectiveSpawnDay não mudar, pulando a parte cara (scan de 30 dias + loop de 6 grupos).
    // NUNCA chamar isso fora da varredura do halo (haloEffectiveDayCache é estado compartilhado
    // entre chamadas, só é seguro em sequência single-threaded dentro de 1 chunk).
    private int ActiveTierGroupAtCached(int cellX, int cellY, int actualDay)
    {
        float distanceFromCenter = Mathf.Sqrt((float)cellX * cellX + (float)cellY * cellY);
        int effectiveDay = WorldTierSelector.ComputeEffectiveSpawnDay(
            distanceFromCenter, settings.safeZoneRadius, settings.distancePerSpawnDay, actualDay);

        if (effectiveDay != haloEffectiveDayCache)
        {
            var dayDef = settings.daySpawnTable != null ? settings.daySpawnTable.GetDay(effectiveDay) : null;
            WorldTierSelector.ComputeTierGroupPercentsNonAlloc(dayDef, tierGroupPercentsBuffer, SuppressedTierCount);
            haloEffectiveDayCache = effectiveDay;
        }

        return WorldTierSelector.ChooseActiveTierGroup(
            cellX, cellY, seedOffsetX, seedOffsetY, settings.tierBlendNoiseScale, tierGroupPercentsBuffer);
    }

    // Preenche tierGroupHaloBuffer (chunk + 1 célula de margem) e tierPresentInChunkBuffer —
    // 1x por chunk, no início de ComputeChunkBuffers. `lx`/`ly` locais vão de -1 a `size`
    // (inclusive) pra cobrir a margem usada por IsTierBorderCellLocal nas bordas do chunk.
    private void ComputeTierGroupHalo(int originX, int originY, int size, int actualDay)
    {
        int halo = size + 2;
        haloEffectiveDayCache = int.MinValue; // força recálculo na 1ª célula deste chunk

        for (int g = 0; g < WorldTierSelector.TierGroupCount; g++) tierPresentInChunkBuffer[g] = false;

        for (int ly = -1; ly <= size; ly++)
        {
            for (int lx = -1; lx <= size; lx++)
            {
                int group = ActiveTierGroupAtCached(originX + lx, originY + ly, actualDay);
                tierGroupHaloBuffer[(ly + 1) * halo + (lx + 1)] = group;
                tierPresentInChunkBuffer[group] = true;
            }
        }
    }

    // Lê o tier ativo já calculado em ComputeTierGroupHalo — `lx`/`ly` locais (0..size-1 pra
    // célula "própria", -1..size pra vizinho de borda).
    private int GetHaloGroup(int lx, int ly, int size)
    {
        int halo = size + 2;
        return tierGroupHaloBuffer[(ly + 1) * halo + (lx + 1)];
    }

    // Vizinhança de Moore (8 direções) — se QUALQUER vizinho pertence a um tier diferente
    // desta célula, ela está na fronteira entre 2 biomas (achado do usuário, Sprint P3: patch/
    // nested/variante foram desenhados assumindo vizinhos do MESMO bioma — colocar um deles
    // bem na fronteira com outro tier fica estranho na maioria das vezes). Célula de fronteira
    // sempre pinta só o terreno-base puro, nunca patch/nested/variante. `lx`/`ly` LOCAIS
    // (0..size-1) — lê do halo (ComputeTierGroupHalo), não recalcula nada.
    private bool IsTierBorderCellLocal(int lx, int ly, int size, int activeGroup)
    {
        for (int dy = -1; dy <= 1; dy++)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                if (dx == 0 && dy == 0) continue;
                if (GetHaloGroup(lx + dx, ly + dy, size) != activeGroup) return true;
            }
        }

        return false;
    }

    // Resolve e escreve nos buffers (tierBaseBuffer/patchesBuffer/nestedBuffer/variantBuffer) o
    // conteúdo de 1 única célula local (x,y dentro do chunk, 0..size-1) — corpo que antes vivia
    // dentro do loop duplo de ComputeChunkBuffers, extraído pra ser reaproveitado tanto pela
    // versão síncrona quanto pela paceada (ComputeChunkBuffersCore) sem duplicar a lógica.
    private void ProcessCell(int originX, int originY, int size, int x, int y)
    {
        int cellX = originX + x;
        int cellY = originY + y;
        int index = y * size + x; // ordem esperada por SetTilesBlock: x varia mais rápido

        int activeGroup = GetHaloGroup(x, y, size);
        var activeBiome = ResolveBiome(activeGroup);
        bool isTierBorder = IsTierBorderCellLocal(x, y, size, activeGroup);

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

        if (hazardBuffer != null)
        {
            // Água/void/lava/rainbow — qualquer nível da pilha ATIVA desta célula (base/patch/
            // nested) marcado como blocksMovement vira `hazardMarkerTile` no Tilemap de colisão
            // dedicado (ver TerrainDefinitionSO.blocksMovement). NUNCA o RuleTile original do
            // terreno — achado em teste: ele tenta casar vizinhança nesse layer isolado (sem o
            // resto do contexto do bioma) e o collider sai inconsistente/ausente.
            // `hazardMarkerTile` é um Tile simples (colliderType=Grid), mesmo bloco sólido fixo
            // pra qualquer hazard — o conteúdo visual não importa de verdade (este Tilemap
            // nunca é renderizado).
            bool hazard = false;
            for (int s = 0; s < stackDepth; s++)
            {
                if (stackBuffer[s] != null && stackBuffer[s].blocksMovement)
                {
                    hazard = true;
                    break;
                }
            }
            hazardBuffer[index] = hazard ? hazardMarkerTile : null;
        }

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

    // Núcleo CPU-only (sem custo de Tilemap) compartilhado entre a versão síncrona
    // (`ComputeChunkBuffers`, usada por PaintChunkImmediate/RegenerateWorld — eventos pontuais,
    // espera curta aceitável) e a paceada (streaming normal, `PaintChunkIncremental`) — a única
    // diferença é que `paced=true` devolve o frame (`yield return null`) sempre que o trabalho
    // deste chunk já consumiu `streamingMsBudgetPerFrame` de tempo real, em vez de travar o
    // frame inteiro calculando as 4096 células de uma vez (achado do usuário, Sprint P4: isso
    // era a causa raiz do lag ao entrar num chunk novo andando).
    private IEnumerator ComputeChunkBuffersCore(ChunkCoord coord, bool paced)
    {
        int size = settings.chunkSize;
        var chunkOrigin = coord.ToWorldOrigin(size);
        int originX = chunkOrigin.x;
        int originY = chunkOrigin.y;
        int actualDay = ActualDay;

        // [Perf] 1 varredura só pro chunk inteiro + margem — ProcessCell/IsTierBorderCellLocal
        // só LEEM o resultado, nunca recalculam por célula. Não paceada de propósito: já é uma
        // passada só, muito mais barata que antes (sem os 9x), não vale a complexidade extra.
        ComputeTierGroupHalo(originX, originY, size, actualDay);

        var stopwatch = paced ? System.Diagnostics.Stopwatch.StartNew() : null;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                ProcessCell(originX, originY, size, x, y);

                if (paced && stopwatch.Elapsed.TotalMilliseconds >= streamingMsBudgetPerFrame)
                {
                    yield return null;
                    stopwatch.Restart();
                }
            }
        }
    }

    // Versão síncrona — drena ComputeChunkBuffersCore(paced:false) inteiro na hora (esse nunca
    // yielda de verdade, já que paced=false pula a checagem de tempo). Mantido com o mesmo nome
    // de antes pra não precisar mudar PaintChunkImmediate/RegenerateWorld.
    private void ComputeChunkBuffers(ChunkCoord coord)
    {
        var enumerator = ComputeChunkBuffersCore(coord, paced: false);
        while (enumerator.MoveNext()) { }
    }

    private IEnumerator PaintChunkIncremental(ChunkCoord coord)
    {
        int size = settings.chunkSize;
        var chunkOrigin = coord.ToWorldOrigin(size);
        int originX = chunkOrigin.x;
        int originY = chunkOrigin.y;
        int actualDay = ActualDay;

        // [Perf] Paceado por orçamento de ms, não mais síncrono — essa é a parte CPU-only que
        // era a causa raiz do lag ao entrar num chunk novo andando (ver comentário em
        // ComputeChunkBuffersCore). Só o streaming NORMAL usa paced:true.
        yield return StartCoroutine(ComputeChunkBuffersCore(coord, paced: true));

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
        if (hazardCollisionTilemap != null)
        {
            hazardCollisionTilemap.SetTilesBlock(bounds, hazardBuffer);
            yield return null;
        }

        yield return StartCoroutine(SpawnPropsCore(coord, originX, originY, size, actualDay, paced: true));

        SyncPathfindingForChunk(coord); // [P4 prep] terreno/props deste chunk no estado final — grafo pode sincronizar
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
        var chunkOrigin = coord.ToWorldOrigin(size);
        int originX = chunkOrigin.x;
        int originY = chunkOrigin.y;
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
        if (hazardCollisionTilemap != null) hazardCollisionTilemap.SetTilesBlock(bounds, hazardBuffer);

        SpawnProps(coord, originX, originY, size, actualDay);

        SyncPathfindingForChunk(coord); // [P4 prep] terreno/props deste chunk no estado final — grafo pode sincronizar
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

    // Núcleo compartilhado entre a versão síncrona (`SpawnProps`, usada por
    // PaintChunkImmediate/RegenerateWorld) e a paceada (streaming normal,
    // `PaintChunkIncremental`) — mesma lógica de sempre, só que `paced=true` devolve o frame
    // depois de `streamingMsBudgetPerFrame` de trabalho real (checado por prop, não por
    // posição — granularidade mais grossa, mas simples e já suficiente já que cada
    // EnvironmentPropSO processa um número modesto de candidatos).
    private IEnumerator SpawnPropsCore(ChunkCoord coord, int originX, int originY, int size, int actualDay, bool paced)
    {
        var placedThisChunk = new List<GameObject>();
        placedPropFootprintsThisChunk.Clear();

        var stopwatch = paced ? System.Diagnostics.Stopwatch.StartNew() : null;

        for (int g = 0; g < WorldTierSelector.TierGroupCount; g++)
        {
            var tierBiome = tierBiomes[g];
            if (tierBiome == null) continue;

            // [Perf] Tier nem existe em nenhuma célula deste chunk (buffer já calculado em
            // ComputeChunkBuffersCore/ComputeTierGroupHalo, chamado antes de SpawnProps na
            // mesma pintura) — pula a lista de props INTEIRA, sem gerar grade de candidatos
            // cara pra nenhum deles (podia ser até 71 props no Fire, cada um até ~1000 iterações).
            if (!tierPresentInChunkBuffer[g]) continue;

            foreach (var prop in tierBiome.props)
            {
                if (prop == null || prop.prefab == null) continue;

                // Salt por identidade do prop (ver PropPlacement) — sem isso, 2 props com o
                // mesmo minSpacing sorteiam a MESMA posição na mesma célula (bug real
                // encontrado pelo usuário: várias árvores empilhadas no mesmo ponto).
                int propSalt = WorldHash.HashString(prop.id);
                var positions = PropPlacement.GenerateCandidatePositions(
                    originX, originY, size, prop.minSpacing, prop.density, prop.jitter, EffectiveWorldSeed, propSalt);

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

                if (paced && stopwatch.Elapsed.TotalMilliseconds >= streamingMsBudgetPerFrame)
                {
                    yield return null;
                    stopwatch.Restart();
                }
            }
        }

        spawnedProps[coord] = placedThisChunk;
    }

    // Versão síncrona — drena SpawnPropsCore(paced:false) inteiro na hora. Mantido com o mesmo
    // nome de antes pra não precisar mudar PaintChunkImmediate/RegenerateWorld.
    private void SpawnProps(ChunkCoord coord, int originX, int originY, int size, int actualDay)
    {
        var enumerator = SpawnPropsCore(coord, originX, originY, size, actualDay, paced: false);
        while (enumerator.MoveNext()) { }
    }

    // Versão síncrona de limpeza, só pro botão de debug RegenerateWorld — ali
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
        if (hazardCollisionTilemap != null) hazardCollisionTilemap.SetTilesBlock(bounds, clearBuffer);

        DestroySpawnedProps(coord);
    }

    private BoundsInt ChunkBounds(ChunkCoord coord)
    {
        int size = settings.chunkSize;
        var origin = coord.ToWorldOrigin(size);
        return new BoundsInt(origin.x, origin.y, 0, size, size, 1);
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
