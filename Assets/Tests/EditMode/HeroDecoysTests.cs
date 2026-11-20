using NUnit.Framework;
using UnityEngine;

// D9.1 (Assassin) — durante o ataque, os monstros perseguem os projéteis (iscas) até eles
// voltarem; cada monstro pega a isca mais perto dele no momento em que ela nasce.
public class HeroDecoysTests
{
    private GameObject a, b;

    [SetUp]
    public void SetUp()
    {
        HeroDecoys.Clear();
        a = new GameObject("DecoyA"); a.transform.position = new Vector3(5f, 0f, 0f);
        b = new GameObject("DecoyB"); b.transform.position = new Vector3(1f, 1f, 0f);
    }

    [TearDown]
    public void TearDown()
    {
        HeroDecoys.Clear();
        Object.DestroyImmediate(a);
        Object.DestroyImmediate(b);
    }

    [Test]
    public void PickNearest_ReturnsClosestDecoy()
    {
        HeroDecoys.Register(a.transform);
        HeroDecoys.Register(b.transform);
        Assert.AreSame(b.transform, HeroDecoys.PickNearest(Vector2.zero));
    }

    [Test]
    public void PickNearest_Empty_ReturnsNull()
    {
        Assert.IsNull(HeroDecoys.PickNearest(Vector2.zero));
    }

    [Test]
    public void Register_IncrementsVersion()
    {
        int before = HeroDecoys.Version;
        HeroDecoys.Register(a.transform);
        Assert.AreEqual(before + 1, HeroDecoys.Version);
    }

    [Test]
    public void Unregister_StopsBeingTargetable()
    {
        HeroDecoys.Register(a.transform);
        HeroDecoys.Unregister(a.transform);
        Assert.IsFalse(HeroDecoys.IsActive(a.transform));
        Assert.IsNull(HeroDecoys.PickNearest(Vector2.zero));
    }

    [Test]
    public void DestroyedDecoy_IsIgnored()
    {
        HeroDecoys.Register(a.transform);
        HeroDecoys.Register(b.transform);
        Object.DestroyImmediate(b);
        Assert.AreSame(a.transform, HeroDecoys.PickNearest(Vector2.zero));
    }
}
