using UnityEngine;
using Pathfinding;

// Substitui FloorSpawnUtility pro mundo aberto (Fase A do spawn de monstro comum) — mesma
// função (achar posição de spawn válida), mas GLOBAL em vez de por-Floor, e com uma
// diferença de arquitetura importante: FloorSpawnUtility ENUMERA e CACHEIA todo nó
// caminhável do GridGraph de 1 vez (viável pra um Floor pequeno, estático depois do Scan
// do Editor). O GridGraph "OpenWorld" é grande (tamanho fixo generoso, centenas de milhares
// de nós) e, mais importante, MUDA ao vivo conforme os chunks terminam de pintar
// (ChunkTerrainGenerator.SyncPathfindingForChunk) — cachear tudo de uma vez ficaria
// desatualizado pras áreas ainda não visitadas. Por isso aqui é amostragem por REJEIÇÃO
// (igual PropPlacement.GenerateCandidatePositions): sorteia um ponto dentro do "Spawn
// Annulus" ao redor do centro (normalmente o jogador) e consulta o nó mais próximo NA HORA
// (sempre atualizado, nunca stale) — barato porque o annulus é sempre pequeno (dezenas de
// unidades), nunca o grafo inteiro.
public static class WorldSpawnUtility
{
    // Margem de tolerância pro ponto "arrastar" um pouco ao encaixar no nó caminhável mais
    // próximo (normal, candidatos raramente caem exatamente num nó) — qualquer coisa além
    // disso é sinal de que o nó mais próximo ficou longe de verdade do candidato (ver bug
    // abaixo), não um simples ajuste fino.
    private const float SnapTolerance = 2f;

    // Sorteia até `maxAttempts` pontos dentro do anel [minAnnulusDistance, maxAnnulusDistance]
    // ao redor de `center`, rejeitando qualquer ponto mais perto da ORIGEM do mundo (0,0) do
    // que `minDistanceFromOrigin` (Safe Zone + padding — doc de design, Seção 2.1.3: "Walkable
    // ≠ Valid Spawn Position", a Safe Zone nunca bloqueia o grafo, só filtra candidato) ou sem
    // nó caminhável próximo o bastante no GridGraph "OpenWorld". Nunca trava em loop infinito
    // (doc, Seção 11.6) — devolve false se nenhuma tentativa for válida.
    // `obstacleClearanceRadius`/`obstacleLayerMask` (opcional — radius<=0 desliga): checagem
    // EXTRA, só pra escolher onde spawnar, separada do grafo A* de propósito (decisão do
    // usuário: Erosion Iterations no GridGraph resolvia o monstro nascer colado num obstáculo,
    // mas afetava o PATHING normal também — corredores genuinamente largos o bastante ficavam
    // bloqueados pra todo mundo, não só a decisão de spawn). Aqui é um Physics2D.OverlapCircle
    // direto na posição candidata, raio do tamanho real do monstro + margem — rejeita e tenta
    // de novo se colidir com qualquer coisa da `obstacleLayerMask`, sem nunca tocar no grafo
    // nem no Erosion (que continua 0, pathing normal sem restrição extra nenhuma).
    public static bool TryGetSpawnPosition(
        Vector2 center,
        float minAnnulusDistance,
        float maxAnnulusDistance,
        float minDistanceFromOrigin,
        int maxAttempts,
        out Vector3 position,
        float obstacleClearanceRadius = 0f,
        LayerMask obstacleLayerMask = default)
    {
        position = default;

        var graph = ChunkTerrainGenerator.OpenWorldGraph;
        if (graph == null || AstarPath.active == null) return false;

        var constraint = NearestNodeConstraint.Walkable;
        constraint.graphMask = GraphMask.FromGraph(graph);

        float minDistanceFromOriginSqr = minDistanceFromOrigin * minDistanceFromOrigin;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            float angle = Random.value * Mathf.PI * 2f;
            float distance = Random.Range(minAnnulusDistance, maxAnnulusDistance);
            Vector2 candidate = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;

            if (candidate.sqrMagnitude < minDistanceFromOriginSqr) continue; // dentro da Safe Zone

            var info = AstarPath.active.GetNearest(candidate, constraint);
            if (info.node == null) continue;

            // Bug real relatado pelo usuário ("monstro nascendo muito distante"): o AstarPath
            // da cena tem maxNearestNodeDistance=100 — se a vizinhança imediata do candidato
            // não tiver nó caminhável (ex.: caiu em cima de um prop/hazard), GetNearest
            // "arrasta" o resultado pro nó caminhável mais próximo, mesmo que isso fique bem
            // mais longe do jogador do que o próprio annulus pedido. Sem essa checagem, a
            // distância real de spawn podia passar longe de maxAnnulusDistance sem nenhum
            // aviso. Rejeita e tenta outro ângulo/distância em vez de aceitar o arrasto.
            Vector2 snapped = info.position;
            if (Vector2.Distance(snapped, center) > maxAnnulusDistance + SnapTolerance) continue;

            // Clearance física — só pro SPAWN, nunca muda o que o grafo considera andável.
            if (obstacleClearanceRadius > 0f && Physics2D.OverlapCircle(snapped, obstacleClearanceRadius, obstacleLayerMask) != null) continue;

            position = snapped;
            return true;
        }

        return false;
    }

    // Mesmo padrão de FloorSpawnUtility.RestrictToOwnerGraph, mas pro grafo global — impede
    // o Seeker do monstro de considerar os GridGraphs antigos de Floor como válidos (caso
    // ainda existam no AstarPath da cena), sem precisar deletá-los pra isso funcionar certo.
    public static void RestrictToOpenWorldGraph(GameObject obj)
    {
        var graph = ChunkTerrainGenerator.OpenWorldGraph;
        if (graph == null) return;

        var seeker = obj.GetComponent<Seeker>();
        if (seeker != null) seeker.graphMask = GraphMask.FromGraph(graph);
    }
}
