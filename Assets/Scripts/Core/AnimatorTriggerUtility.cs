using UnityEngine;

// Trigger do Animator que fica armado (SetTrigger sem a transição consumir no mesmo update)
// sobrevive e dispara depois, fora de hora. Bug real (2026-11-19): o herói morreu com um
// AttackTrigger armado; o DieTrigger venceu primeiro (die vem antes de Attack na lista do
// AnyState do Mage/Ranger), mas no frame seguinte o AttackTrigger levou AnyState -> Attack e
// tirou o herói do die — a animação de morte nunca terminou (respawn só pelo timeout).
public static class AnimatorTriggerUtility
{
    // Limpa TODO parâmetro Trigger do controller atual (ex.: antes do DieTrigger, que precisa
    // ser o único armado). animator.parameters aloca — só pra eventos raros, nunca por frame.
    public static void ResetAllTriggers(Animator animator)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return;
        foreach (var parameter in animator.parameters)
            if (parameter.type == AnimatorControllerParameterType.Trigger)
                animator.ResetTrigger(parameter.nameHash);
    }
}
