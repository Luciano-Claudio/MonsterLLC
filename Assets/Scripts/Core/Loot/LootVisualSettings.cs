using System.Collections.Generic;
using UnityEngine;

// 1 entrada por material — os 4 sprites de pilha (1 / 2+ / 5+ / 20+, GDD Seção 38) de UM item.
[System.Serializable]
public class LootVisualEntry
{
    public string itemName;
    public Sprite tier1;
    public Sprite tier2Plus;
    public Sprite tier5Plus;
    public Sprite tier20Plus;
}

// Qual sprite de pilha usar pra uma quantidade — mesma regra pra ícone de inventário (adiantado,
// Sprint 42) e pro drop físico no chão (Sprint 32), os 2 têm que ficar visualmente idênticos.
// Limiares serializados (GDD Seção 38: "configuráveis, não fixos em código").
[CreateAssetMenu(fileName = "LootVisualSettings", menuName = "MonsterLLC/Loot/Loot Visual Settings")]
public class LootVisualSettings : ScriptableObject
{
    // LootDrop (Sprint 32) nasce via `new GameObject()`/`AddComponent` em EnemyController, sem
    // nenhum slot de Inspector pra arrastar este asset — por isso o acesso global por aqui.
    // OnEnable roda quando o asset é carregado (já é referenciado por InventoryHudUI/
    // InventoryTabUI, então carrega na mesma hora que a cena), sem precisar de Resources.Load
    // nem de outro singleton MonoBehaviour só pra isso.
    public static LootVisualSettings Instance { get; private set; }
    private void OnEnable() => Instance = this;

    public List<LootVisualEntry> icons = new();

    public int tier2Threshold = 2;
    public int tier5Threshold = 5;
    public int tier20Threshold = 20;

    public Sprite GetSprite(string itemName, int quantity)
    {
        foreach (var entry in icons)
        {
            if (entry.itemName != itemName) continue;

            if (quantity >= tier20Threshold) return entry.tier20Plus;
            if (quantity >= tier5Threshold) return entry.tier5Plus;
            if (quantity >= tier2Threshold) return entry.tier2Plus;
            return entry.tier1;
        }

        return null;
    }
}
