using UnityEngine;

public class FloorReorderTest : MonoBehaviour
{
    public FloorDefinition floor1;
    public FloorDefinition floor2;

    [ContextMenu("Swap Floor 1 and Floor 2 Active Positions")]
    public void SwapPositions()
    {
        (floor1.activeFloorPosition, floor2.activeFloorPosition) = (floor2.activeFloorPosition, floor1.activeFloorPosition);
        Debug.Log($"[FloorReorderTest] Floor 1 agora é Active Position {floor1.activeFloorPosition}, Floor 2 é {floor2.activeFloorPosition}");
    }
}
