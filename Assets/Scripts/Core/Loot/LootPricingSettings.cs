using System.Collections.Generic;
using UnityEngine;

// 1 linha de preço (GDD Seção 39) — fora da classe SO pra servir de entrada de lista normal.
[System.Serializable]
public class LootPriceEntry
{
    public string itemName;
    public long sellValue;
}

// Valor de venda dos 15 materiais econômicos (GDD Seção 39/`economy-balance.md` Seção 5) — vive
// num asset, não hardcoded, pro mesmo motivo de qualquer outro `XxxSettings` do projeto
// (ajustável sem recompilar). `Vendor` consulta isto pra saber quanto pagar por qualquer
// material que o jogador tenha na Bag, não só "Monster Essence" (achado real, Sprint 31: o
// Vendor antigo era hardcoded só pra esse 1 item).
[CreateAssetMenu(fileName = "LootPricingSettings", menuName = "MonsterLLC/Loot/Loot Pricing Settings")]
public class LootPricingSettings : ScriptableObject
{
    public List<LootPriceEntry> prices = new();

    public bool TryGetPrice(string itemName, out long sellValue)
    {
        foreach (var entry in prices)
        {
            if (entry.itemName != itemName) continue;
            sellValue = entry.sellValue;
            return true;
        }

        sellValue = 0;
        return false;
    }
}
