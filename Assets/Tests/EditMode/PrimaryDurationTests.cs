using NUnit.Framework;

// Mage — regra do usuário (2026-11-25): cada animação do attack_continuous (12 quadros) é 1 ciclo de
// dano. Fogo: fire_start (quadros 1-4, sem dano) -> fire_continuous (dano) -> fire_end (3 últimos
// quadros da última animação, sem dano). Lasting Flames: +1 animação inteira por nível; nível 10 =
// infinito enquanto segura (soltou tarde demais = mais 1 animação inteira). O dano do ciclo é
// dividido em 3 pedaços da parte de dano daquela animação: 1 tick por pedaço por monstro.
public class PrimaryDurationTests
{
    [Test]
    public void Extra_cycles_are_the_card_level()
    {
        Assert.AreEqual(0, PrimaryDuration.ExtraCycles(0));
        Assert.AreEqual(1, PrimaryDuration.ExtraCycles(1));
        Assert.AreEqual(9, PrimaryDuration.ExtraCycles(9));
        Assert.AreEqual(PrimaryDuration.Infinite, PrimaryDuration.ExtraCycles(10));
    }

    [Test]
    public void Level_0_ends_on_the_first_animation()
    {
        Assert.IsTrue(PrimaryDuration.ShouldEnd(completedCycles: 1, extraCycles: 0, attackHeld: true));
    }

    [Test]
    public void One_card_runs_exactly_one_more_animation()
    {
        Assert.IsFalse(PrimaryDuration.ShouldEnd(1, 1, attackHeld: true));
        Assert.IsTrue(PrimaryDuration.ShouldEnd(2, 1, attackHeld: true));
    }

    [Test]
    public void Finite_ignores_the_button()
    {
        Assert.IsFalse(PrimaryDuration.ShouldEnd(1, 3, attackHeld: false));
    }

    [Test]
    public void Infinite_runs_while_held_and_ends_on_release()
    {
        Assert.IsFalse(PrimaryDuration.ShouldEnd(500, PrimaryDuration.Infinite, attackHeld: true));
        Assert.IsTrue(PrimaryDuration.ShouldEnd(500, PrimaryDuration.Infinite, attackHeld: false));
    }

    [Test]
    public void Last_cycle_is_known_only_when_finite()
    {
        Assert.IsTrue(PrimaryDuration.IsLastCycle(cycleIndex: 0, extraCycles: 0));
        Assert.IsFalse(PrimaryDuration.IsLastCycle(0, 2));
        Assert.IsTrue(PrimaryDuration.IsLastCycle(2, 2));
        Assert.IsFalse(PrimaryDuration.IsLastCycle(7, PrimaryDuration.Infinite));
    }

    // Parte de dano (fire_continuous) dentro dos 12 quadros da animação.
    [Test]
    public void Damage_frames_per_animation()
    {
        Assert.AreEqual(5, PrimaryDuration.DamageFrames(first: true, last: true));   // nível 1: 5-9
        Assert.AreEqual(8, PrimaryDuration.DamageFrames(first: true, last: false));  // 1ª de várias: 5-12
        Assert.AreEqual(12, PrimaryDuration.DamageFrames(first: false, last: false)); // do meio: 1-12
        Assert.AreEqual(9, PrimaryDuration.DamageFrames(first: false, last: true));  // última: 1-9
        Assert.AreEqual(4, PrimaryDuration.DamageStartFrame(first: true));
        Assert.AreEqual(0, PrimaryDuration.DamageStartFrame(first: false));
    }

    [Test]
    public void Damage_window_splits_in_three_pieces()
    {
        Assert.AreEqual(0, PrimaryDuration.Piece(0f, 0.3f));
        Assert.AreEqual(0, PrimaryDuration.Piece(0.099f, 0.3f));
        Assert.AreEqual(1, PrimaryDuration.Piece(0.1f, 0.3f));
        Assert.AreEqual(2, PrimaryDuration.Piece(0.25f, 0.3f));
        Assert.AreEqual(2, PrimaryDuration.Piece(5f, 0.3f));   // passou do fim: último pedaço
        Assert.AreEqual(0, PrimaryDuration.Piece(-1f, 0.3f));  // antes do começo: 1º pedaço
    }
}
