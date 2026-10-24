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
public class DarkChannelerController : RangedEnemyController
{
    [Header("Transformação (Bestiário — 2 Acolyte/Hound/Zealot mais próximos)")]
    [SerializeField] private float allySeekRadius = 10f;
    [SerializeField] private int maxTransformTargets = 2;
    [SerializeField] private float allySeekWeight = 1f; // 🔢 ajustável — peso do "puxão" pros aliados em relação a manter distância do player

    // Mantém a distância do player (igual Ranged comum), mas com um puxão extra pra ficar
    // perto de Acolyte/Hound/Zealot elegíveis — "procura permanecer próximo de... que possam
    // ser transformados" (Bestiário).
    protected override void Move(float deltaTime)
    {
        Vector2 toPlayer = player.position - transform.position;
        float distance = toPlayer.magnitude;
        Vector2 allyBias = ComputeAllySeekBias();

        if (distance > stats.attackRadius)
        {
            SetMoving(true);
            MoveInDirection((toPlayer + allyBias).normalized);
        }
        else if (distance < stats.attackRadius * 0.8f)
        {
            SetMoving(true);
            MoveInDirection((-toPlayer + allyBias).normalized);
        }
        else if (allyBias.sqrMagnitude > 0.0001f)
        {
            SetMoving(true);
            MoveInDirection(allyBias.normalized);
        }
        else
        {
            SetMoving(false); // dentro do alcance ideal e sem aliado pra buscar -> idle_combat
        }
    }

    private Vector2 ComputeAllySeekBias()
    {
        Vector2 sum = Vector2.zero;
        int count = 0;
        foreach (var ally in FindObjectsByType<PossessableMeleeEnemyController>(FindObjectsSortMode.None))
        {
            if (ally == null || ally.IsPossessed) continue;
            Vector2 offset = (Vector2)ally.transform.position - (Vector2)transform.position;
            if (offset.sqrMagnitude > allySeekRadius * allySeekRadius) continue;
            sum += offset;
            count++;
        }
        return count == 0 ? Vector2.zero : (sum / count).normalized * allySeekWeight;
    }

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
