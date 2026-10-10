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

    // Mathf.FloorToInt (não truncamento) garante divisão correta também pra coordenadas de
    // mundo negativas (ex.: -1 em chunkSize 64 cai no chunk -1, não no chunk 0).
    public static ChunkCoord FromWorldPosition(Vector2 worldPosition, int chunkSize)
    {
        int x = Mathf.FloorToInt(worldPosition.x / chunkSize);
        int y = Mathf.FloorToInt(worldPosition.y / chunkSize);
        return new ChunkCoord(x, y);
    }

    public override bool Equals(object obj) => obj is ChunkCoord other && X == other.X && Y == other.Y;
    public override int GetHashCode() => (X, Y).GetHashCode();
    public override string ToString() => $"({X}, {Y})";

    public static bool operator ==(ChunkCoord a, ChunkCoord b) => a.Equals(b);
    public static bool operator !=(ChunkCoord a, ChunkCoord b) => !a.Equals(b);
}
