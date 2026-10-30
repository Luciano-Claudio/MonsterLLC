using System.Collections.Generic;

public class Bag
{
    // Grade fixa da UI (10 → 20 → 30 → 40, GDD Seção 37) — os 40 slots sempre existem, mesmo
    // os ainda travados; `maxSlots` é só até onde o jogador pode guardar algo agora. Posição
    // fixa por índice (em vez de lista compactada) é o que permite a UI do inventário (TAB)
    // reposicionar/trocar itens de lugar sem perder a referência de qual slot é qual.
    public const int TotalSlotCount = 40;

    public int maxSlots;
    // long (não int) — stackSize começa em 16 (GDD Seção 37) mas sobe por compra na loja, sem
    // teto previsto; precisa acompanhar o mesmo teto de quantity abaixo, senão a mesma
    // conversão quebra de novo assim que a loja empurrar isso pra cima de ~2,1 bilhões.
    public long stackSize;
    public List<InventorySlot> Slots = new();

    public Bag(int maxSlots, long stackSize)
    {
        this.maxSlots = maxSlots;
        this.stackSize = stackSize;
        for (int i = 0; i < TotalSlotCount; i++) Slots.Add(new InventorySlot());
    }

    // Retorna quanto REALMENTE entrou — pode ser menor que 'amount' (coleta parcial, GDD Seção 37).
    public long AddItem(string itemName, long amount)
    {
        long remaining = amount;

        for (int i = 0; i < maxSlots; i++)
        {
            var slot = Slots[i];
            if (slot.itemName != itemName) continue;
            long space = stackSize - slot.quantity;
            if (space <= 0) continue;

            long toAdd = System.Math.Min(space, remaining);
            slot.quantity += toAdd;
            remaining -= toAdd;
            if (remaining == 0) return amount;
        }

        for (int i = 0; i < maxSlots && remaining > 0; i++)
        {
            var slot = Slots[i];
            if (!slot.IsEmpty) continue;

            long toAdd = System.Math.Min(stackSize, remaining);
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
    // 2 slots, incluindo quando um dos dois está vazio (equivale a "mover"). Só usado quando
    // os 2 slots NÃO têm o mesmo item — mesmo item cai em MergeStacks() (ver
    // InventoryTabUI.DropOnto), resolvido 2026-10-31.
    public void Swap(int indexA, int indexB)
    {
        if (indexA == indexB) return;
        if (indexA < 0 || indexA >= Slots.Count || indexB < 0 || indexB >= Slots.Count) return;

        var slotA = Slots[indexA];
        var slotB = Slots[indexB];
        (slotA.itemName, slotB.itemName) = (slotB.itemName, slotA.itemName);
        (slotA.quantity, slotB.quantity) = (slotB.quantity, slotA.quantity);
    }

    // Drag entre 2 stacks do MESMO item (GDD Seção 37, resolvido 2026-10-31) — move o MÁXIMO
    // possível de indexFrom pra indexTo (até o espaço livre do destino, stackSize - quantity).
    // Se não couber tudo, o resto fica no slot de origem (merge PARCIAL, nunca perde item).
    // Destino já cheio = nada se move (não vira Swap por acidente). Chamado só quando os 2
    // slots já têm o mesmo itemName — ver InventoryTabUI.DropOnto.
    public void MergeStacks(int indexFrom, int indexTo)
    {
        if (indexFrom == indexTo) return;
        if (indexFrom < 0 || indexFrom >= Slots.Count || indexTo < 0 || indexTo >= Slots.Count) return;

        var from = Slots[indexFrom];
        var to = Slots[indexTo];
        if (from.IsEmpty || to.IsEmpty || from.itemName != to.itemName) return;

        long space = stackSize - to.quantity;
        if (space <= 0) return; // destino já cheio — nada se move

        long moved = System.Math.Min(space, from.quantity);
        to.quantity += moved;
        from.quantity -= moved;
        if (from.quantity <= 0) { from.itemName = null; from.quantity = 0; }
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
