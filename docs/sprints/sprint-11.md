# Sprint 11 — Demanda + Results + Shop Skeleton + Game Over Funcional

## Objetivo

O ciclo econômico do dia fecha de ponta a ponta, incluindo o caminho de falha. O Day Timer nasce de verdade, destravando duas dívidas antigas: a penalidade de -30s real (Sprint 7/8) e a destruição de loot no Death Flow (Sprint 8).

## Sistemas adicionados

- **`DemandCalculator`** (lógica pura) — `40 × 2^(Dia-1)` (GDD Seção 39).
- **`DemandTracker`** — acompanha vendas de Monster Essence do dia, compara contra a demanda.
- **`DayTimer`** — contagem regressiva real (100s), pausa via `TimeManager`, penalidade de -30s, dispara `DayResolver` ao zerar.
- **`DayResolver`** — fim de dia: destrói loot restante na Bag, valida demanda, decide Results ou Game Over.
- **`GameOverHandler`/`ResultsHandler`/`ShopHandler`** — reagem a `GameEvents.OnGameStateChanged`; `ShopHandler.StartNextDay()` avança o dia e reseta Timer/Demanda.
- **HUD:** `TimeIndicatorUI`, `DemandIndicatorUI`.
- **`MeleeEnemyPrototype.monsterEssenceDropAmount`** — campo configurável no Inspector, adicionado a pedido do usuário pra viabilizar teste manual da demanda sem precisar matar dezenas de inimigos.

## Decisões técnicas

- **`DayTimer`/`DayResolver` saíram de `Core/` para uma nova pasta `Assets/Scripts/DayCycle/`, fora do Core.asmdef** — o texto original colocava os dois em `Core/`, mas `DayResolver.ResolveEndOfDay()` precisa chamar `BagController.Instance.Bag.Clear()`, e `BagController` vive em `Player/` (fora do Core). Como `Core.asmdef` é referenciado de forma unidirecional pelo assembly default (regra estabelecida desde o `BagController` da Sprint 10), isso não compilaria dentro de `Core/`. Como `DayTimer.EndDay()` chama `DayResolver` diretamente, os dois precisaram sair juntos — `DemandCalculator`/`DemandTracker` continuam em `Core/` normalmente, por não tocarem nada fora do assembly.
- **Bug real corrigido em teste manual — reentrância em `HeroController.OnDeath()`:** sem guarda, dois hits do inimigo antes do respawn terminar disparavam `OnDeath()` duas vezes — penalidade de -30s duplicada e duas chamadas concorrentes de `TransitionHelper.PlayTransition()`, que o EasyTransition não suporta (erro "You have to assing a transition" + respawn duplicado, reproduzido ao vivo pelo usuário). Corrigido com uma flag `isDead` que bloqueia `TakeDamage()` até o respawn concluir.
- **Removido o `return` condicional que eu mesmo tinha colocado em `OnDeath()`** (pular `Respawn()` quando a penalidade zera o dia) — decisão tomada antes de codar, seguindo a ramificação literal da GDD Seção 11 ("SIM → resolve fim de dia, sem respawn"), mas que geraria uma regressão pior: o herói ficaria com 0 HP parado no lugar da morte indefinidamente, sem posição/HP resetados pro próximo dia. `Respawn()` agora roda sempre, incondicionalmente — a ramificação da GDD é sobre a tela ir para Results/Game Over em vez de "dia continua normalmente", não sobre deixar o personagem fisicamente quebrado.
- **`GameStateManager.SetState()` ganhou `GameEvents.GameStateChanged`** — não estava no plano original, mas os handlers de Results/Shop/GameOver precisavam reagir a troca de estado sem fazer polling em `Update()`.

## Arquivos/classes principais

- `Assets/Scripts/Core/DemandCalculator.cs`, `DemandTracker.cs`.
- `Assets/Scripts/DayCycle/DayTimer.cs`, `DayResolver.cs`.
- `Assets/Scripts/World/GameOverHandler.cs`, `ResultsHandler.cs`, `ShopHandler.cs`.
- `Assets/Scripts/UI/TimeIndicatorUI.cs`, `DemandIndicatorUI.cs`.
- `Assets/Scripts/Player/Heroes/HeroController.cs` — Death Flow real + guarda `isDead`.
- `Assets/Scripts/World/Vendor.cs` — registra venda no `DemandTracker`.
- `Assets/Scripts/UI/MainMenuUI.cs` — `NewGame()` inicia Timer/Demanda do Dia 1.
- `Assets/Scripts/Enemies/MeleeEnemyPrototype.cs` — `monsterEssenceDropAmount` configurável.

## Eventos adicionados

- `GameEvents.OnTimeChanged(float)` / `TimeChanged(float)`.
- `GameEvents.OnDemandChanged(int, int)` / `DemandChanged(int, int)`.
- `GameEvents.OnGameStateChanged(GameState)` / `GameStateChanged(GameState)`.

## Testes executados

- **Automatizado (EditMode):** `DemandCalculatorTests` (3 testes: Dia 1/2/5) — passou, junto com os 27 testes totais da suíte (confirmado pelo usuário no Test Runner).
- **Manual (Play Mode), caso sucesso:** vendeu 40x Monster Essence (usando `monsterEssenceDropAmount` alto pra acelerar o teste) → `[DayResolver] Demanda cumprida` → Results → Shop → `Start Next Day` → HUD volta a `Time: 100s`, `Demand: 0/80` (Dia 2). Log de ponta a ponta confirmado.
- **Manual, caso morte causando Game Over:** herói morreu com o timer em ~20s, a penalidade de -30s zerou o dia, `DayResolver` corretamente decidiu Game Over (demanda não cumprida) → `GameOverHandler` voltou pro Menu. Um segundo ataque do inimigo, que chegou logo depois, foi corretamente ignorado pela guarda `isDead` (só um `[HeroController] Morreu.` no log, confirmando o fix de reentrância). `Respawn()` completou normalmente depois — comportamento esperado (ver "Dívida técnica" abaixo), não bug.
- **Manual, caso demanda não cumprida por não vender:** coletou 40x Monster Essence mas não vendeu nada, deixou o tempo zerar → Game Over corretamente (demanda conta venda, não coleta — GDD Seção 39) → a Essência ainda na Bag foi destruída (GDD Seção 37, reset Daily).
- **Verificação de save:** confirmado por timestamp e conteúdo do arquivo (`%userprofile%\AppData\LocalLow\NanoStudio\MonsterLLC\save.json`) que nenhum save novo foi escrito durante os dois testes de Game Over — o arquivo continua sendo o save antigo da Sprint 9 (`Dia 5, Gold 1200`), confirmando a GDD Seção 43.

## Bugs conhecidos

Nenhum em aberto.

## Dívida técnica

- **Nada ainda pausa/congela o mundo quando o estado sai de `GameState.Gameplay`.** Não existe tela real de Menu/Results/Shop/Game Over cobrindo a cena — inimigos continuam atacando, o player continua recebendo input, mesmo em Results/Shop/Game Over. Isso ficou visível no teste de morte: o `Respawn()` (deliberadamente incondicional, ver "Decisões técnicas") aconteceu depois do Game Over já ter voltado pro estado de Menu, o que pareceu ambíguo no teste mas não corrompeu nenhum estado. Sem sprint explícita atribuída no plano de produção para esse gating — o mais próximo é a Sprint 12 ("Ciclo de Dia Completo"), mas o plano não cita esse ponto especificamente; registrar aqui pra não perder o fio quando a Loja/Menu ganharem tela real.
- Herdada (Sprint 9/10): prioridade entre interagíveis simultâneos (Sprint 35); `ContinueGame()` ainda pula direto pra `Gameplay` sem passar pela Loja nem chamar `DayTimer.ResetForNewDay()`/`DemandTracker.StartDay()` — escopo formal da **Sprint 12** ("Continue Game lê o save e abre a Loja do checkpoint", conforme o plano de produção). Até lá, um `Continue Game` que jogue um dia até o fim bate demanda `0/0` automaticamente (`DemandTracker.Target` nunca inicializado nesse caminho) — bug latente, não deste sprint, mas vale ter em mente ao testar Continue Game antes da Sprint 12.
- `SaveManager.Save()` ainda não é chamado em lugar nenhum do ciclo — checkpoint real na Loja é a Sprint 12 inteira (já documentado no texto original desta sprint).

## Próximos passos

Com Demanda/Results/Shop/Game Over reais, a Sprint 12 (Save Real + Continue Game + Ciclo de Dia Completo) fecha o Vertical Slice: Save automático ao entrar na Loja, Continue Game abrindo a Loja do checkpoint em vez de pular pra Gameplay, e idealmente também o gating de gameplay fora do estado `Gameplay` (dívida registrada acima).
