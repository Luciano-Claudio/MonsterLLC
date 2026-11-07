using UnityEngine;
using UnityEngine.EventSystems;

// Hover de 1 card de mundo/mapa no Map Select (GDD Deadline 8.1, Seção 6, extensão pedida pelo
// usuário) — mesmo critério do HeroCardHoverUI (AddComponent em runtime, via
// MainMenuNavigationUI.WireWorldCards). entry nula (mundo sem linha na WorldDisplayTableSO
// ainda) mostra "???" em vez de quebrar.
public class WorldCardHoverUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private string worldKey;
    private WorldDisplayTableSO table;

    public void Setup(string worldKey, WorldDisplayTableSO table)
    {
        this.worldKey = worldKey;
        this.table = table;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        string description = table != null && table.TryGetEntry(worldKey, out var entry) ? entry.story : "???";
        PopupController.Instance?.ShowSimple(gameObject, worldKey, description);
    }

    public void OnPointerExit(PointerEventData eventData) => PopupController.Instance?.Hide();
}
