using System.Collections.Generic;
using UnityEngine;

// Orquestrador da árvore de Perks de 1 herói (GDD Deadline 8.1, Seção 6/7.3/8-38, R9) — vive no
// GameObject "Perk_Painel". Mapeia a hierarquia REAL já montada pelo usuário em
// "Perk_Painel/Viewport/Content" (Background_Perk = perk Central; "//World"/"//Primary_Attack"/
// "//Survival"/"//Ultimate" = as 4 árvores, cada uma com 37 filhos: 1 header + 6× Connection_N +
// 30 perks na sequência 2,5,7,9,5,2) e liga PerkButtonUI/PerkConnectionUI nela.
//
// Regra de desbloqueio (confirmada com o usuário): Central rank≥1 libera a linha 1 das 4
// árvores ao mesmo tempo; dentro de 1 árvore, a linha N+1 só libera quando TODO perk com
// definição preenchida na linha N tiver rank≥1 (slot sem PerkDefinitionSO ainda — dado de
// teste/placeholder — nunca bloqueia, pra não travar a validação da infra à espera dos 1210
// perks completos).
//
// R9 valida só a infra — os arrays abaixo podem ter poucos slots preenchidos (resto null) até o
// conteúdo real entrar (R11+). 1 controller por herói (hoje só o Barbarian, confirmado pro R11).
public class PerkTreeController : MonoBehaviour
{
    [SerializeField] private Transform content; // "Perk_Painel/Viewport/Content"

    [Header("Perks (slots na mesma ordem da linha 1→6, 2+5+7+9+5+2 = 30 por árvore)")]
    [SerializeField] private PerkDefinitionSO centralPerk;
    [SerializeField] private PerkDefinitionSO[] worldPerks = new PerkDefinitionSO[30];
    [SerializeField] private PerkDefinitionSO[] primaryAttackPerks = new PerkDefinitionSO[30];
    [SerializeField] private PerkDefinitionSO[] survivalPerks = new PerkDefinitionSO[30];
    [SerializeField] private PerkDefinitionSO[] ultimatePerks = new PerkDefinitionSO[30];

    [Header("Sprites compartilhadas do botão (Perk_Button) — Seção 6.1")]
    [SerializeField] private Sprite buttonLockedSprite;
    [SerializeField] private Sprite buttonMaxedSprite;

    [Header("Sprites compartilhadas do fundo do perk (3º estado — \"pode comprar\")")]
    [SerializeField] private Sprite backgroundDefaultSprite;
    [SerializeField] private Sprite backgroundCanAffordSprite;

    [Header("Sprites compartilhadas dos pips de rank")]
    [SerializeField] private Sprite pipEmptySprite;
    [SerializeField] private Sprite pipFilledSprite;

    public Sprite ButtonLockedSprite => buttonLockedSprite;
    public Sprite ButtonMaxedSprite => buttonMaxedSprite;
    public Sprite BackgroundDefaultSprite => backgroundDefaultSprite;
    public Sprite BackgroundCanAffordSprite => backgroundCanAffordSprite;
    public Sprite PipEmptySprite => pipEmptySprite;
    public Sprite PipFilledSprite => pipFilledSprite;

    private static readonly int[] LineSizes = { 2, 5, 7, 9, 5, 2 };
    private static readonly PerkTreeId[] TreeIds = { PerkTreeId.World, PerkTreeId.PrimaryAttack, PerkTreeId.Survival, PerkTreeId.Ultimate };

    private PerkButtonUI centralButton;
    private readonly Dictionary<PerkTreeId, List<List<PerkButtonUI>>> treeLines = new();
    private readonly Dictionary<PerkTreeId, List<PerkConnectionUI>> treeConnections = new();

    private void Awake()
    {
        if (content == null)
        {
            Debug.LogError("[PerkTreeController] Campo \"content\" não configurado — arraste Perk_Painel/Viewport/Content.", this);
            return;
        }

        BuildCentral();
        BuildTree(PerkTreeId.World, "//World", worldPerks);
        BuildTree(PerkTreeId.PrimaryAttack, "//Primary_Attack", primaryAttackPerks);
        BuildTree(PerkTreeId.Survival, "//Survival", survivalPerks);
        BuildTree(PerkTreeId.Ultimate, "//Ultimate", ultimatePerks);
    }

    private void OnEnable()
    {
        GameEvents.OnGoldChanged += HandleGoldChanged;
        RefreshAll(); // reabrir a Loja pode ter mudado o gold enquanto o painel estava escondido
    }

    private void OnDisable() => GameEvents.OnGoldChanged -= HandleGoldChanged;

    private void HandleGoldChanged(long _) => RefreshAll();

    private void BuildCentral()
    {
        var root = content.Find("Background_Perk");
        if (root == null)
        {
            Debug.LogWarning("[PerkTreeController] Não achei \"Background_Perk\" dentro de Content.", this);
            return;
        }
        if (centralPerk == null) return; // placeholder — nada pra ligar ainda

        centralButton = root.gameObject.AddComponent<PerkButtonUI>();
        centralButton.Setup(this, centralPerk, PerkTreeId.Central, 0);
    }

    private void BuildTree(PerkTreeId treeId, string rootName, PerkDefinitionSO[] definitions)
    {
        var root = content.Find(rootName);
        if (root == null)
        {
            Debug.LogWarning($"[PerkTreeController] Não achei \"{rootName}\" dentro de Content.", this);
            return;
        }

        var lines = new List<List<PerkButtonUI>>();
        var connections = new List<PerkConnectionUI>();
        treeLines[treeId] = lines;
        treeConnections[treeId] = connections;

        int childIndex = 1; // índice 0 = header ("{Tree}_Name" ou só "{Tree}") — nunca usado aqui
        int perkGlobalIndex = 0;

        for (int line = 0; line < LineSizes.Length; line++)
        {
            var connectionTransform = root.GetChild(childIndex++);
            var connection = connectionTransform.GetComponent<PerkConnectionUI>();
            if (connection == null)
            {
                Debug.LogWarning($"[PerkTreeController] \"{rootName}/{connectionTransform.name}\" não tem PerkConnectionUI — adicione manualmente com os 2 sprites.", connectionTransform);
            }
            connections.Add(connection);

            var lineButtons = new List<PerkButtonUI>();
            for (int i = 0; i < LineSizes[line]; i++)
            {
                var perkTransform = root.GetChild(childIndex++);
                var definition = perkGlobalIndex < definitions.Length ? definitions[perkGlobalIndex] : null;
                perkGlobalIndex++;

                if (definition != null)
                {
                    var button = perkTransform.gameObject.AddComponent<PerkButtonUI>();
                    button.Setup(this, definition, treeId, line);
                    lineButtons.Add(button);
                }
            }
            lines.Add(lineButtons);
        }
    }

    public void RefreshAll()
    {
        centralButton?.Refresh(true); // Central nunca tem pré-requisito, sempre comprável desde o início

        foreach (var treeId in TreeIds)
        {
            if (!treeLines.TryGetValue(treeId, out var lines)) continue;
            var connections = treeConnections[treeId];

            for (int line = 0; line < lines.Count; line++)
            {
                bool unlocked = IsLineUnlocked(treeId, line);
                connections[line]?.Refresh(unlocked);
                foreach (var button in lines[line]) button.Refresh(unlocked);
            }
        }
    }

    // line==0: depende só do Central. line>0: depende de TODO perk com definição preenchida na
    // linha anterior ter rank≥1 (slot vazio/placeholder nunca bloqueia).
    private bool IsLineUnlocked(PerkTreeId treeId, int line)
    {
        if (line == 0) return centralPerk != null && PerkState.GetRank(centralPerk.perkId) >= 1;

        foreach (var button in treeLines[treeId][line - 1])
        {
            if (PerkState.GetRank(button.Definition.perkId) < 1) return false;
        }
        return true;
    }

    // GDD Deadline 8.1, Seção 6 — "X/5, nome do perk, e a comparação real do efeito (valor
    // atual → valor se comprar)". O "valor do efeito" em si é conteúdo (R11+, PerkDefinitionSO
    // ainda não modela magnitude por rank) — aqui só o rank e o custo, já reais.
    public void ShowPopupFor(PerkButtonUI button)
    {
        var definition = button.Definition;
        if (definition == null) return;

        int rank = PerkState.GetRank(definition.perkId);
        int maxRank = definition.costPerRank.Length;
        bool maxed = rank >= maxRank;

        string levelText = $"Level {rank}/{maxRank}";
        string nextLevelText = maxed ? "Max level" : $"Rank {rank + 1}/{maxRank}";
        string valueText = maxed ? null : $"{LargeNumberFormatter.Format(definition.costPerRank[rank])}g";

        PopupController.Instance?.ShowPerk(button.gameObject, definition.displayName, levelText, definition.description, nextLevelText, valueText);
    }
}
