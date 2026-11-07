using System;
using UnityEngine;

// Dados de hover do Hero Select (GDD Deadline 8.1, Seção 6: herói ativo -> vida base, ataque
// base, história curta). Nada disso existia antes desta feature, nem como stub — valores aqui
// são placeholder até o usuário preencher o real. heroKey precisa bater exatamente com o nome
// usado em MainMenuNavigationUI.WireHeroCards (nome do "<Hero>_Card" sem o sufixo "_Card" nem
// "_", ex. "BloodMage"). Mesmo padrão de tabela única com array interno de DayGoldTableSO.
[CreateAssetMenu(fileName = "HeroDisplayTable", menuName = "MonsterLLC/Hero Display Table")]
public class HeroDisplayTableSO : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public string heroKey;
        public int baseHp = 100;
        public float baseDamage = 10f;
        [TextArea] public string story = "???";
    }

    public Entry[] heroes;

    public bool TryGetEntry(string heroKey, out Entry entry)
    {
        foreach (var e in heroes)
        {
            if (e.heroKey == heroKey)
            {
                entry = e;
                return true;
            }
        }
        entry = null;
        return false;
    }
}
