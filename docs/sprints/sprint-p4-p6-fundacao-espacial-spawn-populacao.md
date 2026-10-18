# Sprint P4-P6 — Fundação Espacial + Spawn Table Data-Driven + Pipeline de População Global

## Objetivo

Fazer monstro comum nascer de verdade no mundo único: Safe Zone (área central sem spawn), EffectiveSpawnDay (distância da origem simula "dias futuros" espacialmente), tabela de spawn data-driven por Tier/Dia, e uma população global com despawn/replacement por distância — substituindo por completo o antigo `FloorPopulationManager`/`FloorSpawnUtility` por-Floor. Escopo reduzido de propósito ("Fase A", decisão do usuário): só monstro comum, sem Boss, sem ciclo Dia/Noite real, sem Simulation Rings — testável com o bestiário que já existia (14 prefabs de Tier1/Tier2).

## Sistemas adicionados

- **`EnemyDefinitionSO`** (`Assets/Scripts/Core/WorldGeneration/`) — substitui a falta de bestiário em código: `tier` (1-10) + `prefab` + `weight` (peso dentro do próprio tier, "Roll Monster Inside Tier"). 14 assets criados em `Assets/Settings/EnemyDefinitions/` (8 Tier1 + 6 Tier2), 1 por prefab já existente.
- **`MonsterSpawnSettings`** (SO) — roster completo + config de população (`populationMinimum/Target/Maximum`, reaproveitando o modelo de crise/escalação do `FloorPopulationManager`, agora 1 população global) + config de distância (Safe Zone padding, `enemyDespawnDistance`, `replacementSpawnMinDistance/MaxDistance`) + `spawnObstacleClearanceRadius`/`obstacleLayerMask`.
- **`WorldTierSelector` ganhou `ComputeTierPercentsNonAlloc`/`RollMonsterTier`** — 10 Tiers de monstro, normalização DIRETA dos pesos (sem o piso de 20% que o terreno usa pros 6 grupos — decisão do usuário: esse piso foi pensado especificamente pra grupo de terreno).
- **`WorldSpawnUtility`** (substitui `FloorSpawnUtility`) — amostragem por REJEIÇÃO dentro do "Spawn Annulus" ao redor do jogador, em vez de cachear todo nó caminhável de uma vez (o GridGraph muda ao vivo conforme os chunks pintam — cache ficaria stale). `obstacleClearanceRadius`/`obstacleLayerMask` opcionais — checagem física EXTRA, separada do grafo A*.
- **`WorldPopulationManager`** (substitui `FloorPopulationManager`) — pipeline completo: `ActualDay` → distância da origem → `EffectiveSpawnDay` → pesos dos 10 Tiers → Roll Tier → Roll Monster Inside Tier → Spawn Annulus → spawn. Despawn por distância com replacement imediato, nunca é morte (sem loot/kill/energia).
- **`ProceduralGraphMover`** (componente pronto do pacote A* Pathfinding Project Pro) — GridGraph "OpenWorld" reduzido de 1024 pra 512 nós, "deslizando" junto com o jogador em vez de cobrir uma área fixa. Resolve o teto de tamanho do GridGraph sem precisar de um grafo gigante.
- **Achado importante**: `ownerFloor == null` já era tratado como "sempre ativo" em TODO o sistema de monstro existente (`FloorActivationCheck.IsActive`) — não precisou tocar em nenhum dos ~18 arquivos que checavam `ownerFloor`/Floor. Monstros do mundo novo simplesmente nunca recebem Floor.

## Decisões técnicas

- **Spawn-only obstacle clearance, nunca Erosion no GridGraph** — primeira tentativa pro bug de monstro nascendo colado em obstáculo foi `Erosion Iterations`, rejeitada pelo usuário: encolhe a área andável pra TODO MUNDO, não só spawn — corredores genuinamente largos ficavam bloqueados pra passagem normal. Solução final: `Physics2D.OverlapCircle` só na hora de ESCOLHER a posição de spawn, nunca tocando o grafo/Erosion.
- **GridGraph móvel em vez de gigante fixo** — usuário calculou que um jogador decidido alcança o limite de qualquer grafo de tamanho fixo (mesmo 1024 nós) andando reto por tempo suficiente, ainda mais com a duração do dia subindo de 100s pra 200-300s. `ProceduralGraphMover.target` setado em runtime (`ChunkTerrainGenerator.HandleHeroSpawned`), mesmo padrão do `cinemachineCamera.Target`.
- **Pivot-escape-offset no GraphUpdateObject** — mantido como margem extra na correção do bug do chunk central (ver Bugs conhecidos), mesmo não sendo a causa raiz.

## Arquivos/classes principais

- `Assets/Scripts/Core/WorldGeneration/EnemyDefinitionSO.cs`, `MonsterSpawnSettings.cs` — bestiário/config.
- `Assets/Scripts/Core/WorldGeneration/WorldTierSelector.cs` — `ComputeTierPercentsNonAlloc`/`RollMonsterTier` novos.
- `Assets/Scripts/World/WorldSpawnUtility.cs` — novo, amostragem por rejeição.
- `Assets/Scripts/World/WorldPopulationManager.cs` — novo, pipeline de população global.
- `Assets/Scripts/World/Generation/ChunkTerrainGenerator.cs` — `hazardCollisionTilemap`/`hazardMarkerTile`, `openWorldGraphMover`, `SyncPathfindingForChunkDelayed` (ver Bugs conhecidos).
- `Assets/Settings/EnemyDefinitions/` — 14 assets.
- `Assets/Settings/MonsterSpawnSettings.asset`, `Assets/Settings/HazardCollisionMarker.asset` — config/assets.
- `docs/new/Plano_Spawn_Monstros_Mundo_Aberto.md` — doc de decisões desta rodada inteira (Fase A até P11), preparado depois da implementação por causa da sessão de debug longa.

## Eventos adicionados

Nenhum em `GameEvents` nesta rodada (o `OnHeroRespawned`/`OnBossSpawned`/etc. vieram em sprints seguintes).

## Testes executados

Manual (Play Mode) — população oscilando entre min/target/max, despawn/replacement por distância, distribuição de Tier variando com distância (Tier2 aparecendo longe da origem mesmo no Dia 1), clearance de obstáculo, grafo móvel seguindo o jogador — todos confirmados funcionando pelo usuário antes do commit.

## Bugs conhecidos

- **[CORRIGIDO] `MeleeAttackSlotManager` crash com `ArgumentNullException`** — `Dictionary<FloorDefinition, SlotPool>` não aceita chave `null` (diferente de `Hashtable`, suposição errada). Todo monstro do mundo aberto tem `ownerFloor = null` de propósito — o primeiro Melee que tentava atacar crashava. Corrigido com um `globalPool` separado só pra `floor == null`.
- **[CORRIGIDO] Água/lava "oca" — collider só na borda, miolo livre** — `CompositeCollider2D.Geometry Type` estava em `Outlines` (contorno fino) em vez de `Polygons` (forma sólida). Achado pelo usuário teleportando pro meio de uma poça e andando livre.
- **[CORRIGIDO] `RuleTile` original gerava collider inconsistente no layer de colisão dedicado** — regras de vizinhança do RuleTile quase nunca batiam num layer isolado (só a água copiada, sem o resto do contexto do bioma ao redor). Corrigido com um `Tile` simples (`HazardCollisionMarker.asset`, `colliderType=Grid`, sem regra de vizinhança).
- **[CORRIGIDO, o mais difícil de achar] Chunk central nunca sincronizava colisão de água/lava com o A\*** — todo chunk normal (pintado andando, via fila paceada) sincronizava certo; só o chunk central (pintado sincronamente no instante em que o herói nasce, `PaintChunkImmediate`) nunca marcava água/prop como não-andável, mesmo com os colliders físicos corretos. 4 tentativas erradas documentadas (esperar 1 `FixedUpdate`; forçar `GenerateGeometry()` na mesma linha; combinar os dois com `WaitForEndOfFrame`; `FlushGraphUpdates()`) — nenhuma resolveu. Causa real, achada só com o usuário testando o gizmo "Show Graphs" do AstarPath ligado (que derruba o FPS do Editor): o `TilemapCollider2D` processa mudanças de tile por TEMPO REAL, não por quantidade de frames — chunks normais sempre tinham folga de sobra pelo próprio pacing da pintura, o chunk central (pintado num frame só) não. Corrigido trocando a espera por `yield return new WaitForSeconds(0.5f)` (tempo real) em vez de uma contagem de frames.
- **[CORRIGIDO] Monstro nascendo colado demais em obstáculo, preso lá** — a sondagem de "andável" do GridGraph (`Diameter: 1`) passa numa beirada que o corpo físico real do monstro não cabe. Resolvido com clearance física só no spawn (ver Decisões técnicas).
- **[PREVENÇÃO, não bug ainda] Teto de 1024×1024 nós do GridGraph** — achado pelo próprio usuário por cálculo (não por teste), resolvido preventivamente com `ProceduralGraphMover` antes de virar problema real.

## Dívida técnica

- **Simulation Rings (otimização de custo por monstro por distância) documentada, não implementada** — testado com população pequena e FPS ficou de boa; sem evidência real de necessidade. Revisitada depois na Sprint P8.
- **Ciclo Dia/Noite real não existe ainda** — spawn usa sempre `dayWeights`, nunca `nightWeights`. Revisitado numa sprint bônus logo após o Boss (P7).
- **Boss (P7) fora de escopo de propósito** — sistema próprio, maior, sprint seguinte.

## Próximos passos

Limpar o sistema antigo de Floor (documentado nesta mesma leva de commits, mas tratado como unidade própria — ver Sprint P10) e documentar esta rodada antes de ir pro Boss (P7).
