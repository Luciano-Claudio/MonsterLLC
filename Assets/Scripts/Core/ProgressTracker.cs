using UnityEngine;
using System.Collections.Generic;

public static class ProgressTracker
{
    private static Dictionary<string, int> counters = new();
    private static bool initialized;

    public static void Init()
    {
        if (initialized) return;
        initialized = true;
        GameEvents.OnEnemyKilled += () => Increment("EnemyKilled");
    }

    public static void Increment(string key)
    {
        if (!counters.ContainsKey(key)) counters[key] = 0;
        counters[key]++;
        Debug.Log($"[ProgressTracker] {key} = {counters[key]}");
    }

    public static int Get(string key) => counters.TryGetValue(key, out var v) ? v : 0;
    public static void ResetAll() => counters.Clear(); // usado nos testes
}
