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

    [Tooltip("Patches que podem nascer DENTRO deste terreno (1 grid mais fundo) — ex.: DarkSoil dentro de DryDirt.")]
    public List<BiomeTerrainEntry> nestedPatches = new();

    [Tooltip("Marca este terreno como perigoso (água, lava, futuros hazards) — nunca nasce dentro do raio de safe zone (WorldGenerationSettings.safeZoneRadius), nem no topo da árvore nem aninhado. Deixe desmarcado pra Grama/Dirt/Pedra e variações delas.")]
    public bool blockedInSafeZone = false;
}
