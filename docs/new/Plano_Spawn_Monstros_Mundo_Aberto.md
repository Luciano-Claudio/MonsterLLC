# Plano — Spawn de Monstro Comum no Mundo Aberto (Fase A)

**Status: Fase A IMPLEMENTADA e testada pelo usuário (população, despawn/replacement, distribuição
de Tier, Safe Zone, clearance de obstáculo, grafo móvel — todos confirmados funcionando).**
Documento preparado depois da implementação, mesmo padrão do `Plano_Performance_Streaming_Chunks.md`
— registra o raciocínio/decisões antes de esquecer, já que essa rodada teve uma sessão de debug
bem longa (sincronização do A* Pathfinding com o terreno gerado por chunk) que não pode se perder.

Contexto: depois da geração de terreno/performance (Sprint P4, ver o outro doc) estar estável, o
próximo passo oficial do plano de produção era Safe Zone + EffectiveSpawnDay pro spawn de monstro
(P4) seguido de Spawn Table Data-Driven + População Global (P5/P6). O usuário decidiu fazer isso
em escopo reduzido ("Fase A"): só monstro comum, sem Boss, sem ciclo Dia/Noite real, sem Simulation
Rings — testável com o que já existe (14 prefabs de Tier1/Tier2, Sprint 20).

## Decisão de escopo (Fase A) — o que ficou de fora de propósito

- **Boss (P7)**: sistema próprio, maior, fica pra depois.
- **Ciclo Dia/Noite real**: `DaySpawnDefinition.nightWeights` existe na tabela mas nunca é lido —
  spawn de monstro usa sempre `dayWeights`, igual o terreno já fazia (decisão consistente).
- **Simulation Rings (P8)**: ver seção própria abaixo — ideia documentada, não implementada (sem
  evidência real de que seja necessária ainda, ver "Por que ainda não implementamos").
- **Bestiário dos Tiers 3-10**: conteúdo (prefab/arte), não é trabalho de engenharia — o sistema já
  nasce pronto pra esses tiers assim que os prefabs existirem (basta criar `EnemyDefinitionSO` novo
  apontando pro prefab e marcar o `tier`).

## O que foi implementado

- **`EnemyDefinitionSO`** (`Assets/Scripts/Core/WorldGeneration/`) — substitui a falta de bestiário
  em código: `tier` (1-10) + `prefab` + `weight` (peso dentro do próprio tier). 14 assets criados
  em `Assets/Settings/EnemyDefinitions/`, 1 por prefab já existente (8 Tier1 + 6 Tier2).
- **`MonsterSpawnSettings`** (SO, `Assets/Settings/MonsterSpawnSettings.asset`) — roster completo +
  config de população (`populationMinimum/Target/Maximum`, reaproveitando o modelo de crise/
  escalação do `FloorPopulationManager`, agora 1 população global) + config de distância (Safe Zone
  padding, `enemyDespawnDistance`, `replacementSpawnMinDistance/MaxDistance`) +
  `spawnObstacleClearanceRadius`/`obstacleLayerMask` (ver "Bug 5" abaixo).
- **`WorldTierSelector`** ganhou `ComputeTierPercentsNonAlloc` (10 Tiers de monstro, normalização
  direta — **sem** o piso de 20% que o terreno usa pros 6 grupos, decisão do usuário: esse piso foi
  pensado especificamente pra grupo de terreno, não faz sentido do mesmo jeito pra tier individual
  de monstro) e `RollMonsterTier` (roleta cumulativa, "Roll Tier" do doc de design).
- **`WorldSpawnUtility`** (substitui `FloorSpawnUtility` pro mundo aberto) — amostragem por
  REJEIÇÃO dentro do "Spawn Annulus" ao redor do jogador (não cacheia todo nó caminhável como o
  antigo, porque o GridGraph muda ao vivo conforme os chunks pintam — cache ficaria stale).
- **`WorldPopulationManager`** (substitui `FloorPopulationManager`) — pipeline completo do doc de
  design: `ActualDay` → distância da origem → `EffectiveSpawnDay` → pesos dos 10 Tiers → Roll Tier
  → Roll Monster Inside Tier → Spawn Annulus → spawn. Despawn por distância com replacement
  imediato (nunca é morte — sem loot/kill/energia).
- **Achado importante**: `ownerFloor == null` já era tratado como "sempre ativo" em TODO o sistema
  de monstro existente (`FloorActivationCheck.IsActive`) — não precisou tocar em nenhum dos ~18
  arquivos que checam `ownerFloor`/Floor. Monstros do mundo novo simplesmente nunca recebem Floor.

## Bugs reais encontrados e corrigidos nesta rodada

### Bug 1 — `MeleeAttackSlotManager` crash com `ArgumentNullException`

`Dictionary<FloorDefinition, SlotPool>` **não aceita chave `null`** (isso é `Hashtable` legado, não
`Dictionary<>` — suposição errada minha de que aceitaria). Todo monstro do mundo aberto tem
`ownerFloor = null` de propósito — o primeiro Melee que tentava atacar crashava. Corrigido com um
`globalPool` separado, só pra `floor == null`, nunca toca o Dictionary.

### Bug 2 — Água/lava "oca" (collider só na borda, miolo livre)

`CompositeCollider2D.Geometry Type` estava em `Outlines` (gera só o CONTORNO fino, tipo parede de 1
célula — correto pra plataforma/parede, errado pra área sólida preenchida como um lago). Trocado
pra `Polygons` (forma sólida de verdade). Esse ficou óbvio só quando o usuário testou teleportando
pro meio de uma poça e andando livre lá dentro.

### Bug 3 — `RuleTile` original gerava collider inconsistente no layer de colisão

Primeira tentativa: copiar o `RuleTile` original (água/lava/etc.) pro Tilemap de colisão dedicado.
Problema: o `RuleTile` tenta casar REGRAS DE VIZINHANÇA pra escolher sprite/collider — nesse layer
isolado (só tem água copiada, sem o resto do contexto do bioma ao redor) as regras quase nunca
batiam. Corrigido com um `Tile` simples (`Assets/Settings/HazardCollisionMarker.asset`,
`colliderType = Grid`, sem regra de vizinhança nenhuma) — bloco sólido fixo por célula, sempre.

### Bug 4 — Chunk central nunca sincronizava com o A* (o mais difícil de achar)

Sintoma: todo chunk normal (andando) sincronizava certo com o grafo A*, só o chunk central (pintado
sincronamente, `PaintChunkImmediate`, no instante em que o herói nasce) nunca marcava água/prop como
não-andável — mesmo com os colliders físicos corretos.

**4 tentativas erradas**, nessa ordem (documentado pra não repetir o caminho): esperar 1
`FixedUpdate`; forçar `CompositeCollider2D.GenerateGeometry()` na mesma linha; combinar os dois com
`WaitForEndOfFrame`; adicionar `AstarPath.active.FlushGraphUpdates()`. Nenhuma resolveu.

**Causa real**, achada só depois de instrumentar com logs + um "censo" (amostra de 25 pontos
perguntando `Walkable` direto pro grafo): o `TilemapCollider2D` (peça que alimenta o
`CompositeCollider2D`) só processa as mudanças de tile por **tempo real**, não por quantidade de
frames. Chunks normais (pintados andando, via fila paceada) sempre tinham várias centenas de ms de
sobra de graça, só por causa do próprio pacing da pintura. O chunk central, pintado inteiro num
frame só (`PaintChunkImmediate`), não tinha essa folga — minha espera baseada em CONTAGEM DE FRAMES
não cobria tempo real suficiente. Prova definitiva: com o gizmo `Show Graphs` do AstarPath ligado
(que derruba o FPS do Editor pesadamente), o chunk central passava a sincronizar certo — porque
cada "frame" ali representava bem mais tempo real.

**Correção final**: trocar a espera por `yield return new WaitForSeconds(0.5f)` (tempo real, não
frame-count) em `ChunkTerrainGenerator.SyncPathfindingForChunkDelayed`. Mantidas como margem extra,
mas não eram a causa raiz: espera pelo Scan inicial terminar (`openWorldGraphScanComplete`),
`ForceHazardColliderRegeneration()`, e bounds com padding generoso (+8 unidades) deslocados por um
offset pequeno não-redondo (evita o bounds de atualização cair EXATAMENTE em cima do pivô (0,0) do
grafo, suspeita de caso-limite de arredondamento no código fechado do pacote).

### Bug 5 — Monstro nascendo colado demais em obstáculo, preso lá

O `GridGraph` testa "andável" com uma sondagem pequena (`Diameter: 1`, raio 0,5) — um nó bem na
beirada da água passa no teste, mas o corpo físico real do monstro é maior, então ele spawnava ali e
travava contra o collider de verdade. Primeira ideia (`Erosion Iterations` no GridGraph) foi
descartada pelo usuário: isso encolhe a área andável pra TODO MUNDO, não só spawn — corredores
genuinamente largos ficavam bloqueados pra passagem normal. Solução adotada: checagem EXTRA, só na
hora de escolher posição de spawn, separada do grafo — `Physics2D.OverlapCircle` no ponto candidato
(`WorldSpawnUtility.TryGetSpawnPosition`, parâmetros `obstacleClearanceRadius`/`obstacleLayerMask`,
configurados em `MonsterSpawnSettings`). Pathing normal nunca é afetado.

### Bug 6 — Teto de tamanho do GridGraph (achado pelo usuário, não bug ainda — prevenção)

GridGraph tem um máximo de 1024×1024 nós (constante de segurança do pacote, `#if !ASTAR_LARGER_GRIDS`
em `GridGraph.cs` — elevável a 8192 via define, mas não resolve o problema de fundo). O usuário
calculou que um jogador consegue alcançar essa distância da origem em ~150s de jogo, e a duração do
dia vai subir de 100s pra 200-300s — um grafo fixo, não importa o tamanho, sempre tem teto que um
jogador decidido alcança andando reto. Adotado `ProceduralGraphMover` (componente pronto do próprio
pacote A*, `Packages/com.arongranberg.astar/Utilities/ProceduralGraphMover.cs`) — mantém um GridGraph
bem menor (reduzido de 1024 pra 512) "deslizando" junto com o jogador (`target = hero`, setado em
runtime por `ChunkTerrainGenerator.HandleHeroSpawned`, mesmo padrão do `cinemachineCamera.Target`).
Não compete com nosso `UpdateGraphs` por chunk (mesma fila de work items do pacote, sem race) —
único cuidado: não guardar `NodeInGridIndex` como identidade estável de nó (muda quando o grafo se
move) — não fazemos isso em lugar nenhum do nosso código, então sem impacto.

## FUTURO — documentar como pendência, não implementar agora

### Simulation Rings (P8) — otimização de custo por monstro, por distância

**Ideia central**: hoje todo monstro ativo roda o ciclo completo de `EnemyController.Update()` (IA,
`AIPath`, `RVOController`, Animator) com o mesmo custo, não importa se está a 2 unidades do jogador
(visível, importa) ou a 28 (quase no limite de despawn, fora de tela). Simulation Rings divide o
espaço ao redor do jogador em faixas de fidelidade:

- **Anel perto** (ex.: até ~15 unidades): simulação completa, igual hoje.
- **Anel médio** (15 até `enemyDespawnDistance`): simulação REDUZIDA, não desligada —
  `EnemyController.Update()` roda com frequência menor (ex.: a cada 0,2-0,5s em vez de todo frame,
  imperceptível a essa distância); `RVOController` desligado ou com prioridade mínima (desvio fino
  entre monstros só importa aglomerado perto do jogador); Animator num modo mais barato (Culling
  Mode da própria Unity).
- **Além do despawn**: já removido — isso já existe, não muda.

**Onde entraria no código**: o gancho natural é o MESMO gate que já existe em
`EnemyController.Update()` (`GameplayGate.IsActive && FloorActivationCheck.IsActive(...)`) — viraria
uma função de 3 níveis em vez de só ligado/desligado, usando a mesma distância até o jogador que
`WorldPopulationManager` já calcula pro despawn. Não precisa de sistema novo do zero.

**Por que ainda não implementamos**: testado com `populationTarget=50`/`Maximum=80` (bem mais monstro
simultâneo do que o "população pequena" que justificou pular isso na Fase A) e o FPS ficou de boa,
principalmente em janela maximizada. Sem evidência real de que o custo por monstro esteja doendo —
construir otimização sem um problema medido é o tipo de trabalho que evitamos a sessão toda. Fica
pronta aqui pra quando (se) um teste real mostrar queda de FPS com população alta.

### Boss (P7)

Sistema próprio, maior — Boss Timer único por dia, Boss Tier = Tier predominante na distribuição
atual (não mais um roll BT1-BT10 separado), sem repetição de `BossDefinition` no mesmo `ActualDay`,
replacement por distância sem resetar o timer. `BossSpawnManager` atual é legado (rotação simples,
Floor-acoplado) — precisa ser substituído, não adaptado, mesmo critério do `FloorPopulationManager`
nesta rodada.

### Ciclo Dia/Noite real

Não existe hoje (`DayTimer` é só um cronômetro único, sem fase interna Dia/Noite). O doc de design
assume que isso já existe pro spawn de monstro consultar (`nightWeights`) — precisa ser construído
antes de ativar essa parte da tabela.

### Limpeza do sistema antigo de Floor

Usuário já confirmou 100% morto (`FloorManager`/`FloorRegistry`/`FloorPopulationManager`/
`FloorSpawnUtility`/`BossSpawnManager`/`MeleeAttackSlotManager` por-Floor/scripts relacionados, ~18
arquivos que checam `ownerFloor`). Pendente — trabalho de remoção de código morto, separado desta
rodada de spawn.
