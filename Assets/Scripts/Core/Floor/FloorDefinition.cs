using UnityEngine;

public class FloorDefinition : MonoBehaviour
{
    public string floorName = "Ground";
    public int originalFloorIdentity = 0;
    public int activeFloorPosition = 0;

    // Nome do GridGraph (A* Pathfinding Project) escaneado pra esse Floor — configurado no
    // AstarPath da cena (Editor). Usado só pra restringir a busca de path de cada monstro
    // ao próprio Floor (Seeker.graphMask); vazio/sem graph correspondente ainda = sem
    // restrição (Seeker busca em todos os graphs), estado seguro enquanto o AstarPath da
    // cena não estiver configurado.
    public string astarGraphName = "";
}
