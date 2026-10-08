using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Extraído do MonsterAnimationGeneratorWindow (Sprint 25) pra ser reaproveitado por qualquer
// ferramenta de animação de monstro/boss — hoje também pelo CustomAnimationGeneratorWindow
// (bosses com mecânica própria, fora do padrão fixo idle/walk/attack/dmg/die). Comportamento
// idêntico ao que já existia no gerador original, só de local novo — nenhuma regra mudou.
public static class MonsterAnimationUtility
{
    public enum ClipEventKind
    {
        None,
        Attack,
        Die
    }

    // =========================================================
    // LOAD SPRITES
    // =========================================================

    public static List<Sprite> LoadSprites(Texture2D spriteSheet)
    {
        string assetPath = AssetDatabase.GetAssetPath(spriteSheet);

        return AssetDatabase
            .LoadAllAssetsAtPath(assetPath)
            .OfType<Sprite>()
            .ToList();
    }

    // =========================================================
    // GROUP SPRITES BY ROW
    // =========================================================

    public static List<List<Sprite>> GroupSpritesByRows(List<Sprite> sprites)
    {
        return sprites
            .GroupBy(sprite => Mathf.RoundToInt(sprite.rect.y))
            .OrderByDescending(group => group.Key)
            .Select(group => group.OrderBy(sprite => sprite.rect.x).ToList())
            .ToList();
    }

    // =========================================================
    // CREATE / UPDATE ANIMATION CLIP
    // =========================================================

    public static AnimationClip CreateOrUpdateClip(
        string clipName,
        List<Sprite> sprites,
        string outputDirectory,
        float fps,
        bool shouldLoop,
        ClipEventKind eventKind = ClipEventKind.None
    )
    {
        string clipPath = outputDirectory + "/" + clipName + ".anim";

        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        bool isNewClip = clip == null;

        if (isNewClip)
        {
            clip = new AnimationClip();
            clip.name = clipName;
            AssetDatabase.CreateAsset(clip, clipPath);
        }

        clip.frameRate = fps;

        EditorCurveBinding spriteBinding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = "",
            propertyName = "m_Sprite"
        };

        ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[sprites.Count];
        for (int i = 0; i < sprites.Count; i++)
        {
            keyframes[i] = new ObjectReferenceKeyframe
            {
                time = i / fps,
                value = sprites[i]
            };
        }

        AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding, keyframes);
        SetLoop(clip, shouldLoop);

        // Só na criação — rodar o gerador de novo num monstro que já existe (ex.: depois de
        // recortar a sprite sheet de novo) não pode sobrescrever o frame exato que você já
        // ajustou manualmente num AnimationHitEvent.
        if (isNewClip && eventKind != ClipEventKind.None)
        {
            float clipDuration = sprites.Count / fps;
            ApplyDefaultAnimationEvents(clip, eventKind, clipDuration);
        }

        EditorUtility.SetDirty(clip);
        return clip;
    }

    // =========================================================
    // DEFAULT ANIMATION EVENTS (ATTACK / DIE)
    // =========================================================

    public static void ApplyDefaultAnimationEvents(AnimationClip clip, ClipEventKind eventKind, float clipDuration)
    {
        AnimationEvent[] events;

        if (eventKind == ClipEventKind.Attack)
        {
            events = new[]
            {
                new AnimationEvent { time = clipDuration * 0.5f, functionName = "AnimationHitEvent" },
                new AnimationEvent { time = clipDuration, functionName = "AnimationAttackEndEvent" }
            };
        }
        else
        {
            events = new[]
            {
                new AnimationEvent { time = clipDuration, functionName = "AnimationDieEndEvent" }
            };
        }

        AnimationUtility.SetAnimationEvents(clip, events);
    }

    // =========================================================
    // SINGLE-FRAME CLIP (IDLE COMBAT / IDLE ESTÁTICO)
    // =========================================================

    public static AnimationClip CreateSingleFrameClip(string clipName, Sprite sprite, string outputDirectory, float fps)
    {
        string clipPath = outputDirectory + "/" + clipName + ".anim";

        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        if (clip == null)
        {
            clip = new AnimationClip();
            clip.name = clipName;
            AssetDatabase.CreateAsset(clip, clipPath);
        }

        clip.frameRate = fps;

        EditorCurveBinding spriteBinding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = "",
            propertyName = "m_Sprite"
        };

        ObjectReferenceKeyframe[] keyframes =
        {
            new ObjectReferenceKeyframe { time = 0f, value = sprite },
            new ObjectReferenceKeyframe { time = 1f / fps, value = sprite }
        };

        AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding, keyframes);
        SetLoop(clip, true);

        EditorUtility.SetDirty(clip);
        return clip;
    }

    // =========================================================
    // LOOP
    // =========================================================

    public static void SetLoop(AnimationClip clip, bool shouldLoop)
    {
        SerializedObject serializedClip = new SerializedObject(clip);
        SerializedProperty loopProperty = serializedClip.FindProperty("m_AnimationClipSettings.m_LoopTime");

        if (loopProperty != null)
        {
            loopProperty.boolValue = shouldLoop;
            serializedClip.ApplyModifiedProperties();
        }
    }

    // =========================================================
    // CREATE FOLDERS
    // =========================================================

    public static void EnsureFolderExists(string folderPath)
    {
        folderPath = NormalizePath(folderPath);

        if (AssetDatabase.IsValidFolder(folderPath)) return;

        string[] parts = folderPath.Split('/');
        string currentPath = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string nextPath = currentPath + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(nextPath)) AssetDatabase.CreateFolder(currentPath, parts[i]);
            currentPath = nextPath;
        }
    }

    // =========================================================
    // NORMALIZE PATH
    // =========================================================

    public static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return "Assets";
        return path.Trim().Replace("\\", "/").TrimEnd('/');
    }

    // =========================================================
    // INVALID FOLDER CHARACTERS
    // =========================================================

    public static bool ContainsInvalidFolderCharacters(string value)
    {
        char[] invalidCharacters = { '/', '\\', ':', '*', '?', '"', '<', '>', '|' };
        return value.IndexOfAny(invalidCharacters) >= 0;
    }
}
