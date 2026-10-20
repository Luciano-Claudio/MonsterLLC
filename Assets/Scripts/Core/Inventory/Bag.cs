using System.Collections.Generic;

public class Bag
{
    // Grade fixa da UI (10 → 20 → 30 → 40, GDD Seção 37) — os 40 slots sempre existem, mesmo
    // os ainda travados; `maxSlots` é só até onde o jogador pode guardar algo agora. Posição
    // fixa por índice (em vez de lista compactada) é o que permite a UI do inventário (TAB)
    // reposicionar/trocar itens de lugar sem perder a referência de qual slot é qual.
    public const int TotalSlotCount = 40;

    public int maxSlots;
    public int stackSize;
    public List<InventorySlot> Slots = new();

    public Bag(int maxSlots, int stackSize)
    {
        this.maxSlots = maxSlots;
        this.stackSize = stackSize;
        for (int i = 0; i < TotalSlotCount; i++) Slots.Add(new InventorySlot());
    }

    // Retorna quanto REALMENTE entrou — pode ser menor que 'amount' (coleta parcial, GDD Seção 37).
    public int AddItem(string itemName, int amount)
    {
        int remaining = amount;

        for (int i = 0; i < maxSlots; i++)
        {
            var slot = Slots[i];
            if (slot.itemName != itemName) continue;
            int space = stackSize - slot.quantity;
            if (space <= 0) continue;

            int toAdd = System.Math.Min(space, remaining);
            slot.quantity += toAdd;
            remaining -= toAdd;
            if (remaining == 0) return amount;
        }

        for (int i = 0; i < maxSlots && remaining > 0; i++)
        {
            var slot = Slots[i];
            if (!slot.IsEmpty) continue;

            int toAdd = System.Math.Min(stackSize, remaining);
            slot.itemName = itemName;
            slot.quantity = toAdd;
            remaining -= toAdd;
        }

        return amount - remaining;
    }

    // Esvazia o slot NA MESMA posição (não desloca os outros) — slots travados (índice >=
    // maxSlots) nunca recebem item, então nunca precisam ser removidos.
    public void RemoveSlot(int index)
    {
        if (index < 0 || index >= Slots.Count) return;
        Slots[index].itemName = null;
        Slots[index].quantity = 0;
    }

    // Reposicionamento no TAB (GDD Seção 37: "swap, nunca bloqueio") — troca o conteúdo dos
    // 2 slots, incluindo quando um dos dois está vazio (equivale a "mover").
    public void Swap(int indexA, int indexB)
    {
        if (indexA == indexB) return;
        if (indexA < 0 || indexA >= Slots.Count || indexB < 0 || indexB >= Slots.Count) return;

        var slotA = Slots[indexA];
        var slotB = Slots[indexB];
        (slotA.itemName, slotB.itemName) = (slotB.itemName, slotA.itemName);
        (slotA.quantity, slotB.quantity) = (slotB.quantity, slotA.quantity);
    }

    public void Clear()
    {
        foreach (var slot in Slots)
        {
            slot.itemName = null;
            slot.quantity = 0;
        }
    }
}
