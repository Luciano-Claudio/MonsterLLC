using UnityEngine;

// Dados de Perk de 1 herói específico (extensão pedida pelo usuário — cada herói agora tem seu
// próprio "Content" duplicado em Perk_Painel/Viewport, nome do GameObject == heroKey sem "_",
// ex. "Blood_Mage"). Antes estes 5 campos viviam direto no PerkTreeController (1 controller só,
// sempre o mesmo herói); agora moram aqui, no Content do herói específico, pra
// PerkTreeController trocar de herói só escolhendo QUAL Content ativar (ver ResolveHeroContent),
// sem precisar duplicar nenhum código.
public class HeroPerkContent : MonoBehaviour
{
    [Header("Perks (slots na mesma ordem da linha 1→6, 2+5+7+9+5+2 = 30 por árvore)")]
    [SerializeField] private PerkDefinitionSO centralPerk;
    [SerializeField] private PerkDefinitionSO[] worldPerks = new PerkDefinitionSO[30];
    [SerializeField] private PerkDefinitionSO[] primaryAttackPerks = new PerkDefinitionSO[30];
    [SerializeField] private PerkDefinitionSO[] survivalPerks = new PerkDefinitionSO[30];
    [SerializeField] private PerkDefinitionSO[] ultimatePerks = new PerkDefinitionSO[30];

    public PerkDefinitionSO CentralPerk => centralPerk;
    public PerkDefinitionSO[] WorldPerks => worldPerks;
    public PerkDefinitionSO[] PrimaryAttackPerks => primaryAttackPerks;
    public PerkDefinitionSO[] SurvivalPerks => survivalPerks;
    public PerkDefinitionSO[] UltimatePerks => ultimatePerks;
}
