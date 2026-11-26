using System.IO;
using NUnit.Framework;
using UnityEngine;

// Gold permanente (2026-11-26): moeda que cai de qualquer monstro (10%), salva num arquivo próprio
// (profile.json) que sobrevive à morte — o save da run (save.json) é apagado quando o herói morre.
public class PermanentGoldTests
{
    private string path;
    private long events;

    [SetUp]
    public void SetUp()
    {
        path = Path.Combine(Application.temporaryCachePath, "profile_test.json");
        if (File.Exists(path)) File.Delete(path);
        PermanentGold.UseFileForTests(path);
        events = -1;
        GameEvents.OnPermanentGoldChanged += OnChanged;
    }

    [TearDown]
    public void TearDown()
    {
        GameEvents.OnPermanentGoldChanged -= OnChanged;
        PermanentGold.UseFileForTests(null);
        if (File.Exists(path)) File.Delete(path);
    }

    private void OnChanged(long gold) => events = gold;

    [Test]
    public void Starts_at_zero_without_a_file()
    {
        Assert.AreEqual(0, PermanentGold.Amount);
    }

    [Test]
    public void Add_saves_and_survives_a_reload()
    {
        PermanentGold.Add(3);
        PermanentGold.Add(2);
        Assert.AreEqual(5, PermanentGold.Amount);
        Assert.AreEqual(5, events);
        PermanentGold.UseFileForTests(path); // relê do disco
        Assert.AreEqual(5, PermanentGold.Amount);
    }

    [Test]
    public void Ignores_zero_and_negative()
    {
        PermanentGold.Add(0);
        PermanentGold.Add(-4);
        Assert.AreEqual(0, PermanentGold.Amount);
    }

    [Test]
    public void Drop_chance_rule()
    {
        Assert.IsTrue(PermanentGold.RollsDrop(0.10f, 0.05f));
        Assert.IsFalse(PermanentGold.RollsDrop(0.10f, 0.10f));
        Assert.IsFalse(PermanentGold.RollsDrop(0f, 0f));
    }
}
