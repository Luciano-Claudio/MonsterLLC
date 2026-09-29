# Sprint 21 — Druid (Primário + Ultimate + Shift) + Floor 3A–4A

> Depende de: Sprint 20 (fechada). Baseado em `HeroController.cs`, `EnemyController.cs`,
> `FloorPopulationManager.cs`, `FloorDefinition.cs`, `Stair.cs`, `Barbarian.cs`, `Ranger.cs`
> e `HeroStats.cs` reais (pós-Sprint 20/19b), + GDD Seção 17.4 (Druid).
>
> Escopo confirmado no plano de produção: **só Primário do Druid + Floor 3A/4A nesta sprint.**
> A GDD completa do Druid (Ultimate + Shift) foi lida por inteiro e a arquitetura abaixo cobre
> os 3 (Primário, Ultimate, Shift) porque são intimamente acoplados (Ultimate e Shift disputam
> os mesmos estados do herói) — mas o conteúdo de Bestiário pros Floors 3-4 fica pra próxima
> sprint, como já estava definido.

---

## Seção 0 — Decisões e suposições explícitas (leia antes de implementar)

Esta sprint é a primeira a exigir mudanças na classe-base `HeroController`. Todas são
pequenas, aditivas e seguem o mesmo precedente já usado pra `OnHeroDeath()` e
`CancelSecondaryAbility()` (hooks virtuais, no-op por padrão, só o Druid sobrescreve). Nenhuma
delas muda o comportamento de Barbarian/Ranger/Mage.

1. **`TakeDamage` vira `virtual`.** Hoje é `public void TakeDamage(float amount)`, não dá pra
   interceptar. O Alce precisa de uma janela de imunidade só durante as animações de
   transformar-em/transformar-de-volta (não durante o "during" — lá ele toma dano normal, só
   com HP máximo maior). Sem isso não tem como implementar a imunidade sem duplicar toda a
   lógica de `TakeDamage` dentro do Druid.
2. **Novo par `IsUltimateActive` / `CancelUltimate()`** em `HeroController`, espelhando
   exatamente o par que já existe pra Secundária (`CancelSecondaryAbility()`). Motivo: a GDD
   pede "cancelamento manual" da Ultimate (o texto usa "RMB" — mas nenhum herói até hoje tinha
   Ultimate com duração, então a Ultimate nunca precisou de um "cancelar" antes; a
   interpretação adotada aqui é a mesma UX que a Secundária já usa: apertar o botão de Ultimate
   de novo, enquanto ela estiver ativa, cancela — não é preciso saber qual tecla física é
   "RMB" no Input Actions, o mecanismo é neutro a isso). `TryUseUltimate()` passa a checar
   `IsUltimateActive` **antes** do gate de energia, senão a Energia zerada (que a própria
   transformação já deixou em 0) bloquearia o cancelamento.
3. **`protected Vector2 MoveInput => moveInput;`** — exposição read-only do vetor de
   movimento cru, mesmo padrão de `AimDirection`. Necessário porque a Coruja anda de verdade
   olhando pra 4 direções cardeais (N/E/S/W) baseadas em pra onde o jogador está andando, não
   na mira do mouse — e hoje nenhum herói expõe isso (GDD Seção 16: "não existe MoveX/MoveY pro
   herói", intencional até agora, mas a Coruja quebra essa regra de propósito).
4. **Suposição sobre o efeito da vinha:** a GDD não define o que a vinha faz ao acertar. Adotei
   **dano direto = `stats.damage`, um hit cheio por vinha** (não dividido entre as vinhas, ao
   contrário das flechas do Ranger) — é o comportamento mais simples e consistente com "cada
   vinha agarra e aperta um monstro". Teste isolado dedicado no checklist (T04). Se for outra
   coisa (DoT, imobilização), me avise que troco só essa parte.
5. **Multiplicadores do Alce (HP máximo/dano/velocidade) e duração da Coruja são todos
   `🔢` placeholders ajustáveis** — a GDD marca o multiplicador de HP como "TBD" explicitamente
   e não dá números pra dano/velocidade nem teto de duração da Coruja. Segue o mesmo padrão já
   usado pro `arrowCount` do Ranger (Sprint 17/18): serializados, testáveis, sem travar a
   sprint esperando um número final de balanceamento.
6. **Alce e Coruja são mutuamente exclusivos** — nenhuma das duas checa isso na GDD
   explicitamente, mas as duas alteram o estado do herói de formas incompatíveis (Alce troca o
   Animator Controller inteiro; Coruja despacha `IsPlayerUntargetable`). Cada uma agora recusa
   ativar se a outra já estiver rodando.
7. **Vinha não precisa de nenhum marcador "já tem vinha"** — "nunca repete um alvo na mesma
   ativação" já é satisfeito de graça: a busca pega os N monstros *distintos* mais próximos
   numa lista (cada `EnemyController` só entra uma vez), não existe repetição possível dentro
   de uma única lista sem duplicatas.
8. **Setup do Animator da Coruja precisa de um estado literalmente chamado "Walk"** dentro da
   sub-state-machine da fase "during" (pode ficar aninhado, ex.:
   `Base Layer/Druid_Owl/During/Walk`) — `Animator.IsName()` (o que `AnimatorStateCheck`
   encapsula) casa pelo nome curto quando a string não tem ponto, então um estado "Walk"
   aninhado em qualquer lugar do mesmo layer já satisfaz o gate de movimento que o
   `HeroController.Update()` já usa pra todo mundo, sem precisar duplicar lógica nenhuma. Isso
   é o que faz "movimento continua livre" funcionar de graça. Detalhado na Seção 5.

---

## Seção 1 — `HeroController.cs` (mudanças mínimas na base)

```csharp
// 1) TakeDamage vira virtual
public virtual void TakeDamage(float amount)
{
    // ... corpo inalterado ...
}

// 2) Novo par simétrico a CancelSecondaryAbility — perto dele, linha ~494
protected virtual bool IsUltimateActive => false;

protected virtual void CancelUltimate() { }

// 3) TryUseUltimate ganha 1 linha no topo
private void TryUseUltimate()
{
    if (IsUltimateActive) { CancelUltimate(); return; }
    if (!EnergySystem.IsReady(stats.energy, stats.maxEnergy)) return;
    UpdateAimDirection(controls.Gameplay.Look.ReadValue<Vector2>());
    UseUltimate();
    stats.energy = 0f;
    GameEvents.EnergyChanged(stats.energy, stats.maxEnergy);
    ultimateEnergyLockoutRemaining = ultimateEnergyLockoutDuration;
}

// 4) Exposição read-only do moveInput — perto de AimDirection, linha ~93
protected Vector2 MoveInput => moveInput;
```

Nenhuma outra linha de `HeroController.cs` muda. Barbarian/Ranger/Mage continuam
funcionando exatamente como antes (todos os hooks novos são default/no-op).

---

## Seção 2 — `Druid.cs` (novo arquivo)

```csharp
using System.Collections.Generic;
using UnityEngine;

public class Druid : HeroController
{
    // ---------- Ataque primário — vinhas (GDD Seção 17.4) ----------
    [Header("Ataque primário — vinhas")]
    [SerializeField] private GameObject vinePrefab;
    [SerializeField] private int vineCount = 1; // 🔢 teto 15, +1 por tier de arma — mesmo padrão placeholder do arrowCount do Ranger (Sprint 17/18), hook de Tier real ainda não existe
    [SerializeField] private float vineSearchRadius = 15f; // 🔢 ajustável
    [SerializeField] private LayerMask enemyLayerMask; // configurar no Inspector = layer dos monstros
    private bool vineHitFired;

    // Rede de segurança genérica — mesmo padrão do Barbarian/Ranger. Só vale pras janelas
    // curtas (vinha, garra do Alce, start/end da Coruja); "during" do Alce/Coruja tem teto
    // próprio, mais longo, tratado à parte no Update().
    [SerializeField] private float maxActionDuration = 3f; // 🔢 ajustável
    private float actionElapsed;

    // ---------- Ultimate — transformação em Alce (GDD Seção 17.4) ----------
    [Header("Ultimate — Alce")]
    [SerializeField] private RuntimeAnimatorController elkAnimatorController; // substitui TODAS as animações durante a forma Alce
    [SerializeField] private float elkMaxHealthMultiplier = 2f; // 🔢 GDD marca como "TBD" — ajustável em teste
    [SerializeField] private float elkDamageMultiplier = 1.5f; // 🔢 GDD: "mais dano", sem número — ajustável
    [SerializeField] private float elkMoveSpeedMultiplier = 1.3f; // 🔢 GDD: "mais velocidade", sem número — ajustável
    [SerializeField] private float elkFormMaxDuration = 30f; // 🔢 GDD: 30s
    [SerializeField] private float elkKnockbackForce = 4f; // 🔢 ajustável, mesmo padrão do Barbarian
    [SerializeField] private Collider2D elkClawHitboxNE;
    [SerializeField] private Collider2D elkClawHitboxNW;
    [SerializeField] private Collider2D elkClawHitboxSE;
    [SerializeField] private Collider2D elkClawHitboxSW;

    private RuntimeAnimatorController humanController;
    private bool isElkForm;
    private bool isTransformImmune; // só true durante os clipes de transformar-em/transformar-de-volta
    private float elkFormElapsed;
    private float humanMaxHealth, humanDamage, humanMoveSpeed;
    private bool elkClawHitFired;

    // ---------- Habilidade Secundária (Shift) — transformação em Coruja (GDD Seção 16/17.4) ----------
    [Header("Habilidade Secundária (Shift) — Coruja")]
    [SerializeField] private float owlDuration = 10f; // 🔢 GDD não dá teto — ajustável, mesma ideia do teto de camuflagem do Ranger
    private bool isOwlForm; // só true na fase "during"
    private float owlElapsed;

    protected override void Awake()
    {
        base.Awake();
        humanController = animator != null ? animator.runtimeAnimatorController : null;
    }

    protected override void Update()
    {
        if (!GameplayGate.IsActive) return;

        base.Update();

        // Rede de segurança genérica — suprimida durante as fases "during" longas (cada uma
        // tem o próprio teto de tempo, tratado abaixo), mesmo padrão do
        // "if (isAttacking && !isCamouflaged)" do Ranger.
        if (isAttacking && !isOwlForm && !isElkForm)
        {
            actionElapsed += Time.deltaTime;
            if (actionElapsed >= maxActionDuration)
            {
                Debug.LogWarning("[Druid] Animation Event de fim de ação nunca chegou — forçando fim (verifique o Animator Controller).");
                isAttacking = false;
                isTransformImmune = false; // não deixa imune pra sempre se o evento de transformação falhar
            }
        }

        if (isOwlForm)
        {
            owlElapsed += Time.deltaTime;
            if (owlElapsed >= owlDuration) EndOwlForm();
            UpdateOwlMoveParams();
        }

        if (isElkForm)
        {
            elkFormElapsed += Time.deltaTime;
            if (elkFormElapsed >= elkFormMaxDuration) EndElkForm();
        }
    }

    // ===================== PRIMÁRIO =====================

    protected override void PrimaryAttack()
    {
        if (isAttacking) return;

        if (isElkForm) { StartElkClawAttack(); return; }

        isAttacking = true;
        actionElapsed = 0f;
        vineHitFired = false;
        AnimatorTrigger("AttackTrigger");
    }

    // Animation Event, no frame exato em que as vinhas saem do chão.
    public void AnimationVineHitEvent()
    {
        if (vineHitFired) return;
        vineHitFired = true;

        var hits = Physics2D.OverlapCircleAll(transform.position, vineSearchRadius, enemyLayerMask);

        // Não existe registro/enumerador de monstros no projeto (confirmado em EnemyController) —
        // busca própria. Cada EnemyController só entra 1x na lista, então "nunca repete um
        // alvo nesta ativação" (GDD) já sai de graça, sem marcador nenhum.
        var enemies = new List<EnemyController>();
        foreach (var hit in hits)
        {
            var enemy = hit.GetComponent<EnemyController>();
            if (enemy != null && !enemies.Contains(enemy)) enemies.Add(enemy);
        }

        enemies.Sort((a, b) =>
            Vector2.Distance(transform.position, a.transform.position)
                .CompareTo(Vector2.Distance(transform.position, b.transform.position)));

        int count = Mathf.Min(vineCount, enemies.Count);
        for (int i = 0; i < count; i++)
        {
            var target = enemies[i];
            if (vinePrefab != null) Instantiate(vinePrefab, target.transform.position, Quaternion.identity);

            // SUPOSIÇÃO (Seção 0, item 4) — dano direto, um hit cheio por vinha.
            target.TakeDamage(stats.damage);
        }
    }

    // Animation Event, no fim do clipe de vinhas.
    public void AnimationVineEndEvent()
    {
        isAttacking = false;
    }

    // ===================== ULTIMATE — ALCE =====================

    protected override void UseUltimate()
    {
        if (isAttacking) return;
        if (isUsingSecondaryAbility) return; // mutuamente exclusivo com a Coruja (Seção 0, item 6)

        isAttacking = true;
        isTransformImmune = true;
        elkFormElapsed = 0f;

        humanMaxHealth = stats.maxHealth;
        humanDamage = stats.damage;
        humanMoveSpeed = stats.moveSpeed;

        // Troca o controller inteiro — a animação de transformar-em já é o estado padrão do
        // controller do Alce (configurar no Editor, ver Seção 5), não precisa de Trigger extra.
        if (animator != null) animator.runtimeAnimatorController = elkAnimatorController;
    }

    protected override bool IsUltimateActive => isElkForm;

    protected override void CancelUltimate()
    {
        if (!isElkForm) return;
        EndElkForm();
    }

    // Animation Event, no fim do clipe de transformar-em-Alce.
    public void AnimationElkTransformInEndEvent()
    {
        isTransformImmune = false;
        isElkForm = true;
        isAttacking = false; // libera o ataque — agora vira a garra do Alce, não mais vinha

        stats.maxHealth = humanMaxHealth * elkMaxHealthMultiplier;
        stats.health = stats.maxHealth; // GDD: cura pra 100% do HP máximo do Alce
        stats.damage = humanDamage * elkDamageMultiplier;
        stats.moveSpeed = humanMoveSpeed * elkMoveSpeedMultiplier;
        GameEvents.HealthChanged(stats.health, stats.maxHealth);
    }

    private void StartElkClawAttack()
    {
        isAttacking = true;
        actionElapsed = 0f;
        elkClawHitFired = false;
        AnimatorTrigger("AttackTrigger"); // trigger próprio do controller do Alce, mesmo nome, asset diferente
    }

    // Animation Event, no frame exato em que a garra acerta.
    public void AnimationElkClawHitEvent()
    {
        if (elkClawHitFired) return;
        elkClawHitFired = true;

        Collider2D hitbox = GetElkClawHitboxForFacing();
        if (hitbox == null) return;

        var results = new Collider2D[16];
        int count = hitbox.Overlap(ContactFilter2D.noFilter, results);
        for (int i = 0; i < count; i++)
        {
            if (!results[i].CompareTag("Enemy")) continue;
            var enemy = results[i].GetComponent<EnemyController>();
            if (enemy == null) continue;

            enemy.TakeDamage(stats.damage); // já multiplicado (AnimationElkTransformInEndEvent)
            enemy.ApplyKnockback(AimDirection, elkKnockbackForce);
        }
    }

    // Animation Event, no fim do clipe de garra.
    public void AnimationElkClawEndEvent()
    {
        isAttacking = false;
    }

    // Mesmo critério de quadrante do Barbarian.GetHitboxForFacing — duplicado de propósito
    // (é local a cada herói lá também, não é utilitário compartilhado no projeto).
    private Collider2D GetElkClawHitboxForFacing()
    {
        bool east = AimDirection.x >= 0f;
        bool north = AimDirection.y >= 0f;
        if (north) return east ? elkClawHitboxNE : elkClawHitboxNW;
        return east ? elkClawHitboxSE : elkClawHitboxSW;
    }

    // Compartilhado entre o cancelamento manual e o teto de 30s — os dois terminam do mesmo jeito.
    private void EndElkForm()
    {
        isElkForm = false;
        isAttacking = true; // bloqueia de novo durante o clipe de transformar-de-volta
        isTransformImmune = true;

        // GDD: cancelar zera toda a Energia da Ultimate — mesmo custo de deixar o timer
        // acabar (já está em 0 desde a ativação, mas setar explícito documenta a intenção e
        // cobre qualquer fonte futura de recarga de energia durante o "during").
        stats.energy = 0f;
        GameEvents.EnergyChanged(stats.energy, stats.maxEnergy);

        AnimatorTrigger("ElkTransformOutTrigger");
    }

    // Animation Event, no fim do clipe de transformar-de-volta.
    public void AnimationElkTransformOutEndEvent()
    {
        // GDD: HumanCurrentHealth = HumanMaxHealth × (CurrentElkHealth/ElkMaxHealth) — nunca
        // absoluto, nunca cura cheia.
        float ratio = stats.maxHealth > 0f ? stats.health / stats.maxHealth : 0f;

        stats.maxHealth = humanMaxHealth;
        stats.damage = humanDamage;
        stats.moveSpeed = humanMoveSpeed;
        stats.health = stats.maxHealth * ratio;
        GameEvents.HealthChanged(stats.health, stats.maxHealth);

        if (animator != null) animator.runtimeAnimatorController = humanController;
        isTransformImmune = false;
        isAttacking = false;
    }

    // TakeDamage vira virtual na base (Seção 1) só pra isso: imunidade durante as duas
    // animações de transição, não durante o "during" em si (lá o Alce toma dano normal).
    public override void TakeDamage(float amount)
    {
        if (isTransformImmune) return;
        base.TakeDamage(amount);
    }

    // GDD: morrer DURANTE a transformação é a única exceção — pula a conversão proporcional
    // e segue o fluxo universal de morte (Respawn() da base já cura pro stats.maxHealth, que
    // aqui já volta a ser o humano antes disso rodar).
    protected override void OnHeroDeath()
    {
        if (!isElkForm) return;

        stats.maxHealth = humanMaxHealth;
        stats.damage = humanDamage;
        stats.moveSpeed = humanMoveSpeed;
        if (animator != null) animator.runtimeAnimatorController = humanController;
        isElkForm = false;
    }

    // ===================== SECUNDÁRIA (SHIFT) — CORUJA =====================

    protected override void UseSecondaryAbility()
    {
        if (isElkForm) return; // mutuamente exclusivo com o Alce (Seção 0, item 6)

        // Ao contrário do Alce, aqui isAttacking fica true a viagem INTEIRA (start+during+end)
        // — a Coruja bloqueia SÓ ataque (GDD), então isAttacking nunca vira false até o fim;
        // o movimento continua livre porque o gate de movimento do HeroController.Update() é
        // por nome de estado do Animator, não por isAttacking (ver Seção 0, item 8 / Seção 5).
        isAttacking = true;
        actionElapsed = 0f;
        AnimatorTrigger("OwlTransformStartTrigger");
    }

    // Animation Event no fim do clipe "start" — o Animator já transiciona sozinho pro
    // "during" (Exit Time, sem condição), mesmo padrão do Ranger.
    public void AnimationOwlHiddenEvent()
    {
        isOwlForm = true;
        owlElapsed = 0f;
        IsPlayerUntargetable = true;
    }

    private void UpdateOwlMoveParams()
    {
        if (animator == null) return;
        Vector2 dir = SnapTo4Cardinals(MoveInput);
        animator.SetFloat("OwlMoveX", dir.x);
        animator.SetFloat("OwlMoveY", dir.y);
    }

    private static Vector2 SnapTo4Cardinals(Vector2 v)
    {
        if (v.sqrMagnitude < 0.0001f) return Vector2.zero; // parado — Blend Tree cai no Idle da Coruja
        return Mathf.Abs(v.x) > Mathf.Abs(v.y)
            ? new Vector2(Mathf.Sign(v.x), 0f)
            : new Vector2(0f, Mathf.Sign(v.y));
    }

    // Shift de novo durante a Coruja — só faz efeito depois que "during" já começou (mesmo
    // critério do Ranger: cancelar no meio do "start" ainda não é caso de uso).
    protected override void CancelSecondaryAbility()
    {
        if (!isOwlForm) return;
        EndOwlForm();
    }

    // Compartilhado entre cancelamento manual e o teto de owlDuration.
    private void EndOwlForm()
    {
        isOwlForm = false;
        IsPlayerUntargetable = false;
        AnimatorTrigger("OwlTransformEndTrigger");
    }

    // Animation Event no fim do clipe "end" (coruja virando herói de novo).
    public void AnimationOwlEndEvent()
    {
        isAttacking = false;
        isUsingSecondaryAbility = false;
    }
}
```

---

## Seção 3 — Prefab da vinha (`Vine.cs`, novo, opcional-mínimo)

A GDD não descreve nenhum comportamento visual/timing próprio pra vinha além de "aparece no
alvo" — o dano já é aplicado direto em `AnimationVineHitEvent` (Druid.cs), então o prefab por
si só não *precisa* de lógica. Ele existe só pra a vinha aparecer e sumir sozinha (efeito
visual), mesmo padrão do `groundCrackPrefab` do Barbarian:

```csharp
using UnityEngine;

public class Vine : MonoBehaviour
{
    [SerializeField] private float lifetime = 1.5f; // 🔢 ajustável — tempo que a vinha fica visível

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }
}
```

Se a vinha precisar prender/imobilizar o monstro por um tempo (ao invés de só dano instantâneo)
isso muda pra usar `StatusEffectController`/`SetTrapped`-equivalente do lado do monstro — mas
isso não está na GDD que tenho, então fica fora do escopo até confirmação (ver Seção 0, item 4).

---

## Seção 4 — Floor 3A e Floor 4A (setup usando o `FloorPopulationManager` novo)

Sem conteúdo de Bestiário nesta sprint (confirmado no plano de produção) — os dois Floors
ficam com o `monsterPrefabs` **vazio** por enquanto (`SpawnOne()` já trata isso com
early-return seguro, nada quebra).

### Montar na Scene

1. **Duplicar a estrutura do Floor 2A** (GameObject com `FloorDefinition` + filhos de
   geometria/colisão) duas vezes, renomeando pra `Floor3A` e `Floor4A`. Ajustar
   `floorName`, `originalFloorIdentity` (3 e 4) e `activeFloorPosition` em cada
   `FloorDefinition`.
2. **Arte/colisão de nível** pros dois Floors — precisa existir geometria real com
   `Collider2D` antes do Scan, senão o `GridGraph` considera tudo caminhável (ver comentário
   já existente em `FloorPopulationManager.TryGetRandomGraphPoint`, isso é estado seguro, só
   não fica com a "forma" certa até a arte final entrar).
3. **AstarPath da cena** — no componente `AstarPath` (Editor), adicionar 2 novos `GridGraph`,
   um por Floor, nomeados por exemplo `Floor3A_Grid` e `Floor4A_Grid`. Rodar **Scan** depois
   que a geometria/colisão dos dois Floors já estiver na cena.
4. Em cada `FloorDefinition` novo, preencher `astarGraphName` com o nome exato do graph
   correspondente (`"Floor3A_Grid"` / `"Floor4A_Grid"`) — tem que bater exatamente com o nome
   do graph no `AstarPath`, senão `FloorPopulationManager` cai no fallback sem restrição
   (comentário já existente no próprio código, estado seguro, mas os monstros de Floors
   diferentes poderiam se enxergar).
5. Adicionar `FloorPopulationManager` em cada Floor novo (ou reaproveitar o padrão de
   GameObject dedicado, igual Floor1A/2A): `ownerFloor` apontando pro `FloorDefinition`
   correspondente, `monsterPrefabs` **vazio** (propositalmente, ver acima), `config`
   (minimum/target/maximum) pode ficar com os valores default por enquanto — sem prefab
   nenhum, `SpawnOne()` não faz nada mesmo que o config peça reposição.
6. **Escadas** — no topo do Floor2A (ou onde a progressão já prevê a subida), adicionar/ajustar
   um `Stair` com `goesUp = true`, `ownerFloor` apontando pro Floor2A, e um `arrivalPoint`
   (Transform filho, posicionado à mão) no ponto de chegada dentro do Floor3A. Repetir o
   par simétrico Floor3A → Floor4A. Se quiser transição de tela (`stairTransition`), usar um
   `TransitionSettings` mais rápido que o de respawn, mesmo critério do comentário já existente
   em `Stair.cs`.
7. Testar a navegação **antes** de considerar os Floors "jogáveis": sem nenhum monstro pra
   validar o pathfinding ainda (não tem prefab), o teste aqui é só posição/colisão/transição —
   o pathfinding real só é validado de novo quando o Bestiário da próxima sprint entrar.

### Montar o Druid na Scene

1. No prefab/GameObject do Druid: trocar o script de herói pra `Druid` (`HeroController`
   abstrato não pode ser usado direto, mesmo padrão dos outros heróis).
2. Configurar os 4 `Collider2D` do golpe de garra do Alce (`elkClawHitboxNE/NW/SE/SW`) como
   filhos do Druid, iguais em espírito aos 4 hitboxes do Barbarian (raio/posição ajustados pro
   alcance da garra, não da espada).
3. Atribuir `vinePrefab` (Seção 3) e configurar `enemyLayerMask` = mesma layer usada pelos
   monstros em todo o resto do projeto.
4. Criar/atribuir `elkAnimatorController` — um **Animator Controller separado**, cujo estado
   padrão (default state) é o clipe de transformar-em-Alce. Dentro dele: estado padrão
   "ElkTransformIn" → (Exit Time ou Animation Event, ver Seção 5) → estado "Attack" (Blend
   Tree 2D 4-diagonal, igual o do Barbarian, só que com a arte do Alce) → gatilho
   "ElkTransformOutTrigger" leva pro estado "ElkTransformOut". Parâmetros mínimos que esse
   controller precisa ter, com os MESMOS nomes que o `HeroController` já seta todo frame
   (`AimX`, `AimY`, `DiagonalAimX`, `DiagonalAimY`, `IsMoving`, `DamageSpeedMultiplier`,
   `IsTrapped`) — sem eles o `SetFloat`/`SetBool` da base simplesmente não faz nada (Unity não
   lança erro), mas as animações do Alce não vão reagir a mira/movimento se os parâmetros não
   existirem nesse controller também.
5. No Animator Controller **humano** do Druid (o de sempre): adicionar a sub-state-machine da
   Coruja com os 3 estados (`OwlTransformStart` → Exit Time → `Walk`/`Idle` da fase "during",
   nomeados exatamente assim dentro da sub-machine → `OwlTransformEnd` via
   `OwlTransformEndTrigger`). Ver Seção 5 pro motivo do nome "Walk" ser obrigatório.
6. Configurar `OwlMoveX`/`OwlMoveY` como novos parâmetros Float nesse mesmo controller humano,
   alimentando o Blend Tree de 4 pontos cardeais da fase "during" da Coruja.

---

## Seção 5 — Nota sobre o gate de movimento da Coruja (por que "movimento livre" funciona de graça)

`HeroController.Update()` já decide se o herói anda assim, sem nenhuma mudança nesta sprint:

```csharp
if (wantsToMove && AnimatorStateCheck.IsInState(animator, "Walk"))
    transform.Translate(moveInput * stats.moveSpeed * Time.deltaTime);
```

Isso não depende de `isAttacking` — depende só do **nome do estado atual do Animator**. A
camuflagem do Ranger bloqueia movimento não porque `isAttacking` seja `true`, mas porque
nenhum dos 3 estados da camuflagem se chama "Walk". A Coruja faz o oposto de propósito: fica
com `isAttacking = true` a viagem inteira (bloqueando ataque/ultimate, que SÃO checados via
`isAttacking`), mas a fase "during" tem um estado literalmente chamado "Walk" (pode estar
aninhado numa sub-state-machine, `Animator.IsName()` casa pelo nome curto). Resultado: ataque
bloqueado, movimento livre, sem tocar em nenhuma linha do gate de movimento da base. É por
isso que o item 8 da Seção 0 pede esse nome exato no Animator — é a única peça que faz esse
comportamento "de graça" funcionar.

---

## Seção 6 — Checklist de teste manual

- [ ] **T01** — Vinha: com 3+ monstros no alcance de `vineSearchRadius` e `vineCount = 2`,
  ativar o Primário e confirmar que exatamente 2 monstros (os 2 mais próximos) tomam dano, os
  demais não.
- [ ] **T02** — Vinha: com só 1 monstro no alcance e `vineCount = 5`, confirmar que não quebra
  (só 1 vinha sai, sem erro de índice).
- [ ] **T03** — Vinha: segurar o botão de ataque não dispara vinha 2x por causa do
  Blend Tree misturando 2 clipes diagonais (`vineHitFired` cobrindo isso).
- [ ] **T04** — Confirmar visualmente/no dano se "dano direto = stats.damage por vinha" (Seção
  0, item 4) é o efeito esperado ou se precisa virar outra coisa.
- [ ] **T05** — Alce: ativar a Ultimate com energia cheia, confirmar troca de Animator
  Controller, cura pra 100% do HP do Alce, e que nenhum dano é aplicado durante a animação de
  transformar-em (bater um monstro nela durante essa janela e ver que não recebe dano).
- [ ] **T06** — Alce: atacar como Alce (garra), confirmar as 4 direções de hitbox e knockback.
- [ ] **T07** — Alce: deixar o timer de 30s estourar sozinho, confirmar transformação de volta
  e a conversão proporcional de HP (tomar dano como Alce até ~50% da vida do Alce, then deixar
  acabar, confirmar que o humano volta com ~50% do HP humano, não com HP cheio nem com o valor
  absoluto do Alce).
- [ ] **T08** — Alce: cancelar manualmente (apertar Ultimate de novo) no meio do "during",
  confirmar que zera a Energia e converte o HP do mesmo jeito que o timeout (T07).
- [ ] **T09** — Alce: **morrer durante a transformação** (durante o "during", com HP baixo o
  suficiente) — confirmar que NÃO roda a conversão proporcional e que o respawn volta com HP
  humano cheio, forma humana, controller humano.
- [ ] **T10** — Alce: tentar ativar a Coruja enquanto já é Alce — confirmar que é ignorado
  (mutuamente exclusivo).
- [ ] **T11** — Coruja: ativar o Shift, confirmar que o Primário/Ultimate ficam bloqueados
  durante as 3 fases, mas o movimento continua livre durante o "during" (andar em pelo menos
  as 4 direções cardeais e ver a animação trocar).
- [ ] **T12** — Coruja: confirmar `IsPlayerUntargetable` de verdade — um monstro que já estava
  perseguindo o Druid perde o alvo assim que "during" começa (mesmo teste já usado pra
  camuflagem do Ranger).
- [ ] **T13** — Coruja: deixar `owlDuration` estourar sozinho E cancelar manualmente (Shift de
  novo) — os dois devem terminar a transformação do mesmo jeito.
- [ ] **T14** — Coruja: tentar ativar a Ultimate (Alce) enquanto já é Coruja — confirmar que é
  ignorado (mutuamente exclusivo).
- [ ] **T15** — Floor 3A/4A: escada sobe/desce corretamente entre 2A→3A→4A, `arrivalPoint`
  posiciona o jogador no lugar certo, sem "salto" de câmera visível.
- [ ] **T16** — Floor 3A/4A: com `monsterPrefabs` vazio, confirmar que nada quebra (sem
  exceptions no console) mesmo com o Floor ativo por um tempo.
- [ ] **T17** — Barbarian e Ranger continuam funcionando exatamente como antes (rodar os
  testes manuais já existentes deles de relance) — confirma que as 3 mudanças na
  `HeroController` (Seção 1) não regrediram nada.

---

## Fechamento

Commits sugeridos (um por unidade de trabalho testável, na ordem):

```
git commit -m "feat(hero): expõe hooks de cancelamento de Ultimate e MoveInput na base HeroController"
git commit -m "feat(druid): ataque primário com vinhas (alvo mais próximo sem repetição)"
git commit -m "feat(druid): ultimate — transformação em Alce com troca de Animator Controller e conversão proporcional de HP"
git commit -m "feat(druid): habilidade secundária — transformação em Coruja (bloqueia só ataque, movimento livre)"
git commit -m "feat(floor): Floor 3A e 4A com FloorPopulationManager baseado em GridGraph, sem conteúdo de Bestiário"
```

Depois de testado e fechado, eu atualizo o plano de produção (linha 21 já reflete esse escopo,
só preciso confirmar o fechamento da Deadline 6 quando você reportar) — sem necessidade de
mudança na GDD além do que ela já descreve (os 3 multiplicadores/tetos ficam documentados como
🔢 ajustáveis no código, não na GDD, mesmo padrão já usado no projeto inteiro).

**Pronto quando:** Druid ataca com vinhas sem repetir alvo na mesma ativação, a Ultimate
transforma em Alce com troca completa de animações e HP convertido proporcionalmente na volta
(exceto morte durante a transformação, que pula a conversão), a Secundária transforma em
Coruja bloqueando só o ataque com movimento livre, e os Floors 3A/4A são navegáveis via
GridGraph com escadas funcionando — tudo isso sem regredir Barbarian/Ranger.
