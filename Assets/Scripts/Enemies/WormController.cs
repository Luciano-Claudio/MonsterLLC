using UnityEngine;

// Worm (Bestiário Andar 9): Melee comum em quase tudo (idle/walk/idleCombat/damage/die
// normais, 4 triggers — aqui capsules, não circles, igual ao Undead_Knight — e
// orthogonalAttack=true no Inspector, mesmo mecanismo já usado pro Undead_Knight/Acolyte:
// os 4 campos attackHitboxNE/NW/SE/SW guardam os triggers de S/N/E/W, GetHitboxForFacing()
// já escolhe certo sozinho). A diferença real é só o ataque, que vira um dash: attack_start
// conecta o golpe normal (Animation Event no último frame = o AnimationHitEvent de sempre,
// golpe "atravessa" o player através do capsule comprido) e, no mesmo instante, o Worm anda
// alguns blocos na direção em que atacou — igual ao teleporte do Assassin (Shift), só que
// mais curto e sempre na direção do próprio golpe, não na mira livre do jogador. attack_end
// termina a sequência já na posição nova (ExitTime=1 a partir do attack_start, sem código
// extra pra essa troca de clipe — só a reposição em si precisa de código).
public class WormController : MeleeEnemyController
{
    [Header("Dash do ataque (anda alguns blocos na direção do golpe)")]
    [SerializeField] private float dashDistance = 2f; // 🔢 ajustável — "não vai muito longe"

    protected override void ExecuteAttackHit()
    {
        base.ExecuteAttackHit(); // golpe normal (dano só se o player estiver no capsule certo)

        Vector2 dashDir = DirectionUtility.SnapTo4Cardinals(AimDirection);
        Vector3 destination = transform.position + (Vector3)(dashDir * dashDistance);

        // ai.Teleport() mantém o estado interno do AIPath em sincronia (posição real,
        // histórico de movimento) — diferente de só escrever transform.position, que o
        // próprio AIPath sobrescreveria de volta no frame seguinte (ele é quem manda na
        // posição enquanto updatePosition=true).
        if (ai != null) ai.Teleport(destination);
        else transform.position = destination;
    }
}
