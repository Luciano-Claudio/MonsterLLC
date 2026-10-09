# Sprint 30 — Blood Mage (GDD Seção 17.10 + "Pets de início de dia")

> Prospectiva. 3 mecânicas sem precedente direto no projeto (reserva de dano por perfuração,
> anel de dano em 3 estágios com zona segura dinâmica, orb de cura por callback) + 1 mecânica
> que É um precedente direto (pet, cópia 1:1 do `SummonPet` da Phoenix do Mage).

## Seção 0 — Decisões e suposições (revisar antes de implementar)

0. **`RangerArrow.cs` (mecanismo real de reserva/perfuração) continua fora de alcance** — pedido
   2x antes (prep do Cleric, prep do Paladin), nunca entregue; da 1ª vez foi aceito prosseguir
   sem ele e validado certo depois. Mesmo critério aqui: reserva = `stats.damage ×
   damageReserveMultiplier` (🔢 ajustável — "quantos hits cheios o projétil aguenta antes de
   esgotar"); cada inimigo perfurado consome `Mathf.Min(stats.damage, reservaRestante)`; o
   projétil se destrói (toca a própria animação de impacto) quando a reserva chega a 0 OU a
   distância máxima é alcançada, o que vier primeiro.

1. **`BloodMageShockwave` como objeto filho dedicado** (novo, mesmo critério "controlador decide
   tudo, filho só recebe" da Sprint 27b). 3 chamadas de `OverlapBoxAll`, uma por estágio; cada
   estágio exclui quem já estava dentro da caixa do estágio ANTERIOR (zona segura dinâmica — GDD:
   "quando o anel 4×4 aparece, o quadrado 2×2 original já está seguro").

2. **O salto da Ultimate não desloca o Blood Mage horizontalmente** — o anel nasce centrado na
   posição onde ele estava ANTES de saltar (= onde pousa). GDD não menciona deslocamento
   horizontal, só "pula... no frame da queda". **Peço confirmação** se é só um salto vertical no
   lugar mesmo.

3. **`BloodOrb` (novo) usa callback, não chamada direta** — `Heal()` é `protected` em
   `HeroController`, inacessível de um script externo. Mesmo padrão do `MageTeleportProjectile`
   (`Launch(..., callback)`): o Blood Mage passa um método privado próprio como callback, que
   chama `Heal()` por dentro.

4. **"Monstro vivo mais próximo"** usa o mesmo critério de busca da vinha do Druid —
   `OverlapCircleAll` + descarta `HealthSystem.IsDead` + ordena por distância + pega o primeiro.

5. **`extract_blood` (Secundária) bloqueia `isAttacking` a animação inteira** (channel sem
   movimento) — GDD não é explícita sobre isso, é suposição por categoria (mesmo critério de
   qualquer "cast" parado do projeto). **Peço confirmação.**

6. **Pet (Elemental de Sangue) é cópia 1:1 do `SummonPet` da Phoenix do Mage** — mesmo
   `PetController.Initialize(...)`, mesma API, só prefab/arte mudam. Não precisei ver
   `PetController.cs` pra isso: não estou mudando o comportamento dele, só reaproveitando a
   mesma chamada que a Phoenix já usa com sucesso.

7. **Dano do anel aplicado 1x por estágio** (3 pulsos totais por uso da Ultimate, "5× o dano" em
   cada), não contínuo enquanto o anel está visível.

8. **Sem knockback em nenhuma das 3 habilidades** — a GDD não menciona empurrão em nenhuma
   delas (diferente de Barbarian/Paladin/Rogue). Se precisar, é fácil de acrescentar depois.

---

## Código — `BloodMage.cs`

```csharp
using System.Collections.Generic;
using UnityEngine;

public class BloodMage : HeroController
{
    // ===================== Primário — projétil reto com reserva de dano (GDD Seção 17.10/13) =====================
    [Header("Ataque primário — projétil reto com perfuração")]
    [SerializeField] private GameObject projectilePrefab; // precisa ter BloodMageProjectile
    [SerializeField] private LayerMask enemyLayerMask;
    [SerializeField] private float projectileSpeed = 10f; // 🔢 ajustável
    [SerializeField] private float projectileMaxDistance = 8f; // 🔢 ajustável
    // Reserva de dano — quantos "hits cheios" o projétil aguenta antes de se esgotar (Seção 0, item 0).
    [SerializeField] private float damageReserveMultiplier = 3f; // 🔢 ajustável
    private bool projectileLaunchFired;

    // Rede de segurança genérica — mesmo padrão de todo herói.
    [SerializeField] private float maxActionDuration = 3f; // 🔢 ajustável
    private float actionElapsed;

    // ===================== Ultimate — onda de choque em anel (GDD Seção 17.10) =====================
    [Header("Ultimate — anel de dano em 3 estágios")]
    [SerializeField] private BloodMageShockwave shockwavePrefab; // filho dedicado, já no prefab
    [SerializeField] private float shockwaveDamageMultiplier = 5f; // GDD: "5x o dano" por estágio
    [SerializeField] private float shockwaveStage1Size = 2f; // 🔢 GDD: 2×2
    [SerializeField] private float shockwaveStage2Size = 4f; // 🔢 GDD: 4×4
    [SerializeField] private float shockwaveStage3Size = 8f; // 🔢 GDD: 8×8
    private bool shockwaveSpawnFired;

    // ===================== Secundária (Shift) — extração de sangue =====================
    [Header("Habilidade Secundária (Shift) — extração de sangue")]
    [SerializeField] private float extractSearchRadius = 15f; // 🔢 ajustável, mesmo critério da vinha do Druid
    [SerializeField] private GameObject bloodOrbPrefab; // precisa ter BloodOrb
    [SerializeField] private float extractDamageMultiplier = 1f; // 🔢 ajustável — GDD não dá número
    [SerializeField] private float extractHealAmount = 15f; // 🔢 ajustável — GDD não dá número
    [SerializeField] private float orbSpeed = 10f; // 🔢 ajustável
    private bool extractHitFired;

    // ===================== Passiva — pet Elemental de Sangue (idêntico à Phoenix do Mage) =====================
    [Header("Pet — Elemental de Sangue")]
    [SerializeField] private GameObject petPrefab; // precisa ter PetController
    [SerializeField] private Transform petSpawnPoint;
    [SerializeField] private float petSizeMultiplier = 1f; // 🔢 upgradable — sem teto definido ainda, mesmo critério do Mage
    [SerializeField] private float petActionSpeedMultiplier = 1f; // 🔢 upgradable — sem teto definido ainda
    private PetController currentPet;

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

    protected override void Update()
    {
        if (!GameplayGate.IsActive) return;
        base.Update();

        if (isAttacking)
        {
            actionElapsed += Time.deltaTime;
            if (actionElapsed >= maxActionDuration)
            {
                Debug.LogWarning("[BloodMage] Animation Event de fim de ação nunca chegou — forçando fim (verifique o Animator Controller).");
                isAttacking = false;
                isUsingSecondaryAbility = false;
            }
        }
    }

    // ===================== Primário =====================

    protected override void PrimaryAttack()
    {
        if (isAttacking) return;
        isAttacking = true;
        actionElapsed = 0f;
        projectileLaunchFired = false;
        AnimatorTrigger("AttackTrigger");
    }

    // Animation Event, no frame exato em que o projétil é lançado — direção = AimDirection (1
    // de 8 direções fixas, "Straight Projectile", mesmo critério do HeroProjectile do Barbarian).
    public void AnimationProjectileLaunchEvent()
    {
        if (projectileLaunchFired) return;
        projectileLaunchFired = true;

        if (projectilePrefab == null) return;

        float reserve = stats.damage * damageReserveMultiplier;
        var obj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        var projectile = obj.GetComponent<BloodMageProjectile>();
        if (projectile != null) projectile.Launch(AimDirection, stats.damage, reserve, enemyLayerMask, projectileSpeed, projectileMaxDistance);
    }

    // Animation Event, no fim do clipe de ataque.
    public void AnimationAttackEndEvent()
    {
        isAttacking = false;
    }

    // ===================== Ultimate =====================

    protected override bool CanUseUltimate() => !isAttacking;

    protected override void UseUltimate()
    {
        isAttacking = true;
        actionElapsed = 0f;
        shockwaveSpawnFired = false;
        AnimatorTrigger("UltimateTrigger");
    }

    // Animation Event, no frame exato da queda do salto — nasce o anel, centrado na posição
    // atual (Seção 0, item 2: sem deslocamento horizontal do próprio salto).
    public void AnimationShockwaveSpawnEvent()
    {
        if (shockwaveSpawnFired) return;
        shockwaveSpawnFired = true;

        if (shockwavePrefab == null) return;

        float damagePerStage = stats.damage * shockwaveDamageMultiplier;
        var wave = Instantiate(shockwavePrefab, transform.position, Quaternion.identity);
        wave.Activate(damagePerStage, shockwaveStage1Size, shockwaveStage2Size, shockwaveStage3Size, enemyLayerMask);
    }

    // Animation Event, no fim do clipe de Ultimate (o anel já está rodando sozinho, GameObject
    // independente — mesmo critério de qualquer Ultimate que nasce um filho e devolve o
    // controle antes dele terminar, ex.: a bomba do Rogue).
    public void AnimationUltimateEndEvent()
    {
        isAttacking = false;
    }

    // ===================== Secundária (Shift) — extração de sangue =====================

    protected override void UseSecondaryAbility()
    {
        isAttacking = true; // channel sem movimento — Seção 0, item 5
        actionElapsed = 0f;
        extractHitFired = false;

        // Pose travada numa das 4 diagonais — mesmo critério do RollAimX/Y da Cambalhota / da
        // reza do Cleric.
        if (animator != null)
        {
            Vector2 pose = DirectionUtility.SnapTo4Diagonals(RawAimDirection);
            animator.SetFloat("ExtractAimX", pose.x);
            animator.SetFloat("ExtractAimY", pose.y);
        }

        AnimatorTrigger("SecondaryAbilityTrigger");
    }

    // Animation Event, no fim da reza — acerta o monstro vivo mais próximo e solta a orb de
    // sangue até o Blood Mage, que cura ao chegar (Seção 0, itens 3 e 4).
    public void AnimationExtractBloodEvent()
    {
        if (extractHitFired) return;
        extractHitFired = true;

        EnemyController target = FindNearestLivingEnemy();
        if (target == null) return;

        target.TakeDamage(stats.damage * extractDamageMultiplier);

        if (bloodOrbPrefab == null) return;
        var obj = Instantiate(bloodOrbPrefab, target.transform.position, Quaternion.identity);
        var orb = obj.GetComponent<BloodOrb>();
        if (orb != null) orb.Launch(transform, orbSpeed, OnBloodOrbArrived);
    }

    // Mesmo critério de busca da vinha do Druid (AnimationVineSummonEvent) — Seção 0, item 4.
    private EnemyController FindNearestLivingEnemy()
    {
        var hits = Physics2D.OverlapCircleAll(transform.position, extractSearchRadius, enemyLayerMask);

        EnemyController nearest = null;
        float nearestDistance = float.MaxValue;
        foreach (var hit in hits)
        {
            var enemy = hit.GetComponent<EnemyController>();
            if (enemy == null || HealthSystem.IsDead(enemy.stats.health)) continue;

            float distance = Vector2.Distance(transform.position, enemy.transform.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = enemy;
            }
        }
        return nearest;
    }

    // Callback do BloodOrb — chamado no instante exato em que a orb chega no Blood Mage
    // (posição ATUAL dele, não a de quando foi lançada — o jogador pode ter se movido durante
    // a viagem, mesmo critério do teleporte do Mage usar a posição real de chegada).
    private void OnBloodOrbArrived()
    {
        Heal(extractHealAmount);
    }

    // Animation Event, no fim do clipe de reza.
    public void AnimationExtractBloodEndEvent()
    {
        isAttacking = false;
        isUsingSecondaryAbility = false;
    }

    // Sem override de CancelSecondaryAbility — não cancelável (GDD).

    // ===================== Passiva — pet Elemental de Sangue =====================

    // Cópia 1:1 do SummonPet/AnimationSummonPetEvent/AnimationSummonPetEndEvent do Mage —
    // mesmo PetController, mesma API (Seção 0, item 6).
    private void SummonPet()
    {
        if (isAttacking) return;
        isAttacking = true;
        actionElapsed = 0f;
        AnimatorTrigger("SummonPetTrigger");
    }

    public void AnimationSummonPetEvent()
    {
        if (currentPet != null) Destroy(currentPet.gameObject);
        if (petPrefab == null) return;

        Vector3 spawnPos = petSpawnPoint != null ? petSpawnPoint.position : transform.position;
        var obj = Instantiate(petPrefab, spawnPos, Quaternion.identity);
        currentPet = obj.GetComponent<PetController>();
        if (currentPet != null) currentPet.Initialize(transform, petSpawnPoint, petSizeMultiplier, petActionSpeedMultiplier);
    }

    public void AnimationSummonPetEndEvent()
    {
        isAttacking = false;
    }

    // GDD (Seção 52, "Pet retorna junto") — o pet sobrevive à morte do herói e se teleporta
    // sozinho pro térreo (PetController.HandleFloorChanged(), já inscrito em
    // GameEvents.OnFloorChanged) — nenhuma limpeza extra necessária aqui, mesmo critério do Mage.
}
```

## Código — `BloodMageProjectile.cs`

```csharp
using System.Collections.Generic;
using UnityEngine;

// Primário do Blood Mage — projétil reto em 1 de 8 direções fixas, com RESERVA de dano:
// perfura vários monstros até a reserva se esgotar ou a distância máxima ser alcançada, o que
// vier primeiro (GDD Seção 17.10/13). Sem valor de balanceamento próprio — tudo recebido via
// Launch() (convenção desde a Sprint 27b).
public class BloodMageProjectile : MonoBehaviour
{
    [SerializeField] private Animator animator; // opcional — só se houver clipe de impacto dedicado

    private Vector2 direction;
    private float hitDamage; // dano de um hit "cheio" — Seção 0, item 0
    private float remainingReserve;
    private LayerMask enemyLayerMask;
    private float speed;
    private float maxDistance;
    private float distanceTraveled;
    private bool impacted;
    private bool impactHitFired; // dedup do Animation Event do clipe de impacto (Blend Tree, mesma trava de sempre)
    private readonly HashSet<EnemyController> hitEnemies = new HashSet<EnemyController>();

    public void Launch(Vector2 dir, float fullHitDamage, float reserve, LayerMask layerMask, float projectileSpeed, float maxTravelDistance)
    {
        direction = dir.normalized;
        hitDamage = fullHitDamage;
        remainingReserve = reserve;
        enemyLayerMask = layerMask;
        speed = projectileSpeed;
        maxDistance = maxTravelDistance;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void Update()
    {
        if (!GameplayGate.IsActive) return;
        if (impacted) return;

        float step = speed * Time.deltaTime;
        transform.position += (Vector3)(direction * step);
        distanceTraveled += step;
        if (distanceTraveled >= maxDistance) Impact();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (impacted) return;

        var enemy = other.GetComponent<EnemyController>();
        if (enemy == null || hitEnemies.Contains(enemy)) return;
        hitEnemies.Add(enemy);

        float damageDealt = Mathf.Min(hitDamage, remainingReserve);
        enemy.TakeDamage(damageDealt);
        remainingReserve -= damageDealt;

        if (remainingReserve <= 0f) Impact();
    }

    private void Impact()
    {
        if (impacted) return;
        impacted = true;
        impactHitFired = false;
        transform.rotation = Quaternion.identity;

        if (animator != null)
        {
            animator.SetTrigger("ImpactTrigger"); // destruição vem de AnimationImpactEndEvent
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Animation Event, no fim do clipe de impacto (só chamado se houver Animator dedicado).
    public void AnimationImpactEndEvent()
    {
        Destroy(gameObject);
    }
}
```

## Código — `BloodMageShockwave.cs`

```csharp
using System.Collections.Generic;
using UnityEngine;

// Ultimate do Blood Mage — anel de dano em 3 estágios (2×2 → 4×4 → 8×8). O dano viaja com a
// BORDA: cada estágio só acerta quem está dentro da caixa ATUAL e FORA da caixa do estágio
// anterior (zona segura dinâmica, GDD Seção 17.10). Controlador (BloodMage.cs) decide todo
// valor de balanceamento e repassa em Activate() — este componente só guarda o timing da
// própria animação de crescimento.
public class BloodMageShockwave : MonoBehaviour
{
    [SerializeField] private Animator animator; // clipe de crescimento com 3 Animation Events (1 por estágio)

    private float damagePerStage;
    private float stage1Size;
    private float stage2Size;
    private float stage3Size;
    private LayerMask enemyLayerMask;
    private readonly List<EnemyController> stageTargets = new();

    public void Activate(float stageDamage, float size1, float size2, float size3, LayerMask layerMask)
    {
        damagePerStage = stageDamage;
        stage1Size = size1;
        stage2Size = size2;
        stage3Size = size3;
        enemyLayerMask = layerMask;

        if (animator != null) animator.SetTrigger("GrowTrigger"); // os 3 Animation Events abaixo tocam dentro deste clipe
    }

    // Animation Event, no frame exato em que o anel atinge 2×2 — nada a excluir ainda (é o
    // primeiro estágio, não existe zona segura anterior).
    public void AnimationStage1Event()
    {
        ApplyRingDamage(stage1Size, 0f);
    }

    // Animation Event, no frame exato em que o anel atinge 4×4 — exclui quem está dentro do
    // 2×2 (já seguro).
    public void AnimationStage2Event()
    {
        ApplyRingDamage(stage2Size, stage1Size);
    }

    // Animation Event, no frame exato em que o anel atinge 8×8 — exclui quem está dentro do 4×4.
    public void AnimationStage3Event()
    {
        ApplyRingDamage(stage3Size, stage2Size);
    }

    private void ApplyRingDamage(float outerSize, float innerSize)
    {
        var hits = Physics2D.OverlapBoxAll(transform.position, Vector2.one * outerSize, 0f, enemyLayerMask);

        stageTargets.Clear();
        foreach (var hit in hits)
        {
            var enemy = hit.GetComponent<EnemyController>();
            if (enemy == null || stageTargets.Contains(enemy)) continue;

            // Zona segura — já estava dentro da caixa do estágio anterior (Seção 0, item 1).
            if (innerSize > 0f)
            {
                Vector2 offset = (Vector2)enemy.transform.position - (Vector2)transform.position;
                float halfInner = innerSize * 0.5f;
                if (Mathf.Abs(offset.x) < halfInner && Mathf.Abs(offset.y) < halfInner) continue;
            }

            stageTargets.Add(enemy);
        }

        foreach (var enemy in stageTargets) enemy.TakeDamage(damagePerStage);
    }

    // Animation Event, no fim do clipe inteiro (depois do estágio 8×8 terminar de mostrar).
    public void AnimationShockwaveEndEvent()
    {
        Destroy(gameObject);
    }
}
```

## Código — `BloodOrb.cs`

```csharp
using UnityEngine;

// Orb de sangue — nasce no monstro atingido pela extração de sangue e viaja (homing, segue a
// posição ATUAL do Blood Mage, que pode se mover durante a viagem) até ele, curando ao chegar
// via callback (Heal() é protected em HeroController — Seção 0, item 3, mesmo critério do
// MageTeleportProjectile).
public class BloodOrb : MonoBehaviour
{
    [SerializeField] private float arrivalDistance = 0.3f; // 🔢 ajustável — o quão perto de "chegar" já conta como chegada

    private Transform followTarget;
    private float speed;
    private System.Action onArrived;
    private bool arrived;

    public void Launch(Transform target, float orbSpeed, System.Action arrivedCallback)
    {
        followTarget = target;
        speed = orbSpeed;
        onArrived = arrivedCallback;
    }

    private void Update()
    {
        if (!GameplayGate.IsActive) return;
        if (arrived || followTarget == null) return;

        Vector3 toTarget = followTarget.position - transform.position;
        float distance = toTarget.magnitude;

        if (distance <= arrivalDistance)
        {
            Arrive();
            return;
        }

        transform.position += toTarget.normalized * speed * Time.deltaTime;
    }

    private void Arrive()
    {
        if (arrived) return;
        arrived = true;
        onArrived?.Invoke();
        Destroy(gameObject);
    }
}
```

---

## Animator / prefab — notas de setup

- **Attack:** 1 estado (Blend Tree 2D de 8 direções, mesmo critério do Barbarian/Paladin),
  `AnimationProjectileLaunchEvent()` no frame do disparo, `AnimationAttackEndEvent()` no fim.
- **Ultimate:** 1 clipe de salto+queda, `AnimationShockwaveSpawnEvent()` no frame exato da
  queda, `AnimationUltimateEndEvent()` no fim do clipe (o anel já roda sozinho).
- **Anel (`BloodMageShockwave`):** 1 clipe de crescimento com 3 Animation Events
  (`AnimationStage1/2/3Event`) nos frames em que o anel visualmente atinge cada tamanho, e
  `AnimationShockwaveEndEvent()` no fim.
- **Secundária (`extract_blood`):** Blend Tree 2D de 4 diagonais (`ExtractAimX/Y`, mesmo
  critério do `RollAimX/Y`/pose da reza do Cleric), `AnimationExtractBloodEvent()` no fim da
  reza, `AnimationExtractBloodEndEvent()` no fim do clipe.
- **Pet (Elemental de Sangue):** mesmíssimo conjunto de animações da Phoenix (`summon`, `fly`,
  `attack`, `die`) — só a arte muda, Animator Controller pode ser clonado do da Phoenix e só
  trocar as sprites.
- **`BloodMageProjectile`/`BloodOrb`:** sem Animator obrigatório — `BloodMageProjectile` só
  precisa de um se houver clipe de impacto dedicado (senão destrói direto, mesma rede de
  segurança da `RogueBomb`); `BloodOrb` não usa Animator nenhum (só move e some).

---

## Checklist de teste

1. Projétil perfura mais de 1 monstro em linha, cada hit tirando a reserva certa — último
   monstro perfurado pode receber dano PARCIAL se a reserva não cobrir um hit cheio.
2. Projétil se destrói (toca impacto) ao esgotar a reserva, mesmo sem alcançar a distância
   máxima.
3. Projétil se destrói ao alcançar a distância máxima, mesmo com reserva sobrando.
4. Anel: estágio 1 (2×2) acerta todo mundo dentro; estágio 2 (4×4) NÃO acerta de novo quem já
   estava dentro do 2×2 original, só quem está no anel entre 2×2 e 4×4; mesmo critério pro
   estágio 3.
5. Jogador parado no centro (dentro do 2×2) durante os 3 estágios nunca é atingido — zona
   segura funciona (se o Blood Mage puder ser atingido pela própria Ultimate; confirmar se
   aplica dano em monstros apenas, ou também no próprio jogador por engano).
6. Extração de sangue acerta o monstro vivo mais próximo, nunca um morto (cadáver ainda com
   collider enquanto o próprio "die" dele toca).
7. Sem monstro nenhum no raio — extração não quebra (não spawna orb, não cura, não dá erro).
8. Orb viaja até a posição ATUAL do Blood Mage (não a de quando foi lançada) e cura ao chegar.
9. Extração não cancelável — Shift de novo durante não faz nada.
10. Pet (Elemental de Sangue) se comporta exatamente como a Phoenix: trava no alvo até morrer,
    nunca é alvo válido, teleporta ao mudar de Floor, retorna junto na morte do herói.
11. Rede de segurança genérica (`maxActionDuration`) força fim de qualquer ação (primário,
    ultimate, secundária) se o Animation Event de fim nunca chegar.
