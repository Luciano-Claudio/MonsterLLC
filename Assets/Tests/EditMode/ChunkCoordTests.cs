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
        // [Sprint P4] Chunk 0 agora é CENTRADO na origem — vai de -32 a 32 (chunkSize 64), não
        // mais de 0 a 64. -33 cai no chunk -1 (passou do limite esquerdo do chunk 0).
        var coord = ChunkCoord.FromWorldPosition(new Vector2(-33f, -33f), chunkSize: 64);

        Assert.AreEqual(-1, coord.X);
        Assert.AreEqual(-1, coord.Y);
    }

    // [Sprint P4] Achado do usuário em teste visual: a origem do mundo (0,0,0, onde o herói
    // nasce) caía no VÉRTICE entre 4 chunks, não no centro de nenhum — quebrava a simetria
    // radial da malha de streaming/safe zone ao redor do jogador. Chunk (0,0) agora é
    // CENTRADO na origem.
    [Test]
    public void FromWorldPosition_Origin_FallsInsideCenterChunk()
    {
        var coord = ChunkCoord.FromWorldPosition(Vector2.zero, chunkSize: 64);

        Assert.AreEqual(0, coord.X);
        Assert.AreEqual(0, coord.Y);
    }

    [Test]
    public void FromWorldPosition_ExactlyAtHalfChunkBoundary_BelongsToTheOuterChunk()
    {
        // Chunk 0 vai de -32 (inclusive) a 32 (exclusive) — exatamente em 32 já é chunk 1,
        // exatamente em -32 ainda é chunk 0 (convenção meio-aberto, igual o floor original).
        Assert.AreEqual(1, ChunkCoord.FromWorldPosition(new Vector2(32f, 0f), chunkSize: 64).X);
        Assert.AreEqual(0, ChunkCoord.FromWorldPosition(new Vector2(-32f, 0f), chunkSize: 64).X);
        Assert.AreEqual(0, ChunkCoord.FromWorldPosition(new Vector2(31.99f, 0f), chunkSize: 64).X);
        Assert.AreEqual(-1, ChunkCoord.FromWorldPosition(new Vector2(-32.01f, 0f), chunkSize: 64).X);
    }

    [Test]
    public void ToWorldOrigin_CenterChunk_StartsHalfChunkBeforeOrigin()
    {
        var origin = new ChunkCoord(0, 0).ToWorldOrigin(chunkSize: 64);

        Assert.AreEqual(-32, origin.x);
        Assert.AreEqual(-32, origin.y);
    }

    [Test]
    public void ToWorldOrigin_RoundTripsWithFromWorldPosition()
    {
        // Qualquer ponto dentro do chunk (ex.: bem no meio dele) tem que voltar pro mesmo
        // ChunkCoord depois de converter chunk->mundo->chunk de novo.
        var original = new ChunkCoord(5, -3);
        var origin = original.ToWorldOrigin(chunkSize: 64);
        var midpoint = new Vector2(origin.x + 32f, origin.y + 32f);

        var roundTripped = ChunkCoord.FromWorldPosition(midpoint, chunkSize: 64);

        Assert.AreEqual(original, roundTripped);
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
