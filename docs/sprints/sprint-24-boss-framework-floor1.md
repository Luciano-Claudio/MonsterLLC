# Sprint 24 — Boss Framework + Boss Timer + Bosses Floor 1

## Objetivo

Criar o Boss Framework genérico (Boss Timer com rotação por Floor, agressão imediata, imunidade a knockback) e torná-lo real com os 3 bosses do Floor 1: Goblin King, Mother Slime Green e Mother Slime Blue.

## Sistemas adicionados

- `BossSpawnManager` — componente irmão de `FloorDefinition`/`FloorPopulationManager`, relógio fixo de 10s por Floor (não entra em `aliveEnemies`/Minimum/Target/Maximum da população comum), spawna 1 boss por tick em rotação embaralhada 1x só (depois repete a mesma ordem).
- `FloorSpawnUtility` — extração de `TryGetRandomGraphPoint()`/`GetOwnerGraph()`/`RestrictToOwnerGraph()` do `FloorPopulationManager` pra uma classe estática compartilhada entre população comum e Boss Timer.
- `EnemyController.SetIsBoss()`/`ForceImmediateAggro()` — expostos publicamente pro spawner marcar o boss e ligar `isInCombat`/`combatLocked` direto, sem fase de patrulha/idle esperando `observationRadius`.
- `MotherSlimeController` — 1 script compartilhado por Mother Slime Green/Blue, spawna 3 filhotes (`childSlimePrefab`) em posições fixas (`childSpawnPoints`) no momento exato da morte (`AnimationDieEndEvent` virou `virtual` na base pra isso).
- Boss × Camuflagem do Ranger — boss perde o alvo igual qualquer monstro comum durante a camuflagem, mas recupera automaticamente no instante em que ela cai (detecção da transição `true → false` de `HeroController.IsPlayerUntargetable`), sem precisar ser redetectado por `observationRadius`.

## Decisões técnicas

Detalhadas ficha a ficha em `docs/sprint-24-task-breakdown.md` (decisões/suposições, diffs completos, checklist). Resumo das principais:

- Boss Timer nunca reseta (nem com morte de boss, nem no fim do dia) e bosses empilham sem fila/limite — decisão do usuário, registrada em `docs/sprint-24-boss-spawn-system-specs.md`.
- Goblin King não precisou de script novo — `MeleeEnemyController` padrão, igual a qualquer Melee comum do Bestiário.
- `isBoss` e agressão imediata viram setters públicos chamados pelo spawner, em vez de depender de cada prefab já vir configurado certo no Inspector.

## Arquivos/classes principais

- `Assets/Scripts/World/BossSpawnManager.cs`, `Assets/Scripts/World/FloorSpawnUtility.cs` (novos).
- `Assets/Scripts/Enemies/EnemyController.cs` — `SetIsBoss()`, `ForceImmediateAggro()`, `AnimationDieEndEvent()` virtual, tratamento de Camuflagem no `Update()`.
- `Assets/Scripts/Enemies/MotherSlimeController.cs` (novo).
- `Assets/Scripts/World/FloorPopulationManager.cs` — refatorado pra usar `FloorSpawnUtility` (comportamento idêntico).
- Prefabs: `Goblin_King.prefab`, `Mother_Slime_Green.prefab`, `Mother_Slime_Blue.prefab`.

## Testes executados

Checklist manual de 14 itens em `docs/sprint-24-task-breakdown.md` (rotação sem repetir dentro do ciclo, repetição da mesma ordem a partir do 2º ciclo, empilhamento, persistência do timer entre trocas de Floor, agressão imediata, Goblin King com golpe real + imunidade a knockback, Mother Slime Green/Blue spawnando os 3 filhotes certos, boss × Camuflagem do Ranger) — **confirmado pelo usuário em Play Mode**.

## Bugs conhecidos

Nenhum pendente.

## Dívida técnica

- Boss Timer/rotação (`bossSpawnTimer`/`rotationIndex`) não resetam no fim do dia — mesma limitação pré-existente do `FloorPopulationManager` (nenhum dos dois se inscreve em `GameEvents.OnDayStart` hoje). Registrado como dívida pré-existente, não nova desta sprint.
- Valores 🔢 de balanceamento (dano/vida dos 3 bosses, `bossSpawnInterval`) são placeholder de teste.

## Próximos passos

Floor 1 fecha com os 3 bosses derrotáveis. Próxima peça da Deadline 6: Sprint 25 (Floor 2 — bosses de arquitetura padrão).
