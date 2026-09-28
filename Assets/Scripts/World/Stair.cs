using UnityEngine;
using EasyTransition;

public class Stair : Interactable
{
    public FloorDefinition ownerFloor;
    public bool goesUp = true;

    // Filho desta própria escada, posicionado à mão no level design — geralmente na frente
    // da escada/buraco correspondente no Floor de destino. O rastreio de "qual Floor está
    // ativo" continua vindo do FloorRegistry/FloorManager (target abaixo); isso só decide
    // pra ONDE o player vai, não afeta a lógica de qual Floor passa a ser o atual.
    public Transform arrivalPoint;

    // Mesmo mecanismo do respawn do HeroController — transição rápida própria (não a de
    // respawn, essa é mais acelerada) só pra cobrir o instante do teleporte e evitar o
    // "salto" visível da câmera seguindo o player.
    public TransitionSettings stairTransition;

    // Trava simples contra apertar interagir de novo (mesma escada, ou outra) enquanto a
    // transição em andamento ainda não terminou — o EasyTransition não suporta duas
    // transições concorrentes (mesma observação já registrada em HeroController.OnDeath).
    private static bool transitioning;

    public override void Interact(Transform interactor)
    {
        if (transitioning) return;

        FloorDefinition target = goesUp
            ? FloorRegistry.Instance.GetNextFloor(ownerFloor)
            : FloorRegistry.Instance.GetPreviousFloor(ownerFloor);

        if (target == null)
        {
            Debug.Log("[Stair] Sem destino — limite da torre.");
            return;
        }

        if (stairTransition == null)
        {
            TeleportTo(interactor, target);
            return;
        }

        transitioning = true;

        // Só libera de novo quando a transição termina de descobrir a tela (onTransitionEnd),
        // não já no cut point — a animação de "destapar" ainda está tocando nesse meio tempo,
        // e o EasyTransition não aceita uma segunda Transition() por cima dessa.
        void ReleaseLock()
        {
            TransitionManager.Instance().onTransitionEnd -= ReleaseLock;
            transitioning = false;
        }
        TransitionManager.Instance().onTransitionEnd += ReleaseLock;

        // Teleporte acontece dentro do callback, no momento em que a transição já cobriu a
        // tela por completo (mesmo padrão de HeroController.Respawn) — a câmera nunca chega
        // a mostrar o salto de posição.
        TransitionHelper.PlayTransition(stairTransition, () => TeleportTo(interactor, target));
    }

    private void TeleportTo(Transform interactor, FloorDefinition target)
    {
        if (arrivalPoint != null)
        {
            interactor.position = arrivalPoint.position;
        }
        else
        {
            Debug.LogWarning("[Stair] Sem arrivalPoint configurado — caindo no pivô do Floor de destino.");
            interactor.position = target.transform.position;
        }

        FloorManager.Instance.SetCurrentFloor(target);
    }
}
