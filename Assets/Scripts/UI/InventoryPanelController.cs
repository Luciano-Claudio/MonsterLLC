using UnityEngine;
using UnityEngine.UI;

// Controla o painel do TAB: abre/fecha com BagController (GameEvents.OnBagToggled) e troca entre
// as abas Inventory/Stats pelos 2 botões que o usuário já montou na cena. Precisa morar num
// GameObject SEMPRE ativo (ex.: o Canvas) — "inventoryRoot" começa desativado de propósito, e um
// MonoBehaviour num GameObject inativo nunca roda Awake/OnEnable, então não pode viver nele.
public class InventoryPanelController : MonoBehaviour
{
    public GameObject inventoryRoot; // GameObject "Inventory" (pai de Inventory_UI/Stats_UI)
    public GameObject inventoryUI;   // GameObject "Inventory_UI"
    public GameObject statsUI;       // GameObject "Stats_UI"

    private void Awake()
    {
        WireTabButton(inventoryUI, "Stats_Button", ShowStats);
        WireTabButton(statsUI, "Inventory_Button", ShowInventory);
    }

    private static void WireTabButton(GameObject panel, string buttonName, UnityEngine.Events.UnityAction action)
    {
        panel.transform.Find(buttonName).GetComponent<Button>().onClick.AddListener(action);
    }

    private void OnEnable() => GameEvents.OnBagToggled += HandleBagToggled;
    private void OnDisable() => GameEvents.OnBagToggled -= HandleBagToggled;

    private void HandleBagToggled(bool isOpen)
    {
        inventoryRoot.SetActive(isOpen);
        if (isOpen) ShowInventory(); // sempre reabre na aba Inventory, nunca lembra a última aba
    }

    private void ShowStats()
    {
        inventoryUI.SetActive(false);
        statsUI.SetActive(true);
    }

    private void ShowInventory()
    {
        statsUI.SetActive(false);
        inventoryUI.SetActive(true);
    }
}
