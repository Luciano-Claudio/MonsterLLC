using System.Collections.Generic;
using NUnit.Framework;

// Catálogo de cartas — pilhas de DOT (Blight Crucible): cada aplicação é uma pilha com duração própria;
// cheio, a nova substitui a mais antiga do mesmo tipo. Sem pilhas extras = comportamento antigo (renova).
public class DotStackRulesTests
{
    private readonly List<StatusStack> stacks = new();
    private long order;

    [SetUp] public void Clear() { stacks.Clear(); order = 0; }

    private void Apply(StatusEffectType type, float duration, float dps, int max)
        => DotStackRules.Apply(stacks, type, duration, duration, dps, max, ref order, 1f);

    private int Count(StatusEffectType type) { int n = 0; foreach (var s in stacks) if (s.type == type) n++; return n; }

    [Test]
    public void Max_stacks_by_blight_level()
    {
        Assert.AreEqual(1, DotStackRules.MaxStacks(0));
        Assert.AreEqual(2, DotStackRules.MaxStacks(1));
        Assert.AreEqual(3, DotStackRules.MaxStacks(2));
        Assert.AreEqual(3, DotStackRules.MaxStacks(5));
    }

    [Test]
    public void One_stack_refreshes_like_before()
    {
        Apply(StatusEffectType.Fire, 3f, 1f, 1);
        DotStackRules.Advance(stacks, 2f);
        Apply(StatusEffectType.Fire, 3f, 5f, 1);
        Assert.AreEqual(1, Count(StatusEffectType.Fire));
        Assert.AreEqual(3f, stacks[0].remaining, 1e-4f);
        Assert.AreEqual(5f, stacks[0].damagePerSecond, 1e-4f);
    }

    [Test]
    public void Fourth_application_replaces_oldest_at_cap_3()
    {
        Apply(StatusEffectType.Bleeding, 3f, 1f, 3);
        Apply(StatusEffectType.Bleeding, 3f, 2f, 3);
        Apply(StatusEffectType.Bleeding, 3f, 3f, 3);
        Apply(StatusEffectType.Bleeding, 3f, 4f, 3);
        Assert.AreEqual(3, Count(StatusEffectType.Bleeding));
        var dps = new List<float>(); foreach (var s in stacks) dps.Add(s.damagePerSecond);
        CollectionAssert.DoesNotContain(dps, 1f); // a mais antiga (dps 1) saiu
        CollectionAssert.Contains(dps, 4f);
    }

    [Test]
    public void Each_stack_expires_on_its_own()
    {
        Apply(StatusEffectType.Poison, 2f, 1f, 3);
        DotStackRules.Advance(stacks, 1f);
        Apply(StatusEffectType.Poison, 2f, 1f, 3);
        DotStackRules.Advance(stacks, 1.5f); // a 1ª acabou (2,5 > 2), a 2ª ainda tem 0,5
        Assert.AreEqual(1, Count(StatusEffectType.Poison));
    }

    [Test]
    public void Different_types_do_not_share_the_cap()
    {
        Apply(StatusEffectType.Fire, 3f, 1f, 1);
        Apply(StatusEffectType.Bleeding, 3f, 1f, 1);
        Assert.AreEqual(1, Count(StatusEffectType.Fire));
        Assert.AreEqual(1, Count(StatusEffectType.Bleeding));
    }

    [Test]
    public void Dot_damage_bonus_from_level_3()
    {
        Assert.AreEqual(1f, DotStackRules.DotDamageMultiplier(2), 1e-4f);
        Assert.AreEqual(1.1f, DotStackRules.DotDamageMultiplier(3), 1e-4f);
        Assert.AreEqual(1.3f, DotStackRules.DotDamageMultiplier(5), 1e-4f);
    }
}
