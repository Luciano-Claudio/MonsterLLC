using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FloorRegistry : MonoBehaviour
{
    public static FloorRegistry Instance { get; private set; }
    public List<FloorDefinition> Floors = new();

    private void Awake() => Instance = this;

    private FloorDefinition GetByPosition(int position) =>
        Floors.FirstOrDefault(f => f.activeFloorPosition == position);

    public FloorDefinition GetNextFloor(FloorDefinition current)
    {
        var next = StairRouting.GetNextPosition(Floors.Select(f => f.activeFloorPosition), current.activeFloorPosition);
        return next.HasValue ? GetByPosition(next.Value) : null;
    }

    public FloorDefinition GetPreviousFloor(FloorDefinition current)
    {
        var prev = StairRouting.GetPreviousPosition(Floors.Select(f => f.activeFloorPosition), current.activeFloorPosition);
        return prev.HasValue ? GetByPosition(prev.Value) : null;
    }
}
