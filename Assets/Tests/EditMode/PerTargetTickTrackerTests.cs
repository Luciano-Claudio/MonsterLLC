using NUnit.Framework;

// Dano por tick contado POR MONSTRO (Mage, depois o Rogue): o monstro leva dano na hora em que
// entra na área pela 1ª vez e de novo a cada intervalo enquanto continuar dentro. Sair e entrar
// de novo não zera o timer.
public class PerTargetTickTrackerTests
{
    private const float Interval = 0.25f;

    [Test]
    public void FirstContact_HitsImmediately()
    {
        var tracker = new PerTargetTickTracker(Interval);
        Assert.IsTrue(tracker.TryHit(1, 10f));
    }

    [Test]
    public void SameTarget_WithinInterval_DoesNotHit()
    {
        var tracker = new PerTargetTickTracker(Interval);
        tracker.TryHit(1, 10f);
        Assert.IsFalse(tracker.TryHit(1, 10.2f));
    }

    [Test]
    public void SameTarget_AfterInterval_HitsAgain()
    {
        var tracker = new PerTargetTickTracker(Interval);
        tracker.TryHit(1, 10f);
        Assert.IsTrue(tracker.TryHit(1, 10.25f));
    }

    [Test]
    public void DifferentTargets_HaveIndependentTimers()
    {
        var tracker = new PerTargetTickTracker(Interval);
        tracker.TryHit(1, 10f);
        Assert.IsTrue(tracker.TryHit(2, 10.1f));
    }

    [Test]
    public void ReEnteringQuickly_DoesNotResetTimer()
    {
        // monstro sai (não é consultado) e volta antes do intervalo — continua esperando
        var tracker = new PerTargetTickTracker(Interval);
        tracker.TryHit(1, 10f);
        Assert.IsFalse(tracker.TryHit(1, 10.1f));
    }

    [Test]
    public void Prune_RemovesExpiredEntries()
    {
        var tracker = new PerTargetTickTracker(Interval);
        tracker.TryHit(1, 10f);
        tracker.TryHit(2, 10.2f);
        tracker.Prune(10.3f);
        Assert.AreEqual(1, tracker.Count);
    }

    [Test]
    public void SetInterval_AppliesToNextCheck()
    {
        var tracker = new PerTargetTickTracker(Interval);
        tracker.TryHit(1, 10f);
        tracker.SetInterval(0.1f);
        Assert.IsTrue(tracker.TryHit(1, 10.15f));
    }
}
