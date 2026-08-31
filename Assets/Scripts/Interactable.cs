using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public abstract class Interactable : MonoBehaviour
{
    public Transform iconAnchor;

    public abstract void Interact(Transform interactor);

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        InteractionManager.Instance.SetAvailable(this, other.transform);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        InteractionManager.Instance.ClearAvailable(this);
    }
}
