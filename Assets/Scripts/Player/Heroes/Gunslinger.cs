using UnityEngine;

public class Gunslinger : HeroController
{
    // Rework (2026-10-30) — Primário, Ultimate e Shift agora compartilham a MESMA sequência de
    // 3 estados no Animator (Attack_Start -> Attack_Shoot (loop) -> Attack_End, cada um um Blend
    // Tree de 8 direções), em vez de 15 clipes de Attack bakeados + giro fixo de 8 direções na
    // Ultimate + chicote dedicado no Shift. activeAction decide qual lógica os 2 Animation Events
    // compartilhados (AnimationShotFireEvent/AnimationAttackEndEvent) devem executar.
    private enum ActiveAction { None, Primary, Ultimate, Shift }
    private ActiveAction activeAction = ActiveAction.None;

    // ===================== Primário — rajada Hitscan (GDD Seção 17.8) =====================
    [Header("Ataque primário — rajada Hitscan (mira em 1 das 8 direções)")]
    [SerializeField] private LayerMask enemyLayerMask;
    [SerializeField] private float hitscanMaxRange = 10f; // 🔢 ajustável
    [SerializeField] private GameObject impactVfxPrefab; // Projectile_Impact — instanciado a cada tiro
    [SerializeField] private float impactVfxDuration = 0.5f; // 🔢 ajustável

    // Quantos tiros o primário dispara por ativação — agora é só um contador de repetições do
    // loop de Attack_Shoot (ver AnimationShotFireEvent), não mais uma seleção de clipe bakeado
    // (Attack_1..Attack_15 não existem mais).
    [SerializeField] private int shotCount = 1; // 🔢 upgradable — 1 a 15 (ver OnValidate)

    // Cone de imprecisão — cada tiro da rajada sorteia o próprio desvio, independente dos
    // outros. Só entra em jogo com shotCount > 1 (ou no Shift, sempre > 1). Compartilhado entre
    // Primário e Shift.
    [SerializeField] private float shotMaxDeviationAngle = 15f; // 🔢 ajustável — teto do desvio por tiro
    private const float MinShotDeviationAngle = 0.1f;

    [Header("Debug — linhas de tiro no Gizmo (Scene, objeto selecionado), não afeta gameplay")]
    [SerializeField] private bool showShotLinesGizmo = false;

    // Rede de segurança genérica — cobre Primário, Ultimate e Shift (os 3 agora passam pela
    // mesma sequência Attack_Start/Attack_Shoot/Attack_End).
    [SerializeField] private float maxActionDuration = 3f; // 🔢 ajustável
    private float actionElapsed;

    // Contador compartilhado do loop de Attack_Shoot — usado pelo Primário e pela Ultimate (o
    // Shift dispara 1x só, sem loop).
    private int shotsFiredThisAction;
    private int shotsRequiredThisAction;

    // Guarda contra o disparo duplo do Blend Tree 2D (Attack_Shoot ainda mistura 2 clipes com
    // peso > 0 quando a mira cai entre 2 das 8 direções) — mesmo bug de sempre (ver Barbarian/
    // Ranger/EnemyController), agora compartilhado pelos 3 usos do Animation Event.
    private int lastShotFireFrame = -1;

    // ===================== Ultimate — tiro teleguiado no monstro mais próximo (rework 2026-10-30) =====================
    // Sempre acerta — não é mais hitscan (sem chance de errar), e não gira mais em 8 direções
    // fixas. A cada tiro, busca de novo o monstro vivo mais próximo (pode repetir o mesmo alvo
    // se ele continuar sendo o mais próximo) e vira a pose (GunAimX/Y) pra direção dele.
    [Header("Ultimate — tiro teleguiado (sempre acerta o monstro mais próximo; sem animação própria, reaproveita Attack_Start/Shoot/End)")]
    [SerializeField] private int ultimateHitCount = 5; // 🔢 ajustável — contador PRÓPRIO, independente do shotCount do primário
    [SerializeField] private float ultimateDamageMultiplier = 1f; // 🔢 ajustável
    [SerializeField] private float ultimateSearchRadius = 20f; // 🔢 ajustável — raio de busca do monstro mais próximo
    private Vector2 currentUltimateDirection;

    // ===================== Habilidade Secundária (Shift) — rajada de pelotas + recuo (rework 2026-10-30) =====================
    // Substitui o chicote antigo. 1 tiro visual só (attack_shoot roda 1x, sem loop), mas solta
    // shiftPelletCount tiros de uma vez (mesmo cone de imprecisão do primário) e empurra o
    // próprio Gunslinger pra trás (impulso físico real, respeita colisão — ver UpdateRecoil).
    [Header("Habilidade Secundária (Shift) — rajada de pelotas + recuo")]
    [SerializeField] private int shiftPelletCount = 5; // 🔢 ajustável — fixo, independente do shotCount/ultimateHitCount
    [SerializeField] private float shiftDamageMultiplier = 1f; // 🔢 ajustável
    [SerializeField] private float shiftRecoilSpeed = 8f; // 🔢 ajustável — velocidade do impulso pra trás
    [SerializeField] private float shiftRecoilDuration = 0.15f; // 🔢 ajustável — por quanto tempo o impulso dura
    private float recoilRemaining;
    private Vector2 recoilVelocity;

    // ===================== Passiva — monstros dropam 2x mais loot =====================
    [Header("Passiva — loot em dobro (requer patch em HeroController.cs/EnemyController.cs)")]
    [SerializeField] private float lootMultiplier = 2f; // 🔢 GDD: "2x mais loot", ajustável

#if UNITY_EDITOR
    private void OnValidate()
    {
        shotCount = Mathf.Clamp(shotCount, 1, 15);
        shotMaxDeviationAngle = Mathf.Max(shotMaxDeviationAngle, MinShotDeviationAngle);
    }

    // Gizmo de debug — vermelho: 1 linha por tiro da rajada do primário (nº = shotCount)
    // saindo da posição atual na direção da mira. Sorteia de novo a cada repaint da Scene view,
    // de propósito: olhando por alguns segundos dá pra ver o espalhamento completo entre as
    // linhas mais distantes e calibrar shotMaxDeviationAngle por cima disso. A Ultimate não tem
    // mais gizmo aqui — a direção real depende dos monstros vivos em campo, só visível em Play Mode.
    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected(); // gizmo do Pickup Radius (HeroController), se ligado no Inspector

        if (!showShotLinesGizmo) return;

        Vector2 origin = transform.position;

        Gizmos.color = Color.red;
        if (shotCount <= 1)
        {
            Gizmos.DrawLine(origin, origin + RawAimDirection * hitscanMaxRange);
        }
        else
        {
            for (int i = 0; i < shotCount; i++)
            {
                Vector2 dir = RotateDegrees(RawAimDirection, RandomShotDeviation());
                Gizmos.DrawLine(origin, origin + dir * hitscanMaxRange);
            }
        }
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
            if (actionElapsed >= maxActionDuration) ForceEndAction();
        }

        // Recuo do Shift — velocidade real no Rigidbody2D, reaplicada todo frame enquanto durar
        // (base.Update() já zera rb.linearVelocity neste mesmo frame por não estar em "Walk" —
        // mesmo critério da Cambalhota do Rogue — então sobrescreve aqui, depois do base).
        if (recoilRemaining > 0f)
        {
            recoilRemaining -= Time.deltaTime;
            if (rb != null) rb.linearVelocity = recoilVelocity;
        }
    }

    // Rede de segurança genérica — se o Animation Event de fim nunca chegar (Primário/Ultimate/
    // Shift), força o fim da ação em vez de travar isAttacking pra sempre.
    private void ForceEndAction()
    {
        Debug.LogWarning("[Gunslinger] Animation Event de fim de ação nunca chegou — forçando fim (verifique o Animator Controller).");
        isAttacking = false;
        isAttackResolving = false;
        isUsingSecondaryAbility = false;
        activeAction = ActiveAction.None;
    }

    private void SetGunAimDirection(Vector2 dir)
    {
        if (animator == null) return;
        animator.SetFloat("GunAimX", dir.x);
        animator.SetFloat("GunAimY", dir.y);
    }

    // ===================== Primário =====================

    protected override void PrimaryAttack()
    {
        if (isAttacking) return;

        isAttacking = true;
        actionElapsed = 0f;
        activeAction = ActiveAction.Primary;
        shotsFiredThisAction = 0;
        shotsRequiredThisAction = shotCount;
        lastShotFireFrame = -1;

        SetGunAimDirection(RawAimDirection);
        AnimatorTrigger("AttackTrigger"); // AnyState -> Attack_Start (compartilhado com Ultimate/Shift)
    }

    // Direção de CADA tiro da rajada — desvio aleatório independente, em torno da mira travada
    // no início da ação. Com 1 tiro só, sempre reto.
    private Vector2 GetBurstShotDirection()
    {
        if (shotCount <= 1) return RawAimDirection;
        return RotateDegrees(RawAimDirection, RandomShotDeviation());
    }

    // ===================== Ultimate =====================

    protected override void UseUltimate()
    {
        isAttacking = true;
        actionElapsed = 0f;
        activeAction = ActiveAction.Ultimate;
        shotsFiredThisAction = 0;
        shotsRequiredThisAction = ultimateHitCount;
        lastShotFireFrame = -1;

        var firstTarget = FindNearestLivingEnemy();
        currentUltimateDirection = firstTarget != null
            ? ((Vector2)firstTarget.transform.position - (Vector2)transform.position).normalized
            : RawAimDirection; // sem monstro nenhum no raio — cai pra mira atual, mesmo assim toca a sequência
        SetGunAimDirection(currentUltimateDirection);

        AnimatorTrigger("UltimateTrigger"); // AnyState -> Attack_Start (compartilhado com Primário/Shift)
    }

    // Monstro vivo mais próximo dentro de ultimateSearchRadius — reavaliado a CADA tiro (não uma
    // lista fixa no início), pra sempre perseguir o mais próximo NO MOMENTO; naturalmente pode
    // repetir o mesmo alvo várias vezes se ele continuar sendo o mais próximo e ainda não tiver
    // morrido.
    private EnemyController FindNearestLivingEnemy()
    {
        var hits = Physics2D.OverlapCircleAll(transform.position, ultimateSearchRadius, enemyLayerMask);
        EnemyController nearest = null;
        float nearestSqrDistance = float.MaxValue;
        foreach (var hit in hits)
        {
            var enemy = hit.GetComponent<EnemyController>();
            if (enemy == null || HealthSystem.IsDead(enemy.stats.health)) continue;

            float sqrDistance = ((Vector2)enemy.transform.position - (Vector2)transform.position).sqrMagnitude;
            if (sqrDistance < nearestSqrDistance)
            {
                nearestSqrDistance = sqrDistance;
                nearest = enemy;
            }
        }
        return nearest;
    }

    private void FireUltimateHomingShot()
    {
        var target = FindNearestLivingEnemy();
        if (target == null) return; // sem monstro nenhum no raio — este tiro específico não acerta nada

        currentUltimateDirection = ((Vector2)target.transform.position - (Vector2)transform.position).normalized;
        SetGunAimDirection(currentUltimateDirection); // Attack_End vai terminar nesta direção, se for o último tiro

        target.TakeDamage(stats.damage * ultimateDamageMultiplier);

        if (impactVfxPrefab != null)
        {
            var vfx = Instantiate(impactVfxPrefab, target.transform.position, Quaternion.identity);
            Destroy(vfx, impactVfxDuration);
        }
    }

    // ===================== Secundária (Shift) — rajada de pelotas + recuo =====================

    protected override void UseSecondaryAbility()
    {
        isAttacking = true; // trava tudo durante start+shoot+end (mesmo padrão do shield bash do Paladin)
        actionElapsed = 0f;
        activeAction = ActiveAction.Shift;
        lastShotFireFrame = -1;

        SetGunAimDirection(RawAimDirection);
        AnimatorTrigger("SecondaryAbilityTrigger"); // AnyState -> Attack_Start (compartilhado com Primário/Ultimate)
    }

    private void FireShiftBurst()
    {
        Vector2 baseDirection = RawAimDirection;

        for (int i = 0; i < shiftPelletCount; i++)
        {
            Vector2 dir = shiftPelletCount <= 1 ? baseDirection : RotateDegrees(baseDirection, RandomShotDeviation());
            FireHitscanShot(dir, stats.damage * shiftDamageMultiplier);
        }

        // Impulso pra trás — velocidade real (não um Impulse único, que seria zerado no próximo
        // frame por base.Update()); ver UpdateRecoil em Update().
        recoilVelocity = -baseDirection * shiftRecoilSpeed;
        recoilRemaining = shiftRecoilDuration;
    }

    // ===================== Animation Events compartilhados (Primário + Ultimate + Shift) =====================

    // Chamado 1x por repetição do loop de Attack_Shoot (Primário/Ultimate) OU 1x só (Shift, sem
    // loop). Guard de frame necessário — Attack_Shoot ainda é um Blend Tree 2D de 8 direções,
    // então mirando entre 2 direções o Animator mistura 2 clipes e dispara o evento 2x no mesmo
    // frame (mesmo bug de sempre, ver Barbarian/Ranger/EnemyController).
    public void AnimationShotFireEvent()
    {
        if (Time.frameCount == lastShotFireFrame) return;
        lastShotFireFrame = Time.frameCount;

        switch (activeAction)
        {
            case ActiveAction.Primary:
                FireHitscanShot(GetBurstShotDirection(), stats.damage);
                shotsFiredThisAction++;
                if (shotsFiredThisAction >= shotsRequiredThisAction) AnimatorTrigger("AttackEndTrigger");
                break;

            case ActiveAction.Ultimate:
                FireUltimateHomingShot();
                shotsFiredThisAction++;
                if (shotsFiredThisAction >= shotsRequiredThisAction) AnimatorTrigger("AttackEndTrigger");
                break;

            case ActiveAction.Shift:
                FireShiftBurst();
                AnimatorTrigger("AttackEndTrigger"); // sempre 1 tiro só — corta o loop na hora
                break;
        }
    }

    // Chamado no fim do clipe attack_end — fecha a ação atual, seja ela qual for.
    public void AnimationAttackEndEvent()
    {
        isAttacking = false;
        isAttackResolving = false; // só tem efeito real se activeAction era Primary — nos outros já estava false (Seção 53 do GDD)
        if (activeAction == ActiveAction.Shift) isUsingSecondaryAbility = false; // arma o cooldown no próximo Update() da base
        activeAction = ActiveAction.None;
    }

    private static readonly RaycastHit2D[] HitscanBuffer = new RaycastHit2D[8];
    private static readonly ContactFilter2D HitscanFilter = new ContactFilter2D
    {
        useTriggers = true, // essencial — ContactFilter2D novo nasce com isso false, e o
                             // collider "trigger genérico maior" de todo EnemyController
                             // (ver EnemyController.cs, CollectDistinct) só é detectado se isso
                             // estiver true (Physics2D.queriesHitTriggers global é só o default
                             // do Raycast de 1 resultado só — ContactFilter2D tem o PRÓPRIO flag,
                             // sobrescreve o global, e nasce false)
        useLayerMask = true,
    };

    // ===================== Hitscan compartilhado (Primário + Shift) =====================

    // Raycast único, sem perfuração — para no primeiro MONSTRO DE VERDADE ou no fim da linha.
    // Sem knockback em nenhum dos dois casos. A Ultimate NÃO usa mais isso — sempre acerta via
    // FireUltimateHomingShot (sem raycast, sem chance de errar).
    //
    // Usa RaycastAll (via ContactFilter2D + buffer) em vez do Raycast de 1 resultado só: todo
    // monstro tem, no MESMO layer "Enemy", os próprios AttackHitbox_N/S/E/W/NE/etc. — hitboxes
    // Untagged do ATAQUE do monstro, sem EnemyController nenhum. Os resultados já vêm ordenados
    // por distância — o primeiro collider marcado "Enemy" é o alvo de verdade.
    private void FireHitscanShot(Vector2 direction, float damage)
    {
        Vector2 origin = transform.position;
        ContactFilter2D filter = HitscanFilter;
        filter.SetLayerMask(enemyLayerMask);
        int count = Physics2D.Raycast(origin, direction, filter, HitscanBuffer, hitscanMaxRange);

        EnemyController enemy = null;
        Vector2 hitPoint = Vector2.zero;
        for (int i = 0; i < count; i++)
        {
            if (!HitscanBuffer[i].collider.CompareTag("Enemy")) continue;
            enemy = HitscanBuffer[i].collider.GetComponent<EnemyController>();
            hitPoint = HitscanBuffer[i].point;
            break;
        }

        Vector3 impactPoint;
        if (enemy != null)
        {
            enemy.TakeDamage(damage);
            impactPoint = hitPoint;
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

    // Desvio angular aleatório, independente por tiro. Magnitude sorteada entre
    // MinShotDeviationAngle (nunca 0 — sempre algum desvio) e shotMaxDeviationAngle (teto
    // ajustável, calibrado pelo Gizmo); sinal sorteado 50/50 entre esquerda e direita da mira.
    // Compartilhado entre o Primário e o Shift.
    private float RandomShotDeviation()
    {
        float magnitude = Random.Range(MinShotDeviationAngle, shotMaxDeviationAngle);
        return Random.value < 0.5f ? magnitude : -magnitude;
    }

    private static Vector2 RotateDegrees(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }
}
