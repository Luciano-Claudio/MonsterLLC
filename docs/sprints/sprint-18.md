# Sprint 18 — Ranger (Ultimate)

## Objetivo

Completar o Ranger: Ultimate lança 8 facas nas 8 direções fixas (uma por Animation Event), cada faca perfura em voo (2× dano) e, ao esgotar a reserva ou alcançar a distância máxima, vira Persistent Area no chão por 30s causando dano contínuo (1×) a quem passar por cima — exceção explícita à regra geral de projétil (GDD Seção 13/17.2). Com isso, o Ranger fica jogável por completo (primário + ultimate).

## Ponto de partida: task breakdown de outra sessão de IA

Como já tinha acontecido no esqueleto original do Ranger (Sprint 17), a sprint começou com um breakdown técnico detalhado escrito por outra sessão de IA (`docs/sprint-18-task-breakdown.md`), revisado antes de implementar. Duas correções feitas na revisão, mesmo espírito da vez anterior:

- `using Core.Combat;` removido — o projeto inteiro é namespace global, nenhum arquivo declara namespace.
- O padrão sugerido pra visual da faca em voo (`Animator.Play()` por nome, 8 sprites estáticas) foi trocado por rotação real do sprite (`Quaternion.Euler`), pra ficar consistente com o que a `RangerArrow`/`EnemyProjectile` já usam — depois revertido de novo pra Animator quando o usuário esclareceu que cada direção tem uma **animação** de 4 frames, não sprite estática (mesmo padrão do `HeroProjectile` do Barbarian).
- `transform.Translate()` trocado por `transform.position +=` (mesmo bug de espaço local já corrigido 2x nesta sprint anterior).

## Decisões do breakdown vs. o que foi de fato construído

O breakdown original (Seção 0) registrou 5 decisões que o GDD não fecha no nível de código. Divergências reais entre o que foi sugerido e o que o usuário pediu/o que foi construído:

1. **Dano em tick, não por frame** — implementado como sugerido (`groundedTickInterval`, reaproveitando `AttackCooldown`).
2. **`RangerKnife` não herda de `HeroProjectile`** — mantido como sugerido, classe própria.
3. **Divergência real:** o breakdown sugeriu **1 método parametrizado** (`AnimationThrowKnifeEvent(int directionIndex)`) pros 8 Animation Events. O usuário pediu explicitamente **8 métodos nomeados** (`AnimationThrowKnife_N`, `_NE`, `_E`, etc.), um por direção — foi o que se construiu. Isso trouxe uma consequência que o breakdown não previa: como a Ultimate é uma Blend Tree de 4 diagonais (confirmado com o usuário, não suposição) e cada um dos 4 clipes carrega **todos os 8** eventos (girando em ordens diferentes conforme o clipe), a proteção contra disparo duplicado precisou ser **por direção** (`knifeThrown[8]`), não um único bool geral como no primário.
4. **`DirectionUtility.DirectionFromIndex(int)`** — implementado como sugerido, adição pura.
5. **`groundedRadius` maior que o collider de voo** — implementado como sugerido, 🔢 ajustável.

**Divergência adicional não prevista pelo breakdown:** a visual da faca em voo não usa rotação nem sprite estática — usa Animator com **8 estados soltos**, cada um com uma animação de 4 frames (mesmo padrão do `HeroProjectile`), porque a direção da faca é sempre uma das 8 fixas (nunca mira livre), diferente da `RangerArrow`. A fase "no chão" também é uma **animação** (9º estado no mesmo Animator, sem variação de direção), não uma troca de sprite estática como o breakdown sugeriu.

## Sistemas adicionados

- `Assets/Scripts/Player/RangerKnife.cs` — projétil de 2 fases (voo perfurante → área persistente no chão via `HashSet<EnemyController>` + tick de dano), Animator com 9 estados soltos (8 direções + `Grounded`), sem Blend Tree.
- `Assets/Scripts/Core/DirectionUtility.cs` — `DirectionFromIndex(int)` (inverso de `GetDirectionIndex`), aditivo.
- `Assets/Scripts/Player/Heroes/Ranger.cs` — `UseUltimate()` real (guarda própria, já que `HeroController` não bloqueia Ultimate por `isAttacking`), 8 métodos `AnimationThrowKnife_X` com trava por direção, `AnimationUltimateEndEvent`.
- Animator do Ranger: estado `Ultimate` (Blend Tree de 4 diagonais, `ult_ne/nw/se/sw`), 3 transições de saída (`→Walk`/`→Idle`/`→Damage`, mesma regra do Attack — corrigida depois de faltar a de `→Damage` na primeira montagem).
- **Bônus não planejado nesta sprint, mas resolvido junto:** estado `Trapped` do Ranger (Blend Tree de 4 diagonais, reaproveitando clipes `trapped_ne/nw/se/sw` que já existiam soltos) — o hook `SetTrapped()` já existia desde o Barbarian (Sprint 17), só faltava o Animator do Ranger ter o estado. Não é usado por nenhum sistema real ainda (Efeitos Nocivos não existe), mas fica pronto.

## Testes executados

Checklist manual em Play Mode (confirmado pelo usuário): Ultimate acumula Energia e dispara via RMB; 8 facas saem ao longo do giro, uma por Animation Event, não todas de uma vez; direções batem com as 8 fixas, não a mira; faca em voo perfura com knockback; faca esgotada/no alcance máximo vira área persistente (collider cresce, Animator troca pro estado `Grounded`); monstro já em cima do ponto de pouso toma dano no primeiro tick (seed via `OverlapCircleAll`); monstro que entra/sai da área começa/para de tomar dano; área desaparece sozinha no fim de `groundedDuration`; pausa (TAB) trava as duas fases; `isAttacking` bloqueia sobrepor ação. Sem testes automatizados novos.

## Dívida técnica / pendências

- `maxActionDuration` (3f) continua compartilhado entre primário e Ultimate, calibrado só pro tiro único — pode precisar de ajuste ou de um campo próprio depois de mais playtesting com o clipe real da Ultimate.
- Valores `🔢` da faca (`flightSpeed`, `maxDistance`, `groundedTickInterval`, `groundedRadius`, forças de knockback) são chutes de primeiro teste.
- Trapped do Ranger existe mas não tem nenhum sistema real que o acione ainda (mesma pendência geral de Efeitos Nocivos já registrada na Sprint 17).

## Próximos passos

Ranger está completo (primário + ultimate) — **fecha o escopo dos heróis Ranger dentro da Deadline 5**. Próxima peça da Deadline: Sprint 19 (Mage — Primário + Pet Phoenix).
