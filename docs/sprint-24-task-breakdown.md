# Sprint 24 — Boss Framework + Boss Timer + Bosses Floor 1

**Deadline 6 — Heróis II + Bosses (Floor 1-2).** Dependência: Sprint 23 (Cleric, fechada) e `docs/sprint-24-boss-spawn-system-specs.md` (decisões de design já confirmadas pelo usuário, GDD Seção 22 já atualizado).

Escopo oficial: Boss Timer vira um intervalo periódico de 10s por Floor (rotação entre os bosses disponíveis, empilha, nunca reseta exceto no fim do dia); todo boss nasce já em combate, em qualquer ponto caminhável do Floor; os 3 bosses de Floor 1 ficam derrotáveis — **Goblin King** (padrão genérico do Boss Framework, igual a qualquer Melee comum) e **Mother Slime Green/Blue** (dano de contato simples, igual ao Slime comum, mais o spawn de 3 filhotes em posições fixas ao morrer).

---

## Seção 0 — Decisões e suposições (leia antes de revisar o código)

1. **`FloorSpawnUtility.cs` — extração, não invenção.** O spec da Sprint 24 pede pra reaproveitar `TryGetRandomGraphPoint()` do `FloorPopulationManager`, mas esse método é `private`. Em vez de duplicar ~25 linhas de lógica de pathfinding no `BossSpawnManager` novo, ou criar uma dependência frágil de `GetComponent` entre os dois componentes irmãos, extraí `TryGetRandomGraphPoint`/`GetOwnerGraph`/`RestrictToOwnerGraph` pra uma classe estática nova, usada pelos dois. **Comportamento idêntico ao que já existia** — `FloorPopulationManager` não muda de regra nenhuma, só de onde a lógica mora.

2. **`BossSpawnManager` é um componente irmão novo, não um modo do `FloorPopulationManager`.** Timer, rotação e spawn de boss são completamente separados da população comum — bosses não entram em `aliveEnemies`/Minimum/Target/Maximum, não têm fase de "preenchimento instantâneo" ao entrar no Floor (diferente da população comum), é só um relógio fixo que nunca para enquanto o Floor está ativo. **Suposição de hierarquia** (mesma already assumida pelo `FloorPopulationManager.ownerFloor`): fica no mesmo GameObject que `FloorDefinition`/`FloorPopulationManager` de cada Floor. Se a Scene real usa outra estrutura, é só ajustar onde o componente é adicionado — a lógica interna não muda.

3. **`isBoss` e agressão imediata viram setters públicos em `EnemyController`**, chamados pelo `BossSpawnManager` logo após `Instantiate()` — em vez de depender de cada prefab de boss já vir configurado certo no Inspector (decisão sugerida pelo próprio spec da sprint). `SetIsBoss(true)` e `ForceImmediateAggro()` (liga `isInCombat`/`combatLocked` direto, sem passar por `observationRadius`).

4. **`AnimationDieEndEvent()` vira `virtual` em `EnemyController`.** Hoje é o único jeito de rodar código no momento exato da morte de um monstro (antes de `Destroy(gameObject)`, depois da animação de die terminar) — precisa disso pro `MotherSlimeController` spawnar os 3 filhotes nas posições certas (filhas do próprio GameObject, que deixam de existir depois do `Destroy`). Mudança de base zero-risco: nenhum dos outros ~29 monstros sobrescreve esse método hoje, então nada muda pra eles.

5. **Resolvido pelo usuário: boss × camuflagem do Ranger.** Durante a camuflagem, o boss perde o alvo igual a qualquer monstro comum (`isInCombat = false`, fica "na fase" sem saber que o jogador existe). Mas no instante exato em que a camuflagem **termina**, o boss recupera o alvo automaticamente — sem precisar que o jogador volte a entrar no `observationRadius` dele (diferente de monstro comum, que precisa ser redetectado de verdade). Implementado detectando a transição `true -> false` de `HeroController.IsPlayerUntargetable` (mesmo padrão de `wasUsingSecondaryAbility` em `HeroController.Update()`) e chamando `ForceImmediateAggro()` de novo nesse instante, só pra boss (`isBoss`). Ver diff de `EnemyController.cs` abaixo.

6. **Rotação (`rotationOrder`/`rotationIndex`) e o timer (`bossSpawnTimer`) nunca resetam em código — nem no fim do dia.** Isso espelha exatamente a mesma limitação que **já existe hoje** no `FloorPopulationManager` (`aliveEnemies`/crise/escalação também não têm nenhum reset de fim de dia em código, apesar do GDD dizer que população é estado Daily). `GameEvents.OnDayStart` existe e já é disparado (`DayTimer.ResetForNewDay()`), mas nada se inscreve nele hoje. Não criei esse reset pro Boss Timer sozinho — seria resolver uma dívida maior (reset de Daily state) que não é escopo desta sprint e afetaria população comum também. Fica registrado como dívida técnica pré-existente, não uma lacuna nova desta sprint.

7. **Mother Slime Green/Blue: 1 script compartilhado, 2 prefabs.** `MotherSlimeController` (herda de `SlimeEnemyController`) recebe o prefab do filho (`Slime_Green.prefab` ou `Slime_Blue.prefab`, já existentes) e os 3 pontos fixos via Inspector — não tem nenhuma lógica de cor embutida no código, cada prefab de boss aponta pro próprio filho.

8. **Goblin King não precisa de nenhum script novo.** Bestiário é explícito: "igual a qualquer Melee comum" — o prefab usa `MeleeEnemyController` (já existe, já é o que Werewolf/Centaur King/etc. usam), só com stats/sprites/Animator Controller próprios via `MonsterAnimationGeneratorWindow` (mesma ferramenta já usada em todo boss de Floor 2).

9. **`bossSpawnInterval` é `public float`, não `[SerializeField] private`** — mesmo padrão de visibilidade de `respawnInterval` no `FloorPopulationManager` (ajustável no Inspector por Floor, caso Floors diferentes precisem de intervalos diferentes no futuro, embora o spec descreva 10s como valor único por enquanto).

---

## Mudanças em arquivos existentes

### `EnemyController.cs` (diff)

```csharp
    // Sprint 24 (Boss Spawn System) — permite ao spawner marcar o boss sem depender do
    // Inspector de cada prefab já vir configurado certo (decisão do spec da sprint).
    public void SetIsBoss(bool value) => isBoss = value;
```
(perto do campo `isBoss`/`ApplyKnockback`.)

```csharp
    // Sprint 24 (Boss Spawn System) — decisão do usuário: todo boss nasce já em combate,
    // mesmo longe do jogador, sem fase de patrulha/idle esperando observationRadius
    // detectar (diferente de todo monstro comum). Chamado pelo BossSpawnManager logo após
    // Instantiate(), junto com SetIsBoss(true) e ownerFloor.
    public void ForceImmediateAggro()
    {
        combatLocked = true;
        isInCombat = true;
    }
```
(perto de `TakeDamage()`, que já faz exatamente esse mesmo par de atribuições pro caso de "tomou dano".)

```csharp
    // virtual a partir desta sprint — MotherSlimeController precisa rodar código (spawnar
    // os 3 filhotes) no momento exato da morte, antes do Destroy(gameObject) deste método.
    // Nenhum dos outros monstros sobrescreve isso hoje — zero mudança de comportamento pra
    // eles.
    public virtual void AnimationDieEndEvent()
```
(era `public void AnimationDieEndEvent()` — só adiciona `virtual`, corpo do método não muda.)

```csharp
    // Sprint 24 (Boss Spawn System) — detecta a transição true -> false da Camuflagem
    // (mesmo padrão de wasUsingSecondaryAbility em HeroController.Update()), só pra saber o
    // instante exato em que ela ACABA de cair.
    private bool wasPlayerUntargetable;
```
(novo campo privado, perto de `combatLocked`.)

E no `Update()`, troca a linha única do bloco de Camuflagem/stealth (depois de `if (player == null) return;`) por:

```csharp
    // Camuflagem/stealth do herói (GDD Seção 16/17 — Ranger, futuramente Druid/Assassin) —
    // o monstro perde o alvo de verdade (isInCombat=false), não só congela: continua se
    // movendo/tocando a própria animação de patrulha normalmente, só sem saber que o player
    // existe. Monstro comum precisa redetectar via observationRadius depois que a
    // camuflagem cai (ver guarda em UpdatePatrol) — não retoma perseguição sozinho.
    //
    // Boss é a ÚNICA exceção (decisão do usuário, Sprint 24): perde o alvo durante a
    // camuflagem igual a qualquer monstro comum, mas RECUPERA automaticamente no instante
    // exato em que ela cai, sem esperar o jogador voltar pro observationRadius — mesmo
    // critério de "agressão imediata" do spawn, reaplicado na queda da camuflagem.
    bool isPlayerUntargetable = HeroController.IsPlayerUntargetable;
    if (isPlayerUntargetable) isInCombat = false;
    else if (wasPlayerUntargetable && isBoss) ForceImmediateAggro();
    wasPlayerUntargetable = isPlayerUntargetable;
```
(era só `if (HeroController.IsPlayerUntargetable) isInCombat = false;`.)

### `FloorPopulationManager.cs` (diff — refatoração, comportamento idêntico)

Remove os campos/métodos privados `cachedWalkableNodes`, `TryGetRandomGraphPoint()`, `GetOwnerGraph()`, `RestrictToOwnerGraph()` — tudo migrado pra `FloorSpawnUtility` (ver abaixo). `SpawnOne()` passa a chamar a versão estática:

```csharp
    private List<GraphNode> cachedWalkableNodes; // continua aqui — passado por ref pro utilitário, cache é por instância (por Floor)

    private void SpawnOne()
    {
        if (monsterPrefabs.Length == 0) return;
        if (!FloorSpawnUtility.TryGetRandomWalkablePoint(ownerFloor, ref cachedWalkableNodes, out var spawnPosition)) return;

        var prefab = monsterPrefabs[Random.Range(0, monsterPrefabs.Length)];
        if (prefab == null) return;

        var enemyObj = Instantiate(prefab, spawnPosition, Quaternion.identity);
        var enemyController = enemyObj.GetComponent<EnemyController>();
        if (enemyController != null) enemyController.ownerFloor = ownerFloor;

        FloorSpawnUtility.RestrictToOwnerGraph(enemyObj, ownerFloor);

        aliveEnemies.Add(enemyObj);
    }
```

---

## `FloorSpawnUtility.cs` (novo)

```csharp
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;

// Extraído do FloorPopulationManager nesta sprint — o BossSpawnManager precisa exatamente
// da mesma lógica de "ponto caminhável aleatório dentro do GridGraph do Floor" que o spawn
// de monstro comum já usava. Comportamento idêntico ao que já existia, só de local novo —
// ver Seção 0, item 1 do breakdown desta sprint.
public static class FloorSpawnUtility
{
    public static bool TryGetRandomWalkablePoint(FloorDefinition ownerFloor, ref List<GraphNode> cachedWalkableNodes, out Vector3 point)
    {
        point = default;

        if (cachedWalkableNodes == null)
        {
            var graph = GetOwnerGraph(ownerFloor);
            if (graph == null) return false;

            cachedWalkableNodes = new List<GraphNode>();
            graph.GetNodes(node => { if (node.Walkable) cachedWalkableNodes.Add(node); });
        }

        if (cachedWalkableNodes.Count == 0) return false;

        point = (Vector3)PathUtilities.GetPointsOnNodes(cachedWalkableNodes, 1)[0];
        return true;
    }

    public static NavGraph GetOwnerGraph(FloorDefinition ownerFloor)
    {
        if (ownerFloor == null || string.IsNullOrEmpty(ownerFloor.astarGraphName)) return null;
        if (AstarPath.active == null) return null;
        return AstarPath.active.data.FindGraph(g => g.name == ownerFloor.astarGraphName);
    }

    public static void RestrictToOwnerGraph(GameObject obj, FloorDefinition ownerFloor)
    {
        var graph = GetOwnerGraph(ownerFloor);
        if (graph == null) return;

        var seeker = obj.GetComponent<Seeker>();
        if (seeker != null) seeker.graphMask = GraphMask.FromGraph(graph);
    }
}
```

---

## `BossSpawnManager.cs` (novo)

```csharp
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;

// Boss Timer — regra final (GDD Seção 22, correção Sprint 24, decisão do usuário). Mesmo
// padrão arquitetural do FloorPopulationManager (componente irmão de FloorDefinition, nunca
// destruído/recriado ao trocar de Floor, gateado por FloorActivationCheck — "congela"
// sozinho quando o jogador sai do Floor, sem precisar de nenhum save/load explícito), mas é
// um sistema PARALELO e independente da população comum: bosses não entram em
// aliveEnemies/Minimum/Target/Maximum, e não existe fase de "preenchimento instantâneo" ao
// entrar no Floor — é só um relógio fixo que nunca para enquanto o Floor está ativo.
public class BossSpawnManager : MonoBehaviour
{
    // Vazio = Floor sem boss AINDA PRODUZIDO (Floor 3+ antes da Deadline 8) — estado de
    // produção incompleta, não uma feature opt-in (decisão do usuário, GDD Seção 22).
    public GameObject[] bossPrefabs;
    public FloorDefinition ownerFloor;

    public float bossSpawnInterval = 10f; // 🔢 GDD Seção 22 — intervalo fixo, placeholder de teste

    private float bossSpawnTimer;
    private List<GameObject> rotationOrder;
    private int rotationIndex;
    private List<GraphNode> cachedWalkableNodes; // mesmo cache-sob-demanda do FloorPopulationManager, instância própria

    private void Update()
    {
        if (!GameplayGate.IsActive) return;
        if (!FloorActivationCheck.IsActive(ownerFloor, FloorManager.Instance.CurrentFloor)) return;
        if (bossPrefabs.Length == 0) return;

        bossSpawnTimer += Time.deltaTime;
        if (bossSpawnTimer < bossSpawnInterval) return;

        // Subtrai o intervalo (não zera cru) — preserva o excedente de um frame com
        // deltaTime maior que o normal, em vez de perder aquela fração pra sempre. O
        // relógio é "fixo e contínuo" (GDD Seção 22) — acúmulo de frame não pode ir
        // empurrando o próximo spawn pra mais tarde, por menor que seja o desvio.
        bossSpawnTimer -= bossSpawnInterval;

        SpawnNextBossInRotation();
    }

    // Decisão do usuário (GDD Seção 22, correção Sprint 24): a ordem só é sorteada 1x — com
    // N bosses, depois que todos já nasceram 1x cada, repete a MESMA ordem pra sempre, nunca
    // sorteia de novo. Lista de 1 elemento "embaralhada" sempre resulta nela mesma, cobrindo
    // o caso degenerado de N=1 sem nenhuma exceção de código.
    private void BuildRotationIfNeeded()
    {
        if (rotationOrder != null) return;

        rotationOrder = new List<GameObject>(bossPrefabs);
        for (int i = rotationOrder.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (rotationOrder[i], rotationOrder[j]) = (rotationOrder[j], rotationOrder[i]);
        }
    }

    private void SpawnNextBossInRotation()
    {
        BuildRotationIfNeeded();

        var prefab = rotationOrder[rotationIndex];
        rotationIndex = (rotationIndex + 1) % rotationOrder.Count;
        if (prefab == null) return;

        // Mesma posição de monstro comum (GDD Seção 23, "Onde monstros nascem") — qualquer
        // ponto caminhável do Floor, não um ponto fixo de arena (decisão do usuário).
        if (!FloorSpawnUtility.TryGetRandomWalkablePoint(ownerFloor, ref cachedWalkableNodes, out var spawnPosition)) return;

        var bossObj = Instantiate(prefab, spawnPosition, Quaternion.identity);
        var enemyController = bossObj.GetComponent<EnemyController>();
        if (enemyController == null) return;

        enemyController.ownerFloor = ownerFloor;
        FloorSpawnUtility.RestrictToOwnerGraph(bossObj, ownerFloor);

        enemyController.SetIsBoss(true);
        enemyController.ForceImmediateAggro(); // decisão do usuário — sem fase de patrulha/idle

        // Decisão do usuário: bosses EMPILHAM — sem checagem de "já tem boss vivo", sem
        // fila, sem limite de bosses simultâneos. O timer já foi decrementado acima,
        // independente deste spawn ter dado certo ou não (não reseta com morte nem com
        // sucesso/falha de spawn).
    }
}
```

---

## `MotherSlimeController.cs` (novo)

```csharp
using UnityEngine;

// Mother Slime Green/Blue (Bestiário, Andar 1 — Bosses). Mesma arquitetura do Slime comum
// (SlimeEnemyController: só dano de contato, hasAttackAnimation=false no Inspector, pula e
// causa dano ao encostar, sem attack dedicado "pra sempre" — exceção permanente do jogo).
// A única adição: ao morrer, nascem 3 Slimes normais em POSIÇÕES FIXAS (3 Transforms filhos
// deste próprio GameObject, configurados no prefab — GDD é explícito que não são
// aleatórios). 1 script serve os dois bosses — childSlimePrefab decide a cor.
public class MotherSlimeController : SlimeEnemyController
{
    [SerializeField] private GameObject childSlimePrefab; // Slime_Green.prefab ou Slime_Blue.prefab, conforme o boss
    [SerializeField] private Transform[] childSpawnPoints; // 3 pontos fixos, filhos deste GameObject (GDD)

    private bool childrenSpawned; // própria trava — AnimationDieEndEvent() pode ser chamado 2x (evento real + timeout de segurança da base), e childSpawnPoints deixam de existir depois do Destroy() da base

    public override void AnimationDieEndEvent()
    {
        if (!childrenSpawned)
        {
            childrenSpawned = true;
            SpawnChildren();
        }
        base.AnimationDieEndEvent(); // dieHandled (privado, na base) já evita destruir/dropar loot 2x
    }

    private void SpawnChildren()
    {
        if (childSlimePrefab == null) return;
        foreach (var point in childSpawnPoints)
        {
            if (point == null) continue;
            Instantiate(childSlimePrefab, point.position, Quaternion.identity);
        }
    }
}
```

---

## Goblin King — sem script novo

Usa `MeleeEnemyController` (mesma classe de Werewolf/Centaur King/qualquer Melee comum) — golpe real via Animation Event, 4 hitboxes diagonais, sem nenhuma mecânica própria (Bestiário confirma: "igual a qualquer Melee comum"). `isBoss` não precisa estar marcado no Inspector — o `BossSpawnManager` liga via `SetIsBoss(true)` em runtime (ver Seção 0, item 3).

---

## Setup necessário no Editor (fora de código)

1. **3 prefabs novos** em `Assets/Prefabs/Monstros/Floor1/Bosses/` (ou pasta equivalente): `Goblin_King.prefab` (`MeleeEnemyController`), `Mother_Slime_Green.prefab` e `Mother_Slime_Blue.prefab` (`MotherSlimeController`, cada um com `childSlimePrefab` apontando pro próprio `Slime_Green.prefab`/`Slime_Blue.prefab` já existente, e 3 `childSpawnPoints` posicionados ao redor do boss).
2. **Animator Controllers** via `MonsterAnimationGeneratorWindow` (mesma ferramenta já usada em Werewolf/Centaur King/Rat People Royalty/Spider Queen): Goblin King reaproveita o override controller base de Melee comum (idle/walk/idleCombat/attack com Animation Event/damage/die, 4 diagonais); Mother Slime Green/Blue reaproveitam o override controller base do Slime comum (idle/walk/damage/die — sem estado de `attack` dedicado, `hasAttackAnimation=false` no Inspector, igual ao Slime normal).
3. **`BossSpawnManager`**, adicionado no GameObject do Floor 1 (mesmo objeto que já tem `FloorDefinition`/`FloorPopulationManager`): `bossPrefabs = [Goblin_King, Mother_Slime_Green, Mother_Slime_Blue]`, `ownerFloor` apontando pro próprio `FloorDefinition` do Floor 1. Floor 2+ ficam pra Sprint 25/26 (bosses ainda não produzidos).
4. **`isBoss`** não precisa ser marcado manualmente em nenhum dos 3 prefabs (ver Seção 0, item 3) — mas vale deixar marcado no Inspector mesmo assim, por clareza visual pra quem for abrir o prefab depois (`SetIsBoss(true)` sobrescreve de qualquer forma em runtime).
5. **`EnemyContactDamage`** nos 2 prefabs de Mother Slime (mesmo componente que `Slime_Green.prefab`/`Slime_Blue.prefab` comuns já usam — `SlimeEnemyController.Awake()` já inicializa ele automaticamente).

## Checklist de teste manual

1. A cada 10s (Floor ativo), nasce exatamente 1 boss — nunca 0, nunca mais de 1 por tick.
2. Com os 3 bosses de Floor 1 disponíveis: a 1ª rodada (10s/20s/30s) nasce uma ordem aleatória sem repetir nenhum dos 3; a partir da 2ª rodada (40s/50s/60s), a MESMA ordem se repete — não sorteia de novo.
3. Boss nascido aos 10s ainda vivo aos 20s: nasce um segundo boss em cima dele, os dois ficam vivos ao mesmo tempo (empilham, sem fila).
4. Matar um boss no meio do intervalo (ex.: aos 13s) não acelera nem atrasa o próximo spawn — continua batendo exatamente nos múltiplos de 10s desde o início da contagem do Floor.
5. Sair do Floor 1 com o timer em, por exemplo, 6s acumulados, trocar de Floor, voltar — o próximo boss nasce em mais 4s (não reseta, não recomeça do zero).
6. Todo boss nasce já perseguindo o jogador na hora, mesmo nascendo longe/fora da tela — sem fase de patrulha/idle.
7. Boss nasce em qualquer ponto caminhável sorteado do Floor (não um ponto fixo), igual a um monstro comum.
8. Goblin King ataca com animação real (Animation Event), é imune a knockback (`isBoss`), e morre normalmente (drop de loot via `AnimationDieEndEvent` da base).
9. Mother Slime Green morrendo nasce exatamente 3 Slime Green normais, nas 3 posições fixas configuradas no prefab — mesmo teste pra Mother Slime Blue com Slime Blue.
10. Forçar o timeout de segurança do `AnimationDieEndEvent` (ex.: Animator sem o evento configurado) na Mother Slime ainda spawna os 3 filhotes exatamente 1 vez, não 2 (evento real + timeout não duplicam).
11. `bossPrefabs` vazio num Floor (ex.: Floor 3, ainda sem conteúdo) não gera erro nenhum — só não nasce boss, resto do Floor funciona normal.
12. Camuflar o Ranger com um boss vivo em combate: o boss perde o alvo (fica "na fase", igual monstro comum) enquanto a camuflagem durar.
13. No instante em que a camuflagem acaba, o boss retoma a perseguição automaticamente, mesmo que o jogador esteja longe (fora do `observationRadius` dele) — sem precisar se aproximar de novo pra ser redetectado.
14. Mesmo teste com um monstro COMUM perto: ele continua precisando que o jogador volte a entrar no `observationRadius` depois da camuflagem cair — a recuperação automática é exclusiva de boss.
