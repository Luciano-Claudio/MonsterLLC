using System;

// 1 linha da tabela de Gold por Dia (GDD Deadline 8.1, Seção 3.1) — quanto 1 kill de monstro
// comum e 1 kill de boss valem em gold fixo naquele dia. long (não int) — Dia 10 já passa de 2,1
// bilhões de possibilidades de overflow em int bem antes do fim da escala planejada.
[Serializable]
public class DayGoldDefinition
{
    public int dayNumber;
    public long killGold;
    public long bossGold;

    public DayGoldDefinition() { }

    public DayGoldDefinition(int dayNumber, long killGold, long bossGold)
    {
        this.dayNumber = dayNumber;
        this.killGold = killGold;
        this.bossGold = bossGold;
    }
}
