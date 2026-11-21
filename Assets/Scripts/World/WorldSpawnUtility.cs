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
    // `preferOutsideCameraView` (default false — Seção 23/P9 do doc de design): PREFERÊNCIA,
    // nunca requisito — "estar fora da câmera não torna uma posição válida caso ela esteja
    // dentro da Safe Zone" (a Safe Zone sempre domina). Dentro do mesmo laço de tentativas,
    // guarda o primeiro candidato totalmente válido como fallback e continua tentando achar
    // um que TAMBÉM fique fora da visão atual da câmera; se as tentativas acabarem sem achar
    // um assim, usa o fallback (nunca falha o spawn só por causa disso).
    public static bool TryGetSpawnPosition(
        Vector2 center,
        float minAnnulusDistance,
        float maxAnnulusDistance,
        float minDistanceFromOrigin,
        int maxAttempts,
        out Vector3 position,
        float obstacleClearanceRadius = 0f,
        LayerMask obstacleLayerMask = default,
        bool preferOutsideCameraView = false)
    {
        position = default;

        var graph = ChunkTerrainGenerator.OpenWorldGraph;
        if (graph == null || AstarPath.active == null) return false;

        var constraint = NearestNodeConstraint.Walkable;
        constraint.graphMask = GraphMask.FromGraph(graph);

        // Bug real reportado pelo usuário: o terreno nasce de ruído Perlin puro por célula
        // (TerrainMask.ShouldPaintOverlay), sem nenhuma garantia de conectividade — uma "ilha"
        // de terra 2x2+ cercada de água é um blob válido como qualquer outro, então os 3
        // spawners (monstro/boss/baú) achavam nó caminhável nela e spawnavam normal, só que o
        // Seeker do monstro (restrito ao grafo "OpenWorld") nunca encontra caminho até o
        // jogador — fica parado pra sempre. O pacote A* já rastreia "componente conectado" de
        // graça por nó (GraphNode.Area, recalculado a cada UpdateGraphs — já chamado por chunk
        // em ChunkTerrainGenerator), então só precisa comparar a Area do candidato com a Area
        // de `center` (sempre o jogador, nos 3 chamadores): Area diferente = sem caminho
        // possível = rejeita e tenta outro ângulo, exatamente como as outras rejeições abaixo.
        // Se nem o PRÓPRIO center tiver nó caminhável perto (não devia acontecer, já que é a
        // posição do jogador), não tem Area de referência — desliga o filtro em vez de travar
        // todo spawn por causa disso.
        var centerInfo = AstarPath.active.GetNearest(center, constraint);
        bool hasCenterArea = centerInfo.node != null;
        uint centerArea = hasCenterArea ? centerInfo.node.Area : 0;

        float minDistanceFromOriginSqr = minDistanceFromOrigin * minDistanceFromOrigin;
        bool hasFallback = false;
        Vector3 fallbackPosition = default;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            float angle = Random.value * Mathf.PI * 2f;
            float distance = Random.Range(minAnnulusDistance, maxAnnulusDistance);
            Vector2 candidate = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;

            if (candidate.sqrMagnitude < minDistanceFromOriginSqr) continue; // dentro da Safe Zone

            var info = AstarPath.active.GetNearest(candidate, constraint);
            if (info.node == null) continue;
            if (hasCenterArea && info.node.Area != centerArea) continue; // ilha isolada — sem caminho até o jogador

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

            if (!preferOutsideCameraView || IsOutsideCameraView(snapped))
            {
                position = snapped;
                return true;
            }

            if (!hasFallback) // candidato válido, mas visível — guarda só o primeiro, não precisa de mais
            {
                hasFallback = true;
                fallbackPosition = snapped;
            }
        }

        if (hasFallback)
        {
            position = fallbackPosition;
            return true;
        }

        return false;
    }

    // Camera.main reflete o que está sendo renderizado de verdade (PixelPerfectCamera +
    // CinemachineBrain já resolveram lente/tamanho ali) — mais simples e sempre correto do
    // que tentar calcular o enquadramento manualmente a partir da CinemachineCamera. Sem
    // câmera ativa (raro, só em teste isolado) trata como "fora de visão" — preferência de
    // apresentação nunca deveria travar o spawn por um detalhe de setup de cena.
    // D9.4 (docs/superpowers/specs/2026-11-20-d9-4-horda-cerco-design.md, 4.5) — spawn do monstro
    // comum: ponto na borda de uma elipse com a proporção da tela que passa fora dos cantos
    // (semi-eixos = meia-largura/meia-altura visíveis × √2 + margem), dentro do setor pedido.
    // Mesmas validações do TryGetSpawnPosition (safe zone, nó caminhável, mesma ilha do grafo,
    // obstáculo) e o ponto final TEM que estar fora da tela: sem plano B visível — devolve false
    // e quem chama tenta outro setor ou o próximo frame.
    public static bool TryGetSpawnPositionInSector(
        Vector2 hero, int sector, int sectorCount, float margin, float jitter,
        float minDistanceFromOrigin, int attempts, float obstacleClearanceRadius, LayerMask obstacleLayerMask,
        out Vector3 position)
    {
        position = default;
        var graph = ChunkTerrainGenerator.OpenWorldGraph;
        if (graph == null || AstarPath.active == null) return false;

        GetViewExtents(out Vector2 viewCenter, out float halfWidth, out float halfHeight);
        float ellipseHalfWidth = halfWidth * Sqrt2 + margin;
        float ellipseHalfHeight = halfHeight * Sqrt2 + margin;

        var constraint = NearestNodeConstraint.Walkable;
        constraint.graphMask = GraphMask.FromGraph(graph);
        var heroInfo = AstarPath.active.GetNearest(hero, constraint);
        bool hasHeroArea = heroInfo.node != null;
        uint heroArea = hasHeroArea ? heroInfo.node.Area : 0;

        float sectorWidth = Mathf.PI * 2f / sectorCount;
        float minDistanceFromOriginSqr = minDistanceFromOrigin * minDistanceFromOrigin;

        for (int attempt = 0; attempt < attempts; attempt++)
        {
            float angle = (sector + Random.value) * sectorWidth;
            float extra = Random.Range(0f, jitter);
            Vector2 candidate = hero + SpawnRing.PointOnEllipse(angle, ellipseHalfWidth + extra, ellipseHalfHeight + extra);
            if (candidate.sqrMagnitude < minDistanceFromOriginSqr) continue; // dentro da Safe Zone

            var info = AstarPath.active.GetNearest(candidate, constraint);
            if (info.node == null) continue;
            if (hasHeroArea && info.node.Area != heroArea) continue; // ilha isolada — sem caminho até o herói

            Vector2 snapped = info.position;
            if ((snapped - candidate).sqrMagnitude > SnapTolerance * SnapTolerance) continue; // o nó caminhável mais perto puxaria pra longe do anel
            if (snapped.sqrMagnitude < minDistanceFromOriginSqr) continue;
            if (!SpawnRing.IsOutsideView(snapped - viewCenter, halfWidth, halfHeight)) continue; // nunca nasce visível
            if (obstacleClearanceRadius > 0f && Physics2D.OverlapCircle(snapped, obstacleClearanceRadius, obstacleLayerMask) != null) continue;

            position = snapped;
            return true;
        }
        return false;
    }

    private static readonly float Sqrt2 = Mathf.Sqrt(2f);

    // Área visível atual da câmera principal (ortográfica). Sem câmera: a de referência do jogo
    // (tamanho 10, 16:9), centrada no herói — só pra não travar o spawn em cena de teste.
    private static void GetViewExtents(out Vector2 center, out float halfWidth, out float halfHeight)
    {
        var cam = Camera.main;
        if (cam == null)
        {
            center = Vector2.zero; halfHeight = 10f; halfWidth = 10f * 16f / 9f;
            return;
        }
        center = cam.transform.position;
        halfHeight = cam.orthographicSize;
        halfWidth = halfHeight * cam.aspect;
    }

    private static bool IsOutsideCameraView(Vector2 worldPos)
    {
        var cam = Camera.main;
        if (cam == null) return true;

        Vector3 viewport = cam.WorldToViewportPoint(worldPos);
        return viewport.z <= 0f || viewport.x < 0f || viewport.x > 1f || viewport.y < 0f || viewport.y > 1f;
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
