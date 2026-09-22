# Sprint 20 — Floor 1A–2A + Bestiary Batch 1+2 Completo (Floors 1–2, todo o elenco)

**Depende de:** Sprint 19b (`StatusEffectController`/`IDamageable`, correções de base em mira — não afetam nada desta sprint diretamente, só documentando a corrente)
**Objetivo:** Floor 1A e 2A jogáveis com layout e população real; Floors 1–2 com o elenco completo do Bestiário (10 fichas novas — Wolf, Bat, Slime Green/Blue, Goblin Raider, Goblin Sapper, Centaur, Minotaur, Gnoll, Spider, Ancient Troll — somadas ao Rat/Goblin/Rat People já prontos desde a Sprint 16).

Essa sprint é majoritariamente **conteúdo**, não arquitetura — a maior parte dos monstros novos são reskins puros de classes que já existem e já rodam em produção. Código novo de verdade só existe onde o Bestiário documenta uma exceção.

---

## Seção 0 — Decisões explícitas antes de começar

1. **8 dos 10 monstros novos são reskins puros, sem nenhuma linha de código nova:** Wolf, Bat, Centaur, Minotaur, Gnoll, Spider e Ancient Troll usam `MeleeEnemyController` direto (mesma classe do Rat/Goblin desde a Sprint 16); Goblin Raider usa `RangedEnemyController` direto (mesma classe do Rat People). Só stats (`EnemyStats`), Animator gerado pela `MonsterAnimationGeneratorWindow` e entrada no `FloorPopulationManager`.
2. **Goblin Raider não precisa de projétil novo** — a ficha descreve exatamente o mesmo mecanismo do Rat People (arremessa algo que explode em área ao contato ou na distância máxima). Como `RangedEnemyController.ExecuteAttackHit()` já é 100% genérico (só passa direção + dano pro `EnemyProjectile`, sem nenhum parâmetro específico do Rat People), e o Rat People já entrega esse comportamento hoje usando exatamente essa classe, a leitura mais direta é que `EnemyProjectile` já implementa a explosão em área como comportamento padrão — não vi o arquivo em si, mas não há nenhum outro lugar no código onde esse comportamento poderia estar. Só um novo prefab de projétil (visual de tocha em vez de pedra). **Se isso estiver errado** (se o Rat People tiver algum override/wrapper que eu não vi), o sintoma vai ser óbvio no teste manual (T08) — a tocha aplicaria dano direto sem explodir em área — e a correção é isolada, não derruba o resto da sprint.
3. **Slime Green/Blue e a camada de contato do Goblin Sapper compartilham 1 componente novo** (`EnemyContactDamage`) — os dois são os únicos casos do jogo com dano de contato passivo (Bestiário, regra padrão): Slime pra sempre, Goblin Sapper como camada extra somada à bomba. Em vez de duplicar a lógica de cooldown+`OnTriggerStay2D` duas vezes, os dois montam o mesmo componente e só chamam `Initialize()` com valores diferentes.
4. **Goblin Sapper é uma classe nova própria** (`GoblinSapperController`), não uma variação de `MeleeEnemyController` — o ciclo armado/bomba/fuga/recarga não cabe no contrato de `Move()`/`InAttackRange()`/`ExecuteAttackHit()` pensado pro Melee comum (ataque por Animation Event), e a ficha já documenta ele como "Exceção à regra padrão". `hasAttackAnimation = false` no Inspector (mesmo mecanismo do Slime) — o Sapper não tem clipe `attack` na lista de animações da própria ficha.
5. **Dano da bomba e da auto-explosão da morte são multiplicadores sobre `stats.attackDamage`** (não campos soltos duplicados) — a ficha só diz "deve ser maior que o de contato", sem valor fechado; multiplicador é o mesmo padrão já usado em outros lugares do projeto (`ultimateDamageMultiplier` do Mage/Ranger) em vez de inventar uma stat nova.
6. **Layout físico dos Floors (tilemap, posição de parede/decoração) é trabalho de nível, não de código** — esta sprint cobre os sistemas (`FloorDefinition`/`FloorPopulationManager`/spawn points) e a configuração deles; o desenho da sala em si é você quem monta na Scene, não tem "código" pra propor aqui.
7. **`PopulationConfig` de Floor 1A/2A fica no default da classe (3/8/12)** — nem o Bestiário nem o GDD que revisei têm um número diferente documentado especificamente pra esses 2 Floors; ajustável em playtest, mesmo espírito de todo `🔢` já usado no projeto.

---

## 1. `EnemyContactDamage.cs` (novo) — dano de contato passivo compartilhado

```csharp
using UnityEngine;

// Dano de contato passivo com cooldown — usado só nos 2 casos que o Bestiário documenta como
// exceção à regra geral de "sem dano de contato" (ver "Regra padrão de combate"): Slime
// Green/Blue (exceção permanente) e a camada extra de contato do Goblin Sapper (soma-se à
// bomba, não a substitui). Componente à parte em vez de método privado duplicado nas duas
// classes — reaproveitável por qualquer outro monstro de contato que apareça no futuro.
public class EnemyContactDamage : MonoBehaviour
{
    private float damage;
    private float cooldownDuration;
    private float cooldownRemaining;
    private bool initialized;

    public void Initialize(float contactDamage, float cooldown)
    {
        damage = contactDamage;
        cooldownDuration = cooldown;
        initialized = true;
    }

    private void Update()
    {
        if (!GameplayGate.IsActive) return;
        if (cooldownRemaining > 0f) cooldownRemaining -= Time.deltaTime;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!initialized || !GameplayGate.IsActive) return;
        if (cooldownRemaining > 0f) return;
        if (!other.CompareTag("Player")) return;

        var hero = other.GetComponent<HeroController>();
        if (hero == null) return;

        hero.TakeDamage(damage);
        cooldownRemaining = cooldownDuration;
    }
}
```

---

## 2. `SlimeEnemyController.cs` (novo)

```csharp
using UnityEngine;

// Única exceção permanente do jogo (Bestiário) — nunca tem attack real, só dano de contato.
// hasAttackAnimation precisa ficar FALSE no Inspector deste prefab: isso já desliga sozinho
// todo o fluxo de animação de ataque herdado de EnemyController (Update()/UpdateCombat()
// pulam attackAnimationCooldown.Tick()/TryStartAttackAnimation() inteiros quando é false).
public class SlimeEnemyController : EnemyController
{
    protected override void Awake()
    {
        base.Awake();
        var contactDamage = GetComponent<EnemyContactDamage>();
        if (contactDamage != null) contactDamage.Initialize(stats.attackDamage, stats.attackAnimationCooldown);
    }

    // Sempre persegue direto — Slime não flanqueia, não usa MeleeAttackSlotManager (a fila
    // de vaga existe pra evitar hordas competindo pela MESMA janela de animação de ataque;
    // Slime não tem janela nenhuma, cada um aplica seu próprio dano de contato de forma
    // independente, sem disputa).
    protected override void Move()
    {
        Vector2 toPlayer = player.position - transform.position;
        SetMoving(true);
        MoveInDirection(toPlayer.normalized);
    }

    // Nunca chamados de verdade — hasAttackAnimation=false já impede o fluxo que os invocaria.
    protected override bool InAttackRange() => false;
    protected override void ExecuteAttackHit() { }
}
```

---

## 3. `GoblinSapperController.cs` (novo)

```csharp
using UnityEngine;

// Exceção à regra padrão (Bestiário) — ciclo armado (persegue) -> planta bomba no contato ->
// foge (sem bomba) até recarregar -> volta a perseguir. Some-se a isso uma camada de dano de
// contato passivo o tempo todo (EnemyContactDamage), armado ou não. hasAttackAnimation deve
// ficar FALSE no Inspector — a ficha não lista clipe "attack" nenhum (só idle/idle_bomb/walk/
// bomb_walk/damage/bomb_damage/die).
public class GoblinSapperController : EnemyController
{
    private enum SapperPhase { Armed, Fleeing }

    [Header("Bomba (GDD Bestiário — Goblin Sapper)")]
    [SerializeField] private GameObject bombPrefab; // precisa ter GoblinSapperBomb
    [SerializeField] private float bombExplosionDamageMultiplier = 2f; // 🔢 "deve ser maior que o de contato"

    [Header("Fuga e recarga")]
    [SerializeField] private float reloadDuration = 4f; // 🔢 ajustável
    [SerializeField] private float safeFleeRadius = 4f; // 🔢 raio onde ele vaga sem bomba

    [Header("Auto-explosão ao morrer")]
    [SerializeField] private float dieExplosionDamageMultiplier = 2.5f; // 🔢 ajustável
    [SerializeField] private float dieExplosionRadius = 1.8f; // 🔢 ajustável

    private SapperPhase phase = SapperPhase.Armed;
    private float fleeElapsed;
    private PatrolAI fleeAI;

    protected override void Awake()
    {
        base.Awake();
        fleeAI = new PatrolAI(1f, 2f, 1.5f, 3f); // 🔢 mesma ideia do flanco — timers curtos de ir/parar dentro do raio seguro
        var contactDamage = GetComponent<EnemyContactDamage>();
        if (contactDamage != null) contactDamage.Initialize(stats.attackDamage, stats.attackAnimationCooldown);
    }

    protected override void Move()
    {
        if (animator != null) animator.SetBool("IsArmed", phase == SapperPhase.Armed);

        Vector2 toPlayer = player.position - transform.position;
        float distance = toPlayer.magnitude;

        if (phase == SapperPhase.Armed)
        {
            if (distance <= stats.attackRadius)
            {
                PlantBomb();
                return;
            }
            SetMoving(true);
            MoveInDirection(toPlayer.normalized); // Bomb_Walk, alimentado por MoveX/MoveY como sempre
            return;
        }

        // Fleeing — recarrega por tempo, e ativamente aumenta distância se o player alcançar
        fleeElapsed += Time.deltaTime;
        if (fleeElapsed >= reloadDuration)
        {
            phase = SapperPhase.Armed;
            fleeElapsed = 0f;
            return;
        }

        if (distance < stats.attackRadius * 1.5f)
        {
            SetMoving(true);
            MoveInDirection(-toPlayer.normalized); // player alcançou de novo -- prioriza afastar
            return;
        }

        fleeAI.Tick(Time.deltaTime, () => (Vector2)transform.position + Random.insideUnitCircle * safeFleeRadius);
        bool wandering = fleeAI.CurrentPhase == PatrolPhase.Walking;
        SetMoving(wandering);
        if (wandering) MoveInDirection((fleeAI.WalkTarget - (Vector2)transform.position).normalized);
    }

    private void PlantBomb()
    {
        if (bombPrefab != null)
        {
            var bombObj = Instantiate(bombPrefab, player.position, Quaternion.identity);
            var bomb = bombObj.GetComponent<GoblinSapperBomb>();
            if (bomb != null) bomb.Initialize(stats.attackDamage * bombExplosionDamageMultiplier, ownerFloor);
        }
        phase = SapperPhase.Fleeing;
        fleeElapsed = 0f;
    }

    // Nunca chamados de verdade — hasAttackAnimation=false já impede o fluxo que os invocaria.
    protected override bool InAttackRange() => false;
    protected override void ExecuteAttackHit() { }

    // Animation Event no frame de explosão do próprio clipe "die" (auto-explosão ao morrer,
    // GDD: "igual em espírito à explosão da bomba, só que centrada no próprio Goblin Sapper").
    public void AnimationDieExplodeEvent()
    {
        var results = new Collider2D[4];
        int count = Physics2D.OverlapCircle(transform.position, dieExplosionRadius, new ContactFilter2D().NoFilter(), results);
        for (int i = 0; i < count; i++)
        {
            if (!results[i].CompareTag("Player")) continue;
            var hero = results[i].GetComponent<HeroController>();
            if (hero != null) hero.TakeDamage(stats.attackDamage * dieExplosionDamageMultiplier);
        }
    }
}
```

---

## 4. `GoblinSapperBomb.cs` (novo) — objeto separado, nasce nos pés do jogador

```csharp
using UnityEngine;

// Objeto independente do Goblin Sapper (GDD Bestiário: "a bomba não é o Goblin Sapper") —
// nasce nos pés do jogador com sua própria animação de carga, e tem 1 Animation Event no
// frame exato da explosão. Se o jogador ainda estiver dentro do círculo NESSE frame, sofre
// dano — pode se afastar antes disso e escapar ileso (única janela de esquiva real da ficha).
public class GoblinSapperBomb : MonoBehaviour
{
    [SerializeField] private float explosionRadius = 1.5f; // 🔢 ajustável
    private float damage;
    private FloorDefinition ownerFloor;

    public void Initialize(float bombDamage, FloorDefinition floor)
    {
        damage = bombDamage;
        ownerFloor = floor;
    }

    private void Update()
    {
        // Respeita Floor Sleep como qualquer outra entidade com timing (GDD Seção 24) — o
        // Animator próprio já conduz o tempo até a explosão sozinho via Animation Event; este
        // Update() só existe pra também congelar esse timing quando o Floor dorme.
        if (!GameplayGate.IsActive) return;
        if (ownerFloor != null && !FloorActivationCheck.IsActive(ownerFloor, FloorManager.Instance.CurrentFloor)) return;
    }

    // Animation Event, no frame exato da explosão.
    public void AnimationExplodeEvent()
    {
        var results = new Collider2D[4];
        int count = Physics2D.OverlapCircle(transform.position, explosionRadius, new ContactFilter2D().NoFilter(), results);
        for (int i = 0; i < count; i++)
        {
            if (!results[i].CompareTag("Player")) continue;
            var hero = results[i].GetComponent<HeroController>();
            if (hero != null) hero.TakeDamage(damage);
        }
        Destroy(gameObject);
    }
}
```

---

## 5. Reskins puros — tabela de stats (sem código novo)

Todos usam `MeleeEnemyController` (com os 4 `attackHitboxNE/NW/SE/SW` configurados, mesmo padrão do Rat/Goblin) exceto o Goblin Raider, que usa `RangedEnemyController` (com `projectilePrefab` apontando pro novo prefab de tocha). `attackAnimationCooldown`/`flankRadius` ficam no default de `EnemyStats` pra todos (sem valor específico documentado no Bestiário) — só `health`/`attackDamage` vêm direto da ficha, e `moveSpeed`/`observationRadius`/`attackRadius` levam um ajuste relativo onde a ficha menciona "velocidade superior".

| Monstro | Floor | Controller | `health`/`maxHealth` | `attackDamage` | Observação |
|---|---|---|---|---|---|
| Wolf | 1 | Melee | 10 | 3 | "velocidade superior" — `moveSpeed` acima do padrão (🔢, mesmo critério do Rat/Goblin) |
| Bat | 1 | Melee | 5 | 2 | — |
| Slime Green | 1 | `SlimeEnemyController` (Seção 2) | 5 | 2 | `hasAttackAnimation = false` |
| Slime Blue | 1 | `SlimeEnemyController` (Seção 2) | 5 | 2 | `hasAttackAnimation = false` |
| Goblin Raider | 1 | Ranged | 8 | 4 | `projectilePrefab` = tocha nova (Seção 0, item 2) |
| Goblin Sapper | 1 | `GoblinSapperController` (Seção 3) | 8 | 6 (contato/passiva) | `hasAttackAnimation = false`; ver Seções 3-4 |
| Centaur | 2 | Melee | 35 | 7 | "velocidade superior" — `moveSpeed` acima do padrão |
| Minotaur | 2 | Melee | 40 | 10 | — |
| Gnoll | 2 | Melee | 35 | 7 | — |
| Spider | 2 | Melee | 30 | 6 | "velocidade superior" — `moveSpeed` acima do padrão |
| Ancient Troll | 2 | Melee | 50 | 12 | maior HP/dano do Batch — confirmar `observationRadius`/`attackRadius` um pouco maiores em playtest, condizente com ser o "boss-like" comum do Floor 2 |

Passos por monstro (repetir 10x, checklist):
1. Rodar `MonsterAnimationGeneratorWindow` sobre o sprite sheet do asset de origem (já citado na ficha de cada um) → gera clipes direcionais + `AnimatorOverrideController` sobre `Base_Melee`/`Base_Ranged`.
2. Criar o prefab: `EnemyController` correto (`MeleeEnemyController`/`RangedEnemyController`/classe própria) + `EnemyStats` preenchido pela tabela acima + `Collider2D` (Melee: os 4 hitboxes direcionais também, como filhos).
3. Slime Green/Blue e Goblin Sapper também recebem o componente `EnemyContactDamage` (Seção 1) no mesmo GameObject.
4. Adicionar o prefab pronto no `monsterPrefabs[]` do `FloorPopulationManager` do Floor correto (Seção 6).

---

## 6. Montar na Scene — Floor 1A e Floor 2A

Usando o sistema de Floor já existente (`FloorDefinition`/`FloorManager`/`FloorRegistry`/`FloorPopulationManager`), sem nenhuma mudança de código nesses arquivos:

1. **Layout físico** (tilemap, paredes, decoração) — trabalho de nível, feito por você diretamente na Scene; fora do escopo desta sprint em termos de "código".
2. Criar 2 GameObjects `FloorDefinition` novos: `Floor_1A` (`originalFloorIdentity = 1`, `activeFloorPosition` = o próximo disponível na sequência atual) e `Floor_2A` (`originalFloorIdentity = 2`, próxima posição). Registrar os dois em `FloorRegistry.Instance.Floors`.
3. Em cada Floor, adicionar um `FloorPopulationManager`:
   - `ownerFloor` = o `FloorDefinition` correspondente.
   - `monsterPrefabs[]` do Floor 1A = Rat, Wolf, Bat, Slime Green, Slime Blue, Goblin, Goblin Raider, Goblin Sapper (8 prefabs).
   - `monsterPrefabs[]` do Floor 2A = Rat People, Centaur, Minotaur, Gnoll, Spider, Ancient Troll (6 prefabs).
   - `spawnPoints[]` — distribuir pelo layout de cada Floor (qualquer quantidade, o manager sorteia).
   - `config` — deixar no default (3/8/12, Seção 0 item 7) pra ambos.
4. Confirmar a rota de escada (`StairRouting`/`FloorRegistry.GetNextFloor`/`GetPreviousFloor`) entre Térreo → Floor 1A → Floor 2A funcionando (`Interactable`/`InteractionManager`, já existentes desde a Sprint 6) — nenhuma mudança de código esperada aqui, só a posição física da escada em cada layout.

---

## 7. Testes manuais (checklist)

- [ ] Os 7 reskins Melee (Wolf, Bat, Centaur, Minotaur, Gnoll, Spider, Ancient Troll) perseguem, atacam via Animation Event, flanqueiam quando a vaga do `MeleeAttackSlotManager` está cheia, e morrem normalmente — mesmo comportamento já validado no Rat/Goblin, só conferindo que os stats/Animator novos não quebraram nada.
- [ ] Goblin Raider ataca à distância, mantém `attackRadius`, foge se o player chegar perto demais, e a tocha explode em área ao contato ou na distância máxima (T08 do item 2 da Seção 0 — se a tocha aplicar dano direto sem explodir em área, o `EnemyProjectile` não tem a explosão embutida como eu assumi, e essa parte específica precisa de ajuste).
- [ ] Slime Green/Blue perseguem direto, nunca flanqueiam, aplicam dano de contato com cooldown (não a cada frame de overlap), sem nenhuma animação de ataque.
- [ ] Goblin Sapper: persegue armado (`Bomb_Walk`) → planta bomba no contato → foge (`Walk`) por `reloadDuration` → volta a perseguir; dano de contato passivo continua acontecendo o tempo todo, armado ou fugindo; a bomba dá tempo real de fuga pro jogador (andar pra longe antes do Animation Event de explosão evita o dano); morrer com o Sapper causa a auto-explosão em área centrada nele.
- [ ] Floor 1A e Floor 2A: população respeita `minimum`/`target`/`maximum`; monstros do Floor 2A não aparecem/atacam enquanto o jogador está no Floor 1A (Floor Sleep, Sprint 15); escada sobe/desce corretamente entre Térreo/1A/2A.
- [ ] Sem testes automatizados novos — mesmo critério das sprints de Bestiário anteriores (tudo aqui é runtime/Scene-dependente: Animator, física, timing de Animation Event).

---

## 8. Commits sugeridos

```
git commit -m "feat(bestiary): EnemyContactDamage compartilhado (Slime + Goblin Sapper)"
git commit -m "feat(bestiary): SlimeEnemyController (Slime Green/Blue)"
git commit -m "feat(bestiary): GoblinSapperController + GoblinSapperBomb"
git commit -m "feat(bestiary): 7 reskins Melee (Wolf, Bat, Centaur, Minotaur, Gnoll, Spider, Ancient Troll)"
git commit -m "feat(bestiary): Goblin Raider (RangedEnemyController + tocha)"
git commit -m "feat(floors): Floor 1A e 2A montados com FloorPopulationManager"
```

---

## Fechamento

- **Bestiary (`bestiary.md`):** marcar as 10 fichas novas como implementadas (mesmo ✅ já usado nas outras); se o item 2 da Seção 0 (explosão do `EnemyProjectile`) se confirmar errado no teste, documentar a correção real na própria ficha do Goblin Raider.
- **GDD:** nenhuma mudança de arquitetura esperada — Seção 22 já cobre tudo que esta sprint usa. Se o ciclo do Goblin Sapper divergir do que a própria ficha do Bestiário descreve, atualizar lá.
- **Plano de Produção:** quando você me mandar o relatório desta sprint, fecho a linha "20" e a Deadline 5 inteira (Sprints 17–20, +18b/19b) — próxima peça é a Deadline 6 (Sprint 21, Druid — Primário + Floor 3A–4A).

**Pronto quando:** Floor 1A e 2A jogáveis de ponta a ponta (escada, população, Floor Sleep) com as 13 fichas de monstro comum de Floors 1–2 funcionando em Play Mode, sem travar em nenhum estado.
