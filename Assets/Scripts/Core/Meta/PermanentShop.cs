using System.Collections.Generic;

// Loja permanente — compra 1 nível por vez com o gold permanente, vende tudo de um herói (devolve 100%,
// inclusive as melhorias desligadas) e liga/desliga uma melhoria sem vender. Os níveis moram no
// profile.json (PermanentGold), por herói.
public static class PermanentShop
{
    public static int Level(string hero, string id) => PermanentGold.FindLevel(hero, id)?.level ?? 0;

    public static bool IsActive(string hero, string id) => !(PermanentGold.FindLevel(hero, id)?.disabled ?? false);

    // Nível que vale na run (0 se desligada no Toggle).
    public static int EffectiveLevel(string hero, string id) => IsActive(hero, id) ? Level(hero, id) : 0;

    // Preço do próximo nível; -1 se já está no máximo (ou sem preço configurado).
    public static int NextPrice(PermanentUpgradeDefinition d, int level)
    {
        if (d == null || level >= d.maxLevel || d.prices == null || level >= d.prices.Length) return -1;
        return d.prices[level];
    }

    public static long TotalSpent(PermanentUpgradeDefinition d, int level)
    {
        long sum = 0;
        if (d?.prices == null) return 0;
        for (int i = 0; i < level && i < d.prices.Length; i++) sum += d.prices[i];
        return sum;
    }

    public static bool CanBuy(string hero, PermanentUpgradeDefinition d)
    {
        int price = NextPrice(d, Level(hero, d?.id));
        return price >= 0 && PermanentGold.Amount >= price;
    }

    public static bool Buy(string hero, PermanentUpgradeDefinition d)
    {
        if (d == null || string.IsNullOrEmpty(hero)) return false;
        int price = NextPrice(d, Level(hero, d.id));
        if (price < 0 || !PermanentGold.TrySpend(price)) return false;
        PermanentGold.GetOrAddLevel(hero, d.id).level++;
        PermanentGold.SaveProfile();
        GameEvents.PermanentUpgradesChanged(hero);
        return true;
    }

    public static long RefundAll(string hero, IReadOnlyList<PermanentUpgradeDefinition> defs)
    {
        if (string.IsNullOrEmpty(hero) || defs == null) return 0;
        long total = 0;
        foreach (var d in defs)
        {
            if (d == null) continue;
            var entry = PermanentGold.FindLevel(hero, d.id);
            if (entry == null) continue;
            total += TotalSpent(d, entry.level);
            entry.level = 0;
            entry.disabled = false;
        }
        PermanentGold.Add(total); // Add já salva (e salva os níveis zerados junto)
        if (total == 0) PermanentGold.SaveProfile();
        GameEvents.PermanentUpgradesChanged(hero);
        return total;
    }

    public static void SetActive(string hero, string id, bool active)
    {
        if (string.IsNullOrEmpty(hero) || string.IsNullOrEmpty(id)) return;
        PermanentGold.GetOrAddLevel(hero, id).disabled = !active;
        PermanentGold.SaveProfile();
        GameEvents.PermanentUpgradesChanged(hero);
    }
}
