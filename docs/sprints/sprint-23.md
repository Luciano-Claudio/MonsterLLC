# Sprint 23 — Cleric Completo (Primário + Ultimate + Shift + Passiva)

## Objetivo

Escopo oficial (tabela da Produção, Deadline 6): "Cleric — Completo". Entrega: projétil homing que persegue o monstro mais próximo (só ataca com 1+ monstro no raio); Oração (ultimate) paralisa e aplica DoT (texto original: "em todos os monstros do Floor" — corrigido durante a sprint, ver Decisões técnicas); cura vira ativa via Shift; passiva dobra o dano do Cleric. Partiu de um task breakdown pronto (`docs/sprint-23-task-breakdown.md`), validado contra GDD Seção 17.6 antes de codar — essa foi a sprint com mais superfície nova até agora (primeira incapacitação do lado do monstro, primeiro Efeito Nocivo que é DoT e paralisia ao mesmo tempo), e também a que mais mudou de desenho em relação ao breakdown original, por decisão direta do usuário durante a implementação.

## Sistemas adicionados

- **`Cleric.cs`** (novo) — os 3 kits do herói:
  - **Primário (Projétil Homing):** persegue o(s) N monstro(s) mais próximos dentro de `attackRadius` (N = `projectileCount`, teste do usuário — começa em 1, upgrade futuro pode subir). Sem monstro no raio, o clique não faz nada (única exceção do MVP a "todo herói sempre pode atacar").
  - **Ultimate (Oração):** raio ao redor do Cleric (não Floor inteiro, correção do usuário), aplica o Efeito Nocivo **WordOfPain** em todo monstro atingido — paralisa e causa DoT ao mesmo tempo, dano por tick sempre = metade do `stats.damage` do Cleric (`prayerTickDamageMultiplier`, SEM o 2× da passiva).
  - **Secundária (Shift) — Reza:** cura ativa em 4 ondas (1/4 da cura total cada), via efeito visual dedicado (`ClericShiftEffect`, filho com Animator próprio) — não mais 1 evento único no corpo do Cleric. Pose diagonal (correção do usuário, GDD previa cardeal).
  - **Passiva:** 2× dano do Cleric, aplicado só no hit do projétil (não no tick da Oração, que tem regra própria).
- **`ClericProjectile.cs`** (novo) — homing com rotação contínua (mesma técnica da `RangerArrow`), reserva-por-hit com perfuração (mesmo mecanismo do `RangerArrow`/`HeroProjectile`), fase de impacto (`Impact()`) com Animator opcional e orientação travada, igual o `RogueBomb`.
- **`ClericShiftEffect.cs`** (novo) — filho dedicado da Reza: `Play()` reinicia o clipe do zero (controller de 1 estado, sem Trigger), e repassa os 4 Animation Events (`AnimationHealWaveEvent`) de volta pro Cleric.
- **`StatusEffectController.FlashStatus()`** (novo) — "piscada" de status de 1x só, sem entrar em `activeEffects` (não é DoT, não acumula): toca por cima de qualquer Efeito ativo por um tempo fixo, depois volta sozinho pro estado real. Usado pelo Heal (Reza) e seria reaproveitável por qualquer outro Efeito instantâneo futuro.
- **`StatusEffectType.WordOfPain`** (novo) — primeiro Efeito que é DoT E incapacitação ao mesmo tempo (as 12 categorias antigas sempre foram uma coisa OU outra); também o único com prioridade visual sobre qualquer outro Efeito ativo (`UpdateVisual()` checa ele primeiro).
- **`StatusEffectController.IsEffectActive()`** (novo) — consulta pública de "esse Efeito está rolando agora", usada pelo `EnemyController` pra decidir paralisia sem precisar de uma flag própria (a incapacitação dura exatamente o tempo que o Efeito durar, sem risco de desincronizar).
- **`EnemyController`** — paralisia da Oração é direto `statusEffectController.IsEffectActive(WordOfPain)` no `Update()` (trava IA/ataque/movimento, dano e outros Efeitos continuam normais) — sem campo/flag novo nenhum.

## Decisões técnicas

- **Oração: raio, não Floor inteiro** — a GDD original e a tabela de Produção diziam "todos os monstros em campo"; o usuário corrigiu durante a sprint pra "raio de visão do Cleric", mesma categoria de área que todo outro herói já usa. Isso também matou a necessidade do `FloorPopulationManager.AliveEnemies` que eu tinha adicionado antes da correção — revertido (ficaria sem nenhum uso).
- **Oração vira Efeito Nocivo de verdade, não GameObject bespoke por alvo** — a primeira versão (`ClericPrayerEffect.cs`, seguindo a GDD ao pé da letra) nascia 1 GameObject por monstro atingido, cada um com Animation Event próprio. O usuário decidiu reaproveitar o sistema de `StatusEffectController` já existente (igual Fire/Bleeding) em vez de duplicar — `ClericPrayerEffect.cs` foi deletado. Isso exigiu resolver 2 problemas novos que o sistema de Efeitos nunca tinha: incapacitação (resolvido com `IsEffectActive` consultado direto pelo `EnemyController`, sem flag própria) e prioridade visual entre Efeitos simultâneos (`WordOfPain` sempre vence).
- **Tick da Oração = metade do dano normal, não um valor fixo nem o 2× da passiva** — decisão explícita do usuário, com exemplo numérico (Cleric com 100 de dano → 50 por tick). Trocado de `prayerTickDamage` (🔢 fixo) pra `prayerTickDamageMultiplier` (🔢 fração do `stats.damage`), e a passiva (`ClericDamageMultiplier`) explicitamente NÃO entra nessa conta — só no hit do projétil.
- **Cura em 4 ondas, não 1 evento único** — decisão do usuário: a cura total (`healPercentOfMaxHealth`) é dividida em 4 partes iguais (1/4 cada), cada uma disparada por um Animation Event do clipe do `ClericShiftEffect` (não mais do corpo do Cleric). Cada onda também "pisca" o status Heal por cima de qualquer Efeito já ativo (`FlashStatus`), que volta sozinho ao normal depois.
- **Pose da Reza é diagonal, não cardeal** — mesmo caso exato da Cambalhota do Rogue (Sprint 22): a GDD previa N/E/S/W, a arte real ficou melhor em NE/NW/SE/SW. `SnapTo4Cardinals` foi recriada especificamente pra essa sprint e removida de novo quando a decisão mudou — segunda vez que essa função nasce e morre na mesma sprint que a criou.
- **`ClericProjectile` não reaproveita `HeroProjectile.cs`** — o breakdown original tinha dúvida sobre isso; resolvido checando o código real: `HeroProjectile.cs` troca entre 8 poses fixas via Animator (não rotaciona), a técnica de rotação contínua citada na GDD é da `RangerArrow.cs`. `ClericProjectile` foi modelado nela, com a adição de homing (redireciona a cada frame até perder/acertar o alvo).
- **`ShiftEffect.controller` precisa de um estado "Empty" como default, com transição de volta por Exit Time** — sem isso (estrutura inicial tinha só 1 estado, sem loop), o Animator ficava preso no último frame do clipe de cura pra sempre depois de tocar 1x, parecendo "sempre ativo". Resolvido com um 2º estado (clipe em branco) como default + transição `HealStatusCleric → Empty` só por Exit Time — e o sprite padrão do SpriteRenderer do `Shift_Effect` precisou ser limpo também (Write Defaults voltava pro 1º frame da cura em vez de ficar invisível).

## Arquivos/classes principais

- `Assets/Scripts/Player/Heroes/Cleric.cs` (novo) — os 3 kits completos.
- `Assets/Scripts/Player/ClericProjectile.cs` (novo).
- `Assets/Scripts/Player/ClericShiftEffect.cs` (novo).
- `Assets/Scripts/Core/StatusEffectController.cs` — `WordOfPain` (enum), `IsEffectActive()`, `FlashStatus()`, `Tick()` reestruturado.
- `Assets/Scripts/Enemies/EnemyController.cs` — paralisia via `IsEffectActive(WordOfPain)` direto no `Update()`.
- `Assets/Scripts/Core/DirectionUtility.cs` — `SnapTo4Cardinals` recriada e removida de novo na mesma sprint.
- `Assets/Animation/Heros/Cleric/Cleric.controller` (novo) — Idle/Walk/Damage/Prayer/Heal diagonais, **Attack em Blend Tree de 8 pontos** (arte tem as 8 direções, não só 4).
- `Assets/Animation/Heros/Cleric/Attack/ClericProjectile.controller` (novo) — Fly/Impact.
- `Assets/Animation/Heros/Cleric/Shift/ShiftEffect.controller` (novo) — HealStatusCleric/Empty.
- `Assets/Prefabs/Heros/Cleric/Cleric.prefab`, `ClericProjectile.prefab` (novos).
- `docs/gdd/index.md` — Seção 17.6: Oração (raio, não global; tick = metade do dano), Reza (diagonal, 4 ondas, flash de status).
- `Assets/Scenes/_TestScene.unity` — Cleric adicionado ao registro do `MainMenuUI`.

## Eventos adicionados

Nenhum `GameEvents` novo — reaproveita `HealthChanged`/`EnemyKilled` já existentes.

## Testes executados

Só manual (Play Mode), muito mais iterativo que as sprints anteriores — vários bugs de prefab achados só depois do usuário montar tudo e testar de verdade: Primário (homing, perfuração, exceção sem-alvo), Ultimate (paralisia + DoT + prioridade visual do WordOfPain), Reza (4 ondas de cura, flash do status Heal), projétil (impacto, rotação). Sem suíte automatizada.

## Bugs conhecidos / corrigidos durante a sprint

- **`ClericProjectile.prefab` sem Rigidbody2D** — mesmo bug do `RogueBomb` (Sprint 22): sem ele, o Collider2D Trigger não gera evento nenhum, o projétil atravessava os monstros reto.
- **`ClericProjectile.controller` sem parâmetro `ImpactTrigger` nem transição `fly → impact`** — o código chamava `SetTrigger("ImpactTrigger")` mas não existia nenhum jeito de sair do estado `fly`; o projétil nunca se destruía.
- **`Cleric.prefab` com Layer `Default`/Tag `Untagged`** — sem a tag `Player`, nenhum monstro detecta o herói (mesma classe de bug já vista com o `Systems` mistagueado, pós-Sprint 22).
- **`enemyLayerMask` vazio (`m_Bits: 0`)** — Primário e Oração nunca achavam nenhum monstro.
- **`StatusEffectController.statusAnimator` caiu sozinho 4 vezes** — a Editor resalvando o prefab por cima da edição direta no arquivo sempre que ele estava aberto (mesmo problema já visto no Rogue e no Druid, mas o pior caso até agora — caiu de novo até durante o tempo de resposta de uma pergunta). Resolvido religando repetidamente até o usuário confirmar que fechou o prefab no Editor antes da última edição.
- **`ShiftEffect.controller` com 2 estados "Empty" duplicados** — resultado direto do bug acima: eu editei o controller enquanto o usuário também editava pela Editor, e os dois salvamentos não se fundiram. Limpo mantendo só o estado feito pelo usuário.

## Dívida técnica

- **Valores `🔢` de balanceamento**: `attackRadius`, `prayerRadius`/`prayerDuration`/`prayerTickDamageMultiplier`, `healPercentOfMaxHealth`/`healFlashDuration`, velocidade/alcance/knockback do `ClericProjectile`.
- **`projectileCount` é um teste explícito, não uma feature fechada** — o usuário quer validar em jogo se vale a pena ter mais de 1 projétil antes de desenhar a carta de upgrade de verdade.
- **Frames exatos dos Animation Events são estimativas visuais** (ponto de lançamento do projétil, pico da explosão, ondas de cura) — calculados a partir de sample rate/contagem de frame, não confirmados contra o clipe rodando. Nenhum ajuste foi pedido até agora, mas vale conferir com calma numa passada futura.
- **Risco recorrente de "Editor resalva por cima"** registrado formalmente aqui: toda vez que um campo de Inspector precisa ser religado via edição direta de arquivo, checar se o prefab está aberto no Editor antes — já aconteceu 3x nesta sprint (Rogue e Druid tiveram 1x cada em sprints anteriores).

## Próximos passos

Cleric completo e testado — Deadline 6 segue com Boss Framework, Boss Timer e os bosses de Floor 1-2. `StatusEffectController.FlashStatus()`/`IsEffectActive()` ficam prontos pra qualquer Efeito futuro que precise de piscada instantânea ou de ser consultado por fora (incapacitação, principalmente) — não precisa redescobrir nenhum dos dois padrões.
