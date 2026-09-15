// Contador genérico de "vagas" com um teto — usado pelo limite de quantos Melee podem
// estar perto do player ao mesmo tempo (o resto flanqueia). Não é o Attack Budget antigo
// (aquilo limitava quem podia ATACAR ao mesmo tempo, um conceito de timing/animação);
// isso aqui é posicionamento/lotação, um conceito espacial — por isso é uma classe nova,
// não a ressuscitada.
public class SlotPool
{
    private readonly int maxSlots;
    private int occupied;

    public SlotPool(int maxSlots)
    {
        this.maxSlots = maxSlots;
    }

    public int Occupied => occupied;
    public bool IsFull => occupied >= maxSlots;

    public bool TryReserve()
    {
        if (IsFull) return false;
        occupied++;
        return true;
    }

    public void Release()
    {
        if (occupied > 0) occupied--;
    }
}
