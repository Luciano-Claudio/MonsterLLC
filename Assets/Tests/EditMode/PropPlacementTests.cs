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
}
