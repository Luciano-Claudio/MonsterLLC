using UnityEngine;

// Movimento real só pode acontecer quando o Animator já entrou de fato no estado
// correspondente — nunca no frame em que só a intenção (ex.: "IsMoving = true") foi
// marcada. Sem isso, uma transição com Exit Time (ex.: Idle -> Walk, que espera o clipe
// de patrulha terminar) deixa a entidade "deslizando" pela tela ainda com a pose de Idle,
// porque o código já move o transform antes do Animator visualmente alcançar o Walk.
// Vale pra qualquer entidade com Animator — monstro ou herói.
public static class AnimatorStateCheck
{
    // Sem Animator (prefab placeholder, ou herói ainda sem animação wireada), não há
    // estado nenhum pra esperar — sempre libera o movimento nesse caso.
    public static bool IsInState(Animator animator, string stateName)
    {
        if (animator == null) return true;
        return animator.GetCurrentAnimatorStateInfo(0).IsName(stateName);
    }
}
