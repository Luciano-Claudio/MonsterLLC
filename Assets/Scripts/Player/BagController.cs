using UnityEngine;

public class BagController : MonoBehaviour
{
    public static BagController Instance { get; private set; }
    public Bag Bag { get; private set; }

    private PlayerControls controls;
    private bool isOpen;

    private void Awake()
    {
        Instance = this;
        Bag = new Bag(maxSlots: 5, stackSize: 16);
        controls = new PlayerControls();
        controls.Gameplay.Inventory.performed += ctx => ToggleBag();
    }

    private void OnEnable() => controls.Enable();
    private void OnDisable() => controls.Disable();

    private void ToggleBag()
    {
        isOpen = !isOpen;
        if (isOpen) TimeManager.Instance.Pause();
        else TimeManager.Instance.Resume();

        Debug.Log($"[BagController] Bag {(isOpen ? "aberta" : "fechada")}.");
    }

    public int AddItem(string itemName, int amount)
    {
        int added = Bag.AddItem(itemName, amount);
        GameEvents.BagChanged(Bag);
        return added;
    }
}
