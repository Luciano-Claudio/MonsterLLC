# Sprint 9 — Main Menu + Run Creation Flow + Loot Básico

**Depende de:** Sprint 8.
**Objetivo:** `New Game` deixa de ser conceito e vira fluxo real — Menu → Mode/Hero/Map Select → cria um `RunState` de verdade. Monster Essence passa a existir como loot: dropa, é coletada via Pickup Radius, sem ainda ser vendida (isso é Sprint 10).

> Esta sprint também é onde o `GameStateManager` (esqueleto desde a Sprint 3, nunca consultado por ninguém) ganha os estados reais de uma vez — de propósito, pra não ficar esticando esse enum estado por estado nas próximas 3 sprints.

---

## 1. GameStateManager — estados reais

Atualizar `GameStateManager.cs`:
```csharp
public enum GameState
{
    MainMenu,
    ModeSelect,
    HeroSelect,
    MapSelect,
    Gameplay,
    Paused,
    Results,
    Shop,
    GameOver
}
```
O manager em si (singleton + `CurrentState`/`SetState`) não muda de forma — só o enum. `Paused` continua existindo como estado próprio, separado do controle de tempo do `TimeManager` (os dois já convivem desde a Sprint 3: `TimeManager.IsPaused` trava o tempo, `GameStateManager.CurrentState` descreve em que tela o jogo está).

## 2. RunState — preenchido de verdade

`RunState.cs` (Sprint 4) já tem os campos `mode`/`hero`/`map`/`day`/`gold`. Nesta sprint eles passam a ser preenchidos pelo fluxo de criação de run, não só por um teste manual:

`Assets/Scripts/Core/RunCreation.cs`:
```csharp
public static class RunCreation
{
    public static RunState CreateNewRun(string mode, string hero, string map)
    {
        return new RunState
        {
            mode = mode,
            hero = hero,
            map = map,
            day = 1,
            gold = 0
        };
    }
}
```
Pura, sem `MonoBehaviour` — mesmo padrão de `StairRouting`/`EnergySystem`/`HealthSystem`.

`Assets/Tests/EditMode/RunCreationTests.cs`:
```csharp
using NUnit.Framework;

public class RunCreationTests
{
    [Test]
    public void CreateNewRun_SetsFieldsCorrectly()
    {
        var run = RunCreation.CreateNewRun("Standard", "Barbarian", "Tower");

        Assert.AreEqual("Standard", run.mode);
        Assert.AreEqual("Barbarian", run.hero);
        Assert.AreEqual("Tower", run.map);
        Assert.AreEqual(1, run.day);
        Assert.AreEqual(0, run.gold);
    }
}
```

## 3. Fluxo de Menu (placeholder de UI, lógica real por trás)

MVP só tem 1 modo funcional de verdade a oferecer por enquanto (Standard — Free continua pendência aberta do GDD), 1 herói (Barbarian) e 1 mapa (Tower). O fluxo existe mesmo assim, porque a estrutura importa mais que a quantidade de opções agora.

`Assets/Scripts/UI/MainMenuUI.cs`:
```csharp
using UnityEngine;

public class MainMenuUI : MonoBehaviour
{
    public static RunState CurrentRun { get; private set; }

    [ContextMenu("New Game (Standard / Barbarian / Tower)")]
    public void NewGame()
    {
        GameStateManager.Instance.SetState(GameState.ModeSelect);
        Debug.Log("[MainMenu] Mode selecionado: Standard");

        GameStateManager.Instance.SetState(GameState.HeroSelect);
        Debug.Log("[MainMenu] Hero selecionado: Barbarian");

        GameStateManager.Instance.SetState(GameState.MapSelect);
        Debug.Log("[MainMenu] Map selecionado: Tower");

        CurrentRun = RunCreation.CreateNewRun("Standard", "Barbarian", "Tower");
        Debug.Log($"[MainMenu] RunState criado — Dia {CurrentRun.day}, Gold {CurrentRun.gold}");

        GameStateManager.Instance.SetState(GameState.Gameplay);
    }

    [ContextMenu("Continue Game")]
    public void ContinueGame()
    {
        if (!SaveManager.HasSave())
        {
            Debug.Log("[MainMenu] Continue indisponível — nenhum save encontrado.");
            return;
        }

        CurrentRun = SaveManager.Load();
        Debug.Log($"[MainMenu] Save carregado — Dia {CurrentRun.day}, Gold {CurrentRun.gold}, Hero {CurrentRun.hero}");
        GameStateManager.Instance.SetState(GameState.Gameplay);
    }
}
```
Anexar em `Systems`. Cada etapa (Mode/Hero/Map) já passa pelo estado correspondente e loga — é o suficiente pra provar o fluxo sem construir UI de seleção ainda (isso é polish de Deadline 13, não bloqueia nada agora).

**Nota:** `ContinueGame()` já implementa a regra do GDD (Continue desabilitado sem save) — não é feature nova desta sprint, é só o primeiro lugar onde ela tem uso real.

## 4. Monster Essence — primeiro material de loot

`Assets/Scripts/Core/LootDefinition.cs`:
```csharp
[System.Serializable]
public class LootDefinition
{
    public string itemName;
    public int quantity;
}
```

`Assets/Scripts/World/LootDrop.cs`:
```csharp
using UnityEngine;

public class LootDrop : MonoBehaviour
{
    public LootDefinition loot = new LootDefinition { itemName = "Monster Essence", quantity = 1 };
    public float pickupRadius = 1f;

    private void Update()
    {
        if (TimeManager.Instance != null && TimeManager.Instance.IsPaused) return;

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        if (Vector2.Distance(transform.position, player.transform.position) <= pickupRadius)
            Collect();
    }

    private void Collect()
    {
        Debug.Log($"[LootDrop] Coletado: {loot.quantity}x {loot.itemName}");
        GameEvents.LootCollected(loot);
        Destroy(gameObject);
    }
}
```

Adicionar em `GameEvents.cs`:
```csharp
public static event Action<LootDefinition> OnLootCollected;
public static void LootCollected(LootDefinition loot) => OnLootCollected?.Invoke(loot);
```

**Fazer o `MeleeEnemyPrototype` dropar ao morrer** — em `Die()` (Sprint 8), antes do `Destroy(gameObject)`:
```csharp
var lootObj = new GameObject("Loot_MonsterEssence");
lootObj.transform.position = transform.position;
var drop = lootObj.AddComponent<LootDrop>();
drop.loot = new LootDefinition { itemName = "Monster Essence", quantity = 1 };
```

Sem Bag ainda (Sprint 10) — por enquanto o loot só é "coletado" no sentido de desaparecer e logar. Guardar de verdade é a próxima sprint.

---

## 5. Teste manual (Play Mode)

1. Botão direito em `MainMenuUI` → "New Game" → Console mostra a sequência Mode/Hero/Map/RunState/Gameplay.
2. Matar o `Enemy_Test` → um `Loot_MonsterEssence` aparece na posição da morte.
3. Aproximar o Barbarian do loot → Console loga "Coletado: 1x Monster Essence", objeto some.
4. Botão direito → "Continue Game" **sem nenhum save existente ainda** → loga "indisponível — nenhum save encontrado" (não quebra, não tenta carregar nada).

---

## 6. Git

```
git add .
git commit -m "feat: game state manager with real states (menu, select, gameplay, results, shop, gameover)"
```
```
git add .
git commit -m "feat: run creation flow (new game / continue game) wired to RunState + SaveManager"
```
```
git add .
git commit -m "feat: monster essence loot drop + pickup"
git push
```

## 7. Fechamento

`docs/sprints/sprint-09.md` (este arquivo) + linha no `docs/sprints/index.md` + linha no `docs/changelog.md`.

---

**Pronto quando:** o teste `RunCreationTests` passa verde; "New Game" cria um `RunState` real passando pelos estados corretos do `GameStateManager`; "Continue Game" reconhece corretamente a ausência de save; o inimigo dropa Monster Essence ao morrer e o Barbarian coleta aproximando-se.
