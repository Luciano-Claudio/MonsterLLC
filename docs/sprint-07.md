# Sprint 7 — Hero Framework + Barbarian

**Depende de:** Sprint 6.
**Objetivo:** primeiro herói jogável de verdade (move, ataca em área, usa ultimate), e a regra de Energia da Ultimate (GDD Seção 11) implementada e testada — mesmo sem inimigo real ainda (isso é Sprint 8).

> Como não existe inimigo de verdade nesta sprint, a Energia é testada com um **simulador de kill** (debug), não com combate real. O golpe do Barbarian já procura alvos com Tag `Enemy`, então quando o `MeleeEnemyPrototype` da Sprint 8 existir, o dano começa a valer sem precisar tocar nesse código de novo.

---

## 1. Energia da Ultimate — lógica pura primeiro

`Assets/Scripts/Core/EnergySystem.cs`:
```csharp
using UnityEngine;

public static class EnergySystem
{
    public static float AddEnergy(float current, float max, float amount) =>
        Mathf.Min(current + amount, max);

    public static bool IsReady(float current, float max) => current >= max;
}
```

`Assets/Tests/EditMode/EnergySystemTests.cs`:
```csharp
using NUnit.Framework;

public class EnergySystemTests
{
    [Test]
    public void AddEnergy_CapsAtMax()
    {
        Assert.AreEqual(100f, EnergySystem.AddEnergy(90f, 100f, 50f));
    }

    [Test]
    public void AddEnergy_BelowMax_AddsNormally()
    {
        Assert.AreEqual(70f, EnergySystem.AddEnergy(50f, 100f, 20f));
    }

    [Test]
    public void IsReady_AtMax_ReturnsTrue()
    {
        Assert.IsTrue(EnergySystem.IsReady(100f, 100f));
    }

    [Test]
    public void IsReady_BelowMax_ReturnsFalse()
    {
        Assert.IsFalse(EnergySystem.IsReady(80f, 100f));
    }
}
```
Rodar no Test Runner antes de seguir — mesmo padrão da Sprint 6 (lógica pura testada antes de virar comportamento na Scene).

## 2. GameEvents — 2 eventos novos

`GameEvents.cs` ganha:
```csharp
public static event Action<int> OnEnemyKilled;
public static void EnemyKilled(int energyValue) => OnEnemyKilled?.Invoke(energyValue);

public static event Action<float, float> OnEnergyChanged;
public static void EnergyChanged(float current, float max) => OnEnergyChanged?.Invoke(current, max);
```

**Mudança que quebra compatibilidade, de propósito:** `OnEnemyKilled` passa a carregar o valor de energia daquele kill (`int`), em vez de não carregar nada. É o GDD Seção 11 dizendo "monstros mais fortes concedem mais Energia por kill" (🔢 valor exato pendente) — melhor a assinatura já nascer certa agora do que o `MeleeEnemyPrototype` da Sprint 8 forçar mudar de novo. Ajustar a assinatura do `ProgressTracker` (Sprint 3) de acordo:
```csharp
GameEvents.OnEnemyKilled += (energy) => Increment("EnemyKilled");
```

## 3. HeroController (base) + HeroStats

`Assets/Scripts/Core/HeroStats.cs`:
```csharp
[System.Serializable]
public class HeroStats
{
    public float health = 100f;
    public float maxHealth = 100f;
    public float damage = 10f;
    public float critChance = 0.1f;
    public float moveSpeed = 5f;
    public float attackSpeed = 1f;
    public float energy = 0f;
    public float maxEnergy = 100f;
}
```

`Assets/Scripts/Core/HeroController.cs`:
```csharp
using UnityEngine;

public abstract class HeroController : MonoBehaviour
{
    public HeroStats stats = new HeroStats();
    protected PlayerControls controls;
    private Vector2 moveInput;

    protected virtual void Awake()
    {
        controls = new PlayerControls();
        controls.Gameplay.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        controls.Gameplay.Move.canceled += ctx => moveInput = Vector2.zero;
        controls.Gameplay.Attack.performed += ctx => { if (!IsPaused()) PrimaryAttack(); };
        controls.Gameplay.Ultimate.performed += ctx => { if (!IsPaused()) TryUseUltimate(); };
    }

    protected virtual void OnEnable()
    {
        controls.Enable();
        GameEvents.OnEnemyKilled += HandleEnemyKilled;
    }

    protected virtual void OnDisable()
    {
        controls.Disable();
        GameEvents.OnEnemyKilled -= HandleEnemyKilled;
    }

    protected virtual void Update()
    {
        if (IsPaused()) return;
        transform.Translate(moveInput * stats.moveSpeed * Time.deltaTime);
    }

    protected bool IsPaused() => TimeManager.Instance != null && TimeManager.Instance.IsPaused;

    private void HandleEnemyKilled(int energyValue)
    {
        stats.energy = EnergySystem.AddEnergy(stats.energy, stats.maxEnergy, energyValue);
        GameEvents.EnergyChanged(stats.energy, stats.maxEnergy);
    }

    private void TryUseUltimate()
    {
        if (!EnergySystem.IsReady(stats.energy, stats.maxEnergy)) return;
        UseUltimate();
        stats.energy = 0f;
        GameEvents.EnergyChanged(stats.energy, stats.maxEnergy);
    }

    // Hook pra Sprint 8 (Death Flow) — ainda não chamado por ninguém nesta sprint.
    public void OnDeath()
    {
        stats.energy = 0f;
        GameEvents.EnergyChanged(stats.energy, stats.maxEnergy);
    }

    protected abstract void PrimaryAttack();
    protected abstract void UseUltimate();
}
```

## 4. Barbarian (concreto)

`Assets/Scripts/Barbarian.cs`:
```csharp
using UnityEngine;

public class Barbarian : HeroController
{
    public float attackRadius = 2f;
    public int ultimateBurstCount = 8;
    public float ultimateBurstRadius = 3f;

    protected override void PrimaryAttack()
    {
        Debug.Log("[Barbarian] Golpe frontal em área.");
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, attackRadius);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Enemy"))
                Debug.Log($"[Barbarian] Atingiu {hit.name} (dano placeholder: {stats.damage})");
        }
    }

    protected override void UseUltimate()
    {
        Debug.Log("[Barbarian] Ultimate — salto + rajada em área.");
        for (int i = 0; i < ultimateBurstCount; i++)
        {
            float angle = i * (360f / ultimateBurstCount) * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            Debug.DrawRay(transform.position, dir * ultimateBurstRadius, Color.red, 1f);
        }
    }
}
```
`PrimaryAttack` não acha nada com Tag `Enemy` ainda (não existe inimigo até a Sprint 8) — o log roda vazio, e é esperado. `UseUltimate` desenha os raios da rajada via `Debug.DrawRay` (visível na aba Scene com Gizmos ligado) só pra confirmar a direção/quantidade, sem efeito de dano ainda.

**No Player da Scene:** remover o componente `PlayerInputTest` (Sprint 2 — cumpriu o papel dele, agora conflitaria por controlar o mesmo `transform` junto do `HeroController`), anexar `Barbarian` no lugar.

## 5. Simulador de kill (debug, pra testar Energia sem inimigo real)

`Assets/Scripts/EnemyKillSimulator.cs`:
```csharp
using UnityEngine;

public class EnemyKillSimulator : MonoBehaviour
{
    public int energyPerKill = 20; // placeholder — valor real varia por monstro (GDD Seção 11, 🔢)

    [ContextMenu("Simulate Enemy Killed")]
    public void SimulateKill() => GameEvents.EnemyKilled(energyPerKill);
}
```
Anexar em `Systems`.

## 6. Indicador de Energia na tela (placeholder)

`Assets/Scripts/UI/EnergyIndicatorUI.cs`:
```csharp
using UnityEngine;
using TMPro;

public class EnergyIndicatorUI : MonoBehaviour
{
    public TMP_Text label;

    private void OnEnable() => GameEvents.OnEnergyChanged += UpdateLabel;
    private void OnDisable() => GameEvents.OnEnergyChanged -= UpdateLabel;

    private void UpdateLabel(float current, float max) => label.text = $"Energy: {current:F0}/{max:F0}";
}
```
No `Canvas` já existente (Sprint 6): criar mais um `Text - TextMeshPro` (`EnergyLabel`, abaixo do `FloorLabel`), anexar `EnergyIndicatorUI`, arrastar a referência.

---

## 7. Teste manual (Play Mode)

1. WASD move o Barbarian normalmente.
2. LMB → loga "Golpe frontal em área" (sem acertar nada, ainda não existe `Enemy`).
3. RMB com energia em 0 → nada acontece (guarda de `EnergySystem.IsReady` bloqueia).
4. Botão direito em `EnemyKillSimulator` → "Simulate Enemy Killed" **5 vezes** (20 × 5 = 100) → `EnergyLabel` sobe até `Energy: 100/100`.
5. RMB → Console loga "Ultimate — salto + rajada em área", raios aparecem na aba Scene (Gizmos ligado), `EnergyLabel` volta pra `Energy: 0/100`.
6. TAB ou Q (pausa) → segurar LMB/RMB não faz nada enquanto pausado; WASD também para.

---

## 8. Git

```
git add .
git commit -m "feat: energy system (pure logic) + editmode tests"
```
```
git add .
git commit -m "feat: hero framework (HeroController + HeroStats) + GameEvents energy/kill payload"
```
```
git add .
git commit -m "feat: barbarian (primary attack + ultimate) + energy HUD + kill simulator"
git push
```

## 9. Fechamento

`docs/sprints/sprint-07.md` (este arquivo) + linha no `docs/sprints/index.md` + linha no `docs/changelog.md`.

---

**Pronto quando:** os 4 testes EditMode de `EnergySystem` passam verdes; Barbarian anda, ataca (loga) e usa a ultimate (loga + desenha os raios); a Energia só sobe via `EnemyKillSimulator`, satura em 100, zera ao usar a ultimate, e o indicador na tela reflete tudo isso em tempo real; nada disso avança enquanto o jogo está pausado.
