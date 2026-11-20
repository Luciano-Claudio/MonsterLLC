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
    public void AimLocked_DuringAction_ByDefault()
    {
        Assert.IsTrue(AttackWalkSwap.AimLocked(isAttacking: true, isAttackResolving: true, canAimDuringPrimaryAttack: false, trapped: false));
    }

    [Test]
    public void AimFree_DuringPrimary_WhenHeroAllows()
    {
        Assert.IsFalse(AttackWalkSwap.AimLocked(isAttacking: true, isAttackResolving: true, canAimDuringPrimaryAttack: true, trapped: false));
    }

    [Test]
    public void AimLocked_DuringNonPrimaryAction_EvenWhenHeroAllows()
    {
        // Ultimate/Shift: isAttacking sem isAttackResolving
        Assert.IsTrue(AttackWalkSwap.AimLocked(isAttacking: true, isAttackResolving: false, canAimDuringPrimaryAttack: true, trapped: false));
    }

    [Test]
    public void AimLocked_WhenTrapped()
    {
        Assert.IsTrue(AttackWalkSwap.AimLocked(isAttacking: true, isAttackResolving: true, canAimDuringPrimaryAttack: true, trapped: true));
    }

    [Test]
    public void AimFree_WhenIdle()
    {
        Assert.IsFalse(AttackWalkSwap.AimLocked(isAttacking: false, isAttackResolving: false, canAimDuringPrimaryAttack: false, trapped: false));
    }

    // Bug real (Rogue, 2026-11-20): durante o pulso o Rogue solta isAttacking (pra andar), e o
    // clique no primário passava — mesmo com a UI vermelha (isAttackResolving). O primário não
    // pode recomeçar enquanto o anterior ainda está resolvendo.
    [Test]
    public void CanStartPrimary_BlockedWhileResolving_EvenIfNotAttacking()
    {
        Assert.IsFalse(AttackWalkSwap.CanStartPrimary(isAttacking: false, isAttackResolving: true, trapped: false));
    }

    [Test]
    public void CanStartPrimary_FreeWhenIdle()
    {
        Assert.IsTrue(AttackWalkSwap.CanStartPrimary(isAttacking: false, isAttackResolving: false, trapped: false));
    }

    [Test]
    public void CanStartPrimary_BlockedWhileAttackingOrTrapped()
    {
        Assert.IsFalse(AttackWalkSwap.CanStartPrimary(isAttacking: true, isAttackResolving: false, trapped: false));
        Assert.IsFalse(AttackWalkSwap.CanStartPrimary(isAttacking: false, isAttackResolving: false, trapped: true));
    }

    // Revisão final D9.1 (achado Important #2): controller sem estado Trapped (urso do Druid,
    // Paladin) deixava o herói em "Walk" e ele andava preso na teia.
    [Test]
    public void CanMove_TrappedNeverMoves_EvenInWalkState()
    {
        Assert.IsFalse(AttackWalkSwap.CanMove(wantsToMove: true, inWalkState: true, attackMoveAllowed: false, trapped: true));
    }

    [Test]
    public void CanMove_WalkingFreely()
    {
        Assert.IsTrue(AttackWalkSwap.CanMove(wantsToMove: true, inWalkState: true, attackMoveAllowed: false, trapped: false));
    }

    [Test]
    public void CanMove_DuringWalkableAttack()
    {
        Assert.IsTrue(AttackWalkSwap.CanMove(wantsToMove: true, inWalkState: false, attackMoveAllowed: true, trapped: false));
    }

    [Test]
    public void CanMove_NoInput_DoesNotMove()
    {
        Assert.IsFalse(AttackWalkSwap.CanMove(wantsToMove: false, inWalkState: true, attackMoveAllowed: true, trapped: false));
    }

    // Prisão durante o pulso do Rogue zerava isAttackResolving e reabria o clique no meio do
    // pulso. Primário ainda rodando (pulso) = continua resolvendo.
    [Test]
    public void Trap_KeepsResolving_WhilePrimaryStillRunning()
    {
        Assert.IsFalse(AttackWalkSwap.ClearsResolvingOnTrap(primaryStillRunning: true));
    }

    [Test]
    public void Trap_ClearsResolving_ForNormalAttack()
    {
        Assert.IsTrue(AttackWalkSwap.ClearsResolvingOnTrap(primaryStillRunning: false));
    }

    [Test]
    public void ContinueTime_NonLooping_ReturnsSame()
    {
        Assert.AreEqual(0.4f, AttackWalkSwap.ContinueTime(0.4f, false), 0.0001f);
    }
}
