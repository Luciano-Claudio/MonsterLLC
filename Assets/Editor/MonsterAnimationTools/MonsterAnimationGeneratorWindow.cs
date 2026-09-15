using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class MonsterAnimationGeneratorWindow : EditorWindow
{
    private enum MonsterType
    {
        Melee,
        Ranged
    }

    // Quais Animation Events entram automaticamente num clipe recém-criado.
    // AnimationHitEvent (attack) nasce em 50% — é só um ponto de partida, o frame exato
    // do golpe varia por clipe e continua sendo ajuste manual (arrastar no Animation
    // window). AnimationAttackEndEvent/AnimationDieEndEvent nascem no fim, que já serve
    // na maioria dos casos.
    private enum ClipEventKind
    {
        None,
        Attack,
        Die
    }

    // =========================================================
    // MONSTER
    // =========================================================

    [SerializeField]
    private string monsterName = "";

    [SerializeField]
    private MonsterType monsterType = MonsterType.Melee;

    [SerializeField]
    private string destinationFolder =
        "Assets/Animation/Monsters/Floor1";

    // =========================================================
    // BASE CONTROLLERS
    // =========================================================

    [SerializeField]
    private RuntimeAnimatorController baseMeleeController;

    [SerializeField]
    private RuntimeAnimatorController baseRangedController;

    // =========================================================
    // SPRITE SHEETS
    // =========================================================

    [SerializeField]
    private Texture2D idleSpriteSheet;

    [SerializeField]
    private Texture2D walkSpriteSheet;

    [SerializeField]
    private Texture2D attackDiagonalSpriteSheet;

    [SerializeField]
    private Texture2D attackOrthogonalSpriteSheet;

    [SerializeField]
    private Texture2D damageSpriteSheet;

    [SerializeField]
    private Texture2D dieSpriteSheet;

    // =========================================================
    // SETTINGS
    // =========================================================

    [SerializeField]
    private float fps = 12f;

    private Vector2 scrollPosition;

    // =========================================================
    // DIRECTIONS
    // =========================================================

    private readonly string[] diagonalDirections =
    {
        "se",
        "sw",
        "ne",
        "nw"
    };

    private readonly string[] orthogonalDirections =
    {
        "e",
        "w",
        "s",
        "n"
    };

    // =========================================================
    // WINDOW
    // =========================================================

    [MenuItem("Tools/Monster Animation Generator")]
    public static void OpenWindow()
    {
        MonsterAnimationGeneratorWindow window =
            GetWindow<MonsterAnimationGeneratorWindow>();

        window.titleContent =
            new GUIContent("Monster Animations");

        window.minSize =
            new Vector2(500, 650);
    }

    private void OnGUI()
    {
        scrollPosition =
            EditorGUILayout.BeginScrollView(scrollPosition);

        GUILayout.Space(12);

        EditorGUILayout.LabelField(
            "Monster Animation Generator",
            EditorStyles.boldLabel
        );

        GUILayout.Space(15);

        DrawMonsterSection();

        GUILayout.Space(20);

        DrawBaseControllerSection();

        GUILayout.Space(20);

        DrawSpriteSheetSection();

        GUILayout.Space(20);

        DrawSettingsSection();

        GUILayout.Space(25);

        DrawGenerateButton();

        GUILayout.Space(20);

        EditorGUILayout.EndScrollView();
    }

    // =========================================================
    // UI - MONSTER
    // =========================================================

    private void DrawMonsterSection()
    {
        EditorGUILayout.LabelField(
            "Monster",
            EditorStyles.boldLabel
        );

        monsterName =
            EditorGUILayout.TextField(
                "Monster Name",
                monsterName
            );

        monsterType =
            (MonsterType)EditorGUILayout.EnumPopup(
                "Type",
                monsterType
            );

        GUILayout.Space(5);

        EditorGUILayout.BeginHorizontal();

        destinationFolder =
            EditorGUILayout.TextField(
                "Destination",
                destinationFolder
            );

        if (GUILayout.Button("Browse", GUILayout.Width(70)))
        {
            SelectDestinationFolder();
        }

        EditorGUILayout.EndHorizontal();

        GUILayout.Space(8);

        EditorGUILayout.HelpBox(
            "Output:\n" + GetMonsterOutputDirectory(),
            MessageType.Info
        );
    }

    // =========================================================
    // UI - BASE CONTROLLERS
    // =========================================================

    private void DrawBaseControllerSection()
    {
        EditorGUILayout.LabelField(
            "Base Animator",
            EditorStyles.boldLabel
        );

        baseMeleeController =
            (RuntimeAnimatorController)
            EditorGUILayout.ObjectField(
                "Base Melee",
                baseMeleeController,
                typeof(RuntimeAnimatorController),
                false
            );

        baseRangedController =
            (RuntimeAnimatorController)
            EditorGUILayout.ObjectField(
                "Base Ranged",
                baseRangedController,
                typeof(RuntimeAnimatorController),
                false
            );

        GUILayout.Space(5);

        if (monsterType == MonsterType.Melee)
        {
            EditorGUILayout.HelpBox(
                "Este monstro usará o Base_Melee.",
                MessageType.None
            );
        }
        else
        {
            EditorGUILayout.HelpBox(
                "Este monstro usará o Base_Ranged.",
                MessageType.None
            );
        }
    }

    // =========================================================
    // UI - SPRITE SHEETS
    // =========================================================

    private void DrawSpriteSheetSection()
    {
        EditorGUILayout.LabelField(
            "Sprite Sheets",
            EditorStyles.boldLabel
        );

        idleSpriteSheet =
            DrawTextureField(
                "Idle",
                idleSpriteSheet
            );

        walkSpriteSheet =
            DrawTextureField(
                "Walk",
                walkSpriteSheet
            );

        attackDiagonalSpriteSheet =
            DrawTextureField(
                "Attack Diagonal",
                attackDiagonalSpriteSheet
            );

        if (monsterType == MonsterType.Ranged)
        {
            attackOrthogonalSpriteSheet =
                DrawTextureField(
                    "Attack Orthogonal",
                    attackOrthogonalSpriteSheet
                );
        }

        damageSpriteSheet =
            DrawTextureField(
                "Damage",
                damageSpriteSheet
            );

        dieSpriteSheet =
            DrawTextureField(
                "Die",
                dieSpriteSheet
            );

        GUILayout.Space(8);

        EditorGUILayout.HelpBox(
            "Diagonal:\n" +
            "1ª linha = NE\n" +
            "2ª linha = NW\n" +
            "3ª linha = SE\n" +
            "4ª linha = SW\n\n" +

            "Orthogonal:\n" +
            "1ª linha = E\n" +
            "2ª linha = W\n" +
            "3ª linha = S\n" +
            "4ª linha = N",
            MessageType.None
        );
    }

    private Texture2D DrawTextureField(
        string label,
        Texture2D texture
    )
    {
        return (Texture2D)
            EditorGUILayout.ObjectField(
                label,
                texture,
                typeof(Texture2D),
                false
            );
    }

    // =========================================================
    // UI - SETTINGS
    // =========================================================

    private void DrawSettingsSection()
    {
        EditorGUILayout.LabelField(
            "Settings",
            EditorStyles.boldLabel
        );

        fps =
            EditorGUILayout.FloatField(
                "FPS",
                fps
            );

        GUILayout.Space(5);

        EditorGUILayout.HelpBox(
            "Loop automático:\n" +
            "Idle = ON\n" +
            "IdleCombat = ON\n" +
            "Walk = ON\n" +
            "Attack = OFF\n" +
            "Damage = OFF\n" +
            "Die = OFF",
            MessageType.None
        );
    }

    // =========================================================
    // GENERATE BUTTON
    // =========================================================

    private void DrawGenerateButton()
    {
        if (
            GUILayout.Button(
                "GENERATE MONSTER",
                GUILayout.Height(50)
            )
        )
        {
            GenerateMonster();
        }
    }

    // =========================================================
    // GENERATE MONSTER
    // =========================================================

    private void GenerateMonster()
    {
        if (!ValidateEverything())
        {
            return;
        }

        string outputDirectory =
            GetMonsterOutputDirectory();

        EnsureFolderExists(
            outputDirectory
        );

        Dictionary<string, AnimationClip> generatedClips =
            new Dictionary<string, AnimationClip>(
                StringComparer.OrdinalIgnoreCase
            );

        // =====================================================
        // IDLE + IDLE COMBAT
        // =====================================================

        GenerateDirectionalAnimationSet(
            idleSpriteSheet,
            "idle",
            diagonalDirections,
            outputDirectory,
            true,
            generatedClips,
            true
        );

        // =====================================================
        // WALK
        // =====================================================

        GenerateDirectionalAnimationSet(
            walkSpriteSheet,
            "walk",
            diagonalDirections,
            outputDirectory,
            true,
            generatedClips,
            false
        );

        // =====================================================
        // ATTACK DIAGONAL
        // =====================================================

        GenerateDirectionalAnimationSet(
            attackDiagonalSpriteSheet,
            "attack",
            diagonalDirections,
            outputDirectory,
            false,
            generatedClips,
            false
        );

        // =====================================================
        // ATTACK ORTHOGONAL - RANGED
        // =====================================================

        if (monsterType == MonsterType.Ranged)
        {
            GenerateDirectionalAnimationSet(
                attackOrthogonalSpriteSheet,
                "attack",
                orthogonalDirections,
                outputDirectory,
                false,
                generatedClips,
                false
            );
        }

        // =====================================================
        // DAMAGE
        // =====================================================

        GenerateDirectionalAnimationSet(
            damageSpriteSheet,
            "dmg",
            diagonalDirections,
            outputDirectory,
            false,
            generatedClips,
            false
        );

        // =====================================================
        // DIE
        // =====================================================

        GenerateSingleRowAnimation(
            dieSpriteSheet,
            "die",
            outputDirectory,
            false,
            generatedClips
        );

        AssetDatabase.SaveAssets();

        // =====================================================
        // OVERRIDE CONTROLLER
        // =====================================================

        AnimatorOverrideController overrideController =
            CreateOrUpdateOverrideController(
                outputDirectory,
                generatedClips
            );

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (overrideController != null)
        {
            EditorGUIUtility.PingObject(
                overrideController
            );
        }

        int expectedClips =
            monsterType == MonsterType.Melee
                ? 21
                : 25;

        EditorUtility.DisplayDialog(
            "Monster Generated",
            monsterName +
            " foi gerado com sucesso!\n\n" +

            "Animation Clips: " +
            expectedClips +
            "\n" +

            "Override Controller: criado/atualizado\n\n" +

            "Destino:\n" +
            outputDirectory,
            "OK"
        );
    }

    // =========================================================
    // VALIDATION
    // =========================================================

    private bool ValidateEverything()
    {
        if (string.IsNullOrWhiteSpace(monsterName))
        {
            ShowError(
                "Informe o nome do monstro."
            );

            return false;
        }

        if (ContainsInvalidFolderCharacters(monsterName))
        {
            ShowError(
                "O nome do monstro contém caracteres inválidos para uma pasta."
            );

            return false;
        }

        if (string.IsNullOrWhiteSpace(destinationFolder))
        {
            ShowError(
                "Informe a pasta de destino."
            );

            return false;
        }

        destinationFolder =
            NormalizePath(destinationFolder);

        if (
            destinationFolder != "Assets" &&
            !destinationFolder.StartsWith(
                "Assets/",
                StringComparison.Ordinal
            )
        )
        {
            ShowError(
                "O destino precisa estar dentro de Assets.\n\n" +
                "Exemplo:\n" +
                "Assets/Animation/Monsters/Floor1"
            );

            return false;
        }

        if (fps <= 0f)
        {
            ShowError(
                "O FPS precisa ser maior que zero."
            );

            return false;
        }

        if (
            monsterType == MonsterType.Melee &&
            baseMeleeController == null
        )
        {
            ShowError(
                "Selecione o Base_Melee.controller."
            );

            return false;
        }

        if (
            monsterType == MonsterType.Ranged &&
            baseRangedController == null
        )
        {
            ShowError(
                "Selecione o Base_Ranged.controller."
            );

            return false;
        }

        if (idleSpriteSheet == null)
        {
            ShowError(
                "Selecione o Sprite Sheet de Idle."
            );

            return false;
        }

        if (walkSpriteSheet == null)
        {
            ShowError(
                "Selecione o Sprite Sheet de Walk."
            );

            return false;
        }

        if (attackDiagonalSpriteSheet == null)
        {
            ShowError(
                "Selecione o Sprite Sheet de Attack Diagonal."
            );

            return false;
        }

        if (
            monsterType == MonsterType.Ranged &&
            attackOrthogonalSpriteSheet == null
        )
        {
            ShowError(
                "Monstros ranged precisam do Attack Orthogonal."
            );

            return false;
        }

        if (damageSpriteSheet == null)
        {
            ShowError(
                "Selecione o Sprite Sheet de Damage."
            );

            return false;
        }

        if (dieSpriteSheet == null)
        {
            ShowError(
                "Selecione o Sprite Sheet de Die."
            );

            return false;
        }

        // =====================================================
        // VALIDATE SPRITE SHEETS
        // =====================================================

        if (
            !ValidateDirectionalSpriteSheet(
                idleSpriteSheet,
                "Idle"
            )
        )
        {
            return false;
        }

        if (
            !ValidateDirectionalSpriteSheet(
                walkSpriteSheet,
                "Walk"
            )
        )
        {
            return false;
        }

        if (
            !ValidateDirectionalSpriteSheet(
                attackDiagonalSpriteSheet,
                "Attack Diagonal"
            )
        )
        {
            return false;
        }

        if (
            monsterType == MonsterType.Ranged &&
            !ValidateDirectionalSpriteSheet(
                attackOrthogonalSpriteSheet,
                "Attack Orthogonal"
            )
        )
        {
            return false;
        }

        if (
            !ValidateDirectionalSpriteSheet(
                damageSpriteSheet,
                "Damage"
            )
        )
        {
            return false;
        }

        if (
            !ValidateSingleRowSpriteSheet(
                dieSpriteSheet,
                "Die"
            )
        )
        {
            return false;
        }

        return true;
    }

    // =========================================================
    // VALIDATE DIRECTIONAL SHEET
    // =========================================================

    private bool ValidateDirectionalSpriteSheet(
        Texture2D spriteSheet,
        string animationLabel
    )
    {
        List<Sprite> sprites =
            LoadSprites(spriteSheet);

        if (sprites.Count == 0)
        {
            ShowError(
                animationLabel +
                " não possui sprites cortados.\n\n" +

                "Confira se:\n" +
                "Sprite Mode = Multiple\n" +
                "e o Sprite Sheet já foi fatiado."
            );

            return false;
        }

        List<List<Sprite>> rows =
            GroupSpritesByRows(sprites);

        if (rows.Count != 4)
        {
            ShowError(
                animationLabel +
                " precisa possuir exatamente 4 linhas.\n\n" +

                "Linhas encontradas: " +
                rows.Count
            );

            return false;
        }

        for (int i = 0; i < rows.Count; i++)
        {
            if (rows[i].Count == 0)
            {
                ShowError(
                    animationLabel +
                    " possui uma linha vazia."
                );

                return false;
            }
        }

        return true;
    }

    // =========================================================
    // VALIDATE DIE
    // =========================================================

    private bool ValidateSingleRowSpriteSheet(
        Texture2D spriteSheet,
        string animationLabel
    )
    {
        List<Sprite> sprites =
            LoadSprites(spriteSheet);

        if (sprites.Count == 0)
        {
            ShowError(
                animationLabel +
                " não possui sprites cortados."
            );

            return false;
        }

        List<List<Sprite>> rows =
            GroupSpritesByRows(sprites);

        if (rows.Count != 1)
        {
            ShowError(
                animationLabel +
                " precisa possuir apenas uma linha.\n\n" +

                "Linhas encontradas: " +
                rows.Count
            );

            return false;
        }

        return true;
    }

    // =========================================================
    // GENERATE DIRECTIONAL SET
    // =========================================================

    private void GenerateDirectionalAnimationSet(
        Texture2D spriteSheet,
        string animationName,
        string[] directions,
        string outputDirectory,
        bool loop,
        Dictionary<string, AnimationClip> generatedClips,
        bool createIdleCombat
    )
    {
        List<Sprite> sprites =
            LoadSprites(spriteSheet);

        List<List<Sprite>> rows =
            GroupSpritesByRows(sprites);

        for (int rowIndex = 0; rowIndex < 4; rowIndex++)
        {
            string direction =
                directions[rowIndex];

            List<Sprite> rowSprites =
                rows[rowIndex];

            string clipName =
                animationName +
                "_" +
                direction;

            AnimationClip clip =
                CreateOrUpdateClip(
                    clipName,
                    rowSprites,
                    outputDirectory,
                    loop,
                    animationName == "attack"
                        ? ClipEventKind.Attack
                        : ClipEventKind.None
                );

            RegisterGeneratedClip(
                generatedClips,
                clipName,
                clip
            );

            // ===============================================
            // DAMAGE ALIAS
            // ===============================================

            if (animationName == "dmg")
            {
                RegisterGeneratedClip(
                    generatedClips,
                    "damage_" + direction,
                    clip
                );
            }

            // ===============================================
            // IDLE COMBAT
            // ===============================================

            if (createIdleCombat)
            {
                string idleCombatName =
                    "idleCombat_" +
                    direction;

                AnimationClip idleCombatClip =
                    CreateSingleFrameClip(
                        idleCombatName,
                        rowSprites[0],
                        outputDirectory
                    );

                RegisterGeneratedClip(
                    generatedClips,
                    idleCombatName,
                    idleCombatClip
                );
            }
        }
    }

    // =========================================================
    // GENERATE DIE
    // =========================================================

    private void GenerateSingleRowAnimation(
        Texture2D spriteSheet,
        string animationName,
        string outputDirectory,
        bool loop,
        Dictionary<string, AnimationClip> generatedClips
    )
    {
        List<Sprite> sprites =
            LoadSprites(spriteSheet);

        List<Sprite> orderedSprites =
            sprites
                .OrderBy(
                    sprite => sprite.rect.x
                )
                .ToList();

        AnimationClip clip =
            CreateOrUpdateClip(
                animationName,
                orderedSprites,
                outputDirectory,
                loop,
                animationName == "die"
                    ? ClipEventKind.Die
                    : ClipEventKind.None
            );

        RegisterGeneratedClip(
            generatedClips,
            animationName,
            clip
        );
    }

    // =========================================================
    // CREATE / UPDATE ANIMATION CLIP
    // =========================================================

    private AnimationClip CreateOrUpdateClip(
        string clipName,
        List<Sprite> sprites,
        string outputDirectory,
        bool shouldLoop,
        ClipEventKind eventKind = ClipEventKind.None
    )
    {
        string clipPath =
            outputDirectory +
            "/" +
            clipName +
            ".anim";

        AnimationClip clip =
            AssetDatabase.LoadAssetAtPath<AnimationClip>(
                clipPath
            );

        bool isNewClip =
            clip == null;

        if (isNewClip)
        {
            clip =
                new AnimationClip();

            clip.name =
                clipName;

            AssetDatabase.CreateAsset(
                clip,
                clipPath
            );
        }

        clip.frameRate =
            fps;

        EditorCurveBinding spriteBinding =
            new EditorCurveBinding
            {
                type = typeof(SpriteRenderer),

                path = "",

                propertyName = "m_Sprite"
            };

        ObjectReferenceKeyframe[] keyframes =
            new ObjectReferenceKeyframe[
                sprites.Count
            ];

        for (int i = 0; i < sprites.Count; i++)
        {
            keyframes[i] =
                new ObjectReferenceKeyframe
                {
                    time =
                        i / fps,

                    value =
                        sprites[i]
                };
        }

        AnimationUtility.SetObjectReferenceCurve(
            clip,
            spriteBinding,
            keyframes
        );

        SetLoop(
            clip,
            shouldLoop
        );

        // Só na criação — rodar o gerador de novo num monstro que já existe (ex.: depois
        // de recortar a sprite sheet de novo) não pode sobrescrever o frame exato que
        // você já ajustou manualmente num AnimationHitEvent.
        if (isNewClip && eventKind != ClipEventKind.None)
        {
            float clipDuration =
                sprites.Count / fps;

            ApplyDefaultAnimationEvents(
                clip,
                eventKind,
                clipDuration
            );
        }

        EditorUtility.SetDirty(
            clip
        );

        return clip;
    }

    // =========================================================
    // DEFAULT ANIMATION EVENTS (ATTACK / DIE)
    // =========================================================

    private void ApplyDefaultAnimationEvents(
        AnimationClip clip,
        ClipEventKind eventKind,
        float clipDuration
    )
    {
        AnimationEvent[] events;

        if (eventKind == ClipEventKind.Attack)
        {
            events = new[]
            {
                new AnimationEvent
                {
                    time = clipDuration * 0.5f,
                    functionName = "AnimationHitEvent"
                },
                new AnimationEvent
                {
                    time = clipDuration,
                    functionName = "AnimationAttackEndEvent"
                }
            };
        }
        else
        {
            events = new[]
            {
                new AnimationEvent
                {
                    time = clipDuration,
                    functionName = "AnimationDieEndEvent"
                }
            };
        }

        AnimationUtility.SetAnimationEvents(
            clip,
            events
        );
    }

    // =========================================================
    // IDLE COMBAT
    // =========================================================

    private AnimationClip CreateSingleFrameClip(
        string clipName,
        Sprite sprite,
        string outputDirectory
    )
    {
        string clipPath =
            outputDirectory +
            "/" +
            clipName +
            ".anim";

        AnimationClip clip =
            AssetDatabase.LoadAssetAtPath<AnimationClip>(
                clipPath
            );

        if (clip == null)
        {
            clip =
                new AnimationClip();

            clip.name =
                clipName;

            AssetDatabase.CreateAsset(
                clip,
                clipPath
            );
        }

        clip.frameRate =
            fps;

        EditorCurveBinding spriteBinding =
            new EditorCurveBinding
            {
                type = typeof(SpriteRenderer),

                path = "",

                propertyName = "m_Sprite"
            };

        ObjectReferenceKeyframe[] keyframes =
        {
            new ObjectReferenceKeyframe
            {
                time = 0f,

                value = sprite
            },

            new ObjectReferenceKeyframe
            {
                time = 1f / fps,

                value = sprite
            }
        };

        AnimationUtility.SetObjectReferenceCurve(
            clip,
            spriteBinding,
            keyframes
        );

        SetLoop(
            clip,
            true
        );

        EditorUtility.SetDirty(
            clip
        );

        return clip;
    }

    // =========================================================
    // LOOP
    // =========================================================

    private void SetLoop(
        AnimationClip clip,
        bool shouldLoop
    )
    {
        SerializedObject serializedClip =
            new SerializedObject(
                clip
            );

        SerializedProperty loopProperty =
            serializedClip.FindProperty(
                "m_AnimationClipSettings.m_LoopTime"
            );

        if (loopProperty != null)
        {
            loopProperty.boolValue =
                shouldLoop;

            serializedClip.ApplyModifiedProperties();
        }
    }

    // =========================================================
    // CREATE / UPDATE OVERRIDE CONTROLLER
    // =========================================================

    private AnimatorOverrideController
        CreateOrUpdateOverrideController(
            string outputDirectory,
            Dictionary<string, AnimationClip> generatedClips
        )
    {
        RuntimeAnimatorController baseController =
            monsterType == MonsterType.Melee
                ? baseMeleeController
                : baseRangedController;

        string overridePath =
            outputDirectory +
            "/" +
            monsterName +
            ".overrideController";

        AnimatorOverrideController overrideController =
            AssetDatabase
                .LoadAssetAtPath<AnimatorOverrideController>(
                    overridePath
                );

        // =====================================================
        // CREATE IF NECESSARY
        // =====================================================

        if (overrideController == null)
        {
            overrideController =
                new AnimatorOverrideController();

            overrideController.name =
                monsterName;

            overrideController.runtimeAnimatorController =
                baseController;

            AssetDatabase.CreateAsset(
                overrideController,
                overridePath
            );
        }
        else
        {
            overrideController.runtimeAnimatorController =
                baseController;
        }

        // =====================================================
        // GET BASE OVERRIDES
        // =====================================================

        List<KeyValuePair<AnimationClip, AnimationClip>>
            overrides =
                new List<
                    KeyValuePair<
                        AnimationClip,
                        AnimationClip
                    >
                >(
                    overrideController.overridesCount
                );

        overrideController.GetOverrides(
            overrides
        );

        List<string> unmatchedClips =
            new List<string>();

        int replacedCount =
            0;

        // =====================================================
        // MATCH GENERATED CLIPS
        // =====================================================

        for (int i = 0; i < overrides.Count; i++)
        {
            AnimationClip baseClip =
                overrides[i].Key;

            if (baseClip == null)
            {
                continue;
            }

            AnimationClip generatedClip =
                FindGeneratedClip(
                    baseClip.name,
                    generatedClips
                );

            if (generatedClip != null)
            {
                overrides[i] =
                    new KeyValuePair<
                        AnimationClip,
                        AnimationClip
                    >(
                        baseClip,
                        generatedClip
                    );

                replacedCount++;
            }
            else
            {
                unmatchedClips.Add(
                    baseClip.name
                );
            }
        }

        // =====================================================
        // APPLY ALL OVERRIDES AT ONCE
        // =====================================================

        overrideController.ApplyOverrides(
            overrides
        );

        EditorUtility.SetDirty(
            overrideController
        );

        // =====================================================
        // DEBUG
        // =====================================================

        Debug.Log(
            "[Monster Animation Generator] " +
            monsterName +
            ": " +
            replacedCount +
            " animações vinculadas ao Override Controller."
        );

        if (unmatchedClips.Count > 0)
        {
            Debug.LogWarning(
                "[Monster Animation Generator] " +
                monsterName +
                " - Clips do Base Animator que não foram encontrados:\n" +
                string.Join(
                    "\n",
                    unmatchedClips
                )
            );
        }

        return overrideController;
    }

    // =========================================================
    // FIND CORRESPONDING GENERATED CLIP
    // =========================================================

    private AnimationClip FindGeneratedClip(
        string baseClipName,
        Dictionary<string, AnimationClip> generatedClips
    )
    {
        string normalizedBaseName =
            NormalizeAnimationName(
                baseClipName
            );

        // =====================================================
        // EXACT MATCH
        // =====================================================

        foreach (
            KeyValuePair<string, AnimationClip> pair
            in generatedClips
        )
        {
            string normalizedGeneratedName =
                NormalizeAnimationName(
                    pair.Key
                );

            if (
                normalizedBaseName ==
                normalizedGeneratedName
            )
            {
                return pair.Value;
            }
        }

        // =====================================================
        // ALLOW PREFIXES
        //
        // Example:
        // Goblin_idle_ne
        // BaseGoblin_idle_ne
        // idle_ne
        // =====================================================

        foreach (
            KeyValuePair<string, AnimationClip> pair
            in generatedClips
                .OrderByDescending(
                    item =>
                        NormalizeAnimationName(
                            item.Key
                        ).Length
                )
        )
        {
            string normalizedGeneratedName =
                NormalizeAnimationName(
                    pair.Key
                );

            if (
                normalizedBaseName.EndsWith(
                    normalizedGeneratedName,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return pair.Value;
            }
        }

        return null;
    }

    // =========================================================
    // REGISTER GENERATED CLIP
    // =========================================================

    private void RegisterGeneratedClip(
        Dictionary<string, AnimationClip> generatedClips,
        string animationName,
        AnimationClip clip
    )
    {
        string normalizedName =
            NormalizeAnimationName(
                animationName
            );

        generatedClips[
            normalizedName
        ] = clip;
    }

    // =========================================================
    // NORMALIZE ANIMATION NAME
    // =========================================================

private string NormalizeAnimationName(
    string value
)
{
    if (string.IsNullOrEmpty(value))
    {
        return "";
    }

    string normalized = value
        .ToLowerInvariant()
        .Replace("_", "")
        .Replace("-", "")
        .Replace(" ", "");

    // ==========================================
    // ALIASES
    // ==========================================

    // attack = atk
    normalized =
        normalized.Replace(
            "attack",
            "atk"
        );

    // damage = dmg
    normalized =
        normalized.Replace(
            "damage",
            "dmg"
        );

    return normalized;
}

    // =========================================================
    // LOAD SPRITES
    // =========================================================

    private List<Sprite> LoadSprites(
        Texture2D spriteSheet
    )
    {
        string assetPath =
            AssetDatabase.GetAssetPath(
                spriteSheet
            );

        return AssetDatabase
            .LoadAllAssetsAtPath(
                assetPath
            )
            .OfType<Sprite>()
            .ToList();
    }

    // =========================================================
    // GROUP SPRITES BY ROW
    // =========================================================

    private List<List<Sprite>> GroupSpritesByRows(
        List<Sprite> sprites
    )
    {
        return sprites

            .GroupBy(
                sprite =>
                    Mathf.RoundToInt(
                        sprite.rect.y
                    )
            )

            .OrderByDescending(
                group =>
                    group.Key
            )

            .Select(
                group =>
                    group
                        .OrderBy(
                            sprite =>
                                sprite.rect.x
                        )
                        .ToList()
            )

            .ToList();
    }

    // =========================================================
    // OUTPUT DIRECTORY
    // =========================================================

    private string GetMonsterOutputDirectory()
    {
        string destination =
            NormalizePath(
                destinationFolder
            );

        string cleanMonsterName =
            monsterName.Trim();

        if (
            string.IsNullOrEmpty(
                cleanMonsterName
            )
        )
        {
            return destination +
                "/<MonsterName>";
        }

        return destination +
            "/" +
            cleanMonsterName;
    }

    // =========================================================
    // CREATE FOLDERS
    // =========================================================

    private void EnsureFolderExists(
        string folderPath
    )
    {
        folderPath =
            NormalizePath(
                folderPath
            );

        if (
            AssetDatabase.IsValidFolder(
                folderPath
            )
        )
        {
            return;
        }

        string[] parts =
            folderPath.Split('/');

        string currentPath =
            parts[0];

        for (
            int i = 1;
            i < parts.Length;
            i++
        )
        {
            string nextPath =
                currentPath +
                "/" +
                parts[i];

            if (
                !AssetDatabase.IsValidFolder(
                    nextPath
                )
            )
            {
                AssetDatabase.CreateFolder(
                    currentPath,
                    parts[i]
                );
            }

            currentPath =
                nextPath;
        }
    }

    // =========================================================
    // BROWSE DESTINATION
    // =========================================================

    private void SelectDestinationFolder()
    {
        string selectedFolder =
            EditorUtility.OpenFolderPanel(
                "Select Animation Destination",
                Application.dataPath,
                ""
            );

        if (
            string.IsNullOrEmpty(
                selectedFolder
            )
        )
        {
            return;
        }

        selectedFolder =
            selectedFolder.Replace(
                "\\",
                "/"
            );

        string projectAssetsPath =
            Application.dataPath.Replace(
                "\\",
                "/"
            );

        if (
            !selectedFolder.StartsWith(
                projectAssetsPath,
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            ShowError(
                "A pasta precisa estar dentro de Assets."
            );

            return;
        }

        destinationFolder =
            "Assets" +
            selectedFolder.Substring(
                projectAssetsPath.Length
            );
    }

    // =========================================================
    // NORMALIZE PATH
    // =========================================================

    private string NormalizePath(
        string path
    )
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return "Assets";
        }

        return path
            .Trim()
            .Replace("\\", "/")
            .TrimEnd('/');
    }

    // =========================================================
    // INVALID FOLDER CHARACTERS
    // =========================================================

    private bool ContainsInvalidFolderCharacters(
        string value
    )
    {
        char[] invalidCharacters =
        {
            '/',
            '\\',
            ':',
            '*',
            '?',
            '"',
            '<',
            '>',
            '|'
        };

        return value.IndexOfAny(
            invalidCharacters
        ) >= 0;
    }

    // =========================================================
    // ERROR
    // =========================================================

    private void ShowError(
        string message
    )
    {
        EditorUtility.DisplayDialog(
            "Monster Animation Generator",
            message,
            "OK"
        );
    }
}