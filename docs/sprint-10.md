# Sprint 10 — Inventory + Vendor + Gold

**Depende de:** Sprint 9.
**Objetivo:** Bag real (com coleta parcial de verdade, não só "coleta e destrói" como na Sprint 9), e o primeiro uso concreto do padrão `Interactable` fora de escada: o NPC vendedor, transformando Monster Essence em Gold.

---

## 1. Bag — lógica pura primeiro (mesmo padrão de sempre)

`Assets/Scripts/Core/Inventory/InventorySlot.cs`:
```csharp
[System.Serializable]
public class InventorySlot
{
    public string itemName;
    public int quantity;
}
```

`Assets/Scripts/Core/Inventory/Bag.cs`:
```csharp
using System.Collections.Generic;

public class Bag
{
    public int maxSlots;
    public int stackSize;
    public List<InventorySlot> Slots = new();

    public Bag(int maxSlots, int stackSize)
    {
        this.maxSlots = maxSlots;
        this.stackSize = stackSize;
    }

    // Retorna quanto REALMENTE entrou — pode ser menor que 'amount' (coleta parcial, GDD Seção 37).
    public int AddItem(string itemName, int amount)
    {
        int remaining = amount;

        foreach (var slot in Slots)
        {
            if (slot.itemName != itemName) continue;
            int space = stackSize - slot.quantity;
            if (space <= 0) continue;

            int toAdd = System.Math.Min(space, remaining);
            slot.quantity += toAdd;
            remaining -= toAdd;
            if (remaining == 0) return amount;
        }

        while (remaining > 0 && Slots.Count < maxSlots)
        {
            int toAdd = System.Math.Min(stackSize, remaining);
            Slots.Add(new InventorySlot { itemName = itemName, quantity = toAdd });
            remaining -= toAdd;
        }

        return amount - remaining;
    }

    public void RemoveSlot(int index)
    {
        if (index >= 0 && index < Slots.Count) Slots.RemoveAt(index);
    }

    public void Clear() => Slots.Clear();
}
```

`Assets/Tests/EditMode/BagTests.cs`:
```csharp
using NUnit.Framework;

public class BagTests
{
    [Test]
    public void AddItem_FitsCompletely_ReturnsFullAmount()
    {
        var bag = new Bag(maxSlots: 5, stackSize: 16);
        int added = bag.AddItem("Monster Essence", 10);
        Assert.AreEqual(10, added);
        Assert.AreEqual(1, bag.Slots.Count);
        Assert.AreEqual(10, bag.Slots[0].quantity);
    }

    [Test]
    public void AddItem_ExceedsStackSize_CreatesNewSlot()
    {
        var bag = new Bag(maxSlots: 5, stackSize: 16);
        bag.AddItem("Monster Essence", 16);
        int added = bag.AddItem("Monster Essence", 10);
        Assert.AreEqual(10, added);
        Assert.AreEqual(2, bag.Slots.Count);
    }

    [Test]
    public void AddItem_NoSpaceLeft_ReturnsZero()
    {
        var bag = new Bag(maxSlots: 1, stackSize: 16);
        bag.AddItem("Monster Essence", 16); // enche o único slot
        int added = bag.AddItem("Monster Essence", 10);
        Assert.AreEqual(0, added);
    }

    [Test]
    public void AddItem_PartialFit_ReturnsOnlyWhatFit()
    {
        var bag = new Bag(maxSlots: 1, stackSize: 16);
        int added = bag.AddItem("Monster Essence", 30); // só cabe 16 (1 slot × stack 16)
        Assert.AreEqual(16, added);
    }

    [Test]
    public void Clear_RemovesAllSlots()
    {
        var bag = new Bag(maxSlots: 5, stackSize: 16);
        bag.AddItem("Monster Essence", 10);
        bag.Clear();
        Assert.AreEqual(0, bag.Slots.Count);
    }
}
```
Rodar no Test Runner antes de seguir — os 5 testes cobrem exatamente a regra de coleta parcial do GDD (nunca tudo-ou-nada).

## 2. BagController + eventos

Adicionar em `GameEvents.cs`:
```csharp
public static event Action<Bag> OnBagChanged;
public static void BagChanged(Bag bag) => OnBagChanged?.Invoke(bag);

public static event Action<int> OnGoldChanged;
public static void GoldChanged(int gold) => OnGoldChanged?.Invoke(gold);
```

`Assets/Scripts/Player/BagController.cs`:
```csharp
using UnityEngine;

public class BagController : MonoBehaviour
{
    public static BagController Instance { get; private set; }
    public Bag Bag { get; private set; }

    private PlayerControls controls;
    private bool isOpen;

    private void Awake()
    {
        Instance = this;
        Bag = new Bag(maxSlots: 5, stackSize: 16);
        controls = new PlayerControls();
        controls.Gameplay.Inventory.performed += ctx => ToggleBag();
    }

    private void OnEnable() => controls.Enable();
    private void OnDisable() => controls.Disable();

    private void ToggleBag()
    {
        isOpen = !isOpen;
        if (isOpen) TimeManager.Instance.Pause();
        else TimeManager.Instance.Resume();

        Debug.Log($"[BagController] Bag {(isOpen ? "aberta" : "fechada")}.");
    }

    public int AddItem(string itemName, int amount)
    {
        int added = Bag.AddItem(itemName, amount);
        GameEvents.BagChanged(Bag);
        return added;
    }
}
```
Anexar em `Systems`.

**Importante — remover uma linha do `SystemsBootstrap` (Sprint 3):** `TAB` (`Inventory`) estava ligado a `TimeManager.TogglePause()` como placeholder desde a Sprint 3, junto com `Q`. Agora que TAB tem dono de verdade (`BagController`), essa linha específica precisa sair do `SystemsBootstrap` — senão os dois brigam pelo mesmo toggle (o Bag abriria e a pausa alternaria de volta no mesmo frame, cancelando uma a outra). `Q` continua no `SystemsBootstrap` como placeholder de teste de pausa — ele só sai quando o Remote Controller (Deadline 12) existir de verdade.

## 3. LootDrop — coleta parcial de verdade

Atualizar `LootDrop.Collect()` (Sprint 9):
```csharp
private void Collect()
{
    int added = BagController.Instance.AddItem(loot.itemName, loot.quantity);

    if (added > 0)
        Debug.Log($"[LootDrop] Coletado: {added}x {loot.itemName}");

    if (added < loot.quantity)
    {
        loot.quantity -= added;
        Debug.Log($"[LootDrop] Bag cheia — {loot.quantity}x {loot.itemName} continuam no chão.");
        return; // não destrói — a pilha remanescente continua existindo, com a quantidade reduzida
    }

    Destroy(gameObject);
}
```
Simplificação deliberada em relação ao GDD Seção 38 (que fala em "entidade separada" pro restante): aqui é a **mesma** entidade com a quantidade reduzida, em vez de duas — efeito idêntico pro jogador, menos código. Só vira problema real se algo precisar diferenciar "essa pilha específica" de outra igual no mesmo lugar, o que não acontece ainda.

## 4. NPC Vendor

`Assets/Scripts/World/Vendor.cs` (segue o mesmo padrão de `Stair`, Sprint 6 — herda de `Interactable`):
```csharp
using UnityEngine;

public class Vendor : Interactable
{
    public int pricePerEssence = 1; // valor placeholder — Economia real chega na Deadline 9 do roadmap

    public override void Interact()
    {
        var bag = BagController.Instance.Bag;
        int totalSold = 0;

        for (int i = bag.Slots.Count - 1; i >= 0; i--)
        {
            if (bag.Slots[i].itemName != "Monster Essence") continue;
            totalSold += bag.Slots[i].quantity;
            bag.RemoveSlot(i);
        }

        if (totalSold == 0)
        {
            Debug.Log("[Vendor] Nada pra vender.");
            return;
        }

        int goldEarned = totalSold * pricePerEssence;
        MainMenuUI.CurrentRun.gold += goldEarned;

        Debug.Log($"[Vendor] Vendeu {totalSold}x Monster Essence por {goldEarned} gold. Total: {MainMenuUI.CurrentRun.gold}");
        GameEvents.GoldChanged(MainMenuUI.CurrentRun.gold);
        GameEvents.BagChanged(bag);
    }
}
```
**Montar na Scene:** GameObject `Vendor` no `Floor_Ground`, perto da posição inicial do Player, com o mesmo setup de collider + prompt que o `Stair` já usa (Sprint 6).

## 5. HUD — Gold e Bag (placeholder)

`Assets/Scripts/UI/GoldIndicatorUI.cs`:
```csharp
using UnityEngine;
using TMPro;

public class GoldIndicatorUI : MonoBehaviour
{
    public TMP_Text label;
    private void OnEnable() => GameEvents.OnGoldChanged += UpdateLabel;
    private void OnDisable() => GameEvents.OnGoldChanged -= UpdateLabel;
    private void UpdateLabel(int gold) => label.text = $"Gold: {LargeNumberFormatter.Format(gold)}";
}
```
Reaproveita o `LargeNumberFormatter` da Sprint 4 — primeira vez que ele é usado fora de teste isolado.

`Assets/Scripts/UI/BagIndicatorUI.cs`:
```csharp
using UnityEngine;
using TMPro;

public class BagIndicatorUI : MonoBehaviour
{
    public TMP_Text label;
    private void OnEnable() => GameEvents.OnBagChanged += UpdateLabel;
    private void OnDisable() => GameEvents.OnBagChanged -= UpdateLabel;
    private void UpdateLabel(Bag bag) => label.text = $"Bag: {bag.Slots.Count}/{bag.maxSlots} slots";
}
```
Mais 2 `Text - TextMeshPro` no `Canvas` já existente, mesmo padrão das Sprints 6-8.

## 6. Inicializar a HUD ao começar a run

Em `MainMenuUI.NewGame()`, depois de criar o `RunState`, adicionar:
```csharp
BagController.Instance.Bag.Clear();
GameEvents.GoldChanged(CurrentRun.gold);
GameEvents.BagChanged(BagController.Instance.Bag);
```
Senão os indicadores de Gold/Bag ficam em branco até a primeira venda/coleta.

---

## 7. Teste manual (Play Mode)

1. New Game → HUD mostra `Gold: 0` e `Bag: 0/5 slots`.
2. Matar o inimigo, coletar o loot → `Bag: 1/5 slots`.
3. Repetir até passar de 16 (o stack) → confirma que abre um segundo slot (`Bag: 2/5 slots`).
4. Encher os 5 slots (80 de Monster Essence) e tentar coletar mais → o `LootDrop` correspondente **não** desaparece, loga quanto ficou no chão, `Bag` continua em `5/5`.
5. TAB → Bag "abre" (loga), jogo pausa (nada se move); TAB de novo → fecha, despausa. Confirma que **não** há mais conflito com o antigo placeholder.
6. Ir até o `Vendor`, apertar E → Console mostra a venda, `Gold` e `Bag` (voltando a `0/5`) atualizam na HUD.
7. Apertar E no Vendor de novo sem nada pra vender → loga "Nada pra vender", não quebra.

---

## 8. Git

```
git add .
git commit -m "feat: bag (partial pickup logic) + editmode tests"
```
```
git add .
git commit -m "feat: bag controller wired to TAB, remove duplicate pause toggle from SystemsBootstrap"
```
```
git add .
git commit -m "feat: loot drop uses real bag with partial pickup"
```
```
git add .
git commit -m "feat: npc vendor (sells monster essence for gold) + gold/bag HUD"
git push
```

## 9. Fechamento

`docs/sprints/sprint-10.md` (este arquivo) + linha no `docs/sprints/index.md` + linha no `docs/changelog.md`.

---

**Pronto quando:** os 5 testes de `Bag` passam verdes; coletar loot além da capacidade da Bag deixa o restante fisicamente no chão em vez de perder ou travar; TAB abre/fecha a Bag e pausa/despausa sem conflito com nenhum outro sistema; o Vendor vende toda a Monster Essence da Bag de uma vez, atualizando Gold e Bag na HUD.
