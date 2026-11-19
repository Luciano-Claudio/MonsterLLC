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

    // Estado com loop acumula normalizedTime (3.25 = 4º ciclo, 25% dele) — Animator.Play
    // recebe só a fração; sem loop, o valor já está entre 0 e 1.
    public static float ContinueTime(float normalizedTime, bool looping) =>
        looping ? normalizedTime - Mathf.Floor(normalizedTime) : normalizedTime;
}
