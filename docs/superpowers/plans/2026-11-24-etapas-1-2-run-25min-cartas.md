# Etapas 1+2: run de 25 min, gemas de XP e cartas do Barbarian — Plano de implementação

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Trocar o ciclo de dias por uma run única de 25 min (boss por minuto na ordem do usuário, enrage, troca de tier, dia/noite, mini boss, escala 1,5×), com XP em gemas no chão e uma tela de 3 cartas a cada nível (Barbarian), até a vitória ao matar o Divine God.

**Architecture:**
- Regras puras em Core, com testes EditMode, respondem a tudo que depende do segundo da run, do XP e das cartas:
  - `RunSchedule` + `RunSettings`;
  - `HeroLevelRules` (curva nova);
  - `CardRules` + `CardBonuses`.
- Os MonoBehaviours que já existem passam a perguntar a essas regras: `DayTimer` (vira o relógio da run), `WorldBossSpawnManager`, `WorldPopulationManager`, `HeroLevel`, `HeroController`/`Barbarian` e o resumo.
- O fluxo de dias, a Loja, os perks, o gold da run, o loot, os baús, o vendedor e o Continue saem do fluxo, mas os arquivos continuam no projeto.

**Tech Stack:** Unity 6000.3.8f1, C#, NUnit (EditMode, asmdef Core), A* Pathfinding. A verificação no Editor é feita por `.superpowers/tools/eval.sh` e `edittests.sh`.

**Spec:** `docs/superpowers/specs/2026-11-24-etapas-1-2-run-25min-cartas-design.md`. O executor lê a spec e o plano.

## Global Constraints

- **Branch e base:** trabalhar na branch `d9-6ab-monstros-xp` (base do trabalho). Commits terminam com `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`. Push só com confirmação do usuário (AskUserQuestion), sempre.
- **Idioma:** prosa, comentários e logs em pt-BR. Números ajustáveis levam o marcador 🔢 e ficam num asset, nunca soltos no código.
- **Onde ficam as regras:** a regra pura vai em `Assets/Scripts/Core/...` com teste em `Assets/Tests/EditMode/`.
  - O asmdef de EditMode não enxerga Assembly-CSharp, então o que depende de MonoBehaviour é verificado com um script `eval.sh` que imprime `PASS`.
- **Desempenho:**
  - nada de alocação por frame em `Update` de spawn, gemas, enrage e tela de cartas;
  - limite de 300 monstros comuns vivos (`hardCap`);
  - limite de gemas no chão: 400 🔢.
- **Prefab e cena:** antes de salvar, `git diff` e backup da cena ou do prefab no scratchpad. Depois, `ImportAsset ForceUpdate`, conferir a PrefabStage aberta e diffar contra o backup.
- **Telas:** o usuário monta o visual (tela de cartas e textos novos do Resumo). O código expõe `[SerializeField]` e preenche, como o `XpBarUI`.
- **Números da spec:**
  - **Relógio e bosses:**
    - run de 1500 s;
    - boss do espaço i em (i−1)×60 s;
    - o Divine God é o espaço 25 (1440 s), e o relógio fica parado em 0 até ele morrer.
  - **Dia e noite:**
    - ciclo de 60 s de dia + 30 s de noite;
    - à noite, a velocidade de monstros e bosses ganha ×1,15;
    - noite travada a partir de 1440 s.
  - **Enrage:** velocidade ×1,5 a partir de 60 s vivo; dano ×1,5 a partir de 120 s, e +25% de dano a cada 30 s depois disso.
  - **Troca de tier:** rampa de 0→100% do próximo tier em 90 s, começando quando nasce o último boss do tier na lista.
  - **Mini boss:** 1 por intervalo entre bosses, de 15 a 45 s depois do boss, do tier atual ou abaixo, sem repetir o anterior.
  - **Horda:** meta de dia de ~50 vivos em 60 s e ~150 em 180 s, ficando em ~150 depois. A noite é 2× o dia, limitada a 300. Os picos periódicos saem.
  - **Escala:**
    - o T1 fica como está e cada tier vale ×1,5 o anterior;
    - ranged ×0,7 de vida e ×1,4 de dano; mini boss ×4 / ×1,5;
    - boss = 20× a vida e 2× o dano do melee do tier, e o Divine God usa 30× a vida.
  - **XP:**
    - gema de 1 XP; mini boss dropa 5 gemas e boss dropa 20 🔢;
    - nível máximo 60, e o 1º nível custa 15 XP;
    - `XpToNext(L) = round(15 · L^0,6 · (L ≥ 40 ? 1,12^(L−40) : 1))`, o que dá ~3.357 XP acumulados até o nível 40 e ~15.043 até o 60.
  - **Cartas:** 3 cartas, 1 reroll por tela. Bônus de dano multiplicam entre si (×1,25 por nível); os outros somam por nível. O Primário não tem carta de velocidade de animação.

## Review Focus

1. **Vários níveis de uma vez:** um boss morto dropa 20 gemas, e o jogador pode subir 2 ou 3 níveis seguidos. As telas de cartas devem abrir uma depois da outra, sem perder nenhuma e sem empilhar. Teste na Task 9.
2. **Morte com a tela de cartas aberta, ou o Divine God morrendo no mesmo frame que o herói:** a run deve terminar uma vez só, com um único resultado. Se o herói morrer, é derrota. Testes nas Tasks 6 e 9.
3. **Lista de bosses com espaço vazio, Divine God fora do lugar ou lista com menos de 25:** o relógio e a troca de tier não podem quebrar nem dividir por zero. O minuto vazio simplesmente não tem boss. Teste na Task 1.
4. **Noite com 300 monstros e gemas passando do limite:** as gemas mais antigas se juntam numa gema grande, sem perder XP e sem passar de 400 objetos. Teste na Task 7.
5. **Pausa (Esc) com a tela de cartas aberta, ou tela de cartas no minuto 24 com o Divine God:** fechar a pausa não pode despausar o jogo por baixo da tela de cartas. Teste na Task 9.

---

### Task 1: `RunSchedule` + `RunSettings` (regras da run)

**Files:**
- Create: `Assets/Scripts/Core/Run/RunSettings.cs` (ScriptableObject) e o asset `Assets/Resources/RunSettings.asset`
- Create: `Assets/Scripts/Core/Run/RunSchedule.cs`
- Test: `Assets/Tests/EditMode/RunScheduleTests.cs`

**Interfaces:**
- **Produces, `RunSettings` (ScriptableObject):** carregado com `RunSettings.Load()` via Resources (mesmo padrão do `MonsterScalingSettings`). Campos:
  - **Relógio:** `List<BossDefinitionSO> bossOrder` (25, na ordem da spec 5.2); `float runSeconds = 1500`; `float bossIntervalSeconds = 60`.
  - **Dia e noite:** `float daySeconds = 60`; `float nightSeconds = 30`; `float nightSpeedMultiplier = 1.15f`.
  - **Enrage:** `float enrageSpeedAfter = 60`; `float enrageSpeedMultiplier = 1.5f`; `float enrageDamageAfter = 120`; `float enrageDamageMultiplier = 1.5f`; `float enrageDamageStepSeconds = 30`; `float enrageDamageStepPercent = 25`.
  - **Troca de tier:** `float tierRampSeconds = 90`.
  - **Mini boss:** `float miniBossMinDelay = 15`; `float miniBossMaxDelay = 45`.
  - **Validação:** `string[] Validate()` devolve avisos em pt-BR (Divine God fora do último espaço, espaço vazio, lista com tamanho ≠ 25). Um `OnValidate` faz `Debug.LogWarning` de cada aviso.
- **Produces, `RunSchedule`:** construída com `new RunSchedule(int[] bossTiers, RunSettings s)`. `bossTiers[i]` é o tier do espaço i, e 0 marca espaço vazio. O índice do Divine God é o último índice com tier 10, ou −1 se não houver.

  | Membro | O que devolve |
  |---|---|
  | `float BossSpawnSecond(int slot)` | `slot × bossIntervalSeconds` |
  | `int SlotsDueBy(float t)` | quantos espaços já deviam ter nascido até t |
  | `float FinalBossSecond` | segundo de nascimento do Divine God, ou `runSeconds` se não houver |
  | `bool IsNight(float t)` | `t >= FinalBossSecond \|\| (t mod (day+night)) >= day` |
  | `int TierShares(float t, out int tierA, out float shareA, out int tierB)` | quantos tiers estão ativos (1 ou 2); `shareB = 1 − shareA` (ver regra abaixo) |
  | `float EnrageSpeedMultiplier(float aliveSeconds)` | multiplicador de velocidade do enrage |
  | `float EnrageDamageMultiplier(float aliveSeconds)` | multiplicador de dano do enrage |
  | `bool ClockPaused(float t, bool finalBossAlive)` | `t >= runSeconds && finalBossAlive` |
  | `float MiniBossSecond(int slot, float random01)` | `BossSpawnSecond(slot) + Lerp(min, max, r)` |
  | `int CurrentTier(float t)` | o tier dominante em t (usado pelo mini boss) |

  Regra do `TierShares`:
  - o tier começa em 1;
  - a troca para o tier N+1 começa em `BossSpawnSecond(último espaço com tier N)`, e o tier N+1 vai de 0 a 100% em `tierRampSeconds`;
  - o T10 nunca entra como horda, então quando a próxima troca seria para o 10, ela é ignorada;
  - um tier sem nenhum boss na lista é pulado (a troca vai do N para o próximo tier que aparece na lista);
  - se uma troca nova começa antes da anterior terminar, a anterior é concluída na hora: o tier mais antigo some, e a horda nunca tem mais de 2 tiers. Isso acontece com o T8, que começa em 1140 s (Dragon), quando o T9 entra em 1200 s (Balrog).

- [ ] **Step 1: testes que falham** em `RunScheduleTests`, usando a lista da spec `{1,1,1,2,2,2,3,3,3,4,4,4,5,5,5,6,6,6,7,7,8,9,9,9,10}` e os valores padrão:
  - `BossSpawnSecond(0)==0`, `BossSpawnSecond(24)==1440`, `FinalBossSecond==1440`.
  - `IsNight`: false em 59 s, true em 60 s e em 89 s, false em 90 s, true em 1440 s, 1441 s e 1600 s.
  - `TierShares(119)`: 1 tier, tier 1.
  - `TierShares(120+45)`: 2 tiers, tierA=1, shareA≈0,5, tierB=2 (o último T1 nasce em 120 s).
  - `TierShares(120+90)`: 1 tier, tier 2.
  - `TierShares(1300)`: tier 9 (o Balrog, último do T8, nasce em 1200 s; a rampa para o T9 termina em 1290 s).
  - `TierShares(1210)`: 2 tiers, tierA=8, tierB=9, parte do T9 ≈ 10/90. O T7 já saiu, porque a rampa do T8 foi concluída na hora.
  - Em 1440 s o tier continua 9.
  - Enrage: `EnrageSpeedMultiplier(59)==1`, `(60)==1.5`. `EnrageDamageMultiplier(119)==1`, `(120)==1.5`, `(150)==1.5*1.25`, `(180)==1.5*1.5`.
  - Pausa: `ClockPaused(1500,true)==true`, `ClockPaused(1500,false)==false`, `ClockPaused(1499,true)==false`.
  - **Review Focus 3:** lista `{1,0,1,2}` (espaço vazio, sem Divine God). `FinalBossSecond==1500`, `IsNight(1499)` segue o ciclo, e `TierShares` não lança erro.
  - **Review Focus 3:** lista vazia `{}`: tier sempre 1 e `FinalBossSecond==1500`.
  - `MiniBossSecond(3, 0)==195`, `MiniBossSecond(3, 1)==225`.
- [ ] **Step 2:** rodar `bash .superpowers/tools/edittests.sh` → os testes novos FALHAM (tipo inexistente).
- [ ] **Step 3:** implementar `RunSettings` e `RunSchedule` sem alocação nas consultas. Pré-calcular, no construtor, os segundos de início de cada troca de tier num `float[]`. Criar o asset com a ordem da spec 5.2: no Editor via `eval.sh`, carregar os `BossDefinition_*.asset` de `Assets/Settings/BossDefinitions/` pelos nomes.
- [ ] **Step 4:** rodar `edittests.sh` → PASS. Pelo `eval.sh`, conferir que `RunSettings.Load().Validate().Length == 0`.
- [ ] **Step 5:** commit `feat(run): RunSchedule e RunSettings (ordem dos bosses, dia/noite, enrage, troca de tier)`.

### Task 2: escala nova (1,5× por tier, bosses como múltiplo do tier)

**Files:**
- Modify: `Assets/Scripts/Core/AI/MonsterScaling.cs`: tabelas padrão e `TargetBossHealth`/`TargetBossDamage`.
- Modify: `Assets/Resources/MonsterScalingSettings.asset`: tabelas novas. **Não** mexer em `appliedTierDamage`, que é o que torna a nova aplicação incremental.
- Modify: `Assets/Editor/MonsterScalingApplier.cs`: `FirstScaledTier = 2` e a passada de bosses.
- Test: `Assets/Tests/EditMode/MonsterScalingTests.cs`.

**Interfaces:**
- **Tabelas novas** (índice 0 sem uso):
  - `TierHealth = {0,12,18,27,40.5,60.75,91.13,136.7,205.0,307.5,461.3}`
  - `TierDamage = {0,3,4.5,6.75,10.13,15.19,22.78,34.17,51.26,76.89,115.3}`

  Ou seja, T1 × 1,5^(t−1), arredondado a 2 casas. `TierXp` fica, porque continua sendo lido pelo `XpForKill` até a Task 7.
- **Produces:**
  - constantes `BossHealthFactor = 20f`, `BossDamageFactor = 2f`, `FinalBossHealthFactor = 30f`;
  - `float TargetBossHealth(int tier, bool finalBoss, float ratioToTierBossAverage)`;
  - `float TargetBossDamage(int tier, float ratioToTierBossAverage)`.
- **Applier:**
  - percorre `RunSettings.Load().bossOrder`;
  - agrupa os bosses por tier e mede a razão de cada um contra a média dos bosses do próprio tier;
  - escreve `stats.maxHealth`, `stats.health` e `stats.attackDamage`;
  - escala os campos extras e os prefabs referenciados pela razão do dano (raios, poças, projéteis), com as mesmas regras de hoje;
  - a velocidade dos bosses não muda (o enrage cuida dela);
  - o Divine God usa `finalBoss = true`.

- [ ] **Step 1: testes que falham:**
  - `TierHealth[2]==18f`, `TierHealth[10]≈461.3f`, `TierDamage[9]≈76.89f` (tolerância 0,01);
  - `TargetBossHealth(1,false,1f)==240f`, `TargetBossHealth(10,true,1f)≈13839f`;
  - `TargetBossDamage(3,1f)==13.5f`.

  O `[SetUp]` com `ResetTables()` já existe.
- [ ] **Step 2:** `edittests.sh` → FAIL.
- [ ] **Step 3:** implementar. Atualizar o asset de settings com as tabelas novas, preenchendo pelo `eval.sh` com `SerializedObject`.
- [ ] **Step 4:** `edittests.sh` → PASS.
- [ ] **Step 5: aplicar nos prefabs.**
  1. Seguir o protocolo de prefab: backup de `Assets/Prefabs` no scratchpad e conferir que não há PrefabStage aberta.
  2. Rodar `MonsterScalingApplier.Run(false)` (prévia) e salvar o relatório no scratchpad.
  3. Rodar `Run(true)`.
  4. Conferir pelo `eval.sh` (imprime PASS se tudo bater):
     - um melee T2 qualquer tem vida igual a 18 × razão (o relatório lista o valor);
     - o `Goblin_King` tem vida ≈ 240 × razão;
     - o `Divine_God` tem vida ≈ 13.839 × razão.
  5. Rodar `Run(true)` uma segunda vez e confirmar com `git diff --stat` que nada mudou entre as duas execuções.
- [ ] **Step 6:** commit `balance(run): escala 1,5x por tier (T2-T10) e bosses = 20x vida / 2x dano do tier (Divine God 30x)`. O commit inclui os prefabs.

### Task 3: relógio da run (`DayTimer` → 25 min) e barra do HUD

**Files:**
- Modify: `Assets/Scripts/DayCycle/DayTimer.cs`
- Modify: `Assets/Scripts/UI/DayTimeBarUI.cs` e `Assets/Scripts/UI/BossSignTimelineUI.cs`
- Modify: `Assets/Scripts/Core/GameEvents.cs`, para o evento `RunClockFinished`, se for preciso.

**Interfaces:**
- **Consumes:** `RunSchedule` e `RunSettings` (Task 1).
- **Produces:**
  - `DayTimer.Schedule` (a `RunSchedule` da run, criada a partir de `RunSettings.Load()` com os tiers de `bossOrder`);
  - `DayTimer.RunElapsed` (float, segundos de run decorridos, sem pausar);
  - `DayTimer.StartRun()`, que substitui `ResetForNewDay` no início da run: zera, liga o relógio, `IsNight = false`, `GlobalAggroActive = true`, dispara `TimeChanged` e `DayStarted` (o evento é mantido para quem já escuta);
  - `DayTimer.FinalBossAlive`, setado pelo spawn de bosses.

  O comportamento:
  - `timeRemaining = runSeconds − RunElapsed`, nunca abaixo de 0;
  - `IsNight` vem de `Schedule.IsNight(RunElapsed)` e dispara `DayNightPhaseChanged` só na troca;
  - quando chega a 0, **não** existe mais `EndDay`: o relógio fica em 0 (e `ClockPaused`) enquanto `FinalBossAlive`.
- **Fluxo:** `ResetForNewDay`, `EndDay` e a chamada a `DayResolver.ResolveEndOfDay` saem do fluxo. Os callers são `MainMenuUI.StartRun` e `ShopHandler`. O `MainMenuUI` passa a chamar `StartRun()`, e o `ShopHandler` fica sem caller.
- **Barra do HUD:** `DayTimeBarUI` usa `runSeconds` como duração. O `BossSignTimelineUI` posiciona um sinal por espaço não vazio, em `BossSpawnSecond(i)`.

- [ ] **Step 1: teste que falha (eval).** Script `eval.sh` que instancia um `DayTimer` num GameObject temporário, injeta `RunSettings.Load()`, chama `StartRun()` e simula o avanço do tempo por um método `DebugAdvance(float seconds)` (só no Editor). Ele confere:
  - `IsNight == false` em 30 s, `true` em 70 s;
  - `timeRemaining == 0` em 1500 s;
  - com `FinalBossAlive = true`, `IsClockRunning` continua true em 1600 s, o `timeRemaining` segue 0 e nenhum `ResolveEndOfDay` foi chamado (contar com um listener de evento).

  Antes da implementação → FAIL.
- [ ] **Step 2:** implementar e recompilar com `unity recompile`.
- [ ] **Step 3:** eval → PASS. `edittests.sh` → sem regressão.
- [ ] **Step 4:** commit `feat(run): DayTimer vira o relogio da run de 25 min (noite pelo RunSchedule, para em 0 com o Divine God)`.

### Task 4: bosses por minuto, enrage e buff noturno

**Files:**
- Modify: `Assets/Scripts/World/WorldBossSpawnManager.cs`
- Modify: `Assets/Scripts/Enemies/EnemyController.cs`, com o hook de multiplicadores de velocidade e de dano.

**Interfaces:**
- **Produces, em `EnemyController`:**
  - `public void SetRunMultipliers(float speed, float damage)`: aplicados em cima de `stats` sem sobrescrever (`ai.maxSpeed = stats.moveSpeed * externalSlowMultiplier * runSpeedMultiplier`). O dano causado pelo monstro passa a usar `stats.attackDamage * runDamageMultiplier` onde hoje lê `stats.attackDamage` para causar dano; o executor procura todos os pontos de dano em `EnemyController` e nas subclasses com `grep attackDamage`.
  - `public float AliveSeconds`: tempo desde o spawn, contado no Update e só com `GameplayGate.IsActive`.
- **Spawn em `WorldBossSpawnManager`:**
  - a cada frame, nasce cada espaço com `SlotsDueBy(RunElapsed) > spawnedSlots`, na posição que o método já usa (`SpawnOneBoss`, com a definição vinda de `bossOrder[slot]`);
  - espaço vazio é pulado;
  - o Divine God marca `DayTimer.FinalBossAlive = true` e, ao morrer, `false`;
  - saem o timer por dia, a escolha por peso e a bag sem repetição.
- **Multiplicadores:**
  - a cada frame, todo boss vivo recebe `SetRunMultipliers(EnrageSpeed(alive) × (IsNight ? nightSpeedMultiplier : 1), EnrageDamage(alive))`;
  - os monstros comuns e os mini bosses recebem o mesmo buff noturno pela Task 5.
- **Produces:**
  - `int BossesKilled`, lido pelo resumo;
  - evento `GameEvents.FinalBossKilled`, disparado na morte do Divine God.

- [ ] **Step 1: teste que falha (eval).**
  1. Com a cena de teste carregada e o `RunSchedule` real, avança o relógio por `DayTimer.DebugAdvance` para 0, 61 e 121 s e roda o `Update` do spawner (método `DebugTick()` só no Editor).
  2. Confere `aliveBosses.Count == 1, 2, 3` e os nomes `Mother_Slime_Green`, `Mother_Slime_Blue`, `Goblin_King`.
  3. Pega o primeiro boss com `AliveSeconds` forçado para 125 e confere que a velocidade efetiva é `moveSpeed × 1,5` e o multiplicador de dano é 1,5.
  4. Limpa tudo no fim (`HandleClearAllEnemies`).

  Antes → FAIL.
- [ ] **Step 2:** implementar e recompilar.
- [ ] **Step 3:** eval → PASS. `edittests.sh` sem regressão.
- [ ] **Step 4:** commit `feat(run): boss por minuto na ordem do RunSettings, enrage e buff noturno`.

### Task 5: horda de 25 min (curva, mistura de tiers, noite 2×, mini boss)

**Files:**
- Modify: `Assets/Scripts/Core/AI/HordeCurve.cs`, que passa a ter uma curva por segundo de run.
- Modify: `Assets/Scripts/World/WorldPopulationManager.cs`
- Modify: `Assets/Settings/MonsterSpawnSettings.asset`, com a curva nova.
- Test: `Assets/Tests/EditMode/HordeCurveTests.cs`, cujos testes atuais por dia são substituídos.

**Interfaces:**
- **`HordeCurveSettings`:**
  - **Campos novos:**
    - `AnimationCurve dayTargetBySecond` (linear): (0, 10), (60, 50), (180, 150), (1500, 150) 🔢;
    - `float nightMultiplier = 2f`;
    - `int hardCap = 300`.
  - **Saem:** `days[]`, os campos de pico e `secondsToCeiling`.
  - **Método:** `int TargetAlive(float runSeconds, bool isNight)` = `min(hardCap, round(dayTarget(t) × (isNight ? nightMultiplier : 1)))`.
- **`WorldPopulationManager`:**
  - **Tier do spawn:** com `Schedule.TierShares(RunElapsed, ...)`, sorteia o tier com `Random.value < shareA ? tierA : tierB`. `WorldTierSelector` e `DaySpawnDefinition` deixam de ser usados aqui.
  - **Ranged:** o bloqueio dos primeiros 30 s vale só no começo da run (`RangedGate` com dia = 1 e o segundo da run).
  - **Mini boss:**
    - 1 por espaço de boss, em `Schedule.MiniBossSecond(slot, Random.value)`;
    - elegíveis: `def.tier <= Schedule.CurrentTier(t)`;
    - usa o `MiniBossRules.Pick` que já existe (sem repetir);
    - saem os intervalos de 30–40 s.
  - **Noite:** todo comum vivo e todo mini boss vivo recebe `SetRunMultipliers(IsNight ? nightSpeedMultiplier : 1, 1)`. Isso é feito só na troca de fase, varrendo a lista de vivos, e em cada spawn novo. Nunca por frame.

- [ ] **Step 1: testes que falham:**
  - `TargetAlive(0,false)==10`, `(60,false)==50`, `(180,false)==150`, `(900,false)==150`;
  - `(180,true)==300`;
  - `(900,true)==300` (limite).
- [ ] **Step 2:** `edittests.sh` → FAIL.
- [ ] **Step 3:** implementar. Atualizar o asset pelo `eval.sh` com `SerializedObject`.
- [ ] **Step 4:** `edittests.sh` → PASS.
- [ ] **Step 5: eval.** Avança para 130 s e roda `DebugTick` do spawner algumas vezes. Confere que nasceu algum monstro T2 (o tier vem do `EnemyController.Tier`) e que, num mini boss forçado em 200 s, o escolhido é o Werewolf (o único T2).
- [ ] **Step 6:** commit `feat(run): horda de 25 min (50/150, noite 2x, limite 300), mistura de tiers e mini boss por intervalo`.

### Task 6: fim da run, mundo da fase 1 e limpeza do fluxo antigo

**Files:**
- Modify: `Assets/Scripts/DayCycle/DayResolver.cs`, que fica com `EndRun(bool victory)`.
- Modify: `Assets/Scripts/World/ResultsHandler.cs`, `Assets/Scripts/UI/RunSummaryScreen.cs`, `Assets/Scripts/UI/RunSummaryUI.cs`
- Modify: `Assets/Scripts/UI/MainMenuUI.cs`: sai o Continue e entra `DayTimer.StartRun`.
- Modify: `Assets/Scripts/Core/Save/RunState.cs`, `RunCreation`, `SaveManager` (sem save no meio da run).
- Modify: `Assets/Scripts/World/Generation/ChunkTerrainGenerator.cs`: bioma único.
- Modify: spawners de baú, mimic, loot e vendor, mais `WorldPerkEffects` e o gold da run. Todos são desligados do fluxo; os arquivos ficam.

**Interfaces:**
- **Fim da run, `DayResolver.EndRun(bool victory)`:**
  - **Chamado por:** `GameEvents.FinalBossKilled` (vitória) ou pela morte do herói (derrota).
  - **Uma vez só:** guarda `runEnded` e ignora a segunda chamada.
  - **Derrota:** mantém o fluxo do D9.5 (CircleWipe e depois o Resumo).
  - **Vitória:** o Resumo aparece sem o CircleWipe *(decisão minha: o CircleWipe é a "morte")*.
  - **Save:** apaga o save (`SaveManager.Delete()`).
- **Resumo, `RunSummaryScreen.Show(bool victory, float secondsSurvived, int bossesKilled)`:**
  - campos opcionais `[SerializeField] TMP_Text resultText, timeText, bossesText`;
  - textos: "Vitória" ou "Derrota", tempo em `mm:ss`, e "X / 25" bosses;
  - os campos antigos (`daysText`, `goldText`) saem.
  - O usuário coloca os textos na cena dele; até lá, os campos nulos são ignorados.
  - `RunSummaryUI` ganha a mesma assinatura.
- **Menu:**
  - o botão Continue é desativado, com `SetActive(false)` no `Start` do `MainMenuUI`. A cena não é editada; o usuário remove o botão depois;
  - `StartRun` chama `DayTimer.Instance.StartRun()` no lugar de `ResetForNewDay`.
- **Mundo:**
  - `ChunkTerrainGenerator` usa `tierBiomes[0]` para qualquer posição;
  - baús e mimic param de nascer (`WorldChestSpawnManager` desabilitado no fluxo);
  - monstros não dropam loot nem gold (`LootDrop` não é instanciado na morte);
  - o vendor não aparece;
  - a Loja nunca é aberta;
  - `WorldPerkEffects.Recompute()` deixa de ser chamado no início da run.

- [ ] **Step 1: teste que falha (eval).** Chama `DayResolver.EndRun(true)` duas vezes seguidas e depois `EndRun(false)`:
  - conta 1 abertura do Resumo (listener num evento novo `GameEvents.RunEnded(bool victory)`), com `victory == true`;
  - confere `SaveManager.HasSave() == false`.

  Antes → FAIL.
- [ ] **Step 2:** implementar e recompilar.
- [ ] **Step 3:** eval → PASS. `edittests.sh` sem regressão.
- [ ] **Step 4:** `grep` confirma que não sobrou nenhum caller ativo de `ResolveEndOfDay`, `ShopHandler`, `ContinueGame` nem `WorldPerkEffects.Recompute` no fluxo da run. Listar o que sobrou no ledger como "desligado, apagar na limpeza".
- [ ] **Step 5:** commit `feat(run): fim da run (vitoria no Divine God, derrota na morte), bioma unico, sem Continue/Loja/perks/loot/baus/vendor no fluxo`.

### Task 7: gemas de XP, ímã e curva de nível até 60

**Files:**
- Modify: `Assets/Scripts/Core/Progression/HeroLevelRules.cs`, que é reescrito: sai cap/gold/crescimento e entra a curva nova.
- Test: `Assets/Tests/EditMode/HeroLevelRulesTests.cs`, cujos testes antigos são substituídos.
- Create: `Assets/Scripts/World/XpGem.cs` e `Assets/Scripts/World/XpGemPool.cs`
- Create: o prefab `Assets/Prefabs/World/XpGem.prefab`: SpriteRenderer com uma sprite existente (o executor escolhe uma gema ou orbe já presente em `Assets/Sprites` e anota qual no ledger), CircleCollider2D trigger, layer Pickup.
- Modify:
  - `Assets/Scripts/Player/HeroLevel.cs`;
  - `Assets/Scripts/Player/Heroes/HeroController.cs`: sai o `ApplyLevel`/`WriteLevelStats` do D9.6;
  - `Assets/Scripts/Player/Heroes/Druid.cs`: sai o override `WriteLevelStats`;
  - `Paladin.cs`: sai o `LevelHealthFactor`;
  - `HeroPickupRadius.cs`;
  - `EnemyController.Die`.

**Interfaces:**
- **`HeroLevelRules`:**
  - constantes: `MaxLevel = 60`, `BaseXp = 15f`, `Exponent = 0.6f`, `LateLevel = 40`, `LateGrowth = 1.12f`;
  - `long XpToNext(int level)`: `long.MaxValue` a partir do 60;
  - `struct XpResult { int level; long xpIntoLevel; int levelsGained; }`;
  - `XpResult AddXp(int level, long xpIntoLevel, long xpGained)`: no nível 60 a barra fica cheia e o resto é descartado.
- **Gemas:**
  - `XpGemPool.Instance.Spawn(Vector2 pos, int xp)`;
  - pool com limite `maxGems = 400` 🔢: ao passar dele, a gema ativa mais antiga absorve o XP da nova e não nasce objeto novo;
  - `XpGem` tem `int Xp`, `void Attract(Transform hero)` (voa até o herói a 12 u/s 🔢) e `void Collect()`, que dispara `GameEvents.XpCollected(int)` e devolve a gema ao pool.
- **Ímã:**
  - `HeroPickupRadius.OnTriggerStay2D` passa a chamar `XpGem.Attract(heroTransform)`;
  - a coleta acontece por distância (< 0,3) dentro do `XpGem.Update`;
  - o raio é o `PickupRadius` que já existe (multiplicado pela carta de ímã na Task 9).
- **Drops:**
  - `EnemyController.Die` chama `XpGemPool.Instance.Spawn(pos, 1)` tantas vezes quanto `RunSettings`: 1 para monstro comum, `miniBossGems = 5` para mini boss e `bossGems = 20` para boss 🔢, espalhadas num raio de 0,5;
  - continua respeitando `GrantsXp`;
  - o evento `EnemyKilledXp` sai.
- **`HeroLevel`:**
  - escuta `XpCollected`, aplica `AddXp` e dispara `GameEvents.HeroLeveledUp(level)` uma vez por nível ganho;
  - continua expondo `Level`, `XpIntoLevel` e `XpToNext` para o `XpBarUI`;
  - saem o `goldFromXpToday`, o limite diário e o `ApplyLevel`.

- [ ] **Step 1: testes que falham:**
  - `XpToNext(1)==15`, `XpToNext(40)==137`, `XpToNext(59)==1492`, `XpToNext(60)==long.MaxValue`;
  - a soma de `XpToNext(1..39)` está entre 3200 e 3500 e a de `1..59` entre 14500 e 15500;
  - `AddXp(1,0,15)` → nível 2, xp 0, ganhou 1;
  - `AddXp(1,0,15+23+5)` → nível 3, xp 5, ganhou 2 (**Review Focus 1**);
  - `AddXp(60,0,999)` → nível 60, ganhou 0.
- [ ] **Step 2:** `edittests.sh` → FAIL.
- [ ] **Step 3:** implementar regras, pool, gema, ímã e drops. Criar o prefab seguindo o protocolo e ligar o pool na cena de teste (`Systems`) ou criá-lo por código no `SystemsBootstrap`. A segunda opção é a preferida, porque não mexe na cena.
- [ ] **Step 4:** `edittests.sh` → PASS.
- [ ] **Step 5: eval (Review Focus 4).** Spawna 450 gemas com xp 1. Confere que existem ≤ 400 objetos ativos e que a soma de `Xp` das ativas é 450.
- [ ] **Step 6:** commit `feat(xp): gemas de XP no chao com ima, pool com limite e curva de nivel ate 60 (sem bonus de status por nivel)`.

### Task 8: cartas (Core)

**Files:**
- Create: `Assets/Scripts/Core/Cards/CardDefinition.cs` (ScriptableObject)
- Create: `Assets/Scripts/Core/Cards/CardRules.cs`
- Create: `Assets/Scripts/Core/Cards/CardBonuses.cs`
- Create: os assets `Assets/Settings/Cards/*.asset` do catálogo do Barbarian
- Test: `Assets/Tests/EditMode/CardRulesTests.cs`

**Interfaces:**
- **Tipos:**
  - `enum CardStat { Damage, Area, Cooldown, AnimSpeed, MoveSpeed, Magnet, MaxHealth }`
  - `enum CardAbility { None, Primary, Shift, Ultimate }`
- **`CardDefinition`:**
  - campos: `string id`, `string title`, `string description` (com `{0}` para o valor), `Sprite icon`, `CardAbility ability`, `CardStat stat`, `float valuePerLevel`, `int maxLevel = 5`, `string heroId` (vazio = todos);
  - `bool Multiplicative => stat == CardStat.Damage`.
- **`CardRules.Roll(IReadOnlyList<CardDefinition> pool, IReadOnlyDictionary<string,int> owned, string heroId, int count, System.Random rng, List<CardDefinition> result)`:**
  - sorteia até `count` cartas distintas;
  - exclui as que já estão no `maxLevel` e as de outro herói;
  - devolve menos de `count` se não houver cartas suficientes;
  - zero alocação além do `result` recebido.
- **`CardBonuses`:**
  - `void Recompute(IReadOnlyList<CardDefinition> pool, IReadOnlyDictionary<string,int> owned)`;
  - `float Get(CardAbility ability, CardStat stat)`. Para dano, `valuePerLevel^nível` (1,25^n). Para os outros, `1 + valuePerLevel × nível`. O cooldown usa `max(0,2, 1 − valuePerLevel × nível)`.
- **Catálogo do Barbarian** (spec 6.4; `heroId` vazio = genérica):

  | Carta | Valor por nível |
  |---|---|
  | Primary Damage | 1,25 |
  | Primary Area | 0,15 |
  | Shift Damage | 1,25 |
  | Shift Cooldown | 0,10 |
  | Ultimate Damage | 1,25 |
  | Ultimate Cooldown | 0,10 |
  | Ultimate Area | 0,15 |
  | MoveSpeed | 0,08 |
  | Magnet | 0,25 |
  | MaxHealth | 0,15 |

  Para a Ultimate, "Cooldown" significa a energia necessária. As cartas Shift Area, Shift AnimSpeed e Ultimate AnimSpeed **não** são criadas, porque o Shift do Barbarian (fúria) não tem área nem animação própria e o arremesso é instantâneo. Isso vai anotado no ledger para o usuário confirmar. O Primário nunca tem AnimSpeed (spec).

- [ ] **Step 1: testes que falham:**
  - `Roll` com 10 cartas e uma no máximo nunca devolve a carta no máximo e devolve 3 distintas (100 sorteios com `System.Random(seed)`);
  - com 2 cartas disponíveis devolve 2;
  - com uma carta de outro herói, ela nunca aparece;
  - `Get(Primary, Damage)` com nível 2 → 1,5625;
  - `Get(None, MoveSpeed)` com nível 3 → 1,24;
  - `Get(Shift, Cooldown)` com nível 10 de 0,10 → 0,2 (piso).
- [ ] **Step 2:** `edittests.sh` → FAIL.
- [ ] **Step 3:** implementar e criar os 10 assets pelo `eval.sh`. Os títulos e descrições em pt-BR seguem o padrão "Machado Pesado — +25% de dano do ataque primário". O executor escolhe os nomes, e o usuário pode renomear.
- [ ] **Step 4:** `edittests.sh` → PASS.
- [ ] **Step 5:** commit `feat(cartas): CardDefinition, sorteio sem repetir e bonus por nivel (catalogo base do Barbarian)`.

### Task 9: tela de cartas e bônus aplicados no Barbarian

**Files:**
- Create: `Assets/Scripts/UI/CardScreenUI.cs` e `Assets/Scripts/UI/CardSlotUI.cs`
- Create: `Assets/Scripts/Player/HeroCards.cs`, que fica no Systems, ao lado do `HeroLevel`
- Modify: `Assets/Scripts/Player/Heroes/HeroController.cs` e `Assets/Scripts/Player/Heroes/Barbarian.cs`
- Modify: `Assets/Scripts/UI/PauseMenuUI.cs` (Review Focus 5)

**Interfaces:**
- **`HeroCards` (singleton):**
  - guarda `Dictionary<string,int> owned` e o `CardBonuses`;
  - escuta `HeroLeveledUp` e enfileira uma oferta por nível;
  - `void Choose(CardDefinition card)` sobe o nível da carta, faz `Recompute` e chama `HeroController.Instance.ApplyCardBonuses(bonuses)`;
  - `bool Reroll()` só funciona uma vez por oferta;
  - expõe `event Action<IReadOnlyList<CardDefinition>> OfferReady` para a UI;
  - `void EnqueueOffer(IReadOnlyList<CardDefinition> cards)` existe para o baú e o vendedor da etapa 3.
- **`CardScreenUI`:**
  - campos `[SerializeField] GameObject root`, `CardSlotUI[] slots` (3) e `Button rerollButton`;
  - ao receber uma oferta: `TimeManager.Instance.Pause()`, preenche os slots e liga os botões;
  - ao escolher: `HeroCards.Choose`, depois, se houver próxima oferta na fila, mostra; se não, `TimeManager.Instance.Resume()` e esconde;
  - **o usuário monta o visual.** O executor deixa o script pronto e avisa no final que falta a cena.
- **`CardSlotUI`:** campos `Image icon`, `TMP_Text title, description, level` e `Button button`.
- **`HeroController`:**
  - `public virtual void ApplyCardBonuses(CardBonuses b)`;
  - a base aplica:
    - `MoveSpeedMultiplier = b.Get(None, MoveSpeed)`;
    - `PickupRadiusMultiplier = b.Get(None, Magnet)` (e chama o refresh do raio que já existe);
    - `MaxHealthMultiplier = b.Get(None, MaxHealth)`, com a vida atual subindo na mesma proporção;
    - `PrimaryDamageMultiplier`, `ShiftDamageMultiplier` e `UltimateDamageMultiplier = b.Get(..., Damage)`;
    - `SetSecondaryCooldownMultiplier(b.Get(Shift, Cooldown))`;
    - `UltimateChargeRequiredMultiplier` passa a ler um campo que recebe `b.Get(Ultimate, Cooldown)`.
- **`Barbarian`:**
  - o override chama a base e aplica `groundCrackAreaMultiplier = b.Get(Primary, Area)` e a escala do machado arremessado = `b.Get(Ultimate, Area)` (onde o projétil é instanciado);
  - os `Recompute*` de perk deixam de rodar (a Task 6 tirou o `WorldPerkEffects`), então não sobrescrevem as cartas.
- **`PauseMenuUI`:** ao fechar, só chama `Resume()` se o `CardScreenUI` não estiver aberto.

- [ ] **Step 1: teste que falha (eval, Review Focus 1, 2 e 5).**
  1. Instancia `HeroCards` e um `CardScreenUI` com slots falsos num GameObject temporário.
  2. Dispara `HeroLeveledUp` duas vezes e confere que existem 2 ofertas na fila e que o jogo está pausado.
  3. Faz `Choose` e confere que ainda está pausado e que a 2ª oferta está na tela.
  4. Faz `Choose` de novo e confere que despausou.
  5. Reroll: o 1º devolve true e o 2º devolve false na mesma oferta.
  6. Com a tela aberta, chama `RunEnded(false)` e confere que a tela fecha sem despausar o jogo por baixo da derrota.
  7. Com a tela aberta, abre e fecha o pause e confere que o jogo continua pausado.

  Antes → FAIL.
- [ ] **Step 2: teste que falha (eval, bônus).** Com o prefab do Barbarian instanciado, chama `ApplyCardBonuses` com a carta Primary Damage no nível 2 e confere `HeroController.PrimaryDamageMultiplier ≈ 1,5625`. Com Magnet no nível 1, confere `PickupRadius ≈ base × 1,25`.
- [ ] **Step 3:** implementar e recompilar.
- [ ] **Step 4:** evals → PASS. `edittests.sh` sem regressão.
- [ ] **Step 5:** commit `feat(cartas): tela de 3 cartas com reroll (pausa, fila de niveis) e bonus aplicados no Barbarian`.

### Task 10: telemetria da run e verificação final

**Files:**
- Modify: `Assets/Scripts/Telemetry/RunTelemetryRecorder.cs`

**Interfaces:**
- **Tentativa:** uma por run (`dayNumber` sai e entra `outcome` = victory/died/abandoned).
- **Snapshot por segundo:**
  - `runSecond`, `isNight`, `aliveMonsters`, `aliveBosses`;
  - `maxBossEnrageSeconds` (o maior `AliveSeconds` entre os bosses vivos);
  - `heroLevel`, `heroXpIntoLevel`, `gemsOnGround`.
- **Evento novo:** a escolha de carta, com o segundo, o id e o nível.
- **Sai:** o gold de monstro e de sobrevivência.

- [ ] **Step 1: teste que falha (eval).** Chama o método de snapshot com o `DayTimer` em 70 s (noite) e confere `isNight == true` e `runSecond == 70` no registro serializado. Antes → FAIL.
- [ ] **Step 2:** implementar. → PASS.
- [ ] **Step 3:** `edittests.sh` completo → tudo passa, menos os 3 `BagTests` que já falhavam antes. Recompilar sem warnings novos.
- [ ] **Step 4:** commit `feat(telemetria): uma tentativa por run, snapshot com segundo/noite/enrage/nivel/gemas e escolhas de carta`.
- [ ] **Step 5:** revisão final da branch inteira (subagente opus), corrigir os Critical e Important com RED→GREEN e depois pedir ao usuário o teste no jogo e a montagem do visual da tela de cartas e dos textos do Resumo.
