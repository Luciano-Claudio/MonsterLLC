# Sprint 11 — Demanda + Results + Shop Skeleton + Game Over Funcional

**Depende de:** Sprint 10.
**Objetivo:** o ciclo econômico do dia fecha de ponta a ponta — inclusive o caminho de falha. É aqui que o **Day Timer nasce de verdade** (não "em algum ponto da Deadline 3" — nesta sprint específica), o que destrava duas dívidas antigas de uma vez: a penalidade de -30s real (Sprint 7/8) e a destruição de loot no Death Flow (Sprint 8).

---

## 0. Limpeza rápida antes de começar

`GameEvents.OnLootCollected` (Sprint 9) ficou sem nenhum assinante depois que a Sprint 10 reescreveu `LootDrop.Collect()` para usar o `BagController` — erro meu no task breakdown daquela sprint, não decisão sua. Remover a declaração do evento agora (`Assets/Scripts/Core/GameEvents.cs`), já que não tem custo nenhum esperar isso virar "dívida formal" quando dá pra apagar em 1 minuto.

---

## 1. Demanda diária — fórmula pura primeiro

`Assets/Scripts/Core/DemandCalculator.cs`:
```csharp
public static class DemandCalculator
{
    public static int GetDemand(int day) => (int)(40 * System.Math.Pow(2, day - 1));
}
```

`Assets/Tests/EditMode/DemandCalculatorTests.cs`:
```csharp
using NUnit.Framework;

public class DemandCalculatorTests
{
    [Test]
    public void GetDemand_Day1_Returns40() => Assert.AreEqual(40, DemandCalculator.GetDemand(1));

    [Test]
    public void GetDemand_Day2_Returns80() => Assert.AreEqual(80, DemandCalculator.GetDemand(2));

    [Test]
    public void GetDemand_Day5_Returns640() => Assert.AreEqual(640, DemandCalculator.GetDemand(5));
}
```

## 2. GameEvents — os novos desta sprint

Adicionar em `GameEvents.cs`:
```csharp
public static event Action<float> OnTimeChanged;
public static void TimeChanged(float time) => OnTimeChanged?.Invoke(time);

public static event Action<int, int> OnDemandChanged;
public static void DemandChanged(int sold, int target) => OnDemandChanged?.Invoke(sold, target);

public static event Action<GameState> OnGameStateChanged;
public static void GameStateChanged(GameState state) => OnGameStateChanged?.Invoke(state);
```
`OnGameStateChanged` não estava no plano original — precisou entrar porque `Results`/`Shop`/`GameOver` (abaixo) reagem à troca de estado, e sem esse evento cada um teria que ficar checando `GameStateManager.CurrentState` por polling no `Update()`. Atualizar `GameStateManager.SetState()` (Sprint 9) pra disparar o evento:
```csharp
public void SetState(GameState state)
{
    CurrentState = state;
    GameEvents.GameStateChanged(state);
}
```

## 3. DayTimer

`Assets/Scripts/Core/DayTimer.cs`:
```csharp
using UnityEngine;

public class DayTimer : MonoBehaviour
{
    public static DayTimer Instance { get; private set; }
    public float timeRemaining = 100f;
    private bool dayEnded;

    private void Awake() => Instance = this;
    private void Start() => GameEvents.TimeChanged(timeRemaining);

    private void Update()
    {
        if (dayEnded) return;
        if (TimeManager.Instance != null && TimeManager.Instance.IsPaused) return;

        timeRemaining -= Time.deltaTime;
        GameEvents.TimeChanged(Mathf.Max(timeRemaining, 0f));

        if (timeRemaining <= 0f)
        {
            timeRemaining = 0f;
            EndDay();
        }
    }

    public void ApplyPenalty(float seconds)
    {
        if (dayEnded) return;
        timeRemaining = Mathf.Max(timeRemaining - seconds, 0f);
        GameEvents.TimeChanged(timeRemaining);
        if (timeRemaining <= 0f) EndDay();
    }

    public void ResetForNewDay(float duration)
    {
        timeRemaining = duration;
        dayEnded = false;
        GameEvents.TimeChanged(timeRemaining);
    }

    private void EndDay()
    {
        dayEnded = true;
        DayResolver.ResolveEndOfDay();
    }
}
```
Anexar em `Systems`.

## 4. DemandTracker

`Assets/Scripts/Core/DemandTracker.cs`:
```csharp
using UnityEngine;

public class DemandTracker : MonoBehaviour
{
    public static DemandTracker Instance { get; private set; }
    public int Sold { get; private set; }
    public int Target { get; private set; }

    private void Awake() => Instance = this;

    public void StartDay(int day)
    {
        Sold = 0;
        Target = DemandCalculator.GetDemand(day);
        GameEvents.DemandChanged(Sold, Target);
    }

    public void RegisterSale(string itemName, int amount)
    {
        if (itemName != "Monster Essence") return;
        Sold += amount;
        GameEvents.DemandChanged(Sold, Target);
    }

    public bool IsMet() => Sold >= Target;
}
```
Anexar em `Systems`.

**Conectar no `Vendor.cs` (Sprint 10)** — dentro de `Interact()`, depois de calcular `goldEarned`:
```csharp
DemandTracker.Instance.RegisterSale("Monster Essence", totalSold);
```

## 5. DayResolver — fim de dia (Results ou Game Over)

`Assets/Scripts/Core/DayResolver.cs`:
```csharp
using UnityEngine;

public static class DayResolver
{
    public static void ResolveEndOfDay()
    {
        // Loot ainda na Bag é destruído ao fim do dia — GDD Seção 37/38, chega de verdade agora.
        BagController.Instance.Bag.Clear();
        GameEvents.BagChanged(BagController.Instance.Bag);

        if (DemandTracker.Instance.IsMet())
        {
            Debug.Log("[DayResolver] Demanda cumprida — indo para Resultados.");
            GameStateManager.Instance.SetState(GameState.Results);
        }
        else
        {
            Debug.Log("[DayResolver] Demanda NÃO cumprida — GAME OVER.");
            GameStateManager.Instance.SetState(GameState.GameOver);
        }
    }
}
```

## 6. Game Over, Results e Shop (handlers de estado)

`Assets/Scripts/World/GameOverHandler.cs`:
```csharp
using UnityEngine;

public class GameOverHandler : MonoBehaviour
{
    private void OnEnable() => GameEvents.OnGameStateChanged += HandleStateChanged;
    private void OnDisable() => GameEvents.OnGameStateChanged -= HandleStateChanged;

    private void HandleStateChanged(GameState state)
    {
        if (state != GameState.GameOver) return;
        Debug.Log("[GameOverHandler] Game Over — retornando ao Menu. O save NÃO é tocado aqui (GDD Seção 43).");
        GameStateManager.Instance.SetState(GameState.MainMenu);
    }
}
```

`Assets/Scripts/World/ResultsHandler.cs`:
```csharp
using UnityEngine;

public class ResultsHandler : MonoBehaviour
{
    private void OnEnable() => GameEvents.OnGameStateChanged += HandleStateChanged;
    private void OnDisable() => GameEvents.OnGameStateChanged -= HandleStateChanged;

    private void HandleStateChanged(GameState state)
    {
        if (state != GameState.Results) return;
        Debug.Log($"[ResultsHandler] Dia concluído. Gold total: {MainMenuUI.CurrentRun.gold}. Demanda: {DemandTracker.Instance.Sold}/{DemandTracker.Instance.Target}.");
        GameStateManager.Instance.SetState(GameState.Shop);
    }
}
```
Skeleton mesmo — item por item vendido (imagem/nome/quantidade/valor) é polish de Deadline 13, não desta sprint. Aqui só prova que o estado transiciona certo com o resumo mínimo.

`Assets/Scripts/World/ShopHandler.cs`:
```csharp
using UnityEngine;

public class ShopHandler : MonoBehaviour
{
    private void OnEnable() => GameEvents.OnGameStateChanged += HandleStateChanged;
    private void OnDisable() => GameEvents.OnGameStateChanged -= HandleStateChanged;

    private void HandleStateChanged(GameState state)
    {
        if (state != GameState.Shop) return;
        Debug.Log("[ShopHandler] Loja aberta (esqueleto — sem compras ainda). Use \"Start Next Day\" pra avançar.");
    }

    [ContextMenu("Start Next Day")]
    public void StartNextDay()
    {
        MainMenuUI.CurrentRun.day++;
        DayTimer.Instance.ResetForNewDay(100f);
        DemandTracker.Instance.StartDay(MainMenuUI.CurrentRun.day);
        GameStateManager.Instance.SetState(GameState.Gameplay);
        Debug.Log($"[ShopHandler] Iniciando Dia {MainMenuUI.CurrentRun.day}.");
    }
}
```
**Nota de escopo:** nenhum `SaveManager.Save()` acontece aqui de propósito — o checkpoint real "salva ao entrar na Loja" é a Sprint 12 inteira. Entrar no estado `Shop` por enquanto só habilita avançar o dia, sem persistir nada ainda.

Anexar os 3 handlers em `Systems`.

## 7. Wire no Death Flow (Sprint 8) — as duas dívidas que se resolvem aqui

Em `HeroController`, dentro do método privado de morte (Sprint 8), substituir os dois placeholders:
```csharp
// Antes: comentário "sem inventário ainda"
BagController.Instance.Bag.Clear();
GameEvents.BagChanged(BagController.Instance.Bag);

// Antes: Debug.Log da penalidade placeholder
DayTimer.Instance.ApplyPenalty(30f);
```

## 8. HUD — Time e Demand

`Assets/Scripts/UI/TimeIndicatorUI.cs`:
```csharp
using UnityEngine;
using TMPro;

public class TimeIndicatorUI : MonoBehaviour
{
    public TMP_Text label;
    private void OnEnable() => GameEvents.OnTimeChanged += UpdateLabel;
    private void OnDisable() => GameEvents.OnTimeChanged -= UpdateLabel;
    private void UpdateLabel(float time) => label.text = $"Time: {time:F0}s";
}
```

`Assets/Scripts/UI/DemandIndicatorUI.cs`:
```csharp
using UnityEngine;
using TMPro;

public class DemandIndicatorUI : MonoBehaviour
{
    public TMP_Text label;
    private void OnEnable() => GameEvents.OnDemandChanged += UpdateLabel;
    private void OnDisable() => GameEvents.OnDemandChanged -= UpdateLabel;
    private void UpdateLabel(int sold, int target) => label.text = $"Demand: {sold}/{target} Monster Essence";
}
```
Mais 2 `Text - TextMeshPro` no `Canvas`.

## 9. Iniciar Timer/Demanda junto com a run

Em `MainMenuUI.NewGame()`, depois de criar o `RunState`:
```csharp
DayTimer.Instance.ResetForNewDay(100f);
DemandTracker.Instance.StartDay(CurrentRun.day);
```

---

## 10. Teste manual (Play Mode)

1. New Game → `Time: 100s` contando pra baixo, `Demand: 0/40 Monster Essence`.
2. Matar inimigos, coletar e vender até bater 40 → `DayResolver` dispara sozinho ou o tempo zera antes: testar os dois casos.
3. **Caso sucesso:** vender 40+ antes do tempo acabar → Console mostra Results (gold/demanda) → transita pra Shop → "Start Next Day" via menu de contexto → `Time` volta a 100s, `Demand` reseta pro Dia 2 (`0/80`).
4. **Caso falha:** deixar o tempo zerar sem bater a demanda → Console mostra Game Over → volta pro estado de Menu. Confirmar no disco (`Application.persistentDataPath/save.json`) que **nenhum arquivo novo foi escrito** — o Game Over realmente não tocou o save.
5. Morrer de propósito com a Bag cheia → Console mostra a Bag sendo limpa e -30s real aplicado no `DayTimer` (o `Time` na HUD cai instantaneamente 30, não só loga mais).

---

## 11. Git

```
git add .
git commit -m "chore: remove unused GameEvents.OnLootCollected"
```
```
git add .
git commit -m "feat: demand calculator (pure) + editmode tests"
```
```
git add .
git commit -m "feat: day timer + demand tracker + game state changed event"
```
```
git add .
git commit -m "feat: day resolver (results/game over) + shop skeleton + start next day"
```
```
git add .
git commit -m "feat: wire real -30s penalty and bag clear into death flow; time/demand HUD"
git push
```

## 12. Fechamento

`docs/sprints/sprint-11.md` (este arquivo) + linha no `docs/sprints/index.md` + linha no `docs/changelog.md`.

---

**Pronto quando:** os 3 testes de `DemandCalculator` passam verdes; o dia termina corretamente nos dois casos (sucesso → Results → Shop → próximo dia; falha → Game Over → Menu, sem tocar o save); a Bag esvazia ao fim do dia e também na morte; a penalidade de -30s reduz o `DayTimer` de verdade, não só loga; os indicadores de Time e Demand refletem tudo em tempo real.
