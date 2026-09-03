using System.Collections.Generic;
using UnityEngine;

public class FloorPopulationManager : MonoBehaviour
{
    public PopulationConfig config = new PopulationConfig();
    public GameObject meleePrefab;
    public GameObject rangedPrefab;
    public Transform[] spawnPoints;
    public float respawnInterval = 3f; // 🔢 GDD — frequência de reposição, placeholder

    private List<GameObject> aliveEnemies = new();
    private float respawnTimer;

    private void Update()
    {
        if (!GameplayGate.IsActive) return;

        aliveEnemies.RemoveAll(e => e == null); // remove os que já morreram

        if (aliveEnemies.Count >= config.target) return;

        respawnTimer += Time.deltaTime;
        if (respawnTimer < respawnInterval) return;
        respawnTimer = 0f;

        SpawnOne();
    }

    private void SpawnOne()
    {
        if (aliveEnemies.Count >= config.maximum) return;
        if (spawnPoints.Length == 0) return;

        var point = spawnPoints[Random.Range(0, spawnPoints.Length)];
        var prefab = Random.value < 0.5f ? meleePrefab : rangedPrefab;
        if (prefab == null) return;

        aliveEnemies.Add(Instantiate(prefab, point.position, Quaternion.identity));
    }
}
