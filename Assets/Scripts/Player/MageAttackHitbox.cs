using System.Collections.Generic;
using UnityEngine;

// GameObject filho do Mage (girado pelo Mage.cs no Update(), não por si próprio) — trigger
// retangular fixo, sempre grudado no Mage, que só existe pra guardar o dano pendente e
// aplicá-lo no instante certo da PRÓPRIA animação de fogo saindo (GDD Seção 17.3). Não viaja
// (ao contrário de HeroProjectile/RangerArrow) — é sempre a mesma instância, reaproveitada a
// cada ataque.
//
// Esquerda #20/#24/#25 (Queimadura Residual/Eco Flamejante/Impacto Explosivo, perks) recebidos
// aqui mesmo, não no Mage — mesmo critério da RangerArrow (que já recebe bleedChance/boomerang/
// etc. como parâmetros de Launch()): quem aplica o hit já tem a lista de alvos e o status de
// cada um ali mesmo, sem precisar expor isso de volta pro Mage.
public class MageAttackHitbox : MonoBehaviour
{
    [SerializeField] private Collider2D hitbox;
    private Animator animator;
    private float pendingDamage;
    private bool hitFired;
    private readonly List<EnemyController> hitTargets = new();
    private readonly List<EnemyController> echoCandidates = new();

    private float residualBurnChancePercent;
    private float burnDamagePerSecond;
    private float burnDuration;
    private float explosiveImpactPercent;
    private float echoChancePercent;

    // Raio de busca do alvo extra do Eco Flamejante / da área da explosão do Impacto
    // Explosivo — nenhum dos 2 perks define o próprio raio (GDD só fala em % de chance/dano),
    // então usa um valor fixo modesto, coerente com o tamanho do hitbox normal do primário.
    private const float EchoSearchRadius = 2f;
    private const float ExplosiveImpactRadius = 1.5f;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    // actionSpeedMultiplier vem do Mage (upgrade de velocidade de ação) — Animator próprio,
    // separado do corpo do Mage, então o SetFloat do HeroController não chega aqui sozinho;
    // repassado a cada Fire() pra sempre refletir o valor atual, sem depender de ordem de
    // Awake() entre os dois GameObjects.
    public void Fire(float damage, float actionSpeedMultiplier, float residualBurnChance = 0f, float burnDps = 0f, float burnDur = 0f, float explosiveImpactPct = 0f, float echoChance = 0f)
    {
        pendingDamage = damage;
        hitFired = false;
        residualBurnChancePercent = residualBurnChance;
        burnDamagePerSecond = burnDps;
        burnDuration = burnDur;
        explosiveImpactPercent = explosiveImpactPct;
        echoChancePercent = echoChance;

        if (animator != null)
        {
            animator.SetFloat("ActionSpeedMultiplier", actionSpeedMultiplier);
            animator.SetTrigger("FireTrigger");
        }
    }

    // Esquerda #22 "Rajada Dupla" (perk) — 2º hit manual chamado direto pelo Mage (sem passar
    // pelo Animator de novo), mesmos parâmetros de perk já guardados do Fire() original desta
    // mesma ação (residual burn/eco/impacto explosivo valem igual nos 2 hits).
    public void FireDirectHit()
    {
        if (hitbox == null) return;
        var results = new Collider2D[128];
        int count = hitbox.Overlap(ContactFilter2D.noFilter, results);
        EnemyController.CollectDistinct(results, count, hitTargets);
        ApplyHitToTargets(hitTargets);
    }

    // Animation Event, no instante exato em que o fogo sai de verdade (dentro do clipe deste
    // próprio filho, não do Mage).
    public void AnimationFireHitEvent()
    {
        if (hitFired) return;
        hitFired = true;
        if (hitbox == null) return;

        // 128 (não 16) — monstro tem 2 Collider2D, 16 saturava com só 8 monstros aglomerados
        // perto do player, deixando o resto fora do hit sem erro nenhum (bug real, 2026-10-26).
        var results = new Collider2D[128];
        int count = hitbox.Overlap(ContactFilter2D.noFilter, results);
        // Dedup obrigatório — todo monstro tem 2 Collider2D no mesmo GameObject (corpo +
        // trigger genérico), então Overlap() sem isso acertava o mesmo monstro 2x (bug real:
        // dano dobrado no primário do Mage). Ver EnemyController.CollectDistinct().
        EnemyController.CollectDistinct(results, count, hitTargets);
        ApplyHitToTargets(hitTargets);
    }

    private void ApplyHitToTargets(List<EnemyController> targets)
    {
        bool rolledEcho = echoChancePercent > 0f && Random.Range(0f, 100f) < echoChancePercent;
        Vector3 firstTargetPos = targets.Count > 0 ? targets[0].transform.position : transform.position;

        foreach (var enemy in targets) ApplyHitToSingleTarget(enemy);

        // Eco Flamejante — só depois do(s) alvo(s) normal(is), pra não contar o eco como alvo
        // "normal" de novo caso ele mesmo esteja perto o bastante de si próprio.
        if (rolledEcho && targets.Count > 0)
        {
            var extra = FindEchoTarget(firstTargetPos, targets);
            if (extra != null) ApplyHitToSingleTarget(extra);
        }
    }

    private void ApplyHitToSingleTarget(EnemyController enemy)
    {
        var statusEffects = enemy.GetComponent<StatusEffectController>();
        // Precisa saber se já estava queimando ANTES deste hit — Impacto Explosivo só conta
        // o status que já estava rolando, não o que a Queimadura Residual acabou de aplicar
        // no mesmo golpe (senão todo kill com os 2 perks comprados explodiria sempre).
        bool wasBurning = statusEffects != null && statusEffects.IsEffectActive(StatusEffectType.Fire);

        enemy.TakeDamage(pendingDamage);

        if (explosiveImpactPercent > 0f && wasBurning && enemy.IsDead)
        {
            float explosionDamage = pendingDamage * explosiveImpactPercent / 100f;
            ApplyExplosiveImpact(enemy.transform.position, explosionDamage);
        }

        if (residualBurnChancePercent > 0f && statusEffects != null && Random.Range(0f, 100f) < residualBurnChancePercent)
            statusEffects.ApplyStatusEffect(StatusEffectType.Fire, burnDuration, burnDamagePerSecond);
    }

    private void ApplyExplosiveImpact(Vector3 origin, float damage)
    {
        var results = Physics2D.OverlapCircleAll(origin, ExplosiveImpactRadius);
        EnemyController.CollectDistinct(results, results.Length, echoCandidates);
        foreach (var enemy in echoCandidates) enemy.TakeDamage(damage, isDirectPlayerHit: false);
    }

    // Procura o inimigo vivo mais próximo do 1º alvo, dentro de EchoSearchRadius, que NÃO
    // esteja já na lista de alvos normais deste hit (senão "alvo extra" vira só dano dobrado
    // no mesmo monstro, não um 2º monstro).
    private EnemyController FindEchoTarget(Vector3 origin, List<EnemyController> alreadyHit)
    {
        var results = Physics2D.OverlapCircleAll(origin, EchoSearchRadius);
        EnemyController.CollectDistinct(results, results.Length, echoCandidates);

        EnemyController closest = null;
        float closestDist = float.MaxValue;
        foreach (var candidate in echoCandidates)
        {
            if (alreadyHit.Contains(candidate)) continue;
            float dist = Vector2.Distance(origin, candidate.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = candidate;
            }
        }
        return closest;
    }
}
