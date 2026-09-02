# Sprint 07 — Hero Framework + Barbarian

## Objetivo

Primeiro herói jogável de verdade (move, ataca em área, usa ultimate) e a regra de Energia da Ultimate (GDD Seção 11) implementada e testada — sem inimigo real ainda (isso é Sprint 8), usando um simulador de kill para validar o fluxo de Energia.

## Sistemas adicionados

- **`EnergySystem`** — lógica pura de energia (`AddEnergy` satura no máximo, `IsReady` checa se está cheia), coberta por 4 testes automatizados antes de virar comportamento na Scene.
- **`HeroController`** (base abstrata) + **`HeroStats`** — framework reutilizável para heróis futuros: movimento, ataque/ultimate ligados ao Input System, ganho de Energia via `GameEvents.OnEnemyKilled`, e a regra dos "3 zera" do GDD (usar ultimate, morrer — hook pronto para a Sprint 8 —, e futuramente fim de dia).
- **Mira do mouse resolvida em 8 direções** (`HeroController.AimDirection`) — não estava no plano original desta sprint, adicionada depois de detectar que faltava (ver "Decisões técnicas").
- **`Barbarian`** — primeiro herói concreto: ataque em área deslocada na direção da mira, ultimate em rajada omnidirecional (`Debug.DrawRay`, sem dano real ainda).
- **`EnemyKillSimulator`** e **indicador de Energia na tela** (`EnergyIndicatorUI`) — validação manual sem depender do `MeleeEnemyPrototype` (Sprint 8).
- **Reorganização completa de `Assets/Scripts/`** em pastas por categoria (ver "Decisões técnicas") — não fazia parte do escopo original, pedida pelo usuário no meio da sprint.

## Decisões técnicas

- **`GameEvents.OnEnemyKilled` mudou de assinatura de propósito** (`Action` → `Action<int>`, carregando o valor de energia do kill) — GDD Seção 11 diz que monstros mais fortes concedem mais Energia por kill; melhor a assinatura nascer certa agora do que o `MeleeEnemyPrototype` da Sprint 8 forçar quebrar de novo. Único assinante existente (`ProgressTracker`, Sprint 3) ajustado; `ProgressTrackerTests` também precisou de ajuste de chamada.
- **Mira do mouse adicionada nesta sprint, fora do escopo original.** O código proposto tinha `Barbarian.PrimaryAttack` como um círculo centrado no próprio personagem — mas o GDD Seção 17.1 descreve o golpe do Barbarian como "área circular **à frente** do personagem", parte da identidade do herói ("posicionamento importa mais que dano puro"). A causa raiz era estrutural: `HeroController` não ligava a action `Look` (mira do mouse) em lugar nenhum, e mira por mouse é regra central do GDD (Seção 11), não um detalhe só do Barbarian — outros heróis futuros vão precisar dela também. Resolvido implementando `AimDirection` no `HeroController` (ângulo até o mouse, arredondado para o múltiplo de 45° mais próximo — as 8 direções exatas que o GDD pede) em vez de aceitar a divergência como dívida técnica.
- **Pendência de design registrada no GDD, não resolvida:** com escadas usando E desde a Sprint 6, e o Magnet (GDD Seção 29) já tendo uma regra de prioridade não escrita para escadas ("interações prioritárias próximas, ex.: baú, têm prioridade sobre largar o Magnet"), não existe hoje nenhuma regra de prioridade entre interagíveis simultâneos — nem no GDD, nem no código (`InteractionManager` reage ao último trigger que disparou, sem ordem definida). Registrado no GDD (Seção 26) como pendência 🟡 com prazo exato: **antes da Sprint 35** (Deadline 9 — primeira sprint em que baús existem de verdade). Não é um problema prático agora, já que escada é o único interagível ativo no jogo.
- **Reorganização de `Assets/Scripts/` em pastas por categoria**, incluindo subdividir `Core/` (que antes só tinha um nível): `Core/Floor/`, `Core/Save/`, `Core/GameState/`; novas pastas fora de `Core/` — `Interaction/`, `World/`, `Player/Heroes/`, `Tests/` (essa última reunindo todos os scripts de demonstração via `ContextMenu`, inclusive `TestTimer`, que só existia para provar que a pausa funciona). `Interaction/`, `World/` e `Player/Heroes/` ficaram deliberadamente fora de `Core/` — mesmo motivo já documentado desde a Sprint 3/6 (dependem, direta ou indiretamente, de `PlayerControls`, que `Core.asmdef` não consegue referenciar de volta). Confirmado que nenhum script do projeto usa `namespace`, então a reorganização é puramente cosmética — Unity resolve componentes por GUID do `.meta`, não por caminho, então nada quebrou.
- **`HeroController` colocado direto em `Assets/Scripts/Player/Heroes/`, não em `Core/`** — terceira vez que esse padrão se repete (depois de `SystemsBootstrap` na Sprint 3 e `Interactable`/`InteractionManager` na Sprint 6), desta vez identificado e evitado antes de gerar erro de compilação.

## Arquivos/classes principais

- `Assets/Scripts/Core/EnergySystem.cs` — lógica pura de energia.
- `Assets/Scripts/Player/Heroes/HeroController.cs`, `HeroStats.cs`, `Barbarian.cs` — framework de herói + primeiro herói concreto.
- `Assets/Scripts/Tests/EnemyKillSimulator.cs`, `Assets/Scripts/UI/EnergyIndicatorUI.cs` — validação manual.
- `docs/gdd/index.md` — Seção 26 ganhou a pendência de prioridade entre interagíveis.

## Eventos adicionados

- `GameEvents.OnEnemyKilled` — assinatura alterada para `Action<int>` (quebra proposital).
- `GameEvents.OnEnergyChanged(float current, float max)` — novo.

## Testes executados

- **Automatizado (EditMode):** `EnergySystemTests` — 4 testes (`AddEnergy_CapsAtMax`, `AddEnergy_BelowMax_AddsNormally`, `IsReady_AtMax_ReturnsTrue`, `IsReady_BelowMax_ReturnsFalse`). Todos passaram, junto com os já existentes (`ProgressTrackerTests` ajustado à nova assinatura).
- **Manual (Play Mode):** Barbarian anda e mira corretamente pelas 8 direções; ataque loga sem acertar nada (sem `Enemy` ainda, esperado); Energia sobe só via `EnemyKillSimulator`, satura em 100, zera ao usar a ultimate; indicador de Energia reflete tudo em tempo real; nada avança durante a pausa (TAB/Q).

## Bugs conhecidos

Nenhum em aberto.

## Dívida técnica

- **Prioridade entre interagíveis simultâneos** (escada vs. baú vs. Magnet) — pendência de design registrada no GDD Seção 26, prazo: antes da **Sprint 35**.
- `HeroController.OnDeath()` existe como hook mas ainda não é chamado por ninguém — vira real na Sprint 8 (Death Flow fase 1).
- Zerar a Energia ao fim do dia (terceira situação da regra do GDD Seção 11) ainda não é possível de implementar — não existe ciclo de dia/noite no projeto ainda.

## Próximos passos

Com o Barbarian completo (movimento, ataque direcional, ultimate, Energia) e o framework de herói pronto para os próximos, a Sprint 8 (MeleeEnemyPrototype + Death Flow fase 1) já tem onde plugar dano de verdade — o ataque do Barbarian já procura `Tag == "Enemy"`, e o hook `OnDeath()` já existe esperando ser chamado.
