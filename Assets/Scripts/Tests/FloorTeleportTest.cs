using UnityEngine;

public class FloorTeleportTest : MonoBehaviour
{
    public Transform player;
    public Vector3 groundSpawn = new Vector3(0, 0, 0);
    public Vector3 floor1Spawn = new Vector3(0, 50, 0);
    public Vector3 floor2Spawn = new Vector3(0, 100, 0);

    [ContextMenu("Teleport To Ground")]
    public void ToGround() => player.position = groundSpawn;

    [ContextMenu("Teleport To Floor 1")]
    public void ToFloor1() => player.position = floor1Spawn;

    [ContextMenu("Teleport To Floor 2")]
    public void ToFloor2() => player.position = floor2Spawn;
}
