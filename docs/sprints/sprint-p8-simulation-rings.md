# Sprint P8 — Simulation Rings

## Objetivo

Reduzir o custo de simulação de monstro por distância do jogador, sem desligar — anel próximo simula em full (igual sempre foi), anel médio reduz a frequência da decisão de IA e desliga o RVOController. Implementado como preparação, sem problema de FPS medido (decisão explícita do usuário: "quero implementar de qualquer forma").

A outra metade do P8 oficial (Limpeza de Combat Scope — `FloorActivationCheck` substituído, ~20 arquivos migrados) já tinha sido feita antes, na limpeza do sistema de Floor (ver Sprint P10) — esta sprint fecha o P8 por completo.

## Sistemas adicionados

- **`EnemyController.simulationNearRingRadius`** (padrão 15) — dentro desse raio do jogador, simulação completa, todo frame, igual sempre foi.
- **Fora do raio** (mas ainda vivo — despawn por distância continua sendo `WorldPopulationManager`/`WorldBossSpawnManager`, não muda): `RVOController` desligado todo frame (checagem barata, sem throttle) + a decisão de IA (patrulha/combate/ataque) passa a rodar num intervalo reduzido (`midRingUpdateInterval`, padrão 0,3s) em vez de todo frame — nunca desligada, só menos frequente. `AIPath` continua se movendo sozinho (Update/FixedUpdate próprios) em direção ao último destino setado entre os ticks reduzidos.

## Decisões técnicas

- **Throttle da DECISÃO, não do `Update()` inteiro** — dano/knockback/DoT (`UpdateDamageFlash`/`UpdateKnockback`/`StatusEffectController.Tick`) continuam em tempo real todo frame, mesmo longe, pra não alterar quanto dano um DoT aplica por segundo real nem atrasar o feedback visual de ter sido atingido.
- **Tempo ACUMULADO na decisão throttled, não `Time.deltaTime` puro** — achado real: simplesmente chamar a decisão com menos frequência quebraria cooldowns/sub-IAs internos (`AttackCooldown`, `PatrolAI`, `flankAI` do Melee) — eles andariam em câmera lenta longe do jogador (ex.: Spider Queen atacando a cada ~20s reais em vez de ~1,5s) em vez de "imperceptível". Corrigido passando `midRingAccumulator` pra toda a cadeia de decisão — exigiu trocar `protected abstract void Move()` por `Move(float deltaTime)` e atualizar as 6 implementações (Melee/Ranged/GoblinSapper/RatPeopleRoyalty/Slime/SpiderQueen).
- **Animator Culling Mode mais barato por distância NÃO implementado** — deixado de fora de propósito (menor impacto, mais uma configuração de asset por prefab do que lógica; o próprio renderer já não desenha monstro fora de tela a essa distância).

## Arquivos/classes principais

- `Assets/Scripts/Enemies/EnemyController.cs` — campos novos, `Update()` reestruturado, `Move(float)` abstrato.
- `Assets/Scripts/Enemies/MeleeEnemyController.cs`, `RangedEnemyController.cs`, `GoblinSapperController.cs`, `RatPeopleRoyaltyController.cs`, `SlimeEnemyController.cs`, `SpiderQueenController.cs` — assinatura `Move(float deltaTime)` + `Time.deltaTime` interno trocado pelo parâmetro.

## Eventos adicionados

Nenhum.

## Testes executados

Manual (Play Mode) — monstro parado/patrulhando longe continuando a reagir normal ao se aproximar (sem "congelar no tempo"), cooldown de ataque de monstros híbridos (Spider Queen/Rat People Royalty) não demorando anormalmente pro primeiro golpe ao se aproximar depois de ficar longe.

## Bugs conhecidos

Nenhum em aberto.

## Dívida técnica

- **Fallback sem AIPath (`MoveInDirection`, prefab placeholder não migrado) usa `Time.deltaTime` puro, não o deltaTime acumulado** — documentado, não corrigido: caminho sem uso real hoje (todo monstro real já tem AIPath), encadear mais uma mudança de assinatura (`MoveInDirection`) por um caso sem uso real não valia o risco.

## Próximos passos

Spawn de monstro/boss priorizando posições fora da visão da câmera (P9 oficial) + checklist de Full Migration Test (P11).
