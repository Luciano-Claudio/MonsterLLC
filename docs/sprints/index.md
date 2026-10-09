# Sprint Reports

Relatório de cada sprint concluída: o que foi entregue, decisões técnicas tomadas, dívida técnica deixada para trás e próximos passos. Veja o [template](_template.md) usado para escrevê-los.

- [Sprint 30 — Blood Mage Completo + Checklist dos 10 Heróis](sprint-30.md) — **10 heróis MVP Gameplay Complete, fecha a Deadline 7** — leque de projéteis com reserva (1 hit por padrão, upgradable pra perfuração), Ultimate em anel circular de 3 estágios escalando com `transform.localScale`, Shift aplica status "Drain" (novo) multi-alvo e cura exatamente o dano causado via orb, pet Elemental de Sangue generaliza o `PetController` da Phoenix pra Idle/Move separados
- [Sprint 29 — Assassin Completo (Primário + Ultimate + Shift + Passiva)](sprint-29.md) — Deadly Dash/Thousand Blades redesenhados em sprint pra "teleporte de ida-e-volta" com projétil próprio, forma sombria troca o Animator Controller inteiro, fix de monstros de emboscada re-dormindo durante stealth, trava de segurança contra a Ultimate acabar no meio de outra ação
- [Sprint 28 — Gunslinger Completo (Primário + Ultimate + Shift + Passiva)](sprint-28.md) — Primeiro hitscan do projeto (raycast + ContactFilter2D, filtrado por tag "Enemy"), rajada de 1-15 tiros com desvio aleatório calibrado por Gizmo, Ultimate com N tiros instantâneos por direção fixa, loot em dobro
- [Sprint 27b — Infraestrutura de Upgrades (todos os heróis) + correções de base no Mage](sprint-27b-upgrade-infrastructure.md) — Valores de upgrade centralizados no controlador em todos os 7 heróis, `ActionSpeedMultiplier`, multiplicadores de tamanho, fix da explosão do Mage e do pet deslizando no Summon
- [Sprint 27 — Paladin Completo (Primário + Ultimate + Shift + Passiva)](sprint-27.md) — Martelo com leque de upgrade (1–5), espadas orbitando escaláveis (2/4/8), shield bash, shield periódico com vida própria
- [Sprint 26 — Bosses Floor 2 (Exceções): Rat People Royalty, Spider Queen](sprint-26-floor2-excecoes.md) — Melee/Ranged híbrido com 2 raios de ataque independentes, `CustomAnimationGeneratorWindow` — **Floor 1–2 100% fechado (7/7 bosses)**
- [Sprint 25 — Bosses Floor 2 (Padrão): Werewolf, Centaur King](sprint-25-floor2-padrao.md) — `MeleeEnemyController` padrão, sem script novo
- [Sprint 24 — Boss Framework + Boss Timer + Bosses Floor 1](sprint-24-boss-framework-floor1.md) — `BossSpawnManager`, `FloorSpawnUtility`, Goblin King, Mother Slime Green/Blue — **inicia os bosses da Deadline 6**
- [Sprint 19b — Sistema de Efeitos Nocivos (Fire + Bleeding) + correções de base em Mira/Shift](sprint-19b-efeitos-nocivos.md) — Efeitos Nocivos genérico, `DiagonalAimX/Y`, bugs de mira corrigidos em todo herói
- [Sprint 19 — Mage Completo (Primário + Ultimate + Secundária/Shift + Pet Phoenix)](sprint-19.md) — Mage 100% jogável, além do escopo original
- [Sprint 18b — Habilidade Secundária (Shift) — Barbarian + Ranger](sprint-18b-habilidade-secundaria.md) — Shift genérico pra todo herói, Barbarian e Ranger completos
- [Sprint 18 — Ranger (Ultimate)](sprint-18.md) — completa o Ranger
- [Sprint 17 — Ranger (Primário) → Detalhamento dos 10 Heróis + Barbarian Completo](sprint-17.md) — **inicia a Deadline 5**
- [Sprint 16 — Teste de Viabilidade de Combate Real + Correção + Pivô Híbrido Final](sprint-16.md) — **fecha a Deadline 4**
- [Sprint 15 — Floor Sleep/Activation v1](sprint-15.md)
- [Sprint 14 — Attack Budget + Population Skeleton](sprint-14.md)
- [Sprint 13 — Enemy Framework Genérico](sprint-13.md) — **inicia a Deadline 4**
- [Sprint 12 — Save Real + Continue Game + Ciclo de Dia Completo](sprint-12.md) — **fecha a Deadline 3**
- [Sprint 11 — Demanda + Results + Shop Skeleton + Game Over Funcional](sprint-11.md)
- [Sprint 10 — Inventory + Vendor + Gold](sprint-10.md)
- [Sprint 09 — Main Menu + Run Creation Flow + Loot Básico](sprint-09.md) — **inicia a Deadline 3**
- [Sprint 08 — MeleeEnemyPrototype + Death Flow (fase 1)](sprint-08.md) — **fecha a Deadline 2**
- [Sprint 07 — Hero Framework + Barbarian](sprint-07.md)
- [Sprint 06 — Stair Routing + Active Floor Position](sprint-06.md)
- [Sprint 05 — Floor System Skeleton](sprint-05.md)
- [Sprint 04 — Localization + Save/RunState + Large Number + Docs Pipeline Maduro](sprint-04.md)
- [Sprint 03 — GameEvents + Time/Pause + Game State + Progress Tracker Skeleton + Testes](sprint-03.md)
- [Sprint 02 — Input System + Organização de Projeto](sprint-02.md)
- [Sprint 01 — Fundação Técnica](sprint-01.md)
