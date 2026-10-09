# Sprint 28 — Gunslinger (GDD Seção 17.8)

> Prospectiva, feita com `HeroController.cs` atual (pós-27b), `Paladin.cs` atual (shield bash —
> base literal do chicote) e `EnemyController.cs` atual (ponto exato do drop de loot) em mãos.
> Nenhum herói existente usa Hitscan ainda — é técnica nova neste projeto. O leque simétrico
> (hammerCount) e o leque de 8 Animation Events nomeados (knives do Ranger) já têm precedente
> direto e foram reaproveitados onde davam.

## Seção 0 — Decisões e suposições (revisar antes de implementar)

0. **Fórmula do cone de imprecisão da rajada — a própria GDD deixa isso como placeholder.**
   Proponho: `halfAngle = (shotCount - 1) * coneDegreesPerExtraShot` (🔢 `coneDegreesPerExtraShot
   = 6°`, ajustável). Com 1 tiro (Arma Básica), desvio é sempre 0° — perfeito. Cada tiro da
   rajada sorteia seu próprio desvio dentro de `[-halfAngle, +halfAngle]`, independente dos
   outros tiros da mesma rajada (não é um leque simétrico fixo como o do martelo do Paladin —
   é aleatório por tiro, conforme o texto da GDD pede).

1. **Duração variável do clipe de Attack conforme `shotCount` (1 a 6) é trabalho de Animator,
   não de C#.** Assumo que existem estados/clipes diferentes por quantidade de tiros (ex.:
   `Shot_1`...`Shot_6`, ou um Blend Tree 1D indexado por um int `ShotCount` que eu exponho no
   Animator) — cada um com o Animation Event `AnimationShotFireEvent()` embutido uma vez por
   repetição de 2 frames (disparo + recuo), exatamente `shotCount` vezes. Isso precisa ser
   montado no Animator Controller; o código só garante que cada chamada do evento dispara
   exatamente 1 tiro.

2. **Orthogonal vs Diagonal (`Shot_Orthogonal`/`Shot_Diagonal`) escolhido por um bool novo no
   Animator, `IsOrthogonalAim`**, calculado a partir de `AimDirection` (um dos eixos exatamente
   0 → ortogonal). Setado uma vez no início do `PrimaryAttack()`, igual à mira já congelada.

3. **Guarda contra disparo duplo por Blend Tree — resolvido de um jeito novo, não copiado 1:1
   de outro herói.** O projeto já tem o bug conhecido de Blend Tree 2D Freeform disparando o
   Animation Event de todo clipe com peso > 0 no mesmo frame (Barbarian, EnemyController). Mas
   ali a solução é um bool "já disparei nessa ação" — não serve aqui, porque preciso que o MESMO
   evento dispare várias vezes (uma por tiro da rajada), só não duas vezes no mesmo frame. Usei
   `Time.frameCount` como guarda (dispara no máximo 1x por frame, mas pode disparar de novo no
   frame seguinte): ver `AnimationShotFireEvent()`. **Peço validação específica disso em teste**
   — é a parte mais nova/arriscada desta sprint.

4. **Raycast único, sem perfuração** — "no monstro atingido ou no fim da linha" (GDD) indica que
   o tiro para no primeiro monstro, não atravessa. `Physics2D.Raycast` simples resolve isso sem
   precisar do dedup de `CollectDistinct` (que é só para resultados de área/Overlap).

5. **Origem do raycast = `transform.position`**, sem um ponto de disparo dedicado (ex.: ponta da
   arma). Ajustável depois com um Transform filho, se necessário visualmente.

6. **Hitscan (primário e ultimate) não aplica knockback** — a GDD não menciona empurrão em
   nenhum dos dois. Só o chicote (secundária) tem knockback, por reaproveitar o mecanismo do
   shield bash do Paladin, que já inclui knockback.

7. **NÃO herdei `IsDamageImmune` do shield bash do Paladin para o chicote do Gunslinger.** A GDD
   do Paladin pede imunidade durante a animação inteira; a do Gunslinger só diz "mesmo mecanismo"
   (os 4 triggers cardeais simultâneos + dano + knockback), sem mencionar imunidade. Tratei como
   dois mecanismos iguais, mas sem herdar um efeito colateral que não foi pedido para este herói.

8. **`shotCount` é um campo de upgrade comum (como `hammerCount`/`bladeCount`/`arrowCount`),
   não uma referência a um sistema de "Tier de Arma" externo.** A GDD usa o Tier só para
   justificar o balanceamento (1 a 6); no código, é só um int serializado com `OnValidate`
   limitando a 1-6, upgradable pelo mesmo sistema externo que já upgrada os outros heróis.

9. **8 Animation Events nomeados na Ultimate** (`AnimationShoot_N/NE/E/SE/S/SW/W/NW`), mesma
   convenção explícita já usada nas facas do Ranger — cada um disparando 1 tiro Hitscan na
   direção fixa correspondente (não a mira do jogador, é um giro completo).

10. **Multiplicador de dano da Ultimate** — a GDD não dá um número explícito para o Gunslinger
    (diferente do Barbarian "2x" ou Paladin "2x"). Adicionei `ultimateDamageMultiplier = 1f`
    (🔢 ajustável) só por consistência estrutural com os outros heróis, default neutro.

11. **Passiva (loot 2x) precisa de uma mudança pequena FORA do `Gunslinger.cs`.** Encontrei o
    ponto exato do drop em `EnemyController.AnimationDieEndEvent()`:
    ```csharp
    drop.loot = new LootDefinition { itemName = "Monster Essence", quantity = monsterEssenceDropAmount };
    ```
    Não existe hoje nenhum hook para um herói influenciar essa quantidade. Proponho o mesmo
    padrão já usado por `HeroController.IsPlayerUntargetable` (campo `static`, lido direto pelo
    `EnemyController` sem precisar de referência cruzada — válido porque só existe 1 herói
    jogável por vez, mesmo raciocínio já documentado nesse campo):
    ```csharp
    // HeroController.cs — novo campo estático
    public static float LootMultiplier = 1f;
    ```
    ```csharp
    // EnemyController.cs — AnimationDieEndEvent(), só a linha da quantidade muda
    int quantity = Mathf.RoundToInt(monsterEssenceDropAmount * HeroController.LootMultiplier);
    drop.loot = new LootDefinition { itemName = "Monster Essence", quantity = quantity };
    ```
    O Gunslinger seta `HeroController.LootMultiplier = lootMultiplier` (2f) no próprio `Awake()`.
    **Essas 2 mudanças (um campo em `HeroController.cs`, uma linha em `EnemyController.cs`)
    precisam ser aplicadas por quem for implementar — não dá para fazer a passiva funcionar só
    editando `Gunslinger.cs`.**

---

## Código — `Gunslinger.cs`

```csharp
using UnityEngine;

public class Gunslinger : HeroController
{
    // ===================== Primário — rajada Hitscan (GDD Seção 17.8) =====================
    [Header("Ataque primário — rajada Hitscan (mira em 1 das 8 direções)")]
    [SerializeField] private LayerMask enemyLayerMask;
    [SerializeField] private float hitscanMaxRange = 10f; // 🔢 ajustável
    [SerializeField] private GameObject impactVfxPrefab; // Projectile_Impact — instanciado a cada tiro, não 1x por rajada (GDD)
    [SerializeField] private float impactVfxDuration = 0.5f; // 🔢 ajustável

    // Upgrade futuro — tiros por rajada, presos ao Tier de Arma na GDD (Básica=1, Iron=2,
    // Silver=3, Emerald=4, Gold=5, Diamond=6), mas aqui é só um campo upgradable comum, mesmo
    // critério de hammerCount/bladeCount/arrowCount (Seção 0, item 8).
    [SerializeField] private int shotCount = 1; // 🔢 upgradable — 1 a 6 (ver OnValidate)

    // Quanto mais tiros, mais impreciso — GDD deixa a fórmula como placeholder (Seção 0, item 0).
    [SerializeField] private float coneDegreesPerExtraShot = 6f; // 🔢 ajustável

    // Mesma rede de segurança do Barbarian/Paladin — Animation Event de fim nunca disparar não
    // pode travar o herói pra sempre em isAttacking.
    [SerializeField] private float maxActionDuration = 3f; // 🔢 ajustável
    private float actionElapsed;

    // Guarda contra o disparo duplo do Blend Tree 2D (2 clipes com peso > 0 no mesmo frame) —
    // diferente do bool "já disparei" de outros heróis, porque aqui o MESMO evento precisa
    // disparar várias vezes (1 por tiro da rajada), só nunca 2x no mesmo frame (Seção 0, item 3).
    private int lastShotFireFrame = -1;

    // ===================== Ultimate — giro disparando nas 8 direções =====================
    [Header("Ultimate — giro, 8 tiros Hitscan fixos")]
    [SerializeField] private float ultimateDamageMultiplier = 1f; // 🔢 GDD não especifica bônus — placeholder neutro (Seção 0, item 10)

    private static readonly Vector2[] EightDirections =
    {
        Vector2.up,
        new Vector2(0.7071f, 0.7071f),
        Vector2.right,
        new Vector2(0.7071f, -0.7071f),
        Vector2.down,
        new Vector2(-0.7071f, -0.7071f),
        Vector2.left,
        new Vector2(-0.7071f, 0.7071f),
    };

    // ===================== Secundária (Shift) — chicote =====================
    // Mesmo mecanismo do shield bash do Paladin (4 triggers cardeais simultâneos) — GDD Seção
    // 17.8 explícita. SEM a imunidade a dano que o Paladin tem (Seção 0, item 7).
    [Header("Habilidade Secundária (Shift) — chicote (4 triggers cardeais simultâneos)")]
    [SerializeField] private Collider2D whipHitboxN;
    [SerializeField] private Collider2D whipHitboxS;
    [SerializeField] private Collider2D whipHitboxE;
    [SerializeField] private Collider2D whipHitboxW;
    [SerializeField] private float whipDamageMultiplier = 1f; // 🔢 ajustável
    [SerializeField] private float whipKnockbackForce = 4f; // 🔢 ajustável
    [SerializeField] private float maxWhipDuration = 2f; // 🔢 rede de segurança
    private float whipElapsed;
    private bool whipHitFired;

    // ===================== Passiva — monstros dropam 2x mais loot =====================
    [Header("Passiva — loot em dobro (requer patch em HeroController.cs/EnemyController.cs — ver Seção 0, item 11)")]
    [SerializeField] private float lootMultiplier = 2f; // 🔢 GDD: "2x mais loot", ajustável

    private readonly System.Collections.Generic.List<EnemyController> hitTargets = new();
    private static readonly Collider2D[] OverlapBuffer = new Collider2D[16];

#if UNITY_EDITOR
    private void OnValidate()
    {
        shotCount = Mathf.Clamp(shotCount, 1, 6);
    }
#endif

    protected override void Awake()
    {
        base.Awake();
        // Passiva: sempre ativa, sem gatilho/duração — seta o multiplicador global de loot uma
        // vez e nunca mais toca nisso (só existe 1 herói jogável por vez, mesmo critério de
        // HeroController.IsPlayerUntargetable).
        LootMultiplier = lootMultiplier;
    }

    protected override void Update()
    {
        if (!GameplayGate.IsActive) return;
        base.Update();

        if (isAttacking)
        {
            actionElapsed += Time.deltaTime;
            if (actionElapsed >= maxActionDuration)
            {
                Debug.LogWarning("[Gunslinger] Animation Event de fim de ação (rajada ou ultimate) nunca chegou — forçando fim (verifique o Animator Controller).");
                isAttacking = false;
            }
        }

        if (isUsingSecondaryAbility)
        {
            whipElapsed += Time.deltaTime;
            if (whipElapsed >= maxWhipDuration)
            {
                Debug.LogWarning("[Gunslinger] Animation Event de fim do chicote nunca chegou — forçando fim (verifique o Animator Controller).");
                isUsingSecondaryAbility = false;
            }
        }
    }

    // ===================== Primário =====================

    protected override void PrimaryAttack()
    {
        isAttacking = true;
        actionElapsed = 0f;
        if (animator != null) animator.SetBool("IsOrthogonalAim", IsAimOrthogonal());
        AnimatorTrigger("AttackTrigger");
    }

    private bool IsAimOrthogonal() =>
        Mathf.Approximately(AimDirection.x, 0f) || Mathf.Approximately(AimDirection.y, 0f);

    // Animation Event, embutido 1x por repetição de 2 frames (disparo+recuo) dentro do clipe —
    // precisa disparar exatamente shotCount vezes por clipe correspondente (Seção 0, item 1).
    public void AnimationShotFireEvent()
    {
        // Guarda só contra o MESMO frame (Blend Tree com 2 clipes de peso > 0) — ver Seção 0,
        // item 3. Chamadas em frames diferentes (repetições seguintes da rajada) disparam normal.
        if (Time.frameCount == lastShotFireFrame) return;
        lastShotFireFrame = Time.frameCount;

        Vector2 dir = GetBurstShotDirection();
        FireHitscanShot(dir, stats.damage);
    }

    // Desvio angular aleatório, independente por tiro — Seção 0, item 0.
    private Vector2 GetBurstShotDirection()
    {
        float halfAngle = (shotCount - 1) * coneDegreesPerExtraShot;
        float deviation = Random.Range(-halfAngle, halfAngle);
        return RotateDegrees(AimDirection, deviation);
    }

    // Animation Event, no fim do clipe de ataque (seja qual for o Shot_N correspondente).
    public void AnimationAttackEndEvent()
    {
        isAttacking = false;
    }

    // ===================== Ultimate =====================

    protected override void UseUltimate()
    {
        isAttacking = true;
        actionElapsed = 0f;
        AnimatorTrigger("UltimateTrigger");
    }

    // 8 Animation Events distintos, um por frame-chave do giro — mesma convenção das facas do
    // Ranger (Seção 0, item 9). Direção FIXA no mundo, não a mira do jogador.
    public void AnimationShoot_N() => FireUltimateShot(EightDirections[0]);
    public void AnimationShoot_NE() => FireUltimateShot(EightDirections[1]);
    public void AnimationShoot_E() => FireUltimateShot(EightDirections[2]);
    public void AnimationShoot_SE() => FireUltimateShot(EightDirections[3]);
    public void AnimationShoot_S() => FireUltimateShot(EightDirections[4]);
    public void AnimationShoot_SW() => FireUltimateShot(EightDirections[5]);
    public void AnimationShoot_W() => FireUltimateShot(EightDirections[6]);
    public void AnimationShoot_NW() => FireUltimateShot(EightDirections[7]);

    private void FireUltimateShot(Vector2 direction)
    {
        FireHitscanShot(direction, stats.damage * ultimateDamageMultiplier);
    }

    // Animation Event, no fim do clipe de giro.
    public void AnimationUltimateEndEvent()
    {
        isAttacking = false;
    }

    // ===================== Hitscan compartilhado (primário + ultimate) =====================

    // Raycast único, sem perfuração — para no primeiro monstro ou no fim da linha (Seção 0,
    // item 4). Sem knockback em nenhum dos dois casos (Seção 0, item 6).
    private void FireHitscanShot(Vector2 direction, float damage)
    {
        Vector2 origin = transform.position;
        RaycastHit2D hit = Physics2D.Raycast(origin, direction, hitscanMaxRange, enemyLayerMask);

        Vector3 impactPoint;
        if (hit.collider != null)
        {
            var enemy = hit.collider.GetComponent<EnemyController>();
            if (enemy != null) enemy.TakeDamage(damage);
            impactPoint = hit.point;
        }
        else
        {
            impactPoint = origin + direction.normalized * hitscanMaxRange;
        }

        if (impactVfxPrefab != null)
        {
            var vfx = Instantiate(impactVfxPrefab, impactPoint, Quaternion.identity);
            Destroy(vfx, impactVfxDuration);
        }
    }

    private static Vector2 RotateDegrees(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }

    // ===================== Secundária (Shift) — chicote =====================

    protected override void UseSecondaryAbility()
    {
        whipElapsed = 0f;
        whipHitFired = false;
        AnimatorTrigger("SecondaryAbilityTrigger");
    }

    // Sem override de IsDamageImmune de propósito — Seção 0, item 7.

    // Animation Event único — os 4 triggers disparam juntos (mesmo critério do shield bash do
    // Paladin), não escolhido pela mira.
    public void AnimationWhipHitEvent()
    {
        if (whipHitFired) return;
        whipHitFired = true;

        float damage = stats.damage * whipDamageMultiplier;
        ApplyWhipHit(whipHitboxN, damage);
        ApplyWhipHit(whipHitboxS, damage);
        ApplyWhipHit(whipHitboxE, damage);
        ApplyWhipHit(whipHitboxW, damage);
    }

    private void ApplyWhipHit(Collider2D hitbox, float damage)
    {
        if (hitbox == null) return;

        int count = hitbox.Overlap(ContactFilter2D.noFilter, OverlapBuffer);
        EnemyController.CollectDistinct(OverlapBuffer, count, hitTargets);
        foreach (var enemy in hitTargets)
        {
            enemy.TakeDamage(damage);
            Vector2 direction = ((Vector2)enemy.transform.position - (Vector2)transform.position).normalized;
            enemy.ApplyKnockback(direction, whipKnockbackForce);
        }
    }

    // Animation Event, no fim do clipe de chicote.
    public void AnimationWhipEndEvent()
    {
        isUsingSecondaryAbility = false;
    }
}
```

---

## Patch necessário — `HeroController.cs` (fora do Gunslinger.cs)

```csharp
// Junto de IsPlayerUntargetable — mesmo critério (só existe 1 herói jogável por vez, então um
// campo estático simples é suficiente, sem precisar de referência cruzada Hero <-> Enemy).
// Sprint 28 (Gunslinger) — passiva: multiplica a quantidade de qualquer loot já dropado.
public static float LootMultiplier = 1f;
```

## Patch necessário — `EnemyController.cs` (dentro de `AnimationDieEndEvent()`)

```csharp
if (dropsLoot)
{
    var lootObj = new GameObject("Loot_MonsterEssence");
    lootObj.transform.position = transform.position;
    var drop = lootObj.AddComponent<LootDrop>();
    // Sprint 28 (Gunslinger) — passiva multiplica a quantidade, nunca a chance de drop em si.
    int quantity = Mathf.RoundToInt(monsterEssenceDropAmount * HeroController.LootMultiplier);
    drop.loot = new LootDefinition { itemName = "Monster Essence", quantity = quantity };
}
```

---

## Animator / prefab — notas de setup

- **Attack:** precisa de variação de duração conforme `shotCount` (Seção 0, item 1) — proponho
  6 estados (`Shot_1`...`Shot_6`) ou um Blend Tree 1D indexado por um int `ShotCount` (exposto
  via `animator.SetInteger` no `PrimaryAttack()`, se for esse o caminho escolhido — não incluí
  isso no código acima porque depende de como o Animator for montado; avisem se precisar).
  Dentro de cada estado, o Animation Event `AnimationShotFireEvent()` embutido 1x por repetição
  de 2 frames, exatamente `shotCount` vezes.
- Bool novo `IsOrthogonalAim` decide entre `Shot_Orthogonal`/`Shot_Diagonal` dentro de cada
  estado (sub-Blend-Tree ou 2 camadas).
- `AnimationAttackEndEvent()` no último frame de qualquer `Shot_N`.
- **Ultimate:** 1 clipe de giro só, com 8 Animation Events (`AnimationShoot_N`...`AnimationShoot_NW`)
  nos frames-chave correspondentes, e `AnimationUltimateEndEvent()` no fim.
- **Secundária:** 1 clipe de chicote, 1 `AnimationWhipHitEvent()` no frame de impacto (os 4
  hitboxes N/S/E/W já vêm prontos no prefab, mesmo setup do Paladin), `AnimationWhipEndEvent()`
  no fim.
- `impactVfxPrefab` ("Projectile_Impact") é instanciado a cada tiro — tanto na rajada quanto na
  ultimate — nunca 1 só por ação inteira.

---

## Checklist de teste

1. 1 tiro (Arma Básica) sempre acerta exatamente a direção da mira (desvio 0°).
2. Rajada de 6 tiros (Diamond) mostra desvio visível e crescente, sem travar em isAttacking.
3. `AnimationShotFireEvent()` nunca dispara 2x no mesmo frame nas diagonais (onde o Blend Tree
   mistura 2 clipes) — validar com Debug.Log contando chamadas por frame.
4. Rajada interrompida (Animation Event de fim nunca chega) força fim em `maxActionDuration`.
5. Hitscan não atinge nada quando não há monstro na linha — `impactVfxPrefab` aparece no fim do
   alcance máximo, não na posição do Gunslinger.
6. Hitscan para no primeiro monstro da linha — monstros atrás dele não recebem dano.
7. Ultimate dispara os 8 tiros nas 8 direções fixas, independente de onde a mira do jogador
   estava.
8. Ultimate não trava o Gunslinger além do fim real do clipe (timeout de segurança funciona).
9. Chicote acerta monstros em qualquer uma das 4 direções cardeais simultaneamente, com
   knockback.
10. Chicote NÃO torna o Gunslinger imune a dano durante a animação (diferente do shield bash do
    Paladin) — tomar um hit durante o chicote deve aplicar dano normal.
11. Chicote não cancelável (apertar Shift de novo durante não faz nada) — comportamento padrão
    sem override de `CancelSecondaryAbility()`.
12. Passiva: matar um monstro comum dropa 2x a quantidade configurada em
    `monsterEssenceDropAmount` (ex.: 1 → 2).
13. Passiva: monstro com `dropsLoot = false` (summons) continua sem dropar nada, mesmo com a
    passiva ativa — multiplicador nunca cria loot do zero, só dobra o que já ia dropar.
14. Trocar de Tier de Arma (upgrade externo) incrementando `shotCount` no Inspector reflete
    corretamente no leque de imprecisão e na contagem de tiros, sem passar de 6.
15. Verificar junto ao time de arte/Animator que os clipes `Shot_1`...`Shot_6` (ou o Blend Tree
    equivalente) realmente embutem o Animation Event o número certo de vezes — esse é o item
    mais arriscado de toda a sprint (Seção 0, item 1 e 3).
