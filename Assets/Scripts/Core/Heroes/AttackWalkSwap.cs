using UnityEngine;

public enum AttackWalkAction { None, ToWalk, ToStand }

// D9.1 (Atacar andando) — decide quando trocar entre o estado de ataque parado (ex.: "Attack")
// e o gêmeo andando ("Attack_Walk"), sem tocar no Animator (quem chama faz o Animator.Play).
// As duas animações têm o mesmo nº de frames e os eventos nos mesmos frames, então a troca
// continua no MESMO normalizedTime — começar a andar no frame 3 segue do frame 4 no walk.
public static class AttackWalkSwap
{
    // Clipe sem loop com normalizedTime >= 1 já terminou e só espera a transição de saída —
    // trocar ali reiniciaria o golpe (ou dispararia o evento de fim de novo), então não troca.
    public static AttackWalkAction Decide(bool wantsToMove, bool trapped, bool actionAllowsWalk,
        bool inStandState, bool inWalkTwin, bool counterpartExists, float normalizedTime, bool looping)
    {
        if (trapped || !actionAllowsWalk || !counterpartExists) return AttackWalkAction.None;
        if (!looping && normalizedTime >= 1f) return AttackWalkAction.None;
        if (wantsToMove && inStandState) return AttackWalkAction.ToWalk;
        if (!wantsToMove && inWalkTwin) return AttackWalkAction.ToStand;
        return AttackWalkAction.None;
    }

    // Mira travada durante qualquer ação (regra do Bestiário, Seção 22) e na Prisão — exceto no
    // ataque PRIMÁRIO de herói que permite mirar enquanto ataca (Mage: o fogo segue o mouse e o
    // corpo vira junto, no mesmo frame da BlendTree). isAttackResolving só é true no primário
    // (Ultimate/Shift usam só isAttacking), então elas continuam travando.
    public static bool AimLocked(bool isAttacking, bool isAttackResolving, bool canAimDuringPrimaryAttack, bool trapped)
    {
        if (trapped) return true;
        if (!isAttacking) return false;
        return !(isAttackResolving && canAimDuringPrimaryAttack);
    }

    // O primário só recomeça quando o anterior terminou de VERDADE: isAttackResolving fica true
    // do clique até o fim real (é o que deixa a UI vermelha). Bug real (Rogue, 2026-11-20): o
    // Rogue solta isAttacking durante o pulso (pra andar) e o clique passava no meio dele.
    public static bool CanStartPrimary(bool isAttacking, bool isAttackResolving, bool trapped) =>
        !isAttacking && !isAttackResolving && !trapped;

    // Movimento do herói: andando (estado "Walk") ou num estado de ataque andável — e NUNCA
    // preso. Revisão final do D9.1: controller sem estado Trapped (urso do Druid, Paladin)
    // deixava o herói em "Walk" durante a Prisão e ele andava livre.
    public static bool CanMove(bool wantsToMove, bool inWalkState, bool attackMoveAllowed, bool trapped) =>
        wantsToMove && !trapped && (inWalkState || attackMoveAllowed);

    // Prisão encerra o primário (isAttackResolving false) — exceto quando ele continua rodando
    // sozinho, sem o herói: o pulso do Rogue segue ativo durante a Prisão, e zerar aqui reabria
    // o clique no meio dele (achado da revisão final do D9.1).
    public static bool ClearsResolvingOnTrap(bool primaryStillRunning) => !primaryStillRunning;

    // Estado com loop acumula normalizedTime (3.25 = 4º ciclo, 25% dele) — Animator.Play
    // recebe só a fração; sem loop, o valor já está entre 0 e 1.
    public static float ContinueTime(float normalizedTime, bool looping) =>
        looping ? normalizedTime - Mathf.Floor(normalizedTime) : normalizedTime;
}
