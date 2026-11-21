using NUnit.Framework;
using UnityEngine;

// D9.4 — regras puras do spawn em cerco: setores, elipse fora da tela, anti-kite e empurrão.
public class HordeSpawnRulesTests
{
    [Test]
    public void SectorOf_RightIs0_UpIsQuarter()
    {
        Assert.AreEqual(0, SpawnRing.SectorOf(new Vector2(1, 0), 12));
        Assert.AreEqual(3, SpawnRing.SectorOf(new Vector2(0, 1), 12));
        Assert.AreEqual(6, SpawnRing.SectorOf(new Vector2(-1, 0), 12));
        Assert.AreEqual(9, SpawnRing.SectorOf(new Vector2(0, -1), 12));
    }

    [Test]
    public void Ellipse_PointsAreOutsideView_ForAnyAspect()
    {
        const float margin = 2f;
        var views = new[] { new Vector2(17.8f, 10f), new Vector2(10f, 10f), new Vector2(22.5f, 10f) };
        foreach (var v in views)
        {
            for (int deg = 0; deg < 360; deg += 5)
            {
                Vector2 p = SpawnRing.PointOnEllipse(deg * Mathf.Deg2Rad, v.x * Mathf.Sqrt(2f) + margin, v.y * Mathf.Sqrt(2f) + margin);
                Assert.IsTrue(SpawnRing.IsOutsideView(p, v.x, v.y), $"view {v} ângulo {deg} ponto {p}");
            }
        }
    }

    [Test]
    public void IsFrontSector_ArcAroundFlee()
    {
        Assert.IsTrue(SpawnRing.IsFrontSector(0, 12, new Vector2(1, 0), 90f));
        Assert.IsFalse(SpawnRing.IsFrontSector(6, 12, new Vector2(1, 0), 90f));
    }

    private static int[] Order(int[] counts, float[] bias, int tieRotation)
    {
        var order = new int[counts.Length];
        SpawnSectorPicker.FillOrder(counts, bias, counts.Length, tieRotation, order);
        return order;
    }

    [Test]
    public void SpawnSectorPicker_PicksLeastCrowded()
    {
        var counts = new[] { 5, 2, 7, 6, 6, 6, 6, 6, 6, 6, 6, 6 };
        Assert.AreEqual(1, Order(counts, new float[12], 0)[0]);
    }

    [Test]
    public void SpawnSectorPicker_BiasCountsAsFewerMonsters()
    {
        Assert.AreEqual(1, Order(new[] { 3, 3 }, new[] { 0f, 3f }, 0)[0]);
    }

    [Test]
    public void SpawnSectorPicker_TieRotation()
    {
        Assert.AreEqual(4, Order(new int[12], new float[12], 4)[0]);
    }

    [Test]
    public void SpawnSectorPicker_Order_CoversEverySector()
    {
        var order = Order(new[] { 4, 1, 1, 9, 0, 3, 3, 2, 8, 1, 0, 5 }, new float[12], 7);
        var seen = new bool[12];
        foreach (int s in order) { Assert.IsFalse(seen[s], $"setor {s} repetido"); seen[s] = true; }
        foreach (bool b in seen) Assert.IsTrue(b);
    }

    [Test]
    public void KiteTracker_SteadyFlee_GivesDirection()
    {
        var k = new KiteTracker(3f, 1.5f, 20);
        for (int i = 0; i < 60; i++) k.AddVelocity(new Vector2(3f, 0f), 0.05f);
        Assert.IsTrue(k.TryGetFleeDirection(out var dir));
        Assert.AreEqual(1f, dir.x, 0.01f);
        Assert.AreEqual(0f, dir.y, 0.01f);
    }

    [Test]
    public void KiteTracker_Slow_NoDirection()
    {
        var k = new KiteTracker(3f, 1.5f, 20);
        for (int i = 0; i < 200; i++) k.AddVelocity(new Vector2(0.5f, 0f), 0.05f);
        Assert.IsFalse(k.TryGetFleeDirection(out _));
    }

    [Test]
    public void KiteTracker_FrontShare_LastN()
    {
        var k = new KiteTracker(3f, 1.5f, 20);
        for (int i = 0; i < 10; i++) k.RecordSpawn(true);
        for (int i = 0; i < 20; i++) k.RecordSpawn(false);
        Assert.AreEqual(0f, k.FrontShare, 1e-5f);
        for (int i = 0; i < 8; i++) k.RecordSpawn(true);
        Assert.AreEqual(0.4f, k.FrontShare, 1e-5f);
    }

    [Test]
    public void LateralPush_MonsterOnRight_PushesRight()
    {
        Vector2 p = CrowdPushRules.LateralPush(new Vector2(0, 1), new Vector2(0.3f, 1f));
        Assert.AreEqual(1f, p.x, 1e-4f);
        Assert.AreEqual(0f, p.y, 1e-4f);
    }

    [Test]
    public void LateralPush_DeadAhead_PushesLeft()
    {
        Vector2 p = CrowdPushRules.LateralPush(new Vector2(0, 1), new Vector2(0f, 1f));
        Assert.AreEqual(-1f, p.x, 1e-4f);
        Assert.AreEqual(0f, p.y, 1e-4f);
    }
}
