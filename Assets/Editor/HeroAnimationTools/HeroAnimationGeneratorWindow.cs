using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Ferramenta de animação pro lado do herói — mesmo espírito do CustomAnimationGeneratorWindow
// (todo slot opcional, sem erro se vazio), mas com o conjunto de slots próprio do herói
// (sem IdleCombat — heróis não têm essa distinção, GDD Seção 16) e sem nenhum evento
// automático de Attack/Shift/Ultimate: cada herói usa nomes de Animation Event DIFERENTES pro
// próprio golpe (ex.: Barbarian = AnimationAttackHitEvent, Paladin = AnimationHammerThrowEvent),
// diferente do lado do monstro (que compartilha AnimationHitEvent/AnimationAttackEndEvent via
// EnemyController) — então só o Die ganha evento automático aqui (AnimationDieEndEvent, que
// é herdado de HeroController por todo herói, nome fixo). Todo o resto é ajustado à mão
// depois, igual qualquer clipe customizado do outro gerador.
public class HeroAnimationGeneratorWindow : EditorWindow
{
    private enum SheetLayout
    {
        Diagonal,  // 2 ou 4 linhas — SE/SW/NE/NW
        Orthogonal // 2 ou 4 linhas — E/W/S/N
    }

    private static readonly string[] DiagonalDirections = { "se", "sw", "ne", "nw" };
    private static readonly string[] OrthogonalDirections = { "e", "w", "s", "n" };

    [SerializeField] private string heroName = "";
    [SerializeField] private string destinationFolder = "Assets/Animation/Heros";
    [SerializeField] private float fps = 12f;
    [SerializeField] private AnimatorController targetController; // opcional

    // Idle — único slot com tratamento especial: Start sozinho já forma a animação normal;
    // com Start + End, os frames do End são concatenados no FIM dos frames do Start (mesmo
    // clipe, 1 só por direção), não 2 clipes/estados separados.
    [SerializeField] private Texture2D idleStartSpriteSheet;
    [SerializeField] private Texture2D idleEndSpriteSheet;

    [SerializeField] private Texture2D walkSpriteSheet;
    [SerializeField] private Texture2D damageSpriteSheet;
    [SerializeField] private Texture2D dieSpriteSheet; // 1 linha só, igual ao lado do monstro

    [SerializeField] private Texture2D attackDiagonalSpriteSheet;
    [SerializeField] private Texture2D attackOrthogonalSpriteSheet;
    [SerializeField] private Texture2D shiftDiagonalSpriteSheet;
    [SerializeField] private Texture2D shiftOrthogonalSpriteSheet;
    [SerializeField] private Texture2D ultimateDiagonalSpriteSheet;
    [SerializeField] private Texture2D ultimateOrthogonalSpriteSheet;

    private Vector2 scrollPosition;

    [MenuItem("Tools/Hero Animation Generator")]
    public static void OpenWindow()
    {
        var window = GetWindow<HeroAnimationGeneratorWindow>();
        window.titleContent = new GUIContent("Hero Animation");
        window.minSize = new Vector2(520, 760);
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        GUILayout.Space(12);
        EditorGUILayout.LabelField("Hero Animation Generator", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Todo slot é opcional — preencheu, gera; deixou vazio, pula sem erro. Sem " +
            "IdleCombat (heróis não têm essa distinção) e sem Animation Event automático em " +
            "Attack/Shift/Ultimate (o nome do evento muda de herói pra herói — ajusta à mão " +
            "depois). Só o Die ganha AnimationDieEndEvent automático (herdado do " +
            "HeroController, nome fixo pra todo herói).",
            MessageType.None
        );
        GUILayout.Space(15);

        heroName = EditorGUILayout.TextField("Hero Name", heroName);

        GUILayout.Space(5);
        EditorGUILayout.BeginHorizontal();
        destinationFolder = EditorGUILayout.TextField("Destination", destinationFolder);
        if (GUILayout.Button("Browse", GUILayout.Width(70))) SelectDestinationFolder();
        EditorGUILayout.EndHorizontal();

        fps = EditorGUILayout.FloatField("FPS", fps);

        GUILayout.Space(15);
        EditorGUILayout.LabelField("Idle (Start obrigatório pra gerar; End é opcional)", EditorStyles.boldLabel);
        idleStartSpriteSheet = DrawTextureField("Idle Start", idleStartSpriteSheet);
        idleEndSpriteSheet = DrawTextureField("Idle End (opcional)", idleEndSpriteSheet);
        EditorGUILayout.HelpBox(
            "Só Start: idle normal, só com os frames do Start.\n" +
            "Start + End: 1 clipe só por direção, frames do End concatenados no FIM dos " +
            "frames do Start.",
            MessageType.None
        );

        GUILayout.Space(15);
        EditorGUILayout.LabelField("Clássicos", EditorStyles.boldLabel);
        walkSpriteSheet = DrawTextureField("Walk", walkSpriteSheet);
        damageSpriteSheet = DrawTextureField("Damage", damageSpriteSheet);
        dieSpriteSheet = DrawTextureField("Die", dieSpriteSheet);

        GUILayout.Space(15);
        EditorGUILayout.LabelField("Attack (caso haja)", EditorStyles.boldLabel);
        attackDiagonalSpriteSheet = DrawTextureField("Attack Diagonal", attackDiagonalSpriteSheet);
        attackOrthogonalSpriteSheet = DrawTextureField("Attack Orthogonal", attackOrthogonalSpriteSheet);

        GUILayout.Space(15);
        EditorGUILayout.LabelField("Shift / Habilidade Secundária (caso haja)", EditorStyles.boldLabel);
        shiftDiagonalSpriteSheet = DrawTextureField("Shift Diagonal", shiftDiagonalSpriteSheet);
        shiftOrthogonalSpriteSheet = DrawTextureField("Shift Orthogonal", shiftOrthogonalSpriteSheet);

        GUILayout.Space(15);
        EditorGUILayout.LabelField("Ultimate (caso haja)", EditorStyles.boldLabel);
        ultimateDiagonalSpriteSheet = DrawTextureField("Ultimate Diagonal", ultimateDiagonalSpriteSheet);
        ultimateOrthogonalSpriteSheet = DrawTextureField("Ultimate Orthogonal", ultimateOrthogonalSpriteSheet);

        EditorGUILayout.HelpBox(
            "Diagonal: 2 ou 4 linhas (SE/SW/NE/NW). Orthogonal: 2 ou 4 linhas (E/W/S/N). " +
            "Die: 1 linha só.\n" +
            "Loop automático: Idle/Walk = ON. Damage/Die/Attack/Shift/Ultimate = OFF.",
            MessageType.None
        );

        GUILayout.Space(15);
        targetController = (AnimatorController)EditorGUILayout.ObjectField(
            "Target Controller (opcional)", targetController, typeof(AnimatorController), false
        );
        EditorGUILayout.HelpBox(
            "Se preenchido, cada clipe gerado também entra como um State solto nesse " +
            "controller — sem transição/parâmetro nenhum, isso você ajusta depois no Editor.",
            MessageType.None
        );

        GUILayout.Space(20);
        if (GUILayout.Button("GENERATE", GUILayout.Height(45))) Generate();

        GUILayout.Space(20);
        EditorGUILayout.EndScrollView();
    }

    private Texture2D DrawTextureField(string label, Texture2D texture) =>
        (Texture2D)EditorGUILayout.ObjectField(label, texture, typeof(Texture2D), false);

    // =========================================================
    // GENERATE
    // =========================================================

    private void Generate()
    {
        if (!Validate()) return;

        string outputDirectory = MonsterAnimationUtility.NormalizePath(destinationFolder) + "/" + heroName.Trim();
        MonsterAnimationUtility.EnsureFolderExists(outputDirectory);

        var generatedClips = new List<AnimationClip>();

        if (idleStartSpriteSheet != null)
            GenerateIdleSet(generatedClips, outputDirectory);

        if (walkSpriteSheet != null)
            GenerateDirectionalSet("walk", walkSpriteSheet, DiagonalDirections, true, generatedClips, outputDirectory);

        if (damageSpriteSheet != null)
            GenerateDirectionalSet("dmg", damageSpriteSheet, DiagonalDirections, false, generatedClips, outputDirectory);

        if (dieSpriteSheet != null)
            GenerateSingleClip("die", dieSpriteSheet, false, generatedClips, outputDirectory, MonsterAnimationUtility.ClipEventKind.Die);

        if (attackDiagonalSpriteSheet != null)
            GenerateDirectionalSet("attack", attackDiagonalSpriteSheet, DiagonalDirections, false, generatedClips, outputDirectory);

        if (attackOrthogonalSpriteSheet != null)
            GenerateDirectionalSet("attack", attackOrthogonalSpriteSheet, OrthogonalDirections, false, generatedClips, outputDirectory);

        if (shiftDiagonalSpriteSheet != null)
            GenerateDirectionalSet("shift", shiftDiagonalSpriteSheet, DiagonalDirections, false, generatedClips, outputDirectory);

        if (shiftOrthogonalSpriteSheet != null)
            GenerateDirectionalSet("shift", shiftOrthogonalSpriteSheet, OrthogonalDirections, false, generatedClips, outputDirectory);

        if (ultimateDiagonalSpriteSheet != null)
            GenerateDirectionalSet("ultimate", ultimateDiagonalSpriteSheet, DiagonalDirections, false, generatedClips, outputDirectory);

        if (ultimateOrthogonalSpriteSheet != null)
            GenerateDirectionalSet("ultimate", ultimateOrthogonalSpriteSheet, OrthogonalDirections, false, generatedClips, outputDirectory);

        AssetDatabase.SaveAssets();

        int statesAdded = 0;
        if (targetController != null)
        {
            foreach (var clip in generatedClips) statesAdded += AddOrUpdateState(targetController, clip);
            EditorUtility.SetDirty(targetController);
            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(targetController);
        }
        else if (generatedClips.Count > 0)
        {
            EditorGUIUtility.PingObject(generatedClips[0]);
        }

        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Hero Animation Generated",
            heroName + " — " + generatedClips.Count + " clipe(s) gerado(s).\n" +
            (targetController != null ? "States no controller: " + statesAdded + "\n" : "") +
            "\nDestino:\n" + outputDirectory,
            "OK"
        );
    }

    // Idle Start (+ End, se houver) — único slot com junção de 2 sheets num clipe só por
    // direção. Sem End: comportamento idêntico a GenerateDirectionalSet. Com End: cada
    // direção recebe os frames do Start seguidos dos frames do End, no mesmo clipe (não 2
    // estados) — CreateOrUpdateClip já numera o tempo de cada frame pela posição na lista
    // recebida, então só concatenar as 2 listas antes de chamar já basta.
    private void GenerateIdleSet(List<AnimationClip> generatedClips, string outputDirectory)
    {
        var startRows = MonsterAnimationUtility.GroupSpritesByRows(MonsterAnimationUtility.LoadSprites(idleStartSpriteSheet));

        List<List<Sprite>> endRows = null;
        if (idleEndSpriteSheet != null)
            endRows = MonsterAnimationUtility.GroupSpritesByRows(MonsterAnimationUtility.LoadSprites(idleEndSpriteSheet));

        for (int rowIndex = 0; rowIndex < 4; rowIndex++)
        {
            var startRowSprites = startRows.Count == 4 ? startRows[rowIndex] : startRows[rowIndex % 2];
            string clipName = "idle_" + DiagonalDirections[rowIndex];

            List<Sprite> frames;
            if (endRows != null)
            {
                var endRowSprites = endRows.Count == 4 ? endRows[rowIndex] : endRows[rowIndex % 2];
                frames = new List<Sprite>(startRowSprites);
                frames.AddRange(endRowSprites);
            }
            else
            {
                frames = startRowSprites;
            }

            AnimationClip clip = MonsterAnimationUtility.CreateOrUpdateClip(clipName, frames, outputDirectory, fps, true);
            generatedClips.Add(clip);
        }
    }

    // Gera 1 clipe por direção (4 sempre, reaproveitando cada linha 2x se a sheet só tiver
    // 2) — mesma convenção do CustomAnimationGeneratorWindow. Sem derivação de IdleCombat
    // (heróis não têm esse estado).
    private void GenerateDirectionalSet(
        string animationName,
        Texture2D spriteSheet,
        string[] directions,
        bool loop,
        List<AnimationClip> generatedClips,
        string outputDirectory
    )
    {
        var sprites = MonsterAnimationUtility.LoadSprites(spriteSheet);
        var rows = MonsterAnimationUtility.GroupSpritesByRows(sprites);

        for (int rowIndex = 0; rowIndex < 4; rowIndex++)
        {
            var rowSprites = rows.Count == 4 ? rows[rowIndex] : rows[rowIndex % 2];
            string clipName = animationName + "_" + directions[rowIndex];

            AnimationClip clip = MonsterAnimationUtility.CreateOrUpdateClip(clipName, rowSprites, outputDirectory, fps, loop);
            generatedClips.Add(clip);
        }
    }

    private void GenerateSingleClip(
        string clipName,
        Texture2D spriteSheet,
        bool loop,
        List<AnimationClip> generatedClips,
        string outputDirectory,
        MonsterAnimationUtility.ClipEventKind eventKind = MonsterAnimationUtility.ClipEventKind.None
    )
    {
        var sprites = MonsterAnimationUtility.LoadSprites(spriteSheet).OrderBy(sprite => sprite.rect.x).ToList();
        AnimationClip clip = MonsterAnimationUtility.CreateOrUpdateClip(clipName, sprites, outputDirectory, fps, loop, eventKind);
        generatedClips.Add(clip);
    }

    // Mesma lógica do CustomAnimationGeneratorWindow — adiciona um State novo pro clipe se
    // ainda não existir no layer base do controller; se já existir, só atualiza o motion.
    private int AddOrUpdateState(AnimatorController controller, AnimationClip clip)
    {
        if (controller.layers.Length == 0) return 0;

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;

        foreach (var childState in stateMachine.states)
        {
            if (childState.state.name == clip.name)
            {
                childState.state.motion = clip;
                return 0;
            }
        }

        int existingCount = stateMachine.states.Length;
        AnimatorState newState = stateMachine.AddState(clip.name, new Vector3(420, 60 * existingCount, 0));
        newState.motion = clip;
        return 1;
    }

    // =========================================================
    // VALIDATION
    // =========================================================

    private bool Validate()
    {
        if (string.IsNullOrWhiteSpace(heroName))
        {
            ShowError("Informe o nome do herói.");
            return false;
        }

        if (MonsterAnimationUtility.ContainsInvalidFolderCharacters(heroName))
        {
            ShowError("O nome contém caracteres inválidos para uma pasta.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(destinationFolder))
        {
            ShowError("Informe a pasta de destino.");
            return false;
        }

        string normalized = MonsterAnimationUtility.NormalizePath(destinationFolder);
        if (normalized != "Assets" && !normalized.StartsWith("Assets/"))
        {
            ShowError("O destino precisa estar dentro de Assets.");
            return false;
        }

        if (fps <= 0f)
        {
            ShowError("O FPS precisa ser maior que zero.");
            return false;
        }

        if (idleStartSpriteSheet != null && !ValidateDirectional(idleStartSpriteSheet, "Idle Start")) return false;
        if (idleEndSpriteSheet != null && !ValidateDirectional(idleEndSpriteSheet, "Idle End")) return false;
        if (walkSpriteSheet != null && !ValidateDirectional(walkSpriteSheet, "Walk")) return false;
        if (damageSpriteSheet != null && !ValidateDirectional(damageSpriteSheet, "Damage")) return false;
        if (dieSpriteSheet != null && !ValidateSingleRow(dieSpriteSheet, "Die")) return false;
        if (attackDiagonalSpriteSheet != null && !ValidateDirectional(attackDiagonalSpriteSheet, "Attack Diagonal")) return false;
        if (attackOrthogonalSpriteSheet != null && !ValidateDirectional(attackOrthogonalSpriteSheet, "Attack Orthogonal")) return false;
        if (shiftDiagonalSpriteSheet != null && !ValidateDirectional(shiftDiagonalSpriteSheet, "Shift Diagonal")) return false;
        if (shiftOrthogonalSpriteSheet != null && !ValidateDirectional(shiftOrthogonalSpriteSheet, "Shift Orthogonal")) return false;
        if (ultimateDiagonalSpriteSheet != null && !ValidateDirectional(ultimateDiagonalSpriteSheet, "Ultimate Diagonal")) return false;
        if (ultimateOrthogonalSpriteSheet != null && !ValidateDirectional(ultimateOrthogonalSpriteSheet, "Ultimate Orthogonal")) return false;

        return true;
    }

    private bool ValidateDirectional(Texture2D spriteSheet, string label)
    {
        var sprites = MonsterAnimationUtility.LoadSprites(spriteSheet);
        if (sprites.Count == 0)
        {
            ShowError(label + " não possui sprites cortados.\n\nConfira se Sprite Mode = Multiple e se já foi fatiado.");
            return false;
        }

        var rows = MonsterAnimationUtility.GroupSpritesByRows(sprites);
        if (rows.Count != 4 && rows.Count != 2)
        {
            ShowError(label + " precisa de 4 linhas (ou 2, se cima/baixo forem idênticos).\n\nLinhas encontradas: " + rows.Count);
            return false;
        }

        foreach (var row in rows)
        {
            if (row.Count == 0)
            {
                ShowError(label + " possui uma linha vazia.");
                return false;
            }
        }

        return true;
    }

    private bool ValidateSingleRow(Texture2D spriteSheet, string label)
    {
        var sprites = MonsterAnimationUtility.LoadSprites(spriteSheet);
        if (sprites.Count == 0)
        {
            ShowError(label + " não possui sprites cortados.");
            return false;
        }

        var rows = MonsterAnimationUtility.GroupSpritesByRows(sprites);
        if (rows.Count != 1)
        {
            ShowError(label + " precisa possuir apenas uma linha.\n\nLinhas encontradas: " + rows.Count);
            return false;
        }

        return true;
    }

    private void SelectDestinationFolder()
    {
        string selectedFolder = EditorUtility.OpenFolderPanel("Select Animation Destination", Application.dataPath, "");
        if (string.IsNullOrEmpty(selectedFolder)) return;

        selectedFolder = selectedFolder.Replace("\\", "/");
        string projectAssetsPath = Application.dataPath.Replace("\\", "/");

        if (!selectedFolder.StartsWith(projectAssetsPath, System.StringComparison.OrdinalIgnoreCase))
        {
            ShowError("A pasta precisa estar dentro de Assets.");
            return;
        }

        destinationFolder = "Assets" + selectedFolder.Substring(projectAssetsPath.Length);
    }

    private void ShowError(string message) => EditorUtility.DisplayDialog("Hero Animation Generator", message, "OK");
}
