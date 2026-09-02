# Sprint 8 — MeleeEnemyPrototype + Death Flow (fase 1)

**Depende de:** Sprint 7.
**Objetivo:** primeiro combate real e completo — inimigo persegue, ataca, recebe dano e morre; Barbarian morre de verdade (HP zero → cancela estados → -30s hook → respawn no térreo com HP cheio e Energia zerada). Fecha a Deadline 2.

---

## 1. Vida — lógica pura (mesmo padrão do `EnergySystem`)

`Assets/Scripts/Core/HealthSystem.cs`:
```csharp
using UnityEngine;

public static class HealthSystem
{
    public static float ApplyDamage(float current, float damage) => Mathf.Max(current - damage, 0f);
    public static bool IsDead(float current) => current <= 0f;
}
```

`Assets/Tests/EditMode/HealthSystemTests.cs`:
```csharp
using NUnit.Framework;

public class HealthSystemTests
{
    [Test]
    public void ApplyDamage_ReducesHealth()
    {
        Assert.AreEqual(70f, HealthSystem.ApplyDamage(100f, 30f));
    }

    [Test]
    public void ApplyDamage_ClampsAtZero()
    {
        Assert.AreEqual(0f, HealthSystem.ApplyDamage(10f, 50f));
    }

    [Test]
    public void IsDead_AtZero_ReturnsTrue()
    {
        Assert.IsTrue(HealthSystem.IsDead(0f));
    }

    [Test]
    public void IsDead_AboveZero_ReturnsFalse()
    {
        Assert.IsFalse(HealthSystem.IsDead(5f));
    }
}
```
Rodar no Test Runner antes de seguir.

## 2. GameEvents — evento de vida

`GameEvents.cs` ganha:
```csharp
public static event Action<float, float> OnHealthChanged;
public static void HealthChanged(float current, float max) => OnHealthChanged?.Invoke(current, max);
```

## 3. EnemyStats + MeleeEnemyPrototype

`Assets/Scripts/Core/EnemyStats.cs`:
```csharp
[System.Serializable]
public class EnemyStats
{
    public float health = 30f;
    public float maxHealth = 30f;
    public float damage = 5f;
    public float moveSpeed = 2f;
    public float observationRadius = 6f;
    public float attackRadius = 1.5f;
    public float attackCooldown = 1f;
}
```

`Assets/Scripts/MeleeEnemyPrototype.cs`:
```csharp
using UnityEngine;

public class MeleeEnemyPrototype : MonoBehaviour
{
    public EnemyStats stats = new EnemyStats();
    public int energyReward = 20; // quanto de Energia concede ao matar (GDD Seção 11, 🔢 valor real pendente)

    private Transform player;
    private float lastAttackTime = -999f;

    private void Start()
    {
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;
    }

    private void Update()
    {
        if (TimeManager.Instance != null && TimeManager.Instance.IsPaused) return;
        if (player == null || HealthSystem.IsDead(stats.health)) return;

        float distance = Vector2.Distance(transform.position, player.position);

        if (distance <= stats.attackRadius) TryAttack();
        else if (distance <= stats.observationRadius) ChasePlayer();
    }

    private void ChasePlayer()
    {
        Vector2 direction = (player.position - transform.position).normalized;
        transform.Translate(direction * stats.moveSpeed * Time.deltaTime);
    }

    private void TryAttack()
    {
        if (Time.time - lastAttackTime < stats.attackCooldown) return;
        lastAttackTime = Time.time;

        var hero = player.GetComponent<HeroController>();
        if (hero == null) return;

        Debug.Log($"[MeleeEnemyPrototype] Ataca o herói por {stats.damage}.");
        hero.TakeDamage(stats.damage);
    }

    public void TakeDamage(float amount)
    {
        stats.health = HealthSystem.ApplyDamage(stats.health, amount);
        Debug.Log($"[MeleeEnemyPrototype] Recebeu {amount} de dano. HP = {stats.health}/{stats.maxHealth}");

        if (HealthSystem.IsDead(stats.health)) Die();
    }

    private void Die()
    {
        Debug.Log("[MeleeEnemyPrototype] Morreu.");
        GameEvents.EnemyKilled(energyReward);
        Destroy(gameObject);
    }
}
```

**Montar na Scene:** criar `Enemy_Test` (sprite placeholder de cor/forma diferente do Player), Tag `Enemy`, Layer `Enemy` (as duas já existem desde a Sprint 2), `CircleCollider2D` (qualquer, trigger ou não — o ataque do Barbarian usa `OverlapCircleAll`, que enxerga o collider de qualquer jeito), componente `MeleeEnemyPrototype`. Posicionar em algum lugar do `Floor_1` (ex.: perto de (0, 55)) — assim só encontra o inimigo depois de usar a escada, aproveitando o trabalho da Sprint 6.

## 4. Barbarian passa a causar dano de verdade

Atualizar `PrimaryAttack()` em `Barbarian.cs`:
```csharp
protected override void PrimaryAttack()
{
    Debug.Log("[Barbarian] Golpe frontal em área.");
    Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, attackRadius);
    foreach (var hit in hits)
    {
        if (!hit.CompareTag("Enemy")) continue;
        var enemy = hit.GetComponent<MeleeEnemyPrototype>();
        if (enemy != null) enemy.TakeDamage(stats.damage);
    }
}
```
O `EnemyKillSimulator` da Sprint 7 continua no projeto — não atrapalha, e segue útil pra testar Energia isoladamente sem precisar reposicionar/matar o inimigo real toda hora.

## 5. Death Flow (fase 1) no HeroController

Adicionar em `HeroController.cs`:
```csharp
public void TakeDamage(float amount)
{
    stats.health = HealthSystem.ApplyDamage(stats.health, amount);
    GameEvents.HealthChanged(stats.health, stats.maxHealth);

    if (HealthSystem.IsDead(stats.health)) Die();
}

private void Die()
{
    Debug.Log("[HeroController] Morreu.");

    // 1. Cancela estados temporários — nenhum existe ainda (hook pra quando ultimates com duração/transformações chegarem)
    // 2. Destrói loot carregado — sem inventário ainda (hook pra Deadline 3 do roadmap)
    // 3. Zera Energia da Ultimate
    stats.energy = 0f;
    GameEvents.EnergyChanged(stats.energy, stats.maxEnergy);

    // 4. Penalidade de -30s no timer do dia — sem Day Timer ainda, só o log por enquanto
    Debug.Log("[HeroController] Penalidade de -30s no timer do dia (placeholder — Day Timer ainda não existe).");

    // 5. Respawn no térreo com HP cheio
    Respawn();
}

private void Respawn()
{
    var ground = FloorRegistry.Instance.Floors.Find(f => f.originalFloorIdentity == 0);
    if (ground != null)
    {
        transform.position = ground.transform.position;
        FloorManager.Instance.SetCurrentFloor(ground);
    }

    stats.health = stats.maxHealth;
    GameEvents.HealthChanged(stats.health, stats.maxHealth);
    Debug.Log("[HeroController] Respawn no térreo com HP cheio.");
}
```
Adicionar também um `Start()` que dispara os eventos iniciais (senão a HUD nasce vazia até o primeiro dano/kill):
```csharp
protected virtual void Start()
{
    GameEvents.HealthChanged(stats.health, stats.maxHealth);
    GameEvents.EnergyChanged(stats.energy, stats.maxEnergy);
}
```

## 6. Indicador de Vida na tela (placeholder)

`Assets/Scripts/UI/HealthIndicatorUI.cs`:
```csharp
using UnityEngine;
using TMPro;

public class HealthIndicatorUI : MonoBehaviour
{
    public TMP_Text label;

    private void OnEnable() => GameEvents.OnHealthChanged += UpdateLabel;
    private void OnDisable() => GameEvents.OnHealthChanged -= UpdateLabel;

    private void UpdateLabel(float current, float max) => label.text = $"HP: {current:F0}/{max:F0}";
}
```
No `Canvas`: mais um `Text - TextMeshPro` (`HealthLabel`, acima do `EnergyLabel`), anexar `HealthIndicatorUI`, arrastar a referência.

---

## 7. Teste manual (Play Mode)

1. Começar no térreo, `HealthLabel` mostra `HP: 100/100`.
2. Subir a escada até o `Floor_1`, aproximar do `Enemy_Test`.
3. Inimigo persegue ao entrar no raio de observação.
4. Deixar ele alcançar o raio de ataque → `HealthLabel` cai a cada ataque (respeitando o cooldown).
5. Atacar de volta com LMB (dentro do `attackRadius`) → Console mostra o inimigo recebendo dano; depois de HP suficiente, ele morre, some da cena, e a Energia sobe (via o mesmo evento do `EnemyKillSimulator`).
6. Deixar o inimigo matar o Barbarian (ou simular repetidamente) → Console mostra a sequência de morte (estados/loot placeholder, Energia zera, penalidade de -30s logada), Barbarian reaparece no térreo com `HP: 100/100`, indicador de Floor volta pra `Floor: Ground`.
7. TAB/Q (pausa) → inimigo para de perseguir/atacar, Barbarian para de andar/atacar.

---

## 8. Git

```
git add .
git commit -m "feat: health system (pure logic) + editmode tests"
```
```
git add .
git commit -m "feat: melee enemy prototype (chase, attack, take damage, die)"
```
```
git add .
git commit -m "feat: barbarian deals real damage + death flow phase 1 (respawn at ground, energy reset)"
```
```
git add .
git commit -m "feat: health HUD placeholder"
git push
```

## 9. Fechamento

`docs/sprints/sprint-08.md` (este arquivo) + linha no `docs/sprints/index.md` + linha no `docs/changelog.md` — e vale marcar aqui o fechamento da **Deadline 2** inteira (Sprints 5-8), já que essa é a última sprint dela.

---

**Pronto quando:** os 4 testes EditMode de `HealthSystem` passam verdes; o inimigo persegue/ataca/recebe dano/morre de verdade; o Barbarian morre, passa pela sequência de Death Flow logada, e reaparece no térreo com HP e Energia zerados; os 3 indicadores de HUD (Floor, Energy, Health) refletem tudo em tempo real; nada disso avança durante a pausa.
