using System.Collections.Generic;
using UnityEngine;

// Prefab de decoração posicionado por regra (docs/new/Mudanca_Estrutural_Geracao_
// Procedural..., Seção 13: "EnvironmentPropSO — prefab, footprint, terrain tags, density,
// min distance, sorting pivot"). O spawn de environments NÃO é o spawn de monstros — é só
// decoração; nenhum Collider de gameplay é assumido aqui (o Collider físico que bloqueia o
// herói, se houver, vive no PREFAB em si — ver HeroController.rb/Sprint P3 — não neste SO).
[CreateAssetMenu(fileName = "EnvironmentProp", menuName = "MonsterLLC/World Generation/Environment Prop")]
public class EnvironmentPropSO : ScriptableObject
{
    public string id;
    public GameObject prefab;

    [Tooltip("Espaçamento mínimo entre instâncias, em unidades de mundo — também define o passo da grade de candidatos.")]
    public float minSpacing = 4f;

    [Range(0f, 1f)]
    [Tooltip("Chance de cada ponto candidato da grade efetivamente receber uma instância.")]
    public float density = 0.3f;

    [Range(0f, 1f)]
    [Tooltip("Deslocamento determinístico aplicado a cada candidato dentro da própria célula da grade, pra não parecer um grid óbvio.")]
    public float jitter = 0.6f;

    [Tooltip("Terrenos (TerrainDefinitionSO) em que este prop NUNCA pode nascer por cima — ex.: árvore não nasce em cima de Água, Lava Bubble só nasce em cima de Lava (lista oposta: tudo que NÃO for Lava vira proibido). Checado em TODA célula do footprint, não só no ponto central (ver footprintSize).")]
    public List<TerrainDefinitionSO> forbiddenTerrains = new();

    [Tooltip("Tamanho do footprint real do prefab, em CÉLULAS de mundo (1 célula = 1 unidade) — não é só o ponto onde ele nasce, é toda a área que ele visualmente ocupa. Props grandes (ex.: Volcano) precisam disso maior que 1x1, senão só o centro é checado contra forbiddenTerrains/tier e o resto do prefab pode ficar pendurado fora do terreno certo. Default 1x1 = comportamento antigo (só o ponto central).")]
    public Vector2 footprintSize = Vector2.one;

    [Tooltip("Se false (padrão), este prop nunca nasce dentro da Safe Zone (raio ao redor da origem — WorldGenerationSettings.safeZoneRadius), igual o comportamento de sempre. Marque true pra props puramente decorativos que podem aparecer ali também (ex.: grama alta, nunca pedra/árvore grande que bloqueiam o herói nascendo).")]
    public bool canSpawnInSafeZone = false;

    [Tooltip("Se true, os 8 vizinhos de cada célula do footprint TAMBÉM precisam ter superfície permitida (fora de forbiddenTerrains) — ou seja, exclui a BORDA do terreno permitido, só aceita o interior dele. Pedido do usuário: Bubble só pode nascer no meio da lava, nunca na borda onde ela encosta em Ashes/Obsidian/etc.")]
    public bool avoidTerrainEdges = false;
}
