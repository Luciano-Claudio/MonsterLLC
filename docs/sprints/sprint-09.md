# Sprint 09 — Main Menu + Run Creation Flow + Loot Básico

## Objetivo

`New Game` deixa de ser conceito e vira fluxo real (Menu → Mode/Hero/Map Select → `RunState` de verdade). Monster Essence passa a existir como loot: dropa do inimigo, é coletada via Pickup Radius. Primeira sprint da Deadline 3 (Primeiro Vertical Slice).

## Sistemas adicionados

- **`GameStateManager` com estados reais** — enum completo desenhado de uma vez (`MainMenu`, `ModeSelect`, `HeroSelect`, `MapSelect`, `Gameplay`, `Paused`, `Results`, `Shop`, `GameOver`), em vez de esticado sprint a sprint. `Paused` continua conceitualmente separado de `TimeManager.IsPaused` (um descreve a tela, o outro trava o tempo).
- **`RunCreation`** — lógica pura que preenche um `RunState` de verdade a partir do fluxo de New Game (antes só existia via teste manual, Sprint 4).
- **`MainMenuUI`** — `NewGame()` (Mode → Hero → Map → cria `RunState` → Gameplay) e `ContinueGame()` (carrega save existente ou reconhece a ausência dele).
- **`LootDefinition` + `LootDrop`** — primeiro material de loot do jogo (Monster Essence), coleta automática por proximidade (Pickup Radius). `MeleeEnemyPrototype` passa a dropar ao morrer.

## Decisões técnicas

- **Correção de enquadramento em relação ao texto original da sprint:** a frase "Free continua pendência aberta do GDD" não é precisa. O GDD Seção 42 (Modo Free) está **✅ fechado e congelado** — não falta nenhuma decisão de design. O que de fato está pendente é só a *ordem de implementação*: Free Mode entra no jogo oficialmente na **Deadline 12**, bem mais tarde, não porque o design esteja em aberto. Esta sprint oferece só Standard por sequenciamento de roadmap, não por indecisão de GDD.
- **`GameStateManager.CurrentState` passou a nascer em `MainMenu`, não mais em `Gameplay`** — o valor default da Sprint 3 nunca importou porque o enum nunca era lido; agora que é consultado de verdade, o estado inicial correto é a tela de menu, não gameplay.
- **`RunCreation.cs` colocado em `Core/Save/`**, junto de `RunState`/`SaveManager`, em vez de solto em `Core/` como o texto original sugeria — par natural, mesmo critério aplicado nas Sprints 7/8.
- **Nova pasta `Core/Loot/`** para `LootDefinition.cs` — mesmo padrão do `World/` para `Stair.cs` (Sprint 6): categoria criada antecipando crescimento (Sprint 10 — Inventory/Bag vai adicionar mais tipos aqui), não porque já existam múltiplos arquivos hoje.
- **Pickup Radius confirmado coerente com o GDD Seção 37** ("Loot entra no Pickup Radius → Player tenta coletar", automático, sem E) — a checagem de distância do `LootDrop` implementa exatamente essa regra.
- **Teste do "Continue Game sem save" não foi demonstrado de fato** — havia um `save.json` real em disco, sobrando dos testes do `SaveTest` desde a Sprint 4/5/6 (`Dia 5, Gold 1200`), então o caminho "sem save" nunca ficou vazio neste ambiente de teste. O caminho *com* save foi validado de ponta a ponta (`ContinueGame()` carregou e logou corretamente) — o caminho vazio é um `if (!HasSave())` de uma linha, risco baixo o suficiente para não exigir apagar o save só para demonstrar.

## Arquivos/classes principais

- `Assets/Scripts/Core/GameState/GameStateManager.cs` — enum completo.
- `Assets/Scripts/Core/Save/RunCreation.cs`.
- `Assets/Scripts/Core/Loot/LootDefinition.cs`.
- `Assets/Scripts/World/LootDrop.cs`.
- `Assets/Scripts/UI/MainMenuUI.cs`.

## Eventos adicionados

- `GameEvents.OnLootCollected(LootDefinition loot)`.

## Testes executados

- **Automatizado (EditMode):** `RunCreationTests.CreateNewRun_SetsFieldsCorrectly` — passou, junto com todos os já existentes (confirmado pelo usuário no Test Runner).
- **Manual (Play Mode):** `New Game` percorre Mode → Hero → Map → `RunState` criado → Gameplay, tudo logado corretamente; Barbarian mata o `Enemy_Test`, loot de Monster Essence aparece na posição da morte e é coletado ao se aproximar; `Continue Game` carrega um save real existente corretamente (ver nota acima sobre o caminho "sem save" não ter sido exercitado).

## Bugs conhecidos

Nenhum em aberto.

## Dívida técnica

- Herdada: prioridade entre interagíveis simultâneos (Sprint 35), Day Timer/-30s real e destruição de loot no Death Flow (ambos previstos para a Sprint 11, conforme nota do plano de produção).
- `GameStateManager` ainda não tem todos os estados consumidos — `Paused`, `Results`, `Shop`, `GameOver` existem no enum mas nenhum sistema os usa ainda (chegam nas Sprints 10-12, conforme cada sistema nascer).
- Caminho "Continue Game sem save" não foi validado neste ambiente (ver "Decisões técnicas") — o código é trivial o suficiente para não bloquear, mas vale testar de fato (apagando `save.json`) antes da Sprint 12 fechar Continue Game de vez.

## Próximos passos

Com o fluxo de Menu e o primeiro loot reais, a Sprint 10 (Inventory + Vendor + Gold) tem onde plugar a Bag de verdade — `LootDrop` só precisa passar a guardar em vez de logar, e o padrão `Interactable` (Sprint 6) já está pronto pra virar o NPC vendedor.
