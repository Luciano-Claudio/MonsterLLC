using UnityEngine;

// Identidade de um chunk no mundo único — puramente por coordenadas globais, nunca por
// ordem de carregamento (docs/new/Mudanca_Estrutural_Geracao_Procedural..., Seção 12:
// "Chaves e seeds derivadas de coordenadas globais, sem depender da ordem de carregamento").
public readonly struct ChunkCoord
{
    public readonly int X;
    public readonly int Y;

    public ChunkCoord(int x, int y)
    {
        X = x;
        Y = y;
    }

    // [Sprint P4] Chunk (0,0) fica CENTRADO na origem do mundo (0,0,0), não com o vértice ali
    // — achado do usuário em teste visual: a malha 5x5 de streaming ficava descentrada em
    // relação ao herói (origem caindo bem no canto compartilhado por 4 chunks, não no meio de
    // 1 só), quebrando a simetria radial do raio de streaming/safe zone. Deslocamento de meio
    // chunk (`HalfSize`) em cada eixo — ver `ToWorldOrigin` pra conversão inversa (chunk →
    // mundo). Isso NÃO afeta terreno/safe zone/EffectiveSpawnDay: esses sistemas usam sempre
    // coordenada de CÉLULA global (absoluta), nunca coordenada de chunk — só a fronteira entre
    // "qual chunk contém qual célula" se move, o conteúdo gerado em cada célula é idêntico.
    public static int HalfSize(int chunkSize) => chunkSize / 2;

    // Mathf.FloorToInt (não truncamento) garante divisão correta também pra coordenadas de
    // mundo negativas (ex.: um pouco abaixo do centro de chunk -1 continua caindo no chunk -1,
    // não no chunk 0).
    public static ChunkCoord FromWorldPosition(Vector2 worldPosition, int chunkSize)
    {
        int half = HalfSize(chunkSize);
        int x = Mathf.FloorToInt((worldPosition.x + half) / chunkSize);
        int y = Mathf.FloorToInt((worldPosition.y + half) / chunkSize);
        return new ChunkCoord(x, y);
    }

    // Inverso de FromWorldPosition — canto inferior-esquerdo do chunk em coordenadas de mundo
    // (ponto de partida pra `BoundsInt`/`SetTilesBlock`, que esperam o canto, não o centro).
    // Único lugar que faz essa conversão — qualquer código que precisar do "mundo de origem"
    // de um chunk deve chamar isto, nunca `coord.X * chunkSize` direto (isso voltaria a
    // ignorar o deslocamento de meio chunk).
    public Vector2Int ToWorldOrigin(int chunkSize)
    {
        int half = HalfSize(chunkSize);
        return new Vector2Int(X * chunkSize - half, Y * chunkSize - half);
    }

    public override bool Equals(object obj) => obj is ChunkCoord other && X == other.X && Y == other.Y;
    public override int GetHashCode() => (X, Y).GetHashCode();
    public override string ToString() => $"({X}, {Y})";

    public static bool operator ==(ChunkCoord a, ChunkCoord b) => a.Equals(b);
    public static bool operator !=(ChunkCoord a, ChunkCoord b) => !a.Equals(b);
}
