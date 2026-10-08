using System.Collections.Generic;
using UnityEngine;
using Pathfinding;

// Boss Timer — regra final (GDD Seção 22, correção Sprint 24, decisão do usuário). Mesmo
// padrão arquitetural do FloorPopulationManager (componente irmão de FloorDefinition, nunca
// destruído/recriado ao trocar de Floor, gateado por FloorActivationCheck — "congela"
// sozinho quando o jogador sai do Floor, sem precisar de nenhum save/load explícito), mas é
// um sistema PARALELO e independente da população comum: bosses não entram em
// aliveEnemies/Minimum/Target/Maximum, e não existe fase de "preenchimento instantâneo" ao
// entrar no Floor — é só um relógio fixo que nunca para enquanto o Floor está ativo.
public class BossSpawnManager : MonoBehaviour
{
    // Vazio = Floor sem boss AINDA PRODUZIDO (Floor 3+ antes da Deadline 8) — estado de
    // produção incompleta, não uma feature opt-in (decisão do usuário, GDD Seção 22).
    public GameObject[] bossPrefabs;
    public FloorDefinition ownerFloor;

    public float bossSpawnInterval = 10f; // 🔢 GDD Seção 22 — intervalo fixo, placeholder de teste

    private float bossSpawnTimer;
    private List<GameObject> rotationOrder;
    private int rotationIndex;
    private List<GraphNode> cachedWalkableNodes; // mesmo cache-sob-demanda do FloorPopulationManager, instância própria

    private void Update()
    {
        if (!GameplayGate.IsActive) return;
        if (!FloorActivationCheck.IsActive(ownerFloor, FloorManager.Instance.CurrentFloor)) return;
        if (bossPrefabs.Length == 0) return;

        bossSpawnTimer += Time.deltaTime;
        if (bossSpawnTimer < bossSpawnInterval) return;

        // Subtrai o intervalo (não zera cru) — preserva o excedente de um frame com
        // deltaTime maior que o normal, em vez de perder aquela fração pra sempre. O
        // relógio é "fixo e contínuo" (GDD Seção 22) — acúmulo de frame não pode ir
        // empurrando o próximo spawn pra mais tarde, por menor que seja o desvio.
        bossSpawnTimer -= bossSpawnInterval;

        SpawnNextBossInRotation();
    }

    // Decisão do usuário (GDD Seção 22, correção Sprint 24): a ordem só é sorteada 1x — com
    // N bosses, depois que todos já nasceram 1x cada, repete a MESMA ordem pra sempre, nunca
    // sorteia de novo. Lista de 1 elemento "embaralhada" sempre resulta nela mesma, cobrindo
    // o caso degenerado de N=1 sem nenhuma exceção de código.
    private void BuildRotationIfNeeded()
    {
        if (rotationOrder != null) return;

        rotationOrder = new List<GameObject>(bossPrefabs);
        for (int i = rotationOrder.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (rotationOrder[i], rotationOrder[j]) = (rotationOrder[j], rotationOrder[i]);
        }
    }

    private void SpawnNextBossInRotation()
    {
        BuildRotationIfNeeded();

        var prefab = rotationOrder[rotationIndex];
        rotationIndex = (rotationIndex + 1) % rotationOrder.Count;
        if (prefab == null) return;

        // Mesma posição de monstro comum (GDD Seção 23, "Onde monstros nascem") — qualquer
        // ponto caminhável do Floor, não um ponto fixo de arena (decisão do usuário).
        if (!FloorSpawnUtility.TryGetRandomWalkablePoint(ownerFloor, ref cachedWalkableNodes, out var spawnPosition)) return;

        var bossObj = Instantiate(prefab, spawnPosition, Quaternion.identity);
        var enemyController = bossObj.GetComponent<EnemyController>();
        if (enemyController == null) return;

        enemyController.ownerFloor = ownerFloor;
        FloorSpawnUtility.RestrictToOwnerGraph(bossObj, ownerFloor);

        enemyController.SetIsBoss(true);
        enemyController.ForceImmediateAggro(); // decisão do usuário — sem fase de patrulha/idle

        // Decisão do usuário: bosses EMPILHAM — sem checagem de "já tem boss vivo", sem
        // fila, sem limite de bosses simultâneos. O timer já foi decrementado acima,
        // independente deste spawn ter dado certo ou não (não reseta com morte nem com
        // sucesso/falha de spawn).
    }
}
