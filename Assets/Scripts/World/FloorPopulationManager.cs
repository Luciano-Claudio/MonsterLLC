using System.Collections.Generic;
using UnityEngine;

public class FloorPopulationManager : MonoBehaviour
{
    public PopulationConfig config = new PopulationConfig();
    public GameObject meleePrefab;
    public GameObject rangedPrefab;
    public Transform[] spawnPoints;
    public float respawnInterval = 3f; // 🔢 GDD — frequência de reposição, placeholder
    public FloorDefinition ownerFloor;

    private List<GameObject> aliveEnemies = new();
    private float respawnTimer;

    private void Update()
    {
        if (!GameplayGate.IsActive) return;
        if (!FloorActivationCheck.IsActive(ownerFloor, FloorManager.Instance.CurrentFloor)) return;

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

        // Desvio aleatório em torno do spawnPoint — sem isso, inimigos parados (fora do
        // observationRadius, nunca se movendo) ficam empilhados exatamente no mesmo ponto,
        // já que o Rigidbody2D só separa corpos que estão de fato se movendo.
        Vector3 offset = new Vector3(Random.Range(0f, 2f), Random.Range(0f, 2f), 0f);
        var enemyObj = Instantiate(prefab, point.position + offset, Quaternion.identity);
        var enemyController = enemyObj.GetComponent<EnemyController>();
        if (enemyController != null) enemyController.ownerFloor = ownerFloor;

        aliveEnemies.Add(enemyObj);
    }
}
