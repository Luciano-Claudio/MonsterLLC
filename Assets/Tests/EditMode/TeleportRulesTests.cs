using NUnit.Framework;

// Teleporte (Mage e Assassin), regra do usuário (2026-11-27): atravessa obstáculos, mas nunca pousa dentro
// de um. Mirou num obstáculo: vai pro outro lado se o alcance der; senão fica na borda de cá.
public class TeleportRulesTests
{
    // "Lago" entre 2 e 4 (distâncias na direção da mira).
    private static bool Lake(float t) => t < 2f || t > 4f;

    [Test]
    public void Free_aim_point_lands_there_even_past_an_obstacle()
    {
        Assert.AreEqual(1f, TeleportRules.ResolveDistance(1f, 6f, Lake), 1e-3f);
        Assert.AreEqual(5f, TeleportRules.ResolveDistance(5f, 6f, Lake), 1e-3f);
    }

    [Test]
    public void Aim_inside_obstacle_goes_to_the_other_side_when_in_range()
    {
        float d = TeleportRules.ResolveDistance(3f, 6f, Lake);
        Assert.Greater(d, 4f);
        Assert.LessOrEqual(d, 4.2f);
    }

    [Test]
    public void Aim_inside_obstacle_without_range_stays_at_the_near_edge()
    {
        float d = TeleportRules.ResolveDistance(3f, 3.5f, Lake);
        Assert.Less(d, 2f);
        Assert.GreaterOrEqual(d, 1.8f);
    }

    [Test]
    public void Aim_is_clamped_to_max_range()
    {
        Assert.AreEqual(6f, TeleportRules.ResolveDistance(50f, 6f, Lake), 1e-3f);
    }

    [Test]
    public void Nothing_free_stays_in_place()
    {
        Assert.AreEqual(0f, TeleportRules.ResolveDistance(3f, 6f, t => t <= 0f), 1e-3f);
    }
}
