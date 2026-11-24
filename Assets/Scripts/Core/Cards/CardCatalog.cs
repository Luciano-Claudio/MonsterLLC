using System.Collections.Generic;
using UnityEngine;

// Etapas 1+2 — todas as cartas que podem sair (Resources/CardCatalog.asset). Cartas de outro herói
// ficam na mesma lista; o sorteio (CardRules.Roll) filtra pelo heroId.
[CreateAssetMenu(fileName = "CardCatalog", menuName = "MonsterLLC/Card Catalog")]
public class CardCatalog : ScriptableObject
{
    public List<CardDefinition> cards = new();

    public static CardCatalog Load() => Resources.Load<CardCatalog>("CardCatalog");
}
