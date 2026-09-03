# Sprint 10 — Inventory + Vendor + Gold

## Objetivo

A Bag deixa de ser placeholder e vira lógica real: 5 slots, stack 16, pickup parcial. Loot coletado passa a ficar guardado de verdade, e o primeiro NPC vendedor do jogo troca Monster Essence por Gold. HUD ganha indicadores de Gold e Bag.

## Sistemas adicionados

- **`Bag`/`InventorySlot`** (`Core/Inventory/`) — lógica pura (não `MonoBehaviour`), 5 slots, stack 16, pickup parcial (nunca tudo-ou-nada — a sobra vira uma entidade de loot separada no chão).
- **`BagController`** (`Player/`) — dono da `Bag`, liga TAB a abrir/fechar a bag com pausa/resume via `TimeManager`.
- **`Vendor`** (`World/`) — primeiro NPC vendedor, herda `Interactable` (Sprint 6), vende todo Monster Essence da bag por Gold.
- **`GoldIndicatorUI`/`BagIndicatorUI`** — HUD mostrando Gold (via `LargeNumberFormatter`) e slots ocupados da bag.
- **`LootDrop.Collect()` reescrito** — guarda de verdade na `Bag` em vez de só logar (placeholder desde a Sprint 9).

## Decisões técnicas

- **Bug real corrigido antes de escrever código — `Vendor.Interact()` sem parâmetro:** o texto original tinha `public override void Interact()`, mas `Interactable` (Sprint 6) declara `Interact(Transform interactor)`. Sem o parâmetro isso nem compila; adicionado o parâmetro (não usado no corpo, mas exigido pela assinatura da base).
- **Bug real corrigido — `Gold` em `int` em vez de `long`:** o texto original especificava `OnGoldChanged`/`GoldIndicatorUI` em `int`, mas `RunState.gold` é `long` desde a Sprint 4, deliberadamente, para suportar o Large Number Format (GDD Seção 48). Usar `int` exigiria um cast redutor em algum ponto da cadeia e reintroduziria o teto de ~2.1 bilhões de gold que o projeto já tinha decidido evitar. Corrigido na origem: `GameEvents.OnGoldChanged`/`GoldChanged(long)` e `GoldIndicatorUI.UpdateLabel(long gold)`.
- **Correção de citação de GDD:** o texto original citava a Seção 38 (Drops no Chão) como fonte da regra "resto fica no chão como entidade separada" do pickup parcial. Verificado direto no GDD: essa regra está na **Seção 37** (Inventário/Bag); a Seção 38 só faz referência de volta pra ela.
- **`ContinueGame()` ganhou a mesma inicialização de HUD que `NewGame()`** — o texto original só previa `Bag.Clear()` + `GoldChanged`/`BagChanged` dentro de `NewGame()`, mas `ContinueGame()` também leva pra `Gameplay` e teria o mesmo problema (indicadores em branco até a primeira venda/coleta) com um save carregado. Aplicado o mesmo fix nos dois pontos de entrada.
- **`BagController` foi para `Player/`, não `Core/`** — depende de `PlayerControls` (Input System) diretamente; `Core.asmdef` é referenciado de forma unidirecional pelo assembly default, então qualquer script que precise de `PlayerControls` fica fora de `Core/` (mesma regra aplicada a `HeroController`/`InteractionManager` em sprints anteriores).
- **`iconAnchor` de `Interactable` é opcional, não obrigatório** — cai pro próprio `transform` do objeto quando vazio (`InteractionManager.SetAvailable`). Nenhuma das 5 `Stair` da cena tem esse campo preenchido; o `Vendor` não precisava de um anchor customizado pra funcionar igual às escadas — só não deixei isso claro no checklist de Editor passado ao usuário.

## Arquivos/classes principais

- `Assets/Scripts/Core/Inventory/Bag.cs`, `InventorySlot.cs` — lógica de inventário.
- `Assets/Scripts/Player/BagController.cs` — dono da `Bag`, bind de TAB.
- `Assets/Scripts/World/Vendor.cs` — NPC vendedor.
- `Assets/Scripts/World/LootDrop.cs` — `Collect()` reescrito para pickup parcial real.
- `Assets/Scripts/UI/GoldIndicatorUI.cs`, `BagIndicatorUI.cs` — HUD.
- `Assets/Scripts/UI/MainMenuUI.cs` — `NewGame()`/`ContinueGame()` inicializam a HUD de Gold/Bag.
- `Assets/Scripts/Core/GameEvents.cs` — `OnBagChanged`/`BagChanged(Bag)`, `OnGoldChanged`/`GoldChanged(long)`.

## Eventos adicionados

- `GameEvents.OnBagChanged(Bag)` / `BagChanged(Bag)`.
- `GameEvents.OnGoldChanged(long)` / `GoldChanged(long)`.

## Testes executados

- **Automatizado (EditMode):** `BagTests` (5 testes: fit, overflow, sem espaço, fit parcial, clear) — passou, junto com os 24 testes totais da suíte (confirmado pelo usuário no Test Runner).
- **Manual (Play Mode):** ciclo completo — `NewGame()` via context menu zera Gold/Bag na HUD; Barbarian mata `MeleeEnemyPrototype`; Monster Essence dropa e é coletada (`[LootDrop] Coletado: 1x Monster Essence`); troca de Floor via `Stair` funciona sem interferência; `Vendor` vende a Monster Essence coletada por Gold (`[Vendor] Vendeu 1x Monster Essence por 1 gold. Total: 1`), HUD atualiza. Log de ponta a ponta confirmado pelo usuário.
- **Regressão pega em teste manual:** primeira tentativa de venda deu `NullReferenceException` em `Vendor.Interact()` — não era bug do `Vendor`, e sim `MainMenuUI.CurrentRun` nulo porque o teste começou direto em Play sem passar pelo `[ContextMenu] NewGame()` primeiro (ainda não existe botão de UI real ligado ao fluxo). Combate/loot/escada não dependem de `CurrentRun`, por isso só o `Vendor` expôs o problema. Refeito o teste chamando `NewGame()` antes — funcionou corretamente.

## Bugs conhecidos

Nenhum em aberto.

## Dívida técnica

- `GameEvents.OnLootCollected`/`LootCollected(LootDefinition)` (Sprint 9) ficou sem nenhum assinante nem chamador — `LootDrop.Collect()` agora chama `BagController.Instance.AddItem(...)` em vez de disparar esse evento. Código morto, baixo risco, sem sprint definida para remoção — remover na próxima vez que `GameEvents.cs` for tocado.
- Herdada (Sprint 9): prioridade entre interagíveis simultâneos (Sprint 35); Day Timer/-30s real e destruição de loot no Death Flow (Sprint 11); caminho "Continue Game sem save" ainda não validado de fato neste ambiente (Sprint 12, quando Continue Game fecha de vez).
- `MainMenuUI.NewGame()`/`ContinueGame()` continuam só `[ContextMenu]`, sem botão de UI real — previsto entrar quando a Main Menu de fato ganhar tela própria (fora do escopo da Deadline 3, que é vertical slice de gameplay).

## Próximos passos

Com Bag/Gold/Vendor reais, a Sprint 11 (Demanda + Results + Shop Skeleton + Game Over Funcional) tem onde plugar a validação de demanda diária e a destruição de loot ainda na Bag ao fim do dia — o `Bag` já expõe `Slots`/`Clear()` o suficiente pra isso sem mudança de interface.
