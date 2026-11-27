using System.Collections.Generic;
using UnityEngine;

// Lista das melhorias de cada herói (Resources/PermanentUpgradeCatalog). Os 10 heróis têm entrada (para o
// ícone do Select_Confirm); só Barbarian, Ranger e Mage têm melhorias na alpha — sem melhorias, sem loja.
[CreateAssetMenu(menuName = "MonsterLLC/Permanent Upgrade Catalog", fileName = "PermanentUpgradeCatalog")]
public class PermanentUpgradeCatalog : ScriptableObject
{
    [System.Serializable]
    public class HeroUpgrades
    {
        public string heroKey;
        [Tooltip("Imagem do herói no Stats_Painel (Hero_Image) — 1º quadro do idle.")]
        public Sprite heroImage;
        [Tooltip("Ícone do herói no Select_Confirm (Icon_Hero). Vazio = usa o heroImage.")]
        public Sprite confirmIcon;
        public List<PermanentUpgradeDefinition> upgrades = new();
    }

    public List<HeroUpgrades> heroes = new();

    private static PermanentUpgradeCatalog cached;
    public static PermanentUpgradeCatalog Load() => cached != null ? cached : cached = Resources.Load<PermanentUpgradeCatalog>("PermanentUpgradeCatalog");

    private static readonly List<PermanentUpgradeDefinition> Empty = new();

    public HeroUpgrades Find(string heroKey)
    {
        foreach (var h in heroes) if (h != null && h.heroKey == heroKey) return h;
        return null;
    }

    public IReadOnlyList<PermanentUpgradeDefinition> For(string heroKey)
    {
        foreach (var h in heroes)
            if (h != null && h.heroKey == heroKey) return h.upgrades;
        return Empty;
    }
}
