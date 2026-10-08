using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Ferramenta genérica pra bosses/mecânicas fora do padrão fixo do
// MonsterAnimationGeneratorWindow (que exige Melee/Ranged e obriga idle/walk/damage/die
// sempre). Aqui, TODO slot é opcional — igual aos campos do gerador normal (idle, walk,
// attack diagonal, attack orthogonal, damage, die), mas sem Monster Type nenhum: se o campo
// tiver uma sheet, gera aquele conjunto de clipes; se estiver vazio, simplesmente não gera
// (sem erro). Além dos slots padrão, aceita uma lista livre de clipes extras — qualquer
// nome, qualquer mecânica própria (ex.: throw_ratpeople, impact) — pra cobrir o que o
// padrão fixo não cobre. Opcionalmente, cada clipe gerado também entra como um State solto
// num Animator Controller (sem transição/parâmetro — isso continua ajuste manual depois).
public class CustomAnimationGeneratorWindow : EditorWindow
{
    private enum SheetLayout
    {
        SingleClip,   // 1 linha — clipe único, não-direcional (igual Die)
        Diagonal,     // 2 ou 4 linhas — SE/SW/NE/NW
        Orthogonal    // 2 ou 4 linhas — E/W/S/N
    }

    [System.Serializable]
    private class CustomClipEntry
    {
        public string name = "";
        public Texture2D spriteSheet;
        public SheetLayout layout = SheetLayout.Diagonal;
        public bool loop = false;
    }

    private static readonly string[] DiagonalDirections = { "se", "sw", "ne", "nw" };
    private static readonly string[] OrthogonalDirections = { "e", "w", "s", "n" };

    [SerializeField] private string monsterName = "";
    [SerializeField] private string destinationFolder = "Assets/Animation/Bosses/Floor1";
    [SerializeField] private float fps = 12f;
    [SerializeField] private AnimatorController targetController; // opcional

    // Slots padrão — TODOS opcionais aqui (diferente do MonsterAnimationGeneratorWindow, que
    // exige idle/walk/damage/die sempre e attack_orthogonal se for Ranged). Vazio = não gera
    // aquele conjunto, sem erro nenhum.
    [SerializeField] private Texture2D idleSpriteSheet;
    [SerializeField] private Texture2D walkSpriteSheet;
    [SerializeField] private Texture2D attackDiagonalSpriteSheet;
    [SerializeField] private Texture2D attackOrthogonalSpriteSheet;
    [SerializeField] private Texture2D damageSpriteSheet;
    [SerializeField] private Texture2D dieSpriteSheet;

    // Clipes extras, fora do padrão fixo acima — qualquer nome, qualquer mecânica (ex.:
    // throw_ratpeople do Rat People Royalty, impact do projétil dele).
    [SerializeField] private List<CustomClipEntry> customClips = new List<CustomClipEntry>();

    private Vector2 scrollPosition;

    [MenuItem("Tools/Custom Animation Generator")]
    public static void OpenWindow()
    {
        var window = GetWindow<CustomAnimationGeneratorWindow>();
        window.titleContent = new GUIContent("Custom Animation");
        window.minSize = new Vector2(520, 700);
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        GUILayout.Space(12);
        EditorGUILayout.LabelField("Custom Animation Generator", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Igual ao Monster Animation Generator, mas sem Melee/Ranged e com TODO slot " +
            "opcional — preencheu, gera; deixou vazio, pula sem erro. Pra bosses com " +
            "mecânica própria, some clipes extras na lista do fim (ex.: throw_ratpeople).",
            MessageType.None
        );
        GUILayout.Space(15);

        monsterName = EditorGUILayout.TextField("Monster/Boss Name", monsterName);

        GUILayout.Space(5);
        EditorGUILayout.BeginHorizontal();
        destinationFolder = EditorGUILayout.TextField("Destination", destinationFolder);
        if (GUILayout.Button("Browse", GUILayout.Width(70))) SelectDestinationFolder();
        EditorGUILayout.EndHorizontal();

        fps = EditorGUILayout.FloatField("FPS", fps);

        GUILayout.Space(15);
        EditorGUILayout.LabelField("Slots padrão (todos opcionais)", EditorStyles.boldLabel);

        idleSpriteSheet = DrawTextureField("Idle", idleSpriteSheet);
        walkSpriteSheet = DrawTextureField("Walk", walkSpriteSheet);
        attackDiagonalSpriteSheet = DrawTextureField("Attack Diagonal", attackDiagonalSpriteSheet);
        attackOrthogonalSpriteSheet = DrawTextureField("Attack Orthogonal", attackOrthogonalSpriteSheet);
        damageSpriteSheet = DrawTextureField("Damage", damageSpriteSheet);
        dieSpriteSheet = DrawTextureField("Die", dieSpriteSheet);

        EditorGUILayout.HelpBox(
            "Idle/Walk/Attack Diagonal/Damage: 2 ou 4 linhas (SE/SW/NE/NW). Attack " +
            "Orthogonal: 2 ou 4 linhas (E/W/S/N). Die: 1 linha só.\n" +
            "IdleCombat é derivado automaticamente do 1º frame do Walk — só se Walk estiver preenchido.",
            MessageType.None
        );

        GUILayout.Space(15);
        DrawCustomClipsSection();

        GUILayout.Space(15);
        targetController = (AnimatorController)EditorGUILayout.ObjectField(
            "Target Controller (opcional)", targetController, typeof(AnimatorController), false
        );
        EditorGUILayout.HelpBox(
            "Se preenchido, cada clipe gerado (padrão + extra) também entra como um State " +
            "solto nesse controller — sem transição/parâmetro nenhum, isso você ajusta " +
            "depois no Editor.",
            MessageType.None
        );

        GUILayout.Space(20);
        if (GUILayout.Button("GENERATE", GUILayout.Height(45))) Generate();

        GUILayout.Space(20);
        EditorGUILayout.EndScrollView();
    }

    private Texture2D DrawTextureField(string label, Texture2D texture) =>
        (Texture2D)EditorGUILayout.ObjectField(label, texture, typeof(Texture2D), false);

    private void DrawCustomClipsSection()
    {
        EditorGUILayout.LabelField("Clipes extras (mecânica própria)", EditorStyles.boldLabel);

        int removeIndex = -1;
        for (int i = 0; i < customClips.Count; i++)
        {
            var entry = customClips[i];
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            entry.name = EditorGUILayout.TextField("Nome", entry.name);
            if (GUILayout.Button("X", GUILayout.Width(22))) removeIndex = i;
            EditorGUILayout.EndHorizontal();

            entry.spriteSheet = DrawTextureField("Sprite Sheet", entry.spriteSheet);
            entry.layout = (SheetLayout)EditorGUILayout.EnumPopup("Layout", entry.layout);
            entry.loop = EditorGUILayout.Toggle("Loop", entry.loop);

            EditorGUILayout.EndVertical();
            GUILayout.Space(4);
        }

        if (removeIndex >= 0) customClips.RemoveAt(removeIndex);

        if (GUILayout.Button("+ Adicionar clipe extra")) customClips.Add(new CustomClipEntry());
    }

    // =========================================================
    // GENERATE
    // =========================================================

    private void Generate()
    {
        if (!Validate()) return;

        string outputDirectory = MonsterAnimationUtility.NormalizePath(destinationFolder) + "/" + monsterName.Trim();
        MonsterAnimationUtility.EnsureFolderExists(outputDirectory);

        var generatedClips = new List<AnimationClip>();

        if (idleSpriteSheet != null)
            GenerateDirectionalSet("idle", idleSpriteSheet, DiagonalDirections, true, generatedClips, deriveIdleCombat: false, outputDirectory);

        if (walkSpriteSheet != null)
            GenerateDirectionalSet("walk", walkSpriteSheet, DiagonalDirections, true, generatedClips, deriveIdleCombat: true, outputDirectory);

        if (attackDiagonalSpriteSheet != null)
            GenerateDirectionalSet("attack", attackDiagonalSpriteSheet, DiagonalDirections, false, generatedClips, false, outputDirectory, MonsterAnimationUtility.ClipEventKind.Attack);

        if (attackOrthogonalSpriteSheet != null)
            GenerateDirectionalSet("attack", attackOrthogonalSpriteSheet, OrthogonalDirections, false, generatedClips, false, outputDirectory, MonsterAnimationUtility.ClipEventKind.Attack);

        if (damageSpriteSheet != null)
            GenerateDirectionalSet("dmg", damageSpriteSheet, DiagonalDirections, false, generatedClips, false, outputDirectory);

        if (dieSpriteSheet != null)
            GenerateSingleClip("die", dieSpriteSheet, false, generatedClips, outputDirectory, MonsterAnimationUtility.ClipEventKind.Die);

        foreach (var entry in customClips)
        {
            if (entry.spriteSheet == null || string.IsNullOrWhiteSpace(entry.name)) continue; // entrada vazia/incompleta na lista — ignorada, não é erro

            if (entry.layout == SheetLayout.SingleClip)
                GenerateSingleClip(entry.name, entry.spriteSheet, entry.loop, generatedClips, outputDirectory);
            else
                GenerateDirectionalSet(
                    entry.name,
                    entry.spriteSheet,
                    entry.layout == SheetLayout.Diagonal ? DiagonalDirections : OrthogonalDirections,
                    entry.loop,
                    generatedClips,
                    false,
                    outputDirectory
                );
        }

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
            "Custom Animation Generated",
            monsterName + " — " + generatedClips.Count + " clipe(s) gerado(s).\n" +
            (targetController != null ? "States no controller: " + statesAdded + "\n" : "") +
            "\nDestino:\n" + outputDirectory,
            "OK"
        );
    }

    // Gera 1 clipe por direção (4 sempre, reaproveitando cada linha 2x se a sheet só tiver
    // 2) — mesma convenção do MonsterAnimationGeneratorWindow. deriveIdleCombat deriva
    // idleCombat_<dir> do 1º frame de CADA clipe gerado aqui (só usado pro Walk).
    private void GenerateDirectionalSet(
        string animationName,
        Texture2D spriteSheet,
        string[] directions,
        bool loop,
        List<AnimationClip> generatedClips,
        bool deriveIdleCombat,
        string outputDirectory,
        MonsterAnimationUtility.ClipEventKind eventKind = MonsterAnimationUtility.ClipEventKind.None
    )
    {
        var sprites = MonsterAnimationUtility.LoadSprites(spriteSheet);
        var rows = MonsterAnimationUtility.GroupSpritesByRows(sprites);

        for (int rowIndex = 0; rowIndex < 4; rowIndex++)
        {
            var rowSprites = rows.Count == 4 ? rows[rowIndex] : rows[rowIndex % 2];
            string clipName = animationName + "_" + directions[rowIndex];

            AnimationClip clip = MonsterAnimationUtility.CreateOrUpdateClip(clipName, rowSprites, outputDirectory, fps, loop, eventKind);
            generatedClips.Add(clip);

            if (deriveIdleCombat)
            {
                string idleCombatName = "idleCombat_" + directions[rowIndex];
                AnimationClip idleCombatClip = MonsterAnimationUtility.CreateSingleFrameClip(idleCombatName, rowSprites[0], outputDirectory, fps);
                generatedClips.Add(idleCombatClip);
            }
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

    // Adiciona um State novo pro clipe (nome = nome do clipe) se ainda não existir no layer
    // base do controller; se já existir (rodar de novo depois de recortar a sheet outra
    // vez), só atualiza o motion — nunca duplica nem toca em transição/parâmetro já
    // configurado à mão.
    private int AddOrUpdateState(AnimatorController controller, AnimationClip clip)
    {
        if (controller.layers.Length == 0) return 0;

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;

        foreach (var childState in stateMachine.states)
        {
            if (childState.state.name == clip.name)
            {
                childState.state.motion = clip;
                return 0; // já existia — não conta como "adicionado"
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
        if (string.IsNullOrWhiteSpace(monsterName))
        {
            ShowError("Informe o nome do monstro/boss.");
            return false;
        }

        if (MonsterAnimationUtility.ContainsInvalidFolderCharacters(monsterName))
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

        // Cada slot padrão só é validado SE tiver sheet — vazio é um "não gera", não um erro.
        if (idleSpriteSheet != null && !ValidateDirectional(idleSpriteSheet, "Idle")) return false;
        if (walkSpriteSheet != null && !ValidateDirectional(walkSpriteSheet, "Walk")) return false;
        if (attackDiagonalSpriteSheet != null && !ValidateDirectional(attackDiagonalSpriteSheet, "Attack Diagonal")) return false;
        if (attackOrthogonalSpriteSheet != null && !ValidateDirectional(attackOrthogonalSpriteSheet, "Attack Orthogonal")) return false;
        if (damageSpriteSheet != null && !ValidateDirectional(damageSpriteSheet, "Damage")) return false;
        if (dieSpriteSheet != null && !ValidateSingleRow(dieSpriteSheet, "Die")) return false;

        foreach (var entry in customClips)
        {
            if (entry.spriteSheet == null) continue; // linha vazia na lista — ignorada

            if (string.IsNullOrWhiteSpace(entry.name))
            {
                ShowError("Um clipe extra tem Sprite Sheet mas está sem nome.");
                return false;
            }

            bool valid = entry.layout == SheetLayout.SingleClip
                ? ValidateSingleRow(entry.spriteSheet, entry.name)
                : ValidateDirectional(entry.spriteSheet, entry.name);

            if (!valid) return false;
        }

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

    private void ShowError(string message) => EditorUtility.DisplayDialog("Custom Animation Generator", message, "OK");
}
