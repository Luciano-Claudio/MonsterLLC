using UnityEngine;

// Decide, de forma pura e determinística, se uma célula global é reivindicada por 1 terreno
// "de mancha" (BiomeTerrainEntry, via TerrainSelector) — sem nenhuma dependência de Tilemap/
// Scene, só matemática (docs/new/Mudanca_Estrutural_Geracao_Procedural..., Seção 8: "não
// sortear cada célula de modo independente... usar ruído coerente formando manchas"). Usa
// Mathf.PerlinNoise (puro, sem estado) em coordenadas GLOBAIS — é assim que 2 chunks vizinhos
// resolvem a mesma máscara lógica sem costura, nenhum dos dois sabe que "chunk" existe.
public static class TerrainMask
{
    public static bool ShouldPaintOverlay(int cellX, int cellY, float seedOffsetX, float seedOffsetY, float noiseScale, float coverageTarget)
    {
        float n = Mathf.PerlinNoise((cellX + seedOffsetX) * noiseScale, (cellY + seedOffsetY) * noiseScale);
        return n < coverageTarget;
    }
}
