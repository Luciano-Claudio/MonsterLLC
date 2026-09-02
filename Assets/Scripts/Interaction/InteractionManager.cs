using UnityEngine;

public class InteractionManager : MonoBehaviour
{
    public static InteractionManager Instance { get; private set; }

    private Interactable currentInteractable;
    private Transform currentInteractor;
    private PlayerControls controls;

    private void Awake()
    {
        Instance = this;
        controls = new PlayerControls();
        controls.Gameplay.Interact.performed += ctx => TryInteract();
    }

    private void OnEnable() => controls.Enable();
    private void OnDisable() => controls.Disable();

    public void SetAvailable(Interactable interactable, Transform interactor)
    {
        currentInteractable = interactable;
        currentInteractor = interactor;
        GameEvents.InteractPromptChanged(interactable.iconAnchor != null ? interactable.iconAnchor : interactable.transform);
    }

    public void ClearAvailable(Interactable interactable)
    {
        if (currentInteractable != interactable) return;
        currentInteractable = null;
        currentInteractor = null;
        GameEvents.InteractPromptChanged(null);
    }

    private void TryInteract()
    {
        if (currentInteractable == null) return;
        currentInteractable.Interact(currentInteractor);
    }
}
