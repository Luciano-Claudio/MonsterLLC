using NUnit.Framework;
using UnityEngine;

public class FanSpreadTests
{
    [Test]
    public void SingleArrow_ReturnsBaseDirectionUnchanged()
    {
        var dirs = FanSpread.GetDirections(Vector2.up, 1, 30f);
        Assert.AreEqual(1, dirs.Length);
        Assert.AreEqual(Vector2.up, dirs[0]);
    }

    [Test]
    public void TwoArrows_Are30DegreesApart()
    {
        var dirs = FanSpread.GetDirections(Vector2.up, 2, 30f);
        Assert.AreEqual(30f, Vector2.Angle(dirs[0], dirs[1]), 0.01f);
    }

    [Test]
    public void ThreeArrows_MiddleOneMatchesBaseDirection()
    {
        var dirs = FanSpread.GetDirections(Vector2.up, 3, 30f);
        Assert.AreEqual(Vector2.up.normalized, dirs[1], "a flecha do meio deve ser igual à direção da mira, sem rotação");
    }

    [Test]
    public void ThreeArrows_AdjacentPairsAre15DegreesApart()
    {
        var dirs = FanSpread.GetDirections(Vector2.up, 3, 30f);
        Assert.AreEqual(15f, Vector2.Angle(dirs[0], dirs[1]), 0.01f);
        Assert.AreEqual(15f, Vector2.Angle(dirs[1], dirs[2]), 0.01f);
    }

    [Test]
    public void FiveArrows_TotalSpreadStays30DegreesEndToEnd()
    {
        var dirs = FanSpread.GetDirections(Vector2.up, 5, 30f);
        Assert.AreEqual(30f, Vector2.Angle(dirs[0], dirs[4]), 0.01f);
    }
}
