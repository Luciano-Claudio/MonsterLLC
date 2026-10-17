using NUnit.Framework;
using UnityEngine;

public class PropPlacementTests
{
    [Test]
    public void GenerateCandidatePositions_SameInputs_AlwaysReturnsSameResult()
    {
        var first = PropPlacement.GenerateCandidatePositions(0, 0, 64, minSpacing: 4f, density: 0.5f, jitter: 0.6f, worldSeed: 42);
        var second = PropPlacement.GenerateCandidatePositions(0, 0, 64, minSpacing: 4f, density: 0.5f, jitter: 0.6f, worldSeed: 42);

        Assert.AreEqual(first.Count, second.Count);
        for (int i = 0; i < first.Count; i++)
        {
            Assert.AreEqual(first[i], second[i]);
        }
    }

    [Test]
    public void GenerateCandidatePositions_RespectsMinimumSpacing()
    {
        var positions = PropPlacement.GenerateCandidatePositions(0, 0, 64, minSpacing: 4f, density: 1f, jitter: 1f, worldSeed: 7);

        for (int i = 0; i < positions.Count; i++)
        {
            for (int j = i + 1; j < positions.Count; j++)
            {
                float distance = Vector2.Distance(positions[i], positions[j]);
                Assert.GreaterOrEqual(distance, 4f);
            }
        }
    }

    [Test]
    public void GenerateCandidatePositions_ZeroDensity_ReturnsEmpty()
    {
        var positions = PropPlacement.GenerateCandidatePositions(0, 0, 64, minSpacing: 4f, density: 0f, jitter: 0.6f, worldSeed: 42);

        Assert.AreEqual(0, positions.Count);
    }

    [Test]
    public void GenerateCandidatePositions_ZeroMinSpacing_ReturnsEmpty()
    {
        var positions = PropPlacement.GenerateCandidatePositions(0, 0, 64, minSpacing: 0f, density: 1f, jitter: 0.6f, worldSeed: 42);

        Assert.AreEqual(0, positions.Count);
    }

    // Bug real (achado do usuário em teste): 2 EnvironmentPropSO com o mesmo minSpacing caem na
    // mesma grade de células e, sem o salt, sorteavam o MESMO densityRoll/jitter na mesma
    // célula — árvores/cacto-em-cima-de-pedra nascendo empilhados no ponto exato. `propSalt`
    // precisa decorrelacionar completamente os dois conjuntos de posições.
    [Test]
    public void GenerateCandidatePositions_DifferentPropSalt_NeverSharesAPosition()
    {
        var propA = PropPlacement.GenerateCandidatePositions(0, 0, 64, minSpacing: 4f, density: 0.5f, jitter: 0.6f, worldSeed: 42, propSalt: WorldHash.HashString("Hickory_Tree_1"));
        var propB = PropPlacement.GenerateCandidatePositions(0, 0, 64, minSpacing: 4f, density: 0.5f, jitter: 0.6f, worldSeed: 42, propSalt: WorldHash.HashString("Hickory_Tree_2"));

        Assert.Greater(propA.Count, 0);
        Assert.Greater(propB.Count, 0);
        foreach (var a in propA)
        {
            foreach (var b in propB)
            {
                Assert.AreNotEqual(a, b);
            }
        }
    }

    [Test]
    public void GenerateCandidatePositions_SamePropSalt_AlwaysReturnsSameResult()
    {
        int salt = WorldHash.HashString("Volcano");
        var first = PropPlacement.GenerateCandidatePositions(0, 0, 64, minSpacing: 4f, density: 0.5f, jitter: 0.6f, worldSeed: 42, propSalt: salt);
        var second = PropPlacement.GenerateCandidatePositions(0, 0, 64, minSpacing: 4f, density: 0.5f, jitter: 0.6f, worldSeed: 42, propSalt: salt);

        Assert.AreEqual(first.Count, second.Count);
        for (int i = 0; i < first.Count; i++)
        {
            Assert.AreEqual(first[i], second[i]);
        }
    }
}
