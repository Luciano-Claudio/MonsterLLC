using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class TerrainSelectorTests
{
    private BiomeProfileSO biome;
    private TerrainDefinitionSO grass;
    private TerrainDefinitionSO dryDirt;
    private TerrainDefinitionSO darkSoil;
    private TerrainDefinitionSO water;
    private TerrainDefinitionSO highGrass;

    [SetUp]
    public void SetUp()
    {
        grass = ScriptableObject.CreateInstance<TerrainDefinitionSO>();
        dryDirt = ScriptableObject.CreateInstance<TerrainDefinitionSO>();
        darkSoil = ScriptableObject.CreateInstance<TerrainDefinitionSO>();
        water = ScriptableObject.CreateInstance<TerrainDefinitionSO>();
        highGrass = ScriptableObject.CreateInstance<TerrainDefinitionSO>();

        biome = ScriptableObject.CreateInstance<BiomeProfileSO>();
        biome.baseTerrain = grass;
        biome.patchTerrains = new List<BiomeTerrainEntry>();
        biome.baseVariants = new List<BiomeTerrainEntry>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(biome);
        Object.DestroyImmediate(grass);
        Object.DestroyImmediate(dryDirt);
        Object.DestroyImmediate(darkSoil);
        Object.DestroyImmediate(water);
        Object.DestroyImmediate(highGrass);
    }

    [Test]
    public void ChooseTerrainStack_NoPatchesMatch_ReturnsOnlyBaseTerrain()
    {
        biome.patchTerrains.Add(new BiomeTerrainEntry { terrain = dryDirt, coverageTarget = 0f, noiseScale = 0.08f });

        var stack = TerrainSelector.ChooseTerrainStack(5, 5, 0f, 0f, biome);

        Assert.AreEqual(1, stack.Count);
        Assert.AreEqual(grass, stack[0]);
    }

    [Test]
    public void ChooseTerrainStack_Depth1PatchMatches_ReturnsBasePlusPatch()
    {
        biome.patchTerrains.Add(new BiomeTerrainEntry { terrain = dryDirt, coverageTarget = 1f, noiseScale = 0.08f });

        var stack = TerrainSelector.ChooseTerrainStack(5, 5, 0f, 0f, biome);

        Assert.AreEqual(2, stack.Count);
        Assert.AreEqual(grass, stack[0]);
        Assert.AreEqual(dryDirt, stack[1]);
    }

    [Test]
    public void ChooseTerrainStack_NestedPatchAlsoMatches_ReturnsAllThreeLevels()
    {
        var dryDirtEntry = new BiomeTerrainEntry { terrain = dryDirt, coverageTarget = 1f, noiseScale = 0.08f };
        dryDirtEntry.nestedPatches.Add(new BiomeTerrainEntry { terrain = darkSoil, coverageTarget = 1f, noiseScale = 0.08f });
        biome.patchTerrains.Add(dryDirtEntry);

        var stack = TerrainSelector.ChooseTerrainStack(5, 5, 0f, 0f, biome);

        Assert.AreEqual(3, stack.Count);
        Assert.AreEqual(grass, stack[0]);
        Assert.AreEqual(dryDirt, stack[1]);
        Assert.AreEqual(darkSoil, stack[2]);
    }

    [Test]
    public void ChooseTerrainStack_NestedPatchDoesNotMatch_StopsAtParentDepth()
    {
        var dryDirtEntry = new BiomeTerrainEntry { terrain = dryDirt, coverageTarget = 1f, noiseScale = 0.08f };
        dryDirtEntry.nestedPatches.Add(new BiomeTerrainEntry { terrain = darkSoil, coverageTarget = 0f, noiseScale = 0.08f });
        biome.patchTerrains.Add(dryDirtEntry);

        var stack = TerrainSelector.ChooseTerrainStack(5, 5, 0f, 0f, biome);

        Assert.AreEqual(2, stack.Count);
        Assert.AreEqual(grass, stack[0]);
        Assert.AreEqual(dryDirt, stack[1]);
    }

    [Test]
    public void ChooseTerrainStack_MultiplePatchesAtSameLevelMatch_FirstInListWins()
    {
        biome.patchTerrains.Add(new BiomeTerrainEntry { terrain = water, coverageTarget = 1f, noiseScale = 0.08f });
        biome.patchTerrains.Add(new BiomeTerrainEntry { terrain = dryDirt, coverageTarget = 1f, noiseScale = 0.08f });

        var stack = TerrainSelector.ChooseTerrainStack(5, 5, 0f, 0f, biome);

        Assert.AreEqual(water, stack[1]);
    }

    [Test]
    public void ChooseTerrainStack_EntryWithNullTerrain_IsSkipped()
    {
        biome.patchTerrains.Add(new BiomeTerrainEntry { terrain = null, coverageTarget = 1f, noiseScale = 0.08f });

        var stack = TerrainSelector.ChooseTerrainStack(5, 5, 0f, 0f, biome);

        Assert.AreEqual(1, stack.Count);
        Assert.AreEqual(grass, stack[0]);
    }

    [Test]
    public void ChooseTerrainStack_SameInputs_AlwaysReturnsSameResult()
    {
        var dryDirtEntry = new BiomeTerrainEntry { terrain = dryDirt, coverageTarget = 0.3f, noiseScale = 0.08f };
        dryDirtEntry.nestedPatches.Add(new BiomeTerrainEntry { terrain = darkSoil, coverageTarget = 0.3f, noiseScale = 0.08f });
        biome.patchTerrains.Add(dryDirtEntry);

        var first = TerrainSelector.ChooseTerrainStack(12, -7, 0f, 0f, biome);
        var second = TerrainSelector.ChooseTerrainStack(12, -7, 0f, 0f, biome);

        CollectionAssert.AreEqual(first, second);
    }

    // ---- Margem de 1 célula obrigatória entre um filho aninhado e a borda do pai ----

    [Test]
    public void IsInteriorToMask_AllNeighborsClaim_ReturnsTrue()
    {
        var entry = new BiomeTerrainEntry { terrain = dryDirt, coverageTarget = 1f, noiseScale = 0.08f };

        Assert.IsTrue(TerrainSelector.IsInteriorToMask(entry, 5, 5, 0f, 0f));
    }

    [Test]
    public void IsInteriorToMask_NoNeighborClaims_ReturnsFalse()
    {
        var entry = new BiomeTerrainEntry { terrain = dryDirt, coverageTarget = 0f, noiseScale = 0.08f };

        Assert.IsFalse(TerrainSelector.IsInteriorToMask(entry, 5, 5, 0f, 0f));
    }

    [Test]
    public void ChooseTerrainStack_NestedPatch_NeverClaimsCellOnParentsOwnEdge()
    {
        // Varre uma faixa real de células com uma máscara de pai não-uniforme (coverage
        // intermediário, então o pai tem borda de verdade em algum lugar) e confirma que,
        // sempre que a pilha desce até o filho aninhado, a célula realmente era interior ao
        // pai — nunca um filho aparece numa célula onde o próprio pai está na borda dele.
        var dryDirtEntry = new BiomeTerrainEntry { terrain = dryDirt, coverageTarget = 0.5f, noiseScale = 0.1f };
        dryDirtEntry.nestedPatches.Add(new BiomeTerrainEntry { terrain = darkSoil, coverageTarget = 1f, noiseScale = 0.1f });
        biome.patchTerrains.Add(dryDirtEntry);

        for (int x = 0; x < 25; x++)
        {
            for (int y = 0; y < 25; y++)
            {
                var stack = TerrainSelector.ChooseTerrainStack(x, y, 0f, 0f, biome);

                if (stack.Count == 3) // chegou até o DarkSoil
                {
                    Assert.IsTrue(TerrainSelector.IsInteriorToMask(dryDirtEntry, x, y, 0f, 0f),
                        $"DarkSoil apareceu em ({x},{y}) mas essa célula está na borda do próprio DryDirt.");
                }
            }
        }
    }

    // ---- ChooseTerrainStackNonAlloc — mesmo resultado que ChooseTerrainStack, sem List ----
    // (Sprint P2: ChunkTerrainGenerator chama isso 1x por célula, até 4096x por chunk — a
    // versão com List alocava lixo suficiente pra causar engasgo perceptível de GC ao
    // streamar chunk novo, confirmado em teste do usuário.)

    [Test]
    public void ChooseTerrainStackNonAlloc_MatchesAllocatingVersion_NoPatches()
    {
        biome.patchTerrains.Add(new BiomeTerrainEntry { terrain = dryDirt, coverageTarget = 0f, noiseScale = 0.08f });
        var buffer = new TerrainDefinitionSO[3];

        var expected = TerrainSelector.ChooseTerrainStack(5, 5, 0f, 0f, biome);
        int depth = TerrainSelector.ChooseTerrainStackNonAlloc(5, 5, 0f, 0f, biome, buffer);

        Assert.AreEqual(expected.Count, depth);
        for (int i = 0; i < depth; i++) Assert.AreEqual(expected[i], buffer[i]);
    }

    [Test]
    public void ChooseTerrainStackNonAlloc_MatchesAllocatingVersion_NestedPatchMatches()
    {
        var dryDirtEntry = new BiomeTerrainEntry { terrain = dryDirt, coverageTarget = 1f, noiseScale = 0.08f };
        dryDirtEntry.nestedPatches.Add(new BiomeTerrainEntry { terrain = darkSoil, coverageTarget = 1f, noiseScale = 0.08f });
        biome.patchTerrains.Add(dryDirtEntry);
        var buffer = new TerrainDefinitionSO[3];

        var expected = TerrainSelector.ChooseTerrainStack(5, 5, 0f, 0f, biome);
        int depth = TerrainSelector.ChooseTerrainStackNonAlloc(5, 5, 0f, 0f, biome, buffer);

        Assert.AreEqual(expected.Count, depth);
        for (int i = 0; i < depth; i++) Assert.AreEqual(expected[i], buffer[i]);
    }

    [Test]
    public void ChooseTerrainStackNonAlloc_BufferShorterThanTree_StopsAtBufferLength()
    {
        var dryDirtEntry = new BiomeTerrainEntry { terrain = dryDirt, coverageTarget = 1f, noiseScale = 0.08f };
        dryDirtEntry.nestedPatches.Add(new BiomeTerrainEntry { terrain = darkSoil, coverageTarget = 1f, noiseScale = 0.08f });
        biome.patchTerrains.Add(dryDirtEntry);
        var buffer = new TerrainDefinitionSO[2]; // menor que a profundidade real (3)

        int depth = TerrainSelector.ChooseTerrainStackNonAlloc(5, 5, 0f, 0f, biome, buffer);

        Assert.AreEqual(2, depth);
        Assert.AreEqual(grass, buffer[0]);
        Assert.AreEqual(dryDirt, buffer[1]);
    }

    [Test]
    public void ChooseTerrainStackNonAlloc_SameInputs_AlwaysReturnsSameResult()
    {
        var dryDirtEntry = new BiomeTerrainEntry { terrain = dryDirt, coverageTarget = 0.3f, noiseScale = 0.08f };
        biome.patchTerrains.Add(dryDirtEntry);
        var bufferA = new TerrainDefinitionSO[3];
        var bufferB = new TerrainDefinitionSO[3];

        int depthA = TerrainSelector.ChooseTerrainStackNonAlloc(12, -7, 0f, 0f, biome, bufferA);
        int depthB = TerrainSelector.ChooseTerrainStackNonAlloc(12, -7, 0f, 0f, biome, bufferB);

        Assert.AreEqual(depthA, depthB);
        CollectionAssert.AreEqual(bufferA, bufferB);
    }

    // ---- safeZoneRadius — terreno perigoso (blockedInSafeZone) nunca nasce perto da origem ----

    [Test]
    public void ChooseTerrainStack_BlockedEntryInsideSafeZoneRadius_NeverMatches()
    {
        biome.patchTerrains.Add(new BiomeTerrainEntry { terrain = water, coverageTarget = 1f, noiseScale = 0.08f, blockedInSafeZone = true });

        // (3,4): distância 5 da origem, dentro de um raio de 10.
        var stack = TerrainSelector.ChooseTerrainStack(3, 4, 0f, 0f, biome, safeZoneRadius: 10f);

        Assert.AreEqual(1, stack.Count);
        Assert.AreEqual(grass, stack[0]);
    }

    [Test]
    public void ChooseTerrainStack_BlockedEntryOutsideSafeZoneRadius_MatchesNormally()
    {
        biome.patchTerrains.Add(new BiomeTerrainEntry { terrain = water, coverageTarget = 1f, noiseScale = 0.08f, blockedInSafeZone = true });

        // (30,40): distância 50 da origem, fora de um raio de 10.
        var stack = TerrainSelector.ChooseTerrainStack(30, 40, 0f, 0f, biome, safeZoneRadius: 10f);

        Assert.AreEqual(2, stack.Count);
        Assert.AreEqual(water, stack[1]);
    }

    [Test]
    public void ChooseTerrainStack_BlockedEntry_DefaultSafeZoneRadiusZero_StillMatches()
    {
        // Compatibilidade: chamador que não passa safeZoneRadius (ou passa 0) não filtra nada,
        // mesmo com blockedInSafeZone = true — comportamento igual a antes dessa feature existir.
        biome.patchTerrains.Add(new BiomeTerrainEntry { terrain = water, coverageTarget = 1f, noiseScale = 0.08f, blockedInSafeZone = true });

        var stack = TerrainSelector.ChooseTerrainStack(0, 0, 0f, 0f, biome);

        Assert.AreEqual(2, stack.Count);
        Assert.AreEqual(water, stack[1]);
    }

    [Test]
    public void ChooseTerrainStack_NonBlockedEntryInsideSafeZoneRadius_StillMatches()
    {
        // Só blockedInSafeZone é filtrado — Dirt/Pedra continuam normais dentro da safe zone.
        biome.patchTerrains.Add(new BiomeTerrainEntry { terrain = dryDirt, coverageTarget = 1f, noiseScale = 0.08f });

        var stack = TerrainSelector.ChooseTerrainStack(2, 2, 0f, 0f, biome, safeZoneRadius: 10f);

        Assert.AreEqual(2, stack.Count);
        Assert.AreEqual(dryDirt, stack[1]);
    }

    [Test]
    public void ChooseTerrainStackNonAlloc_BlockedEntryInsideSafeZoneRadius_NeverMatches()
    {
        biome.patchTerrains.Add(new BiomeTerrainEntry { terrain = water, coverageTarget = 1f, noiseScale = 0.08f, blockedInSafeZone = true });
        var buffer = new TerrainDefinitionSO[3];

        int depth = TerrainSelector.ChooseTerrainStackNonAlloc(3, 4, 0f, 0f, biome, buffer, safeZoneRadius: 10f);

        Assert.AreEqual(1, depth);
        Assert.AreEqual(grass, buffer[0]);
    }

    [Test]
    public void ChooseBaseVariant_NeverBlockedBySafeZone_EvenIfOtherEntriesAre()
    {
        // Variantes de grama não são perigosas — devem continuar aparecendo na safe zone.
        biome.baseVariants.Add(new BiomeTerrainEntry { terrain = highGrass, coverageTarget = 1f, noiseScale = 0.08f });

        var variant = TerrainSelector.ChooseBaseVariant(0, 0, 0f, 0f, biome);

        Assert.AreEqual(highGrass, variant);
    }

    // ---- baseVariants (variações de terreno-base, ex.: HighGrass) — célula "pura" só ----

    [Test]
    public void ChooseBaseVariant_EntryMatches_ReturnsThatVariant()
    {
        biome.baseVariants.Add(new BiomeTerrainEntry { terrain = highGrass, coverageTarget = 1f, noiseScale = 0.08f });

        var variant = TerrainSelector.ChooseBaseVariant(5, 5, 0f, 0f, biome);

        Assert.AreEqual(highGrass, variant);
    }

    [Test]
    public void ChooseBaseVariant_NoEntryMatches_ReturnsNull()
    {
        biome.baseVariants.Add(new BiomeTerrainEntry { terrain = highGrass, coverageTarget = 0f, noiseScale = 0.08f });

        var variant = TerrainSelector.ChooseBaseVariant(5, 5, 0f, 0f, biome);

        Assert.IsNull(variant);
    }

    [Test]
    public void ChooseBaseVariant_EmptyList_ReturnsNull()
    {
        var variant = TerrainSelector.ChooseBaseVariant(5, 5, 0f, 0f, biome);

        Assert.IsNull(variant);
    }

    // ---- Variante de grama só nasce formando bloco 2×2 de verdade — nunca linha fina ----
    // (regra só pra baseVariants; patchTerrains/água/dirt/pedra aceitam qualquer disposição).
    // Histórico: 2 tentativas anteriores exigiam só "1 vizinho reto igual" — pareciam certas no
    // raciocínio e nos testes sintéticos, mas o usuário continuou vendo o bug depois de
    // recompilar/reiniciar o Editor inteiro. Um log real (coordenadas + seed de verdade, não
    // offset 0,0) provou que a geração já estava correta (zero isoladas, menor bloco = 2
    // células) — o problema real era os RuleTiles das variantes só terem 15 das ~47 posições de
    // vizinhança configuradas (confirmado contando `m_Sprites:` nos `.asset`), faltando
    // cobertura pra formas finas como um par de 2 células. Decisão do usuário: restringir a
    // geração pra só criar blocos 2×2+ reais (`HasFilledQuad`), em vez de desenhar as regras que
    // faltam agora.

    [Test]
    public void HasFilledQuad_AllNeighborsMatchSameEntry_ReturnsTrue()
    {
        var entry = new BiomeTerrainEntry { terrain = highGrass, coverageTarget = 2f, noiseScale = 0.08f };
        biome.baseVariants.Add(entry);

        Assert.IsTrue(TerrainSelector.HasFilledQuad(entry, 5, 5, 0f, 0f, biome));
    }

    [Test]
    public void HasFilledQuad_EntryNotInBiome_ReturnsFalse()
    {
        var entry = new BiomeTerrainEntry { terrain = highGrass, coverageTarget = 2f, noiseScale = 0.08f };
        // entry não foi adicionada a biome.baseVariants — nenhuma célula pode "vencer" com ela.

        Assert.IsFalse(TerrainSelector.HasFilledQuad(entry, 5, 5, 0f, 0f, biome));
    }

    [Test]
    public void HasFilledQuad_QuadCellsClaimedByPatchTerrain_AreNotPure_ReturnsFalse()
    {
        // Mesmo a entrada "querendo" nascer nos vizinhos, se eles têm Dirt/Pedra/Água por cima
        // (não são células puras), nenhuma variante nasce ali de verdade — não conta.
        var entry = new BiomeTerrainEntry { terrain = highGrass, coverageTarget = 2f, noiseScale = 0.08f };
        biome.baseVariants.Add(entry);
        biome.patchTerrains.Add(new BiomeTerrainEntry { terrain = dryDirt, coverageTarget = 2f, noiseScale = 0.08f });

        Assert.IsFalse(TerrainSelector.HasFilledQuad(entry, 5, 5, 0f, 0f, biome));
    }

    [Test]
    public void HasFilledQuad_QuadCellsWonByDifferentEntry_ReturnsFalse()
    {
        // Uma variante DIFERENTE vence todas as células vizinhas (primeira da lista, cobertura
        // sempre bate) — no RuleTile isso NÃO conecta com a entrada atual, mesmo ambas sendo
        // "variante de grama" conceitualmente. `water` aqui só representa "qualquer outra
        // entrada", sem relação com o terreno real.
        var otherEntry = new BiomeTerrainEntry { terrain = water, coverageTarget = 2f, noiseScale = 0.08f };
        var highGrassEntry = new BiomeTerrainEntry { terrain = highGrass, coverageTarget = 2f, noiseScale = 0.08f };
        biome.baseVariants.Add(otherEntry); // primeiro na lista = sempre vence
        biome.baseVariants.Add(highGrassEntry);

        Assert.IsFalse(TerrainSelector.HasFilledQuad(highGrassEntry, 5, 5, 0f, 0f, biome));
    }

    [Test]
    public void ChooseBaseVariant_NeverReturnsACellOutsideAFilledQuad()
    {
        // Varre uma faixa real de células com uma máscara moderada (nem tudo bate, nem nada
        // bate — então existem bordas/pontas de verdade) e confirma que, sempre que
        // ChooseBaseVariant devolver uma variante, a célula fazia parte de um bloco 2×2 real.
        var entry = new BiomeTerrainEntry { terrain = highGrass, coverageTarget = 0.3f, noiseScale = 0.08f };
        biome.baseVariants.Add(entry);

        for (int x = 0; x < 25; x++)
        {
            for (int y = 0; y < 25; y++)
            {
                var variant = TerrainSelector.ChooseBaseVariant(x, y, 0f, 0f, biome);

                if (variant != null)
                {
                    Assert.IsTrue(TerrainSelector.HasFilledQuad(entry, x, y, 0f, 0f, biome),
                        $"Variante apareceu fora de um bloco 2×2 em ({x},{y}).");
                }
            }
        }
    }

    [Test]
    public void ChooseBaseVariant_SameInputs_AlwaysReturnsSameResult()
    {
        biome.baseVariants.Add(new BiomeTerrainEntry { terrain = highGrass, coverageTarget = 0.4f, noiseScale = 0.1f });

        var first = TerrainSelector.ChooseBaseVariant(12, -7, 0f, 0f, biome);
        var second = TerrainSelector.ChooseBaseVariant(12, -7, 0f, 0f, biome);

        Assert.AreEqual(first, second);
    }
}
