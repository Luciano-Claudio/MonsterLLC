using UnityEngine;

public class BagController : MonoBehaviour
{
    public static BagController Instance { get; private set; }
    public Bag Bag { get; private set; }
    public bool IsOpen { get; private set; }

    private PlayerControls controls;

    private void Awake()
    {
        Instance = this;
        Bag = new Bag(maxSlots: 10, stackSize: 16); // GDD Seção 37 (revisado 2026-10-19): 10→20→30→40
        controls = new PlayerControls();
        controls.Gameplay.Inventory.performed += ctx => ToggleBag();
    }

    private void OnEnable() => controls.Enable();
    private void OnDisable() => controls.Disable();

    private void ToggleBag()
    {
        IsOpen = !IsOpen;
        if (IsOpen) TimeManager.Instance.Pause();
        else TimeManager.Instance.Resume();

        GameEvents.BagToggled(IsOpen);
        Debug.Log($"[BagController] Bag {(IsOpen ? "aberta" : "fechada")}.");
    }

    public int AddItem(string itemName, int amount)
    {
        int added = Bag.AddItem(itemName, amount);
        GameEvents.BagChanged(Bag);
        return added;
    }
}
