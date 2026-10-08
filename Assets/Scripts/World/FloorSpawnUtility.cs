using System.Collections.Generic;
using UnityEngine;
using Pathfinding;

// Extraído do FloorPopulationManager na Sprint 24 — o BossSpawnManager precisa exatamente
// da mesma lógica de "ponto caminhável aleatório dentro do GridGraph do Floor" que o spawn
// de monstro comum já usava. Comportamento idêntico ao que já existia, só de local novo.
public static class FloorSpawnUtility
{
    public static bool TryGetRandomWalkablePoint(FloorDefinition ownerFloor, ref List<GraphNode> cachedWalkableNodes, out Vector3 point)
    {
        point = default;

        if (cachedWalkableNodes == null)
        {
            var graph = GetOwnerGraph(ownerFloor);
            if (graph == null) return false;

            // Variável local, não o parâmetro ref direto — ref não pode ser capturado
            // dentro da lambda de GetNodes() (CS1628). Só atribui de volta depois de
            // preenchida.
            var walkableNodes = new List<GraphNode>();
            graph.GetNodes(node => { if (node.Walkable) walkableNodes.Add(node); });
            cachedWalkableNodes = walkableNodes;
        }

        if (cachedWalkableNodes.Count == 0) return false;

        point = (Vector3)PathUtilities.GetPointsOnNodes(cachedWalkableNodes, 1)[0];
        return true;
    }

    public static NavGraph GetOwnerGraph(FloorDefinition ownerFloor)
    {
        if (ownerFloor == null || string.IsNullOrEmpty(ownerFloor.astarGraphName)) return null;
        if (AstarPath.active == null) return null;
        return AstarPath.active.data.FindGraph(g => g.name == ownerFloor.astarGraphName);
    }

    public static void RestrictToOwnerGraph(GameObject obj, FloorDefinition ownerFloor)
    {
        var graph = GetOwnerGraph(ownerFloor);
        if (graph == null) return;

        var seeker = obj.GetComponent<Seeker>();
        if (seeker != null) seeker.graphMask = GraphMask.FromGraph(graph);
    }
}
