# Sprint 2 — Input System + Organização de Projeto

**Depende de:** Sprint 1.
**Objetivo:** Player placeholder responde aos inputs do GDD (WASD, Mouse, LMB, RMB, E, TAB, Q) e a Hierarchy fica organizada. Nada de gameplay real ainda — é só a fiação.

---

## 1. Instalar o Input System

`Window > Package Manager` → dropdown "In Project" → "Unity Registry" → buscar **"Input System"** → Install. Aceitar o reinício do editor quando perguntar sobre trocar o backend de input.

## 2. Criar o Input Actions Asset

Em `Assets/`, botão direito → `Create > Input Actions` → renomear para **`PlayerControls`**. Abrir com duplo clique.

Criar um Action Map `Gameplay` com as Actions:

| Nome da Action | Action Type | Binding |
|---|---|---|
| `Move` | Value → Vector2 | 2D Vector Composite: W/S/A/D |
| `Look` | Value → Vector2 | `<Mouse>/position` |
| `Attack` | Button | `<Mouse>/leftButton` |
| `Ultimate` | Button | `<Mouse>/rightButton` |
| `Interact` | Button | `<Keyboard>/e` |
| `Inventory` | Button | `<Keyboard>/tab` |
| `RemoteControl` | Button | `<Keyboard>/q` |

Marcar **"Generate C# Class"** (nome `PlayerControls`) → **Apply**.

## 3. Criar o Player placeholder

`GameObject > 2D Object > Sprites > Square`, renomear para **`Player`**.

Script `Assets/Scripts/PlayerInputTest.cs`:

```csharp
using UnityEngine;

public class PlayerInputTest : MonoBehaviour
{
    private PlayerControls controls;
    private Vector2 moveInput;
    public float moveSpeed = 5f;

    private void Awake()
    {
        controls = new PlayerControls();

        controls.Gameplay.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        controls.Gameplay.Move.canceled += ctx => moveInput = Vector2.zero;

        controls.Gameplay.Attack.performed += ctx => Debug.Log("Attack (LMB)");
        controls.Gameplay.Ultimate.performed += ctx => Debug.Log("Ultimate (RMB)");
        controls.Gameplay.Interact.performed += ctx => Debug.Log("Interact (E)");
        controls.Gameplay.Inventory.performed += ctx => Debug.Log("Inventory (TAB)");
        controls.Gameplay.RemoteControl.performed += ctx => Debug.Log("RemoteControl (Q)");
    }

    private void OnEnable() => controls.Enable();
    private void OnDisable() => controls.Disable();

    private void Update()
    {
        transform.Translate(moveInput * moveSpeed * Time.deltaTime);
    }
}
```

Arrastar o script para o `Player`. Testar em Play: WASD move, LMB/RMB/E/TAB/Q logam no Console.

## 4. Layers e Tags de gameplay

`Edit > Project Settings > Tags and Layers`.

- **Layers** (slots User Layer): `Player`, `Enemy`, `Floor`, `Interactable`.
- **Tags**: `Player`, `Enemy`, `Interactable`.

No `Player`, definir Tag = `Player` e Layer = `Player` no Inspector.

## 5. HierarchySectionHeader

1. Criar `Assets/Editor/`.
2. Salvar como `Assets/Editor/HierarchySectionHeader.cs` o conteúdo de: https://gist.github.com/bsimser/adab42840fa0f5f6dfc467361f3c3e5a
3. Uso: `GameObject > Create Empty`, renomear começando com `//` (ex.: `//SYSTEMS`). O objeto aparece na Hierarchy com fundo preto e texto branco em caixa alta.
4. Criar `//SYSTEMS` e `//ENTITIES`. Colocar o `Player` dentro/abaixo de `//ENTITIES`.

## 6. Git

Direto em `main`, sem branch:
```
git add .
git commit -m "feat: input system + player placeholder"
```
```
git add .
git commit -m "chore: gameplay layers/tags + hierarchy organization"
git push
```

## 7. Fechamento

Sprint Report resumido + linha no changelog. Sem tag esta semana.

---

**Pronto quando:** Play → quadrado anda com WASD → os 5 outros botões logam no Console → Hierarchy mostra `//SYSTEMS` e `//ENTITIES` com o `Player` no lugar certo.
