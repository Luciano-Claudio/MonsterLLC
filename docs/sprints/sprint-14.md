# Sprint 14 — Attack Budget + Population Skeleton

## Objetivo

Hordas legíveis: com 10+ inimigos num Floor, só um número limitado ataca ao mesmo tempo (Melee e Ranged com budgets separados, GDD Seção 14). Inimigos passam a nascer sozinhos, gradualmente, em vez de colocados um a um na mão.

## Sistemas adicionados

- **`AttackBudgetTracker`** (lógica pura) — budgets Melee/Ranged independentes, `TryReserve`/`Release`.
- **`AttackBudgetManager`** (singleton) — wrapper do tracker, exposto pro `EnemyController` reservar/liberar slot em cada ciclo de ataque.
- **`FloorPopulationManager`/`PopulationConfig`** — spawn gradual em `spawnPoints`, respeitando `minimum`/`target`/`maximum`.
- **`AttackBudgetIndicatorUI`** — indicador de debug (`Melee: X/Y | Ranged: X/Y`), não é HUD definitivo do GDD.

## Decisões técnicas

- **`AttackType.cs` foi para `Core/`, não `Enemies/` como o texto original propunha.** `AttackBudgetTracker`/`AttackBudgetManager` (Core.asmdef) recebem `AttackType` como parâmetro — Core não pode referenciar `Enemies/` (fora do asmdef, direção unidirecional, mesmo padrão de bug já visto nas Sprints 10/11). Conferido também que `EditMode.asmdef` só referencia 3 GUIDs específicos, sem acesso ao assembly default — os testes nem enxergariam `AttackType` se ficasse fora do Core.
- **Bug real corrigido antes de testar — `Die()` não liberava o slot do budget se o inimigo morresse no meio do próprio ataque** (Telegraph/Active/Recovery). Sem o fix, isso seria um vazamento permanente e contradiria o próprio passo 4 do teste manual do texto original ("matar um inimigo que estava atacando → o slot libera"). Corrigido liberando o slot em `Die()` quando `attackState != Idle`.
- **Bug de configuração de Editor, não de código — inimigos empilhando uns sobre os outros.** `Collider2D` sem nenhum `Rigidbody2D` não resolve overlap (colisão só é separada fisicamente quando pelo menos um dos dois lados tem `Rigidbody2D` Dynamic). Corrigido adicionando `Rigidbody2D` (Dynamic, Gravity Scale 0, Freeze Rotation Z) aos prefabs `Enemy_Test`/`Enemy_Test_Ranged`. Nota registrada: o movimento continua via `transform.Translate()` (mesmo padrão do Player desde a Sprint 2, não `Rigidbody2D.MovePosition()`), então a separação física pode ficar um pouco menos suave que o ideal em grupos grandes — aceitável para esta sprint, migrar pra `MovePosition()` seria um ajuste de movimento separado, não desta sprint.
- **Esclarecimento pedido pelo usuário — população nunca passa do `target` (8), só do `minimum`/`target`.** Isso é comportamento correto, não bug: a GDD Seção 23 diz literalmente "Reposição gradual **abaixo do Target**" — o código para de tentar repor assim que atinge o `target`. O `maximum` (12) só entraria em jogo com a "reação dinâmica" da mesma seção ("jogador matando rápido → aumenta a frequência de reposição, até o teto do Maximum") — já registrada como simplificação não implementada no texto original desta sprint (`respawnInterval` fixo). Sem essa peça, não existe caminho no código atual pra população passar do `target` — `maximum` fica configurado mas inatingível até essa dívida ser resolvida.

## Arquivos/classes principais

- `Assets/Scripts/Core/AttackType.cs`, `AttackBudgetTracker.cs`, `AttackBudgetManager.cs`.
- `Assets/Scripts/World/PopulationConfig.cs`, `FloorPopulationManager.cs`.
- `Assets/Scripts/UI/AttackBudgetIndicatorUI.cs`.
- `Assets/Scripts/Enemies/EnemyController.cs` — `AttackType` abstrato, reserva/libera slot.
- `Assets/Prefabs/Test/Enemy_Test.prefab`, `Enemy_Test_Ranged.prefab` — primeiros prefabs de inimigo do projeto, com `Rigidbody2D`.

## Eventos adicionados

Nenhum evento novo em `GameEvents`.

## Testes executados

- **Automatizado (EditMode):** `AttackBudgetTrackerTests` (4 testes: dentro do budget, excede budget, libera pra reuso, Melee/Ranged independentes) — passou, junto com os 32 testes totais da suíte (confirmado pelo usuário no Test Runner).
- **Manual (Play Mode):** população nasce gradualmente no Floor 1, não populada de uma vez; indicador de budget nunca passou de `Melee: 3/3`/`Ranged: 2/2` mesmo com múltiplos inimigos no raio de ataque simultaneamente; matar um inimigo em pleno ataque liberou o slot corretamente (confirma o fix do `Die()`); população caiu abaixo do `target` após vários kills e voltou a subir sozinha após o `respawnInterval`; população nunca ultrapassou o `target` (comportamento esperado, ver "Decisões técnicas"); Barbarian matou tanto Melee quanto Ranged normalmente, coletando loot de ambos.
- **Regressão pega em teste manual, corrigida:** inimigos empilhando uns sobre os outros ao convergir pro jogador, mesmo com 5 `spawnPoints` espalhados corretamente — causa raiz diagnosticada pelo próprio usuário (falta de `Rigidbody2D`), corrigida no Editor sem mudança de código.

## Bugs conhecidos

Nenhum em aberto.

## Dívida técnica

- **Reação dinâmica de população não implementada** (GDD Seção 23: "jogador matando rápido → aumenta frequência de reposição, até o teto do Maximum") — `respawnInterval` é fixo, `maximum` fica inatingível pela via normal de reposição. Sem sprint atribuída.
- **Posições de spawn "válidas, fora da câmera, longe do jogador" (GDD Seção 23) reduzidas a "sorteia entre pontos pré-posicionados manualmente"** — aceito como simplificação desta sprint, sem checagem automática de câmera/distância.
- Herdada (Sprint 13): `TargetPosition` não trava no início do telegraph (GDD Seção 22) — sem sprint atribuída.
- Herdada (Sprint 12): compra na Loja/"Start Next Day" não geram save adicional (por design); Weapon Tier stub até a Sprint 33.

## Próximos passos

Com Attack Budget limitando hordas e população nascendo sozinha, a Sprint 15 (Floor Sleep/Activation v1) tem uma base real de múltiplos Floors com população/IA rodando pra validar a suspensão seletiva — inclusive resolvendo o efeito colateral observado nesta sprint de o `FloorPopulationManager` continuar gerando inimigos mesmo com o jogador fora do Floor 1 (esperado até aqui, por não existir Floor Sleep ainda).
