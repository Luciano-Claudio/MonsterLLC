# Projeto Torre

*(nome provisório)*

> Você entra fraco, com uma bag minúscula e uma arma qualquer. A torre está infestada — e o reino quer sua cota diária de Essência. Cada subida é uma aposta: você consegue matar mais rápido do que consegue carregar?

Roguelite de ação, exploração e economia. O jogador sobe uma torre de 10 andares fixos, cumprindo demandas diárias crescentes de Monster Essence ao longo de 15 dias por run, com pós-game opcional até o Dia 30. Combate ativo (mira e ataque pelo mouse), sem XP e sem level-up tradicional — o poder vem de progressão de arma persistente na run, economia, logística e automação via employees.

**Estado atual:** Em desenvolvimento — Sprint 16 concluída (**Deadline 4 fechada**)

---

## Design

- **[Game Design Document (GDD)](gdd/)** — fonte de verdade de todas as regras, sistemas e decisões de design.

## Guias

- **[Project Setup Guide](guides/project-setup.md)** — do clone ao primeiro build local: Unity, Git LFS, abertura do projeto, `_TestScene`.
- **[Git Workflow Guide](guides/git-workflow.md)** — estratégia de branch, convenção de commits e processo de fechamento de sprint.
- **[Editando arquivos da Unity fora do Editor](guides/unity-file-editing.md)** — quando é seguro escrever/editar arquivos serializados da Unity direto em texto vs. quando fazer pela UI do Editor.

## Produção

- **[Plano de Produção (v7)](Projeto_Torre_Plano_Producao_v7.md)** — análise de produção, ordem macro de sistemas e as 14 deadlines/56 sprints planejadas.

### Task Breakdowns por Sprint

<!-- Adicionar aqui o link de cada nova Sprint conforme for detalhada, mantendo a mais recente no topo. -->

- [Sprint 32 — Agregação Visual + Large Number](sprint-32-task-breakdown.md) — física real no loot (Rigidbody2D sem Freeze Rotation Z, hop ao morrer, atrito), stacking por trigger de proximidade entre itens do mesmo material, 4 sprites por limiar de quantidade (1/2+/5+/20+, limiares serializados — regra nova que substitui o esquema antigo do GDD Seção 38, 1-9/10-49/50-99/100+)
- [Sprint 31 — 15 LootDefinitions](sprint-31-task-breakdown.md) — abre a **Deadline 8** (Sprints 31-43, Pré-Deadline Mundo Único P1-P11 já fechada) — drop table por monstro com rolagem independente por material (GDD Seção 38), os 15 materiais + valores de venda já fechados no GDD; achado real: `docs/gdd/bestiary.md` já tem drop table completo (chance+quantidade) de 8 dos 15 materiais pra todo comum Tier1-9, e `Vendor.cs` hoje só vende "Monster Essence" (hardcoded) — decisão em aberto se generaliza nesta sprint ou vira dívida explícita
- [Sprint 16 (Correção) — Enemy Framework Pivot + Patrol AI](sprint-16-correcao-task-breakdown.md) — task breakdown da correção; **ver o [relatório de fechamento](sprints/sprint-16.md) pro estado final real**, que reabriu e revisou boa parte deste breakdown
- [Sprint 15 — Floor Sleep/Activation v1](sprint-15.md)
- [Sprint 14 — Attack Budget + Population Skeleton](sprint-14.md)
- [Sprint 13 — Enemy Framework Genérico](sprint-13.md)
- [Sprint 12 — Save Real + Continue Game + Ciclo de Dia Completo](sprint-12.md)
- [Sprint 11 — Demanda + Results + Shop Skeleton + Game Over Funcional](sprint-11.md)
- [Sprint 10 — Inventory + Vendor + Gold](sprint-10.md)
- [Sprint 09 — Main Menu + Run Creation Flow + Loot Básico](sprint-09.md)
- [Sprint 08 — MeleeEnemyPrototype + Death Flow (fase 1)](sprint-08.md)
- [Sprint 07 — Hero Framework + Barbarian](sprint-07.md)
- [Sprint 06 — Stair Routing + Active Floor Position](sprint-06.md)
- [Sprint 05 — Floor System Skeleton](sprint-05.md)
- [Sprint 04 — Localization + Save/RunState + Large Number + Docs Pipeline Maduro](sprint-04.md)
- [Sprint 03 — GameEvents + Time/Pause + Game State + Progress Tracker Skeleton + Testes](sprint-03.md)
- [Sprint 02 — Input System + Organização de Projeto](sprint-02.md)
- [Sprint 01 — Task Breakdown](Sprint_01_Task_Breakdown.md)

## Acompanhamento

- **[Sprint Reports](sprints/)** — relatório de cada sprint concluída (sistemas adicionados, decisões técnicas, dívida técnica, etc.) e o [template reutilizável](sprints/_template.md).
- **[Adiantamentos](sprints/adiantamentos.md)** — trabalho feito fora do escopo da sprint corrente, adiantado de propósito e documentado em detalhe pra não se perder até a sprint/batch de destino.
- **[Changelog](changelog.md)** — histórico de mudanças por sprint.
