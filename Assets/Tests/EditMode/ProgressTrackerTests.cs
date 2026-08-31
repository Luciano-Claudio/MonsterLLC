using NUnit.Framework;

public class ProgressTrackerTests
{
    [Test]
    public void EnemyKilled_IncrementsCounter()
    {
        ProgressTracker.ResetAll();
        ProgressTracker.Init();

        GameEvents.EnemyKilled();

        Assert.AreEqual(1, ProgressTracker.Get("EnemyKilled"));
    }
}
