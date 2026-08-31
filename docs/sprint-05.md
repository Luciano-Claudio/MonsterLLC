# Sprint 5 — Floor System Skeleton

**Depende de:** Sprint 4.
**Objetivo:** Ground + 2 Floors dentro da **mesma Scene** (regra do GDD Seção 24 — sem carregar Scene nova ao trocar de andar), com o conceito de "Current Floor" sendo identificado corretamente quando o jogador entra em cada região.

> Escopo desta sprint: só existência e detecção dos Floors. **Escadas/teleporte reais são a Sprint 6** — aqui a gente ainda testa manualmente (teleportando o Player na mão) pra provar que a detecção funciona antes de construir a navegação real em cima.

---

## 1. Layout da Scene

Três regiões na mesma `_TestScene`, fisicamente separadas por posição (não por Scene):

| Região | Posição (X, Y) | Original Floor Identity | Active Floor Position |
|---|---|---|---|
| Ground (térreo) | (0, 0) | 0 | 0 |
| Floor 1 | (0, 50) | 1 | 1 |
| Floor 2 | (0, 100) | 2 | 2 |

A distância de 50 unidades entre regiões é proposital — reforça que elas são áreas fisicamente distintas do mesmo mapa, não uma continuação natural que dá pra andar direto (a travessia de verdade só existe a partir da Sprint 6, via escada).

**Original Floor Identity** e **Active Floor Position** começam iguais aqui de propósito — só divergem quando o Remove Tower Layer existir (Deadline 12). Não implementar nada de remoção agora.

---

## 2. Scripts (Core — puro, sem dependência de Editor)

`Assets/Scripts/Core/FloorDefinition.cs`:
```csharp
using UnityEngine;

public class FloorDefinition : MonoBehaviour
{
    public string floorName = "Ground";
    public int originalFloorIdentity = 0;
    public int activeFloorPosition = 0;
}
```

`Assets/Scripts/Core/FloorManager.cs`:
```csharp
using UnityEngine;

public class FloorManager : MonoBehaviour
{
    public static FloorManager Instance { get; private set; }
    public FloorDefinition CurrentFloor { get; private set; }

    private void Awake() => Instance = this;

    public void SetCurrentFloor(FloorDefinition floor)
    {
        if (CurrentFloor == floor) return;
        CurrentFloor = floor;
        Debug.Log($"[FloorManager] Current Floor = {floor.floorName} (Original {floor.originalFloorIdentity}, Active Position {floor.activeFloorPosition})");
    }
}
```

`Assets/Scripts/Core/FloorTrigger.cs`:
```csharp
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class FloorTrigger : MonoBehaviour
{
    public FloorDefinition floor;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        FloorManager.Instance.SetCurrentFloor(floor);
    }
}
```

---

## 3. Montar a Scene

Dentro de `//SYSTEMS`:
1. Criar GameObject vazio `FloorManager`, anexar o script `FloorManager`.

Dentro de `//ENTITIES` (ou uma seção nova `//WORLD`, se preferir — usa o `//` do `HierarchySectionHeader` normalmente):
2. Criar 3 GameObjects vazios: `Floor_Ground`, `Floor_1`, `Floor_2`, posicionados conforme a tabela acima.
3. Em cada um: anexar `FloorDefinition` (preencher `floorName`/`originalFloorIdentity`/`activeFloorPosition` conforme a tabela) + um `BoxCollider2D` marcado **Is Trigger** (tamanho ~10x10, cobrindo a área da região) + `FloorTrigger`, arrastando o próprio `FloorDefinition` do mesmo objeto no campo `Floor`.

Nenhuma Layer/Tag nova necessária — `FloorTrigger` só verifica a Tag `Player`, que já existe desde a Sprint 2.

---

## 4. Script de teste (teleporte manual)

Como a travessia real ainda não existe, testamos movendo o Player na mão via menu de contexto.

`Assets/Scripts/FloorTeleportTest.cs`:
```csharp
using UnityEngine;

public class FloorTeleportTest : MonoBehaviour
{
    public Transform player;
    public Vector3 groundSpawn = new Vector3(0, 0, 0);
    public Vector3 floor1Spawn = new Vector3(0, 50, 0);
    public Vector3 floor2Spawn = new Vector3(0, 100, 0);

    [ContextMenu("Teleport To Ground")]
    public void ToGround() => player.position = groundSpawn;

    [ContextMenu("Teleport To Floor 1")]
    public void ToFloor1() => player.position = floor1Spawn;

    [ContextMenu("Teleport To Floor 2")]
    public void ToFloor2() => player.position = floor2Spawn;
}
```
Anexar em `Systems`, arrastar o `Player` da cena no campo `Player`.

**Testar (Play Mode):** botão direito no componente → "Teleport To Floor 1" → Console mostra `[FloorManager] Current Floor = Floor 1 (Original 1, Active Position 1)`. Repetir pra Ground e Floor 2, conferindo que o log muda certo a cada vez e **não repete** se teleportar duas vezes seguidas pro mesmo Floor (por causa do `if (CurrentFloor == floor) return;`).

## 5. Sobre teste automatizado

Nenhum EditMode/PlayMode automatizado nesta sprint. O que existe aqui (colisão de trigger, posição física na Scene) é fundamentalmente comportamento de runtime que só faz sentido validar em Play Mode — criar um teste EditMode só pra "ter teste" seria teatro, não validação real. Teste automatizado volta quando houver lógica pura pra testar (ex.: o cálculo de roteamento da Sprint 6).

---

## 6. Git

```
git add .
git commit -m "feat: floor system skeleton (Ground + Floor 1 + Floor 2, current floor detection)"
git push
```

## 7. Fechamento

`docs/sprints/sprint-05.md` (este arquivo) + linha no `docs/sprints/index.md` + linha no `docs/changelog.md`.

---

**Pronto quando:** teleportar o Player pra qualquer uma das 3 regiões atualiza o `FloorManager.CurrentFloor` corretamente, logado no Console, sem repetir o log ao entrar duas vezes seguidas no mesmo Floor.
