# Sprint 19 — Mage Completo (Primário + Ultimate + Secundária/Shift + Pet Phoenix)

**Depende de:** Sprint 18b (arquitetura genérica de Shift em `HeroController`, `isBoss`, ESC pausando)
**Objetivo:** Mage sai desta sprint com o kit inteiro de uma vez — primário (fogo saindo na direção exata da mira, via um GameObject filho que gira de verdade), ultimate (bola de fogo em ângulo livre, saindo de 1 de 8 pontos fixos, com impacto + rastro persistente), Shift (teleporte na direção da mira, 3 fases) — e a Phoenix, o primeiro pet do jogo, com uma IA genérica pensada pra já servir o Elemental de Sangue do Blood Mage quando a sprint dele abrir.

---

## Seção 0 — Decisões explícitas antes de começar

Nenhuma delas é uma suposição escondida — são decisões de arquitetura que a ficha do Mage exige e que estão documentadas aqui pra você aprovar/ajustar antes de implementar, não descobertas no meio do código.

1. **Hook de "início de dia" (summon-lock da Phoenix):** nenhum arquivo que vi até agora expõe um evento desse tipo, e você confirmou que também não achou um. Pra não travar a sprint: **T01** adiciona `GameEvents.OnDayStart` (evento estático simples, mesmo padrão de `HealthChanged`/`EnergyChanged`/`BagChanged` que já existem) e você chama `GameEvents.DayStarted()` de onde o dia realmente começa hoje (provavelmente perto de onde a tela de resultado do dia anterior fecha, ou onde o `DayTimer` reinicia a contagem). **Se no meio da implementação você achar que já existe um evento equivalente com outro nome**, é só usar esse — descarte o T01 e troque a assinatura em `Mage.cs` (`GameEvents.OnDayStart +=`) pelo nome real. Nenhuma outra peça da sprint depende de qual dos dois caminhos você escolher.
2. **`HeroController` ganha `RawAimDistance`:** o teleporte do Mage precisa saber a que distância o mouse está (pra respeitar o alcance máximo), e hoje `HeroController` só expõe a *direção* (`RawAimDirection`, normalizada) — a distância crua se perde dentro de `UpdateAimDirection()`. T02 expõe essa distância do mesmo jeito que a direção já é exposta (mesmo congelamento durante `isAttacking`/`isTrapped`, de graça, por já reaproveitar o `if` existente).
3. **8 pontos de lançamento da ultimate são um array novo em `Mage.cs`**, mesmo espírito dos 4 hitboxes fixos do Barbarian e das 8 direções fixas do Ranger — só que aqui são 8 *Transforms* (posições de nascimento), não hitboxes nem projéteis. A ordem no Inspector **precisa** bater com `DirectionUtility` (E, NE, N, NW, W, SW, S, SE) — está documentado no comentário do código e na seção "Montar na Scene".
4. **`MageFireball` é um Ground Target/Impact Area que vira Persistent Area ao explodir** — mesma categoria da Seção 13 que a ultimate do Ranger já usa (facas → área persistente no chão). Não copiei o `RangerKnife` (não estava nos arquivos que você me mandou), mas segui a mesma taxonomia já fechada no GDD, então o comportamento formal é idêntico ao que você já aprovou pro Ranger — só a implementação em si é nova.
5. **`PetController` é uma classe nova e genérica**, não específica do Mage — Phoenix usa ela direto nesta sprint, e a ficha do Blood Mage (17.10) já deixa claro que o Elemental de Sangue "tem comportamento idêntico". Quando a sprint do Blood Mage abrir, o trabalho deve ser só trocar o prefab/Animator, sem tocar em `PetController.cs`.
6. **Pet não precisa de nenhuma flag de imunidade** — conferi o `EnemyController.cs` atual: a detecção de monstro usa só `GameObject.FindGameObjectWithTag("Player")`. Contanto que o pet não receba a tag `Player`, ele já é fisicamente invisível pra qualquer monstro, sem precisar reaproveitar `IsPlayerUntargetable` nem escrever nada novo pra isso.

---

## 1. `GameEvents.cs` — hook de início de dia

Adicionar (não substituir o arquivo — só inclua isso junto do que já existe lá, no mesmo padrão dos outros eventos estáticos):

```csharp
// Início de um novo dia — Pets de início de dia (Phoenix/Elemental de Sangue) escutam isso
// pra se re-sumonar. Chame GameEvents.DayStarted() de onde o dia realmente começa hoje no
// código (ex.: onde a tela de resultado do dia anterior fecha, ou onde o DayTimer reseta).
public static event Action OnDayStart;
public static void DayStarted() => OnDayStart?.Invoke();
```

> Se você achar que já existe um evento equivalente com outro nome em algum lugar do fluxo de dia, ignore este passo e troque a assinatura no `Mage.cs` (Seção 3, `OnEnable`/`OnDisable`) pra escutar o evento real.

---

## 2. `HeroController.cs` — expor a distância crua até o mouse

Duas mudanças pontuais, mesmo arquivo que você já tem:

```diff
     protected Vector2 RawAimDirection { get; private set; } = Vector2.down;
+
+    // Distância crua (não normalizada) até o mouse no instante da mira — hoje só o Ranger e o
+    // Barbarian precisavam da direção; o teleporte do Mage (GDD Seção 16/17.3) também precisa
+    // saber A DISTÂNCIA, pra respeitar um alcance máximo. Congela junto com RawAimDirection
+    // pelo mesmo motivo (isAttacking/isTrapped) — é o mesmo "toWorldMouse" de sempre, só que
+    // sem descartar o .magnitude antes de normalizar.
+    protected float RawAimDistance { get; private set; }
```

```diff
         AimDirection = DirectionUtility.SnapTo8Directions(toMouse);
         RawAimDirection = toMouse.normalized;
+        RawAimDistance = toMouse.magnitude;
```

---

## 3. `Mage.cs` (novo)

```csharp
using UnityEngine;

public class Mage : HeroController
{
    // Ataque primário (GDD Seção 17.3) — GameObject filho, sempre girando pra acompanhar a
    // mira em tempo real (gira aqui no Update(), não dentro do próprio filho), dispara sua
    // própria animação de fogo no Animation Event do cast do Mage.
    [Header("Ataque primário — trigger rotacionando (GDD Seção 17.3)")]
    [SerializeField] private Transform attackHitboxPivot;
    [SerializeField] private MageAttackHitbox attackHitbox;
    [SerializeField] private float attackDamageMultiplier = 1f; // 🔢 ajustável

    [Header("Ultimate — bola de fogo em ângulo livre (GDD Seção 17.3)")]
    [SerializeField] private GameObject fireballPrefab; // precisa ter MageFireball
    // 8 pontos fixos, MESMA ORDEM de DirectionUtility (E, NE, N, NW, W, SW, S, SE) — o índice
    // escolhido por GetDirectionIndex(AimDirection) indexa direto neste array.
    [SerializeField] private Transform[] ultimateLaunchPoints = new Transform[8];
    [SerializeField] private float ultimateDamageMultiplier = 4f; // 🔢 GDD: "4x o dano do Mage" de impacto

    // Habilidade Secundária (Shift) — teleporte na direção da mira (GDD Seção 16/17.3).
    // Não cancelável (sem override de CancelSecondaryAbility). 2 estados no Animator
    // (teleport_start com Exit Time -> teleport_end), cada um com 8 poses (AimX/AimY, já
    // alimentados pela base HeroController) — não precisa de lógica nova de direção aqui.
    [Header("Habilidade Secundária (Shift) — teleporte")]
    [SerializeField] private GameObject teleportStreakPrefab; // opcional — só visual, sem dano
    [SerializeField] private float secondaryAbilityMaxRange = 5f; // 🔢 ajustável

    [Header("Pet — Phoenix (GDD: Pets de início de dia)")]
    [SerializeField] private GameObject petPrefab; // precisa ter PetController
    [SerializeField] private Transform petSpawnPoint;
    private PetController currentPet;

    // Rede de segurança — mesmo padrão do Barbarian/Ranger: se o Animation Event de fim nunca
    // disparar, força o fim da ação em vez de travar isAttacking pra sempre.
    [SerializeField] private float maxActionDuration = 3f; // 🔢 ajustável
    private float actionElapsed;
    private bool ultimateFired;

    protected override void OnEnable()
    {
        base.OnEnable();
        GameEvents.OnDayStart += SummonPet;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        GameEvents.OnDayStart -= SummonPet;
    }

    protected override void Start()
    {
        base.Start();
        SummonPet(); // Dia 1 não dispara OnDayStart (o dia já está em andamento quando a Scene carrega)
    }

    protected override void Update()
    {
        if (!GameplayGate.IsActive) return;

        base.Update();

        // Gira o pivot todo frame acompanhando a mira em tempo real — RawAimDirection já
        // congela sozinho durante isAttacking (HeroController.UpdateAimDirection), então o
        // pivot também para de girar sozinho assim que o cast começa, sem checagem extra aqui.
        if (attackHitboxPivot != null)
        {
            float angle = Mathf.Atan2(RawAimDirection.y, RawAimDirection.x) * Mathf.Rad2Deg - 90f;
            attackHitboxPivot.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        if (isAttacking)
        {
            actionElapsed += Time.deltaTime;
            if (actionElapsed >= maxActionDuration)
            {
                Debug.LogWarning("[Mage] Animation Event de fim de ação nunca chegou — forçando fim (verifique o Animator Controller).");
                isAttacking = false;
            }
        }
    }

    protected override void PrimaryAttack()
    {
        if (isAttacking) return;
        isAttacking = true;
        actionElapsed = 0f;
        AnimatorTrigger("AttackTrigger");
    }

    // Animation Event, no meio do cast — dispara a animação de fogo do próprio filho, que
    // decide o instante exato do dano na SUA própria animação (ver MageAttackHitbox).
    public void AnimationCastFireEvent()
    {
        if (attackHitbox != null) attackHitbox.Fire(stats.damage * attackDamageMultiplier);
    }

    // Animation Event, no fim do clipe de cast.
    public void AnimationAttackEndEvent()
    {
        isAttacking = false;
    }

    protected override void UseUltimate()
    {
        if (isAttacking) return;
        isAttacking = true;
        actionElapsed = 0f;
        ultimateFired = false;
        AnimatorTrigger("UltimateTrigger");
    }

    // Animation Event, no frame exato em que a bola de fogo sai. Ponto de partida = 1 dos 8
    // fixos (escolhido pela mira JÁ travada em 8 direções); trajetória = ângulo livre exato
    // (RawAimDirection), exceção documentada na ficha do Mage (Seção 17.3).
    public void AnimationUltimateLaunchEvent()
    {
        if (ultimateFired) return;
        ultimateFired = true;

        if (fireballPrefab == null) return;

        int index = DirectionUtility.GetDirectionIndex(AimDirection);
        Transform launchPoint = (index >= 0 && index < ultimateLaunchPoints.Length) ? ultimateLaunchPoints[index] : null;
        Vector3 spawnPos = launchPoint != null ? launchPoint.position : transform.position;

        float impactDamage = stats.damage * ultimateDamageMultiplier;
        var obj = Instantiate(fireballPrefab, spawnPos, Quaternion.identity);
        var fireball = obj.GetComponent<MageFireball>();
        if (fireball != null) fireball.Launch(RawAimDirection, impactDamage, stats.damage);
    }

    // Animation Event, no fim do clipe de ultimate.
    public void AnimationUltimateEndEvent()
    {
        isAttacking = false;
    }

    // Bloqueia tudo (isAttacking, mesma regra do Ranger) — não cancelável, roda até o fim.
    protected override void UseSecondaryAbility()
    {
        isAttacking = true;
        actionElapsed = 0f;
        AnimatorTrigger("SecondaryAbilityTrigger");
    }

    // Animation Event opcional, cedo no clipe "teleport_start" — só o streak visual, sem dano
    // (GDD: "solta um projétil visual que viaja na direção do teleporte").
    public void AnimationTeleportVisualEvent()
    {
        if (teleportStreakPrefab == null) return;
        var obj = Instantiate(teleportStreakPrefab, transform.position, Quaternion.identity);
        float angle = Mathf.Atan2(RawAimDirection.y, RawAimDirection.x) * Mathf.Rad2Deg - 90f;
        obj.transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    // Animation Event, mais tarde no MESMO clipe "teleport_start" — o teleporte de verdade
    // (GDD: "o próprio teleporte acontece num Animation Event"). Distância = a do mouse,
    // limitada pelo alcance máximo; direção = RawAimDirection (congelada desde o início do
    // Shift, mesma trava de sempre).
    public void AnimationTeleportMoveEvent()
    {
        float distance = Mathf.Min(RawAimDistance, secondaryAbilityMaxRange);
        transform.position += (Vector3)(RawAimDirection * distance);
    }

    // Animation Event, no fim do clipe "teleport_end" (transição start -> end via Exit Time,
    // sem condição — mesmo padrão hidden -> during da camuflagem do Ranger).
    public void AnimationTeleportEndEvent()
    {
        isAttacking = false;
        isUsingSecondaryAbility = false;
    }

    // Chamado no Start() (Dia 1) e a cada GameEvents.OnDayStart. Se já existir uma Phoenix viva
    // (dia anterior), destrói e sumona de novo — GDD não fala em persistir o pet entre dias,
    // só em sumonar "no início do dia", então cada dia começa com uma Phoenix nova.
    private void SummonPet()
    {
        if (currentPet != null) Destroy(currentPet.gameObject);
        if (petPrefab == null) return;

        Vector3 spawnPos = petSpawnPoint != null ? petSpawnPoint.position : transform.position;
        var obj = Instantiate(petPrefab, spawnPos, Quaternion.identity);
        currentPet = obj.GetComponent<PetController>();
        if (currentPet != null) currentPet.Initialize(transform);
    }
}
```

---

## 4. `MageAttackHitbox.cs` (novo) — o filho rotacionando do primário

```csharp
using UnityEngine;

// GameObject filho do Mage (girado pelo Mage.cs no Update(), não por si próprio) — trigger
// retangular fixo, sempre grudado no Mage, que só existe pra guardar o dano pendente e
// aplicá-lo no instante certo da PRÓPRIA animação de fogo saindo (GDD Seção 17.3). Não viaja
// (ao contrário de HeroProjectile/RangerArrow) — é sempre a mesma instância, reaproveitada a
// cada ataque.
public class MageAttackHitbox : MonoBehaviour
{
    [SerializeField] private Collider2D hitbox;
    private Animator animator;
    private float pendingDamage;
    private bool hitFired;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void Fire(float damage)
    {
        pendingDamage = damage;
        hitFired = false;
        if (animator != null) animator.SetTrigger("FireTrigger");
    }

    // Animation Event, no instante exato em que o fogo sai de verdade (dentro do clipe deste
    // próprio filho, não do Mage).
    public void AnimationFireHitEvent()
    {
        if (hitFired) return;
        hitFired = true;
        if (hitbox == null) return;

        var results = new Collider2D[16];
        int count = hitbox.Overlap(ContactFilter2D.noFilter, results);
        for (int i = 0; i < count; i++)
        {
            if (!results[i].CompareTag("Enemy")) continue;
            var enemy = results[i].GetComponent<EnemyController>();
            if (enemy != null) enemy.TakeDamage(pendingDamage);
        }
    }
}
```

---

## 5. `MageFireball.cs` + `MageFireTrail.cs` (novos) — ultimate

```csharp
using UnityEngine;

// Ground Target/Impact Area que vira Persistent Area ao explodir (GDD Seção 13, mesma
// taxonomia da ultimate do Ranger) — viaja em ângulo LIVRE (RawAimDirection, sem snap de 8
// direções, exceção documentada na ficha do Mage), explode ao acertar um monstro OU ao
// alcançar maxTravelDistance (o que vier primeiro), então deixa um rastro de fogo persistente.
public class MageFireball : MonoBehaviour
{
    [SerializeField] private float speed = 8f;
    [SerializeField] private float maxTravelDistance = 4f; // 🔢 "distância curta" — ajustável
    [SerializeField] private float explosionRadius = 1.2f; // 🔢 ajustável
    [SerializeField] private GameObject fireTrailPrefab; // precisa ter MageFireTrail
    [SerializeField] private float trailDamageMultiplier = 0.5f; // 🔢 GDD: "0,5x o dano do Mage por segundo"
    [SerializeField] private float trailDuration = 30f; // 🔢 GDD
    [SerializeField] private LayerMask enemyLayer; // ajustável no Inspector — mesma layer usada pelo Overlap dos heróis

    private Vector2 direction;
    private float impactDamage;
    private float baseDamageForTrail;
    private float distanceTraveled;

    public void Launch(Vector2 dir, float impact, float baseDamage)
    {
        direction = dir.normalized;
        impactDamage = impact;
        baseDamageForTrail = baseDamage;

        // 1 sprite só, rotação contínua — mesma técnica do RangerArrow, pelo mesmo motivo
        // (ângulo livre, não travado em 8 poses fixas).
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void Update()
    {
        if (!GameplayGate.IsActive) return;

        // Posição direto em espaço de mundo, não Translate — mesmo motivo do RangerArrow: o
        // Transform está rotacionado, então os eixos locais giraram junto.
        float step = speed * Time.deltaTime;
        transform.position += (Vector3)(direction * step);
        distanceTraveled += step;
        if (distanceTraveled >= maxTravelDistance) Explode();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Enemy")) return;
        Explode();
    }

    private void Explode()
    {
        var results = new Collider2D[16];
        int count = Physics2D.OverlapCircle(transform.position, explosionRadius, new ContactFilter2D().NoFilter(), results);
        for (int i = 0; i < count; i++)
        {
            if (!results[i].CompareTag("Enemy")) continue;
            var enemy = results[i].GetComponent<EnemyController>();
            if (enemy != null) enemy.TakeDamage(impactDamage);
        }

        if (fireTrailPrefab != null)
        {
            var trailObj = Instantiate(fireTrailPrefab, transform.position, Quaternion.identity);
            var trail = trailObj.GetComponent<MageFireTrail>();
            if (trail != null) trail.Initialize(baseDamageForTrail * trailDamageMultiplier, trailDuration);
        }

        Destroy(gameObject);
    }
}
```

```csharp
using UnityEngine;

// Persistent Area (GDD Seção 13) deixada pela ultimate do Mage — causa dano por segundo a
// quem passar por cima, durante um tempo fixo. Timer manual no Update() gated por
// GameplayGate.IsActive — não Coroutine (Sprint 13: WaitForSeconds não respeita a pausa
// manual do projeto).
public class MageFireTrail : MonoBehaviour
{
    [SerializeField] private float tickInterval = 1f; // 🔢 ajustável
    private AttackCooldown damageTick;
    private float damagePerTick;
    private float remaining;
    private Collider2D areaCollider;

    private void Awake()
    {
        areaCollider = GetComponent<Collider2D>();
    }

    public void Initialize(float damagePerSecond, float duration)
    {
        damagePerTick = damagePerSecond * tickInterval;
        remaining = duration;
        damageTick = new AttackCooldown(tickInterval);
    }

    private void Update()
    {
        if (!GameplayGate.IsActive) return;

        remaining -= Time.deltaTime;
        if (remaining <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        damageTick.Tick(Time.deltaTime);
        if (!damageTick.TryConsume() || areaCollider == null) return;

        var results = new Collider2D[16];
        int count = areaCollider.Overlap(ContactFilter2D.noFilter, results);
        for (int i = 0; i < count; i++)
        {
            if (!results[i].CompareTag("Enemy")) continue;
            var enemy = results[i].GetComponent<EnemyController>();
            if (enemy != null) enemy.TakeDamage(damagePerTick);
        }
    }
}
```

---

## 6. `PetController.cs` (novo) — IA genérica de pet (Phoenix agora, Elemental de Sangue depois)

```csharp
using UnityEngine;

// IA genérica de pet de início de dia (GDD: "Pets de início de dia (Mage/Blood Mage)") —
// Phoenix e Elemental de Sangue usam esta MESMA classe, só trocando prefab/Animator (arte).
// Não é EnemyController nem HeroController: sem Vida própria, sem TakeDamage — o pet nunca é
// alvo válido porque EnemyController só procura GameObject.FindGameObjectWithTag("Player")
// (conferido no código atual); contanto que este prefab NÃO tenha a tag "Player", já é
// invisível pra detecção de monstro sem nenhuma flag extra.
public class PetController : MonoBehaviour
{
    [SerializeField] private float chaseRadius = 6f; // 🔢 raio ao redor do DONO (não do pet) onde aceita perseguir
    [SerializeField] private float leashDistance = 2.5f; // 🔢 distância que tenta manter do dono sem alvo
    [SerializeField] private float moveSpeed = 3.5f; // 🔢
    [SerializeField] private float contactRange = 0.6f; // 🔢 alcance de contato, igual Melee comum

    [Header("Ataque — 4 hitboxes fixas por direção, igual golpe do Barbarian/Melee comum")]
    [SerializeField] private Collider2D attackHitboxNE;
    [SerializeField] private Collider2D attackHitboxNW;
    [SerializeField] private Collider2D attackHitboxSE;
    [SerializeField] private Collider2D attackHitboxSW;
    [SerializeField] private float attackCooldownDuration = 1f; // 🔢
    [SerializeField] private float damage = 2f; // 🔢 — cartas específicas (dano/velocidade/vel. ataque) chegam no Card Framework, Sprint 35

    private Transform owner;
    private Animator animator;
    private AttackCooldown attackCooldown;
    private Transform currentTarget;
    private Vector2 facing = Vector2.down;
    private bool isAttacking;
    private bool attackHitFired;

    public void Initialize(Transform petOwner)
    {
        owner = petOwner;
    }

    private void Awake()
    {
        animator = GetComponent<Animator>();
        attackCooldown = new AttackCooldown(attackCooldownDuration);
        if (animator != null) animator.Play("summon");
    }

    private void Update()
    {
        if (!GameplayGate.IsActive || owner == null) return;

        attackCooldown.Tick(Time.deltaTime);
        if (isAttacking) return; // golpe em andamento -- não se move nem re-escolhe alvo

        FindTarget();

        if (currentTarget != null)
        {
            float distToTarget = Vector2.Distance(transform.position, currentTarget.position);
            if (distToTarget <= contactRange) TryAttack();
            else MoveTowards(currentTarget.position);
        }
        else
        {
            float distToOwner = Vector2.Distance(transform.position, owner.position);
            if (distToOwner > leashDistance) MoveTowards(owner.position);
            else if (animator != null) animator.SetBool("IsMoving", false);
        }
    }

    // Raio de perseguição centrado no DONO, não no pet (GDD, explícito) — um pet que já esteja
    // longe perseguindo não passa a "enxergar" mais longe do que o próprio herói enxergaria.
    private void FindTarget()
    {
        if (currentTarget != null)
        {
            var enemy = currentTarget.GetComponent<EnemyController>();
            bool stillValid = enemy != null && Vector2.Distance(owner.position, currentTarget.position) <= chaseRadius;
            if (!stillValid) currentTarget = null;
        }

        if (currentTarget != null) return;

        var hits = Physics2D.OverlapCircleAll(owner.position, chaseRadius);
        float closest = float.MaxValue;
        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Enemy")) continue;
            float dist = Vector2.Distance(transform.position, hit.transform.position);
            if (dist < closest)
            {
                closest = dist;
                currentTarget = hit.transform;
            }
        }
    }

    private void MoveTowards(Vector2 destination)
    {
        Vector2 dir = (destination - (Vector2)transform.position).normalized;
        facing = dir;
        transform.position += (Vector3)(dir * moveSpeed * Time.deltaTime);
        if (animator != null)
        {
            animator.SetBool("IsMoving", true);
            animator.SetFloat("MoveX", dir.x);
            animator.SetFloat("MoveY", dir.y);
        }
    }

    private void TryAttack()
    {
        if (animator != null) animator.SetBool("IsMoving", false);
        if (!attackCooldown.TryConsume()) return;

        isAttacking = true;
        attackHitFired = false;
        if (currentTarget != null) facing = (currentTarget.position - transform.position).normalized;
        if (animator != null)
        {
            animator.SetFloat("AimX", facing.x);
            animator.SetFloat("AimY", facing.y);
            animator.SetTrigger("AttackTrigger");
        }
    }

    // Animation Event -- mesmo instante exato do golpe do Barbarian/Melee comum.
    public void AnimationAttackHitEvent()
    {
        if (attackHitFired) return;
        attackHitFired = true;

        Collider2D hitbox = GetHitboxForFacing();
        if (hitbox == null) return;

        var results = new Collider2D[16];
        int count = hitbox.Overlap(ContactFilter2D.noFilter, results);
        for (int i = 0; i < count; i++)
        {
            if (!results[i].CompareTag("Enemy")) continue;
            var enemy = results[i].GetComponent<EnemyController>();
            if (enemy != null) enemy.TakeDamage(damage);
        }
    }

    // Animation Event, fim do clipe de ataque.
    public void AnimationAttackEndEvent()
    {
        isAttacking = false;
    }

    // Mesmo critério de 4 quadrantes do Barbarian.GetHitboxForFacing().
    private Collider2D GetHitboxForFacing()
    {
        bool east = facing.x >= 0f;
        bool north = facing.y >= 0f;
        if (north) return east ? attackHitboxNE : attackHitboxNW;
        return east ? attackHitboxSE : attackHitboxSW;
    }
}
```

---

## 7. Montar na Scene

**Mage:**
- `Mage` (prefab do herói): `attackHitboxPivot` = um filho vazio na posição do Mage; dentro dele, um filho com `MageAttackHitbox` + `Collider2D` (Is Trigger) posicionado um pouco à frente (offset local em +Y, por exemplo) + `Animator` próprio com 1 estado (`fire`) e o parâmetro `FireTrigger`; clipe `fire` tem o Animation Event `AnimationFireHitEvent()` no instante em que o fogo sai.
- `ultimateLaunchPoints[0..7]` = 8 Transforms vazios ao redor do Mage, na **ordem exata** E, NE, N, NW, W, SW, S, SE (mesma ordem de `DirectionUtility.DirectionNames`) — o índice errado aqui faz a bola de fogo nascer do lado errado silenciosamente, sem erro de compilação.
- `petSpawnPoint` = Transform vazio perto do Mage.
- Animator do Mage: `AttackTrigger` (clipe de cast, com o Animation Event `AnimationCastFireEvent()` no meio e `AnimationAttackEndEvent()` no fim), `UltimateTrigger` (clipe de salto/conjuração, com `AnimationUltimateLaunchEvent()` no frame de disparo e `AnimationUltimateEndEvent()` no fim), `SecondaryAbilityTrigger` → estado `teleport_start` (Blend Tree 2D Freeform Directional por `AimX`/`AimY`, com `AnimationTeleportVisualEvent()` cedo e `AnimationTeleportMoveEvent()` mais tarde no clipe) → transição por Exit Time (sem condição) → `teleport_end` (Blend Tree igual, com `AnimationTeleportEndEvent()` no fim).

**Prefabs novos:**
- `MageFireball` prefab: `SpriteRenderer` + `Collider2D` (Is Trigger) + `Rigidbody2D` (Kinematic) + `MageFireball.cs`.
- `MageFireTrail` prefab: `SpriteRenderer` (visual de fogo no chão) + `Collider2D` (Is Trigger, área do rastro) + `MageFireTrail.cs`.
- `Phoenix` prefab: `SpriteRenderer` + `Animator` (`summon`/`idle`/`walk`/`attack`, parâmetros `IsMoving`, `MoveX`/`MoveY`, `AimX`/`AimY`, `AttackTrigger`) + `PetController.cs` com os 4 `attackHitbox*` configurados (mesmo estilo dos hitboxes do Barbarian) — **sem** a tag `Player`, sem `Collider2D` marcado como `Enemy` (não precisa de nenhuma tag especial, só não pode ter as duas que já significam algo pro projeto).

---

## 8. Testes manuais (checklist)

- [ ] Primário: o filho rotaciona em tempo real acompanhando o mouse antes de atacar; ao castar, o fogo sai exatamente na direção em que o filho estava apontando; dano aplicado uma única vez por golpe.
- [ ] Ultimate: a bola de fogo nasce do ponto certo dentre os 8 (testar mirando pras 8 direções e confirmar visualmente); viaja em ângulo livre (testar num ângulo entre duas das 8 direções); explode ao tocar um monstro OU ao alcançar `maxTravelDistance` sem tocar nada; deixa um rastro que causa dano por tick a quem fica em cima, durante `trailDuration`.
- [ ] Shift: teleporta na direção do mouse; respeita `secondaryAbilityMaxRange` quando o mouse está mais longe que isso; não bloqueia se usado com o mouse bem perto (distância pequena ainda teleporta, só que pouco); bloqueia movimento/ataque/ultimate durante as 2 fases; não é cancelável (Shift de novo no meio não faz nada).
- [ ] Pet: sumona no `Start()` (Dia 1); some e sumona de novo em `GameEvents.OnDayStart` (ou o evento real que você usar); persegue o monstro mais próximo dentro do raio centrado no Mage (não no próprio pet — testar afastando o pet do Mage perseguindo e confirmar que o raio efetivo é medido a partir do Mage); ataca ao entrar em alcance de contato; volta a ficar perto do Mage quando não há monstro no raio; monstros nunca miram nele (confirmar visualmente que nenhum monstro persegue/ataca o pet).
- [ ] Sem testes automatizados novos — toda a lógica desta sprint é runtime/Scene-dependente (Animator, física, timing de Animation Event), mesmo critério já usado nas sprints de herói anteriores.

---

## 9. Commits sugeridos

```
git commit -m "feat(mage): ataque primário com hitbox rotacionando em tempo real"
git commit -m "feat(mage): ultimate com 8 pontos de lançamento e ângulo livre + rastro persistente"
git commit -m "feat(mage): habilidade secundária (Shift) — teleporte com alcance máximo"
git commit -m "feat(hero): expõe RawAimDistance em HeroController"
git commit -m "feat(pets): PetController genérico + Phoenix (primeiro pet do jogo)"
git commit -m "feat(events): adiciona GameEvents.OnDayStart"
```

---

## Fechamento

- **GDD:** Seções 17.3 (Mage) e a sub-seção "Pets de início de dia" já estão com o ✅ — nenhuma mudança de design esperada, só confirmar depois do playtest se algum 🔢 mudou de ideia.
- **Bestiary:** não se aplica (Mage não é Bestiário).
- **Plano de Produção:** quando você me mandar o relatório desta sprint, atualizo a linha "19" (já corrigida pra "Mage — Completo" na revisão anterior) confirmando o fechamento e abro a Sprint 20 (Floor 1A–2A + Bestiary Batch 1+2).

**Pronto quando:** Mage jogável com primário, ultimate, Shift e Phoenix funcionando de ponta a ponta em Play Mode, sem travar em nenhuma fase (mesmo critério de "pronto" usado nas sprints de herói anteriores).
