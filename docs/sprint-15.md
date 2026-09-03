# Sprint 15 — Floor Sleep/Activation v1

**Depende de:** Sprint 6, Sprint 14.
**Objetivo:** com os 3 Floors existentes (Ground/Floor 1/Floor 2), só o Current Floor executa simulação completa — inimigos e população dos outros congelam exatamente onde estavam, sem perder estado, em vez de continuar simulando fora de vista ou resetar ao sair/voltar.

> Esta é a peça de maior risco técnico do projeto (sinalizada desde a Etapa 1 da análise de produção). O escopo aqui é **v1, validado com 3 Floors** — a prova em escala real (10 Floors + térreo) é a Sprint 32 (Deadline 8), não esta sprint.

---

## 1. A decisão central: Floor existir ≠ Floor simular

Regra do GDD Seção 24 aplicada literalmente: **não** significa manter todos os GameObjects fora do Current Floor rodando `Update()`. Significa só **não destruir nada** — os inimigos de um Floor "dormindo" continuam existindo na Scene, só param de processar lógica por frame. Estado (posição, HP, timers em andamento) fica congelado no exato ponto em que parou, porque nada roda pra mudá-lo — não precisa de nenhum mecanismo de "salvar e restaurar" separado.

## 2. FloorActivationCheck — lógica pura primeiro

`Assets/Scripts/Core/FloorActivationCheck.cs`:
```csharp
public static class FloorActivationCheck
{
    public static bool IsActive(FloorDefinition ownerFloor, FloorDefinition currentFloor)
    {
        if (ownerFloor == null) return true; // sem Floor dono definido = sempre ativo (compatibilidade com objetos que não pertencem a nenhum Floor específico)
        return ownerFloor == currentFloor;
    }
}
```

`Assets/Tests/EditMode/FloorActivationCheckTests.cs`:
```csharp
using NUnit.Framework;
using UnityEngine;

public class FloorActivationCheckTests
{
    [Test]
    public void IsActive_SameFloor_ReturnsTrue()
    {
        var floorObj = new GameObject();
        var floor = floorObj.AddComponent<FloorDefinition>();

        Assert.IsTrue(FloorActivationCheck.IsActive(floor, floor));

        Object.DestroyImmediate(floorObj);
    }

    [Test]
    public void IsActive_DifferentFloor_ReturnsFalse()
    {
        var floorAObj = new GameObject();
        var floorA = floorAObj.AddComponent<FloorDefinition>();
        var floorBObj = new GameObject();
        var floorB = floorBObj.AddComponent<FloorDefinition>();

        Assert.IsFalse(FloorActivationCheck.IsActive(floorA, floorB));

        Object.DestroyImmediate(floorAObj);
        Object.DestroyImmediate(floorBObj);
    }

    [Test]
    public void IsActive_NoOwnerFloor_ReturnsTrue()
    {
        var floorObj = new GameObject();
        var floor = floorObj.AddComponent<FloorDefinition>();

        Assert.IsTrue(FloorActivationCheck.IsActive(null, floor));

        Object.DestroyImmediate(floorObj);
    }
}
```
O terceiro teste importa tanto quanto os outros dois: garante que objetos sem `ownerFloor` definido (o Player, o Vendor, qualquer coisa que a gente não queira gatear por Floor) continuam funcionando normalmente em vez de quebrar silenciosamente.

## 3. EnemyController ganha dono de Floor

Em `EnemyController.cs` (Sprint 13), adicionar:
```csharp
public FloorDefinition ownerFloor;
```
E no topo do `Update()`:
```csharp
protected virtual void Update()
{
    if (!GameplayGate.IsActive) return;
    if (!FloorActivationCheck.IsActive(ownerFloor, FloorManager.Instance.CurrentFloor)) return;
    if (isDead || player == null) return;
    // ... resto do método igual à Sprint 13/14
}
```

## 4. FloorPopulationManager também dorme

Em `FloorPopulationManager.cs` (Sprint 14), adicionar:
```csharp
public FloorDefinition ownerFloor;
```
No topo do `Update()`:
```csharp
private void Update()
{
    if (!GameplayGate.IsActive) return;
    if (!FloorActivationCheck.IsActive(ownerFloor, FloorManager.Instance.CurrentFloor)) return;
    // ... resto igual à Sprint 14
}
```
E em `SpawnOne()`, atribuir o dono ao inimigo recém-criado:
```csharp
private void SpawnOne()
{
    if (aliveEnemies.Count >= config.maximum) return;
    if (spawnPoints.Length == 0) return;

    var point = spawnPoints[Random.Range(0, spawnPoints.Length)];
    var prefab = Random.value < 0.5f ? meleePrefab : rangedPrefab;
    if (prefab == null) return;

    var enemyObj = Instantiate(prefab, point.position, Quaternion.identity);
    var enemyController = enemyObj.GetComponent<EnemyController>();
    if (enemyController != null) enemyController.ownerFloor = ownerFloor;

    aliveEnemies.Add(enemyObj);
}
```
Assim cada inimigo nasce já sabendo a qual Floor pertence, sem precisar de configuração manual por prefab.

**Consequência importante, de graça:** o `respawnTimer` do `FloorPopulationManager` também congela quando o Floor dorme (a linha de gating fica antes do incremento do timer) — sair e voltar de um Floor **não** acelera nem reseta a reposição de inimigos. É exatamente a regra anti-exploit do GDD Seção 24 ("sair e voltar não pode parecer reset artificial"), sem precisar de nenhum código extra pra garantir isso — é consequência direta de onde o gating foi colocado.

## 5. Montar na Scene

- `PopulationManager_Floor1` (Sprint 14) ganha `ownerFloor = Floor_1` (o `FloorDefinition` do próprio Floor 1 — não existia esse campo antes desta sprint).
- Criar `PopulationManager_Floor2`, mesmo componente, `ownerFloor = Floor_2`, config menor pra diferenciar no teste (ex.: `minimum=2, target=4, maximum=6`), com seus próprios `spawnPoints` na área do Floor 2.

Com isso, os 3 Floors da Deadline 2 (Ground, Floor 1, Floor 2) agora têm: Ground sem população (é só o térreo), Floor 1 e Floor 2 cada um com sua própria simulação independente, gateada por Floor Sleep.

---

## 6. Teste manual (Play Mode) — a prova real da sprint

1. Entrar no `Floor_1`, esperar a população crescer, observar inimigos se movendo/atacando normalmente.
2. Deixar pelo menos 1 inimigo **no meio de um telegraph** (ver o log "Telegraph..." aparecer) e, nesse exato momento, subir a escada pro `Floor_2`.
3. No `Floor_2`: observar que a população dele começa a crescer independentemente (spawn próprio, budget próprio).
4. Voltar pro `Floor_1` (descida) → o inimigo que estava em telegraph **continua exatamente de onde parou** (não reiniciou o ataque, não pulou pro hit direto, não sumiu) — prova que o estado ficou congelado, não resetado nem perdido.
5. Contar quantos inimigos existiam no `Floor_1` antes de sair vs. depois de voltar → **mesma quantidade** (population não avançou "escondida" enquanto o Floor dormia, nem foi zerada).
6. Repetir o ciclo Floor_1 → Floor_2 → Floor_1 várias vezes rápido, tentando forçar algum reset perceptível de população ou de HP de algum inimigo específico → nada deve mudar além do que já estava acontecendo antes de sair.

---

## 7. Git

```
git add .
git commit -m "feat: floor activation check (pure logic) + editmode tests"
```
```
git add .
git commit -m "feat: enemy controller respects floor sleep (owner floor gating)"
```
```
git add .
git commit -m "feat: floor population manager respects floor sleep + respawn timer freezes correctly"
```
```
git add .
git commit -m "feat: population manager for floor 2, floor 1 ownerFloor wired correctly"
git push
```

## 8. Fechamento

`docs/sprints/sprint-15.md` (este arquivo) + linha no `docs/sprints/index.md` + linha no `docs/changelog.md`.

Vale registrar explicitamente no relatório: esta é a validação **v1, com 3 Floors**. A validação em escala real (10 Floors + térreo, com profiling de performance) é a **Sprint 32 (Deadline 8)** — se algo aqui precisar de ajuste de arquitetura quando escalar, é esperado, não sinal de que esta sprint saiu errada.

---

**Pronto quando:** os 3 testes de `FloorActivationCheck` passam verdes; inimigos e população de um Floor param completamente de simular ao sair dele (sem consumir ciclo de CPU desnecessário, sem se mover, sem atacar) e retomam exatamente do ponto congelado ao voltar; nenhum ciclo de troca de Floor altera contagem de população ou estado de ataque de forma inesperada.
