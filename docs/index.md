# Projeto Torre

*(nome provisório)*

> Você entra fraco, com uma bag minúscula e uma arma qualquer. A torre está infestada — e o reino quer sua cota diária de Essência. Cada subida é uma aposta: você consegue matar mais rápido do que consegue carregar?

Roguelite de ação, exploração e economia. O jogador sobe uma torre de 10 andares fixos, cumprindo demandas diárias crescentes de Monster Essence ao longo de 15 dias por run, com pós-game opcional até o Dia 30. Combate ativo (mira e ataque pelo mouse), sem XP e sem level-up tradicional — o poder vem de progressão de arma persistente na run, economia, logística e automação via employees.

**Estado atual:** Em desenvolvimento — Sprint 4 (Deadline 1 encerrada)

---

## Design

- **[Game Design Document (GDD)](gdd/)** — fonte de verdade de todas as regras, sistemas e decisões de design.

## Guias

- **[Project Setup Guide](guides/project-setup.md)** — do clone ao primeiro build local: Unity, Git LFS, abertura do projeto, `_TestScene`.
- **[Git Workflow Guide](guides/git-workflow.md)** — estratégia de branch, convenção de commits e processo de fechamento de sprint.
- **[Editando arquivos da Unity fora do Editor](guides/unity-file-editing.md)** — quando é seguro escrever/editar arquivos serializados da Unity direto em texto vs. quando fazer pela UI do Editor.

## Produção

- **[Plano de Produção (v6)](Projeto_Torre_Plano_Producao_v6.md)** — análise de produção, ordem macro de sistemas e as 14 deadlines/56 sprints planejadas.

### Task Breakdowns por Sprint

<!-- Adicionar aqui o link de cada nova Sprint conforme for detalhada, mantendo a mais recente no topo. -->

- [Sprint 04 — Localization + Save/RunState + Large Number + Docs Pipeline Maduro](sprint-04.md)
- [Sprint 03 — GameEvents + Time/Pause + Game State + Progress Tracker Skeleton + Testes](sprint-03.md)
- [Sprint 02 — Input System + Organização de Projeto](sprint-02.md)
- [Sprint 01 — Task Breakdown](Sprint_01_Task_Breakdown.md)

## Acompanhamento

- **[Sprint Reports](sprints/)** — relatório de cada sprint concluída (sistemas adicionados, decisões técnicas, dívida técnica, etc.) e o [template reutilizável](sprints/_template.md).
- **[Changelog](changelog.md)** — histórico de mudanças por sprint.
