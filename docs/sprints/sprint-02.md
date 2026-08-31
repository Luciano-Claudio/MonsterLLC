# Sprint 02 — Input System + Organização de Projeto

## Objetivo

Ter um Player placeholder respondendo a todos os inputs do GDD (WASD, Mouse, LMB, RMB, E, TAB, Q) e a Hierarchy organizada com seções claras. Sem gameplay real — só a fiação de input.

## Sistemas adicionados

- **Input System** configurado com um Action Map `Gameplay` cobrindo os 7 inputs do MVP (Move, Look, Attack, Ultimate, Interact, Inventory, RemoteControl).
- **Player placeholder** na cena: um Sprite quadrado com `PlayerInputTest`, Tag e Layer `Player`, organizado dentro de `//ENTITIES`.
- **Convenção de organização de Hierarchy**: objetos prefixados com `//` (ex.: `//SYSTEMS`, `//ENTITIES`) ganham destaque visual (fundo preto, texto em caixa alta) via `HierarchySectionHeader.cs`.

## Decisões técnicas

- **`PlayerControls.inputactions` foi escrito à mão como JSON**, em vez de criado pela UI do Input Actions Editor — o schema desse formato é estável o suficiente para reproduzir com segurança, poupando os cliques manuais de configurar 7 actions e seus bindings um por um.
- **"Generate C# Class" habilitado direto no `.meta`** do `.inputactions`, usando o GUID real do importer do Input System 1.18.0 (copiado do `InputSystem_Actions.inputactions.meta` padrão do projeto) — evitou o passo manual de marcar a checkbox na UI, mas ainda assim precisou de confirmação na Unity para o wrapper (`PlayerControls.cs`) ser efetivamente gerado.
- **Layers e Tags (`Player`, `Enemy`, `Floor`, `Interactable`) editados direto em `ProjectSettings/TagManager.asset`**, em vez de via `Edit > Project Settings`. Formato do arquivo é sensível a detalhes (contagem exata de 32 slots de layer, e um espaço à direita obrigatório em cada entrada vazia — `"- "`, não `"-"`) — um erro nesse formato quebrou o parser da Unity na primeira tentativa e foi corrigido.
- **Input System e o Editor já vinham pré-instalados** no bootstrap do projeto (Sprint 1 / template URP 2D) — não foi necessário passar pelo Package Manager.

## Arquivos/classes principais

- `Assets/PlayerControls.inputactions` + `.meta` — definição dos inputs.
- `Assets/PlayerControls.cs` — wrapper C# gerado pela Unity a partir do `.inputactions`.
- `Assets/Scripts/PlayerInputTest.cs` — consome o `PlayerControls`, move o Player e loga cada input no Console (sem lógica de gameplay real).
- `Assets/Editor/HierarchySectionHeader.cs` — utilitário de Editor (MIT, Bil Simser) para destacar headers `//` na Hierarchy.
- `ProjectSettings/TagManager.asset` — Layers e Tags novos.

## Eventos adicionados

Nenhum (`GameEvents` só nasce na Sprint 3).

## Testes executados

Nenhum teste automatizado. Validação manual em Play Mode: WASD move o Player; LMB/RMB/E/TAB/Q logam corretamente no Console; Hierarchy mostra `//SYSTEMS` e `//ENTITIES`, com o Player dentro de `//ENTITIES`.

## Bugs conhecidos

Nenhum em aberto.

## Dívida técnica

- O fechamento formal desta sprint (`docs/sprints/sprint-02.md` — este arquivo) só foi escrito depois da Sprint 3, não durante a própria Sprint 2.

## Próximos passos

Com os 7 inputs mapeados e a Hierarchy organizada em `//SYSTEMS`/`//ENTITIES`, a Sprint 3 pôde plugar sistemas de verdade (eventos, pausa, progresso) nesses inputs sem precisar reconfigurar nada do Input System.
