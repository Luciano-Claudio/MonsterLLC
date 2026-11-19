using NUnit.Framework;

// D9.1 (Atacar andando) — decisão pura de troca entre o estado de ataque parado e o gêmeo
// "_Walk", sem Animator (ver AttackWalkSwap).
public class AttackWalkSwapTests
{
    private static AttackWalkAction Decide(
        bool wantsToMove = true, bool trapped = false, bool actionAllowsWalk = true,
        bool inStandState = true, bool inWalkTwin = false, bool counterpartExists = true,
        float normalizedTime = 0.4f, bool looping = false) =>
        AttackWalkSwap.Decide(wantsToMove, trapped, actionAllowsWalk, inStandState, inWalkTwin,
            counterpartExists, normalizedTime, looping);

    [Test]
    public void Decide_StartsWalking_InStandMidClip_ReturnsToWalk()
    {
        Assert.AreEqual(AttackWalkAction.ToWalk, Decide());
    }

    [Test]
    public void Decide_StopsWalking_InWalkTwin_ReturnsToStand()
    {
        Assert.AreEqual(AttackWalkAction.ToStand, Decide(wantsToMove: false, inStandState: false, inWalkTwin: true));
    }

    [Test]
    public void Decide_ClipFinished_NonLooping_ReturnsNone()
    {
        Assert.AreEqual(AttackWalkAction.None, Decide(normalizedTime: 1.0f));
    }

    [Test]
    public void Decide_Looping_PastOne_StillSwaps()
    {
        Assert.AreEqual(AttackWalkAction.ToWalk, Decide(normalizedTime: 3.25f, looping: true));
    }

    [Test]
    public void Decide_ActionDisallowsWalk_ReturnsNone()
    {
        Assert.AreEqual(AttackWalkAction.None, Decide(actionAllowsWalk: false));
    }

    [Test]
    public void Decide_CounterpartMissing_ReturnsNone()
    {
        Assert.AreEqual(AttackWalkAction.None, Decide(counterpartExists: false));
    }

    [Test]
    public void Decide_Trapped_ReturnsNone()
    {
        Assert.AreEqual(AttackWalkAction.None, Decide(trapped: true));
    }

    [Test]
    public void ContinueTime_Looping_ReturnsFraction()
    {
        Assert.AreEqual(0.25f, AttackWalkSwap.ContinueTime(3.25f, true), 0.0001f);
    }

    [Test]
    public void ContinueTime_NonLooping_ReturnsSame()
    {
        Assert.AreEqual(0.4f, AttackWalkSwap.ContinueTime(0.4f, false), 0.0001f);
    }
}
