# Sprint 17 — Ranger — Primário

**Depende de:** Sprint 16.
**Objetivo:** Ranger jogável com ataque primário real — flechas retas na direção da mira (GDD Seção 17.2) que, ao aumentar a quantidade, formam um leque simétrico (2 flechas = 30° entre si, 3 = 15°...). Primeira sprint da Deadline 5, primeiro herói novo desde o Barbarian (Sprint 7-8).

> **Fora de escopo, de propósito:** Ultimate (facas persistentes) é a Sprint 18. Teto de 5 flechas com hook de carta é Sprint 18 também — aqui a quantidade é só um campo testável no Inspector/`ContextMenu`, sem cap nem integração com Card Framework (Sprint 35). Animator do Ranger não entra aqui — `HeroController` ainda não tem Animator wireado, mesmo estado do Barbarian.

---

## 0. Três coisas que o código real corrigiu em relação ao que eu tinha assumido

- **O método abstrato é `UseUltimate()`, não `Ultimate()`.** Barbarian implementa `UseUltimate()`.
- **`PrimaryAttack()` já é chamado só quando `GameplayGate.IsActive`** — isso acontece no `Awake()` da própria base (`controls.Gameplay.Attack.performed += ctx => { if (GameplayGate.IsActive) PrimaryAttack(); };`). Checar `GameplayGate.IsActive` de novo dentro do `PrimaryAttack()` do Ranger seria redundante — Barbarian não faz isso, o Ranger também não precisa.
- **Não existe cooldown de ataque na base, nem no Barbarian.** `Barbarian.PrimaryAttack()` executa a cada clique, sem nenhum gate de `attackSpeed` — ele é golpe único por clique, então isso nunca apareceu como necessidade. O Ranger precisa do próprio cooldown de verdade (GDD amarra "Velocidade de Ataque" ao ritmo de disparo, e sem isso o jogador atira flechas na velocidade que conseguir clicar) — isso é uma adição nova do Ranger, não a reutilização de algo que já existia.

Confirmado direto no `HeroStats.cs`: os campos são `damage` e `attackSpeed` (float, ataques por segundo — default `1f`), exatamente os nomes usados abaixo. Confirmado no `EnemyController.cs`: `TakeDamage(float amount)` é público, mesma assinatura que o Barbarian já chama.

## 1. `FanSpread` — lógica pura primeiro

Isola a regra central da sprint (o cálculo do leque) antes de tocar em `MonoBehaviour` — mesmo padrão já estabelecido (`AttackCooldown`, `PatrolAI`, `SlotPool`, Sprint 16).

`Assets/Scripts/Core/Combat/FanSpread.cs`:
```csharp
using UnityEngine;

namespace Core.Combat
{
    public static class FanSpread
    {
        /// Distribui `count` direções simetricamente ao redor de `baseDirection`, cobrindo `totalSpreadDegrees`
        /// no total entre a primeira e a última. 1 direção = baseDirection sem alteração nenhuma.
        /// GDD Seção 17.2: 2 flechas = 30° entre si, 3 = 15° entre si (mesmo total de 30° de ponta a ponta).
        public static Vector2[] GetDirections(Vector2 baseDirection, int count, float totalSpreadDegrees)
        {
            if (count <= 1) return new[] { baseDirection };

            var directions = new Vector2[count];
            float step = totalSpreadDegrees / (count - 1);
            float startAngle = -totalSpreadDegrees / 2f;

            for (int i = 0; i < count; i++)
                directions[i] = Rotate(baseDirection, startAngle + step * i);

            return directions;
        }

        private static Vector2 Rotate(Vector2 v, float degrees)
        {
            float rad = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad), sin = Mathf.Sin(rad);
            return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
        }
    }
}
```

`Assets/Tests/EditMode/FanSpreadTests.cs`:
```csharp
using NUnit.Framework;
using UnityEngine;
using Core.Combat;

public class FanSpreadTests
{
    [Test]
    public void SingleArrow_ReturnsBaseDirectionUnchanged()
    {
        var dirs = FanSpread.GetDirections(Vector2.up, 1, 30f);
        Assert.AreEqual(1, dirs.Length);
        Assert.AreEqual(Vector2.up, dirs[0]);
    }

    [Test]
    public void TwoArrows_Are30DegreesApart()
    {
        var dirs = FanSpread.GetDirections(Vector2.up, 2, 30f);
        Assert.AreEqual(30f, Vector2.Angle(dirs[0], dirs[1]), 0.01f);
    }

    [Test]
    public void ThreeArrows_MiddleOneMatchesBaseDirection()
    {
        var dirs = FanSpread.GetDirections(Vector2.up, 3, 30f);
        Assert.AreEqual(Vector2.up.normalized, dirs[1], "a flecha do meio deve ser igual à direção da mira, sem rotação");
    }

    [Test]
    public void ThreeArrows_AdjacentPairsAre15DegreesApart()
    {
        var dirs = FanSpread.GetDirections(Vector2.up, 3, 30f);
        Assert.AreEqual(15f, Vector2.Angle(dirs[0], dirs[1]), 0.01f);
        Assert.AreEqual(15f, Vector2.Angle(dirs[1], dirs[2]), 0.01f);
    }

    [Test]
    public void FiveArrows_TotalSpreadStays30DegreesEndToEnd()
    {
        var dirs = FanSpread.GetDirections(Vector2.up, 5, 30f);
        Assert.AreEqual(30f, Vector2.Angle(dirs[0], dirs[4]), 0.01f);
    }
}
```

## 2. `RangerArrow` — o projétil

`Assets/Scripts/Player/RangerArrow.cs`:
```csharp
using UnityEngine;

public class RangerArrow : MonoBehaviour
{
    [SerializeField] private float speed = 10f;
    [SerializeField] private float lifetime = 2f; // 🔢 alcance efetivo da flecha — placeholder de balanceamento

    private Vector2 direction;
    private float damage;
    private float timer;

    public void Launch(Vector2 dir, float dmg)
    {
        direction = dir.normalized;
        damage = dmg;
    }

    private void Update()
    {
        if (!GameplayGate.IsActive) return;

        transform.Translate(direction * speed * Time.deltaTime);
        timer += Time.deltaTime;
        if (timer >= lifetime) Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Enemy")) return;

        var enemy = other.GetComponent<EnemyController>();
        if (enemy != null) enemy.TakeDamage(damage); // assinatura confirmada em EnemyController.cs

        Destroy(gameObject); // sem perfuração — 🔢 GDD Seção 13 permite as duas variantes pra Straight Projectile, não confirma qual o Ranger usa; assumindo sem perfuração até decisão explícita
    }
}
```

## 3. `Ranger` — o herói

`Assets/Scripts/Player/Heroes/Ranger.cs`:
```csharp
using UnityEngine;
using Core.Combat;

public class Ranger : HeroController
{
    [SerializeField] private GameObject arrowPrefab;
    [SerializeField] private int arrowCount = 1; // 🔢 teto de 5 + hook de carta chegam na Sprint 18
    [SerializeField] private float totalSpreadDegrees = 30f; // GDD Seção 17.2

    private AttackCooldown attackCooldown;

    protected override void Awake()
    {
        base.Awake(); // essencial — é onde PlayerControls/Input System são montados
        attackCooldown = new AttackCooldown(1f / stats.attackSpeed);
    }

    protected override void Update()
    {
        base.Update(); // movimento (transform.Translate por moveInput) continua vindo da base
        attackCooldown.Tick(Time.deltaTime);
    }

    protected override void PrimaryAttack()
    {
        // Sem checar GameplayGate aqui — a base já só chama PrimaryAttack() quando
        // GameplayGate.IsActive (ver o wiring de controls.Gameplay.Attack no HeroController).
        if (!attackCooldown.TryConsume()) return;

        Vector2[] directions = FanSpread.GetDirections(AimDirection, arrowCount, totalSpreadDegrees);
        foreach (var dir in directions)
        {
            var arrowObj = Instantiate(arrowPrefab, transform.position, Quaternion.identity);
            arrowObj.GetComponent<RangerArrow>().Launch(dir, stats.damage);
        }
    }

    protected override void UseUltimate()
    {
        Debug.Log("[Ranger] Ultimate (facas persistentes) ainda não implementada — Sprint 18.");
    }

    // Sem Card Framework ainda (Sprint 35) — incremento manual só pra provar o leque nesta sprint.
    [ContextMenu("Debug: +1 Flecha")]
    private void DebugIncreaseArrowCount()
    {
        arrowCount++;
        Debug.Log($"[Ranger] arrowCount = {arrowCount}");
    }
}
```

## 4. Montar na Scene

- Criar prefab **`Ranger`**: sprite placeholder, `Ranger.cs`, `HeroStats` com **Dano Base 1,6 / Vida Base 30** (GDD Seção 17.2) — `damage = 1.6`, `maxHealth = 30`, `health = 30`; `attackSpeed` 🔢 placeholder (ex.: `2` — dois tiros por segundo, ajustável).
- Criar prefab **`RangerArrow_Placeholder`**: sprite pequeno, `CircleCollider2D` marcado **Is Trigger**, `RangerArrow.cs`. Não precisa de `Rigidbody2D` próprio — o Enemy já tem o seu (Dynamic, Sprint 14), o que basta pra Unity disparar `OnTriggerEnter2D`. Arrastar no campo `Arrow Prefab` do `Ranger`.
- Adicionar `Ranger` como opção selecionável no fluxo de Hero Select (Sprint 9) — mesmo registro que o Barbarian já usa.

## 5. Teste manual (Play Mode)

1. Selecionar Ranger no Hero Select → spawna em `Gameplay` normalmente (Death Flow, Floor System etc. já herdados de `HeroController`, sem mudança).
2. `arrowCount = 1` (default): LMB atira uma flecha reta na direção da mira, respeitando o cooldown (`attackSpeed`) — clicar rápido demais não dispara mais de uma vez antes do cooldown zerar (diferente do Barbarian, que atira a cada clique sem limite).
3. `arrowCount = 2` (via `ContextMenu`): confirma 2 flechas saindo simétricas, 30° entre si — nenhuma sai exatamente na direção da mira.
4. `arrowCount = 3`: 3 flechas — a do meio exatamente na direção da mira, 15° entre cada par adjacente.
5. Acertar um monstro (Rat/Goblin/Rat People, já reais desde a Sprint 16) → `EnemyController.TakeDamage()` real, monstro reage normalmente (flash ou `DamageTrigger`, dependendo se está em `Attacking` ou não — nada muda no lado do inimigo); matar sobe a Energia do Ranger via `GameEvents.EnemyKilled` (já herdado da base, nenhum código novo de Energia necessário).
6. Deixar uma flecha voar sem acertar nada → some sozinha ao fim do `lifetime`.
7. Pausar (TAB) com uma flecha em voo → ela para no lugar; despausar retoma o movimento normalmente.
8. Rodar os 5 testes de `FanSpreadTests` no Test Runner — todos verdes, junto com a suíte anterior (39 da Sprint 16).

## 6. Git

```
git add .
git commit -m "feat: fan spread utility (pure logic) + editmode tests"
```
```
git add .
git commit -m "feat: ranger arrow projectile"
```
```
git add .
git commit -m "feat: ranger hero — primary attack fires arrows in a fan based on aim direction"
git push
```

## 7. Fechamento

`docs/sprints/sprint-17.md` (relatório real, depois de implementado) + linha no `docs/sprints/index.md` + linha no `docs/changelog.md`.

---

**Pronto quando:** Ranger dispara flechas retas na direção da mira, respeitando cooldown próprio (`attackSpeed`); com `arrowCount` ≥ 2 elas formam um leque simétrico nos ângulos exatos do GDD (30° entre 2, 15° entre 3); flechas causam dano real via `EnemyController.TakeDamage()`, contam pra Energia da Ultimate herdada da base; nenhuma flecha se move durante a pausa.
