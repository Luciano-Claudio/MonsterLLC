# Sprint 6 — Stair Routing + Active Floor Position

**Depende de:** Sprint 5.
**Objetivo:** subir/descer leva pro Floor certo **por posição relativa (Active Floor Position)**, não por identidade fixa — é essa regra que vai permitir o Remove Tower Layer (Deadline 12) funcionar sem reescrever nada. Indicador de Floor atual aparece na tela.

---

## 1. A regra de roteamento (lógica pura, sem MonoBehaviour)

Essa é a peça mais importante da sprint, e é pura o suficiente pra testar sem precisar de Play Mode.

`Assets/Scripts/Core/StairRouting.cs`:
```csharp
using System.Collections.Generic;
using System.Linq;

public static class StairRouting
{
    // Próxima Active Floor Position acima da atual, ou null se já for o topo ativo.
    public static int? GetNextPosition(IEnumerable<int> activePositions, int currentPosition)
    {
        var ordered = activePositions.Distinct().OrderBy(p => p).ToList();
        int index = ordered.IndexOf(currentPosition);
        if (index == -1 || index == ordered.Count - 1) return null;
        return ordered[index + 1];
    }

    // Active Floor Position anterior à atual, ou null se já for o térreo.
    public static int? GetPreviousPosition(IEnumerable<int> activePositions, int currentPosition)
    {
        var ordered = activePositions.Distinct().OrderBy(p => p).ToList();
        int index = ordered.IndexOf(currentPosition);
        if (index <= 0) return null;
        return ordered[index - 1];
    }
}
```

Repara que essa classe **nunca olha pra `originalFloorIdentity`** — só pra posição. É essa separação que evita o erro que o GDD explicitamente veta (ligar escada por `OriginalFloorId + 1` fixo).

## 2. Teste automatizado (EditMode) — antes de plugar na Scene

`Assets/Tests/EditMode/StairRoutingTests.cs`:
```csharp
using NUnit.Framework;
using System.Collections.Generic;

public class StairRoutingTests
{
    [Test]
    public void GetNextPosition_ReturnsTheOneAbove()
    {
        var positions = new List<int> { 0, 1, 2 };
        Assert.AreEqual(1, StairRouting.GetNextPosition(positions, 0));
        Assert.AreEqual(2, StairRouting.GetNextPosition(positions, 1));
    }

    [Test]
    public void GetNextPosition_AtTop_ReturnsNull()
    {
        var positions = new List<int> { 0, 1, 2 };
        Assert.IsNull(StairRouting.GetNextPosition(positions, 2));
    }

    [Test]
    public void GetPreviousPosition_AtGround_ReturnsNull()
    {
        var positions = new List<int> { 0, 1, 2 };
        Assert.IsNull(StairRouting.GetPreviousPosition(positions, 0));
    }

    [Test]
    public void GetPreviousPosition_ReturnsTheOneBelow()
    {
        var positions = new List<int> { 0, 1, 2 };
        Assert.AreEqual(0, StairRouting.GetPreviousPosition(positions, 1));
    }

    [Test]
    public void Routing_WorksEvenWhenPositionsAreReassigned()
    {
        // simula uma troca de Active Floor Position — o mesmo tipo de coisa
        // que o Remove Tower Layer vai fazer na Deadline 12.
        var positions = new List<int> { 0, 2, 1 }; // Floor 1 e Floor 2 trocaram de posição
        Assert.AreEqual(1, StairRouting.GetNextPosition(positions, 0));
        Assert.AreEqual(2, StairRouting.GetNextPosition(positions, 1));
    }
}
```
Rodar no Test Runner **antes** de seguir pro resto da sprint — se essas 5 passarem, a lógica está certa antes de qualquer coisa física na Scene entrar no caminho.

## 3. FloorRegistry (conecta a lógica pura aos FloorDefinition da Scene)

`Assets/Scripts/Core/FloorRegistry.cs`:
```csharp
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FloorRegistry : MonoBehaviour
{
    public static FloorRegistry Instance { get; private set; }
    public List<FloorDefinition> Floors = new();

    private void Awake() => Instance = this;

    private FloorDefinition GetByPosition(int position) =>
        Floors.FirstOrDefault(f => f.activeFloorPosition == position);

    public FloorDefinition GetNextFloor(FloorDefinition current)
    {
        var next = StairRouting.GetNextPosition(Floors.Select(f => f.activeFloorPosition), current.activeFloorPosition);
        return next.HasValue ? GetByPosition(next.Value) : null;
    }

    public FloorDefinition GetPreviousFloor(FloorDefinition current)
    {
        var prev = StairRouting.GetPreviousPosition(Floors.Select(f => f.activeFloorPosition), current.activeFloorPosition);
        return prev.HasValue ? GetByPosition(prev.Value) : null;
    }
}
```

Anexar em `FloorManager` (mesmo GameObject, dentro de `//SYSTEMS`) e arrastar os 3 `Floor_Ground`/`Floor_1`/`Floor_2` da Sprint 5 pra lista `Floors` no Inspector.

## 4. Escadas de verdade (substituem o teleporte manual como forma principal de navegar)

`Assets/Scripts/Core/Stair.cs`:
```csharp
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Stair : MonoBehaviour
{
    public FloorDefinition ownerFloor;
    public bool goesUp = true;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        FloorDefinition target = goesUp
            ? FloorRegistry.Instance.GetNextFloor(ownerFloor)
            : FloorRegistry.Instance.GetPreviousFloor(ownerFloor);

        if (target == null)
        {
            Debug.Log("[Stair] Sem destino — limite da torre.");
            return;
        }

        other.transform.position = target.transform.position;
        FloorManager.Instance.SetCurrentFloor(target);
    }
}
```

**Montar na Scene** (dentro de cada `Floor_X`, como filho):
- `Floor_Ground`: 1 `Stair` filho (posição ~(3,0)), `ownerFloor = Floor_Ground`, `goesUp = true`. Collider2D trigger pequeno (~2x2).
- `Floor_1`: 2 `Stair` filhos — um em ~(-3,50) com `goesUp = false` (desce), outro em ~(3,50) com `goesUp = true` (sobe).
- `Floor_2`: 1 `Stair` filho em ~(-3,100), `goesUp = false` (desce). Sem escada de subida — é o topo desta Deadline.

## 5. Indicador de Floor na tela (placeholder)

Adicionar o evento em `GameEvents.cs`:
```csharp
public static event Action<FloorDefinition> OnFloorChanged;
public static void FloorChanged(FloorDefinition floor) => OnFloorChanged?.Invoke(floor);
```

Em `FloorManager.SetCurrentFloor`, depois do `Debug.Log`, adicionar `GameEvents.FloorChanged(floor);`.

**Passo de UI** (primeira vez usando TextMeshPro no projeto, o Unity precisa importar os recursos essenciais — isso é import/dependência resolvida pelo Editor, não arquivo autocontido):
1. `GameObject > UI > Text - TextMeshPro`. Se aparecer o popup "TMP Essentials", clicar **Import TMP Essentials**.
2. Isso cria automaticamente um `Canvas` — renomear o texto pra `FloorLabel`, posicionar no canto superior esquerdo.

`Assets/Scripts/UI/FloorIndicatorUI.cs`:
```csharp
using UnityEngine;
using TMPro;

public class FloorIndicatorUI : MonoBehaviour
{
    public TMP_Text label;

    private void OnEnable() => GameEvents.OnFloorChanged += UpdateLabel;
    private void OnDisable() => GameEvents.OnFloorChanged -= UpdateLabel;

    private void UpdateLabel(FloorDefinition floor) => label.text = $"Floor: {floor.floorName}";
}
```
Anexar no `Canvas` (ou num objeto UI dentro dele), arrastar `FloorLabel` no campo `Label`.

## 6. Teste de reordenação (a prova real da sprint)

`Assets/Scripts/FloorReorderTest.cs`:
```csharp
using UnityEngine;

public class FloorReorderTest : MonoBehaviour
{
    public FloorDefinition floor1;
    public FloorDefinition floor2;

    [ContextMenu("Swap Floor 1 and Floor 2 Active Positions")]
    public void SwapPositions()
    {
        (floor1.activeFloorPosition, floor2.activeFloorPosition) = (floor2.activeFloorPosition, floor1.activeFloorPosition);
        Debug.Log($"[FloorReorderTest] Floor 1 agora é Active Position {floor1.activeFloorPosition}, Floor 2 é {floor2.activeFloorPosition}");
    }
}
```
Anexar em `Systems`, arrastar `Floor_1` e `Floor_2`.

**Teste manual completo (Play Mode):**
1. Andar até a escada do `Floor_Ground` → deve teleportar pro `Floor_1`, indicador muda pra "Floor: Floor 1".
2. Continuar até a escada de subida do `Floor_1` → teleporta pro `Floor_2`, indicador atualiza.
3. Usar a escada de descida do `Floor_2` → volta pro `Floor_1`.
4. Usar a escada de descida do `Floor_1` → volta pro `Floor_Ground`.
5. **Botão direito em `FloorReorderTest` → "Swap Floor 1 and Floor 2 Active Positions"** (pode fazer em qualquer momento, inclusive no meio do teste). Repetir os passos 1-4 — a navegação continua funcionando corretamente, só que agora fisicamente entrando na região que era "Floor 1" te leva pro que passou a ser Active Position 2, e vice-versa. Isso prova que o roteamento não está preso à identidade do objeto, só à posição.

---

## 7. Git

```
git add .
git commit -m "feat: stair routing by active floor position + editmode tests"
```
```
git add .
git commit -m "feat: real stairs (up/down) replacing manual floor teleport as primary navigation"
```
```
git add .
git commit -m "feat: floor indicator UI placeholder"
git push
```

## 8. Fechamento

`docs/sprints/sprint-06.md` (este arquivo) + linha no `docs/sprints/index.md` + linha no `docs/changelog.md`.

---

**Pronto quando:** os 5 testes EditMode passam verdes; andar pelas escadas físicas na Scene teleporta corretamente entre os 3 Floors nos dois sentidos; o indicador de tela mostra o Floor atual certo; depois de trocar as Active Floor Position de Floor 1 e Floor 2 no meio do teste, a navegação continua correta usando a nova ordem, sem nenhuma mudança de código.
