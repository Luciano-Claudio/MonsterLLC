using NUnit.Framework;
using UnityEngine;

public class PatrolAITests
{
    private static PatrolAI MakeAI(System.Random rng = null) => new PatrolAI(1f, 1f, 1f, 1f, rng);

    [Test]
    public void NewPatrolAI_StartsInIdle()
    {
        var ai = MakeAI();
        Assert.AreEqual(PatrolPhase.Idle, ai.CurrentPhase);
    }

    [Test]
    public void AlternatesIdleAndWalkingIndefinitely()
    {
        var ai = MakeAI();
        Assert.AreEqual(PatrolPhase.Idle, ai.CurrentPhase);

        ai.Tick(1.1f, () => Vector2.one);
        Assert.AreEqual(PatrolPhase.Walking, ai.CurrentPhase);

        ai.Tick(1.1f, () => Vector2.one);
        Assert.AreEqual(PatrolPhase.Idle, ai.CurrentPhase);

        ai.Tick(1.1f, () => Vector2.one);
        Assert.AreEqual(PatrolPhase.Walking, ai.CurrentPhase);
    }

    [Test]
    public void EnteringWalking_PicksTheGivenTarget()
    {
        var ai = MakeAI();
        var target = new Vector2(3f, 4f);

        ai.Tick(1.1f, () => target);

        Assert.AreEqual(PatrolPhase.Walking, ai.CurrentPhase);
        Assert.AreEqual(target, ai.WalkTarget);
    }

    [Test]
    public void PhaseDurations_AlwaysWithinConfiguredRange()
    {
        var rng = new System.Random(42);
        var ai = new PatrolAI(1f, 3f, 2f, 5f, rng);

        for (int i = 0; i < 200; i++)
        {
            var phaseBefore = ai.CurrentPhase;
            // Avança em passos pequenos até trocar de fase, contando quanto tempo levou.
            float elapsed = 0f;
            while (ai.CurrentPhase == phaseBefore && elapsed < 10f)
            {
                ai.Tick(0.05f, () => Vector2.one);
                elapsed += 0.05f;
            }

            if (phaseBefore == PatrolPhase.Idle)
            {
                Assert.GreaterOrEqual(elapsed, 1f - 0.05f);
                Assert.LessOrEqual(elapsed, 3f + 0.05f);
            }
            else
            {
                Assert.GreaterOrEqual(elapsed, 2f - 0.05f);
                Assert.LessOrEqual(elapsed, 5f + 0.05f);
            }
        }
    }
}
