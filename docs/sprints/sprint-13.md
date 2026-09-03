# Sprint 13 — Enemy Framework Genérico

## Objetivo

Generaliza `MeleeEnemyPrototype` (Sprint 8) numa base reutilizável de inimigo (`EnemyController`), com timing/telegraph de ataque real (GDD Seção 22 — Telegraph → Hitbox Ativa → Recovery, não mais dano instantâneo). Primeira sprint da Deadline 4. Fecha também o **gating de `GameState`**, dívida sem sprint atribuída desde a Sprint 11, formalmente atribuída a esta sprint após a análise da Deadline 4.

## Sistemas adicionados

- **`GameplayGate`** — gating combinado de pausa (`TimeManager`) + estado (`GameStateManager.CurrentState == GameState.Gameplay`) num único ponto, em vez de espalhar um segundo `if` em cada `Update()`.
- **`EnemyController`** (base abstrata) — máquina de estado de ataque (`Idle → Telegraph → Active → Recovery → Idle`), timing configurável via `AttackTiming`, dano real acontece no instante certo (`ExecuteHit()`), não no início do ataque.
- **`MeleeEnemyController`/`RangedEnemyController`** — implementações concretas de `Move()`/`ExecuteHit()`.
- **`EnemyProjectile`** — projétil do Ranged, respeita `GameplayGate` (pausa/estado), destruído ao atingir o Player ou expirar.

## Decisões técnicas

- **Máquina de estado manual dentro do próprio `Update()`, não Coroutine.** `IEnumerator`/`WaitForSeconds` não respeitam o `GameplayGate` — o projeto pausa via flag manual (`TimeManager.IsPaused`), não via `Time.timeScale`, então um inimigo no meio do telegraph continuaria contando tempo com o jogo pausado e completaria o golpe assim que o jogador reabrisse a Bag. A máquina de estado dentro do `Update()` já gateado resolve isso de graça.
- **Bug real corrigido antes de codar — `EnemyController.Die()` não soltava loot.** O texto original da base nova omitia inteiramente a criação do `LootDrop`/`monsterEssenceDropAmount` que o `MeleeEnemyPrototype.Die()` (Sprint 11) já tinha. Implementado literalmente, isso pararia a economia inteira (Vendor/Demanda) de receber Monster Essence, silenciosamente — sem erro de compilação, só comportamento errado. Restaurado na base, herdado por Melee e Ranged.
- **Bug real corrigido antes de codar — `Barbarian.PrimaryAttack()` ainda chamava `GetComponent<MeleeEnemyPrototype>()` diretamente.** Nunca teria detectado o `RangedEnemyController` (só checava o tipo Melee), e pararia de compilar assim que `MeleeEnemyPrototype.cs` fosse removido (passo seguinte da mesma sprint). Trocado para `GetComponent<EnemyController>()` — funciona pros dois tipos via polimorfismo, e de quebra corrige um bug que já existia desde a Sprint 8 (o Barbarian nunca conseguiria matar um inimigo Ranged, mesmo que um existisse).
- **Nota de precisão aceita, não corrigida:** `Time.time` continua avançando durante a pausa (tempo real da engine, não gateado) — `lastAttackTime` (cooldown entre ataques) pode "parecer" ter passado mais tempo do que realmente se passou em jogo se o jogador pausar bem no meio da janela de cooldown. Com cooldowns curtos e pausas tipicamente breves, o efeito é imperceptível. Já existia desde o `MeleeEnemyPrototype` original (Sprint 8), não introduzido por esta sprint.
- **Simplificação aceita, registrada como dívida:** a GDD Seção 22 especifica que o alvo (`TargetPosition`) trava no início da animação do ataque, não continua atualizando até o impacto ("preserva a sensação legítima de 'eu desviei'"). A implementação desta sprint não trava posição nenhuma — o hit do Melee não rechecha distância (sempre acerta se o telegraph completar, sem chance de esquiva), e o Ranged mira na posição atual do player no instante exato do hit, não numa posição congelada no início do telegraph. Sem sprint atribuída para corrigir — registrado aqui pra não se perder.

## Arquivos/classes principais

- `Assets/Scripts/Core/GameplayGate.cs`.
- `Assets/Scripts/Enemies/AttackTiming.cs`, `EnemyController.cs`, `MeleeEnemyController.cs`, `RangedEnemyController.cs`, `EnemyProjectile.cs`.
- `Assets/Scripts/Player/Heroes/HeroController.cs` — `IsPaused()` removido, todos os usos trocados por `GameplayGate.IsActive`.
- `Assets/Scripts/Player/Heroes/Barbarian.cs` — `PrimaryAttack()` usa `EnemyController` em vez de `MeleeEnemyPrototype`.
- `Assets/Prefabs/Test/ProjectilePrefab.prefab` — primeiro prefab do projeto.
- `Assets/Scripts/Enemies/MeleeEnemyPrototype.cs` — removido, substituído por `MeleeEnemyController`.

## Eventos adicionados

Nenhum evento novo em `GameEvents` — `OnEnemyKilled` (Sprint 7) já cobre os dois tipos de inimigo.

## Testes executados

- **Automatizado (EditMode):** nenhum teste novo — os sistemas desta sprint são todos comportamento de `MonoBehaviour` (máquina de estado ligada a `Update()`/física), sem lógica pura isolável como `HealthSystem`/`EnergySystem`/`DemandCalculator`. Suíte anterior (28 testes) segue intacta, não afetada por esta sprint.
- **Manual (Play Mode):** confirmado pelo usuário em duas rodadas de log — Melee mostra `Telegraph...` com delay perceptível antes do dano (não mais instantâneo); Ranged mantém distância e dispara projétil visível; **Barbarian mata os dois tipos** (confirma o fix da referência direta a `MeleeEnemyPrototype`); **loot dropa nos dois tipos** (confirma o fix do `Die()`); projétil do Ranged pausa junto com `TimeManager`/`GameState` (confirmado explicitamente pelo usuário); a guarda `isDead` (Sprint 11) segurou firme contra duas mortes seguidas via `MeleeEnemyController` sem duplicar `OnDeath()`/Respawn; **gating de `GameState` confirmado** — usuário testou explicitamente WASD/LMB/inimigos parando durante `Results`/`Shop`/`GameOver` e reportou os 3 cenários funcionando.
- **Regressão pega antes do teste, não durante:** ambos os bugs da seção "Decisões técnicas" foram encontrados e corrigidos na revisão do texto da sprint, antes de qualquer código ser escrito — não apareceram como falha em Play Mode.

## Bugs conhecidos

Nenhum em aberto.

## Dívida técnica

- **`TargetPosition` não trava no início do telegraph (GDD Seção 22)** — ver "Decisões técnicas". Sem sprint atribuída.
- Herdada (Sprint 12): compra na Loja/"Start Next Day" não geram save adicional (por design, GDD Seção 43); Weapon Tier stub até a Sprint 33.
- Herdada (Sprint 9): prioridade entre interagíveis simultâneos (Sprint 35).

## Próximos passos

Com Melee e Ranged genéricos rodando na mesma base e o gating de `GameState` fechado, a Sprint 14 (Attack Budget + Population Skeleton) tem uma base real de múltiplos inimigos atacando pra limitar — impossível de testar de verdade com um único inimigo hard-coded como era até esta sprint.
