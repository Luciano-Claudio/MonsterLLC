# Sprint 21 — Druid Completo (Primário + Ultimate + Shift)

## Objetivo

Escopo original (`docs/sprint-21-task-breakdown.md`, batendo com o plano de produção): "Druid — Primário + Floor 3A–4A", com Ultimate/Shift ficando pra Sprint 22. Logo no início da sprint, decisão explícita do usuário: implementar as 3 ações do Druid juntas (Primário, Ultimate, Shift), já que compartilham estado profundamente acoplado (`isAttacking`, `isUsingSecondaryAbility`, `TakeDamage`, o próprio Animator) — mesmo precedente das Sprints 17/18b/19b/20, sprint real maior que a sprint planejada. No meio do caminho, o usuário decidiu reestruturar o roadmap a partir da Deadline 6 (prompt separado enviado pra outra conversa, que cuida do `Projeto_Torre_Plano_Producao_v7.md`) — nesse plano novo, Floor 3A–4A não é mais construído cedo, migra pra dentro de uma Deadline 8 dedicada só à Torre (todos os Floors 3-10 de uma vez, qualidade final). Resultado: **Sprint 21 fecha só com o Druid**, sem nenhum Floor novo — não é escopo cortado, é escopo que já tem lar certo mais à frente.

## Sistemas adicionados

- **`Druid.cs`** (novo) — os 3 kits do herói:
  - **Primário (vinhas):** padrão *Summoned Target Hit* (GDD Seção 13/17.4) — dois Animation Events em dois objetos diferentes, não um hit instantâneo. `AnimationVineSummonEvent` (no Druid) escolhe os N monstros vivos mais próximos e distintos, instancia uma vinha em cada; `AnimationVineHitEvent` (na própria `Vine`) causa o dano de verdade, na hora que a animação dela "aperta" o alvo — cobre de graça "acertar mais de 1 monstro se estiverem muito próximos".
  - **Ultimate (Alce):** troca completa de `runtimeAnimatorController` (não sub-state-machine — ver Decisões técnicas), imunidade a dano durante as 2 animações de transição, cura pra 100% do HP do Alce ao entrar, conversão proporcional de HP ao sair (exceto morte durante a transformação, que pula a conversão e seica volta direto pro fluxo universal de morte), collider físico com forma própria (offset/size ajustáveis, independentes do humano).
  - **Secundária (Coruja):** bloqueia só ataque (movimento livre via gate por nome de estado do Animator, não por `isAttacking`), imune a dano a viagem inteira, sem colisão física com monstros (reposicionamento de verdade), mutuamente exclusiva com o Alce.
- **`Vine.cs`** (novo) — prefab autônomo: recebe dano/layer via `Launch()`, aplica o hit no próprio Animation Event, some sozinho (Animation Event de fim + timeout de segurança), gizmo de debug pro raio de hit.
- **`Druid.controller` + `Druid_Elk.controller`** (novos, 2 Animator Controllers separados) + o controller da Vinha — Blend Trees 4-diagonais em todo Idle/Walk/Dmg/Attack, sub-state machine da Coruja dentro do controller humano.
- **Hooks novos em `HeroController.cs`** (todos default no-op/`false`/`true`, sem efeito em quem não sobrescreve): `CanUseUltimate()`, `IsDamageImmune`, `IsUltimateActive`/`CancelUltimate()`, `MoveInput` (exposição read-only), espelhamento automático do collider físico por `DiagonalAimDirection` (`AutoFlipBodyCollider`).
- **`AttackCooldown.Start()`** (novo, separado de `TryConsume()`) — desacopla "checar se está pronto" de "começar a contar o cooldown", usado pro cooldown da Habilidade Secundária de todo herói.

## Decisões técnicas

- **Alce como `RuntimeAnimatorController` separado, não sub-state-machine** — cogitado fazer como a Coruja (tudo num controller só), mas o Alce precisa reaproveitar os nomes `AttackTrigger`/`DamageTrigger` (a garra/dano dele), e "Any State" no Unity é global por layer — um trigger disparado dentro do Alce competiria com a transição equivalente do humano. Controller separado dá isolamento total de graça; o custo é manter os nomes de parâmetro em sincronia entre os 2 arquivos (documentado, não automatizado).
- **Coruja usa arte diagonal, não cardeal** — GDD previa 4 direções cardeais (N/E/S/W) pra fase `during`; a arte entregue é diagonal, igual todo o resto do Bestiário/heróis. Decisão explícita do usuário: adaptar o código pra arte real (reaproveitar `DirectionUtility.SnapTo4Diagonals`) em vez de pedir arte nova. GDD Seção 17.4 corrigida pra refletir isso.
- **Collider físico nunca espelhava certo — causa raiz era `SpriteRenderer.flipX`, que nunca muda em herói nenhum** — investigação a fundo (grep em centenas de clipes, `m_FloatCurves: []` vazio em todos, nenhum código seta `flipX`) confirmou que a arte de todo herói usa clipes dedicados por diagonal, nunca espelhamento via `flipX`. O sinal real de "olhando pra E ou W" é `DiagonalAimDirection.x` (o mesmo que já alimenta o Blend Tree). Correção movida pra `HeroController.cs` (mecanismo automático, `AutoFlipBodyCollider`) — beneficia Barbarian/Ranger/Mage também, não só o Druid, sem precisar de componente extra nos prefabs deles (removi o `FlippedColliderOffsetX` que tinha colocado neles por engano antes de achar a causa raiz).
- **Corrida de Trigger no Animator matando monstro em 2-3 hits** — `DamageTrigger` de um hit não-letal ainda não consumido quando `DieTrigger` era setado em seguida fazia o Unity processar só o primeiro, descartando o segundo — monstro ficava preso em `IdleCombat` pra sempre, só resgatado pelo timeout de segurança. Corrigido com `animator.ResetTrigger("DamageTrigger")` antes do `DieTrigger` em `EnemyController.Die()`. Não é bug do Druid — afeta qualquer herói que mate em múltiplos hits (por isso nunca apareceu com Barbarian/Ranger, que geralmente matam num golpe só nos testes).
- **`HeroController.OnDeath()` reordenado** — `OnHeroDeath()` (hook que o Druid usa pra restaurar o controller humano) passou a rodar **antes** de `AnimatorTrigger("DieTrigger")`, não depois. Sem isso, morrer como Alce disparava o Trigger no controller errado (Alce, sem estado Die na hora), a troca de controller logo em seguida resetava o Trigger já "gasto", e nenhuma animação de morte tocava.
- **Dano de vinha duplicado em monstro agrupado — resolvido com `HashSet` compartilhado, não cooldown por tempo** — proposta inicial do usuário foi um cooldown de milissegundos; trocado por uma referência `HashSet<EnemyController>` compartilhada entre todas as vinhas nascidas na mesma ativação (passada via `Launch()`), determinística, sem depender de timing (2 vinhas podem disparar no mesmo frame). Uma vinha ainda pode acertar vários monstros próximos sozinha (GDD) — só não pode haver dano de 2 vinhas diferentes no mesmo monstro.
- **`CanUseUltimate()` — falha estrutural que existia em todo herói, não só no Druid** — `TryUseUltimate()` (base) sempre gastava a energia depois de chamar `UseUltimate()`, mesmo quando o `UseUltimate()` do herói dava "return" cedo por dentro (`isAttacking`, ou `isUsingSecondaryAbility` do Druid durante a Coruja) — o "return" interno era invisível pra quem já tinha decidido gastar a energia antes. Corrigido com um hook checado ANTES do gasto; Barbarian/Ranger/Mage também tinham o mesmo `if (isAttacking) return;` interno agora redundante, removido dos 4.
- **`IsDamageImmune` consolidado na base, não replicado por herói** — Druid (Alce em transição + Coruja inteira), Ranger (camuflagem inteira) e Mage (teleporte inteiro) reusam o mesmo hook; Barbarian não sobrescreve (o buff dele não esconde/transforma, continua tomando dano normal). Cobre um caso que `IsPlayerUntargetable` sozinho não cobria: dano de contato (Slime) não é "escolha de alvo" do monstro, bate por estar encostado fisicamente.
- **Reposicionamento da Coruja via `Physics2D.IgnoreLayerCollision`, não `collider.enabled = false`** — primeira tentativa desligava o collider inteiro, o que também tirava colisão com parede/escada (jogador saía do mapa). Corrigido pra desligar só a colisão entre as layers Player×Enemy (a matriz de colisão do projeto está toda aberta hoje, então isso não afeta nada mais) — preserva escada/parede, remove só o empurra-empurra com monstro.
- **Cooldown da Habilidade Secundária vira "tempo de uso", não "tempo desde o clique"** — `TryConsume()` armava o timer no instante do clique, deixando o cooldown correr por baixo dos panos enquanto o jogador ainda estava transformado/escondido. Separado em `IsReady` (checagem, sem efeito colateral) + `Start()` (arma o timer), disparado pelo `HeroController.Update()` no frame exato em que `isUsingSecondaryAbility` vira `false` de verdade — funciona igual pra todo herói (Owl, camuflagem, teleporte, buff do Barbarian).
- **Morte do Alce reaproveita os frames do `die` humano por enquanto** (`elk_die.anim`, cópia literal, GUID próprio) — pedido explícito do usuário: quer arte própria de morte do Alce no futuro, sem precisar mexer em Animator/código de novo quando ela chegar, só trocar o conteúdo desse arquivo.

## Arquivos/classes principais

- `Assets/Scripts/Player/Heroes/HeroController.cs` — `CanUseUltimate()`, `IsDamageImmune`, `MoveInput`, `AutoFlipBodyCollider`/`ApplyBodyColliderFlip()`, `TakeDamage`/`AnimationDieEndEvent` virtuais, reordenação de `OnDeath()`, detecção de fim de Habilidade Secundária pro cooldown.
- `Assets/Scripts/Player/Heroes/Druid.cs` (novo) — os 3 kits completos.
- `Assets/Scripts/Player/Heroes/Vine.cs` (novo).
- `Assets/Scripts/Player/Heroes/{Barbarian,Ranger,Mage}.cs` — `CanUseUltimate()`/`IsDamageImmune` onde se aplica, remoção dos guards internos redundantes.
- `Assets/Scripts/Enemies/EnemyController.cs` — `ResetTrigger("DamageTrigger")` em `Die()`.
- `Assets/Scripts/Core/Combat/AttackCooldown.cs` — `Start()`.
- `Assets/Animation/Heros/Druid/Druid.controller`, `Ultimate/Druid_Elk.controller`, `Vine/*.controller` (novos) + 45 clipes de animação (24 Animation Events wireados) + `Ultimate/elk_die.anim`.
- `Assets/Prefabs/Heros/Druid/Druid.prefab`, `Vine.prefab` (novos).
- `docs/gdd/index.md` — Seção 17.4 corrigida (Coruja é diagonal, não cardeal).

## Eventos adicionados

Nenhum `GameEvents` novo — reaproveita `HealthChanged`/`EnergyChanged`/`DamageTaken`/`EnemyKilled` já existentes.

## Testes executados

Só manual (Play Mode), iterativo — sem suíte automatizada nesta sprint. Cobertura confirmada pelo usuário: vinhas (seleção de alvo, dano em área, anti-duplicação), ciclo completo do Alce (transformação, garra, morte durante a forma), ciclo completo da Coruja (movimento livre, imunidade, reposicionamento, cooldown), interação com Bestiário (monstro morrendo em múltiplos hits). Regressão explícita em Barbarian/Ranger/Mage depois das mudanças na base (`CanUseUltimate`, `IsDamageImmune`, espelhamento de collider) — sem bugs encontrados.

## Bugs conhecidos / corrigidos durante a sprint

- Vinha mirando monstro já morto (collider podia continuar ativo até o próprio `die` terminar) — trava por `HealthSystem.IsDead`.
- Vinha duplicando dano em monstro agrupado — `HashSet` compartilhado.
- Monstro preso em `IdleCombat` ao morrer em 2-3 hits — corrida de Trigger, `ResetTrigger` no `Die()`.
- Energia da Ultimate gasta sem transformar (Coruja ativa) — `CanUseUltimate()`.
- Projétil de monstro "errando" o Alce — causa raiz era o collider físico nunca espelhando (mito do `flipX`), corrigido na base.
- `StatusEffectController.statusAnimator` do Druid ficou vazio 2x (sobrescrito ao salvar o prefab pela Editor por cima da edição direta no arquivo) — religado, e identificado como risco geral: editar arquivo direto e a Editor ter o mesmo asset aberto ao mesmo tempo pode perder a edição.
- Dano de contato (Slime) contando durante Coruja/camuflagem/teleporte — `IsDamageImmune`.
- Coruja empurrando/travando em monstro mesmo imune a dano — `Physics2D.IgnoreLayerCollision`.
- Cooldown da Habilidade Secundária correndo durante a própria habilidade — `AttackCooldown.Start()` separado do clique.

## Dívida técnica

- **`elk_die.anim` é cópia literal do `die` humano** — placeholder assumido, usuário já sinalizou que quer arte própria de morte do Alce depois; troca é só no arquivo, sem mexer em Animator/código.
- **Valores `🔢` de balanceamento**, todos placeholder de primeiro teste: `elkMaxHealthMultiplier`, `elkDamageMultiplier`, `elkMoveSpeedMultiplier` (setado propositalmente em 0.8 — Alce mais lento, decisão explícita do usuário, não é engano), `elkFormMaxDuration`, `owlDuration`, `vineCount`, `vineSearchRadius`, `elkKnockbackForce`, tamanho dos 4 hitboxes da garra do Alce.
- **Nome do controller da Vinha ficou o auto-gerado do Unity** (`Minifantasy_TrueHeroesDruidRootAttack_0.controller`) — cosmético, sem efeito funcional.
- **Reestruturação de Deadline 6+ pendente de aplicação** — prompt já escrito e entregue ao usuário pra outra conversa (a que gerencia `Projeto_Torre_Plano_Producao_v7.md`); este documento assume que Sprint 21 = só Druid, mas a tabela de Sprints/Deadlines do plano de produção ainda não foi atualizada nesta sessão — fica pra quem aplicar aquele prompt.

## Próximos passos

Druid completo (Primário/Ultimate/Shift), testado, sem Floor novo pendente. Deadline 6 continua com Rogue, Cleric, Boss Framework, Boss Timer e os bosses de Floor 1/2 (escopo novo, pendente de refletir no plano de produção). O padrão de correção de collider (`AutoFlipBodyCollider`/`DiagonalAimDirection`) e os hooks novos da base (`CanUseUltimate`, `IsDamageImmune`) já ficam prontos pra qualquer herói futuro que precise de Ultimate cancelável ou de uma fase "escondido/imune" — não precisa redescobrir nenhum dos dois.
