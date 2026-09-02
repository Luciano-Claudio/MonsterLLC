using NUnit.Framework;

public class ProgressTrackerTests
{
    [Test]
    public void EnemyKilled_IncrementsCounter()
    {
        ProgressTracker.ResetAll();
        ProgressTracker.Init();

        GameEvents.EnemyKilled(10); // valor de energia arbitrário — irrelevante para este teste

        Assert.AreEqual(1, ProgressTracker.Get("EnemyKilled"));
    }
}
