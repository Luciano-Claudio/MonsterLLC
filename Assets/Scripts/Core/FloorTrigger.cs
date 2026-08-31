using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class FloorTrigger : MonoBehaviour
{
    public FloorDefinition floor;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        FloorManager.Instance.SetCurrentFloor(floor);
    }
}
