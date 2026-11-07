using System;
using UnityEngine;

// Dados de hover do Map Select (GDD Deadline 8.1, Seção 6, extensão pedida pelo usuário — o
// 2º botão do fluxo de New Game, "mundo/mapa", que ele chama de "open world"). Só 1 mundo
// existe hoje (Tower) — mesmo padrão de tabela única de HeroDisplayTableSO, pronta pra mais
// mapas no futuro sem mudar estrutura. worldKey precisa bater com o nome do card na cena
// (MainMenuNavigationUI.WireWorldCards), mesma convenção de heroKey.
[CreateAssetMenu(fileName = "WorldDisplayTable", menuName = "MonsterLLC/World Display Table")]
public class WorldDisplayTableSO : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public string worldKey;
        [TextArea] public string story = "???";
    }

    public Entry[] worlds;

    public bool TryGetEntry(string worldKey, out Entry entry)
    {
        foreach (var e in worlds)
        {
            if (e.worldKey == worldKey)
            {
                entry = e;
                return true;
            }
        }
        entry = null;
        return false;
    }
}
