using NUnit.Framework;

// Mini bosses (2026-11-21): sorteio uniforme entre os elegíveis pelo tier do dia, sem repetir o
// último que nasceu (a menos que seja o único elegível), e intervalo sorteado entre 30 e 40 s.
public class MiniBossRulesTests
{
    [Test]
    public void Pick_NoneEligible_ReturnsMinusOne()
    {
        Assert.AreEqual(-1, MiniBossRules.Pick(new bool[5], 5, -1, 0.5f));
    }

    [Test]
    public void Pick_ExcludesLast()
    {
        var eligible = new[] { true, true, false };
        foreach (float r in new[] { 0f, 0.3f, 0.7f, 0.999f })
            Assert.AreEqual(1, MiniBossRules.Pick(eligible, 3, 0, r));
    }

    [Test]
    public void Pick_OnlyLastEligible_Repeats()
    {
        Assert.AreEqual(2, MiniBossRules.Pick(new[] { false, false, true }, 3, 2, 0.4f));
    }

    [Test]
    public void Pick_UniformOverEligible()
    {
        var eligible = new[] { true, false, true, false, true };
        Assert.AreEqual(0, MiniBossRules.Pick(eligible, 5, -1, 0f));
        Assert.AreEqual(2, MiniBossRules.Pick(eligible, 5, -1, 0.5f));
        Assert.AreEqual(4, MiniBossRules.Pick(eligible, 5, -1, 0.99f));
    }

    [Test]
    public void NextInterval_Range()
    {
        Assert.AreEqual(30f, MiniBossRules.NextInterval(30f, 40f, 0f), 1e-5f);
        Assert.AreEqual(40f, MiniBossRules.NextInterval(30f, 40f, 1f), 1e-5f);
    }
}
