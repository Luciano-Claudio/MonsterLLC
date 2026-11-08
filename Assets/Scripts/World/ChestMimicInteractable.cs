using UnityEngine;

// Componente irmão do ChestMimicController no mesmo GameObject (não dá pra herdar os dois —
// EnemyController e Interactable são classes separadas). Interact() só precisa ligar
// isInCombat/GlobalAggroActive (ForceImmediateAggro, já público em EnemyController) — a partir
// daí, staysDormantUntilDetected + a transição "InCombat" do Animator cuidam do resto (saem do
// Idle dormente pro Activate sozinhos, ver comentário em ChestMimicController).
public class ChestMimicInteractable : Interactable
{
    [SerializeField] private ChestMimicController mimic;

    public override void Interact(Transform interactor)
    {
        InteractionManager.Instance.ClearAvailable(this);
        enabled = false;
        mimic.ForceImmediateAggro();
    }
}
