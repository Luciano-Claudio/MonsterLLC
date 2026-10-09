# Sprint 28 — Gunslinger Completo (Primário + Ultimate + Shift + Passiva)

## Objetivo

Gunslinger Gameplay Complete: rajada hitscan em leque imprecisa (primário), giro disparando nas 8 direções fixas (ultimate), chicote em 4 triggers cardeais (Shift) e loot em dobro (passiva) — primeiro herói do projeto a usar hitscan em vez de projétil instanciado.

## Sistemas adicionados

- **Rajada hitscan (primário)** — `Gunslinger.cs`: raycast reto, sem perfuração, de 1 a 15 tiros por rajada (`shotCount`, upgradable comum — sem Tier de Arma externo). Com 1 tiro só, sai sempre reto; com mais de 1, cada tiro sorteia seu próprio desvio angular (`RandomShotDeviation()`), magnitude entre `MinShotDeviationAngle` (0.1°, nunca perfeitamente reto) e `shotMaxDeviationAngle` (teto ajustável), sinal esquerda/direita sorteado 50/50 — independente entre os tiros da mesma rajada.
- **Variações de animação por `shotCount`** — 120 clipes gerados (`attack_{direção}_{N}.anim`, 8 direções × 15 contagens): a mesma dupla de frames "gatilho + disparo" é duplicada N vezes entre a pose de mira inicial e o recuo final, com `AnimationShotFireEvent` embutido 1x por repetição. Animator Controller ganhou os estados `Attack_2`...`Attack_15` (cada um com o próprio Blend Tree de 8 direções) e o parâmetro int `ShotCount`, roteado via `AnyState` (`AttackTrigger` + `ShotCount == N`).
- **Guarda de disparo por frame** (`lastShotFireFrame`) — como o Blend Tree 2D mistura 2 clipes com peso > 0 ao mesmo tempo (mesmo bug de sempre), e aqui o MESMO evento precisa disparar várias vezes por clipe (1 por tiro da rajada), o guard não podia ser o bool "já disparei" de outros heróis — trava por `Time.frameCount`, aceitando no máximo 1 disparo por frame.
- **Ultimate — giro nas 8 direções, N tiros por direção** — `UseUltimate()` dispara 8 Animation Events fixos (`AnimationShoot_N/NE/E/SE/S/SW/W/NW`, direção no MUNDO, não a mira), cada um soltando `ultimateShotsPerDirection` tiros instantâneos (upgradable, default 15) com o mesmo `RandomShotDeviation()` do primário — pedido explícito do usuário pra "fingir" que a arma da Ultimate é mais forte sem precisar desenhar mais frames de animação.
- **Chicote (Shift)** — mesmo mecanismo de 4 triggers cardeais simultâneos do shield bash do Paladin (`whipHitboxN/S/E/W`, dano + knockback), mas SEM a imunidade a dano que o Paladin tem — decisão explícita, dois mecanismos iguais sem herdar efeito colateral não pedido.
- **Passiva — loot em dobro** — `HeroController.LootMultiplier` (novo campo estático, mesmo critério do `IsPlayerUntargetable`), lido em `EnemyController.AnimationDieEndEvent()` multiplicando a quantidade de loot dropado por qualquer monstro (não só os do Gunslinger). Default 1 = sem efeito em nenhum outro herói.
- **`GunslingerImpactVfx`** — VFX de impacto puramente cosmético (sem dano, sem collider — o dano já foi aplicado pelo raycast antes de instanciar), 1 animação só, se destrói no próprio Animation Event de fim (`AnimationImpactEndEvent`) com timeout de segurança.
- **Gizmo de debug das linhas de tiro** — `showShotLinesGizmo` (Inspector), desenha ao vivo (recalculando a cada repaint da Scene view) as linhas do primário (vermelho) e da Ultimate (amarelo), usando a mesma fórmula real de desvio e o mesmo `hitscanMaxRange` — criado a pedido do usuário pra calibrar `shotMaxDeviationAngle` visualmente antes de fechar o valor.

## Decisões técnicas

- **Hitscan via `Physics2D.Raycast` com `ContactFilter2D` + buffer, não o overload de 1 resultado só.** Todo monstro tem, no MESMO layer "Enemy", tanto o próprio corpo (`EnemyController`, tag "Enemy") quanto os próprios hitboxes de ataque (`AttackHitbox_N/S/E/W/...`, Untagged, sem `EnemyController`) — ver `Goblin.prefab`. Com o monstro em melee, essas hitboxes ficam bem na frente dele, no caminho do raycast. O Raycast de 1 resultado só parava ali — sem dano — mas o `hit.point` ainda produzia um VFX de impacto perto do monstro, "parecendo" que tinha acertado (bug relatado em teste). Fix: pegar TODOS os colliders no caminho (já ordenados por distância) e escolher o primeiro marcado com a tag `"Enemy"` — mesmo critério de `EnemyController.CollectDistinct()`. `ContactFilter2D.useTriggers` precisou ser forçado pra `true` explicitamente (um filtro novo nasce com isso `false`, diferente do `Physics2D.queriesHitTriggers` global, que é só o default do Raycast de 1 resultado).
- **`RawAimDirection`, não `AimDirection`, pro raycast real.** Primeira versão do primário usava `AimDirection` (mira travada nas 8 direções, só pra escolher a pose no Animator) pra calcular a direção do tiro — o raycast só acertava quando o monstro estava exatamente alinhado a um dos 8 ângulos fixos, errando "no range certo" sempre que a mira real estivesse num ângulo intermediário (bug relatado em teste). Corrigido pra `RawAimDirection` (mesmo critério já usado pelo voo da flecha do Ranger) — `AimDirection`/`IsOrthogonalAim` seguem existindo só pra decidir a pose.
- **120 clipes de Attack gerados por script, não à mão** — duplicar manualmente 2 frames entre 1 e 14 vezes em 8 direções seria um trabalho mecânico gigante e propenso a erro; um script Python (via Bash, não comitado) leu os 8 clipes originais, extraiu as 7 sprites de cada um e gerou as 112 variações faltantes (`attack_{dir}_{N}.anim`, N=2..15) com os tempos recalculados pro fps pedido (16) e os eventos posicionados na fórmula `fire_indices = {4, 6, 8, ..., 2N+2}`, `end_index = 2N+4` — validada contra o caso N=1 (original) e o exemplo do próprio usuário (N=2 → eventos nos frames 5 e 7).
- **Animator Controller do Gunslinger inteiramente escrito à mão** (fileIDs sequenciais simples, mesmo padrão já usado no Paladin) — incluindo o estado `Trapped` (ausente no Paladin, dívida dele, não copiada aqui) reaproveitando a BlendTree do Walk como placeholder, mesmo critério que o Ranger usava antes da Sprint 27 (sem arte dedicada ainda).
- **`ultimateShotsPerDirection` como campo upgradable, não constante** — pedido explícito do usuário depois de eu inicialmente sugerir um número fixo de 15; todo valor de balanceamento deste projeto é ajustável no Inspector, sem exceção.
- **Correção de bug próprio na criação do prefab**: `StatusEffectController.statusAnimator` tinha sido ligado ao componente errado (`Light2D` em vez do `Animator` do GameObject "Status") desde a criação do prefab — por isso o ícone/efeito do Fire nunca aparecia. Achado e corrigido depois de teste manual do usuário.

## Arquivos/classes principais

- `Assets/Scripts/Player/Heroes/Gunslinger.cs` (novo).
- `Assets/Scripts/Player/GunslingerImpactVfx.cs` (novo).
- `Assets/Scripts/Player/Heroes/HeroController.cs` — `public static float LootMultiplier = 1f;` (novo).
- `Assets/Scripts/Enemies/EnemyController.cs` — `AnimationDieEndEvent()` multiplica a quantidade de loot por `HeroController.LootMultiplier`.
- `Assets/Prefabs/Heros/Gunslinger/Gunslinger.prefab`, `GunslingerImpactVfx.prefab` (novos).
- `Assets/Animation/Heros/Gunslinger/Gunslinger.controller` (novo — Idle/Walk/Damage/Die/Trapped/Attack_1..15/Ultimate/Shift) + `Impact/GunslingerImpactVfx.controller` (novo).
- `Assets/Animation/Heros/Gunslinger/*.anim` — 32 clipes gerados pelo `HeroAnimationGeneratorWindow` a partir das sprites do usuário + 112 variações de Attack geradas por script + 1 clipe de impacto em branco (`Impact/impact.anim`, preenchido pelo usuário depois).

## Eventos adicionados

Nenhum em `GameEvents` — toda a comunicação desta sprint é local (Animation Events do próprio herói) ou um campo estático simples (`HeroController.LootMultiplier`, mesmo critério do `IsPlayerUntargetable` já existente).

## Testes executados

Validação manual em Play Mode pelo usuário, iterativa, com 3 rodadas de bugfix real encontradas em teste:
1. Raycast acertando às vezes, às vezes não, mesmo com o monstro no alcance certo — causa raiz: `AimDirection` travada nas 8 direções em vez de `RawAimDirection` — corrigido.
2. Raycast não acertando NUNCA, mesmo com o monstro em melee e o VFX de impacto nascendo nos pés dele — causa raiz: hitbox de ataque do próprio monstro (Untagged, mesmo layer) bloqueando o primeiro resultado do raycast — corrigido com multi-hit + filtro de tag.
3. Status do Fire não aparecendo visualmente — causa raiz: `statusAnimator` apontando pro componente errado desde a criação do prefab — corrigido.

Confirmado funcionando pelo usuário: primário (rajada 1-15 com desvio calibrado via Gizmo), Ultimate (8 direções, N tiros por direção), status do Fire. Passiva de loot e chicote do Shift **não foram re-testados explicitamente** nesta sprint (ver Dívida técnica).

## Bugs conhecidos

Nenhum pendente do que foi testado.

## Dívida técnica

- `stats` (HP/dano/velocidade/etc.) e o `CapsuleCollider2D` seguem com valores placeholder (copiados do Ranger como base) — sem calibração de balance real.
- Passiva de loot em dobro (`LootMultiplier`) implementada mas não confirmada em teste manual nesta sprint.
- Chicote (Shift) não foi re-testado depois das últimas correções (nenhuma delas tocou nesse código, mas não houve confirmação explícita).
- Sem arte dedicada de `Trapped` — reaproveita a BlendTree do Walk, mesmo placeholder que o Ranger usava antes da Sprint 27.
- `docs/gdd/balance-values.md` segue desatualizado (dívida já registrada desde a Sprint 27b, sem mudança de status aqui).
- `impactVfxPrefab` é único e genérico — a arte de impacto direcional da Ultimate (`DS_Projectile_Impact_N/NE/E/.../NW`, já presente em `Assets/Sprites/Heros/Gunslinger/Ultimate/`) não foi usada ainda; o VFX de impacto atual não gira por direção.

## Próximos passos

Gunslinger Gameplay Complete — Deadline 7 segue para a Sprint 29 (Assassin), dependência inalterada (nenhum problema cross-cutting apareceu nesta sprint que justificasse uma "28b", diferente da 27/27b).
