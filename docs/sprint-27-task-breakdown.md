# Sprint 27 — Paladin Completo (Primário + Ultimate + Shift + Passiva)

> Baseado na GDD Seção 17.7, `HeroController.cs` (atual, pós-Sprint 23/EndRoll/ShouldConsumeCooldownOnAttack), `StatusEffectController.cs` (atual) e `Barbarian.cs` (precedente real do padrão de triggers fixos direcionais via Animation Event).

## Seção 0 — Decisões e suposições documentadas

1. **Martelo (primário) sem o arquivo real do Ranger.** A GDD pede "mesma técnica da flecha do Ranger" (rotação contínua) pro martelo do Paladin, igual já pedia pro primário do Cleric (Sprint 23) — nas duas vezes não recebi o arquivo real (`RangerArrow.cs` ou nome equivalente). Modelei `PaladinHammer.cs` no mesmo padrão que usei no `ClericProjectile.cs` (rotação contínua fixada no lançamento + reserva de dano com perfuração), só que **sem homing** (o martelo nunca redireciona, vai reto até esgotar a reserva ou o alcance). Na Sprint 23 essa mesma suposição foi confirmada certa depois, comparando com o arquivo real (relatório de fechamento: "`ClericProjectile` foi modelado nela, com a adição de homing") — não travei a sprint esperando o arquivo de novo, mas se o real for estruturalmente diferente, me avisa que eu ajusto.
2. **Ultimate (espadas orbitando) é o primeiro "Orbiting Hitbox" do projeto.** Não existe nenhum precedente real (Necromancer é Visão Expandida, nunca implementado) — desenhei do zero: um `GameObject` filho dedicado sempre presente no prefab (não instanciado em runtime), com Animator próprio (`BladesStart`/`BladesCycle`/`BladesEnd`) e 2 sub-filhos (`bladeA`/`bladeB`) cujos triggers o código reposiciona a cada frame (ângulo incrementado, 180° de diferença entre os dois) — o Animator só decide a sprite, igual todo o resto do projeto (código resolve física/posição, Animator só mostra o estado).
3. **Cada espada pode acertar o mesmo monstro mais de uma vez durante a Ultimate** (uma vez por volta que passar por cima dele) — usei `OnTriggerEnter2D` em vez do padrão `HashSet`/"hit único por ativação" das vinhas do Druid, porque aqui a arma gira continuamente por vários segundos, então acertar de novo a cada volta parece ser o comportamento esperado (perigo giratório contínuo, não um AoE de pulso único). Se a intenção era "1 hit só, nunca mais naquela ativação", é só adicionar o mesmo padrão `HashSet` do Druid.
4. **Ultimate sem knockback.** A GDD menciona knockback explicitamente pro primário (Seção 16, lista geral) e implicitamente pro shield bash ("causa dano e knockback"), mas não menciona nada pra Ultimate — segui a ficha ao pé da letra e não adicionei.
5. **Ultimate não bloqueia nada e não é cancelável** (suposição que já tinha avisado na mensagem anterior, sem correção recebida) — Paladin continua andando/atacando normalmente com as espadas girando ao redor (como um pet), sem override de `CanUseUltimate()`/`IsUltimateActive()`/`CancelUltimate()`. A duração inteira (começo → ciclo → fim) é controlada dentro do próprio `PaladinOrbitingBlades`, não no `HeroController`.
6. **Shield bash: dedup por trigger, não combinado entre os 4.** Cada um dos 4 triggers cardeais roda `Overlap()`/`CollectDistinct()` isoladamente — um monstro bem na borda entre 2 triggers (ex.: um monstro na diagonal NE, entre o trigger N e o E) pode em teoria ser atingido pelos 2 ao mesmo tempo. Decidi aceitar isso como comportamento correto (reforça a ideia de "golpe em cruz atingindo tudo ao redor"), não como bug — se quiser 1 hit garantido por monstro mesmo nas bordas, precisa de um dedup combinado entre os 4 buffers antes de aplicar dano.
7. **Shield (passivo) nasce já carregado no `Awake()`**, em vez de esperar os primeiros 30s pra ganhar o primeiro shield. A GDD descreve o ciclo recorrente (ativo → quebra → cooldown → reaparece) mas não diz explicitamente se o primeiro já existe desde o spawn — se for pra nascer sem shield, é só inverter a chamada inicial (começar em cooldown em vez de `ActivateShield()`).
8. **Shield não bloqueia Efeitos Nocivos aplicados junto com o dano** (ex.: um projétil de monstro que causa dano E aplica Fire ao mesmo tempo) — só intercepta o dano em si via `TakeDamage()` sobrescrito. `IsCurrentlyDamageImmune` (usado por fontes externas pra decidir se também aplicam o Efeito) não foi sobrescrito pro shield, porque o shield é "absorve dano", não "imune de verdade" como `isDead`/`IsDamageImmune`. Resultado possível: o Paladin pode "pegar fogo" (Fire) sem perder vida nenhuma enquanto o shield aguenta o dano direto. Se isso for estranho em teste, é fácil fechar depois.
9. **Morte reativa o shield na hora** (`OnHeroDeath()` chama `ActivateShield()`), descartando qualquer cooldown que estivesse em andamento — mesmo critério de "renasce são" que já existe pro resto do `HeroController` (HP cheio, Energia zerada, Efeitos limpos).
10. **`Dano Base 1,8 / Vida Base 50`** (maior Vida Base do MVP até agora) — só dado de `HeroStats` pro Inspector do prefab, não afeta nenhuma decisão de código.

## `Assets/Scripts/Player/Heroes/Paladin.cs` (novo)

```csharp
using System.Collections.Generic;
using UnityEngine;

public class Paladin : HeroController
{
    // ===================== Primário — martelo arremessado (GDD Seção 17.7) =====================
    [Header("Ataque primário — martelo (projétil reto, mira livre)")]
    [SerializeField] private GameObject hammerProjectilePrefab; // precisa ter PaladinHammer

    // Mesma rede de segurança do Barbarian — Animation Event de fim de ação nunca disparar
    // não pode travar o herói pra sempre em isAttacking.
    [SerializeField] private float maxActionDuration = 3f; // 🔢 ajustável
    private float actionElapsed;
    private bool hammerThrowFired;

    // ===================== Ultimate — espadas orbitando (Orbiting Hitbox) =====================
    [Header("Ultimate — espadas orbitando")]
    [SerializeField] private PaladinOrbitingBlades orbitingBlades; // filho dedicado, já no prefab, começa "Empty"
    [SerializeField] private float ultimateDamageMultiplier = 2f; // 🔢 GDD: "2x o dano do Paladin"
    [SerializeField] private float ultimateDuration = 5f; // 🔢 "por alguns segundos"

    // ===================== Secundária (Shift) — shield bash =====================
    [Header("Habilidade Secundária (Shift) — shield bash (4 triggers cardeais simultâneos)")]
    [SerializeField] private Collider2D shieldBashHitboxN;
    [SerializeField] private Collider2D shieldBashHitboxS;
    [SerializeField] private Collider2D shieldBashHitboxE;
    [SerializeField] private Collider2D shieldBashHitboxW;
    [SerializeField] private float shieldBashKnockbackForce = 4f; // 🔢 ajustável
    [SerializeField] private float maxShieldBashDuration = 2f; // 🔢 rede de segurança
    private float shieldBashElapsed;
    private bool shieldBashHitFired;

    // ===================== Passiva — shield periódico com vida própria =====================
    [Header("Passiva — shield periódico")]
    [SerializeField] private Animator shieldFrontAnimator; // DomeStart/DomeCycle/DomeEnd — na frente
    [SerializeField] private Animator shieldBackAnimator;   // DomeBaseStart/DomeBaseCycle/DomeBaseEnd — atrás
    [SerializeField] private float shieldMaxHealth = 20f; // 🔢 ajustável
    [SerializeField] private float shieldRecoverInterval = 30f; // 🔢 GDD: "a cada 30s, ajustável"
    private float shieldCurrentHealth;
    private float shieldCooldownRemaining;
    private bool shieldActive;

    private readonly List<EnemyController> hitTargets = new();
    private static readonly Collider2D[] OverlapBuffer = new Collider2D[16];

    protected override void Awake()
    {
        base.Awake();
        // Suposição (Seção 0, item 7): shield já nasce carregado, sem esperar o 1º intervalo.
        ActivateShield();
    }

    protected override void Update()
    {
        if (!GameplayGate.IsActive) return;
        base.Update();

        if (isAttacking)
        {
            actionElapsed += Time.deltaTime;
            if (actionElapsed >= maxActionDuration)
            {
                Debug.LogWarning("[Paladin] Animation Event de fim do martelo nunca chegou — forçando fim (verifique o Animator Controller).");
                isAttacking = false;
            }
        }

        if (isUsingSecondaryAbility)
        {
            shieldBashElapsed += Time.deltaTime;
            if (shieldBashElapsed >= maxShieldBashDuration)
            {
                Debug.LogWarning("[Paladin] Animation Event de fim do shield bash nunca chegou — forçando fim (verifique o Animator Controller).");
                isUsingSecondaryAbility = false;
            }
        }

        if (!shieldActive && shieldCooldownRemaining > 0f)
        {
            shieldCooldownRemaining -= Time.deltaTime;
            if (shieldCooldownRemaining <= 0f) ActivateShield();
        }
    }

    // ===================== Primário =====================

    protected override void PrimaryAttack()
    {
        isAttacking = true;
        actionElapsed = 0f;
        hammerThrowFired = false;
        AnimatorTrigger("AttackTrigger");
    }

    // Animation Event, no frame exato em que o martelo é solto da mão.
    public void AnimationHammerThrowEvent()
    {
        if (hammerThrowFired) return;
        hammerThrowFired = true;

        if (hammerProjectilePrefab == null) return;
        var obj = Instantiate(hammerProjectilePrefab, transform.position, Quaternion.identity);
        var hammer = obj.GetComponent<PaladinHammer>();
        if (hammer != null) hammer.Launch(RawAimDirection, stats.damage);
    }

    // Animation Event, no fim do clipe de ataque.
    public void AnimationAttackEndEvent()
    {
        isAttacking = false;
    }

    // ===================== Ultimate =====================

    protected override void UseUltimate()
    {
        if (orbitingBlades != null)
            orbitingBlades.Activate(ultimateDuration, stats.damage * ultimateDamageMultiplier);
    }

    // Sem override de CanUseUltimate()/IsUltimateActive()/CancelUltimate() — Seção 0, item 5.

    // ===================== Secundária (Shift) — shield bash =====================

    protected override void UseSecondaryAbility()
    {
        shieldBashElapsed = 0f;
        shieldBashHitFired = false;
        AnimatorTrigger("SecondaryAbilityTrigger");
    }

    // Imune a dano durante a animação inteira (GDD) — reaproveita a flag que o próprio
    // HeroController já liga/desliga ao redor da Habilidade Secundária.
    protected override bool IsDamageImmune => isUsingSecondaryAbility;

    // Animation Event único — os 4 triggers disparam juntos (GDD: "simultaneamente"), não
    // escolhido pela mira como o golpe do Barbarian.
    public void AnimationShieldBashHitEvent()
    {
        if (shieldBashHitFired) return;
        shieldBashHitFired = true;

        float damage = stats.damage;
        ApplyShieldBashHit(shieldBashHitboxN, damage);
        ApplyShieldBashHit(shieldBashHitboxS, damage);
        ApplyShieldBashHit(shieldBashHitboxE, damage);
        ApplyShieldBashHit(shieldBashHitboxW, damage);
    }

    private void ApplyShieldBashHit(Collider2D hitbox, float damage)
    {
        if (hitbox == null) return;

        int count = hitbox.Overlap(ContactFilter2D.noFilter, OverlapBuffer);
        // Dedup por trigger isolado — Seção 0, item 6.
        EnemyController.CollectDistinct(OverlapBuffer, count, hitTargets);
        foreach (var enemy in hitTargets)
        {
            enemy.TakeDamage(damage);
            Vector2 direction = ((Vector2)enemy.transform.position - (Vector2)transform.position).normalized;
            enemy.ApplyKnockback(direction, shieldBashKnockbackForce);
        }
    }

    // Animation Event, no fim do clipe de shield bash.
    public void AnimationShieldBashEndEvent()
    {
        isUsingSecondaryAbility = false;
    }

    // ===================== Passiva — shield periódico =====================

    public override void TakeDamage(float amount)
    {
        // Absorção total enquanto o shield tiver QUALQUER vida restante — mesmo um hit que
        // sozinho exceda a vida restante do shield não vaza pro Paladin (GDD: "o shield
        // absorve o hit inteiro que o estoura, e só então quebra"). Só o PRÓXIMO hit, já sem
        // shield, volta a afetar a vida real do Paladin.
        if (shieldActive && shieldCurrentHealth > 0f)
        {
            shieldCurrentHealth -= amount;
            if (shieldCurrentHealth <= 0f) BreakShield();
            return;
        }

        base.TakeDamage(amount);
    }

    private void ActivateShield()
    {
        shieldActive = true;
        shieldCurrentHealth = shieldMaxHealth;
        if (shieldFrontAnimator != null) shieldFrontAnimator.Play("DomeStart");
        if (shieldBackAnimator != null) shieldBackAnimator.Play("DomeBaseStart");
    }

    private void BreakShield()
    {
        shieldActive = false;
        shieldCurrentHealth = 0f;
        shieldCooldownRemaining = shieldRecoverInterval;
        if (shieldFrontAnimator != null) shieldFrontAnimator.Play("DomeEnd");
        if (shieldBackAnimator != null) shieldBackAnimator.Play("DomeBaseEnd");
    }

    // Seção 0, item 9 — morte reativa o shield na hora, descartando cooldown pendente.
    protected override void OnHeroDeath()
    {
        ActivateShield();
    }
}
```

## `Assets/Scripts/Player/PaladinHammer.cs` (novo)

```csharp
using UnityEngine;

// Martelo arremessado (GDD Seção 17.7) — projétil reto na direção exata da mira (sem snap de
// 8 direções, diferente do HeroProjectile), reserva de dano/perfuração (Seção 13), rotação
// contínua do sprite. Sem homing — vai sempre reto. Ver Seção 0, item 1 pra suposição sobre
// a técnica de rotação (modelado no ClericProjectile da Sprint 23).
public class PaladinHammer : MonoBehaviour
{
    [SerializeField] private float speed = 12f; // 🔢 ajustável
    [SerializeField] private float maxRange = 10f; // 🔢 ajustável
    [SerializeField] private Animator animator; // opcional — Fly/Impact, igual RogueBomb/ClericProjectile

    private Vector2 direction;
    private float reserve;
    private float distanceTraveled;
    private bool impacted;

    public void Launch(Vector2 launchDirection, float damageReserve)
    {
        direction = launchDirection.normalized;
        reserve = damageReserve;
        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f);
    }

    private void Update()
    {
        if (!GameplayGate.IsActive || impacted) return;

        float step = speed * Time.deltaTime;
        transform.Translate(direction * step, Space.World);
        distanceTraveled += step;

        if (distanceTraveled >= maxRange) Impact();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (impacted) return;
        if (!other.CompareTag("Enemy")) return;

        var enemy = other.GetComponent<EnemyController>();
        if (enemy == null) return;

        float damage = Mathf.Min(reserve, enemy.stats.health);
        enemy.TakeDamage(damage);
        reserve -= damage;

        if (reserve <= 0f) Impact();
    }

    // GDD: "tem sua própria animação de impacto ao esgotar a reserva de dano ou alcançar o
    // alcance máximo" — mesmo padrão do RogueBomb/ClericProjectile: trava movimento, toca o
    // clipe de impacto se existir, senão destrói na hora.
    private void Impact()
    {
        impacted = true;
        if (animator != null) animator.Play("Impact");
        else Destroy(gameObject);
    }

    // Animation Event, no fim do clipe de impacto (só chamado se houver Animator/clipe).
    public void AnimationImpactEndEvent()
    {
        Destroy(gameObject);
    }
}
```

## `Assets/Scripts/Player/PaladinOrbitingBlades.cs` (novo)

```csharp
using UnityEngine;

// Ultimate do Paladin (GDD Seção 17.7) — 2 espadas orbitando, 1ª implementação real de
// "Orbiting Hitbox" (Seção 13) no projeto. GameObject filho dedicado, sempre presente no
// prefab (nunca instanciado), Animator próprio (BladesStart/BladesCycle/BladesEnd) — começa
// invisível/"Empty" e só aparece quando Activate() é chamado pelo Paladin. As posições dos 2
// triggers são calculadas em código (ângulo incrementado a cada frame, gated por
// GameplayGate — sem Coroutine), o Animator só decide a sprite.
public class PaladinOrbitingBlades : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private Transform bladeA;
    [SerializeField] private Collider2D bladeAHitbox; // precisa de Rigidbody2D Kinematic no mesmo GameObject (trigger sem Rigidbody2D não gera evento — mesmo bug do RogueBomb/ClericProjectile)
    [SerializeField] private Transform bladeB;
    [SerializeField] private Collider2D bladeBHitbox; // idem
    [SerializeField] private float orbitRadius = 1.2f; // 🔢 ajustável
    [SerializeField] private float orbitSpeedDegreesPerSecond = 360f; // 🔢 ajustável

    private enum State { Inactive, Starting, Orbiting, Ending }
    private State state = State.Inactive;

    private float orbitAngle;
    private float remainingDuration;
    private float damagePerHit;

    public void Activate(float duration, float damage)
    {
        remainingDuration = duration;
        damagePerHit = damage;
        orbitAngle = 0f;
        state = State.Starting;
        SetHitboxesActive(false); // só liga de verdade no AnimationBladesStartEvent
        if (animator != null) animator.Play("BladesStart");
        else AnimationBladesStartEvent(); // sem Animator — pula direto pro ciclo
    }

    private void Update()
    {
        if (!GameplayGate.IsActive || state == State.Inactive) return;

        if (state == State.Orbiting)
        {
            orbitAngle += orbitSpeedDegreesPerSecond * Time.deltaTime;
            PositionBlade(bladeA, orbitAngle);
            PositionBlade(bladeB, orbitAngle + 180f);

            remainingDuration -= Time.deltaTime;
            if (remainingDuration <= 0f) EndOrbit();
        }
    }

    private void PositionBlade(Transform blade, float angleDegrees)
    {
        if (blade == null) return;
        float rad = angleDegrees * Mathf.Deg2Rad;
        blade.localPosition = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * orbitRadius;
    }

    // Animation Event, no fim do clipe BladesStart — só aqui os triggers passam a causar
    // dano de verdade, não desde o clique da Ultimate.
    public void AnimationBladesStartEvent()
    {
        state = State.Orbiting;
        SetHitboxesActive(true);
        if (animator != null) animator.Play("BladesCycle");
    }

    private void EndOrbit()
    {
        state = State.Ending;
        SetHitboxesActive(false);
        if (animator != null) animator.Play("BladesEnd");
        else AnimationBladesEndEvent();
    }

    // Animation Event, no fim do clipe BladesEnd.
    public void AnimationBladesEndEvent()
    {
        state = State.Inactive;
    }

    private void SetHitboxesActive(bool active)
    {
        if (bladeAHitbox != null) bladeAHitbox.enabled = active;
        if (bladeBHitbox != null) bladeBHitbox.enabled = active;
    }

    // Chamado pela ponte PaladinBladeHitbox (abaixo) — Collider2D só dispara o evento de
    // trigger no próprio GameObject dele, não no pai. Seção 0, item 3: cada volta que passar
    // por cima de um monstro conta como 1 hit novo (sem dedup entre voltas).
    public void NotifyHit(Collider2D other)
    {
        if (!other.CompareTag("Enemy")) return;
        var enemy = other.GetComponent<EnemyController>();
        if (enemy == null) return;
        enemy.TakeDamage(damagePerHit);
        // Sem knockback — Seção 0, item 4.
    }
}

// Ponte mínima — 1 componente por hitbox (bladeAHitbox/bladeBHitbox), sem lógica própria,
// só repassa OnTriggerEnter2D pro dono.
public class PaladinBladeHitbox : MonoBehaviour
{
    [SerializeField] private PaladinOrbitingBlades owner;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (owner != null) owner.NotifyHit(other);
    }
}
```

## Setup do Animator Controller / Prefab

- **`Paladin.controller`:** `Idle`/`Walk`/`Damage` diagonais (padrão), `Attack` com `AttackTrigger` → `AnimationHammerThrowEvent()` no frame do lançamento → `AnimationAttackEndEvent()` no fim. `SecondaryAbility` com `SecondaryAbilityTrigger` → `AnimationShieldBashHitEvent()` (1 evento só, dispara os 4 triggers juntos) → `AnimationShieldBashEndEvent()` no fim.
- **`PaladinHammer.controller`** (no prefab do martelo): `Fly`/`Impact`, igual `RogueBomb`/`ClericProjectile`. **Precisa de `Rigidbody2D` + `Collider2D` no prefab** — mesmo bug já visto 2x (Sprint 22 e 23): sem `Rigidbody2D`, o Collider2D Trigger não gera evento nenhum.
- **`Blades.controller`** (no filho `Blades` do Paladin): `Empty` (default) / `BladesStart` / `BladesCycle` (loop) / `BladesEnd` → `Empty` por Exit Time, mesmo padrão já corrigido no `ShiftEffect` do Cleric (Sprint 23) pra não ficar presa no último frame.
- **Hierarquia do filho `Blades`:** `Blades` (⟵ `PaladinOrbitingBlades`) → `BladeA` (⟵ `PaladinBladeHitbox` + `Collider2D` Trigger + `Rigidbody2D` Kinematic) e `BladeB` (idem). **Os 2 precisam de `Rigidbody2D` Kinematic** — mesmo motivo do martelo.
- **Shield (passiva):** 2 filhos do Paladin, `Shield_Front` (`Animator`: `DomeStart`/`DomeCycle`/`DomeEnd`) e `Shield_Back` (`Animator`: `DomeBaseStart`/`DomeBaseCycle`/`DomeBaseEnd`), com `Order in Layer` do `Shield_Back` menor que o sprite do Paladin e o do `Shield_Front` maior. Nenhum dos dois precisa de Collider — o shield é só visual, a lógica é inteira em `TakeDamage()`.
- **4 hitboxes do shield bash** — `ShieldBash_N/S/E/W`, mesmo padrão dos `AttackHitbox_NE/NW/SE/SW` do Barbarian (Collider2D Trigger, posicionados à mão no Editor nas 4 direções cardeais ao redor do Paladin).

## Checklist de teste manual

1. Martelo voa reto na direção exata da mira (não snapa em 8 direções), rotação do sprite acompanha.
2. Martelo aplica reserva de dano com perfuração (mata 1 monstro fraco, continua e acerta outro).
3. Martelo toca animação de impacto ao esgotar reserva OU ao alcançar `maxRange`.
4. Ultimate: as 2 espadas aparecem, orbitam visualmente ao redor do Paladin, e desaparecem depois de `ultimateDuration`.
5. Ultimate: cada espada causa dano (2× o dano do Paladin) a cada passada por um monstro — confirmar se acerta de novo numa volta seguinte (Seção 0, item 3) é o comportamento desejado.
6. Ultimate: Paladin continua andando e atacando normalmente enquanto as espadas orbitam.
7. Shield bash: os 4 triggers cardeais disparam juntos num único Animation Event, dano + knockback em qualquer monstro presente em qualquer um dos 4.
8. Shield bash: Paladin imune a dano durante toda a animação.
9. Shield bash: `isUsingSecondaryAbility` volta a `false` no fim do clipe (não cancelável, sem clique duplo pra interromper).
10. Passiva: shield visual aparece desde o início (Seção 0, item 7).
11. Passiva: um hit que exceda a vida restante do shield não vaza dano pro Paladin — só o hit seguinte, já sem shield, afeta a vida real.
12. Passiva: shield quebra (`DomeEnd`/`DomeBaseEnd`), espera `shieldRecoverInterval`, reaparece (`DomeStart`/`DomeBaseStart` → `DomeCycle`).
13. Passiva: morrer com o shield quebrado (em cooldown) faz ele reaparecer cheio no respawn (Seção 0, item 9).
14. Verificar se um projétil/Efeito Nocivo de monstro aplica Fire/Bleeding no Paladin mesmo com o shield absorvendo o dano direto (Seção 0, item 8) — confirmar se esse comportamento é aceitável.
15. `Rigidbody2D` presente no martelo e nas 2 espadas da Ultimate (sem ele, nenhum Trigger gera evento).
