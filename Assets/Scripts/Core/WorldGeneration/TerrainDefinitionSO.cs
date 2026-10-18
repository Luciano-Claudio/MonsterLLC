using UnityEngine;
using UnityEngine.Tilemaps;

// Categoria de terreno (docs/new/Mudanca_Estrutural_Geracao_Procedural..., Seção 23:
// "TerrainDefinitionSO"). Modelo final descoberto na Sprint P2 (ver sprint report): cada
// terreno é 1 único Tile/AnimatedTile/RuleTile JÁ CONFIGURADO que resolve TUDO sozinho
// (interior + borda), pintado no seu PRÓPRIO grid/Tilemap (profundidade de aninhamento —
// ver BiomeTerrainEntry/TerrainSelector). Como cada profundidade tem seu próprio Tilemap,
// um RuleTile nunca "vê" um terreno aninhado mais fundo como vizinho (está em outro
// Tilemap) — por isso RuleTile comum (This/Not This) já basta, sem precisar saber o vizinho
// específico.
[CreateAssetMenu(fileName = "TerrainDefinition", menuName = "MonsterLLC/World Generation/Terrain Definition")]
public class TerrainDefinitionSO : ScriptableObject
{
    public string id;
    public TileBase tile;

    [Tooltip("Água/void/lava/rainbow — bloqueio sólido simples (sem dano, sem regra por estado do personagem; ver docs/new/Mudanca_Estrutural_Geracao_Procedural...md, item 11/seção 'Água, lava, paredes e colisores'). Pintado também no Tilemap de colisão dedicado (ChunkTerrainGenerator.hazardCollisionTilemap) sempre que este terreno aparecer na pilha de uma célula — os RuleTiles de água/lava/void/rainbow já têm colliderType=Sprite configurado, só faltava o Tilemap com TilemapCollider2D pra usar isso.")]
    public bool blocksMovement;
}
