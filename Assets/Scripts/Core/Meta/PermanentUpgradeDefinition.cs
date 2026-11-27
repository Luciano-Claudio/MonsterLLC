using UnityEngine;

// Loja permanente (docs/superpowers/specs/2026-11-26-loja-permanente-design.md) — o que cada melhoria
// muda na run (lido pelo PermanentBonuses).
public enum PermanentStat
{
    Damage, MaxHealth, Armor, Regen, MoveSpeed, Cooldown, UltimateCharge, UltimateCost, Magnet, Xp, Gold,
    Luck, Curse, Revival, Reroll, Size, Multishot, FlameArea,
}

// 1 melhoria comprável no menu. buttonName = nome do botão no Stats_Painel (o código acha o botão por ele).
[CreateAssetMenu(menuName = "MonsterLLC/Permanent Upgrade", fileName = "perm_")]
public class PermanentUpgradeDefinition : ScriptableObject
{
    public string id;
    public string buttonName;
    public string title;
    [TextArea] public string description;
    public Sprite icon;
    public PermanentStat stat;
    [Tooltip("Pips no menu = nível máximo.")]
    public int maxLevel = 5;
    [Tooltip("Valor de cada nível (ex.: 0,10 = +10%).")]
    public float valuePerLevel = 0.1f;
    [Tooltip("Preço de cada nível, em ordem (precisa de pelo menos maxLevel valores).")]
    public int[] prices = { 50, 100, 300, 500, 1000 };
}
