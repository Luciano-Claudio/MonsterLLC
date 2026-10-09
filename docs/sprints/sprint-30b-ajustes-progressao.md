# Sprint 30b — Ajustes de Progressão (Assassin/Blood Mage/Rogue/Druid)

## Objetivo

Preparar 4 heróis já completos (Assassin, Blood Mage, Rogue, Druid) para um sistema de progressão futuro ainda não desenhado — sem fechar nenhum kit novo, só tornando mecânicas existentes configuráveis/ajustáveis e corrigindo 1 inconsistência real de mira encontrada no processo.

## Sistemas adicionados

- **Assassin — leque de projéteis no Deadly Dash/Thousand Blades** — `dashProjectileCount` (1–5, mesmo critério do `hammerCount` do Paladin): a partir de 2, nascem múltiplos `AssassinDashProjectile` fazendo a viagem de ida-e-volta inteira e independente cada, em ângulos diferentes ao redor da mesma mira — sempre o mesmo Assassin único e invisível, só "multiplica" a saraivada de golpes. `dashFanAngleStep` (padrão 15°) deixa a separação entre os ângulos configurável — diferente do Paladin/Blood Mage (fixo em 15°), o dash do Assassin precisa de mais distância entre os projéteis. Leque **simétrico centrado no mouse** (não 1 projétil sempre fixo nele, diferente de propósito do Paladin/Blood Mage): com N par, os 2 projéteis centrais ficam a ±metade do passo um do outro, e o ponto médio entre eles aponta pro mouse.
- **Assassin — dedup compartilhado entre projéteis da mesma leva** — com `dashProjectileCount > 1`, os raios de 2 projéteis vizinhos podem se interceptar num mesmo monstro; `Assassin.TryClaimDashHit()` (HashSet, zerado a cada novo dash) garante que cada monstro só recebe dano do PRIMEIRO projétil que o alcançar, nunca de 2+.
- **Assassin/Blood Mage — projétil segue o mouse de verdade** — os 2 heróis travavam a direção real do projétil em `AimDirection` (8 direções fixas), resquício de antes do projétil rotacionar o próprio sprite; trocado pra `RawAimDirection` (ângulo livre), mesmo critério já usado por `MageFireball`/`RangerArrow`/`PaladinHammer` (confirmado que o Paladin já usava `RawAimDirection` — nenhuma mudança necessária nele).
- **Rogue — primário (Self Area Pulse) redesenhado em 3 estágios** — 1) `attack_start` (corpo, 4 diagonais) ativa 2) `RoguePulseArea`, filho FIXO do Rogue (nunca instanciado, sempre no prefab, segue a posição dele) que fica tocando "Cycle" em loop (igual à Ultimate orbital do Paladin) e dando dano por segundo via `CircleCollider2D` real (trigger — antes era `OverlapCircleAll` manual no próprio Rogue, 1 hit só); 3) quando `pulseDuration` (configurável) esgota, desativa a área e o Rogue toca `attack_end` (corpo, 4 diagonais). Rogue fica LIVRE pra se mover/agir durante a fase 2 (decisão do usuário) — como a área é filha dele, ela acompanha. `pulseTickInterval` (cadência do DPS) e `pulseSizeMultiplier` (escala o filho inteiro — visual + o próprio `CircleCollider2D`, que a Unity já escala sozinha via `transform.localScale`, sem lógica extra) completam a configuração.
- **Druid — tamanho da forma Alce configurável** — `elkSizeMultiplier` (1–3x) escala `transform.localScale` no instante em que a transformação termina: visual, o `CapsuleCollider2D` físico (definido em espaço local, escala junto de graça) e as 4 hitboxes de garra (filhas do Druid, seguem a escala do pai — um Alce maior ataca mais longe também). Revertido pra tamanho humano normal (`Vector3.one`) tanto ao voltar ao normal quanto se a morte acontecer durante a forma Alce (mesmo instante em que o resto da transformação já revertia).

## Decisões técnicas

- **Leque do Assassin é matematicamente diferente do Paladin/Blood Mage, de propósito** — `GetFanAngles(count, angleStep)` usa `(i - (count-1)/2) × angleStep` em vez de "0° fixo + pares simétricos + sobra resolvida por quadrante": o resultado é sempre simétrico ao redor de 0°, então o MEIO do conjunto (não um projétil específico) é que aponta pro mouse. Pedido explícito do usuário — queria o conjunto controlado pelo mouse, não 1 projétil fixo nele.
- **`RoguePulseArea` não precisa de nenhuma lógica de escala manual** — diferente do anel do Blood Mage (que usa `OverlapCircleAll` puro, sem `Collider2D` real, e por isso precisa ler `transform.localScale.x` manualmente pra calcular o raio), o `RoguePulseArea` tem um `CircleCollider2D` de verdade: a própria Unity já escala o trigger físico E o `SpriteRenderer` a partir do `transform.localScale` do GameObject, de graça. `pulseSizeMultiplier` só precisa setar a escala 1 vez, no `Rogue.cs`, antes de ativar.
- **`attackEndPending` no Rogue** (mesmo critério do `stealthEndPending` do Assassin, Sprint 29) — `AttackEndTrigger` é `AnyState` no `Rogue.controller`; se `pulseDuration` esgotar enquanto o Rogue está no meio da Cambalhota (que trava `isAttacking` a viagem INTEIRA), disparar na hora arrancaria o Animator do "Roll". A desativação da área em si (parar o dano por segundo) não espera — só o Trigger/animação do `attack_end` é deferido até `isAttacking` voltar a `false` sozinho.
- **Rogue fica livre durante o pulso, trava de novo só no `attack_end`** — decisão explícita do usuário (perguntada antes de implementar): `isAttacking` volta a `false` assim que `attack_start` termina, mesmo critério de "o herói recupera o controle na hora" já usado pro Ranger (flecha)/Rogue (bomba) com objetos filhos independentes.

## Arquivos/classes principais

- `Assets/Scripts/Player/Heroes/Assassin.cs` — `dashProjectileCount`/`dashFanAngleStep`/`dashVolleyHitTargets`/`TryClaimDashHit`/`GetFanAngles` (redesenhado) — leque + dedup compartilhado.
- `Assets/Scripts/Player/AssassinDashProjectile.cs` — `AnimationProjectileReturnedEvent` passa a própria referência pro owner; `AnimationProjectileEffectHitEvent` consulta `TryClaimDashHit` antes do dano.
- `Assets/Scripts/Player/Heroes/BloodMage.cs` — `AnimationProjectileLaunchEvent` usa `RawAimDirection`.
- `Assets/Scripts/Player/Heroes/Rogue.cs` — reescrita do primário (3 estágios), `pulseArea`/`pulseDuration`/`pulseTickInterval`/`pulseSizeMultiplier`/`attackEndPending`.
- `Assets/Scripts/Player/RoguePulseArea.cs` (novo) — filho fixo, Cycle em loop, DPS via `AttackCooldown` + `HashSet` (mesmo critério do rastro "Grounded" do `MageFireball`).
- `Assets/Scripts/Player/Heroes/Druid.cs` — `elkSizeMultiplier`, aplicado/revertido nas 2 transições + morte durante a forma.
- `Assets/Animation/Heros/Rogue/attack_ne/nw/se/sw.anim` renomeados para `attack_start_ne/nw/se/sw.anim` (arte real preservada); `attack_end_ne/nw/se/sw.anim` (novos, placeholder) criados.
- `Assets/Animation/Heros/Rogue/PulseArea/` (novo) — `RoguePulseArea.controller` + `cycle.anim`.
- `Assets/Animation/Heros/Rogue/Rogue.controller` — estado "Attack" renomeado "AttackStart"; novo estado "AttackEnd" + `AttackEndTrigger`.
- `Assets/Prefabs/Heros/Rogue/Rogue.prefab` — novo filho `PulseArea` (Transform/SpriteRenderer/Animator/CircleCollider2D trigger/`RoguePulseArea`); campos antigos do pulso (`pulseRadius`/`pulseKnockbackForce`/`showPulseGizmo`/`pulseGizmoOffsetY`) removidos.
- `Assets/Prefabs/Heros/Assassin/Assassin.prefab`, `Assets/Prefabs/Heros/Druid/Druid.prefab` — novos campos (`dashProjectileCount`/`dashFanAngleStep`/`elkSizeMultiplier`).

## Eventos adicionados

Nenhum em `GameEvents` — toda a comunicação é local (Animation Events) ou callback direto.

## Testes executados

Validação manual incremental pelo usuário a cada mudança, sem bug relatado nesta leva (diferente das sprints anteriores — essa foi só "ajuste de design", sem ciclo de bugfix). Confirmado funcionando: "Perfeito!!" (leque do Assassin), implícito nas demais pela continuidade do pedido seguinte.

## Bugs conhecidos

Nenhum reportado.

## Dívida técnica

- `attack_end_ne/nw/se/sw.anim` do Rogue seguem como placeholder (sem arte real) — usuário precisa substituir pela Animation window.
- `cycle.anim` do `RoguePulseArea` segue como placeholder (sem arte real).
- `pulseDuration`/`pulseTickInterval`/raio do `CircleCollider2D` do `RoguePulseArea` são chute inicial, não calibrados.
- Nenhum sistema de progressão foi desenhado ainda — esses campos (`dashProjectileCount`, `elkSizeMultiplier`, `pulseSizeMultiplier`, etc.) só tornam as mecânicas configuráveis no Inspector; a camada que vai realmente controlá-los em runtime (cartas, upgrades, o que for) continua inteiramente pendente.

## Próximos passos

Nenhum sprint novo disparado — ajustes pontuais dentro da Deadline 7 (mesmo critério da Sprint 27b), sem mudar a dependência da Sprint 31 (15 LootDefinitions, Deadline 8), que segue valendo a partir da Sprint 30.
