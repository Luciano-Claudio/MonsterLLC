using UnityEngine;

public class FloorManager : MonoBehaviour
{
    public static FloorManager Instance { get; private set; }
    public FloorDefinition CurrentFloor { get; private set; }

    private void Awake() => Instance = this;

    public void SetCurrentFloor(FloorDefinition floor)
    {
        if (CurrentFloor == floor) return;
        CurrentFloor = floor;
        Debug.Log($"[FloorManager] Current Floor = {floor.floorName} (Original {floor.originalFloorIdentity}, Active Position {floor.activeFloorPosition})");
        GameEvents.FloorChanged(floor);
    }
}
