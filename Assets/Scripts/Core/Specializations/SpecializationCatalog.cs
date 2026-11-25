using System.Collections.Generic;
using UnityEngine;

// Etapa 4 — todas as especializações (Resources/SpecializationCatalog.asset), mesmo padrão do CardCatalog.
[CreateAssetMenu(fileName = "SpecializationCatalog", menuName = "MonsterLLC/Specialization Catalog")]
public class SpecializationCatalog : ScriptableObject
{
    public List<SpecializationDefinition> specializations = new();

    public static SpecializationCatalog Load() => Resources.Load<SpecializationCatalog>("SpecializationCatalog");

    // As do herói, em ordem de slot (0 = esquerda). Lista vazia = herói ainda sem especialização.
    public void ForHero(string heroId, List<SpecializationDefinition> result)
    {
        result.Clear();
        foreach (var s in specializations)
            if (s != null && s.heroId == heroId) result.Add(s);
        result.Sort((a, b) => a.slot.CompareTo(b.slot));
    }
}
