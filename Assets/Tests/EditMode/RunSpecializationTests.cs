using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

// Etapa 4 (docs/superpowers/specs/2026-11-24-etapa-4-especializacoes-tags-design.md) — escolha da
// especialização da run: tags, evento, reset no começo de cada run e catálogo por herói.
public class RunSpecializationTests
{
    private readonly List<Object> created = new();

    private SpecializationDefinition Spec(string id, string heroId, int slot, SpecTag tags = SpecTag.None)
    {
        var s = ScriptableObject.CreateInstance<SpecializationDefinition>();
        s.id = id; s.heroId = heroId; s.slot = slot; s.tags = tags;
        created.Add(s);
        return s;
    }

    [TearDown]
    public void Cleanup()
    {
        RunSpecialization.Reset();
        foreach (var o in created) Object.DestroyImmediate(o);
        created.Clear();
    }

    [Test]
    public void Tags_are_none_before_choosing()
    {
        RunSpecialization.Reset();
        Assert.AreEqual(SpecTag.None, RunSpecialization.Tags);
        Assert.IsFalse(RunSpecialization.IsChosen);
        Assert.IsNull(RunSpecialization.Current);
    }

    [Test]
    public void Choose_sets_tags_and_fires_event()
    {
        RunSpecialization.Reset();
        var spec = Spec("barbarian_bloodlust", "Barbarian", 1, SpecTag.Dot | SpecTag.Tank);
        int fired = 0;
        System.Action<SpecializationDefinition> h = s => { if (s == spec) fired++; };
        RunSpecialization.Chosen += h;
        try { RunSpecialization.Choose(spec); }
        finally { RunSpecialization.Chosen -= h; }

        Assert.AreEqual(SpecTag.Dot | SpecTag.Tank, RunSpecialization.Tags);
        Assert.IsTrue(RunSpecialization.Is("barbarian_bloodlust"));
        Assert.IsFalse(RunSpecialization.Is("barbarian_earthquake"));
        Assert.AreEqual(1, fired);
    }

    [Test]
    public void Reset_clears_choice()
    {
        RunSpecialization.Choose(Spec("a", "Barbarian", 0, SpecTag.Pet));
        RunSpecialization.Reset();
        Assert.AreEqual(SpecTag.None, RunSpecialization.Tags);
        Assert.IsFalse(RunSpecialization.IsChosen);
    }

    [Test]
    public void ForHero_returns_only_that_hero_sorted_by_slot()
    {
        var catalog = ScriptableObject.CreateInstance<SpecializationCatalog>();
        created.Add(catalog);
        var r1 = Spec("r1", "Ranger", 1);
        var b0 = Spec("b0", "Barbarian", 0);
        var r0 = Spec("r0", "Ranger", 0);
        catalog.specializations.AddRange(new[] { r1, b0, r0 });

        var result = new List<SpecializationDefinition>();
        catalog.ForHero("Ranger", result);
        CollectionAssert.AreEqual(new[] { r0, r1 }, result);
    }
}
