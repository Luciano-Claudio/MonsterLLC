using NUnit.Framework;

// D9.4 (pedido do usuário, 2026-11-21): aviso de explosão em 2 círculos — a área completa, fraca,
// e um círculo mais forte que cresce do centro até preencher a área no instante da explosão.
public class ExplosionTelegraphTests
{
    [Test] public void Progress_StartsAtZero() => Assert.AreEqual(0f, TelegraphRules.Progress(0f, 1.2f), 1e-5f);

    [Test] public void Progress_HalfWay() => Assert.AreEqual(0.5f, TelegraphRules.Progress(0.6f, 1.2f), 1e-5f);

    [Test] public void Progress_FullAtExplosion() => Assert.AreEqual(1f, TelegraphRules.Progress(1.2f, 1.2f), 1e-5f);

    [Test] public void Progress_NeverPassesFull() => Assert.AreEqual(1f, TelegraphRules.Progress(3f, 1.2f), 1e-5f);

    [Test] public void Progress_ZeroDuration_IsFull() => Assert.AreEqual(1f, TelegraphRules.Progress(0f, 0f), 1e-5f);
}
