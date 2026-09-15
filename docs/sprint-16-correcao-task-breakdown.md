# Sprint 16 (Correção) — Task Breakdown — Enemy Framework Pivot + Patrol AI

> Continuação da Sprint 16 original. A Sprint 16 já rodou o teste de viabilidade (Rat/Goblin/Rat People com Animator real, `AnimationHitEvent`/`AnimationAttackEndEvent`) e a contingência foi acionada — isso está documentado em `docs/sprints/sprint-16-task-breakdown.md` (o teste em si) e no GDD v1.01237 Seção 22. Esta sprint de correção é o trabalho que restou depois da decisão: o código dos 3 monstros de teste ainda está na arquitetura antiga, o Attack Budget virou código morto ainda referenciado, e a IA de patrulha (`idle`/`walk` aleatório + `idle_combat`) nunca chegou a ser implementada — não é ajuste, é feature nova.

## Objetivo

Rat, Goblin e Rat People rodando de ponta a ponta no modelo definitivo do GDD Seção 22: patrulha aleatória → detecção com atraso de reação → combate por contato/auto-disparo com cooldown próprio → morte só se completa quando a animação `die` termina. Código morto do Attack Budget removido do projeto.

## Dependência

Sprint 15 (Floor Sleep v1) — concluída. Nenhuma dependência nova.

## Correções aplicadas após revisão técnica

Revisão contra o código já commitado encontrou 4 divergências reais (não estilo) entre este breakdown e o projeto real, mais 1 decisão de arquitetura confirmada. Todas aplicadas na versão abaixo:

1. **Timeout de morte não pode usar Coroutine.** `WaitForSeconds` não respeita `GameplayGate` — o projeto pausa por flag manual, não por `Time.timeScale` (regra estabelecida desde a Sprint 13, a mesma que motivou a máquina de estado manual no `Update()` do `EnemyController` original). Um Coroutine de timeout continuaria contando com o jogo pausado. Trocado por um timer manual (`dieElapsed`) incrementado dentro do próprio `Update()` já gateado — e, por consistência com o Floor Sleep (Sprint 15), também gateado por `ownerFloor`/`FloorActivationCheck`: um monstro morrendo num Floor dormindo não deve ter o timeout correndo enquanto o jogador está em outro Floor.
2. **Método renomeado para `AnimationDieEndEvent()`**, seguindo a convenção já commitada de `AnimationHitEvent`/`AnimationAttackEndEvent`, em vez do `OnDieAnimationEnd()` deste breakdown.
3. **Chamadas ao `Animator` centralizadas em `SetAnimBool`/`SetAnimTrigger`**, com guard explícito (`animator != null`, não `?.` — o operador de null-conditional do C# não pega "fake-null" de `UnityEngine.Object` destruído). Centralizar num único ponto evita esquecer o guard num call site novo no futuro.
4. **Dano via `HealthSystem` estático**, sobre `stats.health` — `HealthSystem.ApplyDamage(stats.health, amount)` / `HealthSystem.IsDead(stats.health)` — não mais `health.ApplyDamage(...)`/`health.IsDead` como se `health` fosse uma instância com métodos próprios.
5. **Campos de configuração movidos pra `EnemyStats`** (`observationRadius`, `attackCooldownDuration`, `contactRange`/`minAttackRange`/`maxAttackRange`, `moveSpeed`, durações de patrulha) em vez de `[SerializeField]` soltos nos Controllers — mesmo lugar de onde os dados do Bestiário já vêm hoje. Sem razão pra preferir campos soltos; a única ressalva é que este breakdown assume `EnemyStats` unificado (Melee e Ranged compartilhando a mesma classe, com campos que um dos dois não usa) — se `EnemyStats` já for especializado por subtipo no projeto real, ajuste a estrutura de herança, a ideia (dados no Bestiário-object, não no Controller) continua valendo.

O constante de segurança do timeout (`dieSafetyTimeoutDuration`) ficou fora do `EnemyStats` — não é dado de balanceamento por criatura, é constante de engenharia, então continua como campo simples no `EnemyController`.

---

## S16C-T01 — `AttackCooldown` (lógica pura, testável)

Antes de tocar em qualquer `MonoBehaviour`, isola a regra "posso atacar agora?" numa classe pura — mesmo padrão já estabelecido no projeto (`HealthSystem`, `EnergySystem`, `DemandCalculator`).

```csharp
// Assets/Scripts/Core/Combat/AttackCooldown.cs
namespace Core.Combat
{
    public class AttackCooldown
    {
        private readonly float cooldownDuration;
        private float timeRemaining;

        public AttackCooldown(float cooldownDuration)
        {
            this.cooldownDuration = cooldownDuration;
            timeRemaining = 0f;
        }

        public bool IsReady => timeRemaining <= 0f;

        public void Tick(float deltaTime)
        {
            if (timeRemaining > 0f)
                timeRemaining -= deltaTime;
        }

        /// Consome o cooldown se estiver pronto. Retorna false sem efeito colateral se não estiver.
        public bool TryConsume()
        {
            if (!IsReady) return false;
            timeRemaining = cooldownDuration;
            return true;
        }
    }
}
```

**Teste automatizado (EditMode) — `AttackCooldownTests`:**
- `NewCooldown_IsReadyImmediately`
- `TryConsume_WhenReady_ReturnsTrueAndStartsCooldown`
- `TryConsume_WhileOnCooldown_ReturnsFalse`
- `Tick_AfterFullDuration_BecomesReadyAgain`

---

## S16C-T02 — `PatrolAI` (lógica pura, testável) — idle/walk aleatório com reação atrasada

Isola a regra mais delicada desta sprint: o monstro nunca interrompe o `idle` de patrulha no meio, mesmo detectando o jogador — a reação só acontece quando a fase atual termina naturalmente.

```csharp
// Assets/Scripts/Core/AI/PatrolAI.cs
using UnityEngine;

namespace Core.AI
{
    public enum PatrolPhase { Idle, Walking }

    public class PatrolAI
    {
        private readonly float minIdleDuration, maxIdleDuration;
        private readonly float minWalkDuration, maxWalkDuration;
        private readonly System.Random rng;

        public PatrolPhase CurrentPhase { get; private set; }
        public Vector2 WalkTarget { get; private set; }
        private float phaseTimer;
        private bool combatTransitionPending;

        public bool ShouldEnterCombat { get; private set; }

        public PatrolAI(float minIdle, float maxIdle, float minWalk, float maxWalk, System.Random rng = null)
        {
            minIdleDuration = minIdle; maxIdleDuration = maxIdle;
            minWalkDuration = minWalk; maxWalkDuration = maxWalk;
            this.rng = rng ?? new System.Random();
            CurrentPhase = PatrolPhase.Idle;
            phaseTimer = NextDuration(minIdleDuration, maxIdleDuration);
        }

        /// Chamado assim que o observationRadius detecta o jogador. NÃO troca de fase na hora —
        /// só marca a intenção. A fase atual (idle ou walk) sempre termina primeiro.
        public void RequestCombatTransition() => combatTransitionPending = true;

        public void Tick(float deltaTime, System.Func<Vector2> pickRandomWalkTarget)
        {
            if (ShouldEnterCombat) return; // já entrou em combate, PatrolAI para de rodar

            phaseTimer -= deltaTime;
            if (phaseTimer > 0f) return;

            // fase atual terminou de verdade — só agora a detecção pendente pode valer
            if (combatTransitionPending)
            {
                ShouldEnterCombat = true;
                return;
            }

            // sem detecção pendente: continua o ciclo normal idle <-> walk
            if (CurrentPhase == PatrolPhase.Idle)
            {
                CurrentPhase = PatrolPhase.Walking;
                WalkTarget = pickRandomWalkTarget();
                phaseTimer = NextDuration(minWalkDuration, maxWalkDuration);
            }
            else
            {
                CurrentPhase = PatrolPhase.Idle;
                phaseTimer = NextDuration(minIdleDuration, maxIdleDuration);
            }
        }

        private float NextDuration(float min, float max) => min + (float)rng.NextDouble() * (max - min);
    }
}
```

**Teste automatizado (EditMode) — `PatrolAITests`:**
- `RequestCombatTransition_DuringIdle_DoesNotEnterCombatImmediately`
- `RequestCombatTransition_DuringIdle_EntersCombatOnlyAfterPhaseTimerCompletes`
- `WithoutCombatTransition_AlternatesIdleAndWalkingIndefinitely`
- `PhaseDurations_AlwaysWithinConfiguredRange` (rodar N vezes, checar min/max)

Isso cobre a regra sem precisar de Play Mode — a ligação com o clipe real do Animator (Exit Time) é responsabilidade do Controller (T05), não desta classe.

---

## S16C-T03 — Reescrever `EnemyController` — remover a arquitetura antiga

Em `Assets/Scripts/Enemies/EnemyController.cs`, remover:
- O enum de estado de ataque (`Idle/Telegraph/Active/Recovery`) e o campo `AttackTrigger`.
- `lockedTargetPosition` (não se aplica mais — sem telegraph, não há posição pra travar).
- Toda chamada a `AttackBudgetManager`/`AttackType` (reserva/liberação de slot).

Adicionar:

```csharp
// Assets/Scripts/Enemies/EnemyController.cs (trechos relevantes)
using Core.Combat;
using Core.AI;

public abstract class EnemyController : MonoBehaviour
{
    [SerializeField] protected EnemyStats stats; // observationRadius, attackCooldownDuration, contactRange/minAttackRange/maxAttackRange, moveSpeed, patrolRadius, duração de idle/walk — tudo que antes era campo solto agora vive aqui, mesmo lugar de onde os dados do Bestiário já vêm
    [SerializeField] private float dieSafetyTimeoutDuration = 3f; // constante de engenharia, não dado de balanceamento — fica fora do EnemyStats

    protected PatrolAI patrolAI;
    protected AttackCooldown attackCooldown;
    protected bool isInCombat;
    protected bool isDead;
    private bool hasFinishedDying;
    private float dieElapsed;

    private Vector2 spawnOrigin;
    protected Transform player;

    protected virtual void Awake()
    {
        spawnOrigin = transform.position;
        patrolAI = new PatrolAI(stats.minIdleDuration, stats.maxIdleDuration, stats.minWalkDuration, stats.maxWalkDuration);
        attackCooldown = new AttackCooldown(stats.attackCooldownDuration);
        player = GameObject.FindGameObjectWithTag("Player")?.transform; // mesmo padrão já usado no projeto
    }

    protected virtual void Update()
    {
        if (!GameplayGate.IsActive) return;
        if (ownerFloor != null && !FloorActivationCheck.IsActive(ownerFloor, FloorManager.Instance.CurrentFloor)) return;

        if (isDead)
        {
            UpdateDeathSafetyTimeout();
            return;
        }

        attackCooldown.Tick(Time.deltaTime);

        if (!isInCombat)
            UpdatePatrol();
        else
            UpdateCombat();
    }

    private void UpdatePatrol()
    {
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        if (distanceToPlayer <= stats.observationRadius)
            patrolAI.RequestCombatTransition();

        patrolAI.Tick(Time.deltaTime, () => spawnOrigin + Random.insideUnitCircle * stats.patrolRadius);

        if (patrolAI.ShouldEnterCombat)
        {
            isInCombat = true;
            return;
        }

        bool moving = patrolAI.CurrentPhase == PatrolPhase.Walking;
        SetAnimBool(AnimIsMoving, moving);
        SetAnimBool(AnimInCombat, false);
        if (moving)
            MoveTowards(patrolAI.WalkTarget);
    }

    private void UpdateCombat()
    {
        SetAnimBool(AnimInCombat, true);
        Move(); // implementado por Melee/Ranged — aproximar até contato, ou manter alcance
        TryAttack(); // implementado por Melee/Ranged — contato/auto-disparo via attackCooldown
    }

    protected abstract void Move();
    protected abstract void TryAttack();

    // Guard centralizado — animator != null explícito, não `?.` (não pega "fake-null" de UnityEngine.Object destruído).
    protected void SetAnimBool(int hash, bool value)
    {
        if (animator != null) animator.SetBool(hash, value);
    }

    protected void SetAnimTrigger(int hash)
    {
        if (animator != null) animator.SetTrigger(hash);
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;
        HealthSystem.ApplyDamage(stats.health, amount);
        SetAnimTrigger(AnimDamageTrigger);
        if (HealthSystem.IsDead(stats.health)) Die();
    }

    protected virtual void Die()
    {
        isDead = true;
        dieElapsed = 0f;
        GetComponent<Collider2D>().enabled = false;
        SetAnimTrigger(AnimDieTrigger);
    }

    private void UpdateDeathSafetyTimeout()
    {
        dieElapsed += Time.deltaTime;
        if (dieElapsed >= dieSafetyTimeoutDuration)
        {
            Debug.LogWarning($"{name}: Animation Event de morte não disparou em {dieSafetyTimeoutDuration}s — destruindo via timeout de segurança.");
            AnimationDieEndEvent();
        }
    }

    // Chamado pelo Animation Event no último frame do clipe `die` — mesmo padrão de AnimationHitEvent/AnimationAttackEndEvent.
    public void AnimationDieEndEvent()
    {
        if (hasFinishedDying) return; // guarda contra dupla chamada (Animation Event e timeout de segurança podem colidir no mesmo frame)
        hasFinishedDying = true;
        DropLoot();
        Destroy(gameObject);
    }

    protected abstract void DropLoot();
}
```

**Notas de implementação:**
- `Move()`/`TryAttack()` continuam abstratos — `MeleeEnemyController` e `RangedEnemyController` implementam cada um do seu jeito (T04).
- `ownerFloor`/`FloorActivationCheck` (Sprint 15) não mudam — o gate de Floor Sleep continua igual, agora protegendo também o timer de morte, não só patrulha/combate.
- `DropLoot()` já existia antes (Sprint 13, corrigido no bugfix daquela sprint) — só muda **quando** é chamado: antes era dentro de `Die()`, agora é dentro de `AnimationDieEndEvent()`.

---

## S16C-T04 — `MeleeEnemyController`/`RangedEnemyController` simplificados

```csharp
// Assets/Scripts/Enemies/MeleeEnemyController.cs
public class MeleeEnemyController : EnemyController
{
    protected override void Move()
    {
        Vector2 direction = (player.position - transform.position).normalized;
        if (Vector2.Distance(transform.position, player.position) > stats.contactRange)
        {
            transform.Translate(direction * stats.moveSpeed * Time.deltaTime);
            SetAnimBool(AnimIsMoving, true);
        }
        else
        {
            SetAnimBool(AnimIsMoving, false); // parado, colado no player -> idle_combat
        }
    }

    protected override void TryAttack()
    {
        if (Vector2.Distance(transform.position, player.position) > stats.contactRange) return;
        if (!attackCooldown.TryConsume()) return;
        player.GetComponent<HeroController>().TakeDamage(stats.damage);
    }
}
```

```csharp
// Assets/Scripts/Enemies/RangedEnemyController.cs
public class RangedEnemyController : EnemyController
{
    [SerializeField] private GameObject projectilePrefab; // referência de prefab, não dado de balanceamento — fica fora do EnemyStats

    protected override void Move()
    {
        float distance = Vector2.Distance(transform.position, player.position);
        if (distance > stats.maxAttackRange)
        {
            MoveToward(player.position);
            SetAnimBool(AnimIsMoving, true);
        }
        else if (distance < stats.minAttackRange)
        {
            MoveAway(player.position); // mantém a distância mínima, mesma ideia do Spider Queen/Devoted Stalker
            SetAnimBool(AnimIsMoving, true);
        }
        else
        {
            SetAnimBool(AnimIsMoving, false); // dentro do range -> idle_combat
        }
    }

    protected override void TryAttack()
    {
        float distance = Vector2.Distance(transform.position, player.position);
        if (distance < stats.minAttackRange || distance > stats.maxAttackRange) return;
        if (!attackCooldown.TryConsume()) return;

        Vector2 direction = (player.position - transform.position).normalized; // fixada no instante do disparo, GDD Seção 13
        var proj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        proj.GetComponent<EnemyProjectile>().Launch(direction, stats.damage, ownerFloor);
    }
}
```

**O que sai daqui, sem substituto:** nenhuma chamada a `AttackTiming`, nenhum estado `Telegraph/Active/Recovery`, nenhum `AttackTrigger` no Animator, nenhuma reserva de budget. `EnemyProjectile` (Sprint 13/15) não muda — já respeita `GameplayGate` e `ownerFloor`, e a direção já nasce fixada no `Instantiate`.

---

## S16C-T05 — `idle_combat` e os parâmetros novos do Animator

Sem código C# adicional além dos `SetAnimBool`/`SetAnimTrigger` já usados acima. No Animator Controller (Editor, um por monstro — Rat/Goblin/Rat People nesta sprint):

1. Parâmetros: `IsMoving` (bool), `InCombat` (bool), `DamageTrigger` (trigger), `DieTrigger` (trigger) — `AttackTrigger` sai.
2. Estado novo `IdleCombat`: blend tree simples de 4 direções (reaproveita sprites já existentes de `walk`/`idle`, sem arte nova — confirmado no contexto que não há sprite de `idle_combat` produzida ainda, a intenção é reaproveitar).
3. Transições:
   - `Idle → Walk`: `IsMoving == true` **com Exit Time ativado, perto de 1.0** — regra rígida do GDD Seção 22, vale pra toda transição que sai de `Idle`, inclusive essa (ciclo normal de patrulha).
   - `Idle → IdleCombat`: `InCombat == true`, **também com Exit Time perto de 1.0** — é o caso que materializa o atraso de reação: mesmo com `InCombat` já `true` internamente (`PatrolAI.ShouldEnterCombat`), a Animator só troca quando o clipe de `Idle` termina.
   - `Walk ↔ IdleCombat`: sem Exit Time — aqui pode trocar imediatamente conforme `IsMoving`, já que não é mais o clipe `Idle` de patrulha.
   - Qualquer estado `→ Damage`: via `DamageTrigger`, sem Exit Time (dano interrompe visualmente, mas não cancela nenhuma ação real — não existe mais ação real pra cancelar nesses 3 monstros).
   - Qualquer estado `→ Die`: via `DieTrigger`, sem Exit Time, sem transição de volta.

---

## S16C-T06 — Morte orientada por Animation Event

1. No clipe `die` de cada um dos 3 Animator Controllers, adicionar um **Animation Event** no último frame chamando `AnimationDieEndEvent()` (método público já criado em `EnemyController`, T03).
2. Confirmar que o `GameObject` do Animation Event é o mesmo que tem o `EnemyController` anexado (Unity chama o método por nome no mesmo GameObject do Animator) — se o Animator estiver num filho (comum quando o sprite tem pivot/offset separado do collider), usar um pequeno relay:

```csharp
// Assets/Scripts/Enemies/AnimationEventRelay.cs — só necessário se o Animator estiver em um filho do GameObject principal
public class AnimationEventRelay : MonoBehaviour
{
    [SerializeField] private EnemyController owner;
    public void AnimationDieEndEvent() => owner.AnimationDieEndEvent();
}
```

3. Teste manual: matar o monstro e cronometrar — o `GameObject` some exatamente no fim do clipe `die`, nunca antes. Depois, **desconfigurar de propósito** o Animation Event num teste isolado (numa cópia do prefab) pra confirmar que o timeout de segurança (T03) realmente destrói sozinho e loga o aviso — assim o safety net é validado antes de confiar nele em produção.

---

## S16C-T07 — Remover o código morto do Attack Budget

**Só depois que T03/T04 estiverem funcionando** (é isso que desbloqueia — `EnemyController` deixa de chamar `AttackBudgetManager`):

1. Deletar `Assets/Scripts/Core/AttackType.cs`, `AttackBudgetTracker.cs`, `AttackBudgetManager.cs`.
2. Deletar `Assets/Scripts/UI/AttackBudgetIndicatorUI.cs` e remover o GameObject correspondente da `_TestScene` (o indicador de debug `Melee: X/Y | Ranged: X/Y`).
3. Deletar `Assets/Tests/EditMode/AttackBudgetTrackerTests.cs` (4 testes da Sprint 14) — sem isso o projeto não compila depois do passo 1.
4. Rodar o Test Runner completo depois da remoção — confirmar que a suíte cai para (35 testes anteriores − 4 removidos + testes novos desta sprint) sem nenhum erro de referência quebrada.

---

## S16C-T08 — Ajustar os 3 prefabs já montados

Para `Enemy_Rat`, `Enemy_Goblin`, `Enemy_RatPeople` (prefabs já existentes do teste da Sprint 16 original):

1. Remover o componente/campo que referenciava `AttackTiming` (se existir como `[SerializeField]` separado).
2. Ajustar os valores em `EnemyStats` de cada um (ficha do prefab ou asset, conforme já estruturado no projeto): `observationRadius`, `attackCooldownDuration` (reaproveitar os valores de cooldown que já existiam como "velocidade de ataque" nas fichas do Bestiário — Rat/Goblin/Rat People já têm Dano/Vida estimados, cooldown fica como 🔢 placeholder até balanceamento formal), `patrolRadius`, `contactRange` (Rat/Goblin) ou `minAttackRange`/`maxAttackRange` (Rat People).
3. Aplicar o Animator Controller atualizado (T05/T06) a cada prefab.
4. Rat People especificamente: confirmar que `RangedEnemyController.TryAttack()` ainda dispara o projétil de pedra (Ground Target/Impact Area, GDD Seção 13) corretamente — esse comportamento já existia, só a forma de decidir *quando* disparar mudou (cooldown puro, não mais telegraph).

---

## Testes esperados no relatório final

- **Automatizado (EditMode):** `AttackCooldownTests` (4), `PatrolAITests` (4) — novos. Suíte total = anteriores − 4 (`AttackBudgetTrackerTests` removidos) + 8 novos.
- **Manual (Play Mode):**
  1. Rat/Goblin/Rat People alternam `idle`/`walk` sozinhos antes de detectar o Barbarian, nunca cortando o clipe de `idle` no meio.
  2. Aproximar o Barbarian durante o `idle` de um deles — confirmar visualmente que ele só reage depois que o `idle` termina (atraso perceptível, não instantâneo).
  3. Em combate: Melee gruda no player e causa dano no cooldown certo; Ranged mantém distância e atira no cooldown certo; ambos tocam `idle_combat` (não `idle` de patrulha) parados entre uma ação e outra.
  4. Matar um de cada tipo — `GameObject` só some no fim do clipe `die`, loot aparece junto.
  5. Timeout de segurança validado numa cópia de teste com o Animation Event desconfigurado de propósito (ver T06).
  6. Floor Sleep (Sprint 15) continua funcionando — inimigo em patrulha/combate no Floor 1 congela exatamente onde estava ao subir pro Floor 2, mesmo teste de sempre, agora sobre a arquitetura nova.

## Fora de escopo (não fazer nesta sprint)

- Produzir o resto do Floor 1/2 (Wolf, Bat, Slime Green/Blue, Goblin Raider, Goblin Sapper) — isso é a Bestiary Batch 1/2 (Sprints 16→20 conforme o plano de produção), só depois que o framework estiver estável nos 3 monstros já existentes.
- Qualquer exceção com Animation Event real (Goblin Sapper, Orc Shaman, Burning Skull, Serpent, Bicephalous, ou as 8 exceções de boss) — nenhuma delas é Rat/Goblin/Rat People, ficam para quando o Batch/Floor correspondente chegar.
- Recalibrar Boss Framework (Deadline 6) mesmo sabendo agora que a maioria dos 30 bosses também simplificou — vale nota pro plano de produção, não é trabalho desta sprint.

## Critério de pronto

Os 3 monstros de teste rodando no modelo definitivo, zero referência a `AttackBudget*`/`AttackTiming`/`AttackTrigger` no projeto, suíte de testes verde.