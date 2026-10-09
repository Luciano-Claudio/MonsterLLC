# Sprint 29 — Assassin Completo (Primário + Ultimate + Shift + Passiva)

## Objetivo

Assassin Gameplay Complete: Deadly Dash/Thousand Blades (primário, mesma mecânica nas 2 formas), forma sombria com `RuntimeAnimatorController` próprio (Ultimate), teleporte em 4 diagonais (Shift) e maior velocidade base do elenco (passiva).

## Sistemas adicionados

- **Deadly Dash / Thousand Blades — "teleporte de ida-e-volta"** (redesenho em sprint, a pedido do usuário — ver Decisões técnicas): o Assassin NÃO se move de verdade. No fim do clipe "Start" ele fica invisível e nasce um `AssassinDashProjectile` na posição dele, rotacionado pra direção da mira; o projétil anda pra frente (Start), bate 1x em área via `CircleCollider2D` trigger (Effect) e volta pro ponto de origem (End) — só então o Assassin reaparece ali mesmo e toca o clipe de "volta". Thousand Blades (ativo durante a forma sombria) usa a MESMA classe/mecânica, só com prefab/dano/raio próprios (`thousandBladesDamageMultiplier` = 2×).
- **`AssassinDashProjectile.cs`** (novo) — projétil reutilizável pelas 2 variantes; recebe tudo por `Launch()` (direção, dano, velocidade, knockback, raio do trigger), sem nenhum valor de balanceamento próprio (mesmo critério de todo projétil desde a Sprint 27b).
- **Ultimate — forma sombria** — troca o `runtimeAnimatorController` inteiro (mesmo critério do Alce do Druid), com animação de entrada (`StealthTransformIn`, estado padrão do controller sombrio) e de saída (`StealthTransformOut`, via Trigger). Remove o aggro dos monstros (`HeroController.IsPlayerUntargetable`, mesmo campo estático da camuflagem do Ranger/Coruja do Druid). Imune a dano na transformação INTEIRA (transições + during — decisão do usuário, diferente do Alce, que só é imune nas transições).
- **Thousand Blades sem cooldown durante a Ultimate** — novo hook `HeroController.IsAttackCooldownBypassed()` (default `false`, sem efeito em nenhum outro herói), sobrescrito pelo Assassin como `=> isStealthActive`.
- **Teleporte (Shift)** — 4 diagonais (`TeleportAimX/Y`, mesmo critério do Mage), sem fase de projétil (posição muda direto no Animation Event de fim do "disappear"), distância real respeita um teto (`teleportMaxRange`) em vez de sempre pular o máximo. Tem arte e Animator próprios tanto na forma humana quanto na sombria (`ShiftStart`/`ShiftEnd` nos dois controllers).
- **Passiva** — `stats.moveSpeed` mais alto que a média do elenco, sem código (só valor no Inspector).
- **`HeroAnimationGeneratorWindow` ganhou "Clipes extras"** (mesma seção já existente no `CustomAnimationGeneratorWindow`) — necessário porque o Assassin tem vários pares Start/End (Attack, Shift) e o conjunto INTEIRO da forma sombria (idle/walk/dmg/die/attack/shift próprios), que não cabem nos slots fixos da ferramenta.
- **Trava de segurança contra a Ultimate acabar no meio de outra ação** (`stealthEndPending`, pedido do usuário) — nem o timeout (`stealthDuration`) nem o cancelamento manual disparam `StealthTransformOutTrigger` na hora; só marcam a intenção, e `Update()` só executa de verdade quando `isAttacking` voltar a `false` (Attack/Thousand Blades ou Shift em andamento já terminado sozinho).

## Decisões técnicas

- **Redesenho do primário NO MEIO da sprint.** O desenho original (prospectivo, reaproveitando a Cambalhota do Rogue — dano contínuo ao longo do trajeto via `HashSet`, mesmo padrão de dedup) foi implementado, mas o usuário pediu pra trocar por um mecanismo novo depois de ver as animações reais (Start/Effect/End): o Assassin fica fixo e invisível, um projétil representa a "velocidade absurda" indo e voltando, e o dano passa a ser 1 hit em área só, no Effect do projétil — igual pros 2 ataques (Deadly Dash e Thousand Blades), que antes tinham regras diferentes (contínuo vs. hit único). Essa mudança eliminou o `HashSet` de dedup do dash contínuo (não existe mais "ao longo do trajeto" pra deduplicar).
- **2 Animator Controllers completos, não 1 com bool de roteamento** — como a troca de controller já decide qual Attack toca (Deadly Dash no humano, Thousand Blades no sombrio), não precisou de um parâmetro `IsStealthActive` no Animator: cada controller só tem os estados que fazem sentido pra ele, mais simples que a alternativa.
- **Guard por `Time.frameCount` no `AnimationDashStartEndEvent`/`AnimationDashEndEvent`** (bug encontrado em teste) — `DashStart`/`DashEnd` (humano) só têm 4 poses ORTOGONAIS (N/E/S/W), mas a mira pode cair em qualquer uma das 8 direções; mirando numa diagonal, a Blend Tree 2D mistura 2 clipes vizinhos com peso > 0 cada, e os 2 disparavam o Animation Event no mesmo frame — saíam 2 projéteis por dash. Mesmo bug de sempre (Barbarian/EnemyController/Gunslinger), resolvido com o mesmo critério. `ThousandBladesStart/End` (sombrio) usam `DiagonalAimX/Y` com as 4 amostras diagonais certas — sem zona morta, não precisou do guard.
- **`stealthEndPending`** (bug encontrado em teste) — `StealthTransformOutTrigger` é `AnyState` no controller sombrio; disparar ele com um Attack/Thousand Blades (projétil ainda viajando) ou Shift (teleporte ainda no meio) em andamento arrancava o Animator da ação atual, deixando `isAttacking`/visibilidade do sprite inconsistentes. Resolvido deferindo o disparo de verdade pra quando `isAttacking` voltar a `false` por conta própria — tanto no caminho do timeout quanto no cancelamento manual.
- **Patch em `EnemyController.UpdatePatrol()`** — monstro de emboscada (`staysDormantUntilDetected`) voltava pra pose dormente sempre que `isInCombat` virava `false`, mesmo quando o motivo era só `IsPlayerUntargetable` (stealth/camuflagem), não falta de detecção real. Mesmo bug já afetava a camuflagem do Ranger (nunca tinha sido flagrado); corrigido pros dois de uma vez.
- **`ult_die` nasceu placeholder, ganhou arte real durante a sprint** — não existia nenhuma animação de morte pra forma sombria; criei um clipe de 1 frame (pose congelada do idle sombrio) só pra garantir que o `AnimationDieEndEvent` existisse em algum lugar; o usuário substituiu pela arte real (18 frames) antes do fechamento da sprint.

## Arquivos/classes principais

- `Assets/Scripts/Player/Heroes/Assassin.cs` (novo).
- `Assets/Scripts/Player/AssassinDashProjectile.cs` (novo).
- `Assets/Scripts/Player/Heroes/HeroController.cs` — `IsAttackCooldownBypassed()` (novo hook virtual).
- `Assets/Scripts/Enemies/EnemyController.cs` — `UpdatePatrol()` ganhou `&& !HeroController.IsPlayerUntargetable` na condição de voltar a dormir.
- `Assets/Editor/HeroAnimationTools/HeroAnimationGeneratorWindow.cs` — seção "Clipes extras" (mesmo padrão do `CustomAnimationGeneratorWindow`).
- `Assets/Prefabs/Heros/Assassin/Assassin.prefab`, `AssassinDashProjectile.prefab`, `AssassinThousandBladesProjectile.prefab` (novos).
- `Assets/Animation/Heros/Assassin/Assassin.controller` (humano) + `Assassin_Shadow.controller` (sombrio) (novos) — cada um com Idle/Walk/Damage/Die/Trapped + estados próprios de Attack/Shift/transformação, `ActionSpeedMultiplier` ligado em todas as ações (Attack, Ultimate e Shift, incluindo o teleporte puramente de mobilidade — divergência deliberada da regra geral do projeto, a pedido do usuário).
- `Assets/Animation/Heros/Assassin/Projectile/Dash/` e `Projectile/ThousandBlades/` — Animator Controllers + clipes `start`/`effect`/`end` dos 2 projéteis.

## Eventos adicionados

Nenhum em `GameEvents` — toda a comunicação é local (Animation Events) ou um hook virtual novo em `HeroController` (`IsAttackCooldownBypassed`).

## Testes executados

Validação manual em Play Mode pelo usuário, iterativa, com 2 rodadas de bugfix real encontradas em teste:
1. 2 projéteis nascendo por dash (mirando em diagonal) — causa raiz: Blend Tree 2D com só 4 amostras ortogonais pra uma mira de 8 direções, disparando o Animation Event 2x no mesmo frame — corrigido com guard por `Time.frameCount`.
2. Ultimate acabando (timeout ou cancelamento manual) no meio de um Attack/Shift em andamento, arrancando o Animator da ação atual — corrigido com `stealthEndPending`.

Confirmado funcionando pelo usuário após as 2 correções: "Ao meu ver, tá perfeito!".

## Bugs conhecidos

Nenhum pendente do que foi testado.

## Dívida técnica

- `stats` (HP/dano/velocidade/etc.) seguem com valores placeholder — sem calibração de balance real.
- `Trapped` reaproveita a `BlendTree` do Walk nos 2 controllers — sem arte própria ainda (mesmo placeholder que o Ranger usava antes da Sprint 27).
- Raio do `CircleCollider2D` dos 2 projéteis (0.8 Dash / 1.0 Thousand Blades) é chute inicial, não calibrado.
- `docs/gdd/balance-values.md` segue desatualizado (dívida já registrada desde a Sprint 27b, sem mudança de status aqui).
- `ActionSpeedMultiplier` ligado no Shift do Assassin mesmo sendo puramente mobilidade (sem dano/cura) — diverge da regra geral do projeto (só Shift que causa dano/cura recebe isso); decisão explícita do usuário pra este herói, registrada aqui pra não ser confundida com inconsistência não-intencional.

## Próximos passos

Assassin Gameplay Complete — Deadline 7 segue para a Sprint 30 (Blood Mage + Checklist dos 10 Heróis), dependência inalterada (nenhum problema cross-cutting apareceu nesta sprint que justificasse uma "29b").
