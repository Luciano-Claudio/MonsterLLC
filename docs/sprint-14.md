# Sprint 14 — Attack Budget + Population Skeleton

**Depende de:** Sprint 13.
**Objetivo:** hordas legíveis. Com 10+ inimigos num Floor, só um número limitado ataca ao mesmo tempo (Melee e Ranged com budgets separados, GDD Seção 14) — o resto continua existindo, só não entra em estado de ataque. E os inimigos passam a nascer sozinhos, gradualmente, em vez de serem colocados um a um na mão.

---

## 1. Attack Budget — lógica pura primeiro

`Assets/Scripts/Enemies/AttackType.cs`:
```csharp
public enum AttackType { Melee, Ranged }
```

`Assets/Scripts/Core/AttackBudgetTracker.cs`:
```csharp
public class AttackBudgetTracker
{
    public int MeleeBudget { get; }
    public int RangedBudget { get; }
    public int MeleeInUse { get; private set; }
    public int RangedInUse { get; private set; }

    public AttackBudgetTracker(int meleeBudget, int rangedBudget)
    {
        MeleeBudget = meleeBudget;
        RangedBudget = rangedBudget;
    }

    public bool TryReserve(AttackType type)
    {
        if (type == AttackType.Melee)
        {
            if (MeleeInUse >= MeleeBudget) return false;
            MeleeInUse++;
            return true;
        }

        if (RangedInUse >= RangedBudget) return false;
        RangedInUse++;
        return true;
    }

    public void Release(AttackType type)
    {
        if (type == AttackType.Melee) MeleeInUse = System.Math.Max(0, MeleeInUse - 1);
        else RangedInUse = System.Math.Max(0, RangedInUse - 1);
    }
}
```

`Assets/Tests/EditMode/AttackBudgetTrackerTests.cs`:
```csharp
using NUnit.Framework;

public class AttackBudgetTrackerTests
{
    [Test]
    public void TryReserve_WithinBudget_Succeeds()
    {
        var tracker = new AttackBudgetTracker(meleeBudget: 2, rangedBudget: 1);
        Assert.IsTrue(tracker.TryReserve(AttackType.Melee));
        Assert.IsTrue(tracker.TryReserve(AttackType.Melee));
    }

    [Test]
    public void TryReserve_ExceedsBudget_Fails()
    {
        var tracker = new AttackBudgetTracker(meleeBudget: 1, rangedBudget: 1);
        Assert.IsTrue(tracker.TryReserve(AttackType.Melee));
        Assert.IsFalse(tracker.TryReserve(AttackType.Melee));
    }

    [Test]
    public void Release_FreesSlotForReuse()
    {
        var tracker = new AttackBudgetTracker(meleeBudget: 1, rangedBudget: 1);
        tracker.TryReserve(AttackType.Melee);
        tracker.Release(AttackType.Melee);
        Assert.IsTrue(tracker.TryReserve(AttackType.Melee));
    }

    [Test]
    public void MeleeAndRanged_AreIndependentBudgets()
    {
        var tracker = new AttackBudgetTracker(meleeBudget: 1, rangedBudget: 1);
        Assert.IsTrue(tracker.TryReserve(AttackType.Melee));
        Assert.IsTrue(tracker.TryReserve(AttackType.Ranged)); // não compete com o slot de Melee
    }
}
```
Rodar antes de seguir — os 4 testes cobrem exatamente a regra central da sprint (budgets separados e independentes, GDD Seção 14).

## 2. AttackBudgetManager (singleton, wrapper do tracker)

`Assets/Scripts/Core/AttackBudgetManager.cs`:
```csharp
using UnityEngine;

public class AttackBudgetManager : MonoBehaviour
{
    public static AttackBudgetManager Instance { get; private set; }

    public int meleeBudget = 3; // 🔢 GDD Seção 14 — placeholder, balanceamento real pendente
    public int rangedBudget = 2;

    private AttackBudgetTracker tracker;

    public int MeleeInUse => tracker.MeleeInUse;
    public int RangedInUse => tracker.RangedInUse;

    private void Awake()
    {
        Instance = this;
        tracker = new AttackBudgetTracker(meleeBudget, rangedBudget);
    }

    public bool TryReserveSlot(AttackType type) => tracker.TryReserve(type);
    public void ReleaseSlot(AttackType type) => tracker.Release(type);
}
```
Anexar em `Systems`.

## 3. Conectar no EnemyController (Sprint 13)

`EnemyController.cs` ganha uma propriedade abstrata:
```csharp
protected abstract AttackType AttackType { get; }
```

`MeleeEnemyController`: `protected override AttackType AttackType => AttackType.Melee;`
`RangedEnemyController`: `protected override AttackType AttackType => AttackType.Ranged;`

`TryStartAttack()` passa a reservar slot antes de iniciar o telegraph:
```csharp
private void TryStartAttack()
{
    if (Time.time - lastAttackTime < stats.attackCooldown) return;
    if (!AttackBudgetManager.Instance.TryReserveSlot(AttackType)) return; // sem slot — fica esperando, tenta de novo no próximo frame
    lastAttackTime = Time.time;
    attackState = AttackState.Telegraph;
    attackStateTimer = 0f;
}
```

E `UpdateAttackState()` libera o slot ao terminar o Recovery:
```csharp
case AttackState.Recovery:
    if (attackStateTimer >= attackTiming.recoveryDuration)
    {
        attackState = AttackState.Idle;
        AttackBudgetManager.Instance.ReleaseSlot(AttackType);
    }
    break;
```

**Simplificação assumida nesta sprint:** um inimigo sem slot disponível fica parado tentando de novo a cada frame, em vez de "perseguir/cercar" ativamente enquanto espera (comportamento mais rico que o GDD sugere, mas que exige lógica de posicionamento em grupo — fora do escopo de um skeleton). Ele não fica invisível nem irreagente — só não se move enquanto está no raio de ataque sem slot. Registrado como simplificação consciente, não bug.

**Bosses/Employees/Traps continuam fora do Attack Budget por regra** (GDD Seção 14) — nenhum dos três existe ainda no projeto, mas nenhum código futuro deles deve chamar `AttackBudgetManager`.

## 4. Population Skeleton

`Assets/Scripts/World/PopulationConfig.cs`:
```csharp
[System.Serializable]
public class PopulationConfig
{
    public int minimum = 3;
    public int target = 8;
    public int maximum = 12;
}
```

`Assets/Scripts/World/FloorPopulationManager.cs`:
```csharp
using System.Collections.Generic;
using UnityEngine;

public class FloorPopulationManager : MonoBehaviour
{
    public PopulationConfig config = new PopulationConfig();
    public GameObject meleePrefab;
    public GameObject rangedPrefab;
    public Transform[] spawnPoints;
    public float respawnInterval = 3f; // 🔢 GDD — frequência de reposição, placeholder

    private List<GameObject> aliveEnemies = new();
    private float respawnTimer;

    private void Update()
    {
        if (!GameplayGate.IsActive) return;

        aliveEnemies.RemoveAll(e => e == null); // remove os que já morreram

        if (aliveEnemies.Count >= config.target) return;

        respawnTimer += Time.deltaTime;
        if (respawnTimer < respawnInterval) return;
        respawnTimer = 0f;

        SpawnOne();
    }

    private void SpawnOne()
    {
        if (aliveEnemies.Count >= config.maximum) return;
        if (spawnPoints.Length == 0) return;

        var point = spawnPoints[Random.Range(0, spawnPoints.Length)];
        var prefab = Random.value < 0.5f ? meleePrefab : rangedPrefab;
        if (prefab == null) return;

        aliveEnemies.Add(Instantiate(prefab, point.position, Quaternion.identity));
    }
}
```
**Simplificação assumida:** "posições válidas, fora da câmera, longe do jogador" (GDD Seção 23) fica reduzido a "sorteia entre pontos pré-posicionados manualmente no Editor" — o level designer já escolhe posições razoáveis ao criar os `spawnPoints`, então checagem automática de câmera/distância não é necessária ainda. A **reação dinâmica** ("jogador matando rápido → aumenta frequência de reposição") também não está implementada — `respawnInterval` é fixo. Ambas ficam como pendência de balanceamento/comportamento, não bloqueiam esta sprint.

**Montar no `Floor_1`:** criar `PopulationManager_Floor1` com o script acima, `meleePrefab`/`rangedPrefab` apontando pros prefabs do `MeleeEnemyController`/`RangedEnemyController` (Sprint 13), e 5-6 `Transform` vazios espalhados pela área do Floor como `spawnPoints`. Remover o `Enemy_Test`/`Enemy_Ranged_Test` fixos desse Floor — agora eles nascem sozinhos.

## 5. Indicador de debug do budget (não é HUD "real" do GDD — só pra validar a sprint)

`Assets/Scripts/UI/AttackBudgetIndicatorUI.cs`:
```csharp
using UnityEngine;
using TMPro;

public class AttackBudgetIndicatorUI : MonoBehaviour
{
    public TMP_Text label;

    private void Update()
    {
        if (AttackBudgetManager.Instance == null) return;
        label.text = $"Melee: {AttackBudgetManager.Instance.MeleeInUse}/{AttackBudgetManager.Instance.meleeBudget} | " +
                      $"Ranged: {AttackBudgetManager.Instance.RangedInUse}/{AttackBudgetManager.Instance.rangedBudget}";
    }
}
```
Mais um `Text - TextMeshPro` no `Canvas`. Este indicador é ferramenta de debug pra provar a sprint, não um elemento de HUD definitivo do GDD — pode sair quando não fizer mais falta.

---

## 6. Teste manual (Play Mode)

1. Entrar no `Floor_1` e esperar — inimigos começam a aparecer gradualmente nos `spawnPoints`, não todos de uma vez.
2. Deixar a população chegar perto de 10-12 (`minimum`/`target`/`maximum` = 3/8/12).
3. Se aproximar de um grupo grande → Console mostra vários `Telegraph...`, mas o indicador de budget nunca passa de `Melee: 3/3` / `Ranged: 2/2` ao mesmo tempo, mesmo com mais inimigos no raio de ataque.
4. Matar um inimigo que estava atacando → o slot libera (indicador cai), e outro inimigo que estava esperando começa seu próprio telegraph no frame seguinte.
5. Matar inimigos até ficar abaixo do `target` → depois de `respawnInterval` segundos, um novo aparece sozinho num `spawnPoint` aleatório.
6. Rodar os 4 testes `AttackBudgetTrackerTests` no Test Runner — todos verdes.

---

## 7. Git

```
git add .
git commit -m "feat: attack budget tracker (pure logic) + editmode tests"
```
```
git add .
git commit -m "feat: attack budget manager wired into enemy controller (melee/ranged separate slots)"
```
```
git add .
git commit -m "feat: floor population skeleton (gradual spawn, min/target/max)"
```
```
git add .
git commit -m "feat: attack budget debug indicator"
git push
```

## 8. Fechamento

`docs/sprints/sprint-14.md` (este arquivo) + linha no `docs/sprints/index.md` + linha no `docs/changelog.md`.

---

**Pronto quando:** os 4 testes de `AttackBudgetTracker` passam verdes; com 10+ inimigos no Floor, nunca mais que `meleeBudget` Melee e `rangedBudget` Ranged atacam ao mesmo tempo, independente de quantos estejam fisicamente no raio de ataque; a população nasce sozinha e gradualmente, respeitando `minimum`/`target`/`maximum`.
