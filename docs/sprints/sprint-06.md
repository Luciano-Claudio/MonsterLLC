# Sprint 06 — Stair Routing + Active Floor Position

## Objetivo

Subir/descer leva pro Floor certo por posição relativa (Active Floor Position), não por identidade fixa — pré-requisito documentado para o Remove Tower Layer (Deadline 12) funcionar sem reescrever nada. Indicador de Floor atual visível na tela.

## Sistemas adicionados

- **`StairRouting`** — lógica pura (sem `MonoBehaviour`) de roteamento por posição: `GetNextPosition`/`GetPreviousPosition` nunca olham para `originalFloorIdentity`, só para `activeFloorPosition`.
- **`FloorRegistry`** — conecta `StairRouting` aos `FloorDefinition` reais da Scene.
- **Sistema de interação genérico** (`Interactable` + `InteractionManager`) — mudança de design no meio da sprint (ver "Decisões técnicas"): escadas deixaram de ser gatilho automático e passaram a exigir a tecla **E**, com um ícone de prompt aparecendo quando o Player está no alcance. Construído como padrão reutilizável, já que baús vão usar a mesma base no futuro.
- **`Stair`** reescrito sobre `Interactable` — a lógica de roteamento em si não mudou, só quando ela dispara.
- **Indicador de Floor na tela** (`FloorIndicatorUI`, TextMeshPro) e **prompt de interação** (`InteractPromptUI`) — primeira UI do projeto.

## Decisões técnicas

- **Mudança de design a meio da sprint: escadas exigem E, não são mais automáticas.** O plano original desta sprint (e o GDD Seção 26, marcada ✅/congelada) descrevia escadas como teleportadores passivos, com E reservado só para baús (Seção 30). O usuário decidiu, durante a implementação, que escadas devem seguir o mesmo padrão de interação contextual dos baús (ícone de prompt + E). Como isso contradizia o texto congelado do GDD, a mudança foi tratada como decisão de design deliberada, não um ajuste de implementação — **o GDD foi atualizado** (Seção 26 ganhou a subseção "Interação — travessia com E, não automática"; Seção 46 passou a listar escadas também na linha de E) para o documento continuar sendo a fonte de verdade real do projeto.
- **`SystemsBootstrap` tinha `E → GameEvents.EnemyKilled()` como placeholder desde a Sprint 3** ("até existir inimigo real"). Com E ganhando uso real nesta sprint, esse placeholder foi removido — ele colidiria com toda interação (cada escada ativada também dispararia um `EnemyKilled` falso, inflando o `ProgressTracker`).
- **`Interactable` e `InteractionManager` moram em `Assets/Scripts/`, não em `Core/`** — mesmo motivo do `SystemsBootstrap` na Sprint 3: `InteractionManager` precisa do `PlayerControls` (assembly padrão), e `Core.asmdef` não consegue referenciar o assembly padrão de volta (só o contrário). Primeira tentativa colocou `Interactable` em `Core/`, o que quebrou a compilação (`InteractionManager` não visível) — corrigido movendo os dois scripts pra fora de `Core/`. `Stair`, mesmo dependendo de `FloorRegistry`/`FloorManager`/`FloorDefinition` (`Core.asmdef`), também ficou fora, já que herda de `Interactable`.
- **Ícone de prompt exige Canvas em World Space, não Screen Space - Overlay** (o padrão ao criar um Canvas novo). Com Overlay, `transform.position` setado via script não tem efeito na renderização — a Unity desenha em coordenadas de tela, e como a câmera segue o Player, o ícone aparecia sempre no centro da tela (= em cima do Player, não da escada). Corrigido trocando o Render Mode do Canvas para World Space.
- **Prompt de interação escondido via um filho separado (`iconVisual`), não desativando o próprio GameObject do `InteractPromptUI`** — desativar o GameObject que carrega o script quebraria a própria inscrição em `GameEvents.OnInteractPromptChanged` (Awake/OnEnable não rodam de novo em objeto desativado por dentro do próprio Awake).

## Arquivos/classes principais

- `Assets/Scripts/Core/StairRouting.cs`, `FloorRegistry.cs` — roteamento (`Core.asmdef`).
- `Assets/Scripts/Interactable.cs`, `InteractionManager.cs`, `Stair.cs` — interação (assembly padrão).
- `Assets/Scripts/UI/FloorIndicatorUI.cs`, `InteractPromptUI.cs` — UI placeholder.
- `docs/gdd/index.md` — Seção 26 e 46 atualizadas para refletir a nova regra de interação das escadas.

## Eventos adicionados

- `GameEvents.OnFloorChanged(FloorDefinition)` — disparado em `FloorManager.SetCurrentFloor`.
- `GameEvents.OnInteractPromptChanged(Transform anchor)` — `null` esconde o prompt.

## Testes executados

- **Automatizado (EditMode):** `StairRoutingTests` — 5 testes, incluindo `Routing_WorksEvenWhenPositionsAreReassigned`, que simula o mesmo tipo de troca que o Remove Tower Layer vai fazer. Todos passaram, junto com os 5 já existentes.
- **Manual (Play Mode):** navegação completa nos dois sentidos entre Ground/Floor 1/Floor 2 via E; indicador de Floor atualizando; teste de reordenação (`FloorReorderTest`) confirmando que a rota segue a posição ativa, não a identidade do objeto nem sua localização física — inclusive teleportando para o Floor fisicamente "errado" corretamente quando as posições são trocadas.

## Bugs conhecidos

Nenhum em aberto — os três problemas encontrados (erro de compilação por posicionamento de assembly, `NullReferenceException` por `InteractionManager` ausente na cena, ícone preso no Player por Canvas em Overlay) foram todos diagnosticados e corrigidos durante a própria sprint.

## Dívida técnica

- `Floor_2` ganhou uma escada de subida extra (além da de descida pedida) — sem destino registrado no `FloorRegistry`, falha graciosamente logando "sem destino". Inofensivo, não bloqueante; decisão de remover ou manter fica em aberto, sem prazo — não é dívida estrutural.

## Próximos passos

Com roteamento por posição validado (inclusive sob reordenação) e o padrão de interação por E pronto e reutilizável, a Sprint 7 (Hero Framework + Barbarian) não depende de nada pendente desta sprint. O sistema de `Interactable` já está pronto para os baús (GDD Seção 30) quando a Sprint correspondente chegar.
