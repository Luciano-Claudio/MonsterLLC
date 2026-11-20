using NUnit.Framework;
using UnityEngine;

// D9.1 (Druid Feral, Ultimate) — Stun incapacita o monstro (IA/ataque/movimento travam), mesma
// mecânica da paralisia da Oração do Cleric (WordOfPain): EnemyController consulta
// StatusEffectController.IsIncapacitated.
public class StunTests
{
    private GameObject go;
    private StatusEffectController status;

    [SetUp]
    public void SetUp()
    {
        go = new GameObject("StunTestTarget");
        status = go.AddComponent<StatusEffectController>();
    }

    [TearDown]
    public void TearDown() => Object.DestroyImmediate(go);

    [Test]
    public void Stun_Incapacitates()
    {
        status.ApplyStatusEffect(StatusEffectType.Stun, 2f, 0f);
        Assert.IsTrue(status.IsIncapacitated);
    }

    [Test]
    public void WordOfPain_StillIncapacitates()
    {
        status.ApplyStatusEffect(StatusEffectType.WordOfPain, 2f, 0f);
        Assert.IsTrue(status.IsIncapacitated);
    }

    [Test]
    public void Poison_DoesNotIncapacitate()
    {
        status.ApplyStatusEffect(StatusEffectType.Poison, 2f, 1f);
        Assert.IsFalse(status.IsIncapacitated);
    }

    [Test]
    public void Stun_EndsAfterDuration()
    {
        status.ApplyStatusEffect(StatusEffectType.Stun, 2f, 0f);
        status.Tick(2.1f);
        Assert.IsFalse(status.IsIncapacitated);
    }
}
