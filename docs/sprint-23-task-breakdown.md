# Sprint 23 — Cleric (Completo)

**Deadline 6 — Heróis II + Bosses (Floor 1-2).** Dependência: Sprint 22 (Rogue, fechada).

Escopo oficial (GDD Seção 17.6): projétil homing que persegue o monstro mais próximo, só usável com 1+ monstro no raio de ataque; Oração (ultimate) paralisa + aplica DoT em todos os monstros do Floor simultaneamente; cura vira ativa via Shift (reza, 4 direções cardeais); passiva dobra o dano do Cleric contra monstros.

---

## Seção 0 — Decisões e suposições (leia antes de revisar o código)

Esse kit tem mais superfície nova do que Druid/Rogue — nenhum herói até agora precisou de "todos os monstros do Floor" nem de incapacitar um monstro. As mudanças de base aqui são maiores que as das 2 sprints anteriores (que só mexiam no `HeroController`); esta mexe também no `EnemyController` e no `FloorPopulationManager`.

1. **`FloorPopulationManager.cs` — nova exposição pública** (`AliveEnemies`). A Oração precisa afetar todos os monstros do Floor atual, não só num raio. O Druid já deixou documentado no próprio código que "não existe registro/enumerador de monstros no projeto" (por isso as vinhas usam `OverlapCircleAll`) — mas um raio não cobre "o Floor inteiro". O `FloorPopulationManager` já mantém essa lista internamente (`aliveEnemies`) por Floor; só faltava expor.

2. **`EnemyController.cs` — nova incapacitação** (`isParalyzed`/`SetParalyzed()`). Espelha o `isTrapped`/`SetTrapped()` que o herói já tem, do lado do monstro, que não tinha nada equivalente até agora. `TakeDamage()` continua funcionando normalmente enquanto paralisado (é chamado de fora, não passa pelo `Update()`) — é assim que o DoT da própria Oração consegue continuar acertando um monstro que ela mesma paralisou. **Suposição:** paralisia afeta todo monstro igual, inclusive bosses (futuro) — GDD diz "todos os monstros em campo", sem exceção; não adicionei nenhuma trava tipo `isBoss` (que hoje só impede knockback, nada mais). Se bosses devem ser imunes a isso quando existirem (Sprint 24+), é um ajuste de uma linha depois.

3. **`DirectionUtility.SnapTo4Cardinals` recriada.** Foi criada e removida na própria Sprint 22 (ficou sem uso depois que a Cambalhota do Rogue virou diagonal, não cardeal). Agora tem uso real de novo: a Reza (Shift) do Cleric é a única ação direcional do projeto com pose **cardeal** (N/E/S/W), não diagonal — GDD é explícito nisso. **Não tenho o arquivo `DirectionUtility.cs` em mãos** (nunca foi pedido nas sprints anteriores) — a implementação abaixo é inferida pelo padrão de `SnapTo8Directions`/`SnapTo4Diagonals` (que eu conheço só pela assinatura de uso, não pelo corpo), não verificada contra o arquivo real. Se a assinatura ou a classe divergir, é só ajustar — a lógica (maior componente decide o eixo, sinal decide o lado) é a parte que importa.

4. **`ClericProjectile.cs` — classe nova, não reaproveita `HeroProjectile.cs`.** O `HeroProjectile.cs` que o Ranger usa (visto na Sprint 21) troca de pose via 8 estados fixos do Animator (`animator.Play(DirectionUtility.GetDirectionName(direction))`), não rotaciona o sprite. Mas a GDD do Cleric diz explicitamente "usa rotação real do sprite continuamente, **mesma técnica descoberta na flecha do Ranger**" — ou a flecha real do Ranger não usa o `HeroProjectile.cs` genérico (tem uma classe própria que eu não tenho), ou o arquivo que tenho está desatualizado. Construí `ClericProjectile.cs` do zero com rotação contínua (mesma técnica do `EnemyProjectile`/`RogueBomb`, que já rotacionam), mantendo o comportamento de reserva-de-dano-com-perfuração do `HeroProjectile` (acerta, gasta só o mínimo da reserva, continua se sobrar). **Se existir uma classe de projétil do Ranger com rotação que eu deveria ter estendido em vez de duplicar, me avisa** — troco depois sem problema, a lógica de homing fica isolada o bastante pra portar.

5. **Alvo perdido durante o voo (monstro morre/destruído antes do projétil chegar)** — GDD não cobre esse caso. Tratei como o mesmo desfecho de "acertou com reserva sobrando": para de perseguir, continua reto na última direção conhecida até o alcance máximo. Sem isso, o projétil ficaria perseguindo uma referência nula e travaria.

6. **`ClericPrayerEffect.cs` — efeito bespoke por Animation Event, não usa `StatusEffectController`.** A GDD é específica: "ganham um GameObject de efeito próprio sobre a cabeça... sofre dano por segundo (via Animation Event do próprio efeito)" — isso é visivelmente diferente do sistema de Efeitos Nocivos existente (Fire/Bleeding, com tick por código). Respeitei a GDD à risca: um prefab novo, instanciado como filho de cada monstro, cujo próprio clipe (em loop) dispara os ticks de dano via Animation Event, não um timer em código.

7. **Suposição de hierarquia:** `FloorPopulationManager` está no **mesmo GameObject** que o `FloorDefinition` correspondente (`currentFloor.GetComponent<FloorPopulationManager>()`). Não tenho a Scene pra confirmar — se forem objetos irmãos/separados, é só trocar o lookup (ex.: um array/dicionário em `FloorRegistry`, ou uma referência cruzada), sem mudar nada da lógica da Oração em si.

8. **Direção da Reza resolvida pela mira** (`RawAimDirection` → `SnapTo4Cardinals`), mesmo critério de toda ação direcional do projeto (golpe do Barbarian, vinhas do Druid, Cambalhota do Rogue) — GDD não diz explicitamente de onde vem a direção, só que existem "4 direções (N/E/S/W)".

9. **Passiva implementada como multiplicador local no Cleric** (`ClericDamageMultiplier = 2f`), aplicado em cada ponto onde o Cleric causa dano (hit do projétil e tick da Oração) — diferente da passiva do Rogue (que precisou de um hook na base porque mexia num fluxo compartilhado, `HandleEnemyKilled`). Aqui não precisa de nada na base: nenhum outro herói tem esse conceito, e o dano do Cleric só é aplicado dentro do próprio `Cleric.cs`/`ClericProjectile.cs`/`ClericPrayerEffect.cs`.

10. **Pergunta em aberto, não resolvi sozinho:** o gate de cooldown do primário, na base (`HeroController.Update()`), já **consome** o cooldown (`attackCooldown.TryConsume()`) ANTES de chamar `PrimaryAttack()`. Como a exceção do Cleric ("sem monstro no raio, não acontece nada") só decide DENTRO de `PrimaryAttack()`, o cooldown já foi gasto mesmo quando não havia alvo — ou seja, clicar sem monstro por perto ainda "trava" o próximo ataque de verdade pelo tempo do cooldown, mesmo sem nada ter acontecido na tela. GDD não fala sobre isso. Duas opções:
    - **(a)** Aceitar assim — é o comportamento mais simples, zero mudança na base, e o jogador só percebe se ficar clicando no vazio e depois um monstro aparecer.
    - **(b)** Dar um jeito do cooldown não ser gasto em "miss" — exigiria mexer no gate da base (`HeroController.Update()`) ou no `AttackCooldown` (que eu não tenho o código) pra permitir um "devolver" o cooldown, mecanismo que não existe hoje e que nenhum outro herói precisa.
    
    Fui com **(a)** no código abaixo por ser zero-risco pros outros 9 heróis. Avisa se (b) for importante pra sensação do Cleric — não é difícil, só precisa decidir o mecanismo certo antes de mexer numa base que todo herói compartilha.

---

## Mudanças em arquivos existentes

### `HeroController.cs`
**Nenhuma mudança.** Diferente de Druid/Rogue, o Cleric não precisou de hook novo na base do herói — `CanUseUltimate()` já cobre o que ele precisa (sobrescrito, ver abaixo), e a passiva/Oração/Reza são resolvidas inteiramente dentro do próprio `Cleric.cs` e dos 2 arquivos novos.

### `EnemyController.cs` (diff)

```csharp
    // Sprint 23 (Cleric) — paralisia da Ultimate (Oração). Incapacitação total do lado do
    // monstro, espelho do isTrapped/SetTrapped() do herói: sem movimento, sem atacar, até
    // SetParalyzed(false). TakeDamage() continua funcionando normalmente (chamado de fora,
    // não passa pelo Update()) — é assim que o DoT da própria Oração consegue continuar
    // acertando um monstro que ela mesma paralisou.
    private bool isParalyzed;

    public void SetParalyzed(bool paralyzed)
    {
        isParalyzed = paralyzed;
        if (paralyzed) SetMoving(false); // trava o AIPath na hora, mesmo critério de "parado de propósito"
    }
```

E em `Update()`, uma linha nova logo depois do tick de `statusEffectController` (deixa Fire/Bleeding continuarem rodando normalmente mesmo paralisado — só IA/ataque/movimento travam):

```csharp
        if (statusEffectController != null) statusEffectController.Tick(Time.deltaTime);

        if (isParalyzed) return; // incapacitado pela Oração do Cleric — IA/ataque/movimento travam, dano e Efeitos Nocivos continuam normais

        if (player == null) return;
```

### `FloorPopulationManager.cs` (diff)

```csharp
    // Sprint 23 (Cleric) — exposição pública da lista de monstros vivos deste Floor. A
    // Ultimate do Cleric (Oração) precisa afetar TODOS os monstros do Floor atual
    // simultaneamente — não existia nenhum jeito de enumerar isso de fora (ver Seção 0 do
    // breakdown da sprint).
    public IReadOnlyList<GameObject> AliveEnemies => aliveEnemies;
```
(precisa de `using System.Collections.Generic;` — já deve estar no arquivo, já que `aliveEnemies` é `List<GameObject>`.)

### `DirectionUtility.cs` (adição — ver Seção 0, item 3, não verificada contra o arquivo real)

```csharp
    // Resolve a direção crua em 1 dos 4 cardeais puros (N/S/L/O) — nunca diagonal. Recriada
    // nesta sprint (removida na Sprint 22 por ficar sem uso) — agora tem uso real: a Reza
    // (Shift) do Cleric é a única pose cardeal do projeto, GDD Seção 17.6, exceção ao padrão
    // diagonal do resto do jogo.
    public static Vector2 SnapTo4Cardinals(Vector2 input)
    {
        if (input.sqrMagnitude < 0.0001f) return Vector2.down;
        return Mathf.Abs(input.x) > Mathf.Abs(input.y)
            ? new Vector2(Mathf.Sign(input.x), 0f)
            : new Vector2(0f, Mathf.Sign(input.y));
    }
```

---

## `Cleric.cs` (completo)

```csharp
using UnityEngine;

public class Cleric : HeroController
{
    // ---------- Ataque primário — Projétil Homing (GDD Seção 17.6) ----------
    [Header("Ataque primário — Projétil Homing")]
    [SerializeField] private GameObject projectilePrefab; // precisa ter ClericProjectile
    [SerializeField] private float attackRadius = 8f; // 🔢 GDD: "raio de ataque do Cleric" — conceito novo, nenhum outro herói tem
    [SerializeField] private LayerMask enemyLayerMask; // configurar no Inspector = layer dos monstros
    private EnemyController pendingTarget;

    // Rede de segurança genérica — mesmo padrão do resto do elenco.
    [SerializeField] private float maxActionDuration = 3f; // 🔢 ajustável
    private float actionElapsed;

    // ---------- Ultimate — Oração (GDD Seção 17.6) ----------
    [Header("Ultimate — Oração")]
    [SerializeField] private GameObject prayerEffectPrefab; // precisa ter ClericPrayerEffect
    [SerializeField] private float prayerDuration = 4f; // 🔢 GDD: "alguns segundos, ajustável"
    [SerializeField] private float prayerTickDamage = 3f; // 🔢 ajustável — já SEM o multiplicador da passiva, aplicado abaixo
    [SerializeField] private float prayerTickInterval = 1f; // 🔢 só documenta a cadência esperada do clipe — o tick real vem do Animation Event do efeito, não de um timer aqui
    [SerializeField] private float prayerEffectHeightOffset = 0.5f; // 🔢 "sobre a cabeça" — ajuste fino

    // ---------- Habilidade Secundária (Shift) — Reza / Cura Ativa (GDD Seção 16/17.6) ----------
    [Header("Habilidade Secundária (Shift) — Cura Ativa")]
    [SerializeField] private float healPercentOfMaxHealth = 0.25f; // 🔢 GDD: "% da própria Vida Máxima, ajustável"

    // ---------- Passiva (GDD Seção 17.6) ----------
    // "Monstros sofrem 2× de dano do Cleric" — aplicado em todo ponto onde o Cleric causa
    // dano (hit do projétil e tick da Oração), não na base (nenhum outro herói usa isso).
    private const float ClericDamageMultiplier = 2f;

    protected override void Update()
    {
        if (!GameplayGate.IsActive) return;
        base.Update();

        if (isAttacking)
        {
            actionElapsed += Time.deltaTime;
            if (actionElapsed >= maxActionDuration)
            {
                Debug.LogWarning("[Cleric] Animation Event de fim de ação nunca chegou — forçando fim (verifique o Animator Controller).");
                isAttacking = false;
            }
        }
    }

    // ===================== PRIMÁRIO — PROJÉTIL HOMING =====================

    // GDD: "única exceção do MVP a 'todo herói sempre pode tentar atacar'" — sem monstro no
    // raio, o clique simplesmente não faz nada, nem toca animação (ver Seção 0, item 10,
    // sobre o cooldown já ter sido consumido pela base antes de chegar aqui).
    protected override void PrimaryAttack()
    {
        if (isAttacking) return;

        var target = FindNearestEnemyInRadius();
        if (target == null) return; // exceção do MVP — sem alvo, não acontece nada

        pendingTarget = target;
        isAttacking = true;
        actionElapsed = 0f;
        AnimatorTrigger("AttackTrigger");
    }

    private EnemyController FindNearestEnemyInRadius()
    {
        var hits = Physics2D.OverlapCircleAll(transform.position, attackRadius, enemyLayerMask);
        EnemyController nearest = null;
        float nearestDistSqr = float.MaxValue;
        foreach (var hit in hits)
        {
            var enemy = hit.GetComponent<EnemyController>();
            if (enemy == null || HealthSystem.IsDead(enemy.stats.health)) continue;

            float distSqr = ((Vector2)enemy.transform.position - (Vector2)transform.position).sqrMagnitude;
            if (distSqr < nearestDistSqr)
            {
                nearestDistSqr = distSqr;
                nearest = enemy;
            }
        }
        return nearest;
    }

    // Animation Event, no frame exato em que o projétil é lançado.
    public void AnimationProjectileLaunchEvent()
    {
        if (projectilePrefab == null || pendingTarget == null) { isAttacking = false; return; }

        var projObj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        var proj = projObj.GetComponent<ClericProjectile>();
        if (proj != null) proj.Launch(pendingTarget, stats.damage * ClericDamageMultiplier, enemyLayerMask);
        pendingTarget = null;
    }

    // Animation Event, no fim do clipe de lançamento — o projétil já está voando sozinho
    // (GameObject independente), o Cleric recupera o controle na hora (mesmo padrão da bomba
    // do Rogue).
    public void AnimationProjectileEndEvent()
    {
        isAttacking = false;
    }

    // ===================== ULTIMATE — ORAÇÃO =====================

    protected override bool CanUseUltimate() => !isAttacking;

    protected override void UseUltimate()
    {
        isAttacking = true;
        actionElapsed = 0f;
        AnimatorTrigger("PrayerTrigger");
    }

    // Animation Event, no frame exato em que a Oração "dispara" — efeito GLOBAL (GDD: "todos
    // os monstros em campo, não só ao redor do Cleric"), usa o Floor inteiro via
    // FloorPopulationManager.AliveEnemies (Seção 0, item 1), não um raio.
    public void AnimationPrayerCastEvent()
    {
        var currentFloor = FloorManager.Instance.CurrentFloor;
        if (currentFloor == null) return;

        var populationManager = currentFloor.GetComponent<FloorPopulationManager>();
        if (populationManager == null) return; // ver Seção 0, item 7 (suposição de hierarquia)

        foreach (var enemyObj in populationManager.AliveEnemies)
        {
            if (enemyObj == null) continue;
            var enemy = enemyObj.GetComponent<EnemyController>();
            if (enemy == null || HealthSystem.IsDead(enemy.stats.health)) continue;

            SpawnPrayerEffectOn(enemy);
        }
    }

    private void SpawnPrayerEffectOn(EnemyController enemy)
    {
        if (prayerEffectPrefab == null) return;

        Vector3 spawnPos = enemy.transform.position + new Vector3(0f, prayerEffectHeightOffset, 0f);
        var effectObj = Instantiate(prayerEffectPrefab, spawnPos, Quaternion.identity, enemy.transform);
        var effect = effectObj.GetComponent<ClericPrayerEffect>();
        if (effect != null) effect.Initialize(enemy, prayerTickDamage * ClericDamageMultiplier, prayerTickInterval, prayerDuration);
    }

    // Animation Event, no fim do clipe da Oração — o efeito por monstro já está rodando
    // sozinho (ClericPrayerEffect, um GameObject por alvo), o Cleric recupera o controle na
    // hora, independente de quanto tempo a paralisia/DoT ainda tem pra durar.
    public void AnimationPrayerEndEvent()
    {
        isAttacking = false;
    }

    // ===================== SECUNDÁRIA (SHIFT) — REZA / CURA ATIVA =====================

    protected override void UseSecondaryAbility()
    {
        isAttacking = true; // bloqueia primário/ultimate durante a reza inteira
        actionElapsed = 0f;

        // GDD: "4 direções (N/E/S/W)" — cardeal, não diagonal (exceção ao padrão do resto do
        // projeto). Direção resolvida pela mira (Seção 0, item 8). RawAimDirection já está
        // fresco aqui: TryUseSecondaryAbility() (base) força a releitura antes de chamar este método.
        Vector2 prayDirection = DirectionUtility.SnapTo4Cardinals(RawAimDirection);
        if (animator != null)
        {
            animator.SetFloat("PrayDirX", prayDirection.x);
            animator.SetFloat("PrayDirY", prayDirection.y);
        }

        AnimatorTrigger("HealTrigger");
    }

    // Animation Event, no frame exato em que a reza "conecta" a cura — mesmo efeito de cura
    // que já existia antes (GDD, Sprint 18→19: % da própria Vida Máxima), só que agora
    // acionado pelo jogador via Shift em vez de automático a cada 10s.
    public void AnimationHealEvent()
    {
        stats.health = Mathf.Min(stats.maxHealth, stats.health + stats.maxHealth * healPercentOfMaxHealth);
        GameEvents.HealthChanged(stats.health, stats.maxHealth);
    }

    // Animation Event, no fim do clipe de reza. GDD: "Não cancelável" — CancelSecondaryAbility()
    // default (no-op, herdado sem override) já cobre isso.
    public void AnimationHealEndEvent()
    {
        isAttacking = false;
        isUsingSecondaryAbility = false; // arma o cooldown no próximo Update() da base
    }
}
```

---

## `ClericProjectile.cs` (novo)

```csharp
using UnityEngine;

// Projétil do ataque primário do Cleric — persegue o monstro-alvo (Homing) até acertá-lo ou
// perdê-lo; ao acertar com reserva de dano sobrando, PARA de perseguir e continua reto na
// última direção (GDD Seção 17.6), podendo ainda perfurar outros monstros no caminho (mesmo
// comportamento de reserva-por-hit do HeroProjectile do Ranger). Rotação contínua do sprite
// (mesma técnica da flecha do Ranger segundo a GDD — ver Seção 0, item 4, sobre não ter
// reaproveitado o HeroProjectile.cs diretamente).
public class ClericProjectile : MonoBehaviour
{
    [SerializeField] private float speed = 7f; // 🔢 ajustável — GDD: "upgrades da loja aumentam a velocidade"
    [SerializeField] private float maxDistance = 12f; // 🔢 ajustável
    [SerializeField] private float knockbackForce = 2f; // 🔢 ajustável — Cleric não é um herói de empurrão forte

    private EnemyController homingTarget;
    private Vector2 direction;
    private float damageReserve;
    private LayerMask enemyLayerMask;
    private float distanceTraveled;
    private bool isHoming = true;

    public void Launch(EnemyController target, float reserve, LayerMask layerMask)
    {
        homingTarget = target;
        damageReserve = reserve;
        enemyLayerMask = layerMask;
        direction = ((Vector2)target.transform.position - (Vector2)transform.position).normalized;
        ApplyRotation();
    }

    private void Update()
    {
        if (!GameplayGate.IsActive) return;

        // Alvo morreu/destruído antes de alcançar — GDD não cobre, tratado como o mesmo
        // desfecho de "acertou com reserva sobrando" (Seção 0, item 5).
        if (isHoming && homingTarget == null) isHoming = false;

        if (isHoming)
        {
            direction = ((Vector2)homingTarget.transform.position - (Vector2)transform.position).normalized;
            ApplyRotation();
        }

        float step = speed * Time.deltaTime;
        transform.position += (Vector3)(direction * step);
        distanceTraveled += step;
        if (distanceTraveled >= maxDistance) Destroy(gameObject);
    }

    private void ApplyRotation()
    {
        // Mesmo ajuste do EnemyProjectile/RogueBomb — sprite de referência aponta pra "cima".
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var enemy = other.GetComponent<EnemyController>();
        if (enemy == null) return;

        float damageDealt = Mathf.Min(damageReserve, enemy.stats.health);
        enemy.TakeDamage(damageDealt);
        enemy.ApplyKnockback(direction, knockbackForce);

        damageReserve -= damageDealt;
        if (damageReserve <= 0f) { Destroy(gameObject); return; }

        isHoming = false; // reserva sobrando — para de perseguir, segue reto (GDD)
        if (enemy == homingTarget) homingTarget = null;
    }
}
```

---

## `ClericPrayerEffect.cs` (novo)

```csharp
using UnityEngine;

// Efeito da Ultimate do Cleric (Oração) — nasce sobre a cabeça de UM monstro (filho dele na
// hierarquia), paralisa esse monstro e aplica dano por segundo via Animation Event do próprio
// efeito (GDD Seção 17.6 — não um timer em código, ver Seção 0 item 6 do breakdown).
public class ClericPrayerEffect : MonoBehaviour
{
    private EnemyController target;
    private float tickDamage;
    private float duration;
    private float elapsed;
    private bool ended;

    public void Initialize(EnemyController enemyTarget, float dotTickDamage, float tickInterval, float effectDuration)
    {
        target = enemyTarget;
        tickDamage = dotTickDamage;
        duration = effectDuration;
        target.SetParalyzed(true);
        // tickInterval não é consumido em código — a cadência real dos ticks vem do Animation
        // Event do próprio clipe (GDD). Guardado só como documentação da intenção pro Editor.
    }

    private void Update()
    {
        if (!GameplayGate.IsActive) return;
        if (ended) return;

        // Alvo morreu/destruído durante o efeito — encerra na hora, sem esperar o Animation
        // Event de fim (não há mais ninguém pra paralisar ou aplicar dano).
        if (target == null) { EndEffect(); return; }

        // Rede de segurança — se o Animation Event de fim nunca chegar, força depois da
        // duração nominal + folga (mesmo padrão de maxDieDuration/owlDuration).
        elapsed += Time.deltaTime;
        if (elapsed >= duration + 1f) // 🔢 folga de segurança
        {
            Debug.LogWarning("[ClericPrayerEffect] AnimationPrayerEffectEndEvent nunca chegou — forçando fim (verifique o Animator Controller).");
            EndEffect();
        }
    }

    // Animation Event, a cada tick do clipe em loop (ex.: 1x por segundo — GDD: "sofre dano
    // por segundo via Animation Event do próprio efeito").
    public void AnimationPrayerTickEvent()
    {
        if (target == null) return;
        target.TakeDamage(tickDamage);
    }

    // Animation Event, no fim real do clipe (duração = effectDuration configurada).
    public void AnimationPrayerEffectEndEvent()
    {
        EndEffect();
    }

    private void EndEffect()
    {
        if (ended) return;
        ended = true;
        if (target != null) target.SetParalyzed(false);
        Destroy(gameObject);
    }
}
```

---

## Setup necessário no Animator Controller (Editor, fora de código)

- **Cleric.controller — Attack:** novo estado, `AttackTrigger`. Não precisa de Blend Tree direcional (o projétil decide a própria direção via rotação, não o Animator) — pode ser uma pose única ou um Blend Tree simples por `AimX/AimY` se a arte tiver variação por direção do lançamento. Wire `AnimationProjectileLaunchEvent` no frame do lançamento e `AnimationProjectileEndEvent` no último frame.
- **Cleric.controller — Ultimate (Oração):** novo estado, `PrayerTrigger`. Wire `AnimationPrayerCastEvent` no frame em que a oração "dispara" e `AnimationPrayerEndEvent` no último frame.
- **Cleric.controller — Heal (Reza, Shift):** novo estado, Blend Tree por `PrayDirX/PrayDirY` com só 4 poses (N/E/S/W — **cardeal**, diferente de todo outro Blend Tree do projeto, que é diagonal). Wire `AnimationHealEvent` no frame em que a cura "conecta" e `AnimationHealEndEvent` no último frame.
- **Prefab `ClericProjectile`:** precisa de `Rigidbody2D` (Kinematic) + `Collider2D` (Is Trigger) pra gerar `OnTriggerEnter2D` — lição da Sprint 22 (a bomba do Rogue não funcionava sem isso).
- **Prefab `ClericPrayerEffect`:** Animator com um clipe em loop de duração = `prayerDuration` (🔢, default 4s no código), com `AnimationPrayerTickEvent` repetido a cada `prayerTickInterval` (🔢, default 1s) e `AnimationPrayerEffectEndEvent` no frame final.

## Checklist de teste manual

1. Sem monstro no raio de ataque, clicar o primário não faz nada (sem animação, sem projétil).
2. Com 1+ monstro no raio, o projétil nasce e persegue o mais próximo, girando o sprite continuamente em voo.
3. Projétil que acerta o alvo com reserva esgotada é destruído na hora.
4. Projétil que acerta o alvo com reserva sobrando para de perseguir e continua reto, podendo perfurar um segundo monstro no caminho.
5. Alvo morre antes do projétil chegar — projétil não trava nem dá erro, continua reto na última direção conhecida.
6. Oração: todos os monstros do Floor atual (não só os perto do Cleric) ganham o efeito, inclusive um monstro bem longe/fora de tela.
7. Monstro paralisado não persegue, não ataca, não anda — mas continua tomando os ticks de dano da própria Oração.
8. Monstro com Fire/Bleeding ativo continua recebendo o DoT desses Efeitos Nocivos normalmente mesmo paralisado.
9. Monstro paralisado que morre durante a Oração não trava o `ClericPrayerEffect` (destruído corretamente, sem erro de referência nula).
10. `CanUseUltimate()` bloqueia a Oração durante o Primário e durante a Reza.
11. Reza: pose trava numa das 4 direções cardeais (nunca diagonal) e cura a % configurada da Vida Máxima, sem exceder o teto.
12. Reza não é cancelável — apertar Shift de novo no meio dela não faz nada.
13. Passiva: dano do projétil E dano de cada tick da Oração saem exatamente 2× o valor base configurado (comparar com `prayerTickDamage`/dano do projétil "cru").
14. Morrer no meio de qualquer uma das 3 ações não deixa o Cleric travado após o respawn.
15. (Checklist de produção) Confirmar que `FloorPopulationManager` está mesmo no mesmo GameObject que `FloorDefinition` na Scene real — se não estiver, o `GetComponent` em `AnimationPrayerCastEvent` falha silenciosamente (a Oração não erra, só não acerta ninguém). Ver Seção 0, item 7.
