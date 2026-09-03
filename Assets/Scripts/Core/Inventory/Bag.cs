using System.Collections.Generic;

public class Bag
{
    public int maxSlots;
    public int stackSize;
    public List<InventorySlot> Slots = new();

    public Bag(int maxSlots, int stackSize)
    {
        this.maxSlots = maxSlots;
        this.stackSize = stackSize;
    }

    // Retorna quanto REALMENTE entrou — pode ser menor que 'amount' (coleta parcial, GDD Seção 37).
    public int AddItem(string itemName, int amount)
    {
        int remaining = amount;

        foreach (var slot in Slots)
        {
            if (slot.itemName != itemName) continue;
            int space = stackSize - slot.quantity;
            if (space <= 0) continue;

            int toAdd = System.Math.Min(space, remaining);
            slot.quantity += toAdd;
            remaining -= toAdd;
            if (remaining == 0) return amount;
        }

        while (remaining > 0 && Slots.Count < maxSlots)
        {
            int toAdd = System.Math.Min(stackSize, remaining);
            Slots.Add(new InventorySlot { itemName = itemName, quantity = toAdd });
            remaining -= toAdd;
        }

        return amount - remaining;
    }

    public void RemoveSlot(int index)
    {
        if (index >= 0 && index < Slots.Count) Slots.RemoveAt(index);
    }

    public void Clear() => Slots.Clear();
}
