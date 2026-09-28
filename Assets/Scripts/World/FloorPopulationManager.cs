using System.Collections.Generic;
using UnityEngine;
using Pathfinding;

public class FloorPopulationManager : MonoBehaviour
{
    public PopulationConfig config = new PopulationConfig(); // minimum/target/maximum — ver Active Minimum/Target abaixo pra como isso é usado agora
    public GameObject[] monsterPrefabs; // qualquer quantidade/tipo de monstro — sorteia 1 por spawn

    public float respawnInterval = 3f; // 🔢 GDD — cooldown do repovoamento normal (1 por vez); ignorado nos 3 cenários instantâneos abaixo
    public FloorDefinition ownerFloor;

    [Header("Escalação — crise repetida sobe o patamar permanentemente")]
    [SerializeField] private int crisisStreakToEscalate = 3; // 🔢 quantas crises seguidas (bateu no minimum) até minimum/target subirem de patamar

    private List<GameObject> aliveEnemies = new();
    private float respawnTimer;
    private List<GraphNode> cachedWalkableNodes; // populado sob demanda — o GridGraph do Floor só existe depois do Scan (Editor)

    private bool wasFloorActive;
    private bool escalated; // true depois que minimum virou o antigo target e target virou o antigo maximum — só acontece 1 vez, maximum é teto
    private int crisisStreak; // quantas vezes seguidas bateu no minimum e foi reabastecido, desde a última escalação

    private int ActiveMinimum => escalated ? config.target : config.minimum;
    private int ActiveTarget => escalated ? config.maximum : config.target;

    private void Update()
    {
        if (!GameplayGate.IsActive) return;

        // Detecta a borda "acabou de entrar" (Floor virou o atual agora, não estava antes) —
        // é o gatilho do preenchimento instantâneo até o target, independente de já ter
        // visitado esse Floor antes ou não.
        bool floorActive = FloorActivationCheck.IsActive(ownerFloor, FloorManager.Instance.CurrentFloor);
        if (floorActive && !wasFloorActive) FillToTarget();
        wasFloorActive = floorActive;

        if (!floorActive) return;

        aliveEnemies.RemoveAll(e => e == null); // remove os que já morreram

        if (aliveEnemies.Count <= ActiveMinimum)
        {
            HandleCrisis();
            return;
        }

        if (aliveEnemies.Count >= ActiveTarget) return;

        respawnTimer += Time.deltaTime;
        if (respawnTimer < respawnInterval) return;
        respawnTimer = 0f;

        SpawnOne();
    }

    // Crise: caiu até o minimum (ou abaixo). Reabastece até o target na hora, sem cooldown, e
    // conta mais 1 na sequência — se acumular crisisStreakToEscalate crises seguidas, o
    // patamar sobe de vez (minimum := target antigo, target := maximum antigo) e a sequência
    // zera. Só escala 1 vez — maximum é teto, não existe um patamar acima dele.
    private void HandleCrisis()
    {
        crisisStreak++;
        if (!escalated && crisisStreak >= crisisStreakToEscalate)
        {
            escalated = true;
            crisisStreak = 0;
        }

        FillToTarget();
    }

    // Preenche instantaneamente até o ActiveTarget, ignorando o cooldown normal — usado tanto
    // ao entrar no Floor quanto numa crise (Seção "Crise" acima).
    private void FillToTarget()
    {
        aliveEnemies.RemoveAll(e => e == null);
        int missing = ActiveTarget - aliveEnemies.Count;
        for (int i = 0; i < missing; i++) SpawnOne();
    }

    private void SpawnOne()
    {
        if (monsterPrefabs.Length == 0) return;
        if (!TryGetRandomGraphPoint(out var spawnPosition)) return;

        var prefab = monsterPrefabs[Random.Range(0, monsterPrefabs.Length)];
        if (prefab == null) return;

        var enemyObj = Instantiate(prefab, spawnPosition, Quaternion.identity);
        var enemyController = enemyObj.GetComponent<EnemyController>();
        if (enemyController != null) enemyController.ownerFloor = ownerFloor;

        RestrictToOwnerGraph(enemyObj);

        aliveEnemies.Add(enemyObj);
    }

    // Sorteia um ponto caminhável dentro do próprio GridGraph do Floor — substitui a
    // necessidade de vários spawnPoints colocados à mão. Só sabe quais pontos são "sala, não
    // parede" porque o GridGraph já escaneou os colisores no Scan (Editor); com colliders
    // faltando/parciais (você mencionou que alguns detalhes de Floor ainda não têm), o graph
    // simplesmente considera aquela área caminhável até o collider real ser adicionado e o
    // graph reescaneado — nada quebra, só o "chão" pode nascer maior do que a arte final.
    private bool TryGetRandomGraphPoint(out Vector3 point)
    {
        point = default;

        if (cachedWalkableNodes == null)
        {
            var graph = GetOwnerGraph();
            if (graph == null) return false;

            cachedWalkableNodes = new List<GraphNode>();
            graph.GetNodes(node => { if (node.Walkable) cachedWalkableNodes.Add(node); });
        }

        if (cachedWalkableNodes.Count == 0) return false;

        point = (Vector3)PathUtilities.GetPointsOnNodes(cachedWalkableNodes, 1)[0];
        return true;
    }

    private NavGraph GetOwnerGraph()
    {
        if (ownerFloor == null || string.IsNullOrEmpty(ownerFloor.astarGraphName)) return null;
        if (AstarPath.active == null) return null;
        return AstarPath.active.data.FindGraph(g => g.name == ownerFloor.astarGraphName);
    }

    // Restringe a busca de path do Seeker ao GridGraph do próprio Floor (evita, por exemplo,
    // um monstro do Floor 2 encontrar um nó de path pertencente ao Floor 1). Precisa do
    // AstarPath da cena já configurado (Editor) com um graph batizado igual a
    // ownerFloor.astarGraphName — sem isso (ainda não configurado, ou nome não bate), o
    // Seeker fica sem restrição (busca em todos os graphs), estado seguro de fallback.
    private void RestrictToOwnerGraph(GameObject enemyObj)
    {
        var graph = GetOwnerGraph();
        if (graph == null) return;

        var seeker = enemyObj.GetComponent<Seeker>();
        if (seeker != null) seeker.graphMask = GraphMask.FromGraph(graph);
    }
}
