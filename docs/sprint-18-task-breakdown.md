# Sprint 18 — Ranger — Ultimate

**Depende de:** Sprint 17.
**Objetivo:** Ranger jogável por completo. Ultimate = giro lançando 8 facas nas 8 direções fixas (uma por Animation Event); cada faca é um projétil perfurante (2× dano do Ranger) em voo e, ao esgotar a reserva ou alcançar a distância máxima, **vira Persistent Area no chão** por 30s causando dano contínuo (1× dano) a quem passar por cima — exceção explícita à regra geral de projétil (GDD Seção 13, Seção 17.2).

> **Confirmado direto no GDD atualizado, não suposição:** a Ultimate **não** passa pelo sistema de Efeitos Nocivos (Seção 33) — esse sistema é "exclusivo de monstro sobre herói" (linha 966 do documento), então a faca no chão não precisa se anunciar como Efeito Nocivo nem competir por prioridade visual com nada disso. É só Persistent Area (Seção 13), mecanismo próprio.

---

## 0. Cinco decisões que o GDD não fecha no nível de código — registradas, não escondidas

1. **"Dano contínuo" virou dano em tick, não por frame.** O GDD não especifica cadência — modelei como um intervalo configurável (`groundedTickInterval`, 🔢), reaproveitando `AttackCooldown` (já existe, Sprint 17). Dano por frame seria absurdamente mais forte que qualquer outra fonte do jogo.
2. **`RangerKnife` não herda de `HeroProjectile`.** O comportamento diverge demais (fase extra, dano em tick em vez de hit único por alvo) pra justificar mexer numa classe que o Barbarian já usa em produção. Fica como classe própria — se um 3º herói precisar do mesmo padrão de "perfura, depois vira área", aí sim vale extrair uma base comum (regra dos 3).
3. **Os 8 Animation Events chamam o mesmo método com um parâmetro `int` (0-7), não 8 métodos diferentes.** O GDD diz "8 eventos distintos... cada um chamando a função que lança a faca daquela direção" — interpretei como 1 método parametrizado (jeito nativo do Animation Event no Unity), não 8 assinaturas. Ajustável se a intenção era realmente 8 métodos.
4. **`DirectionUtility` ganha um método novo: `DirectionFromIndex(int)`.** Preciso do caminho inverso de `GetDirectionIndex` — dado o índice do Animation Event, devolver o vetor de direção. Adição pura (não toca nada que já existe), mas é arquivo compartilhado, por isso registrado aqui.
5. **`groundedRadius` é maior que o collider fino de voo, valor novo sem contrapartida no GDD.** Faz sentido uma área-de-chão ser mais generosa que uma lâmina voando, mas é 🔢 chute, ajustável.

---

## 1. `RangerKnife.cs` — o projétil de duas fases

`Assets/Scripts/Player/RangerKnife.cs`:
```csharp
using UnityEngine;
using System.Collections.Generic;
using Core.Combat;

// Faca da Ultimate do Ranger (GDD Seção 17.2/13) — exceção à regra geral de projétil: em
// vez de sumir ao esgotar a reserva ou alcançar a distância máxima, fica no chão como
// Persistent Area, continuando a causar dano por um tempo.
public class RangerKnife : MonoBehaviour
{
    private enum Phase { Flying, Grounded }
    private Phase phase = Phase.Flying;

    [Header("Voo — 2x o dano do Ranger (GDD Seção 17.2)")]
    [SerializeField] private float flightSpeed = 12f;
    [SerializeField] private float maxDistance = 6f; // 🔢 alcance em voo
    [SerializeField] private float flightKnockbackForce = 4f; // 🔢

    [Header("No chão — Persistent Area, 1x o dano do Ranger (GDD Seção 17.2)")]
    [SerializeField] private float groundedDuration = 30f; // GDD: 30s
    [SerializeField] private float groundedTickInterval = 0.5f; // 🔢 cadência do "dano contínuo" — GDD não especifica
    [SerializeField] private float groundedRadius = 0.6f; // 🔢 raio da área depois de pousar, maior que o collider de voo
    [SerializeField] private float groundedKnockbackForce = 2f; // 🔢

    private Animator animator;
    private CircleCollider2D circleCollider;
    private Vector2 direction;
    private float flightDamageReserve;
    private float groundedDamagePerTick;
    private float distanceTraveled;
    private float groundedElapsed;
    private AttackCooldown groundedTick;
    private readonly HashSet<EnemyController> enemiesInRange = new();

    private void Awake()
    {
        animator = GetComponent<Animator>();
        circleCollider = GetComponent<CircleCollider2D>();
    }

    public void Launch(Vector2 dir, float flightDamage, float groundedDamage)
    {
        direction = dir.normalized;
        flightDamageReserve = flightDamage;
        groundedDamagePerTick = groundedDamage;
        if (animator != null) animator.Play(DirectionUtility.GetDirectionName(direction));
    }

    private void Update()
    {
        if (!GameplayGate.IsActive) return;

        if (phase == Phase.Flying) UpdateFlying();
        else UpdateGrounded();
    }

    private void UpdateFlying()
    {
        float step = flightSpeed * Time.deltaTime;
        transform.Translate(direction * step);
        distanceTraveled += step;
        if (distanceTraveled >= maxDistance) BecomeGrounded();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Enemy")) return;
        var enemy = other.GetComponent<EnemyController>();
        if (enemy == null) return;

        if (phase == Phase.Flying)
        {
            float damageDealt = Mathf.Min(flightDamageReserve, enemy.stats.health);
            enemy.TakeDamage(damageDealt);
            enemy.ApplyKnockback(direction, flightKnockbackForce);

            flightDamageReserve -= damageDealt;
            if (flightDamageReserve <= 0f) BecomeGrounded();
        }
        else
        {
            // Grounded: só entra na lista de quem toma dano no próximo tick — não aplica na
            // hora, pra não dar 2 hits (entrada + tick) no mesmo instante.
            enemiesInRange.Add(enemy);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (phase != Phase.Grounded) return;
        if (!other.CompareTag("Enemy")) return;
        var enemy = other.GetComponent<EnemyController>();
        if (enemy != null) enemiesInRange.Remove(enemy);
    }

    private void BecomeGrounded()
    {
        phase = Phase.Grounded;
        groundedElapsed = 0f;
        groundedTick = new AttackCooldown(groundedTickInterval);
        if (circleCollider != null) circleCollider.radius = groundedRadius;

        // Pega de graça quem já estava exatamente em cima do ponto de pouso — mudar o raio
        // do collider não reemite OnTriggerEnter2D pra quem já estava sobreposto.
        var hits = Physics2D.OverlapCircleAll(transform.position, groundedRadius);
        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Enemy")) continue;
            var enemy = hit.GetComponent<EnemyController>();
            if (enemy != null) enemiesInRange.Add(enemy);
        }
    }

    private void UpdateGrounded()
    {
        groundedElapsed += Time.deltaTime;
        if (groundedElapsed >= groundedDuration)
        {
            Destroy(gameObject);
            return;
        }

        groundedTick.Tick(Time.deltaTime);
        if (!groundedTick.TryConsume()) return;

        enemiesInRange.RemoveWhere(e => e == null); // limpa quem morreu desde o último tick

        foreach (var enemy in enemiesInRange)
        {
            enemy.TakeDamage(groundedDamagePerTick);
            Vector2 away = ((Vector2)enemy.transform.position - (Vector2)transform.position).normalized;
            enemy.ApplyKnockback(away, groundedKnockbackForce);
        }
    }
}
```

## 2. `DirectionUtility.cs` — um método novo, aditivo

Adicionar em `Assets/Scripts/Core/DirectionUtility.cs` (não mexe em nada que já existe):
```csharp
// Inverso de GetDirectionIndex — dado o índice (0-7, mesma ordem de DirectionNames: E, NE,
// N, NW, W, SW, S, SE), devolve o vetor unitário. Usado quando a direção nasce de um índice
// (ex.: parâmetro int de Animation Event), não de mira real — ver Ultimate do Ranger.
public static Vector2 DirectionFromIndex(int index)
{
    float angle = index * 45f * Mathf.Deg2Rad;
    return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
}
```

## 3. `Ranger.cs` — a Ultimate de verdade

Substituir o stub `UseUltimate()` e adicionar os dois Animation Events novos:
```csharp
[SerializeField] private GameObject knifePrefab; // novo campo

protected override void UseUltimate()
{
    if (isAttacking) return; // guarda própria — HeroController não bloqueia Ultimate por isAttacking, então o Ranger se protege
    isAttacking = true;
    actionElapsed = 0f;
    AnimatorTrigger("UltimateTrigger");
}

// Animation Event, 8 vezes na mesma animação (GDD Seção 17.2) — directionIndex configurado
// no próprio clipe, um valor por chamada (0-7, mesma ordem de DirectionUtility).
public void AnimationThrowKnifeEvent(int directionIndex)
{
    Vector2 dir = DirectionUtility.DirectionFromIndex(directionIndex);
    var knifeObj = Instantiate(knifePrefab, transform.position, Quaternion.identity);
    knifeObj.GetComponent<RangerKnife>().Launch(dir, stats.damage * 2f, stats.damage * 1f);
}

// Animation Event, no fim do clipe da Ultimate.
public void AnimationUltimateEndEvent()
{
    isAttacking = false;
}
```

**Nota sobre `maxActionDuration`:** hoje é `3f`, calibrado pro tiro único do primário. O clipe da Ultimate (giro + 8 eventos espaçados) provavelmente precisa de mais tempo — ajustar esse valor (ou separar um campo próprio pra Ultimate) depois de montar o Animator de verdade e medir a duração real do clipe.

## 4. Montar na Scene

- Criar prefab **`RangerKnife_Placeholder`**: sprite pequeno, `CircleCollider2D` marcado **Is Trigger** (raio pequeno — o `groundedRadius` aumenta ele em código ao pousar, não precisa configurar dois colliders), `RangerKnife.cs`. Animator opcional nesta fase (8 estados soltos, mesmo padrão do `HeroProjectile` — `Animator.Play()` direto por nome, sem Blend Tree).
- Arrastar no campo `Knife Prefab` do `Ranger`.
- No Animator Controller do Ranger: novo estado `Ultimate` (clipe de giro), parâmetro `UltimateTrigger`, e **8 Animation Events** espaçados ao longo do clipe chamando `AnimationThrowKnifeEvent` com `directionIndex` de 0 a 7 (um por evento) + `AnimationUltimateEndEvent` no último frame.

## 5. Teste manual (Play Mode)

1. Acumular Energia (matar monstros) até a Ultimate ficar pronta, apertar RMB → giro toca, 8 facas saem ao longo da animação (uma por Animation Event, não todas de uma vez).
2. Confirmar as 8 direções fixas (N/NE/E/SE/S/SW/W/NW), não a mira.
3. Uma faca em voo acerta um monstro → 2× dano do Ranger, perfura se sobrar reserva (mesma mecânica do `HeroProjectile`/`RangerArrow`), aplica knockback na direção de voo.
4. Uma faca esgota a reserva ou alcança `maxDistance` sem morrer tudo no caminho → vira Persistent Area: para de se mover, collider cresce pro `groundedRadius`.
5. Um monstro já em cima do ponto de pouso no instante da transição toma dano no primeiro tick (confirma o `OverlapCircleAll` de seed em `BecomeGrounded()`).
6. Um monstro que entra na área depois do pouso começa a tomar dano nos ticks seguintes; um que sai para de tomar dano (`enemiesInRange` atualizando via Enter/Exit).
7. Reduzir `groundedDuration` pra uns 3-5s só durante o teste → confirmar que a faca desaparece sozinha no fim, sem tick nenhum depois disso.
8. Pausar (TAB) com uma faca em voo e outra já no chão → nenhuma das duas fases avança (nem posição, nem tick de dano, nem o timer dos 30s).
9. Tentar disparar o primário ou a Ultimate de novo no meio do giro → bloqueado por `isAttacking`, mesma trava já usada no primário.

## 6. Git

```
git add .
git commit -m "feat: DirectionUtility.DirectionFromIndex (inverse of GetDirectionIndex)"
```
```
git add .
git commit -m "feat: ranger knife — two-phase projectile (flying pierce + grounded persistent area)"
```
```
git add .
git commit -m "feat: ranger ultimate — 8 knives via animation events, persistent area on landing"
git push
```

## 7. Fechamento

`docs/sprints/sprint-18.md` (relatório real, depois de implementado) + linha no `docs/sprints/index.md` + linha no `docs/changelog.md`.

Registrar no relatório real qualquer divergência entre as 5 decisões da Seção 0 e o que de fato foi construído — mesmo espírito de todas as sprints anteriores.

---

**Pronto quando:** Ultimate lança 8 facas nas 8 direções fixas via Animation Events; cada faca causa 2× dano perfurante em voo com knockback; ao esgotar a reserva ou alcançar a distância máxima, vira Persistent Area por 30s causando dano em tick (1×) a quem estiver na área — incluindo quem já estava lá no instante do pouso; nada avança durante a pausa; `isAttacking` impede sobrepor outra ação. Ranger está completo (primário + ultimate).
