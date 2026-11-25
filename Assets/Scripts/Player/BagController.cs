using UnityEngine;

public class BagController : MonoBehaviour
{
    public static BagController Instance { get; private set; }
    public Bag Bag { get; private set; }

    // GDD Seção 37/8 — base fixa do herói, nunca muda. Mais Espaço de Bag/Stack Maior (World
    // Perks) somam/substituem por cima disso em ApplyWorldPerkBonuses(), nunca aqui.
    private const int BaseMaxSlots = 10;
    private const long BaseStackSize = 16;

    private void Awake()
    {
        Instance = this;
        Bag = new Bag(maxSlots: BaseMaxSlots, stackSize: BaseStackSize);
        // Etapa 4: o Tab (abrir a Bag/painel Inventory) foi removido — o painel saiu da cena.

        ApplyWorldPerkBonuses();
    }

    // Chamado por WorldPerkEffects.Recompute() (Mais Espaço de Bag/Stack Maior) — e também no
    // Awake acima, pro caso de um save carregado já vir com ranks > 0. BagController é
    // DontDestroyOnLoad e nunca é recriado entre runs, então isto precisa recalcular do zero
    // (BaseMaxSlots + bônus) a cada chamada, nunca só somar em cima do valor atual — senão uma
    // run nova (perkRanks resetado) herdaria o bônus da run anterior pra sempre.
    public void ApplyWorldPerkBonuses()
    {
        Bag.maxSlots = BaseMaxSlots + WorldPerkEffects.BagSlotBonus;
        Bag.stackSize = WorldPerkEffects.BagStackSize;
        GameEvents.BagChanged(Bag);
    }

    public long AddItem(string itemName, long amount)
    {
        long added = Bag.AddItem(itemName, amount);
        GameEvents.BagChanged(Bag);
        return added;
    }
}
