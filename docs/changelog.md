# Changelog

Histórico de mudanças por sprint. Para o detalhe completo de cada uma (decisões técnicas, dívida técnica, etc.), veja os [Sprint Reports](sprints/).

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
