# Plano — Spawn de Monstro Comum no Mundo Aberto (Fase A)

**Status: Fase A IMPLEMENTADA e testada pelo usuário (população, despawn/replacement, distribuição
de Tier, Safe Zone, clearance de obstáculo, grafo móvel — todos confirmados funcionando). Depois
desta rodada, o sistema antigo de Floor foi removido por completo (commit `16bc2b1`), o Boss (P7)
foi implementado, o Ciclo Dia/Noite real (lógica + visual) também, e o Simulation Rings (P8) também
— ver seções próprias abaixo.**
Documento preparado depois da implementação, mesmo padrão do `Plano_Performance_Streaming_Chunks.md`
— registra o raciocínio/decisões antes de esquecer, já que essa rodada teve uma sessão de debug
bem longa (sincronização do A* Pathfinding com o terreno gerado por chunk) que não pode se perder.

Contexto: depois da geração de terreno/performance (Sprint P4, ver o outro doc) estar estável, o
próximo passo oficial do plano de produção era Safe Zone + EffectiveSpawnDay pro spawn de monstro
(P4) seguido de Spawn Table Data-Driven + População Global (P5/P6). O usuário decidiu fazer isso
em escopo reduzido ("Fase A"): só monstro comum, sem Boss, sem ciclo Dia/Noite real, sem Simulation
Rings — testável com o que já existe (14 prefabs de Tier1/Tier2, Sprint 20).

## Decisão de escopo (Fase A) — o que ficou de fora de propósito

- **Boss (P7)**: sistema próprio, maior, ficou pra depois nesta Fase A — **implementado depois**,
  ver seção "Boss (P7) — implementado" abaixo.
- **Ciclo Dia/Noite real**: `DaySpawnDefinition.nightWeights` existia na tabela mas nunca era lido —
  ficou pra depois nesta Fase A, **implementado depois**, ver seção "Ciclo Dia/Noite real —
  implementado" abaixo.
- **Simulation Rings (P8)**: ficou pra depois nesta Fase A (sem evidência real de que fosse
  necessária ainda) — **implementado depois, como preparação** (decisão do usuário: sem um problema
  de FPS medido, mas quis adiantar mesmo assim), ver seção "Simulation Rings — implementado" abaixo.
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

## Boss (P7) — implementado

Depois da limpeza do sistema antigo de Floor (que incluiu remover o `BossSpawnManager` legado,
Floor-acoplado e com rotação simples), construído do zero, mesma arquitetura do monstro comum
(`WorldBossSpawnManager`/`BossSpawnSettings`/`BossDefinitionSO`, paralelos a
`WorldPopulationManager`/`MonsterSpawnSettings`/`EnemyDefinitionSO`), mas com as regras próprias do
doc de design (Seção 17):

- **Boss Timer** (`BossSpawnSettings.bossSpawnInterval`, default 30s) — decide QUANDO. Cada
  intervalo completo tenta adicionar 1 boss (bosses anteriores continuam vivos, podem acumular —
  **sem teto de boss vivo simultâneo**, decisão explícita do usuário).
- **Boss Tier = Tier predominante** (`WorldBossSpawnManager.TryPickBossDefinition`) — reaproveita
  `WorldTierSelector.ComputeTierPercentsNonAlloc` (mesma distribuição do monstro comum na posição
  atual do jogador), mas pega o Tier de MAIOR peso em vez de sortear. Se a pool desse Tier já
  esgotou, avança pro próximo Tier por ordem de predominância (`PickNextStrongestTier`).
- **`UsedBossIdsToday`** (`HashSet<BossDefinitionSO>`) — dentro de um Tier, sorteio uniforme entre
  os ainda não usados (decisão do usuário: sem peso configurável por boss, diferente do `weight` do
  monstro comum). Ao esgotar TODOS os `BossDefinitionSO` de um Tier específico, esse Tier reseta
  sozinho (`TryPickUnusedBossForTier`) — decisão do usuário: continuar girando bosses enquanto
  sobrar tempo de dia, em vez de travar até o próximo `ActualDay`. O reset diário
  (`GameEvents.OnDayStart`, mesmo gatilho do `DayTimer.ResetForNewDay()`) continua existindo por
  cima, mas na prática o reset por-Tier tende a disparar primeiro.
- **Distance Despawn + replacement** (`bossDespawnDistance`, maior que o `enemyDespawnDistance` do
  monstro comum de propósito) — nunca é morte, repõe a vaga na hora SEM resetar o Boss Timer
  (`Replacement != novo Boss Timer Event`, doc). `bossReplacementCooldown` protege contra reroll por
  ficar andando de um lado pro outro bem na borda da distância de despawn.
- Se o jogador MATA o boss (não despawn por distância), não existe replacement automático — só o
  próximo Boss Timer repõe.
- Spawn usa o mesmo `WorldSpawnUtility.TryGetSpawnPosition` do monstro comum (Spawn Annulus, Safe
  Zone, clearance de obstáculo) com os parâmetros próprios de `BossSpawnSettings`. Todo boss nasce
  já em combate (`EnemyController.SetIsBoss(true)` + `ForceImmediateAggro()`), sem fase de
  patrulha/idle.
- Roster inicial: 7 `BossDefinitionSO` em `Assets/Settings/BossDefinitions/` — 3 Tier1
  (Goblin King, Mother Slime Blue, Mother Slime Green) + 4 Tier2 (Centaur King, Rat People Royalty,
  Spider Queen, Werewolf), todos reaproveitando prefabs já existentes em `Assets/Prefabs/Bosses/`.

## Ciclo Dia/Noite real — implementado

Lógica de gameplay + visual, numa rodada só (decisão do usuário: fechar a feature de ponta a ponta
em vez de só a lógica).

- **`DayTimer.IsNight`** — fase calculada a partir do tempo decorrido vs. a duração REAL do dia
  corrente (`currentDayDuration`, guardada a cada `ResetForNewDay(duration)` — cobre o caso de
  duração customizada, não só `defaultDayDuration`). 50% Dia / 50% Noite sempre, nunca quebra essa
  proporção (doc, Seção 3). Todo dia novo começa em Dia. `GameEvents.OnDayNightPhaseChanged` dispara
  só na troca de fase (edge trigger), nunca todo frame.
- **`WorldTierSelector.ComputeTierPercentsNonAlloc`** ganhou parâmetro `isNight` (default `false`,
  aditivo — não quebra nenhum call site existente) — troca `dayWeights` por `nightWeights`.
  `ComputeTierGroupPercentsNonAlloc` (terreno/bioma) **não** ganhou esse parâmetro de propósito —
  decisão de escopo já registrada: bioma nunca oscila a cada troca de fase.
- **`WorldPopulationManager`/`WorldBossSpawnManager`** leem `DayTimer.Instance.IsNight` e passam pro
  cálculo de Tier — monstro comum e boss já reagem à fase real (Tiers superiores ganham mais peso à
  noite, conforme a tabela de `nightWeights` de cada Dia, Seção 4 do doc).
- **`DayNightLightController`** (novo) — crossfade suave (smoothstep, ~5s configurável) de cor/
  intensidade de um `Light2D` Global do URP 2D Renderer, disparado por
  `GameEvents.OnDayNightPhaseChanged`. Respeita a pausa do jogo (achado real: a pausa deste projeto
  é só uma flag — `TimeManager.IsPaused` — `Time.timeScale` continua 1, então sem o gate explícito
  `GameplayGate.IsActive` o crossfade continuaria rodando com o menu de pausa/loja aberto).
- **Numeração**: não existe uma Sprint/Deadline oficial reservada especificamente pra essa feature
  no `Projeto_Torre_Plano_Producao_v7.md` (P8 lá é "Simulation Rings + Limpeza de Combat Scope",
  não Dia/Noite — `DayTimer` como cronômetro único já existia de uma sprint numerada bem anterior).
  Por isso os comentários no código não citam nenhum `Px`/Sprint, só a Seção 3/4/25 do doc de design.

## Simulation Rings (P8) — implementado

**Por que agora, sem problema de FPS medido**: testado antes com `populationTarget=50`/`Maximum=80`
e o FPS ficou de boa — não tinha evidência real de necessidade. O usuário decidiu implementar mesmo
assim, como preparação, em vez de esperar um teste real mostrar queda. `Limpeza de Combat Scope`
(a outra metade do P8 oficial — `FloorActivationCheck`/`ownerFloor` substituídos) já tinha sido feita
na rodada de limpeza do sistema de Floor, antes mesmo do Boss (P7) — então esta rodada fecha o P8
inteiro.

**Implementado em `EnemyController.cs`** (sem sistema novo — tudo dentro do `Update()` já existente):

- `simulationNearRingRadius` (padrão 15) — dentro desse raio do jogador, simulação completa, todo
  frame, igual sempre foi.
- Fora do raio (mas ainda vivo — despawn por distância continua sendo
  `WorldPopulationManager`/`WorldBossSpawnManager`, não muda): `RVOController` desligado todo frame
  (checagem barata, sem throttle — desvio fino entre monstros só importa aglomerado perto do
  jogador) e a DECISÃO de IA (patrulha/combate/ataque) passa a rodar num intervalo reduzido
  (`midRingUpdateInterval`, padrão 0,3s) em vez de todo frame — nunca desligada, só menos frequente.
  `AIPath` continua se movendo sozinho (Update/FixedUpdate próprios, fora do controle deste
  `Update()`) em direção ao último destino setado entre os ticks reduzidos.
- **Achado real, correção de ponta a ponta**: simplesmente chamar a decisão com menos frequência
  quebraria cooldowns/timers internos (`AttackCooldown`, `PatrolAI`, `flankAI` do Melee, etc.) — eles
  andariam em câmera lenta longe do jogador (ex.: Spider Queen atacando a cada ~20s reais em vez de
  a cada ~1,5s) em vez de "imperceptível". Corrigido passando o tempo ACUMULADO
  (`midRingAccumulator`, não só o `Time.deltaTime` daquele frame) pra toda a cadeia de decisão —
  exigiu trocar `protected abstract void Move()` por `Move(float deltaTime)` e atualizar as 6
  implementações (Melee/Ranged/GoblinSapper/RatPeopleRoyalty/Slime/SpiderQueen) pra usar o parâmetro
  em vez de `Time.deltaTime` direto nos próprios cooldowns/sub-IAs.
- Dano/knockback/DoT (`UpdateDamageFlash`/`UpdateKnockback`/`StatusEffectController.Tick`) **não**
  são throttled — continuam em tempo real todo frame, mesmo longe, pra não alterar quanto dano um
  DoT aplica por segundo real nem atrasar o feedback visual de ter sido atingido.
- **Não implementado** (deixado de fora de propósito, menor impacto): Animator Culling Mode mais
  barato por distância — é mais uma configuração de asset por prefab do que lógica, e o
  `Light2D`/câmera já naturalmente não renderiza monstro fora de tela a essa distância.

## Spawn preferindo fora da visão da câmera (P9, parte dele) — implementado

Doc de design, Seção 23: "uma posição precisa passar pelas duas verificações quando aplicável:
OutsideSafeZone e preferencialmente: OutsideCameraView. Estar fora da câmera não torna uma posição
válida caso ela esteja dentro da Safe Zone." A rodada de Game Feel/câmera (Cinemachine Impulse/
Target Group/boss reveal) ficou fora deste doc — só a parte de spawn entra aqui.

- **`WorldSpawnUtility.TryGetSpawnPosition`** ganhou `preferOutsideCameraView` (default `false`,
  aditivo). Dentro do mesmo laço de tentativas já existente: guarda o primeiro candidato
  totalmente válido (Safe Zone + nó caminhável + clearance de obstáculo) como fallback e continua
  tentando achar um que TAMBÉM fique fora da visão atual da câmera; se as tentativas acabarem sem
  achar um assim, usa o fallback — nunca falha o spawn só por causa disso (a Safe Zone continua
  sendo a única verificação que de fato bloqueia).
- Checagem via `Camera.main.WorldToViewportPoint` — reflete o que está sendo renderizado de
  verdade (já passou por `PixelPerfectCamera`/`CinemachineBrain`), mais simples e sempre correto
  do que recalcular o enquadramento manualmente a partir da `CinemachineCamera`.
- **`outsideCameraViewChance`** (`MonsterSpawnSettings`/`BossSpawnSettings`, padrão `0.6` = 60%) —
  decisão do usuário: não é por TIPO de monstro/boss, é uma % rolada em CADA spawn individual
  (`Random.value < outsideCameraViewChance`), independente de qual `EnemyDefinitionSO`/
  `BossDefinitionSO` foi sorteado. Exemplo do usuário: população alvo de 50 → ~30 nascem fora da
  tela, ~20 nascem perto/à vista do jogador de propósito, pra dar um ar de perigo.
  `WorldPopulationManager`/`WorldBossSpawnManager` rolam isso a cada chamada de spawn (inclusive
  replacement por despawn de distância).

