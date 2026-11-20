using System.Collections.Generic;
using NUnit.Framework;

// D9.3 (lixo de memória): ChunkCoord é chave de Dictionary/HashSet do streaming de chunks —
// precisa de IEquatable<ChunkCoord> pra comparar sem empacotar a struct.
public class ChunkCoordEquatableTests
{
    [Test]
    public void ImplementsIEquatable()
    {
        Assert.IsTrue(typeof(System.IEquatable<ChunkCoord>).IsAssignableFrom(typeof(ChunkCoord)));
    }

    [Test]
    public void EqualCoords_AreEqual_AndSameHash()
    {
        var a = new ChunkCoord(3, -2);
        var b = new ChunkCoord(3, -2);
        Assert.IsTrue(a.Equals(b));
        Assert.IsTrue(a == b);
        Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
    }

    [Test]
    public void DifferentCoords_AreNotEqual()
    {
        Assert.IsFalse(new ChunkCoord(1, 2) == new ChunkCoord(2, 1));
        Assert.IsTrue(new ChunkCoord(1, 2) != new ChunkCoord(1, 3));
    }

    [Test]
    public void WorksAsHashSetKey()
    {
        var set = new HashSet<ChunkCoord> { new ChunkCoord(0, 0), new ChunkCoord(0, 0), new ChunkCoord(1, 0) };
        Assert.AreEqual(2, set.Count);
        Assert.IsTrue(set.Contains(new ChunkCoord(1, 0)));
    }
}
