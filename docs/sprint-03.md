# Sprint 3 — GameEvents + Time/Pause + Game State + Progress Tracker Skeleton + Testes

**Depende de:** Sprint 2.
**Objetivo:** o núcleo reativo do jogo existe — eventos centralizados, pausa que realmente para o tempo, e um contador de progresso que reage a eventos. Mais um teste automatizado provando que a engrenagem gira.

---

## 1. GameEvents (o catálogo central)

`Assets/Scripts/Core/GameEvents.cs`:
```csharp
using System;

public static class GameEvents
{
    public static event Action OnEnemyKilled;
    public static void EnemyKilled() => OnEnemyKilled?.Invoke();

    // Mais eventos entram aqui conforme os sistemas nascerem.
    // Nenhum outro script deve declarar um event solto — tudo passa por aqui.
}
```

## 2. TimeManager + GameStateManager

`Assets/Scripts/Core/TimeManager.cs`:
```csharp
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance { get; private set; }
    public bool IsPaused { get; private set; }

    private void Awake() => Instance = this;

    public void Pause() => IsPaused = true;
    public void Resume() => IsPaused = false;
    public void TogglePause() => IsPaused = !IsPaused;
}
```

`Assets/Scripts/Core/GameStateManager.cs`:
```csharp
public enum GameState { Gameplay, Paused }

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }
    public GameState CurrentState { get; private set; } = GameState.Gameplay;

    private void Awake() => Instance = this;
    public void SetState(GameState state) => CurrentState = state;
}
```

## 3. ProgressTracker skeleton

`Assets/Scripts/Core/ProgressTracker.cs`:
```csharp
using UnityEngine;
using System.Collections.Generic;

public static class ProgressTracker
{
    private static Dictionary<string, int> counters = new();
    private static bool initialized;

    public static void Init()
    {
        if (initialized) return;
        initialized = true;
        GameEvents.OnEnemyKilled += () => Increment("EnemyKilled");
    }

    public static void Increment(string key)
    {
        if (!counters.ContainsKey(key)) counters[key] = 0;
        counters[key]++;
        Debug.Log($"[ProgressTracker] {key} = {counters[key]}");
    }

    public static int Get(string key) => counters.TryGetValue(key, out var v) ? v : 0;
    public static void ResetAll() => counters.Clear(); // usado nos testes
}
```

## 4. TestTimer (pra ver a pausa funcionando de verdade)

`Assets/Scripts/Core/TestTimer.cs`:
```csharp
using UnityEngine;

public class TestTimer : MonoBehaviour
{
    public float timeRemaining = 100f;

    private void Update()
    {
        if (TimeManager.Instance != null && TimeManager.Instance.IsPaused) return;

        timeRemaining -= Time.deltaTime;
        if (Mathf.FloorToInt(timeRemaining * 10) % 10 == 0)
            Debug.Log($"Time remaining: {timeRemaining:F1}");
    }
}
```

## 5. Ligar tudo (SystemsBootstrap)

Na Hierarchy, dentro de `//SYSTEMS`, criar um GameObject vazio `Systems`. Anexar `TimeManager`, `GameStateManager`, `TestTimer`, e este novo script:

`Assets/Scripts/Core/SystemsBootstrap.cs`:
```csharp
using UnityEngine;

public class SystemsBootstrap : MonoBehaviour
{
    private PlayerControls controls;

    private void Awake()
    {
        ProgressTracker.Init();
        controls = new PlayerControls();

        controls.Gameplay.Inventory.performed += ctx => TimeManager.Instance.TogglePause();
        controls.Gameplay.RemoteControl.performed += ctx => TimeManager.Instance.TogglePause();
        controls.Gameplay.Interact.performed += ctx => GameEvents.EnemyKilled(); // placeholder até existir inimigo real
    }

    private void OnEnable() => controls.Enable();
    private void OnDisable() => controls.Disable();
}
```
Não mexe no `PlayerInputTest.cs` do Sprint 2 — ele continua só logando, esse script novo é quem efetivamente aciona o sistema.

Testar: apertar **E** várias vezes → Console mostra `[ProgressTracker] EnemyKilled = 1, 2, 3...`. Apertar **TAB** ou **Q** → o log do `TestTimer` para de avançar até apertar de novo.

## 6. Test Framework + 1 teste automatizado

1. `Window > General > Test Runner`.
2. Aba **EditMode** → botão **"Create EditMode Test Assembly Folder"** (o Unity cria `Assets/Tests/EditMode/` já com o assembly definition certo).
3. Abrir o `.asmdef` gerado → em **Assembly Definition References**, adicionar `Assembly-CSharp` (pra ele enxergar `GameEvents`/`ProgressTracker`).
4. Criar `Assets/Tests/EditMode/ProgressTrackerTests.cs`:
```csharp
using NUnit.Framework;

public class ProgressTrackerTests
{
    [Test]
    public void EnemyKilled_IncrementsCounter()
    {
        ProgressTracker.ResetAll();
        ProgressTracker.Init();

        GameEvents.EnemyKilled();

        Assert.AreEqual(1, ProgressTracker.Get("EnemyKilled"));
    }
}
```
5. No Test Runner, rodar o teste → deve passar verde.

## 7. Git

```
git add .
git commit -m "feat: GameEvents, TimeManager, GameStateManager, ProgressTracker skeleton"
```
```
git add .
git commit -m "test: add EditMode test for ProgressTracker + GameEvents"
git push
```

## 8. Fechamento

`docs/sprints/sprint-03.md` (este arquivo) + linha no `docs/sprints/index.md` + linha no `docs/changelog.md`.

---

**Pronto quando:** apertar E incrementa o `ProgressTracker` no Console; apertar TAB/Q pausa e despausa o `TestTimer`; o teste EditMode passa verde no Test Runner.
