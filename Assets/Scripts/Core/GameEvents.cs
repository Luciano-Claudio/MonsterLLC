using System;
using UnityEngine;

public static class GameEvents
{
    public static event Action OnEnemyKilled;
    public static void EnemyKilled() => OnEnemyKilled?.Invoke();

    public static event Action<FloorDefinition> OnFloorChanged;
    public static void FloorChanged(FloorDefinition floor) => OnFloorChanged?.Invoke(floor);

    // anchor == null significa "esconder o prompt".
    public static event Action<Transform> OnInteractPromptChanged;
    public static void InteractPromptChanged(Transform anchor) => OnInteractPromptChanged?.Invoke(anchor);

    // Mais eventos entram aqui conforme os sistemas nascerem.
    // Nenhum outro script deve declarar um event solto — tudo passa por aqui.
}
