using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Sprites;
using UnityEngine;

// D9.1 (Atacar andando) — ferramentas de Editor pros assets do walk_attack. Cada sheet
// "Walk_Attack*" é desenhada pelo usuário com o MESMO layout da sheet de ataque original
// (mesmo nº de sprites, mesma ordem, mesmo tamanho de célula), então tudo aqui trabalha por
// ÍNDICE de sprite (sufixo "_N" do nome), nunca por nome completo — o nome pode mudar
// (ex.: re-fatiamento), o índice não.
//
// Sem janela: tudo é estático e devolve string PASS/FAIL, pra ser chamado via Unity CLI
// (script eval) — mesmo padrão dos testes [ContextMenu] do projeto (RunTelemetryTest).
public static class AttackWalkAssetTools
{
    // Pares (sheet de ataque original -> sheet walk). Cada herói da Parte B do plano
    // acrescenta os próprios pares quando chegar a vez dele.
    public static readonly List<(string original, string walk)> SheetPairs = new List<(string, string)>
    {
        ("Assets/Sprites/Heros/Barbarian/Attack/Minifantasy_TrueHeroesBarbarianAttack.png",
         "Assets/Sprites/Heros/Barbarian/Attack/Walk_Attack.png"),
    };

    // Índice = número depois do último "_" no nome do sprite (padrão do fatiamento da Unity:
    // "<arquivo>_<N>"). -1 se o nome não seguir o padrão.
    public static int ParseSpriteIndex(string spriteName)
    {
        int underscore = spriteName.LastIndexOf('_');
        if (underscore < 0 || underscore == spriteName.Length - 1) return -1;
        return int.TryParse(spriteName.Substring(underscore + 1), out int index) ? index : -1;
    }

    public static Sprite SpriteAtIndex(Texture2D sheet, int index)
    {
        if (sheet == null) return null;
        foreach (var sprite in LoadSprites(AssetDatabase.GetAssetPath(sheet)))
            if (ParseSpriteIndex(sprite.name) == index) return sprite;
        return null;
    }

    private static List<Sprite> LoadSprites(string texturePath)
    {
        var sprites = new List<Sprite>();
        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(texturePath))
            if (asset is Sprite sprite) sprites.Add(sprite);
        return sprites;
    }

    private static Dictionary<int, Sprite> SpritesByIndex(string texturePath, StringBuilder errors)
    {
        var map = new Dictionary<int, Sprite>();
        foreach (var sprite in LoadSprites(texturePath))
        {
            int index = ParseSpriteIndex(sprite.name);
            if (index < 0) { errors.AppendLine($"FAIL: {texturePath} sprite sem índice no nome: {sprite.name}"); continue; }
            if (map.ContainsKey(index)) { errors.AppendLine($"FAIL: {texturePath} índice repetido {index}"); continue; }
            map[index] = sprite;
        }
        return map;
    }

    // heroFilter = trecho do caminho (ex.: "Barbarian") — null valida todos os pares.
    public static string ValidateSheets(string heroFilter = null)
    {
        var errors = new StringBuilder();
        int checkedPairs = 0;

        foreach (var (original, walk) in SheetPairs)
        {
            if (heroFilter != null && !original.Contains(heroFilter) && !walk.Contains(heroFilter)) continue;
            checkedPairs++;
            string pair = $"{System.IO.Path.GetFileName(original)} -> {System.IO.Path.GetFileName(walk)}";

            var originalImporter = AssetImporter.GetAtPath(original) as TextureImporter;
            var walkImporter = AssetImporter.GetAtPath(walk) as TextureImporter;
            if (originalImporter == null || walkImporter == null)
            {
                errors.AppendLine($"FAIL: {pair} importer não encontrado");
                continue;
            }

            Compare(errors, pair, "spriteMode", originalImporter.spriteImportMode, walkImporter.spriteImportMode);
            Compare(errors, pair, "spritePixelsPerUnit", originalImporter.spritePixelsPerUnit, walkImporter.spritePixelsPerUnit);
            Compare(errors, pair, "filterMode", originalImporter.filterMode, walkImporter.filterMode);
            Compare(errors, pair, "textureCompression", originalImporter.textureCompression, walkImporter.textureCompression);

            var originalSprites = SpritesByIndex(original, errors);
            var walkSprites = SpritesByIndex(walk, errors);
            Compare(errors, pair, "spriteCount", originalSprites.Count, walkSprites.Count);

            foreach (var entry in originalSprites)
            {
                if (!walkSprites.TryGetValue(entry.Key, out var walkSprite))
                {
                    errors.AppendLine($"FAIL: {pair} índice {entry.Key} faltando na sheet walk");
                    continue;
                }
                Compare(errors, pair, $"rect[{entry.Key}]", entry.Value.rect, walkSprite.rect);
                Compare(errors, pair, $"pivot[{entry.Key}]", entry.Value.pivot, walkSprite.pivot);
            }
        }

        if (checkedPairs == 0) return $"FAIL: nenhum par encontrado (filtro: {heroFilter})";
        return errors.Length == 0 ? $"PASS ({checkedPairs} pares)" : errors.ToString();
    }

    private static void Compare<T>(StringBuilder errors, string pair, string field, T original, T walk)
    {
        if (!EqualityComparer<T>.Default.Equals(original, walk))
            errors.AppendLine($"FAIL: {pair} {field} {original} != {walk}");
    }

    // Copia o fatiamento (retângulo, pivô, alinhamento, borda) da sheet original pra sheet
    // walk, sprite por sprite, e renomeia os sprites do alvo pra "<arquivoAlvo>_<índice>"
    // mantendo o índice do original. Pra sheet walk fatiada errado (ex.: "Automatic" em vez
    // da grade). Mesmo padrão do RuleTileClonerWindow (API oficial do Sprite Editor).
    public static string CopySlicing(string sourceTexturePath, string targetTexturePath)
    {
        var sourceImporter = AssetImporter.GetAtPath(sourceTexturePath) as TextureImporter;
        var targetImporter = AssetImporter.GetAtPath(targetTexturePath) as TextureImporter;
        if (sourceImporter == null || targetImporter == null) return "FAIL: importer não encontrado";

        var sourceTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(sourceTexturePath);
        var targetTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(targetTexturePath);
        if (sourceTexture.width != targetTexture.width || sourceTexture.height != targetTexture.height)
            return $"FAIL: tamanhos diferentes {sourceTexture.width}x{sourceTexture.height} != {targetTexture.width}x{targetTexture.height}";

        var factory = new SpriteDataProviderFactories();
        factory.Init();

        var sourceProvider = factory.GetSpriteEditorDataProviderFromObject(sourceImporter);
        sourceProvider.InitSpriteEditorDataProvider();
        SpriteRect[] sourceRects = sourceProvider.GetSpriteRects();
        if (sourceRects == null || sourceRects.Length == 0) return "FAIL: original sem recortes";

        targetImporter.textureType = TextureImporterType.Sprite;
        targetImporter.spriteImportMode = SpriteImportMode.Multiple;
        targetImporter.spritePixelsPerUnit = sourceImporter.spritePixelsPerUnit;
        targetImporter.filterMode = sourceImporter.filterMode;
        targetImporter.textureCompression = sourceImporter.textureCompression;
        targetImporter.SaveAndReimport();
        targetImporter = AssetImporter.GetAtPath(targetTexturePath) as TextureImporter;

        var targetProvider = factory.GetSpriteEditorDataProviderFromObject(targetImporter);
        targetProvider.InitSpriteEditorDataProvider();

        string baseName = System.IO.Path.GetFileNameWithoutExtension(targetTexturePath);
        var newRects = new SpriteRect[sourceRects.Length];
        var nameIdPairs = new List<SpriteNameFileIdPair>(sourceRects.Length);
        for (int i = 0; i < sourceRects.Length; i++)
        {
            int index = ParseSpriteIndex(sourceRects[i].name);
            if (index < 0) return $"FAIL: sprite original sem índice no nome: {sourceRects[i].name}";
            newRects[i] = new SpriteRect
            {
                name = $"{baseName}_{index}",
                spriteID = GUID.Generate(),
                rect = sourceRects[i].rect,
                pivot = sourceRects[i].pivot,
                alignment = sourceRects[i].alignment,
                border = sourceRects[i].border,
            };
            nameIdPairs.Add(new SpriteNameFileIdPair(newRects[i].name, newRects[i].spriteID));
        }

        targetProvider.SetSpriteRects(newRects);
        targetProvider.GetDataProvider<ISpriteNameFileIdDataProvider>()?.SetNameFileIdPairs(nameIdPairs);
        targetProvider.Apply();
        targetImporter.SaveAndReimport();
        return $"OK: {newRects.Length} sprites copiados";
    }

    // ===================== Clipes walk_* =====================

    public static string WalkClipPath(string sourceClipPath) =>
        System.IO.Path.Combine(System.IO.Path.GetDirectoryName(sourceClipPath),
            "walk_" + System.IO.Path.GetFileName(sourceClipPath)).Replace(System.IO.Path.DirectorySeparatorChar, '/');

    private static EditorCurveBinding? SpriteBinding(AnimationClip clip)
    {
        foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
            if (binding.propertyName == "m_Sprite") return binding;
        return null;
    }

    // Confere cada par original/walk: frameRate, length, loop, keys de sprite (quantidade,
    // tempos, índice do sprite vindo da sheet walk pareada) e eventos (função + frame).
    public static string ValidateDerivedClips(string heroFolder, string[] clipNames)
    {
        var errors = new StringBuilder();
        foreach (var clipName in clipNames)
        {
            string sourcePath = $"{heroFolder}/{clipName}.anim";
            var source = AssetDatabase.LoadAssetAtPath<AnimationClip>(sourcePath);
            if (source == null) { errors.AppendLine($"FAIL: original não encontrado {sourcePath}"); continue; }
            var walk = AssetDatabase.LoadAssetAtPath<AnimationClip>(WalkClipPath(sourcePath));
            if (walk == null) { errors.AppendLine($"FAIL: walk faltando pra {clipName}"); continue; }

            Compare(errors, clipName, "frameRate", source.frameRate, walk.frameRate);
            Compare(errors, clipName, "length", source.length, walk.length);
            Compare(errors, clipName, "isLooping", source.isLooping, walk.isLooping);

            var sourceBinding = SpriteBinding(source);
            var walkBinding = SpriteBinding(walk);
            if (sourceBinding == null || walkBinding == null) { errors.AppendLine($"FAIL: {clipName} sem curva de sprite"); continue; }
            var sourceKeys = AnimationUtility.GetObjectReferenceCurve(source, sourceBinding.Value);
            var walkKeys = AnimationUtility.GetObjectReferenceCurve(walk, walkBinding.Value);
            Compare(errors, clipName, "spriteKeys", sourceKeys.Length, walkKeys.Length);
            for (int i = 0; i < Mathf.Min(sourceKeys.Length, walkKeys.Length); i++)
            {
                Compare(errors, clipName, $"keyTime[{i}]", sourceKeys[i].time, walkKeys[i].time);
                var sourceSprite = sourceKeys[i].value as Sprite;
                var walkSprite = walkKeys[i].value as Sprite;
                if (sourceSprite == null || walkSprite == null) { errors.AppendLine($"FAIL: {clipName} key {i} sem sprite"); continue; }
                Compare(errors, clipName, $"spriteIndex[{i}]", ParseSpriteIndex(sourceSprite.name), ParseSpriteIndex(walkSprite.name));
                string expectedWalkSheet = WalkSheetFor(AssetDatabase.GetAssetPath(sourceSprite));
                Compare(errors, clipName, $"spriteSheet[{i}]", expectedWalkSheet, AssetDatabase.GetAssetPath(walkSprite));
            }

            var sourceEvents = AnimationUtility.GetAnimationEvents(source);
            var walkEvents = AnimationUtility.GetAnimationEvents(walk);
            Compare(errors, clipName, "events", sourceEvents.Length, walkEvents.Length);
            for (int i = 0; i < Mathf.Min(sourceEvents.Length, walkEvents.Length); i++)
            {
                Compare(errors, clipName, $"event[{i}].function", sourceEvents[i].functionName, walkEvents[i].functionName);
                Compare(errors, clipName, $"event[{i}].frame",
                    Mathf.RoundToInt(sourceEvents[i].time * source.frameRate), Mathf.RoundToInt(walkEvents[i].time * walk.frameRate));
            }

            CheckSpriteReferencesOnDisk(errors, clipName, WalkClipPath(sourcePath));
        }
        return errors.Length == 0 ? $"PASS ({clipNames.Length} clipes)" : errors.ToString();
    }

    // Bug real (2026-11-19): o usuário re-fatiou a sheet walk depois dos clipes gerados — os
    // sprites ganharam internalID novo, mas o Editor continuou resolvendo os antigos pela
    // memória (validação acima passava, jogo funcionava), enquanto o .anim no disco apontava
    // pra IDs que não existiam mais (quebraria ao reabrir o Unity). Por isso confere o TEXTO
    // do .anim contra o .meta da textura, sem passar pelo AssetDatabase.
    private static readonly System.Text.RegularExpressions.Regex SpriteReferencePattern =
        new System.Text.RegularExpressions.Regex(@"fileID: (-?\d+), guid: ([0-9a-f]{32}), type: 3");

    private static void CheckSpriteReferencesOnDisk(StringBuilder errors, string clipName, string clipPath)
    {
        var metaCache = new Dictionary<string, string>();
        foreach (System.Text.RegularExpressions.Match match in SpriteReferencePattern.Matches(System.IO.File.ReadAllText(clipPath)))
        {
            string texturePath = AssetDatabase.GUIDToAssetPath(match.Groups[2].Value);
            if (!texturePath.EndsWith(".png")) continue;
            if (!metaCache.TryGetValue(texturePath, out string meta))
            {
                meta = System.IO.File.ReadAllText(texturePath + ".meta");
                metaCache[texturePath] = meta;
            }
            if (!meta.Contains($"internalID: {match.Groups[1].Value}\n") && !meta.Contains($"internalID: {match.Groups[1].Value}\r\n"))
                errors.AppendLine($"FAIL: {clipName} referencia sprite {match.Groups[1].Value} que não existe mais em {System.IO.Path.GetFileName(texturePath)} (no disco)");
        }
    }

    // null quando a sheet não tem par cadastrado em SheetPairs.
    public static string WalkSheetFor(string originalSheetPath)
    {
        foreach (var (original, walk) in SheetPairs)
            if (original == originalSheetPath) return walk;
        return null;
    }

    // Cria (ou sobrescreve, mantendo o GUID) "walk_<nome>.anim" na mesma pasta do original:
    // mesmos tempos/curvas/eventos/configurações, só o sprite de cada key trocado pelo de
    // mesmo índice na sheet walk pareada. Eventos iguais = golpe sai no mesmo frame.
    public static AnimationClip DeriveWalkClip(AnimationClip source)
    {
        string sourcePath = AssetDatabase.GetAssetPath(source);
        var clip = new AnimationClip { frameRate = source.frameRate };
        AnimationUtility.SetAnimationClipSettings(clip, AnimationUtility.GetAnimationClipSettings(source));

        foreach (var binding in AnimationUtility.GetCurveBindings(source))
            AnimationUtility.SetEditorCurve(clip, binding, AnimationUtility.GetEditorCurve(source, binding));

        foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(source))
        {
            var keys = AnimationUtility.GetObjectReferenceCurve(source, binding);
            if (binding.propertyName == "m_Sprite")
            {
                for (int i = 0; i < keys.Length; i++)
                {
                    var sprite = keys[i].value as Sprite;
                    if (sprite == null) throw new System.InvalidOperationException($"{source.name}: key {i} sem sprite");
                    string walkSheetPath = WalkSheetFor(AssetDatabase.GetAssetPath(sprite));
                    if (walkSheetPath == null)
                        throw new System.InvalidOperationException($"{source.name}: sheet sem par walk ({AssetDatabase.GetAssetPath(sprite)})");
                    var walkSprite = SpriteAtIndex(AssetDatabase.LoadAssetAtPath<Texture2D>(walkSheetPath), ParseSpriteIndex(sprite.name));
                    if (walkSprite == null)
                        throw new System.InvalidOperationException($"{source.name}: índice {ParseSpriteIndex(sprite.name)} faltando em {walkSheetPath}");
                    keys[i].value = walkSprite;
                }
            }
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
        }

        AnimationUtility.SetAnimationEvents(clip, AnimationUtility.GetAnimationEvents(source));

        string walkPath = WalkClipPath(sourcePath);
        var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(walkPath);
        clip.name = System.IO.Path.GetFileNameWithoutExtension(walkPath);
        if (existing != null)
        {
            EditorUtility.CopySerialized(clip, existing);
            EditorUtility.SetDirty(existing);
            Object.DestroyImmediate(clip);
            clip = existing;
        }
        else
        {
            AssetDatabase.CreateAsset(clip, walkPath);
        }
        AssetDatabase.SaveAssets();
        return clip;
    }

    public static string DeriveForHero(string heroFolder, string[] clipNames)
    {
        int count = 0;
        foreach (var clipName in clipNames)
        {
            var source = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{heroFolder}/{clipName}.anim");
            if (source == null) return $"FAIL: original não encontrado {heroFolder}/{clipName}.anim";
            DeriveWalkClip(source);
            count++;
        }
        return $"Derivados: {count}";
    }

    // ===================== Estado gêmeo "<Estado>_Walk" =====================

    public const string WalkTwinSuffix = "_Walk";

    private static AnimatorState FindState(AnimatorController controller, string stateName)
    {
        foreach (var child in controller.layers[0].stateMachine.states)
            if (child.state.name == stateName) return child.state;
        return null;
    }

    // Destino de uma transição do gêmeo: igual ao do parado, exceto a transição pra ele
    // mesmo (ex.: Attack_Continuous em loop), que no gêmeo aponta pro próprio gêmeo.
    private static AnimatorState MapDestination(AnimatorState destination, AnimatorState stand, AnimatorState twin) =>
        destination == stand ? twin : destination;

    public static string ValidateTwin(AnimatorController controller, string standStateName)
    {
        var errors = new StringBuilder();
        string label = $"{controller.name}/{standStateName}";
        var stand = FindState(controller, standStateName);
        if (stand == null) return $"FAIL: {label} estado parado não existe";
        var twin = FindState(controller, standStateName + WalkTwinSuffix);
        if (twin == null) return $"FAIL: {label} gêmeo {standStateName + WalkTwinSuffix} não existe";

        Compare(errors, label, "speed", stand.speed, twin.speed);
        Compare(errors, label, "speedParameterActive", stand.speedParameterActive, twin.speedParameterActive);
        Compare(errors, label, "speedParameter", stand.speedParameter, twin.speedParameter);
        Compare(errors, label, "tag", stand.tag, twin.tag);
        Compare(errors, label, "writeDefaultValues", stand.writeDefaultValues, twin.writeDefaultValues);

        if (stand.motion is BlendTree standTree)
        {
            if (!(twin.motion is BlendTree twinTree)) errors.AppendLine($"FAIL: {label} gêmeo sem BlendTree");
            else
            {
                Compare(errors, label, "blendType", standTree.blendType, twinTree.blendType);
                Compare(errors, label, "blendParameter", standTree.blendParameter, twinTree.blendParameter);
                Compare(errors, label, "blendParameterY", standTree.blendParameterY, twinTree.blendParameterY);
                Compare(errors, label, "children", standTree.children.Length, twinTree.children.Length);
                for (int i = 0; i < Mathf.Min(standTree.children.Length, twinTree.children.Length); i++)
                {
                    var s = standTree.children[i];
                    var t = twinTree.children[i];
                    Compare(errors, label, $"child[{i}].position", s.position, t.position);
                    Compare(errors, label, $"child[{i}].motion", s.motion != null ? "walk_" + s.motion.name : null, t.motion != null ? t.motion.name : null);
                }
            }
        }
        else
        {
            Compare(errors, label, "motion", stand.motion != null ? "walk_" + stand.motion.name : null, twin.motion != null ? twin.motion.name : null);
        }

        Compare(errors, label, "transitions", stand.transitions.Length, twin.transitions.Length);
        for (int i = 0; i < Mathf.Min(stand.transitions.Length, twin.transitions.Length); i++)
        {
            var s = stand.transitions[i];
            var t = twin.transitions[i];
            var expected = MapDestination(s.destinationState, stand, twin);
            Compare(errors, label, $"transition[{i}].destination", expected != null ? expected.name : "exit", t.destinationState != null ? t.destinationState.name : "exit");
            Compare(errors, label, $"transition[{i}].hasExitTime", s.hasExitTime, t.hasExitTime);
            Compare(errors, label, $"transition[{i}].exitTime", s.exitTime, t.exitTime);
            Compare(errors, label, $"transition[{i}].duration", s.duration, t.duration);
            Compare(errors, label, $"transition[{i}].conditions", s.conditions.Length, t.conditions.Length);
        }

        return errors.Length == 0 ? $"PASS ({label})" : errors.ToString();
    }

    // Cria "<standStateName>_Walk" ao lado do estado parado: mesma configuração, BlendTree
    // espelhada com os clipes walk_*, mesmas saídas. Sem transição de AnyState — o gêmeo só é
    // alcançado pela troca em runtime (HeroController, Animator.Play no mesmo normalizedTime).
    // Idempotente: se o gêmeo já existe, ele é recriado do zero.
    public static string AddWalkTwin(AnimatorController controller, string standStateName)
    {
        var stateMachine = controller.layers[0].stateMachine;
        var stand = FindState(controller, standStateName);
        if (stand == null) return $"FAIL: {controller.name}/{standStateName} não existe";

        string twinName = standStateName + WalkTwinSuffix;
        var old = FindState(controller, twinName);
        Vector3 position = Vector3.zero;
        bool hasOldPosition = false;
        if (old != null)
        {
            foreach (var child in stateMachine.states)
                if (child.state == old) { position = child.position; hasOldPosition = true; }
            if (old.motion is BlendTree oldTree && AssetDatabase.GetAssetPath(oldTree) == AssetDatabase.GetAssetPath(controller))
            {
                AssetDatabase.RemoveObjectFromAsset(oldTree);
                Object.DestroyImmediate(oldTree, true);
            }
            stateMachine.RemoveState(old);
        }
        if (!hasOldPosition)
        {
            foreach (var child in stateMachine.states)
                if (child.state == stand) position = child.position + new Vector3(0f, 60f, 0f);
        }

        var twin = stateMachine.AddState(twinName, position);
        twin.speed = stand.speed;
        twin.speedParameterActive = stand.speedParameterActive;
        twin.speedParameter = stand.speedParameter;
        twin.cycleOffset = stand.cycleOffset;
        twin.cycleOffsetParameterActive = stand.cycleOffsetParameterActive;
        twin.cycleOffsetParameter = stand.cycleOffsetParameter;
        twin.mirror = stand.mirror;
        twin.mirrorParameterActive = stand.mirrorParameterActive;
        twin.mirrorParameter = stand.mirrorParameter;
        twin.tag = stand.tag;
        twin.writeDefaultValues = stand.writeDefaultValues;
        twin.iKOnFeet = stand.iKOnFeet;

        if (stand.motion is BlendTree standTree)
        {
            var twinTree = new BlendTree
            {
                name = twinName,
                blendType = standTree.blendType,
                blendParameter = standTree.blendParameter,
                blendParameterY = standTree.blendParameterY,
                minThreshold = standTree.minThreshold,
                maxThreshold = standTree.maxThreshold,
                useAutomaticThresholds = standTree.useAutomaticThresholds,
                hideFlags = HideFlags.HideInHierarchy,
            };
            AssetDatabase.AddObjectToAsset(twinTree, controller);
            var children = standTree.children;
            for (int i = 0; i < children.Length; i++)
            {
                var walkMotion = WalkMotionFor(children[i].motion);
                if (walkMotion == null) return $"FAIL: clipe walk faltando pra {children[i].motion?.name}";
                children[i].motion = walkMotion;
            }
            twinTree.children = children;
            twin.motion = twinTree;
        }
        else
        {
            var walkMotion = WalkMotionFor(stand.motion);
            if (walkMotion == null) return $"FAIL: clipe walk faltando pra {stand.motion?.name}";
            twin.motion = walkMotion;
        }

        foreach (var source in stand.transitions)
        {
            AnimatorStateTransition copy;
            if (source.isExit) copy = twin.AddExitTransition();
            else if (source.destinationStateMachine != null) copy = twin.AddTransition(source.destinationStateMachine);
            else copy = twin.AddTransition(MapDestination(source.destinationState, stand, twin));
            copy.hasExitTime = source.hasExitTime;
            copy.exitTime = source.exitTime;
            copy.hasFixedDuration = source.hasFixedDuration;
            copy.duration = source.duration;
            copy.offset = source.offset;
            copy.interruptionSource = source.interruptionSource;
            copy.orderedInterruption = source.orderedInterruption;
            copy.canTransitionToSelf = source.canTransitionToSelf;
            copy.mute = source.mute;
            copy.solo = source.solo;
            foreach (var condition in source.conditions)
                copy.AddCondition(condition.mode, condition.threshold, condition.parameter);
        }

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        return $"OK: {controller.name}/{twinName}";
    }

    private static Motion WalkMotionFor(Motion standMotion)
    {
        if (standMotion == null) return null;
        string path = AssetDatabase.GetAssetPath(standMotion);
        return AssetDatabase.LoadAssetAtPath<AnimationClip>(WalkClipPath(path));
    }
}
