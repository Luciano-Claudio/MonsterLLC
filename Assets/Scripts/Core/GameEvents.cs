using System;
using UnityEngine;

public static class GameEvents
{
    // Sprint 7: passou a carregar o valor de energia do kill (GDD Seção 11 —
    // monstros mais fortes concedem mais Energia por kill). Quebra de assinatura proposital.
    public static event Action<int> OnEnemyKilled;
    public static void EnemyKilled(int energyValue) => OnEnemyKilled?.Invoke(energyValue);

    public static event Action<float, float> OnEnergyChanged;
    public static void EnergyChanged(float current, float max) => OnEnergyChanged?.Invoke(current, max);

    public static event Action<float, float> OnHealthChanged;
    public static void HealthChanged(float current, float max) => OnHealthChanged?.Invoke(current, max);

    public static event Action<FloorDefinition> OnFloorChanged;
    public static void FloorChanged(FloorDefinition floor) => OnFloorChanged?.Invoke(floor);

    public static event Action<LootDefinition> OnLootCollected;
    public static void LootCollected(LootDefinition loot) => OnLootCollected?.Invoke(loot);

    public static event Action<Bag> OnBagChanged;
    public static void BagChanged(Bag bag) => OnBagChanged?.Invoke(bag);

    // long, não int — Gold usa o mesmo tipo de RunState.gold (Sprint 4), sem teto de ~2,1bi.
    public static event Action<long> OnGoldChanged;
    public static void GoldChanged(long gold) => OnGoldChanged?.Invoke(gold);

    // anchor == null significa "esconder o prompt".
    public static event Action<Transform> OnInteractPromptChanged;
    public static void InteractPromptChanged(Transform anchor) => OnInteractPromptChanged?.Invoke(anchor);

    // Mais eventos entram aqui conforme os sistemas nascerem.
    // Nenhum outro script deve declarar um event solto — tudo passa por aqui.
}
