[System.Serializable]
public class InventorySlot
{
    public string itemName;
    public long quantity;

    public bool IsEmpty => string.IsNullOrEmpty(itemName);
}
