#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// One-time importer for the approved AI ruins warden animation frames.
/// It intentionally consumes only the OpenSpec raw-frame output, never the concept or turnaround images.
/// </summary>
internal static class BossArtImportTool
{
    private const string CharacterId = "boss_ai_ruins_warden";
    private const string RawFrameRelativeDirectory = "openspec/changes/produce-totem-art-assets/art/raw/characters/boss_ai_ruins_warden";
    private const string SpriteDirectory = "Assets/Game/Sprite/Actors/BossAIruinsWarden";
    private const string AnimationDirectory = "Assets/Game/Animation/Actors/BossAIruinsWarden";
    private const string ControllerPath = AnimationDirectory + "/BossAIruinsWarden.controller";
    private const string PrefabPath = "Assets/Game/Prefabs/Entity/Actors/Boss.prefab";

    private const int FramePixels = 512;
    // Source processing records the feet at y=480 in top-left image coordinates. Unity pivots use bottom-left coordinates.
    private const float FootBaselineImageY = 480f;
    private const float PixelsPerUnit = 512f;
    private const float FrameRate = 12f;

    private static readonly string[] Actions = { "idle", "walk", "attack", "death" };
    private static readonly string[] Directions = { "down", "up", "left", "right" };

    [MenuItem("Game/Totem/Art/Import Approved Boss AI Ruins Warden")]
    private static void ImportApprovedBoss()
    {
        string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        if (string.IsNullOrWhiteSpace(projectRoot))
        {
            throw new InvalidOperationException("Could not resolve the Unity project root.");
        }

        string rawFrameDirectory = Path.Combine(projectRoot, RawFrameRelativeDirectory);
        ValidateRawFrames(rawFrameDirectory);
        EnsureTargetIsNew();
        EnsureFolder("Assets/Game/Sprite");
        EnsureFolder("Assets/Game/Sprite/Actors");
        EnsureFolder(SpriteDirectory);
        EnsureFolder("Assets/Game/Animation");
        EnsureFolder("Assets/Game/Animation/Actors");
        EnsureFolder(AnimationDirectory);

        try
        {
            AssetDatabase.StartAssetEditing();
            CopyFrames(rawFrameDirectory);
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        ConfigureImportedFrames();
        ValidateImportedFrames();
        var clips = CreateAnimationClips();
        var controller = CreateAnimatorController(clips);
        BindBossPrefab(controller, clips["idle_down"]);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        Debug.Log($"Imported {CharacterId}: 96 frames, 16 clips, and {ControllerPath}. Boss prefab now uses the approved animation frames.");
    }

    [MenuItem("Game/Totem/Art/Rebuild Boss AI Ruins Warden Controller")]
    private static void RebuildController()
    {
        ValidateImportedFrames();
        var oldController = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (oldController == null)
        {
            throw new InvalidOperationException("Boss AnimatorController is missing; use the full approved Boss import instead.");
        }

        if (!AssetDatabase.DeleteAsset(ControllerPath))
        {
            throw new InvalidOperationException("Could not replace the generated Boss AnimatorController.");
        }

        var clips = LoadAnimationClips();
        var controller = CreateAnimatorController(clips);
        BindBossPrefab(controller, clips["idle_down"]);
        AssetDatabase.SaveAssets();
        Debug.Log($"Rebuilt {ControllerPath} with explicit integer Direction states.");
    }

    private static void ValidateRawFrames(string rawFrameDirectory)
    {
        if (!Directory.Exists(rawFrameDirectory))
        {
            throw new DirectoryNotFoundException($"Boss raw-frame directory is missing: {rawFrameDirectory}");
        }

        for (int actionIndex = 0; actionIndex < Actions.Length; actionIndex++)
        {
            string action = Actions[actionIndex];
            int expectedCount = string.Equals(action, "idle", StringComparison.Ordinal) ? 4 : string.Equals(action, "death", StringComparison.Ordinal) ? 8 : 6;
            for (int directionIndex = 0; directionIndex < Directions.Length; directionIndex++)
            {
                string direction = Directions[directionIndex];
                for (int frame = 0; frame < expectedCount; frame++)
                {
                    string fileName = $"{CharacterId}_{action}_{direction}_{frame:00}.png";
                    string path = Path.Combine(rawFrameDirectory, fileName);
                    if (!File.Exists(path))
                    {
                        throw new FileNotFoundException($"Required approved Boss frame is missing: {fileName}", path);
                    }
                }
            }
        }
    }

    private static void EnsureTargetIsNew()
    {
        if (AssetDatabase.IsValidFolder(SpriteDirectory) || AssetDatabase.IsValidFolder(AnimationDirectory) || File.Exists(ControllerPath))
        {
            throw new InvalidOperationException(
                "Boss import destination already exists. This importer deliberately does not overwrite generated runtime art. " +
                "Inspect the existing integration before rerunning it.");
        }
    }

    private static void EnsureFolder(string assetFolder)
    {
        if (AssetDatabase.IsValidFolder(assetFolder))
        {
            return;
        }

        string parent = Path.GetDirectoryName(assetFolder)?.Replace('\\', '/');
        string folderName = Path.GetFileName(assetFolder);
        if (string.IsNullOrWhiteSpace(parent) || string.IsNullOrWhiteSpace(folderName) || !AssetDatabase.IsValidFolder(parent))
        {
            throw new InvalidOperationException($"Cannot create asset folder because its parent is missing: {assetFolder}");
        }

        AssetDatabase.CreateFolder(parent, folderName);
    }

    private static void CopyFrames(string rawFrameDirectory)
    {
        for (int actionIndex = 0; actionIndex < Actions.Length; actionIndex++)
        {
            string action = Actions[actionIndex];
            int frameCount = string.Equals(action, "idle", StringComparison.Ordinal) ? 4 : string.Equals(action, "death", StringComparison.Ordinal) ? 8 : 6;
            for (int directionIndex = 0; directionIndex < Directions.Length; directionIndex++)
            {
                string direction = Directions[directionIndex];
                for (int frame = 0; frame < frameCount; frame++)
                {
                    string fileName = $"{CharacterId}_{action}_{direction}_{frame:00}.png";
                    string source = Path.Combine(rawFrameDirectory, fileName);
                    string assetPath = SpriteDirectory + "/" + fileName;
                    string destination = Path.GetFullPath(assetPath);
                    File.Copy(source, destination, false);
                }
            }
        }
    }

    private static void ConfigureImportedFrames()
    {
        for (int actionIndex = 0; actionIndex < Actions.Length; actionIndex++)
        {
            string action = Actions[actionIndex];
            int frameCount = string.Equals(action, "idle", StringComparison.Ordinal) ? 4 : string.Equals(action, "death", StringComparison.Ordinal) ? 8 : 6;
            for (int directionIndex = 0; directionIndex < Directions.Length; directionIndex++)
            {
                string direction = Directions[directionIndex];
                for (int frame = 0; frame < frameCount; frame++)
                {
                    string assetPath = SpriteDirectory + "/" + $"{CharacterId}_{action}_{direction}_{frame:00}.png";
                    var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                    if (importer == null)
                    {
                        throw new InvalidOperationException($"Failed to resolve TextureImporter for {assetPath}");
                    }

                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.spritePixelsPerUnit = PixelsPerUnit;
                    var spriteSettings = new TextureImporterSettings();
                    importer.ReadTextureSettings(spriteSettings);
                    spriteSettings.spriteAlignment = (int)SpriteAlignment.Custom;
                    spriteSettings.spritePivot = new Vector2(0.5f, (FramePixels - FootBaselineImageY) / FramePixels);
                    importer.SetTextureSettings(spriteSettings);
                    importer.alphaIsTransparency = true;
                    importer.mipmapEnabled = false;
                    importer.wrapMode = TextureWrapMode.Clamp;
                    importer.filterMode = FilterMode.Bilinear;
                    importer.textureCompression = TextureImporterCompression.CompressedHQ;
                    importer.maxTextureSize = FramePixels;
                    importer.SaveAndReimport();
                }
            }
        }
    }

    private static void ValidateImportedFrames()
    {
        const float pivotYPixels = FramePixels - FootBaselineImageY;
        for (int actionIndex = 0; actionIndex < Actions.Length; actionIndex++)
        {
            string action = Actions[actionIndex];
            int frameCount = string.Equals(action, "idle", StringComparison.Ordinal) ? 4 : string.Equals(action, "death", StringComparison.Ordinal) ? 8 : 6;
            for (int directionIndex = 0; directionIndex < Directions.Length; directionIndex++)
            {
                string direction = Directions[directionIndex];
                for (int frame = 0; frame < frameCount; frame++)
                {
                    string assetPath = SpriteDirectory + "/" + $"{CharacterId}_{action}_{direction}_{frame:00}.png";
                    var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                    if (texture == null || sprite == null || texture.width != FramePixels || texture.height != FramePixels)
                    {
                        throw new InvalidOperationException($"Imported Boss frame is not a valid 512×512 Sprite: {assetPath}");
                    }

                    if (!Mathf.Approximately(sprite.pixelsPerUnit, PixelsPerUnit) || !Mathf.Approximately(sprite.pivot.y, pivotYPixels))
                    {
                        throw new InvalidOperationException($"Imported Boss frame has an invalid common foot pivot or PPU: {assetPath}");
                    }
                }
            }
        }
    }

    private static Dictionary<string, AnimationClip> CreateAnimationClips()
    {
        var clips = new Dictionary<string, AnimationClip>(16);
        for (int actionIndex = 0; actionIndex < Actions.Length; actionIndex++)
        {
            string action = Actions[actionIndex];
            int frameCount = string.Equals(action, "idle", StringComparison.Ordinal) ? 4 : string.Equals(action, "death", StringComparison.Ordinal) ? 8 : 6;
            for (int directionIndex = 0; directionIndex < Directions.Length; directionIndex++)
            {
                string direction = Directions[directionIndex];
                string key = action + "_" + direction;
                string clipPath = AnimationDirectory + "/" + CharacterId + "_" + key + ".anim";
                var clip = new AnimationClip { name = CharacterId + "_" + key, frameRate = FrameRate };
                var keyframes = new ObjectReferenceKeyframe[frameCount];
                for (int frame = 0; frame < frameCount; frame++)
                {
                    string framePath = SpriteDirectory + "/" + $"{CharacterId}_{action}_{direction}_{frame:00}.png";
                    keyframes[frame] = new ObjectReferenceKeyframe
                    {
                        time = frame / FrameRate,
                        value = AssetDatabase.LoadAssetAtPath<Sprite>(framePath),
                    };
                }

                var binding = EditorCurveBinding.PPtrCurve(string.Empty, typeof(SpriteRenderer), "m_Sprite");
                AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);
                var settings = AnimationUtility.GetAnimationClipSettings(clip);
                settings.loopTime = !string.Equals(action, "death", StringComparison.Ordinal);
                AnimationUtility.SetAnimationClipSettings(clip, settings);
                AssetDatabase.CreateAsset(clip, clipPath);
                clips.Add(key, clip);
            }
        }

        return clips;
    }

    private static Dictionary<string, AnimationClip> LoadAnimationClips()
    {
        var clips = new Dictionary<string, AnimationClip>(16);
        for (int actionIndex = 0; actionIndex < Actions.Length; actionIndex++)
        {
            string action = Actions[actionIndex];
            for (int directionIndex = 0; directionIndex < Directions.Length; directionIndex++)
            {
                string key = action + "_" + Directions[directionIndex];
                string clipPath = AnimationDirectory + "/" + CharacterId + "_" + key + ".anim";
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
                if (clip == null)
                {
                    throw new InvalidOperationException($"Required Boss animation clip is missing: {clipPath}");
                }

                clips.Add(key, clip);
            }
        }

        return clips;
    }

    private static AnimatorController CreateAnimatorController(Dictionary<string, AnimationClip> clips)
    {
        var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Direction", AnimatorControllerParameterType.Int);
        controller.AddParameter("AttackTrigger", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Die", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Dead", AnimatorControllerParameterType.Bool);

        var stateMachine = controller.layers[0].stateMachine;
        var idleStates = new AnimatorState[Directions.Length];
        var walkStates = new AnimatorState[Directions.Length];
        for (int direction = 0; direction < Directions.Length; direction++)
        {
            string name = Directions[direction];
            idleStates[direction] = stateMachine.AddState("Idle_" + name);
            idleStates[direction].motion = clips["idle_" + name];
            walkStates[direction] = stateMachine.AddState("Walk_" + name);
            walkStates[direction].motion = clips["walk_" + name];
        }

        stateMachine.defaultState = idleStates[0];
        for (int sourceDirection = 0; sourceDirection < Directions.Length; sourceDirection++)
        {
            for (int targetDirection = 0; targetDirection < Directions.Length; targetDirection++)
            {
                AddTransition(
                    idleStates[sourceDirection],
                    walkStates[targetDirection],
                    false,
                    0.05f,
                    AnimatorConditionMode.If,
                    0f,
                    "IsMoving",
                    AnimatorConditionMode.Equals,
                    targetDirection,
                    "Direction");
                AddTransition(
                    walkStates[sourceDirection],
                    idleStates[targetDirection],
                    false,
                    0.05f,
                    AnimatorConditionMode.IfNot,
                    0f,
                    "IsMoving",
                    AnimatorConditionMode.Equals,
                    targetDirection,
                    "Direction");
            }
        }

        for (int direction = 0; direction < Directions.Length; direction++)
        {
            string name = Directions[direction];
            var attackState = stateMachine.AddState("Attack_" + name);
            attackState.motion = clips["attack_" + name];
            AddAnyStateTransition(stateMachine, attackState, AnimatorConditionMode.If, 0f, "AttackTrigger", AnimatorConditionMode.Equals, direction, "Direction");
            AddTransition(attackState, idleStates[direction], true, 0.05f);

            var deathState = stateMachine.AddState("Death_" + name);
            deathState.motion = clips["death_" + name];
            AddAnyStateTransition(stateMachine, deathState, AnimatorConditionMode.If, 0f, "Die", AnimatorConditionMode.Equals, direction, "Direction");
        }

        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static void AddAnyStateTransition(AnimatorStateMachine stateMachine, AnimatorState destination, AnimatorConditionMode firstMode, float firstThreshold, string firstParameter, AnimatorConditionMode secondMode, float secondThreshold, string secondParameter)
    {
        var transition = stateMachine.AddAnyStateTransition(destination);
        transition.hasExitTime = false;
        transition.duration = 0f;
        transition.canTransitionToSelf = false;
        transition.AddCondition(firstMode, firstThreshold, firstParameter);
        transition.AddCondition(secondMode, secondThreshold, secondParameter);
    }

    private static void AddTransition(AnimatorState source, AnimatorState destination, bool hasExitTime, float duration, params object[] conditions)
    {
        var transition = source.AddTransition(destination);
        transition.hasExitTime = hasExitTime;
        transition.exitTime = 1f;
        transition.duration = duration;
        for (int index = 0; index + 2 < conditions.Length; index += 3)
        {
            transition.AddCondition((AnimatorConditionMode)conditions[index], Convert.ToSingle(conditions[index + 1]), (string)conditions[index + 2]);
        }
    }

    private static void BindBossPrefab(AnimatorController controller, AnimationClip idleDown)
    {
        var prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var spriteRenderer = prefabRoot.GetComponent<SpriteRenderer>();
            var animator = prefabRoot.GetComponent<Animator>();
            if (spriteRenderer == null || animator == null)
            {
                throw new InvalidOperationException("Boss prefab must contain SpriteRenderer and Animator components.");
            }

            var firstSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteDirectory + "/" + CharacterId + "_idle_down_00.png");
            if (firstSprite == null || idleDown == null)
            {
                throw new InvalidOperationException("Boss idle-down visual assets could not be loaded for prefab binding.");
            }

            spriteRenderer.sprite = firstSprite;
            animator.runtimeAnimatorController = controller;
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }
}
#endif
