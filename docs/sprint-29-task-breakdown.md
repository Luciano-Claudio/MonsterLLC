# Sprint 29 — Assassin (GDD Seção 17.9)

> Prospectiva. Reaproveitei 3 mecanismos já existentes: a Cambalhota do Rogue (movimento de
> dash ao longo do tempo + dedup por HashSet), a transformação em Alce do Druid (troca completa
> de Animator Controller + imunidade só nas transições) e a Coruja do Druid / camuflagem do
> Ranger (`HeroController.IsPlayerUntargetable`, mesmo campo estático, pra stealth). Nenhum
> código novo precisou ser "adivinhado" sem precedente — mas 2 patches pequenos fora do
> `Assassin.cs` são necessários (ver Seção 0, itens 0 e 1).

## Seção 0 — Decisões e suposições (revisar antes de implementar)

0. **Patch necessário em `EnemyController.cs` — monstros de emboscada não podem re-dormir
   durante stealth.** A GDD é explícita sobre isso ser o único cuidado real desta sprint. Achei
   a causa exata em `UpdatePatrol()`: hoje, QUALQUER monstro com `staysDormantUntilDetected`
   volta pra pose dormente sempre que `isInCombat` vira false — incluindo quando o motivo é só
   `HeroController.IsPlayerUntargetable` (camuflagem/stealth), não falta de detecção real. Isso
   já afeta a camuflagem do Ranger hoje (nunca foi flagrado antes), e afetaria o stealth do
   Assassin do mesmo jeito. Fix:
   ```csharp
   // EnemyController.cs — UpdatePatrol()
   if (staysDormantUntilDetected && !HeroController.IsPlayerUntargetable)
   {
       SetMoving(false);
       SetInCombat(false);
       return;
   }
   ```
   Com isso, durante qualquer stealth (Ranger ou Assassin), um monstro de emboscada cai pro
   mesmo fluxo de patrulha de um monstro comum (`patrolAI.Tick(...)`, a chamada logo abaixo) em
   vez de travar na pose estática — exatamente "continuam andando/parados normalmente, sem
   re-dormir".

1. **Patch necessário em `HeroController.cs` — novo hook pra "dash sem cooldown durante a
   ultimate".** Não existe hoje um jeito de um herói pular o cooldown do primário inteiro (só
   existe `ShouldConsumeCooldownOnAttack()`, que decide se um clique que "não fez nada" consome
   cooldown — não serve aqui, porque o dash sempre faz algo). Proponho o mesmo padrão:
   ```csharp
   // HeroController.cs — novo hook, default sem efeito em nenhum outro herói
   protected virtual bool IsAttackCooldownBypassed() => false;
   ```
   ```csharp
   // HeroController.cs — Update(), só essa linha muda
   if (attackHeld && !isAttacking && !isTrapped && ShouldConsumeCooldownOnAttack() && (IsAttackCooldownBypassed() || attackCooldown.TryConsume()))
       PrimaryAttack();
   ```
   O Assassin sobrescreve `IsAttackCooldownBypassed() => isStealthActive;`. `!isAttacking` já
   impede reiniciar o dash ANTES do atual terminar — então "sem cooldown" na prática só remove a
   espera ENTRE dashes, não permite 2 ao mesmo tempo.

2. **Direção do dash = `AimDirection`** (travada em 8 direções fixas), não `RawAimDirection` —
   a GDD pede "1 de 8 direções fixas", diferente da Cambalhota do Rogue (ângulo livre).

3. **Distância do dash é implícita** (velocidade × duração real do clipe "Start"), mesmo
   critério da Cambalhota do Rogue — `dashMaxSafetyDuration` é só rede de segurança, nunca o
   critério principal de quando parar.

4. **Dano contínuo ao longo do trajeto usa o mesmo dedup por HashSet da Cambalhota** (GetComponent
   + `Contains()`, sem `CollectDistinct` — o próprio HashSet já resolve o problema dos 2
   Collider2D por monstro). Cada monstro tocado leva exatamente 1 hit por dash, não 1 por frame
   de overlap.

5. **Thousand_Blades reaproveita a MESMA movimentação física do dash normal**, só que o dano
   não é contínuo — é 1 hit em área, só no frame do Animation Event "Effect" (GDD: "solta o dano
   no fim do dash, antes do End"). Usei `CollectDistinct` aqui (hit único em área, mesmo critério
   do Pulso do Rogue/garra do Alce), não o HashSet do dash contínuo.

6. **Imunidade da forma sombria — só durante as 2 animações de transição** (entrar/sair), igual
   ao Alce do Druid: a GDD cita "mesmo padrão do Druid" pro mecanismo, e entendi isso como
   cobrindo também o ESCOPO da imunidade (não a Ultimate inteira) — durante o "during" em si, o
   Assassin sombrio toma dano normal, igual o Alce. **Peço confirmação específica disso.**

7. **Stealth cancelável manualmente** (apertar Ultimate de novo cancela, mesma UX do Alce) — a
   GDD do Assassin não repete essa regra explicitamente (a do Druid sim, pro Alce), mas assumi
   por analogia direta ("mesmo padrão do Druid"). **Peço confirmação.**

8. `stealthDuration` não tem número na GDD — placeholder ajustável.

9. **Teleporte respeita `RawAimDistance` até um teto (`teleportMaxRange`)**, igual ao Mage — "com
   alcance máximo" interpretado como um TETO, não "sempre pula a distância máxima". A GDD não é
   100% explícita aqui, só contrasta "mais simples: sem a fase de projétil visual". **Peço
   confirmação** se o certo é sempre pular o máximo, independente de onde o mouse está.

10. **Teleporte imune a dano durante toda a duração** (`isUsingSecondaryAbility`) — estendendo o
    padrão já usado por TODA Habilidade Secundária de mobilidade/sobrevivência do projeto
    (Cambalhota, teleporte do Mage, Coruja do Druid, camuflagem do Ranger), mesmo a GDD do
    Assassin não repetindo isso explicitamente.

11. **Sem projétil/fase de voo** — a posição muda direto no Animation Event `AnimationTeleportDisappearEvent()`
    (fim do clipe "disappear"), sem objeto intermediário nem callback — é o que a GDD pede
    ("mais simples: sem a fase de projétil visual").

12. **Passiva (maior velocidade base) é só um valor de stats**, sem código — `stats.moveSpeed`
    mais alto que a média no Inspector/`HeroStats`. A GDD menciona que esse traço foi
    "reatribuído do Rogue" (Seção 17.5) — se o texto da Seção 17.5 ainda disser isso sobre o
    Rogue, vale uma Nota removendo de lá.

---

## Código — `Assassin.cs`

```csharp
using System.Collections.Generic;
using UnityEngine;

public class Assassin : HeroController
{
    // ===================== Primário — Deadly Dash (GDD Seção 17.9) =====================
    [Header("Ataque primário — Deadly Dash (1 de 8 direções fixas)")]
    [SerializeField] private LayerMask enemyLayerMask;
    [SerializeField] private float dashSpeed = 14f; // 🔢 ajustável
    [SerializeField] private float dashHitRadius = 0.8f; // 🔢 ajustável — raio do trigger que acompanha o dash
    [SerializeField] private float dashKnockbackForce = 5f; // 🔢 ajustável
    [SerializeField] private float dashMaxSafetyDuration = 1f; // 🔢 rede de segurança — distância real = dashSpeed × duração do clipe (Seção 0, item 3)
    private bool isDashing;
    private bool isThousandBladesVariant;
    private float dashSafetyElapsed;
    private Vector2 dashDirection;
    private readonly HashSet<EnemyController> dashHitEnemiesThisActivation = new HashSet<EnemyController>();

    // ===================== Thousand Blades — variante sombria do primário (durante a Ultimate) =====================
    [Header("Primário sombrio — Thousand Blades (ativo só durante a forma sombria)")]
    [SerializeField] private float thousandBladesDamageMultiplier = 2f; // GDD: "2x o dano do Deadly_Dash normal"
    [SerializeField] private float thousandBladesHitRadius = 1f; // 🔢 ajustável — raio do hit único no Effect
    private bool thousandBladesHitFired;
    private readonly List<EnemyController> thousandBladesTargets = new();

    // Rede de segurança genérica — ações curtas sem fase "during" própria (dash tem a própria,
    // ver dashMaxSafetyDuration; stealth tem a própria, ver stealthDuration).
    [SerializeField] private float maxActionDuration = 3f; // 🔢 ajustável
    private float actionElapsed;

    // ===================== Ultimate — forma sombria =====================
    [Header("Ultimate — forma sombria (GDD Seção 17.9)")]
    [SerializeField] private RuntimeAnimatorController shadowAnimatorController; // substitui TODAS as animações durante a forma sombria
    [SerializeField] private float stealthDuration = 10f; // 🔢 GDD não dá número (Seção 0, item 8)
    private RuntimeAnimatorController humanController;
    private bool isStealthActive; // só true na fase "during" (depois de entrar, antes de sair)
    private bool isTransformImmune; // só true durante as 2 animações de transição — mesmo padrão do Druid (Seção 0, item 6)
    private float stealthElapsed;

    // ===================== Secundária (Shift) — Teleporte =====================
    [Header("Habilidade Secundária (Shift) — Teleporte")]
    [SerializeField] private float teleportMaxRange = 5f; // 🔢 ajustável (Seção 0, item 9)

    protected override void Awake()
    {
        base.Awake();
        humanController = animator != null ? animator.runtimeAnimatorController : null;
    }

    protected override void Update()
    {
        if (!GameplayGate.IsActive) return;
        base.Update();

        if (isAttacking && !isDashing)
        {
            actionElapsed += Time.deltaTime;
            if (actionElapsed >= maxActionDuration)
            {
                Debug.LogWarning("[Assassin] Animation Event de fim de ação nunca chegou — forçando fim (verifique o Animator Controller).");
                isAttacking = false;
                isTransformImmune = false;
            }
        }

        if (isDashing) UpdateDash();

        if (isStealthActive)
        {
            stealthElapsed += Time.deltaTime;
            if (stealthElapsed >= stealthDuration) EndStealthForm();
        }
    }

    // ===================== Primário =====================

    protected override void PrimaryAttack()
    {
        if (isAttacking) return;

        if (isStealthActive) { StartThousandBlades(); return; }

        isAttacking = true;
        actionElapsed = 0f;
        isDashing = true;
        isThousandBladesVariant = false;
        dashSafetyElapsed = 0f;
        dashDirection = AimDirection; // 1 de 8 direções fixas, já travada pela base (Seção 0, item 2)
        dashHitEnemiesThisActivation.Clear();
        AnimatorTrigger("AttackTrigger");
    }

    private void StartThousandBlades()
    {
        isAttacking = true;
        actionElapsed = 0f;
        isDashing = true; // mesma movimentação física do dash normal (Seção 0, item 5)
        isThousandBladesVariant = true;
        dashSafetyElapsed = 0f;
        dashDirection = AimDirection;
        thousandBladesHitFired = false;
        AnimatorTrigger("AttackTrigger"); // Animator decide Deadly_Dash x Thousand_Blades via IsStealthActive
    }

    private void UpdateDash()
    {
        string movingState = isThousandBladesVariant ? "ITS_Thousand_Blades_Start" : "Deadly_Dash_Start";
        bool canMove = AnimatorStateCheck.IsInState(animator, movingState);
        if (canMove)
        {
            transform.Translate(dashDirection * dashSpeed * Time.deltaTime);

            // Dano contínuo só no dash normal — Thousand_Blades aplica dano 1x no Effect (ver
            // AnimationThousandBladesEffectEvent), não ao longo do trajeto (Seção 0, item 5).
            if (!isThousandBladesVariant)
            {
                var hits = Physics2D.OverlapCircleAll(transform.position, dashHitRadius, enemyLayerMask);
                foreach (var hit in hits)
                {
                    var enemy = hit.GetComponent<EnemyController>();
                    if (enemy == null || dashHitEnemiesThisActivation.Contains(enemy)) continue;
                    dashHitEnemiesThisActivation.Add(enemy);
                    enemy.TakeDamage(stats.damage);
                    enemy.ApplyKnockback(dashDirection, dashKnockbackForce);
                }
            }
        }

        dashSafetyElapsed += Time.deltaTime;
        if (dashSafetyElapsed >= dashMaxSafetyDuration)
        {
            Debug.LogWarning("[Assassin] Animation Event de fim do dash nunca chegou — forçando fim (verifique o Animator Controller).");
            EndDash();
        }
    }

    // Animation Event, no frame exato do "Effect" do Thousand_Blades — hit único em área,
    // 2x o dano do dash normal (Seção 0, item 5).
    public void AnimationThousandBladesEffectEvent()
    {
        if (thousandBladesHitFired) return;
        thousandBladesHitFired = true;

        float damage = stats.damage * thousandBladesDamageMultiplier;
        var hits = Physics2D.OverlapCircleAll(transform.position, thousandBladesHitRadius, enemyLayerMask);
        EnemyController.CollectDistinct(hits, hits.Length, thousandBladesTargets);
        foreach (var enemy in thousandBladesTargets)
        {
            enemy.TakeDamage(damage);
            enemy.ApplyKnockback(dashDirection, dashKnockbackForce);
        }
    }

    // Animation Event, no último frame de QUALQUER UM dos dois clipes (Deadly_Dash_End ou
    // ITS_Thousand_Blades_End) — mesmo método, os dois terminam a ação do mesmo jeito.
    public void AnimationDashEndEvent()
    {
        EndDash();
    }

    private void EndDash()
    {
        if (!isDashing) return;
        isDashing = false;
        isThousandBladesVariant = false;
        isAttacking = false;
    }

    // Dash sem cooldown durante a Ultimate — patch necessário em HeroController.cs (Seção 0, item 1).
    protected override bool IsAttackCooldownBypassed() => isStealthActive;

    // ===================== Ultimate — forma sombria =====================

    protected override bool CanUseUltimate() => !isAttacking;

    protected override void UseUltimate()
    {
        isAttacking = true;
        isTransformImmune = true;
        actionElapsed = 0f;

        // Troca o controller inteiro — a animação de entrar na forma sombria já é o estado
        // padrão do controller sombrio (mesmo critério do Alce do Druid).
        if (animator != null) animator.runtimeAnimatorController = shadowAnimatorController;
        RefreshActionSpeedMultiplier(); // trocar de controller reseta os parâmetros pro default dele
    }

    protected override bool IsUltimateActive => isStealthActive;

    protected override void CancelUltimate()
    {
        if (!isStealthActive) return;
        EndStealthForm();
    }

    // Animation Event, no fim do clipe de entrar na forma sombria.
    public void AnimationStealthTransformInEndEvent()
    {
        isTransformImmune = false;
        isStealthActive = true;
        isAttacking = false;
        stealthElapsed = 0f;
        // Mesmo campo estático já usado pela Coruja do Druid / camuflagem do Ranger.
        IsPlayerUntargetable = true;
    }

    // Compartilhado entre cancelamento manual e o teto de stealthDuration.
    private void EndStealthForm()
    {
        isStealthActive = false;
        IsPlayerUntargetable = false;
        isAttacking = true; // bloqueia de novo durante o clipe de voltar ao normal
        isTransformImmune = true;

        // Mesmo custo do cancelamento manual do Alce — zera a Energia da Ultimate.
        stats.energy = 0f;
        GameEvents.EnergyChanged(stats.energy, stats.maxEnergy);

        AnimatorTrigger("StealthTransformOutTrigger");
    }

    // Animation Event, no fim do clipe de voltar ao normal.
    public void AnimationStealthTransformOutEndEvent()
    {
        if (animator != null) animator.runtimeAnimatorController = humanController;
        RefreshActionSpeedMultiplier();
        isTransformImmune = false;
        isAttacking = false;
    }

    // Cobre as 2 transições da forma sombria (isTransformImmune) e a duração inteira do
    // teleporte (isUsingSecondaryAbility) — mesmo critério combinado do Druid.
    protected override bool IsDamageImmune => isTransformImmune || isUsingSecondaryAbility;

    // Mesma exceção do Alce do Druid — morrer durante o stealth não pode deixar
    // IsPlayerUntargetable travado em true pra sempre (cegaria os monstros pro respawn inteiro).
    protected override void OnHeroDeath()
    {
        if (isStealthActive)
        {
            isStealthActive = false;
            IsPlayerUntargetable = false;
        }
    }

    // Mesmo motivo do Druid — se a morte aconteceu na forma sombria, o Animator ainda está no
    // controller sombrio (de propósito, pra tocar o "Die" dele); troca pro humano ANTES do
    // Respawn() da base tentar dar Play("Idle") num controller sem esse estado certo.
    public override void AnimationDieEndEvent()
    {
        if (animator != null && animator.runtimeAnimatorController != humanController)
        {
            animator.runtimeAnimatorController = humanController;
            RefreshActionSpeedMultiplier();
        }
        base.AnimationDieEndEvent();
    }

    // ===================== Secundária (Shift) — Teleporte =====================

    protected override void UseSecondaryAbility()
    {
        isAttacking = true;
        actionElapsed = 0f;

        // Pose travada numa das 4 diagonais (NE/NW/SE/SW, não as 8 completas — GDD) — mesmo
        // critério do RollAimX/Y da Cambalhota / TeleportAimX/Y do Mage. Não influencia a
        // direção real do teleporte (RawAimDirection), só a pose.
        if (animator != null)
        {
            Vector2 pose = DirectionUtility.SnapTo4Diagonals(RawAimDirection);
            animator.SetFloat("TeleportAimX", pose.x);
            animator.SetFloat("TeleportAimY", pose.y);
        }

        AnimatorTrigger("SecondaryAbilityTrigger");
    }

    // Animation Event, no fim do clipe "disappear" — sem fase de voo (diferente do Mage): o
    // teleporte acontece direto aqui, sem objeto intermediário (Seção 0, item 11).
    public void AnimationTeleportDisappearEvent()
    {
        float distance = Mathf.Min(RawAimDistance, teleportMaxRange); // Seção 0, item 9
        transform.position += (Vector3)(RawAimDirection * distance);
    }

    // Animation Event, no fim do clipe "appear".
    public void AnimationTeleportAppearEndEvent()
    {
        isAttacking = false;
        isUsingSecondaryAbility = false;
    }

    // Sem override de CancelSecondaryAbility — não cancelável (GDD).
}
```

---

## Patch necessário — `HeroController.cs`

```csharp
// Junto dos outros hooks virtuais (ShouldConsumeCooldownOnAttack, CanUseUltimate etc.) —
// Sprint 29 (Assassin): permite pular o cooldown do primário inteiro, não só o "não consome
// num clique que não fez nada". Default sem efeito em nenhum outro herói.
protected virtual bool IsAttackCooldownBypassed() => false;
```
```csharp
// HeroController.cs — Update(), só a condição do "if" muda
if (attackHeld && !isAttacking && !isTrapped && ShouldConsumeCooldownOnAttack() && (IsAttackCooldownBypassed() || attackCooldown.TryConsume()))
    PrimaryAttack();
```

## Patch necessário — `EnemyController.cs`

```csharp
// EnemyController.cs — UpdatePatrol(), só a condição do "if" muda
if (staysDormantUntilDetected && !HeroController.IsPlayerUntargetable)
{
    SetMoving(false);
    SetInCombat(false);
    return;
}
```

---

## Animator / prefab — notas de setup

- **Attack (Deadly Dash):** 2 estados, `Deadly_Dash_Start` (Blend Tree 2D de 8 direções — o
  dash acontece durante este) → `Deadly_Dash_End` (recovery, sem movimento), encadeados por Exit
  Time. `AnimationDashEndEvent()` no último frame de `Deadly_Dash_End`.
- **Attack sombrio (Thousand Blades):** 3 estados encadeados, `ITS_Thousand_Blades_Start`
  (movimento) → `ITS_Thousand_Blades_Effect` (`AnimationThousandBladesEffectEvent()` aqui,
  sem movimento) → `ITS_Thousand_Blades_End` (recovery). Roteamento entre o dash normal e este
  decidido por um bool novo no Animator, `IsStealthActive` (setado junto da troca de
  `isStealthActive` em código), já que os dois ficam no MESMO `AttackTrigger`.
- **Ultimate:** estado padrão do `shadowAnimatorController` já É a transformação de entrada
  (mesmo critério do Alce) — `AnimationStealthTransformInEndEvent()` no fim dela.
  `StealthTransformOutTrigger` dispara a transformação de saída, `AnimationStealthTransformOutEndEvent()`
  no fim.
- **`shadowAnimatorController`:** precisa do PRÓPRIO conjunto completo (`walk`/`idle`/`damage`/`die`),
  igual ao Druid — inclui o roteamento interno pro `AttackTrigger` escolher Thousand_Blades (via
  `IsStealthActive`, sempre true dentro deste controller) em vez de Deadly_Dash.
  `ActionSpeedMultiplier` precisa ser reconfigurado como Speed Parameter nos estados certos
  deste controller também (mesma regra de qualquer controller novo).
- **Secundária (Teleporte):** 2 clipes, `disappear` (Blend Tree 2D de 4 diagonais, via
  `TeleportAimX/Y`) → `appear` (mesmo par), encadeados por Exit Time — SEM trigger de chegada
  no meio (diferente do Mage), porque não há fase de voo. `AnimationTeleportDisappearEvent()` no
  fim de `disappear`, `AnimationTeleportAppearEndEvent()` no fim de `appear`.

---

## Checklist de teste

1. Dash sai sempre numa das 8 direções fixas, nunca num ângulo livre — mesmo mirando entre 2
   direções.
2. Dash acerta e empurra todo monstro tocado ao longo do trajeto, não só no ponto de chegada;
   cada monstro leva exatamente 1 hit por dash (parado no meio do trajeto não leva 2+).
3. Fora da forma sombria, dash respeita cooldown normal.
4. Dentro da forma sombria, dash encadeia várias vezes sem espera nenhuma (`IsAttackCooldownBypassed`).
5. Thousand_Blades aplica 2x o dano do Deadly_Dash normal, 1 hit só (não repetido ao longo do
   trajeto).
6. Entrar/sair da forma sombria: Assassin imune a dano durante as 2 transições; durante o
   "during" em si, toma dano normal (confirmar Seção 0, item 6).
7. Durante a forma sombria: monstros comuns perdem o alvo (`IsPlayerUntargetable`); monstros de
   emboscada (Skeleton/Gargoyle) continuam andando/parados normalmente, SEM voltar pra pose
   dormente (valida o patch do item 0).
8. Boss continua reagressando instantaneamente quando a forma sombria acaba (comportamento já
   existente, não deveria mudar).
9. Cancelar a forma sombria manualmente (apertar Ultimate de novo) funciona como o Alce.
10. Morrer durante a forma sombria: `IsPlayerUntargetable` volta a false, Animator troca pro
    controller humano antes do respawn tocar "Idle".
11. Teleporte: distância real nunca passa de `teleportMaxRange`, mesmo mirando mais longe;
    confirmar se deveria SEMPRE pular a distância máxima em vez disso (Seção 0, item 9).
12. Teleporte imune a dano do início ao fim; não cancelável (Shift de novo durante não faz
    nada).
13. Teleporte sem fase de projétil visível — morte/dano no trajeto não é possível fisicamente
    (é instantâneo), sem precisar de tratamento especial de `OnHeroDeath()` para ele.
