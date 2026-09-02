# Changelog

Histórico de mudanças por sprint. Para o detalhe completo de cada uma (decisões técnicas, dívida técnica, etc.), veja os [Sprint Reports](sprints/).

## Sprint 09 — Main Menu + Run Creation Flow + Loot Básico

- `GameStateManager` ganha o enum completo de estados (Menu/Select/Gameplay/Results/Shop/GameOver) de uma vez.
- `MainMenuUI` (`New Game`/`Continue Game`) cria um `RunState` real via `RunCreation`, ou carrega um save existente.
- Primeiro loot do jogo: Monster Essence dropa do inimigo e é coletada automaticamente via Pickup Radius (`LootDrop`).
- **Inicia a Deadline 3 (Primeiro Vertical Slice).**

## Sprint 08 — MeleeEnemyPrototype + Death Flow (fase 1)

- `HealthSystem` (lógica pura, testada) + `MeleeEnemyPrototype` (primeiro inimigo real: persegue, ataca, recebe dano, morre).
- Barbarian passa a causar dano real; `HeroController` ganha Death Flow fase 1 completo (GDD Seção 11) — morre, zera Energia, respawna no térreo com HP cheio.
- Integrado o asset **Easy Transitions** — respawn agora usa fade de tela (`Fade.asset`, definido como estilo padrão no GDD Seção 47), teleporte escondido durante a transição.
- **Encerra a Deadline 2 (Floor + Combat Testbed).**

## Sprint 07 — Hero Framework + Barbarian

- `EnergySystem` (lógica pura, testada) + `HeroController`/`HeroStats` (framework reutilizável de herói) + `Barbarian` (primeiro herói jogável: movimento, ataque direcional, ultimate em rajada).
- Mira do mouse resolvida em 8 direções (GDD Seção 11), adicionada durante a sprint para o ataque do Barbarian ficar à frente do personagem, não centrado nele.
- `GameEvents.OnEnemyKilled` ganhou payload de energia (quebra de assinatura proposital) — `EnergyIndicatorUI` e `EnemyKillSimulator` validam o fluxo sem depender de inimigo real ainda.
- **Pendência registrada no GDD:** prioridade entre interagíveis simultâneos (escada vs. baú vs. Magnet) segue em aberto, prazo antes da Sprint 35.
- `Assets/Scripts/` reorganizado inteiro em pastas por categoria (`Core/Floor`, `Core/Save`, `Core/GameState`, `Interaction/`, `World/`, `Player/Heroes/`, `Tests/`).

## Sprint 06 — Stair Routing + Active Floor Position

- `StairRouting` + `FloorRegistry`, roteamento por Active Floor Position (não índice fixo) — validado inclusive sob reordenação de Floors.
- Escadas reescritas como interação real (tecla E + ícone de prompt), via novo padrão genérico `Interactable`/`InteractionManager` — reutilizável para baús no futuro.
- Indicador de Floor atual na tela (primeira UI do projeto, TextMeshPro).
- **Mudança de design registrada no GDD:** escadas agora exigem interação por E (Seção 26/46 atualizadas) — antes eram passivas.

## Sprint 05 — Floor System Skeleton

- Ground + Floor 1 + Floor 2 como regiões físicas da mesma `_TestScene` (sem Scene por andar).
- `FloorDefinition`/`FloorManager`/`FloorTrigger`, detectando o Floor atual do jogador via trigger 2D.
- Inicia a Deadline 2 (Floor + Combat Testbed).

## Sprint 04 — Localization + Save/RunState + Large Number + Docs Pipeline Maduro

- Localization configurado (`en`, `pt-BR`), String Table Collection `UI Text`, troca de locale em runtime.
- `RunState` + `SaveManager`, save/load de um `RunState` via JSON em `Application.persistentDataPath`.
- `LargeNumberFormatter`, notação k/m/b/t sobre `double`, coberto por 3 testes automatizados.
- Pipeline de docs via DocFX avaliado e **descartado** — quebrava a publicação inteira em CI sem Unity instalada; mantido o GitHub Pages atual (já publica automaticamente desde a Sprint 1).
- Encerra a Deadline 1 (Fundação Técnica Completa).

## Sprint 03 — GameEvents + Time/Pause + Game State + Progress Tracker Skeleton + Testes

- Adicionado `GameEvents`, catálogo central de eventos do jogo.
- Adicionado `TimeManager` (pausa) e `GameStateManager` (estado de alto nível, esqueleto).
- Adicionado `ProgressTracker`, contador de progresso reativo a eventos.
- Adicionado `SystemsBootstrap`, ligando Input System aos novos sistemas (TAB/Q pausa, E dispara `EnemyKilled`).
- Primeira suíte de testes automatizados do projeto (EditMode) — `ProgressTrackerTests`.
- `Core.asmdef` criado para isolar a lógica central em seu próprio assembly.

## Sprint 02 — Input System + Organização de Projeto

- Input System configurado com Action Map `Gameplay` (Move, Look, Attack, Ultimate, Interact, Inventory, RemoteControl).
- Player placeholder na cena, respondendo a WASD/Mouse/LMB/RMB/E/TAB/Q.
- Layers e Tags de gameplay (`Player`, `Enemy`, `Floor`, `Interactable`).
- Hierarchy organizada com headers `//SYSTEMS` e `//ENTITIES`.

## Sprint 01 — Fundação Técnica

- Projeto Unity inicializado (6000.0.35f1 LTS, template URP 2D).
- Git configurado: `.gitattributes` + Git LFS, Build Settings, `_TestScene`.
- Documentação pública publicada via GitHub Pages: Home, GDD, guias de setup e de fluxo Git, README.
