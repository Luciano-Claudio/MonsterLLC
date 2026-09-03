[System.Serializable]
public class RunState
{
    public string mode = "Standard";
    public string hero = "Barbarian";
    public string map = "Tower";
    public int day = 1;
    public long gold = 0;

    // Stub — nao e o sistema real de Weapon Tier (15 tiers, Deadline 9, Sprint 33).
    // So o suficiente pra provar que uma compra na Loja sobrevive ao Save/Load.
    public string weaponTier = "Basic";
}
