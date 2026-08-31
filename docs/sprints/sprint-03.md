# Sprint 03 — GameEvents + Time/Pause + Game State + Progress Tracker Skeleton + Testes

## Objetivo

Ter o núcleo reativo do jogo funcionando: eventos centralizados, uma pausa que realmente interrompe o tempo, e um contador de progresso que reage a eventos — mais um teste automatizado provando que a engrenagem gira.

## Sistemas adicionados

- **`GameEvents`** — catálogo estático central de eventos do jogo (`OnEnemyKilled` por enquanto). Regra do projeto: nenhum outro script declara `event` solto, tudo passa por aqui.
- **`TimeManager`** — singleton com o estado `IsPaused`, consultado (não imposto) por quem precisa respeitar a pausa.
- **`GameStateManager`** — singleton com o estado de alto nível da partida (`Gameplay`/`Paused`); só existe como esqueleto, ainda não é consultado por ninguém.
- **`ProgressTracker`** — contador estático `string → int`, incrementado via inscrição em `GameEvents.OnEnemyKilled`.
- **`SystemsBootstrap`** — liga o Input System (Sprint 2) a esses sistemas: TAB/Q chamam `TimeManager.TogglePause()`, E chama `GameEvents.EnemyKilled()` (placeholder até existir um inimigo real).
- **Test Framework** — primeira suíte de testes automatizados do projeto (EditMode).

## Decisões técnicas

- **`Core.asmdef` criado para `Assets/Scripts/Core/`, fora do plano original.** A task pedia só adicionar `Assembly-CSharp` nas referências do assembly de teste — isso não funcionou nesta versão do Unity (6000.0.35f1): o picker de "Assembly Definition References" no Inspector não lista `Assembly-CSharp` como opção válida (ele não tem `.asmdef` próprio), e a referência por nome escrita à mão no JSON não resolvia (aparecia como "None" no Inspector, e o teste dava `CS0103` mesmo depois de reimportar). A correção foi dar um assembly próprio para `Core/` e referenciá-lo por GUID a partir do teste — mecanismo que de fato funciona e aparece resolvido no Inspector.
- **`SystemsBootstrap.cs` mora em `Assets/Scripts/`, não em `Assets/Scripts/Core/`**, como consequência direta da decisão acima: ele depende de `PlayerControls` (assembly padrão), e colocá-lo dentro de `Core.asmdef` exigiria `Core` referenciar o assembly padrão de volta — mesmo problema de novo. `Core/` ficou só com lógica pura (eventos, tempo, estado, progresso), sem nenhuma dependência do Input System.
- **`EditMode.asmdef` escrito à mão inicialmente, depois corrigido com ajuda da UI.** As referências a `UnityEngine.TestRunner`/`UnityEditor.TestRunner` por nome também não resolviam sozinhas; precisaram ser re-adicionadas via Inspector (Unity as reescreveu como referências por GUID). Um efeito colateral desse ajuste manual foi `includePlatforms` ganhar `WindowsStandalone32`/`64` além de `Editor` — o que fez o Test Runner parar de listar o teste (`testcasecount="0"`). Corrigido restringindo de volta a só `Editor`, que é o esperado para um assembly EditMode-only.

## Arquivos/classes principais

- `Assets/Scripts/Core/GameEvents.cs`, `TimeManager.cs`, `GameStateManager.cs`, `ProgressTracker.cs`, `TestTimer.cs` — núcleo reativo.
- `Assets/Scripts/Core/Core.asmdef` — assembly próprio do núcleo, referenciável por outros assemblies (como o de testes).
- `Assets/Scripts/SystemsBootstrap.cs` — liga Input System ao núcleo.
- `Assets/Tests/EditMode/EditMode.asmdef` — assembly de testes EditMode, referenciando `Core` por GUID.
- `Assets/Tests/EditMode/ProgressTrackerTests.cs` — teste `EnemyKilled_IncrementsCounter`.

## Eventos adicionados

- `GameEvents.OnEnemyKilled` (`static event Action`), disparado via `GameEvents.EnemyKilled()`.

## Testes executados

- **Automatizado (EditMode):** `ProgressTrackerTests.EnemyKilled_IncrementsCounter` — reseta o tracker, dispara `GameEvents.EnemyKilled()`, verifica que o contador foi para 1. **Passou.**
- **Manual (Play Mode):** apertar E várias vezes incrementa `[ProgressTracker] EnemyKilled = 1, 2, 3...` no Console; TAB/Q pausam e despausam o `TestTimer` (validado via campo `Time Remaining` no Inspector, já que o log de debug foi comentado durante os testes).

## Bugs conhecidos

Nenhum em aberto — os dois problemas encontrados durante a sprint (resolução de `Assembly-CSharp` e `includePlatforms` incorreto) foram corrigidos e estão descritos em "Decisões técnicas".

## Dívida técnica

- Um commit extra (`chore: remove accidentally committed test-result artifacts...`) foi necessário para remover `TestResults_*.xml` e um `.asmdef` redundante que entraram sem querer num `git add` amplo — `.gitignore` atualizado (`TestResults*.xml`) para não repetir.
- `GameStateManager` existe mas não é lido por nenhum sistema ainda — puramente esqueleto.
- `SystemsBootstrap` usa `Interact (E)` como placeholder de "matar inimigo"; precisa ser substituído por um evento real assim que existir combate.

## Próximos passos

Com `GameEvents`, pausa e um contador de progresso funcionando (e testados), as próximas sprints de gameplay (combate, monstros) já têm onde plugar seus eventos sem inventar um sistema de comunicação novo.
