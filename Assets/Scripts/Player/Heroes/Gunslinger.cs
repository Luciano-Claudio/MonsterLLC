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
    // critério de hammerCount/bladeCount/arrowCount (Seção 0, item 8 do breakdown). Teto de
    // código em 15 (Animator Controller tem Attack_1..Attack_15 prontos) — bem acima do Tier
    // máximo da GDD (6) de propósito, pra já existir margem pronta se um upgrade futuro quebrar
    // esse teto sem precisar gerar mais animação depois.
    [SerializeField] private int shotCount = 1; // 🔢 upgradable — 1 a 15 (ver OnValidate)

    // Cone de imprecisão (redesenhado a pedido do usuário) — cada tiro da rajada sorteia o
    // PRÓPRIO desvio, independente dos outros: magnitude entre MinShotDeviationAngle (nunca
    // exatamente 0 — sempre um pouco impreciso) e shotMaxDeviationAngle (o teto que o usuário
    // calibra olhando o Gizmo), sinal (esquerda/direita da mira) sorteado 50/50. Só entra em
    // jogo com shotCount > 1 — com 1 tiro só, sai sempre reto (sem rajada não existe o que
    // comparar/espalhar). shotMaxDeviationAngle é o desvio de CADA tiro, não o espalhamento
    // total: o ângulo que o usuário de fato vê entre as 2 linhas mais distantes entre si (o
    // que ele decide olhando o Gizmo) pode chegar a 2x esse valor, no caso extremo de um tiro
    // sair em +max e outro em -max.
    [SerializeField] private float shotMaxDeviationAngle = 15f; // 🔢 ajustável — teto do desvio por tiro
    private const float MinShotDeviationAngle = 0.1f;

    [Header("Debug — linhas de tiro no Gizmo (Scene, objeto selecionado), não afeta gameplay")]
    [SerializeField] private bool showShotLinesGizmo = false;

    // Mesma rede de segurança do Barbarian/Paladin — Animation Event de fim nunca disparar não
    // pode travar o herói pra sempre em isAttacking.
    [SerializeField] private float maxActionDuration = 3f; // 🔢 ajustável
    private float actionElapsed;

    // Guarda contra o disparo duplo do Blend Tree 2D (2 clipes com peso > 0 no mesmo frame) —
    // diferente do bool "já disparei" de outros heróis, porque aqui o MESMO evento precisa
    // disparar várias vezes (1 por tiro da rajada), só nunca 2x no mesmo frame (Seção 0, item 3
    // do breakdown — parte mais nova/arriscada desta sprint, validar em Play Mode).
    private int lastShotFireFrame = -1;

    // ===================== Ultimate — giro disparando nas 8 direções =====================
    [Header("Ultimate — giro, 8 tiros Hitscan fixos")]
    [SerializeField] private float ultimateDamageMultiplier = 1f; // 🔢 GDD não especifica bônus — placeholder neutro (Seção 0, item 10)

    // Pedido do usuário — a animação da Ultimate só tem 1 frame de disparo por direção, mas
    // cada Animation Event (AnimationShoot_N/NE/etc.) solta uma RAJADA instantânea de N tiros
    // nessa direção (mesmo critério de desvio do primário — RandomShotDeviation, reaproveitado
    // tal e qual), não 1 tiro só. É uma forma de "fingir" que a arma da Ultimate é mais forte
    // sem precisar desenhar mais frames: visualmente é 1 disparo, mas o resultado é como se
    // fossem shotCount = ultimateShotsPerDirection tiros do attack normal, instantâneos, em
    // cada uma das 8 direções fixas.
    [SerializeField] private int ultimateShotsPerDirection = 15; // 🔢 ajustável

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
    // 17.8 explícita. SEM a imunidade a dano que o Paladin tem (Seção 0, item 7 do breakdown).
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
        shotCount = Mathf.Clamp(shotCount, 1, 15);
        shotMaxDeviationAngle = Mathf.Max(shotMaxDeviationAngle, MinShotDeviationAngle);
    }

    // Gizmo de debug — vermelho: 1 linha por tiro da rajada do primário (nº = shotCount)
    // saindo da posição atual na direção da mira; amarelo: ultimateShotsPerDirection linhas
    // por direção fixa da Ultimate (nº = EightDirections.Length × ultimateShotsPerDirection).
    // As duas usam a MESMA fórmula de desvio (RandomShotDeviation) e o MESMO hitscanMaxRange
    // do real FireHitscanShot/FireUltimateShot — é literalmente o cálculo real, não uma cópia
    // aproximada. Sorteia de novo a cada repaint da Scene view, de propósito: olhando por
    // alguns segundos dá pra ver o espalhamento completo entre as linhas mais distantes e
    // calibrar shotMaxDeviationAngle por cima disso.
    private void OnDrawGizmosSelected()
    {
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

        Gizmos.color = Color.yellow;
        foreach (Vector2 baseDir in EightDirections)
        {
            for (int i = 0; i < ultimateShotsPerDirection; i++)
            {
                Vector2 dir = RotateDegrees(baseDir, RandomShotDeviation());
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
        if (animator != null)
        {
            // ShotCount decide qual Attack_N o AnyState resolve (ver Gunslinger.controller) —
            // precisa ser setado ANTES do AttackTrigger, já que os dois são lidos juntos na
            // próxima avaliação do Animator.
            animator.SetInteger("ShotCount", shotCount);
            animator.SetBool("IsOrthogonalAim", IsAimOrthogonal());
        }
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

    // Desvio angular aleatório, independente por tiro — Seção 0, item 0. Com 1 tiro só, sempre
    // reto (sem rajada não existe imprecisão pra calcular). RawAimDirection (mira exata, não
    // travada nas 8 direções), mesmo critério do voo da flecha do Ranger
    // (Ranger.AnimationShootEvent) — AimDirection é só pra pose do Animator; usar ela aqui
    // fazia o raycast só acertar quando o monstro estivesse exatamente alinhado a um dos 8
    // ângulos fixos, errando "no range certo" sempre que a mira real estivesse num ângulo
    // intermediário (bug relatado em teste).
    private Vector2 GetBurstShotDirection()
    {
        if (shotCount <= 1) return RawAimDirection;
        return RotateDegrees(RawAimDirection, RandomShotDeviation());
    }

    // Magnitude sorteada entre MinShotDeviationAngle (nunca 0 — sempre algum desvio) e
    // shotMaxDeviationAngle (teto ajustável, calibrado pelo Gizmo — ver OnDrawGizmosSelected);
    // sinal sorteado 50/50 entre esquerda e direita da mira.
    private float RandomShotDeviation()
    {
        float magnitude = Random.Range(MinShotDeviationAngle, shotMaxDeviationAngle);
        return Random.value < 0.5f ? magnitude : -magnitude;
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
        float damage = stats.damage * ultimateDamageMultiplier;
        for (int i = 0; i < ultimateShotsPerDirection; i++)
        {
            Vector2 dir = RotateDegrees(direction, RandomShotDeviation());
            FireHitscanShot(dir, damage);
        }
    }

    // Animation Event, no fim do clipe de giro.
    public void AnimationUltimateEndEvent()
    {
        isAttacking = false;
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

    // ===================== Hitscan compartilhado (primário + ultimate) =====================

    // Raycast único, sem perfuração — para no primeiro MONSTRO DE VERDADE ou no fim da linha
    // (Seção 0, item 4). Sem knockback em nenhum dos dois casos (Seção 0, item 6).
    //
    // Usa RaycastAll (via ContactFilter2D + buffer) em vez do Raycast de 1 resultado só: todo
    // monstro tem, no MESMO layer "Enemy", os próprios AttackHitbox_N/S/E/W/NE/etc. — hitboxes
    // Untagged do ATAQUE do monstro (ver Goblin.prefab e similares), sem EnemyController
    // nenhum. Com monstro em melee, essas hitboxes costumam estar bem na frente dele, exatamente
    // no caminho do raycast — Raycast de 1 resultado só parava ali (sem GetComponent<EnemyController>,
    // sem dano), mas o VFX de impacto ainda nascia em cima do hit.point, perto o bastante do
    // monstro pra "parecer" que acertou (bug relatado em teste: "vfx sai nos pés do monstro, mas
    // não dá dano"). Os resultados já vêm ordenados por distância — o primeiro collider marcado
    // "Enemy" (mesmo critério de EnemyController.CollectDistinct) é o alvo de verdade.
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
