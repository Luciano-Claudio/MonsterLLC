[System.Serializable]
public class InventorySlot
{
    public string itemName;
    public int quantity;

    public bool IsEmpty => string.IsNullOrEmpty(itemName);
}
