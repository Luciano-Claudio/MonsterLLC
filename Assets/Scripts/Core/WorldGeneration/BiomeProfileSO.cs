using System.Collections.Generic;
using UnityEngine;

// Perfil de bioma (docs/new/Mudanca_Estrutural_Geracao_Procedural..., Seção 23:
// "BiomeProfileSO"). Modelo de grids aninhados (ver TerrainDefinitionSO/BiomeTerrainEntry):
// `baseTerrain` é o fundo do bioma (ex.: Grama — grid 0, sempre presente onde nenhum
// `patchTerrains` reivindicou a célula); `patchTerrains` são os terrenos de grid 1 (Água,
// DryDirt, Pedra...), cada um com sua própria máscara de ruído (primeiro que bater vence) e,
// opcionalmente, patches ANINHADOS dentro dele (grid 2+, via `BiomeTerrainEntry.nestedPatches`
// — ex.: DarkSoil só nasce dentro de onde já é DryDirt).
//
// `baseVariants` é um sistema SEPARADO (Sprint P2, confirmado com o usuário): variações do
// terreno-base (ex.: HighGrass/ThickGrass/SoftGrass) que só nascem em célula "pura" — onde
// nenhum `patchTerrains` reivindicou nada. Não disputa a mesma célula que Dirt/Stone/Água;
// é pintado numa Tilemap própria (`ChunkTerrainGenerator.baseVariantTilemap`), separada da
// lista `layerTilemaps` por profundidade.
[CreateAssetMenu(fileName = "BiomeProfile", menuName = "MonsterLLC/World Generation/Biome Profile")]
public class BiomeProfileSO : ScriptableObject
{
    public string id;
    public TerrainDefinitionSO baseTerrain;
    public List<BiomeTerrainEntry> patchTerrains = new();
    public List<BiomeTerrainEntry> baseVariants = new();
    public List<EnvironmentPropSO> props = new();
}
