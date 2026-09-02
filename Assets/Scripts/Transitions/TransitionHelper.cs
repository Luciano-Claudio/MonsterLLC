using System;
using EasyTransition;

public static class TransitionHelper
{
    // Toca uma transição sem troca de Scene (o jogo é Scene única — GDD Seção 24) e chama
    // onCutPoint uma única vez, no momento em que a tela está totalmente coberta.
    public static void PlayTransition(TransitionSettings settings, Action onCutPoint)
    {
        void Handler()
        {
            TransitionManager.Instance().onTransitionCutPointReached -= Handler;
            onCutPoint?.Invoke();
        }

        TransitionManager.Instance().onTransitionCutPointReached += Handler;
        TransitionManager.Instance().Transition(settings, 0f);
    }
}
