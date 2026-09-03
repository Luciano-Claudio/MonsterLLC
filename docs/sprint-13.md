# Sprint 13 — Enemy Framework Genérico

**Depende de:** Sprint 12.
**Objetivo:** generalizar `MeleeEnemyPrototype` (Sprint 8) numa base reutilizável de inimigo, com timing/telegraph configurável de verdade (GDD Seção 22 — dano não acontece mais no instante em que o ataque "começa", segue a linha do tempo Telegraph → Hitbox Ativa → Recovery). Primeira sprint da Deadline 4. Também inclui o **gating de `GameState`** que ficou como dívida sem sprint atribuída desde a Sprint 11.

> Esta sprint fecha uma dívida de 5 sprints atrás: o telegraph configurável que faltava no `MeleeEnemyPrototype` desde o relatório da Sprint 8 ("framework completo é escopo da Deadline 4") é exatamente o que nasce aqui.

---

## 1. GameplayGate — o gating combinado, primeiro

Antes de tocar em inimigo, resolver a dívida da Sprint 11/12 (nada parava de verdade fora do estado `Gameplay`). Em vez de espalhar um segundo `if` em cada `Update()`, centralizar num único ponto:

`Assets/Scripts/Core/GameplayGate.cs`:
```csharp
public static class GameplayGate
{
    public static bool IsActive =>
        (TimeManager.Instance == null || !TimeManager.Instance.IsPaused) &&
        (GameStateManager.Instance == null || GameStateManager.Instance.CurrentState == GameState.Gameplay);
}
```
Pura o suficiente pra viver em `Core/` (só referencia `TimeManager`/`GameStateManager`, os dois já lá).

**Atualizar `HeroController.cs` (Sprint 7):** remover o helper antigo `IsPaused()` (só checava `TimeManager`) e trocar todos os usos por `GameplayGate.IsActive`:
```csharp
protected virtual void Update()
{
    if (!GameplayGate.IsActive) return;
    transform.Translate(moveInput * stats.moveSpeed * Time.deltaTime);
}
```
Mesma troca nos callbacks de `Attack`/`Ultimate` no `Awake()`.

**Efeito esperado:** durante `Results`/`Shop`/`GameOver`, o Barbarian para de responder a WASD/LMB/RMB — antes ele continuava andando e atacando mesmo com a tela "oficialmente" em outro estado.

## 2. Timing de ataque — a peça central da sprint

`Assets/Scripts/Enemies/AttackTiming.cs`:
```csharp
[System.Serializable]
public class AttackTiming
{
    public float telegraphDuration = 0.5f;
    public float hitboxActiveDuration = 0.2f;
    public float recoveryDuration = 0.3f;
}
```

`Assets/Scripts/Enemies/EnemyController.cs` (base abstrata — substitui `MeleeEnemyPrototype` como ponto central):
```csharp
using UnityEngine;

public abstract class EnemyController : MonoBehaviour
{
    public EnemyStats stats = new EnemyStats();
    public AttackTiming attackTiming = new AttackTiming();
    public int energyReward = 20;

    protected Transform player;
    private float lastAttackTime = -999f;
    private bool isDead;

    private enum AttackState { Idle, Telegraph, Active, Recovery }
    private AttackState attackState = AttackState.Idle;
    private float attackStateTimer;

    protected virtual void Start()
    {
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;
    }

    protected virtual void Update()
    {
        if (!GameplayGate.IsActive) return;
        if (isDead || player == null) return;

        if (attackState != AttackState.Idle)
        {
            UpdateAttackState();
            return; // travado durante o próprio ataque — não persegue nem re-ataca no meio do timing
        }

        float distance = Vector2.Distance(transform.position, player.position);

        if (distance <= stats.attackRadius) TryStartAttack();
        else if (distance <= stats.observationRadius) Move();
    }

    protected abstract void Move();
    protected abstract void ExecuteHit(); // dano de verdade acontece aqui — no instante certo do timing, não no início do ataque

    private void TryStartAttack()
    {
        if (Time.time - lastAttackTime < stats.attackCooldown) return;
        lastAttackTime = Time.time;
        attackState = AttackState.Telegraph;
        attackStateTimer = 0f;
        Debug.Log($"[{GetType().Name}] Telegraph...");
    }

    private void UpdateAttackState()
    {
        attackStateTimer += Time.deltaTime;

        switch (attackState)
        {
            case AttackState.Telegraph:
                if (attackStateTimer >= attackTiming.telegraphDuration)
                {
                    ExecuteHit();
                    attackState = AttackState.Active;
                    attackStateTimer = 0f;
                }
                break;

            case AttackState.Active:
                if (attackStateTimer >= attackTiming.hitboxActiveDuration)
                {
                    attackState = AttackState.Recovery;
                    attackStateTimer = 0f;
                }
                break;

            case AttackState.Recovery:
                if (attackStateTimer >= attackTiming.recoveryDuration)
                    attackState = AttackState.Idle;
                break;
        }
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;
        stats.health = HealthSystem.ApplyDamage(stats.health, amount);
        Debug.Log($"[{GetType().Name}] Recebeu {amount} de dano. HP = {stats.health}/{stats.maxHealth}");
        if (HealthSystem.IsDead(stats.health)) Die();
    }

    protected virtual void Die()
    {
        isDead = true;
        Debug.Log($"[{GetType().Name}] Morreu.");
        GameEvents.EnemyKilled(energyReward);
        Destroy(gameObject);
    }
}
```

**Decisão importante — por que máquina de estado manual em `Update()`, não Coroutine:** a primeira versão óbvia disso usaria `IEnumerator` + `WaitForSeconds`. Não fazer isso: Coroutines do Unity não respeitam o `GameplayGate` — `WaitForSeconds` continua contando durante a pausa, porque o projeto pausa via flag manual (`TimeManager.IsPaused`), não via `Time.timeScale`. Um inimigo no meio do telegraph continuaria avançando o timer com o jogo pausado, e completaria o golpe assim que o jogador reabrisse a Bag. A máquina de estado manual dentro do próprio `Update()` (que já é gateado no topo) resolve isso de graça, sem precisar tocar em `Time.timeScale` — mudança maior, fora do escopo desta sprint.

**Nota de precisão aceita, não corrigida:** `Time.time` continua avançando durante a pausa (é tempo real da engine, não gateado). Isso significa que `lastAttackTime` (cooldown entre ataques) pode "parecer" ter passado mais tempo do que realmente se passou em jogo, caso o jogador pause bem no meio da janela de cooldown. Prático: com cooldowns curtos (~1s) e pausas tipicamente breves, o efeito é imperceptível — registrado aqui como imprecisão conhecida e aceita, não um bug a perseguir agora. Já existia desde o `MeleeEnemyPrototype` original (Sprint 8), não é introduzido por esta sprint.

## 3. Melee e Ranged concretos

`Assets/Scripts/Enemies/MeleeEnemyController.cs`:
```csharp
using UnityEngine;

public class MeleeEnemyController : EnemyController
{
    protected override void Move()
    {
        Vector2 direction = (player.position - transform.position).normalized;
        transform.Translate(direction * stats.moveSpeed * Time.deltaTime);
    }

    protected override void ExecuteHit()
    {
        var hero = player.GetComponent<HeroController>();
        if (hero == null) return;
        Debug.Log($"[MeleeEnemyController] Ataca o herói por {stats.damage}.");
        hero.TakeDamage(stats.damage);
    }
}
```

`Assets/Scripts/Enemies/RangedEnemyController.cs`:
```csharp
using UnityEngine;

public class RangedEnemyController : EnemyController
{
    public float projectileSpeed = 6f;
    public GameObject projectilePrefab;

    protected override void Move()
    {
        Vector2 toPlayer = player.position - transform.position;

        // Mantém distância: se está muito perto, afasta; senão, se aproxima até o raio de ataque.
        Vector2 dir = toPlayer.magnitude < stats.attackRadius * 0.8f ? -toPlayer.normalized : toPlayer.normalized;
        transform.Translate(dir * stats.moveSpeed * Time.deltaTime);
    }

    protected override void ExecuteHit()
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning("[RangedEnemyController] Sem projectilePrefab — dano aplicado direto como fallback.");
            player.GetComponent<HeroController>()?.TakeDamage(stats.damage);
            return;
        }

        var projObj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        var proj = projObj.GetComponent<EnemyProjectile>() ?? projObj.AddComponent<EnemyProjectile>();
        proj.direction = (player.position - transform.position).normalized;
        proj.speed = projectileSpeed;
        proj.damage = stats.damage;
    }
}
```

`Assets/Scripts/Enemies/EnemyProjectile.cs`:
```csharp
using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    public Vector2 direction;
    public float speed = 6f;
    public float damage = 5f;
    public float lifetime = 3f;

    private float timer;

    private void Update()
    {
        if (!GameplayGate.IsActive) return;

        transform.Translate(direction * speed * Time.deltaTime);
        timer += Time.deltaTime;
        if (timer >= lifetime) Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        other.GetComponent<HeroController>()?.TakeDamage(damage);
        Destroy(gameObject);
    }
}
```

**Montar na Scene:**
- `Enemy_Test` (Sprint 8): trocar o componente `MeleeEnemyPrototype` por `MeleeEnemyController` (mesmos valores de `EnemyStats` no Inspector).
- Criar `Enemy_Ranged_Test`: sprite placeholder diferente, `RangedEnemyController`, `Tag = Enemy`, `Layer = Enemy`.
- Criar um Prefab simples `EnemyProjectile_Placeholder` (sprite pequeno, `CircleCollider2D` marcado **Is Trigger**), arrastar no campo `Projectile Prefab` do `RangedEnemyController`.

## 4. Remover `MeleeEnemyPrototype.cs`

Depois de confirmar `MeleeEnemyController` funcionando na Scene, apagar `Assets/Scripts/Enemies/MeleeEnemyPrototype.cs` — ele cumpriu o papel desde a Sprint 8, mas manter os dois lado a lado só criaria confusão sobre qual é "o" inimigo real do projeto.

---

## 5. Teste manual (Play Mode)

1. Aproximar do `Enemy_Test` (Melee) → Console mostra `Telegraph...` **antes** do dano acontecer, com um delay perceptível (não instantâneo como na Sprint 8).
2. Aproximar do `Enemy_Ranged_Test` → ele mantém distância, dispara um projétil visível que viaja até acertar o Barbarian.
3. **Testar o gating:** provocar o fim de um dia (demanda cumprida ou tempo esgotado) e, enquanto a tela estiver logicamente em `Results`/`Shop`, tentar mover o Barbarian (WASD) e atacar (LMB) → nada deve acontecer. Os inimigos também devem parar de perseguir/atacar nesse período.
4. Pausar (TAB) no meio do telegraph de um inimigo → o log de `Telegraph...` não avança pro hit enquanto pausado; despausar completa o timing normalmente, sem "pular" o dano acumulado.
5. Matar os dois tipos de inimigo → ambos soltam Energia (`GameEvents.EnemyKilled`) e loot normalmente (herdado das sprints anteriores).

---

## 6. Git

```
git add .
git commit -m "feat: gameplay gate (combined pause + gamestate check), applied to hero"
```
```
git add .
git commit -m "feat: enemy framework (EnemyController base, telegraph/hitbox/recovery timing)"
```
```
git add .
git commit -m "feat: melee and ranged enemy controllers + enemy projectile"
```
```
git add .
git commit -m "chore: remove MeleeEnemyPrototype, replaced by EnemyController"
git push
```

## 7. Fechamento

`docs/sprints/sprint-13.md` (este arquivo) + linha no `docs/sprints/index.md` + linha no `docs/changelog.md`.

Registrar explicitamente no relatório: a ressalva sobre `HeroController` chamar `Respawn()` incondicionalmente (Sprint 11) **continua em aberto, não decidida nesta sprint** — o gating agora existe, o que era o pré-requisito técnico pra reavaliar aquela decisão, mas reavaliar não é o mesmo que já ter reavaliado. Deixar isso para uma sprint futura explícita se/quando quiser reabrir.

---

**Pronto quando:** os dois tipos de inimigo (Melee e Ranged) usam a mesma base (`EnemyController`) com timing de ataque real (telegraph visível antes do dano); nada no jogo (herói ou inimigo) continua agindo fora do estado `Gameplay`; a pausa não "acelera" nem "pula" o timing de ataque de ninguém.
