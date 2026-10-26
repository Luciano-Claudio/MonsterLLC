using System.Collections.Generic;
using UnityEngine;

// Dark Channeler (Bestiário Andar 4 Bosses) — Ranged/Suporte especial: não lança projétil e
// não causa dano direto (ExecuteAttackHit fica vazio). O "ataque" é um cast que, no frame
// certo, escolhe os Acolyte/Hound/Zealot elegíveis (ainda não possuídos) mais próximos e
// dispara a transformação de cada um (PossessableMeleeEnemyController.TriggerPossession()) —
// cada alvo afetado toca sua PRÓPRIA animação `possession` depois, a troca de sprite/stats de
// verdade acontece lá (AnimationPossessionEndEvent), não aqui.
// Cooldown reaproveita o pipeline padrão de qualquer Ranged (stats.attackAnimationCooldown via
// TryStartAttackAnimation, herdado de EnemyController) — só InAttackRange() muda de critério:
// em vez de medir distância do player, só libera o cast quando existe pelo menos 1 alvo
// elegível no alcance (reflete "sempre que a transformação estiver disponível, prioriza...").
// Movimento (2026-10-26, decisão do usuário): não sobrescreve mais Move() — o "puxão" extra
// em direção aos aliados elegíveis (bias somado ao vetor aproximar/fugir) deixava a
// movimentação com cara de indecisa mesmo com histerese. Agora é um Ranged comum de verdade,
// herdado sem alteração de RangedEnemyController.Move(): foge se o player chegar perto, para
// na distância que deseja, pronto.
public class DarkChannelerController : RangedEnemyController
{
    [Header("Transformação (Bestiário — 2 Acolyte/Hound/Zealot mais próximos)")]
    [SerializeField] private float allySeekRadius = 10f;
    [SerializeField] private int maxTransformTargets = 2;

    private List<PossessableMeleeEnemyController> FindEligibleTargets()
    {
        var result = new List<PossessableMeleeEnemyController>();
        foreach (var ally in FindObjectsByType<PossessableMeleeEnemyController>(FindObjectsSortMode.None))
        {
            if (ally == null || ally.IsPossessed) continue;
            if (Vector2.Distance(transform.position, ally.transform.position) <= allySeekRadius)
                result.Add(ally);
        }
        return result;
    }

    protected override bool InAttackRange() => FindEligibleTargets().Count > 0;

    protected override void ExecuteAttackHit() { } // sem dano direto — ver AnimationCastTransformEvent

    // Animation Event, no frame certo do cast (clipes attack_ne/nw/se/sw) — dispara a
    // transformação de verdade, nos N mais próximos entre os alvos elegíveis no momento.
    public void AnimationCastTransformEvent()
    {
        var targets = FindEligibleTargets();
        targets.Sort((a, b) =>
            Vector2.Distance(transform.position, a.transform.position)
                .CompareTo(Vector2.Distance(transform.position, b.transform.position)));

        for (int i = 0; i < targets.Count && i < maxTransformTargets; i++)
            targets[i].TriggerPossession();
    }
}
