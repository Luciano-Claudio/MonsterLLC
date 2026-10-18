using UnityEngine;

// Primeiro ScriptableObject do projeto — decisão deliberada (docs/new/Mudanca_Estrutural_
// Geracao_Procedural..., Seção 23: "WorldGenerationSettingsSO") pra manter a configuração do
// mundo editável como asset, sem exigir recompilar código pra ajustar seed/tamanho de
// chunk/raio. Valores default são ilustrativos, não balanceamento final (mesmo documento,
// Seção 26: "não fixar sem teste").
[CreateAssetMenu(fileName = "WorldGenerationSettings", menuName = "MonsterLLC/World Generation Settings")]
public class WorldGenerationSettings : ScriptableObject
{
    [Tooltip("Sprint P10: a seed de verdade passou a morar em RunState.worldSeed (sorteada em RunCreation.CreateNewRun — cada jogo novo tem um mundo diferente). Este campo virou só o fallback de teste do Editor: usado quando ChunkTerrainGenerator/ChunkStreamer têm useTestWorldSeedOverride ligado, ou quando ainda não existe run nenhuma (fora do Play mode).")]
    public int worldSeed = 0;

    [Tooltip("Tamanho de cada chunk, em unidades de mundo. Ilustrativo (32x32 ou 64x64) — medir em protótipo antes de fechar.")]
    public int chunkSize = 64;

    [Tooltip("Quantos chunks de raio ficam ativos ao redor do alvo de streaming (normalmente o jogador).")]
    public int streamingRadiusInChunks = 2;

    [Tooltip("Margem extra de chunks consultados (mas não necessariamente ativados) pra resolver vizinhança/bordas sem depender de quem carregou primeiro (halo).")]
    public int haloInChunks = 1;

    [Tooltip("Raio (em células) ao redor da origem do mundo (0,0) — onde o herói nasce — sem terreno perigoso (BiomeTerrainEntry.blockedInSafeZone, ex.: água/lava) nem props. Pensado pra NPCs de posição fixa e pro herói nunca nascer em cima de água. 0 = desativado.")]
    public float safeZoneRadius = 12f;

    [Tooltip("DistancePerSpawnDay (docs/new/Mudanca_Estrutural_Mapa_Unico_Spawn_Bosses_ATUALIZADO_V2.md, Seção 8.2) — a cada tantas células além da safe zone, o EffectiveSpawnDay usado pra decidir o bioma sobe +1 (mapa simula \"dias futuros\" conforme o jogador se afasta do centro). Usado por WorldTierSelector.")]
    public float distancePerSpawnDay = 100f;

    [Tooltip("Tabela real de % de spawn por Tier/Dia/Período (Seção 5) — fonte de verdade de qual bioma deve dominar em cada EffectiveSpawnDay. Obrigatório pra WorldTierSelector funcionar.")]
    public DaySpawnTableSO daySpawnTable;

    [Tooltip("Escala do ruído usado pra decidir, célula a célula, qual dos 6 tiers de terreno está ativo (WorldTierSelector) — pequeno = manchas de transição grandes e suaves entre tiers, grande = manchas pequenas e irregulares.")]
    public float tierBlendNoiseScale = 0.02f;

    [Tooltip("Espaçamento mínimo (em unidades de mundo) entre QUALQUER par de props, mesmo de EnvironmentPropSO diferentes — considera a área de cada um (footprintSize), não só o ponto central. Default 1 = 8px no PPU=8 do projeto (1 tile), pedido explícito do usuário depois de props de tipos diferentes nascendo empilhados/colados (ex.: cacto em cima de pedra).")]
    public float minPropSpacing = 1f;
}
