using System.Collections.Generic;
using UnityEngine;

// Orquestrador da árvore de Perks de 1 herói (GDD Deadline 8.1, Seção 6/7.3/8-38, R9/R10) — vive
// no GameObject "Perk_Painel". Cada herói agora tem seu próprio "Content" (duplicado pelo
// usuário em "Perk_Painel/Viewport", nome do GameObject == heroKey sem "_", ex. "Blood_Mage" ->
// "BloodMage", mesma convenção de MainMenuNavigationUI.WireHeroCards). Content mapeia a
// hierarquia REAL montada à mão (Background_Perk = perk Central; "//World"/"//Primary_Attack"/
// "//Survival"/"//Ultimate" = as 4 árvores, cada uma com 37 filhos: 1 header + 6× Connection_N +
// 30 perks na sequência 2,5,7,9,5,2) e os PerkDefinitionSO de cada árvore vivem no
// HeroPerkContent daquele Content (não mais aqui) — este controller só escolhe QUAL Content
// ativar (ResolveHeroContent, por MainMenuUI.CurrentRun.hero) e liga PerkButtonUI/
// PerkConnectionUI nele.
//
// Regra de desbloqueio (confirmada com o usuário): Central rank≥1 libera a linha 1 das 4
// árvores ao mesmo tempo; dentro de 1 árvore, a linha N+1 só libera quando TODO perk com
// definição preenchida na linha N tiver rank≥1 (slot sem PerkDefinitionSO ainda — dado de
// teste/placeholder — nunca bloqueia, pra não travar a validação da infra à espera dos 1210
// perks completos).
//
// R9/R10 validam só a infra — os 9 heróis sem HeroPerkContent preenchido ainda ficam com os 4
// arrays vazios (resto null) até o conteúdo real entrar (R11+). Hoje só o Barbarian tem dado
// de verdade.
public class PerkTreeController : MonoBehaviour
{
    [SerializeField] private Transform contentsRoot; // "Perk_Painel/Viewport" — guarda 1 Content por herói

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

    private PerkDefinitionSO centralPerk;
    private PerkButtonUI centralButton;
    private readonly Dictionary<PerkTreeId, List<List<PerkButtonUI>>> treeLines = new();
    private readonly Dictionary<PerkTreeId, List<PerkConnectionUI>> treeConnections = new();

    private void Awake()
    {
        if (contentsRoot == null)
        {
            Debug.LogError("[PerkTreeController] Campo \"contentsRoot\" não configurado — arraste Perk_Painel/Viewport.", this);
            return;
        }

        var content = ResolveHeroContent();
        if (content == null) return; // ResolveHeroContent já logou o motivo

        // Liga e nunca mais desativa — "até o final da run" (pedido do usuário). As outras 9
        // Content ficam desativadas pra sempre (nunca têm PerkButtonUI nenhum ligado, nem
        // consomem Awake de PerkButtonUI à toa).
        content.gameObject.SetActive(true);

        // PerkMapController mora no mesmo GameObject de contentsRoot (Viewport) — precisa saber
        // qual dos 10 Content é o ativo pra arrastar/zoom funcionarem nele (achado real:
        // continuava fixo no Content original, agora desativado pra sempre).
        var mapController = contentsRoot.GetComponent<PerkMapController>();
        if (mapController != null) mapController.SetContent((RectTransform)content);

        var data = content.GetComponent<HeroPerkContent>();
        if (data == null)
        {
            Debug.LogError($"[PerkTreeController] \"{content.name}\" não tem HeroPerkContent.", content);
            return;
        }

        centralPerk = data.CentralPerk;
        BuildCentral(content);
        BuildTree(PerkTreeId.World, content, "//World", data.WorldPerks);
        BuildTree(PerkTreeId.PrimaryAttack, content, "//Primary_Attack", data.PrimaryAttackPerks);
        BuildTree(PerkTreeId.Survival, content, "//Survival", data.SurvivalPerks);
        BuildTree(PerkTreeId.Ultimate, content, "//Ultimate", data.UltimatePerks);
    }

    private void OnEnable()
    {
        GameEvents.OnGoldChanged += HandleGoldChanged;
        RefreshAll(); // reabrir a Loja pode ter mudado o gold enquanto o painel estava escondido
    }

    private void OnDisable() => GameEvents.OnGoldChanged -= HandleGoldChanged;

    private void HandleGoldChanged(long _) => RefreshAll();

    // Nome do GameObject Content == heroKey sem "_" (mesma convenção de
    // MainMenuNavigationUI.WireHeroCards, ex. "Blood_Mage" -> "BloodMage") pra bater com
    // MainMenuUI.CurrentRun.hero.
    private Transform ResolveHeroContent()
    {
        string heroKey = MainMenuUI.CurrentRun?.hero;
        if (string.IsNullOrEmpty(heroKey))
        {
            Debug.LogError("[PerkTreeController] MainMenuUI.CurrentRun/CurrentRun.hero nulo — não dá pra escolher o Content certo.", this);
            return null;
        }

        for (int i = 0; i < contentsRoot.childCount; i++)
        {
            var candidate = contentsRoot.GetChild(i);
            if (candidate.name.Replace("_", "") == heroKey) return candidate;
        }

        Debug.LogError($"[PerkTreeController] Nenhum Content em \"{contentsRoot.name}\" bate com o herói \"{heroKey}\".", this);
        return null;
    }

    private void BuildCentral(Transform content)
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

    // Transform.Find trata "/" como separador de caminho hierárquico — mas os GameObjects reais
    // se chamam literalmente "//World"/"//Primary_Attack"/etc. (as barras fazem parte do nome, não
    // são um caminho), então content.Find(rootName) sempre falhava (achado real: só veio à tona
    // quando Perk_Painel foi ativado pela 1ª vez numa sessão de Play). Busca por filho direto com
    // nome exato em vez disso.
    private static Transform FindDirectChild(Transform parent, string name)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            if (parent.GetChild(i).name == name) return parent.GetChild(i);
        }
        return null;
    }

    private void BuildTree(PerkTreeId treeId, Transform content, string rootName, PerkDefinitionSO[] definitions)
    {
        var root = FindDirectChild(content, rootName);
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
