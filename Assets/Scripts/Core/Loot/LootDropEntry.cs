using UnityEngine;

// 1 linha do drop table de um monstro/boss (GDD Seção 38: "cada tipo de loot realiza sua
// própria rolagem de chance de drop, independentemente das demais" — um abate testa CADA
// entrada da lista separadamente, nunca escolhe 1 só item da tabela). `itemName` é string
// livre, igual LootDefinition.itemName/BagController.AddItem — sem SO dedicado por material,
// os 15 nomes oficiais vivem na GDD Seção 39 / LootPricingSettings.
[System.Serializable]
public class LootDropEntry
{
    public string itemName;

    [Range(0f, 100f)]
    public float dropChancePercent = 100f;

    // long (não int) — Tier 10 do Bestiário já passa de 2,1 bilhões por drop sozinho (ex.:
    // Monster Essence 50-500 bilhões), bem acima do teto do int32. Achado real (2026-10-25):
    // o Unity recusava os prefabs com "Failed to convert ... to a signed 32 bit int" assim que
    // o campo era int, mesmo a GDD já pedindo esses valores desde o Tier 5+.
    public long minQuantity = 1;
    public long maxQuantity = 1;
}
