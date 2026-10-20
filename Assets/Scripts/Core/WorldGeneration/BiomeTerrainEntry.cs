using System;
using System.Collections.Generic;
using UnityEngine;

// Associa 1 TerrainDefinitionSO à sua máscara de ruído dentro de um bioma, mais os patches
// que podem nascer ANINHADOS dentro dele (1 grid mais fundo — docs/sprints/sprint-p2-...:
// "se há um bloco acima do outro, um novo grid é criado pra ele"). Exemplo: DryDirt é uma
// entrada de `BiomeProfileSO.patchTerrains` (profundidade 1); DarkSoil vive dentro de
// `nestedPatches` do DryDirt (profundidade 2) — só pode nascer onde já é DryDirt.
// coverageTarget e noiseScale são metas de geração, não garantia matemática local por célula.
[Serializable]
public class BiomeTerrainEntry
{
    public TerrainDefinitionSO terrain;

    [Range(0f, 1f)]
    [Tooltip("Meta aproximada de cobertura — não é garantia matemática local, é objetivo de geração.")]
    public float coverageTarget = 0.12f;

    [Tooltip("Quanto maior, mais rápido o padrão de manchas varia por célula — pequeno = manchas grandes e suaves, grande = manchas pequenas e irregulares.")]
    public float noiseScale = 0.08f;

    // [SerializeReference] (2026-10-20) — sem isso, o serializador padrão do Unity trata este
    // campo como profundidade infinita só por ser do MESMO tipo (BiomeTerrainEntry contendo
    // List<BiomeTerrainEntry>), e sempre loga "Serialization depth limit 10 exceeded" no
    // Console mesmo com os dados reais tendo só 1-2 níveis — SerializeReference serializa por
    // referência (tabela plana em vez de aninhado inline), o que resolve o aviso de vez.
    [Tooltip("Patches que podem nascer DENTRO deste terreno (1 grid mais fundo) — ex.: DarkSoil dentro de DryDirt.")]
    [SerializeReference]
    public List<BiomeTerrainEntry> nestedPatches = new();

    [Tooltip("Marca este terreno como perigoso (água, lava, futuros hazards) — nunca nasce dentro do raio de safe zone (WorldGenerationSettings.safeZoneRadius), nem no topo da árvore nem aninhado. Deixe desmarcado pra Grama/Dirt/Pedra e variações delas.")]
    public bool blockedInSafeZone = false;

    [Tooltip("Marque se o RuleTile deste terreno NÃO tem arte pra célula única/linha fina de 1 célula de largura (ex.: as 7 variantes de grama, que só têm 15 das 47 posições de vizinhança). Restringe a geração a só criar blocos 2x2 reais (TerrainSelector.HasFilledQuad) — funciona em patchTerrains, nestedPatches ou baseVariants, qualquer profundidade. Deixe desmarcado pra terrenos com arte completa (ex.: Dirt/Stone/Água atuais).")]
    public bool requiresFilledQuad = false;
}
