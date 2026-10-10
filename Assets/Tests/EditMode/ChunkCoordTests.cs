using NUnit.Framework;
using UnityEngine;

public class ChunkCoordTests
{
    [Test]
    public void FromWorldPosition_PositiveCoordinate_FloorsCorrectly()
    {
        var coord = ChunkCoord.FromWorldPosition(new Vector2(130f, 65f), chunkSize: 64);

        Assert.AreEqual(2, coord.X);
        Assert.AreEqual(1, coord.Y);
    }

    [Test]
    public void FromWorldPosition_NegativeCoordinate_FloorsTowardsNegativeInfinity()
    {
        // -1 com chunkSize 64 tem que cair no chunk -1, não truncar pra 0 — é o chunk
        // imediatamente à esquerda/abaixo da origem.
        var coord = ChunkCoord.FromWorldPosition(new Vector2(-1f, -1f), chunkSize: 64);

        Assert.AreEqual(-1, coord.X);
        Assert.AreEqual(-1, coord.Y);
    }

    [Test]
    public void Equality_SameCoordinates_AreEqual()
    {
        var a = new ChunkCoord(3, -7);
        var b = new ChunkCoord(3, -7);

        Assert.IsTrue(a == b);
        Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
    }

    [Test]
    public void Equality_DifferentCoordinates_AreNotEqual()
    {
        var a = new ChunkCoord(3, -7);
        var b = new ChunkCoord(-7, 3);

        Assert.IsTrue(a != b);
    }
}
