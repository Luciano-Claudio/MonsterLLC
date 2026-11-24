using NUnit.Framework;

// Etapa 3 (docs/superpowers/specs/2026-11-24-etapa-3-bau-vendedor-itens-design.md, 5.4/5.5 + decisões
// do Claude) — quando o baú e o vendedor aparecem: N entre mín/máx, um por faixa igual da run.
public class RewardScheduleTests
{
    [Test]
    public void Count_within_min_max()
    {
        for (int seed = 0; seed < 100; seed++)
        {
            var t = RewardSchedule.Build(4, 5, 1500f, 60f, new System.Random(seed));
            Assert.That(t.Length, Is.InRange(4, 5));
        }
    }

    [Test]
    public void Sorted_and_inside_its_band()
    {
        for (int seed = 0; seed < 100; seed++)
        {
            var t = RewardSchedule.Build(6, 8, 1500f, 60f, new System.Random(seed));
            float w = (1500f - 60f) / t.Length;
            for (int i = 0; i < t.Length; i++)
            {
                Assert.GreaterOrEqual(t[i], 60f + i * w);
                Assert.Less(t[i], 60f + (i + 1) * w);
                if (i > 0) Assert.Greater(t[i], t[i - 1]);
            }
        }
    }

    [Test]
    public void Nothing_before_quiet_seconds()
    {
        for (int seed = 0; seed < 100; seed++)
            foreach (var s in RewardSchedule.Build(4, 5, 1500f, 60f, new System.Random(seed)))
                Assert.GreaterOrEqual(s, 60f);
    }

    [Test]
    public void Min_greater_than_max_or_zero_is_safe()
    {
        Assert.AreEqual(0, RewardSchedule.Build(0, 0, 1500f, 60f, new System.Random(1)).Length);
        Assert.AreEqual(5, RewardSchedule.Build(5, 3, 1500f, 60f, new System.Random(1)).Length);
        Assert.AreEqual(0, RewardSchedule.Build(4, 5, 30f, 60f, new System.Random(1)).Length); // run menor que o silêncio
    }
}
