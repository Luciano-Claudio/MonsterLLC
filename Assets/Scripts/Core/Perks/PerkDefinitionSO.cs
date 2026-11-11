using UnityEngine;

// 1 perk compravel (GDD Deadline 8.1, Secao 6/7.3/8-38, R9) - dado puro, sem logica de efeito
// ainda (isso e conteudo, R11+). maxRank NAO e um campo aqui de proposito - e sempre
// costPerRank.Length (1 rank por entrada de custo), e o numero de "pips" visiveis na cena
// (Perk_Qtd_Content) deve bater com isso, mas quem decide o numero real e o usuario editando a
// cena, nao este asset.
[CreateAssetMenu(fileName = "PerkDefinition", menuName = "MonsterLLC/Perks/Perk Definition")]
public class PerkDefinitionSO : ScriptableObject
{
    [Tooltip("Identificador unico e estavel - usado no save (RunState.perkRanks) e nunca deve mudar depois de publicado.")]
    public string perkId;

    public string displayName;

    [TextArea]
    public string description;

    public Sprite icon;

    [Tooltip("Custo em gold de cada rank, na ordem (index 0 = custo pra comprar o rank 1, etc.). O tamanho deste array = rank maximo do perk.")]
    public long[] costPerRank = { 10 };

    [Tooltip("perkId de outro perk que precisa ter pelo menos rank 1 ANTES deste poder ser comprado (ex.: Ranger 'Imortal em Combate' exige 'Veterano de Guerra'). Vazio = sem pre-requisito. Checado em PerkState.CanAfford().")]
    public string prerequisitePerkId;
}
