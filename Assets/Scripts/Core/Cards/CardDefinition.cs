using UnityEngine;

// Etapas 1+2 (docs/superpowers/specs/2026-11-24-etapas-1-2-run-25min-cartas-design.md, 6.3/6.4) — uma
// carta de upgrade. Genérica (heroId vazio) ou específica de um herói. Pegar a mesma de novo sobe o
// nível dela até maxLevel. Dano multiplica por nível (valuePerLevel^nível); o resto soma.
// Só ACRESCENTAR no fim (valor serializado como número nos assets).
public enum CardStat { Damage, Area, Cooldown, AnimSpeed, MoveSpeed, Magnet, MaxHealth, DamageTaken, Regen, XpGain, Thorns, BlastChance }
public enum CardAbility { None, Primary, Shift, Ultimate }
// Tipo da carta (muda fundo, rótulo e cor no Cards_Popup): Item = vendedor; Upgrade/Stat = baú;
// subir de nível pode trazer os 3. Stat = os atributos (etapa 4).
public enum CardKind { Item, Upgrade, Stat }

[CreateAssetMenu(fileName = "Card", menuName = "MonsterLLC/Card")]
public class CardDefinition : ScriptableObject
{
    public string id;
    public CardKind kind = CardKind.Upgrade;
    public string title;
    [Tooltip("Texto da carta. {0} = o valor do próximo nível (ex.: 25%).")]
    [TextArea] public string description;
    public Sprite icon;
    public CardAbility ability;
    public CardStat stat;
    [Tooltip("Dano: multiplicador por nível (1,25 = +25% por nível, multiplicando). Os outros: soma por nível (0,15 = +15%; no cooldown, -10% por 0,10).")]
    public float valuePerLevel = 0.1f; // 🔢
    public int maxLevel = 5; // 🔢
    [Tooltip("Somado UMA vez quando a carta tem nível >= 1 (ex.: Blast Charm 5% + 5%/nível = 10% no nível 1).")]
    public float baseValue = 0f; // 🔢
    [Tooltip("Itens (etapa 3): vida máxima por nível, em fração da vida base (0,10 = +10% por nível).")]
    public float healthPerLevel = 0f; // 🔢
    [Tooltip("Vazio = vale pra todos os heróis; senão, só pro herói com esse nome (ex.: Barbarian).")]
    public string heroId = "";

    public bool Multiplicative => stat == CardStat.Damage;
}
