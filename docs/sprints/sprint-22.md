# Sprint 22 — Rogue Completo (Primário + Ultimate + Shift + Passiva)

## Objetivo

Escopo oficial (tabela da Produção, Deadline 6): "Rogue — Completo". Entrega: Self Area Pulse (trigger circular nos pés, 1x por Animation Event) funciona com cooldown; bomba da ultimate viaja até colidir ou alcançar o alcance máximo e explode em área (4× o dano); cambalhota (Shift) em 1 de 4 direções fixas, imune a dano, aplica knockback; passiva rende 4× mais Energia de Ultimate por kill. Partiu de um task breakdown pronto (`docs/sprint-22-task-breakdown.md`), validado contra GDD Seção 17.5 antes de codar — achou 1 divergência real (direção da Cambalhota) e um item que o próprio breakdown deixou em aberto (reset de `isRolling` na morte), resolvidos durante a implementação.

## Sistemas adicionados

- **`Rogue.cs`** (novo) — os 3 kits do herói:
  - **Primário (Self Area Pulse):** trigger circular centrado no próprio Rogue (`OverlapCircleAll`, não child hitbox), 1x por Animation Event, com dano **e knockback** (decisão tomada durante a sprint — mesma categoria "Self Area Pulse" do Barbarian, que já tem os dois).
  - **Ultimate (Bomba):** projétil em ângulo livre (`RawAimDirection`, rotação real, não travada em 8 direções) — espelha a bola de fogo do Mage, mas sem fase de superfície persistente no chão.
  - **Secundária (Cambalhota/Shift):** decisão explícita do usuário no meio da sprint — movimento **livre** seguindo a mira (`RawAimDirection`), não travado numa das 4 direções como o breakdown original assumia; só a **pose** do Animator trava numa das 4 diagonais (`RollAimX/Y`, `DirectionUtility.SnapTo4Diagonals`). Imune a dano e **sem colisão física com monstro** (`Physics2D.IgnoreLayerCollision`, mesmo padrão da Coruja do Druid) durante a viagem inteira. Aplica knockback, não dano.
  - **Passiva:** `UltimateEnergyMultiplier => 4f` (hook já preparado no `HeroController` desde o breakdown).
- **`RogueBomb.cs`** (novo) — projétil autônomo, reestruturado durante a sprint pra bater exatamente com o `MageFireball` (voo em ângulo livre → `AnimationExplodeHitEvent` separado de `Explode()`, não dano direto nele → `AnimationExplodeEndEvent` destrói), com gizmo + offset Y da área de explosão pra posicionar no Editor.
- **`Rogue.controller`** (novo) — Idle/Walk/Damage/Attack/Ultimate/Roll como Blend Tree `2D Freeform Directional` diagonal (`DiagonalAimX/Y` ou `RollAimX/Y` pro Roll), Die como clipe único sem Blend Tree — todos os 25 clipes fatiados e os 13 não-loop com `AnimationXHitEvent`/`AnimationXEndEvent` wireados no frame certo.
- **`Bomb/RogueBomb.controller`** (novo) — 2 estados (`Fly`/`Explode`), bem mais simples que o do herói (sem direção, a rotação do GameObject já resolve isso).
- **`HeroController.IsCurrentlyDamageImmune`** (novo, público) — achado e corrigido nesta sprint: expõe `isDead || IsDamageImmune` publicamente pra quem aplica dano de fora (ex.: `EnemyProjectile`) saber se o `TakeDamage()` vai ser um no-op ANTES de decidir aplicar um Efeito Nocivo junto.

## Decisões técnicas

- **Pulso ganhou knockback, não só dano** — GDD/breakdown original não mencionava knockback no Primário; usuário pediu durante a sprint, citando o precedente do Barbarian (mesma categoria "Self Area Pulse" na tabela da Seção 13). Usa `AimDirection` (8 direções, mesmo critério do Barbarian), não um vetor radial por monstro.
- **Cambalhota: movimento livre, pose travada em diagonal** — divergência dupla em relação ao breakdown original: (1) a trajetória segue a mira de verdade, não trava em 1 de 4 direções; (2) as 4 poses da arte são diagonais (NE/NW/SE/SW), não cardeais (N/E/S/W) como a GDD previa — corrigido depois que o usuário viu a arte real funcionando melhor assim. GDD Seção 17.5 corrigida nos dois pontos.
- **`SnapTo4Cardinals` criado e depois removido** — a primeira versão da Cambalhota usava snap cardeal; quando a arte virou diagonal, a função ficou sem nenhum uso no projeto inteiro (confirmado por grep) e foi removida — `SnapTo4Diagonals` (já existente, usado por Idle/Walk/Dmg) cobre o caso.
- **Bomba redesenhada pra espelhar o `MageFireball`, não o design original do breakdown** — pedido explícito do usuário ("vai ser exatamente igual ao do mage"). Saiu de "dano aplicado dentro de `Explode()`" pra "Animation Event dedicado (`AnimationExplodeHitEvent`) no clipe de explosão", igual o fireball. Única diferença combinada: sem fase `Grounded` (não deixa superfície persistente).
- **Gizmo + offset Y em toda área de dano em círculo** (Pulso, Bomba) — mesmo padrão já usado na Vine/`EnemyController` (`showXGizmo`/`xGizmoOffsetY`, propriedade `XCenter` computada), adicionado a pedido do usuário pra conseguir posicionar a área visualmente no Editor em vez de adivinhar.
- **`IsCurrentlyDamageImmune` consolidado na base, não só no Rogue** — achado via bug real (tocha aplicando Fire na Cambalhota mesmo com imunidade a dano). Causa raiz: `EnemyProjectile.ApplyStatus()` chamava `TakeDamage()` (que já bloqueava o dano certo) mas aplicava o Efeito Nocivo **sem checar se o dano tinha sido bloqueado**. Corrigido com uma propriedade pública nova na base, consultada pelo `EnemyProjectile` antes de aplicar status — cobre Druid (Alce/Coruja), Mage (teleporte) e Ranger (camuflagem) de graça, mesmo raciocínio do `CanUseUltimate()`/`IsDamageImmune` da Sprint 21 (um fix na base em vez de 4 fixes espalhados).

## Arquivos/classes principais

- `Assets/Scripts/Player/Heroes/Rogue.cs` (novo) — os 3 kits completos.
- `Assets/Scripts/Player/Heroes/RogueBomb.cs` (novo).
- `Assets/Scripts/Player/Heroes/HeroController.cs` — `UltimateEnergyMultiplier` (já do breakdown), `IsCurrentlyDamageImmune` (novo, bugfix desta sprint).
- `Assets/Scripts/Enemies/EnemyProjectile.cs` — `ApplyStatus()` agora checa `IsCurrentlyDamageImmune`.
- `Assets/Scripts/Core/DirectionUtility.cs` — `SnapTo4Cardinals` adicionada e removida na mesma sprint (ver Decisões técnicas).
- `Assets/Animation/Heros/Rogue/Rogue.controller` (novo) + 25 clipes (13 com Animation Events).
- `Assets/Animation/Heros/Rogue/Bomb/RogueBomb.controller` (novo) + `bomb_fly`/`bomb_explode`.
- `Assets/Prefabs/Heros/Rogue/Rogue.prefab`, `RogueBomb.prefab` (novos).
- `docs/gdd/index.md` — Seção 17.5: knockback do Primário, correção da direção da Cambalhota (diagonal, não cardeal), correção do ponto de nascimento da bomba (não "na posição do mouse").
- `Assets/Scenes/_TestScene.unity` — override órfão de `statusAnimator` removido (ver Bugs conhecidos).

## Eventos adicionados

Nenhum `GameEvents` novo — reaproveita `EnergyChanged`/`EnemyKilled`/`DamageTaken` já existentes.

## Testes executados

Só manual (Play Mode), iterativo, confirmado pelo usuário: Pulso (dano + knockback em área), Ultimate (voo em ângulo livre, colisão, explosão em área — depois do fix de collider), Cambalhota (movimento livre, pose diagonal, imunidade a dano, atravessar monstro sem colidir, knockback), passiva de energia (multiplicador 4×, lockout não-farmável depois do ajuste), status Fire (DoT + ícone visual, depois de 2 fixes). Sem suíte automatizada.

## Bugs conhecidos / corrigidos durante a sprint

- **Bomba atravessava monstro reto, nunca explodia** — `RogueBomb.prefab` não tinha Rigidbody2D nem Collider2D nenhum (sem isso o Unity 2D não gera evento de trigger). Corrigido com Rigidbody2D Kinematic + CircleCollider2D (Is Trigger), mesmo padrão do `MageFireball.prefab`.
- **Rogue farmando a própria Ultimate** — lockout de energia pós-Ultimate (2s, padrão de todo herói) era mais curto que o pior caso do pipeline voo+explosão da bomba (~2,1s), deixando o kill da própria explosão render energia cheia (4×) depois do lockout já ter expirado. Corrigido subindo `ultimateEnergyLockoutDuration` do Rogue pra 3s (só Inspector, sem código novo).
- **Imunidade a dano não bloqueava Efeito Nocivo** — ver `IsCurrentlyDamageImmune` em Decisões técnicas. Afetava potencialmente todo herói com janela de imunidade, não só o Rogue.
- **Status Fire sem ícone visual** — 2 causas empilhadas: (1) `statusAnimator` do Rogue desligado (`fileID: 0`) no prefab, religado pro Animator do filho "Status"; (2) depois de religado, ainda não aparecia — causa real era um **override órfão na cena** (`_TestScene.unity`) apontando o `statusAnimator` da instância do Rogue pro Animator de Status **do Druid** (referência cross-prefab, provavelmente sobrou de copiar a instância do Druid pra criar a do Rogue). Removido o override, a instância passou a herdar o valor correto do prefab.
- **Falso alarme**: suspeita inicial de que o ícone de Status ficava atrás do corpo por empate de Sorting Order — descartado pelo usuário (offset de posição já garante que fica acima do player, e não deve ficar acima de monstro de propósito); a mudança de Sorting Order feita por engano foi revertida.

## Dívida técnica

- **Valores `🔢` de balanceamento**, todos placeholder de primeiro teste: `pulseRadius`, `pulseKnockbackForce`, `speed`/`defaultMaxDistance`/`explosionRadius` da bomba, `rollSpeed`/`rollKnockbackForce`/`rollHitRadius`/`rollMaxSafetyDuration`, `maxActionDuration`.
- **`RogueRun.png` fatiado mas não usado** — Walk acabou sendo a animação escolhida pro estado "Walk" do controller; os frames do Run existem no projeto mas não estão wireados em nenhum clipe/controller. Puramente cosmético, sem efeito de gameplay.
- **Nome "Shurikens" no sprite sheet do Attack é resquício do design antigo** ("adagas orbitando", descartado antes desta sprint) — a arte em si já bate com o Self Area Pulse atual (burst/sparkle), só o nome do arquivo ficou desatualizado.
- **`explosionGizmoOffsetY`/`pulseGizmoOffsetY` ainda no valor default (0)** — gizmos adicionados pra o usuário ajustar no Editor, mas o ajuste fino fica pra quando ele for calibrar a sensação das áreas em Play Mode.
- **`IsCurrentlyDamageImmune` só é consultado pelo `EnemyProjectile` hoje** — é o único call site que aplica Efeito Nocivo em herói; qualquer fonte de dano nova que também aplique status precisa lembrar de checar essa propriedade (documentado no comentário do método, não há enforcement automático).

## Próximos passos

Rogue completo e testado — Deadline 6 segue com Cleric, Boss Framework, Boss Timer e os bosses de Floor 1-2. O fix de `IsCurrentlyDamageImmune` e a lição do Rigidbody2D/Collider2D obrigatório pra trigger funcionar ficam prontos pra qualquer herói/projétil futuro — não precisa redescobrir nenhum dos dois. Vale também lembrar, pro próximo herói com instância de cena copiada de outro: conferir se não sobrou nenhum override de Prefab Instance apontando pro objeto errado (mesma classe de bug do `statusAnimator` cross-prefab encontrado aqui).
