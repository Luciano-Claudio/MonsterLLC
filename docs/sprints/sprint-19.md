# Sprint 19 — Mage Completo (Primário + Ultimate + Secundária/Shift + Pet Phoenix)

## Objetivo

Mage sai da sprint com o kit inteiro de uma vez, não só primário + pet como o plano original previa — Ultimate e Shift do Mage (que estavam reservados pra Sprint 20) também saíram junto.

## Sistemas adicionados

- **Ataque primário** — `AttackHitboxPivot`/`AttackHitbox`: um GameObject filho gira em tempo real seguindo `RawAimDirection` (trava sozinho durante `isAttacking`, já que a mira congela); no Animation Event do cast do Mage, o filho dispara sua própria animação de fogo (Animator próprio, separado do Animator do Mage) e aplica o dano no instante certo dessa segunda animação.
- **Ultimate** — `MageFireball.cs`: um objeto só com 3 fases (`Projectile` → `Exploding` → `Grounded` → `Disappearing`, mesmo mecanismo da `RangerKnife`, voo→chão). Viaja em **ângulo livre** (única exceção do MVP entre projéteis retos, que travam nas 8 direções), explode ao colidir ou alcançar a distância máxima (dano de impacto = 4× o dano do Mage), e deixa um rastro de fogo persistente (0,5× o dano do Mage por segundo, 30s). Nasce direto da posição do próprio Mage — sem pontos fixos de lançamento, decisão tomada em cima da hora já que a mira é livre.
- **Habilidade Secundária (Shift) — teleporte**: o Mage some (sprite desligada) no último frame de `teleport_start`, um `MageTeleportProjectile` viaja pela sala pela distância combinada, e só quando ele termina de viajar (tempo variável, não fixo) é que o Mage reaparece de verdade (posição + `teleport_end`) — a transição entre as 2 fases é disparada por código (`TeleportArriveTrigger`), não por Exit Time, exatamente por causa dessa variabilidade.
- **Phoenix (pet)** — `PetController.cs`, genérico o bastante pra também servir o Elemental de Sangue do Blood Mage quando a sprint dele abrir. Sumona com animação própria do Mage (Animation Event no meio, duração = a do clipe, não um tempo fixo); trava no alvo até ele morrer de verdade (não fica trocando de alvo toda vez que o jogador se move); muda de Floor teleportando pro spawn point, não andando; toca a própria animação de `die`/disappear quando o herói morre, só destruída de verdade no fim dela.
- **GDD sincronizado** ao longo de toda a sprint (Seção 17.3, "Pets de início de dia", Seção 46).

## Decisões técnicas

- **Ultimate sem pontos fixos de lançamento** — o breakdown original previa 8 GameObjects filhos escolhidos pela mira (espelhando o Barbarian); como a Ultimate do Mage usa ângulo livre, não faz sentido ter "portas de saída" fixas — a bola nasce direto de `transform.position`.
- **Teleporte é guiado pelo projétil, não por tempo fixo** — desenho corrigido no meio da sprint: inicialmente o teleporte seria instantâneo (Animation Event move o Mage na hora); o usuário pediu que o projétil viajasse fisicamente pela sala primeiro, e só quando ele chegasse é que o Mage reaparecesse — isso trocou a transição `teleport_start`→`teleport_end` de Exit Time automático pra Trigger disparado por código.
- **`TeleportAimX`/`TeleportAimY`** — par de parâmetros próprio do teleporte, separado do `AimX`/`AimY` genérico, travado uma única vez no instante do cast — garante que `teleport_start` e `teleport_end` sempre mostrem a mesma pose, não importa o que mais mexa em `AimX`/`AimY` nesse meio tempo.
- **`isTeleporting`** — exclui o teleporte do timer genérico de segurança (`maxActionDuration`, pensado pro golpe único do Attack/Ultimate) porque a viagem do projétil tem duração variável.
- **Pet trava no alvo até morrer** — versão inicial reavaliava o alvo mais próximo a cada frame (raio centrado no dono), o que fazia o pet trocar de alvo toda vez que o jogador se movia; corrigido pra só reavaliar quando o alvo atual é destruído de verdade (Unity já resolve isso sozinho: `Transform` de um `GameObject` destruído vira "null" pelo operador sobrecarregado).

## Arquivos/classes principais

- `Assets/Scripts/Player/Heroes/Mage.cs` — primário, ultimate, shift, invocação do pet, hook de morte.
- `Assets/Scripts/Player/MageAttackHitbox.cs`, `MageFireball.cs`, `MageTeleportProjectile.cs` — filhos/projéteis do kit do Mage.
- `Assets/Scripts/Enemies/PetController.cs` — IA do pet, genérica (Phoenix hoje, Elemental de Sangue no futuro).
- `Assets/Scripts/Player/Heroes/HeroController.cs` — `RawAimDistance`, `OnHeroDeath()` (hook virtual novo).
- `Assets/Scripts/Core/GameEvents.cs` + `Assets/Scripts/DayCycle/DayTimer.cs` — `OnDayStart`/`DayStarted()`, fonte única dentro de `ResetForNewDay()`.

## Bugs conhecidos / corrigidos durante a sprint

- Campo `spriteRenderer` do Mage colidindo com o do `HeroController` (erro de serialização) — renomeado pra `mageSpriteRenderer`.
- Espelhamento Leste-Oeste no Blend Tree de Attack/Idle/Walk do Mage (NE↔NW, SE↔SW trocados) — corrigido trocando qual clipe ocupa qual posição na árvore.
- `Phoenix` sem `AnimationDieEndEvent` nos 4 clipes de `die` — ficava congelada na pose de morte pra sempre; corrigido pelo usuário depois do aviso.

## Dívida técnica

- Valores `🔢` de balanceamento (multiplicadores, distâncias, durações) são chutes de primeiro teste.
- `MageTeleportProjectile`/`MageFireball` não colidem com paredes — viajam a distância máxima sempre, sem obstáculo.

## Próximos passos

Mage está completo (primário + ultimate + secundária + pet). Ver [Sprint 19b](sprint-19b-efeitos-nocivos.md) pro sistema de Efeitos Nocivos e as correções de base que saíram logo depois, antes de abrir a Sprint 20 (Floor 1A–2A + Bestiary Batch 1+2).
