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

    public int minQuantity = 1;
    public int maxQuantity = 1;
}
