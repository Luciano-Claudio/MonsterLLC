# D9.4 Horda (curva por dia, cerco, fluxo contínuo, anti-kite, empurrão) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Trocar o spawn de monstros comuns por uma horda que cresce no tempo real do dia com teto por dia, nasce sempre fora da tela em cerco por setores com anti-kite, repõe em fluxo contínuo, reposiciona quem fica longe, deixa o herói empurrar a horda e mostra avisos de explosão legíveis.

**Architecture:** Regras puras no assembly `Core` (curva, setores, elipse, anti-kite, empurrão, relógio), com testes EditMode. O `WorldPopulationManager` e o `WorldSpawnUtility` continuam e trocam o miolo (caminho 1 do design). O empurrão é um componente novo no herói, mais `Physics2D.IgnoreCollision` por par entre o corpo do herói e o corpo de cada monstro comum.

**Tech Stack:** Unity 6000.3.8f1, 2D, A* Pathfinding Project (`IAstarAI`/`AIPath`/`RVOController`), NUnit EditMode. Unity CLI pelos helpers `.superpowers/tools/eval.sh` e `.superpowers/tools/edittests.sh`.

**Spec:** `docs/superpowers/specs/2026-11-20-d9-4-horda-cerco-design.md` (aprovado). Contexto: `docs/new/Mudanca_Estrutural_Deadline_9_XP_Cartas_Horda.md`, Seção 2.

## Global Constraints

- Toda prosa (comentários, docs, mensagens) em pt-BR. Commits terminam com `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.
- Push só com confirmação explícita do usuário (AskUserQuestion), mesmo depois de commit aprovado.
- Antes de editar prefab por API: `git diff` do prefab, backup, `AssetDatabase.ImportAsset(path, ForceUpdate)`. Prefab aberto em Prefab Mode: editar pelo PrefabStage. Conferir o diff depois.
- Caminhos novos sem `new`, lambda ou LINQ por frame (padrão do D9.3). Buffers fixos e reaproveitados.
- Consultas de monstro usam a layer `Enemy` (filtro do `EnemyController.EnemyOnlyFilter`). Projétil/efeito novo vai em `HeroAttack`/`EnemyAttack`. Não desligar `Auto Sync Transforms`.
- Valores iniciais (🔢, todos editáveis no Inspector):

  | Valor | Inicial |
  |---|---|
  | Tabela por dia (começo/teto) | 1: 10/150 · 2: 15/180 · 3: 25/210 · 4: 30/240 · 5: 40/270 · 6–10: 50/300 |
  | `secondsToCeiling` | 240 |
  | Forma da curva (tempo normalizado → valor normalizado) | (0,0) (0.125,0.0357) (0.25,0.1429) (0.5,0.3571) (0.75,0.6429) (1,1) — vem de 10/15/30/60/100/150 em 0/30/60/120/180/240 s |
  | Picos | +50% (`spikeMultiplier` 1.5), 15 s, a cada 60 s, primeiro em 60 s |
  | `hardCap` | 300 |
  | `spawnsPerSecond` | 10 |
  | `rangedUnlockSeconds` | 30 (só dia 1) |
  | Setores / margem / jitter | 12 / 2 / 1 unidade |
  | Anti-kite | janela 3 s, velocidade mínima 1,5 u/s, arco 90°, `kiteBias` 3, `kiteMaxFrontShare` 0,4 nos últimos 20 spawns |
  | Empurrão | velocidade ×0,5; sonda raio 0,6 a 0,5 à frente; força lateral 6 |
  | `enemyDespawnDistance` | 30 (mantém) |

- Dia da curva = dia da run (`MainMenuUI.CurrentRun?.day ?? 1`, ou `testDayOverride`). Dia < 1 vira 1; dia > 10 usa a linha do dia 10.

## Review Focus

1. **Lugar sem ponto válido** (herói encostado na água/borda do mundo/safe zone): a maioria dos setores falha. Esperado: tenta os setores em ordem de menos cheio, não nasce nada visível e volta a nascer quando houver lugar. Teste: `SpawnSectorPicker_Order_CoversEverySector` (Task 2).
2. **Pausa, loading e morte**: o relógio da horda e o spawn não andam com `GameplayGate` inativo nem com `HeroController.IsDying`. Teste: `HordeClock_DoesNotAdvanceWhenInactive` (Task 1) + passo manual da Task 3.
3. **Tamanho de câmera diferente** (resolução, Pixel Perfect): a elipse vem da câmera em runtime, e todo ponto dela fica fora da área visível. Teste: `Ellipse_PointsAreOutsideView_ForAnyAspect` (Task 2).
4. **Picos acima do teto e dias fora de 1–10**: a meta nunca passa de `hardCap`, e o dia é limitado a 1–10. Testes: `Curve_SpikeNeverExceedsHardCap`, `Curve_DayOutOfRange_IsClamped` (Task 1).
5. **Corpo do herói trocado** (troca de forma do Druid, herói novo): a colisão ignorada tem que valer pro collider novo. Esperado: o par volta a ser ignorado quando o collider do herói muda. Passo manual na Task 7 (Druid Feral empurrando a horda).

---

### Task 0: Branch

- [ ] **Step 1:** `git checkout -b d9-4-horda-cerco` a partir da `main` atualizada.

### Task 1: Curva da horda e relógio (Core)

**Files:**
- Create: `Assets/Scripts/Core/AI/HordeCurve.cs`
- Test: `Assets/Tests/EditMode/HordeCurveTests.cs`

**Interfaces:**
- Produces:
  - `[System.Serializable] public struct DayPopulation { public int start; public int ceiling; }`
  - `[System.Serializable] public class HordeCurveSettings` com os campos públicos `AnimationCurve shape`, `float secondsToCeiling`, `DayPopulation[] days` (10 posições), `float spikeInterval`, `float spikeDuration`, `float spikeMultiplier`, `int hardCap`, e o método `public int TargetAlive(float elapsedSeconds, int day)`.
  - `public static class RangedGate { public static bool Blocks(int day, float elapsedSeconds, float unlockSeconds) }`: verdadeiro só com `day == 1 && elapsed < unlock`.
  - `public class HordeClock { public float Elapsed { get; } public void Reset(float startOffsetSeconds); public void Tick(float deltaTime, bool active); }`

- [ ] **Step 1: Escrever os testes** (com uma `HordeCurveSettings` montada com os valores da tabela de Global Constraints):
  - `Curve_Day1_Start_Is10`: `TargetAlive(0, 1) == 10`
  - `Curve_Day1_At240_IsCeiling150`: `TargetAlive(240, 1) == 150`
  - `Curve_AfterCeiling_StaysAtCeiling`: `TargetAlive(400, 1) == 150` fora de pico (use um tempo fora da janela de pico, ex. 590 com pico a cada 60 s por 15 s → 590 está fora)
  - `Curve_Day1_At30_Is15`: `TargetAlive(30, 1) == 15` (± 1 por arredondamento)
  - `Curve_Day3_Start_Is25`: `TargetAlive(0, 3) == 25`
  - `Curve_SpikeWindow_Adds50Percent`: `TargetAlive(65, 1) == round(base(65) * 1.5)`; `TargetAlive(80, 1) == base(80)` (fora da janela 60–75)
  - `Curve_NoSpikeBeforeFirstInterval`: `TargetAlive(10, 1)` sem multiplicador
  - `Curve_SpikeNeverExceedsHardCap`: dia 10, `TargetAlive(245, 10) == 300` (300 × 1,5 limitado em 300)
  - `Curve_DayOutOfRange_IsClamped`: `TargetAlive(0, 0) == TargetAlive(0, 1)`; `TargetAlive(0, 15) == TargetAlive(0, 10)`
  - `RangedGate_Day1_BlocksBefore30`: `Blocks(1, 29.9f, 30) == true`, `Blocks(1, 30f, 30) == false`
  - `RangedGate_Day2_NeverBlocks`: `Blocks(2, 0f, 30) == false`
  - `HordeClock_AdvancesWhenActive`: `Reset(0); Tick(1, true); Elapsed == 1`
  - `HordeClock_DoesNotAdvanceWhenInactive`: `Reset(0); Tick(1, false); Elapsed == 0`
  - `HordeClock_ResetUsesOffset`: `Reset(180); Elapsed == 180`
- [ ] **Step 2:** Rodar `bash .superpowers/tools/edittests.sh` (depois de `unity recompile`). Esperado: falha de compilação/teste pelos tipos inexistentes.
- [ ] **Step 3: Implementar** `HordeCurve.cs`. `meta = start + (ceiling − start) × shape.Evaluate(clamp01(t / secondsToCeiling))`, arredondado. Pico quando `t >= spikeInterval && (t % spikeInterval) < spikeDuration`: multiplica por `spikeMultiplier`. Resultado final: `min(hardCap, …)`. Dia: `clamp(day, 1, days.Length)`.
- [ ] **Step 4:** Rodar a suíte. Esperado: `failed=0` (fora os 3 `BagTests` pré-existentes).
- [ ] **Step 5: Commit** `feat(D9.4): curva da horda por dia, picos, teto e relogio (Core)`.

### Task 2: Setores, elipse, anti-kite e empurrão (Core)

**Files:**
- Create: `Assets/Scripts/Core/AI/HordeSpawnRules.cs`
- Test: `Assets/Tests/EditMode/HordeSpawnRulesTests.cs`

**Interfaces:**
- Produces:
  - `public static class SpawnRing`:
    - `static int SectorOf(Vector2 offsetFromHero, int sectorCount)`: setor 0 começa no ângulo 0 (eixo +X) e cresce no sentido anti-horário.
    - `static Vector2 PointOnEllipse(float angleRad, float halfWidth, float halfHeight)`.
    - `static bool IsOutsideView(Vector2 offsetFromHero, float viewHalfWidth, float viewHalfHeight)`.
    - `static bool IsFrontSector(int sector, int sectorCount, Vector2 fleeDirection, float arcDegrees)`: o centro do setor fica a até `arcDegrees/2` da direção de fuga.
  - `public static class SpawnSectorPicker { static void FillOrder(int[] counts, float[] bias, int sectorCount, int tieRotation, int[] orderOut) }`. A pontuação é `counts[i] − bias[i]`, em ordem crescente. Empate: vence quem vem primeiro a partir de `tieRotation % sectorCount`. Não aloca.
  - `public class KiteTracker`:
    - `KiteTracker(float windowSeconds, float minSpeed, int shareWindowSpawns)`;
    - `void AddVelocity(Vector2 velocity, float deltaTime)`: média móvel exponencial, `alpha = clamp01(deltaTime / windowSeconds)`;
    - `bool TryGetFleeDirection(out Vector2 dir)`;
    - `void RecordSpawn(bool wasFront)`;
    - `float FrontShare { get; }`: fração de `true` nos últimos `shareWindowSpawns`, num buffer circular fixo.
  - `public static class CrowdPushRules { static Vector2 LateralPush(Vector2 moveDir, Vector2 heroToMonster) }`: perpendicular a `moveDir`, pro lado em que o monstro está. Se estiver exatamente na frente, vai pra esquerda (anti-horário).

- [ ] **Step 1: Escrever os testes:**
  - `SectorOf_RightIs0_UpIsQuarter`: 12 setores; `(1,0) → 0`, `(0,1) → 3`, `(-1,0) → 6`, `(0,-1) → 9`
  - `Ellipse_PointsAreOutsideView_ForAnyAspect`: pra (halfW, halfH) em {(17.8,10), (10,10), (22.5,10)}, margem 2 e ângulos a cada 5°, `PointOnEllipse(a, halfW·√2 + margem, halfH·√2 + margem)` dá `IsOutsideView == true`. *(Este teste define os semi-eixos da elipse como (meia-largura × √2 + margem, meia-altura × √2 + margem): a elipse com a proporção da tela que passa pelos cantos tem semi-eixos √2 × as meias-dimensões.)*
  - `IsFrontSector_ArcAroundFlee`: fuga `(1,0)`, arco 90°, 12 setores: o setor 0 é frente; o setor 6 não é
  - `SpawnSectorPicker_PicksLeastCrowded`: `counts = {5,2,7,…}` → `order[0] == 1`
  - `SpawnSectorPicker_BiasCountsAsFewerMonsters`: `counts {3,3}`, `bias {0,3}` → `order[0] == 1`
  - `SpawnSectorPicker_TieRotation`: todos iguais, `tieRotation = 4` → `order[0] == 4`
  - `SpawnSectorPicker_Order_CoversEverySector`: `order` contém cada índice 0..11 exatamente uma vez
  - `KiteTracker_SteadyFlee_GivesDirection`: 60 frames de `(3,0)` com dt 0,05 → `TryGetFleeDirection` verdadeiro, `dir ≈ (1,0)`
  - `KiteTracker_Slow_NoDirection`: velocidade `(0.5,0)` → falso
  - `KiteTracker_FrontShare_LastN`: N = 20; 10 × `true` e depois 20 × `false` → `FrontShare == 0`; mais 8 × `true` → `0.4`
  - `LateralPush_MonsterOnRight_PushesRight`: `moveDir (0,1)`, `heroToMonster (0.3,1)` → resultado ≈ `(1,0)`
  - `LateralPush_DeadAhead_PushesLeft`: `moveDir (0,1)`, `heroToMonster (0,1)` → ≈ `(-1,0)`
- [ ] **Step 2:** Rodar a suíte. Esperado: falha.
- [ ] **Step 3: Implementar** `HordeSpawnRules.cs`. Em `FillOrder`, insertion sort sobre `orderOut` (no máximo 12 itens), sem alocar.
- [ ] **Step 4:** Rodar a suíte. Esperado: `failed=0`.
- [ ] **Step 5: Commit** `feat(D9.4): regras de setor, elipse, anti-kite e empurrao (Core)`.

### Task 3: O dia conta desde o começo (sem loading) e todo monstro nasce com alvo

**Files:**
- Modify: `Assets/Scripts/DayCycle/DayTimer.cs` (`ResetForNewDay`, `StartDayClock`, `Update`)
- Modify: `Assets/Scripts/Player/Heroes/HeroController.cs:266-285` (comentário e setter de `GlobalAggroActive`)
- Modify: `Assets/Scripts/World/WorldPopulationManager.cs` (`SpawnOne`: aggro sempre; relógio da horda)
- Test: `Assets/Tests/EditMode/DayClockStartTests.cs`

**Interfaces:**
- Consumes: `HordeClock` (Task 1).
- Produces:
  - `DayTimer.IsClockRunning` (`public bool`);
  - `WorldPopulationManager.HordeElapsed` (`public float`, lê `HordeClock.Elapsed`);
  - campo de debug `WorldPopulationManager.debugHordeStartSeconds` (`float`, 0 = desligado).

- [ ] **Step 1: Escrever os testes** (GameObject com `DayTimer` criado no teste; `DayTimer.Instance` é setado no `Awake`, então chame o setup via `AddComponent`):
  - `ResetForNewDay_StartsClockImmediately`: depois de `ResetForNewDay(150)`, `IsClockRunning == true`
  - `ResetForNewDay_TurnsGlobalAggroOn`: depois de `ResetForNewDay(150)`, `HeroController.GlobalAggroActive == true`
- [ ] **Step 2:** Rodar a suíte. Esperado: falha.
- [ ] **Step 3: Implementar**
  - `ResetForNewDay` passa a ligar o relógio na hora e põe `GlobalAggroActive = true`, no lugar de `false`. Atualizar os comentários da Seção 4 da D8.1 que deixam de valer.
  - O `Update` continua com o gate `GameplayGate.IsActive && !HeroController.IsDying`. Isso já garante que nada conta durante o loading: o estado só vira `Gameplay` no `GameplayBootstrap.HideLoadingScreenWhenWorldReady` (e no `ShopHandler`), no mesmo frame do `LoadingScreenUI.Hide()`, que é instantâneo.
  - No `SpawnOne`, chamar `ForceImmediateAggro()` sempre, no lugar do `if (!DayResolver.PlayerSurvivedLastDay)`.
  - No `WorldPopulationManager`, criar um `HordeClock`:
    - reset em `GameEvents.OnDayStart` (retry incluso) com `debugHordeStartSeconds`;
    - tick no `Update` com `active = GameplayGate.IsActive && !HeroController.IsDying`.
- [ ] **Step 4:** Rodar a suíte. Esperado: `failed=0`.
- [ ] **Step 5: Conferir quem lê `GlobalAggroActive`/`dayClockStarted`** (`grep -rn "GlobalAggroActive\|StartDayClock" Assets/Scripts`) e confirmar que ligar no início do dia não quebra o `WorldBossSpawnManager`, o `BossSignTimelineUI` nem os monstros de emboscada. Anotar no commit o que foi conferido.
- [ ] **Step 6: Manual (usuário):** começar uma run e confirmar duas coisas: o cronômetro não andou durante a tela de loading, e os monstros já chegam perseguindo.
- [ ] **Step 7: Commit** `feat(D9.4): dia conta desde o comeco (nunca no loading); monstros nascem com alvo`.

### Task 4: Meta pela curva, fluxo contínuo e ranged só depois de 30 s no dia 1

**Files:**
- Modify: `Assets/Scripts/Core/WorldGeneration/MonsterSpawnSettings.cs`
- Modify: `Assets/Scripts/Core/WorldGeneration/EnemyDefinitionSO.cs`
- Modify: `Assets/Scripts/World/WorldPopulationManager.cs`
- Modify: `Assets/Settings/MonsterSpawnSettings.asset` e os assets `EnemyDefinitionSO` (via Editor)

**Interfaces:**
- Consumes: `HordeCurveSettings.TargetAlive`, `RangedGate.Blocks` (Task 1); `HordeElapsed` (Task 3).
- Produces:
  - `MonsterSpawnSettings.horde` (`HordeCurveSettings`), `spawnsPerSecond` (`float`), `rangedUnlockSeconds` (`float`);
  - `EnemyDefinitionSO.isRanged` (`bool`);
  - `WorldPopulationManager.CurrentTarget` (`public int`, lido pela Task 5 e pela telemetria, se quiser).

- [ ] **Step 1: Settings**
  - Adicionar ao `MonsterSpawnSettings` os campos acima, com os valores iniciais de Global Constraints (curva `shape` com as 6 chaves).
  - Remover `populationMinimum`, `populationTarget`, `populationMaximum`, `respawnInterval`, `maxCrisisSpawnsPerFrame`, `outsideCameraViewChance`, `replacementSpawnMinDistance` e `replacementSpawnMaxDistance`. O `BossSpawnSettings` tem os próprios campos e não muda.
  - Adicionar `isRanged` ao `EnemyDefinitionSO`.
- [ ] **Step 2: `WorldPopulationManager.Update`**
  - Sai `PopulationCurve01`/`ActiveMinimum`/`ActiveTarget`/`HandleCrisis`/`respawnTimer`.
  - Entra: `CurrentTarget = horde.TargetAlive(HordeElapsed, ActualDay)`. Com vivos abaixo da meta, acumulador `spawnAccumulator += spawnsPerSecond * dt`, limitado em 3. Nasce enquanto `spawnAccumulator >= 1` e vivos < meta, descontando 1 por spawn.
  - O despawn por distância fica como está nesta task (vira reposicionamento na Task 6).
- [ ] **Step 3: Bloqueio dos ranged:** no `TryPickEnemyDefinition`, pular `def.isRanged` quando `RangedGate.Blocks(ActualDay, HordeElapsed, rangedUnlockSeconds)`. Isso não pode alocar (o filtro entra no laço que já existe).
- [ ] **Step 4: Preencher os assets pelo Editor** (`.superpowers/tools/eval.sh`):
  - `isRanged = def.prefab != null && def.prefab.GetComponent<RangedEnemyController>() != null` pra cada `EnemyDefinitionSO` em `Assets/`, com `SetDirty` e `SaveAssets`. Retornar a lista dos marcados como ranged, pra mostrar ao usuário.
  - Preencher a `horde` do `MonsterSpawnSettings.asset` com os valores iniciais.
  - Conferir que o Goblin Sapper ficou `false`.
- [ ] **Step 5:** `unity recompile` e suíte. Esperado: compila e `failed=0`.
- [ ] **Step 6: Manual (usuário), dia 1:** a quantidade começa perto de 10 e sobe aos poucos; sem ranged até 0:30; sem rajadas visíveis. Pra checar o fim da curva, usar `debugHordeStartSeconds = 180`.
- [ ] **Step 7: Commit** `feat(D9.4): meta pela curva por dia, fluxo continuo e ranged so apos 30s no dia 1`.

### Task 5: Nascer fora da tela, em elipse e setores, com anti-kite

**Files:**
- Modify: `Assets/Scripts/World/WorldSpawnUtility.cs` (método novo; o antigo fica pros bosses)
- Modify: `Assets/Scripts/World/WorldPopulationManager.cs`
- Modify: `Assets/Scripts/Core/WorldGeneration/MonsterSpawnSettings.cs`

**Interfaces:**
- Consumes: `SpawnRing`, `SpawnSectorPicker`, `KiteTracker` (Task 2); `HeroController.Velocity`.
- Produces: `WorldSpawnUtility.TryGetSpawnPositionInSector(Vector2 hero, int sector, int sectorCount, float halfWidth, float halfHeight, float jitter, float minDistanceFromOrigin, int attempts, float clearance, LayerMask obstacleMask, out Vector3 position)`. Retorna `false` em vez de aceitar um ponto visível. Mantém as validações atuais (safe zone, nó caminhável, mesma área do grafo, obstáculo) e ainda exige `SpawnRing.IsOutsideView` contra a câmera atual.

- [ ] **Step 1: Settings** (no `MonsterSpawnSettings`): `sectorCount` 12, `offscreenMargin` 2, `ringJitter` 1, `kiteWindowSeconds` 3, `kiteMinSpeed` 1.5, `kiteArcDegrees` 90, `kiteBias` 3, `kiteMaxFrontShare` 0.4, `kiteShareWindowSpawns` 20.
- [ ] **Step 2: `TryGetSpawnPositionInSector`**
  - O ângulo é sorteado dentro do setor.
  - O ponto fica em `hero + PointOnEllipse(ângulo, halfW·√2 + margem, halfH·√2 + margem)`, mais um jitter radial de `[0, ringJitter]`.
  - `halfH = cam.orthographicSize` e `halfW = halfH * cam.aspect`, lidos da `Camera.main` a cada chamada.
- [ ] **Step 3: `WorldPopulationManager`**
  - O `aliveEnemies` passa a ser `List<EnemyController>`. O `AliveCount` continua.
  - Uma vez por frame em que for nascer alguém: preencher `sectorCounts[12]` com os vivos (sem bosses) por `SpawnRing.SectorOf` e preencher `sectorBias[12]`:
    - `kiteBias` nos setores de frente, se `kite.TryGetFleeDirection` der direção e `kite.FrontShare < kiteMaxFrontShare`;
    - `0` nos demais.
  - Por spawn:
    - `FillOrder` com `tieRotation` sorteado;
    - tentar setor por setor, na ordem, até um `TryGetSpawnPositionInSector` dar certo;
    - `sectorCounts[setor]++`;
    - `kite.RecordSpawn(IsFrontSector(...))`.
  - Se nenhum setor deu certo, não nasce e tenta no próximo frame.
  - `kite.AddVelocity(HeroController.Velocity, dt)` todo frame ativo.
- [ ] **Step 4:** `unity recompile` e suíte. Esperado: `failed=0`.
- [ ] **Step 5: Manual (usuário):**
  - parado, a horda chega de todos os lados ao mesmo tempo, sem lado vazio;
  - nenhum monstro surge dentro da tela;
  - correndo pro mesmo lado, aparecem mais monstros na frente, mas não todos.
- [ ] **Step 6: Commit** `feat(D9.4): spawn fora da tela em elipse, cerco por setores e anti-kite`.

### Task 6: Reposicionar quem fica longe (sem destruir)

**Files:**
- Modify: `Assets/Scripts/Enemies/EnemyController.cs` (método novo)
- Modify: `Assets/Scripts/World/WorldPopulationManager.cs` (laço do despawn por distância)

**Interfaces:**
- Consumes: a escolha de setor e o `TryGetSpawnPositionInSector` (Task 5).
- Produces: `EnemyController.TeleportTo(Vector3 position)`. Chama `CancelAttackForTeleport()`, depois `ai.Teleport(position, true)` (ou `transform.position` se `ai == null`), e zera o stuck-check (`stuckCheckPosition`, `stuckCheckTimer`, `stuckStreak`) e a janela de knockback. Não mexe em vida, status nem aggro.

- [ ] **Step 1:** Implementar `TeleportTo`.
- [ ] **Step 2:** No `WorldPopulationManager`, o monstro comum além de `enemyDespawnDistance` é reposicionado pela mesma rotina de setor e anti-kite da Task 5, no lugar de `Destroy` + `SpawnOne`. Se não achar ponto, fica onde está e tenta no frame seguinte. Bosses nunca passam por aqui.
- [ ] **Step 3:** `unity recompile` e suíte. Esperado: `failed=0`.
- [ ] **Step 4: Manual (usuário):** correndo pra longe, a horda que ficou pra trás reaparece à frente, fora da tela. Um monstro ferido ou envenenado continua com a vida e o status de antes.
- [ ] **Step 5: Commit** `feat(D9.4): monstro que fica longe e reposicionado na frente, sem destruir`.

### Task 7: O herói empurra a horda

**Files:**
- Create: `Assets/Scripts/Player/Heroes/HeroCrowdPush.cs`
- Modify: `Assets/Scripts/Player/Heroes/HeroController.cs` (Awake: adicionar o componente; linha ~722: multiplicar a velocidade)
- Modify: `Assets/Scripts/Enemies/EnemyController.cs` (`RefreshPlayerCache`: ignorar a colisão do corpo)

**Interfaces:**
- Consumes: `CrowdPushRules.LateralPush` (Task 2); `EnemyController.EnemyOnlyFilter`, `CollectDistinct`, `ApplyKnockback`.
- Produces:
  - `HeroCrowdPush.SpeedMultiplier` (`float`, 1 ou `pushSpeedMultiplier`);
  - os 🔢 `pushSpeedMultiplier` 0.5, `probeRadius` 0.6, `probeForwardOffset` 0.5, `lateralForce` 6.

- [ ] **Step 1: Separar o corpo físico.**
  - No `EnemyController.RefreshPlayerCache`, quando o cache muda (herói novo ou collider novo) e o monstro não é boss: `Physics2D.IgnoreCollision(heroBody, corpo, true)` pra cada `Collider2D` não-trigger na raiz do monstro.
  - `heroBody` = o `CapsuleCollider2D` do herói, guardado no cache.
  - Os triggers (hitboxes de ataque, contato, trigger genérico, coleta) não podem ser afetados. Por isso não usar `IgnoreLayerCollision`.
  - Ajustar o `RefreshPlayerCache` pra perceber quando o collider do corpo do herói foi trocado (Druid), comparando a referência.
- [ ] **Step 2: `HeroCrowdPush`** (no herói, adicionado no `Awake` do `HeroController`, como o `PickupRadius`).
  - No `FixedUpdate`, com o herói se movendo (`HeroController.Velocity` acima de um mínimo): `Physics2D.OverlapCircle(hero + dir × probeForwardOffset, probeRadius, EnemyOnlyFilter sem triggers, buffer[16])`, depois `CollectDistinct`.
  - Pra cada monstro não-boss e vivo: `ApplyKnockback(LateralPush(dir, monstro − hero), lateralForce)`.
  - `SpeedMultiplier = pushSpeedMultiplier` se empurrou alguém; senão 1.
  - O `EnemyOnlyFilter` fica `internal` ou ganha uma versão pública sem triggers. Não duplicar a máscara.
- [ ] **Step 3:** No `HeroController`, linha ~722, multiplicar `moveSpeed` por `crowdPush.SpeedMultiplier`.
- [ ] **Step 4:** `unity recompile` e suíte. Esperado: `failed=0`.
- [ ] **Step 5: Manual (usuário):**
  - cercado e parado, continua cercado e apanhando;
  - andando contra a horda, avança devagar e abre caminho;
  - boss continua bloqueando;
  - golpes e dano de contato dos monstros continuam acertando;
  - Druid Feral (troca de collider) e Coruja/Cambalhota (atravessam) continuam funcionando.
- [ ] **Step 6: Commit** `feat(D9.4): heroi empurra a horda (corpo sem colisao fisica + empurrao lateral)`.

### Task 8: Avisos de explosão

**Files:**
- Modify: `Assets/Scripts/Enemies/BurningSkullExplosionArea.cs`, `Assets/Scripts/Enemies/BurningSkullController.cs`
- Create: `Assets/Scripts/Enemies/ExplosionWarningArea.cs`
- Modify: `Assets/Scripts/Enemies/GoblinSapperBomb.cs` e o prefab da bomba do Goblin Sapper (filho novo com `SpriteRenderer` + `ExplosionWarningArea`, mesma sprite neutra da área do Burning Skull)

**Interfaces:**
- Produces:
  - `BurningSkullExplosionArea.SetPreview(bool visible)`: mostra o raio final com alpha 🔢 0,3, sem crescer;
  - piscar durante o crescimento: alpha oscilando, 🔢 6 Hz;
  - `ExplosionWarningArea.Show(float radius)` e `ExplosionWarningArea.SetUrgency(float t01)`: a frequência da piscada vai de 🔢 2 Hz a 8 Hz.

- [ ] **Step 1: Burning Skull.**
  - `BurningSkullController.Move` chama `explosionArea.SetPreview(distância ao herói <= 🔢 6)`.
  - No `BeginGrowth`, o preview dá lugar ao crescimento piscando.
  - Respeitar a pausa (sem animar com `GameplayGate` inativo).
- [ ] **Step 2: Bomba do Sapper.**
  - `GoblinSapperBomb.Awake` chama `warning.Show(explosionRadius)`.
  - No `Update`, `warning.SetUrgency(animator.GetCurrentAnimatorStateInfo(0).normalizedTime)`.
  - A escala do círculo bate com o `explosionRadius` real. Conferir pelo gizmo que já existe.
- [ ] **Step 3: Prefab da bomba.** Seguir o protocolo de prefab de Global Constraints (diff, backup, ForceUpdate, PrefabStage se aberto). Depois conferir o `git diff` do prefab.
- [ ] **Step 4:** `unity recompile` e suíte. Esperado: `failed=0`.
- [ ] **Step 5: Manual (usuário):** dá pra ver o raio do Burning Skull chegando e a piscada antes de explodir. A área da bomba aparece desde o momento em que ela é plantada e pisca mais rápido perto do estouro.
- [ ] **Step 6: Commit** `feat(D9.4): avisos de explosao (Burning Skull e bomba do Goblin Sapper)`.

### Task 9: Spec, roadmap e fechamento

**Files:**
- Modify: `docs/new/Mudanca_Estrutural_Deadline_9_XP_Cartas_Horda.md` (Seções 2.1, 2.2, 2.3 e 8, conforme a Seção 6 do design)
- Modify: `docs/superpowers/plans/2026-11-18-deadline-9-roadmap.md` (D9.4: marcar o que entrou; tirar a task 6, "rever a regra do mínimo 3 vezes", que já não existe)

- [ ] **Step 1:** Aplicar as mudanças de spec da Seção 6 do design, com a data 2026-11-20.
- [ ] **Step 2:** Rodar a suíte inteira. Esperado: `failed=0`.
- [ ] **Step 3: Commit** `docs(D9.4): spec e roadmap atualizados com as decisoes do D9.4`.
- [ ] **Step 4:** Revisão final da branch (`superpowers:requesting-code-review`), corrigir os achados e perguntar ao usuário antes de qualquer push ou merge.
