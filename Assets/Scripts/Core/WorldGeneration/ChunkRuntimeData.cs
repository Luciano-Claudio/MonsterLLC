// Dados de 1 chunk, derivados 100% de (WorldSeed, ChunkCoord) — nunca de estado externo ou
// da ordem em que o chunk foi carregado. Nesta sprint (P1 — Fundação) só expõe um hash
// "representativo" (DebugSeedValue), usado pra provar — em teste e visualmente — que o
// mesmo chunk sempre produz o mesmo resultado, qualquer que seja a ordem de carregamento.
// A Sprint P2 (Terreno Natural) estende isso pra máscaras de bioma/autotile reais, sem mudar
// a Fundação construída aqui.
public readonly struct ChunkRuntimeData
{
    public readonly ChunkCoord Coord;
    public readonly uint DebugSeedValue;

    public ChunkRuntimeData(ChunkCoord coord, int worldSeed)
    {
        Coord = coord;
        DebugSeedValue = WorldHash.Hash2D(coord.X, coord.Y, channel: 0, worldSeed);
    }
}
