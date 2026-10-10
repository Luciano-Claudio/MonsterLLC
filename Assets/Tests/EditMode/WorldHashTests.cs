using NUnit.Framework;

public class WorldHashTests
{
    [Test]
    public void Hash2D_SameInputs_AlwaysReturnsSameValue()
    {
        uint first = WorldHash.Hash2D(5, -3, channel: 0, seed: 42);
        uint second = WorldHash.Hash2D(5, -3, channel: 0, seed: 42);

        Assert.AreEqual(first, second);
    }

    [Test]
    public void Hash2D_DifferentSeed_ReturnsDifferentValue()
    {
        uint withSeedA = WorldHash.Hash2D(5, -3, channel: 0, seed: 1);
        uint withSeedB = WorldHash.Hash2D(5, -3, channel: 0, seed: 2);

        Assert.AreNotEqual(withSeedA, withSeedB);
    }

    [Test]
    public void Hash2D_DifferentChannel_ReturnsDifferentValue()
    {
        uint channel0 = WorldHash.Hash2D(5, -3, channel: 0, seed: 42);
        uint channel1 = WorldHash.Hash2D(5, -3, channel: 1, seed: 42);

        Assert.AreNotEqual(channel0, channel1);
    }

    [Test]
    public void Hash2D_DifferentCoordinate_ReturnsDifferentValue()
    {
        uint origin = WorldHash.Hash2D(0, 0, channel: 0, seed: 42);
        uint neighbor = WorldHash.Hash2D(1, 0, channel: 0, seed: 42);

        Assert.AreNotEqual(origin, neighbor);
    }

    [Test]
    public void Hash2DFloat_IsWithinZeroToOneRange()
    {
        float value = WorldHash.Hash2DFloat(123, 456, channel: 2, seed: 7);

        Assert.GreaterOrEqual(value, 0f);
        Assert.Less(value, 1f);
    }
}
