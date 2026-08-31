using UnityEngine;

public class Stair : Interactable
{
    public FloorDefinition ownerFloor;
    public bool goesUp = true;

    public override void Interact(Transform interactor)
    {
        FloorDefinition target = goesUp
            ? FloorRegistry.Instance.GetNextFloor(ownerFloor)
            : FloorRegistry.Instance.GetPreviousFloor(ownerFloor);

        if (target == null)
        {
            Debug.Log("[Stair] Sem destino — limite da torre.");
            return;
        }

        interactor.position = target.transform.position;
        FloorManager.Instance.SetCurrentFloor(target);
    }
}
