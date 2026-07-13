#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// One-time importer for the approved Actor Common M02 sprite animation output.
/// It only consumes the reviewed OpenSpec cut frames and deliberately refuses to overwrite an existing integration.
/// </summary>
internal static class ActorCommonM02ArtImportTool
{
    private const string CharacterId = "actor_common_m02";
    private const string RawFrameRelativeDirectory = "openspec/changes/produce-totem-art-assets/art/raw/characters/actor_common_m02";
    private const string SpriteDirectory = "Assets/Game/Sprite/Actors/ActorCommonM02";
    private const string AnimationDirectory = "Assets/Game/Animation/Actors/ActorCommonM02";
    private const string ControllerPath = AnimationDirectory + "/ActorCommonM02.controller";
    private const string PlayerPrefabPath = "Assets/Game/Prefabs/Entity/Actors/Player.prefab";
    private const string SmartAiPrefabPath = "Assets/Game/Prefabs/Entity/Actors/SmartAI.prefab";
    private const string LightAiPrefabPath = "Assets/Game/Prefabs/Entity/Actors/LightAI.prefab";

    private const int FramePixels = 512;
    private const int FirstFrameNumber = 1;
    // The source process uses top-left image coordinates; Sprite pivots use bottom-left coordinates.
    private const float FootBaselineImageY = 511f;
    private const float PixelsPerUnit = 512f;
    private const float FrameRate = 12f;

    private static readonly string[] Actions = { "idle", "walk", "attack", "death" };
    private static readonly string[] Directions = { "down", "up", "left", "right" };
    private static readonly string[] ParticipantPrefabPaths = { PlayerPrefabPath, SmartAiPrefabPath, LightAiPrefabPath };

    [MenuItem("Game/Totem/Art/Import Actor Common M02")]
    private static void ImportActorCommonM02()
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
        Dictionary<string, AnimationClip> clips = CreateAnimationClips();
        AnimatorController controller = CreateAnimatorController(clips);
        BindParticipantPrefabs(controller, clips["idle_down"]);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        Debug.Log($"Imported {CharacterId}: 96 frames, 16 clips, and {ControllerPath}. Player, SmartAI, and LightAI share this visual controller.");
    }

    [MenuItem("Game/Totem/Art/Validate Actor Common M02 Import")]
    private static void ValidateImportedIntegration()
    {
        ValidateImportedFrames();
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            throw new InvalidOperationException("Actor Common M02 AnimatorController is missing.");
        }

        ValidateControllerParameters(controller);
        for (int i = 0; i < ParticipantPrefabPaths.Length; i++)
        {
            ValidateParticipantPrefab(ParticipantPrefabPaths[i], controller);
        }

        Debug.Log("Actor Common M02 import validation passed: 96 Sprites, 16 clips, shared controller, and three participant prefabs.");
    }

    private static void ValidateRawFrames(string rawFrameDirectory)
    {
        if (!Directory.Exists(rawFrameDirectory))
        {
            throw new DirectoryNotFoundException("Actor Common M02 raw-frame directory is missing: " + rawFrameDirectory);
        }

        for (int actionIndex = 0; actionIndex < Actions.Length; actionIndex++)
        {
            string action = Actions[actionIndex];
            int frameCount = GetFrameCount(action);
            for (int directionIndex = 0; directionIndex < Directions.Length; directionIndex++)
            {
                string direction = Directions[directionIndex];
                for (int frame = FirstFrameNumber; frame < FirstFrameNumber + frameCount; frame++)
                {
                    string fileName = GetFrameName(action, direction, frame);
                    string path = Path.Combine(rawFrameDirectory, fileName);
                    if (!File.Exists(path))
                    {
                        throw new FileNotFoundException("Required Actor Common M02 frame is missing: " + fileName, path);
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
                "Actor Common M02 import destination already exists. This importer deliberately does not overwrite generated runtime art.");
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
            throw new InvalidOperationException("Cannot create asset folder because its parent is missing: " + assetFolder);
        }

        AssetDatabase.CreateFolder(parent, folderName);
    }

    private static void CopyFrames(string rawFrameDirectory)
    {
        for (int actionIndex = 0; actionIndex < Actions.Length; actionIndex++)
        {
            string action = Actions[actionIndex];
            int frameCount = GetFrameCount(action);
            for (int directionIndex = 0; directionIndex < Directions.Length; directionIndex++)
            {
                string direction = Directions[directionIndex];
                for (int frame = FirstFrameNumber; frame < FirstFrameNumber + frameCount; frame++)
                {
                    string fileName = GetFrameName(action, direction, frame);
                    string source = Path.Combine(rawFrameDirectory, fileName);
                    string destination = Path.GetFullPath(SpriteDirectory + "/" + fileName);
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
            int frameCount = GetFrameCount(action);
            for (int directionIndex = 0; directionIndex < Directions.Length; directionIndex++)
            {
                string direction = Directions[directionIndex];
                for (int frame = FirstFrameNumber; frame < FirstFrameNumber + frameCount; frame++)
                {
                    string assetPath = SpriteDirectory + "/" + GetFrameName(action, direction, frame);
                    var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                    if (importer == null)
                    {
                        throw new InvalidOperationException("Failed to resolve TextureImporter for " + assetPath);
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
        const float PivotYPixels = FramePixels - FootBaselineImageY;
        for (int actionIndex = 0; actionIndex < Actions.Length; actionIndex++)
        {
            string action = Actions[actionIndex];
            int frameCount = GetFrameCount(action);
            for (int directionIndex = 0; directionIndex < Directions.Length; directionIndex++)
            {
                string direction = Directions[directionIndex];
                for (int frame = FirstFrameNumber; frame < FirstFrameNumber + frameCount; frame++)
                {
                    string assetPath = SpriteDirectory + "/" + GetFrameName(action, direction, frame);
                    Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                    Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                    if (texture == null || sprite == null || texture.width != FramePixels || texture.height != FramePixels)
                    {
                        throw new InvalidOperationException("Imported Actor Common M02 frame is not a valid 512x512 Sprite: " + assetPath);
                    }

                    if (!Mathf.Approximately(sprite.pixelsPerUnit, PixelsPerUnit) || !Mathf.Approximately(sprite.pivot.y, PivotYPixels))
                    {
                        throw new InvalidOperationException("Imported Actor Common M02 frame has an invalid shared foot pivot or PPU: " + assetPath);
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
            int frameCount = GetFrameCount(action);
            for (int directionIndex = 0; directionIndex < Directions.Length; directionIndex++)
            {
                string direction = Directions[directionIndex];
                string key = action + "_" + direction;
                string clipPath = AnimationDirectory + "/" + CharacterId + "_" + key + ".anim";
                var clip = new AnimationClip { name = CharacterId + "_" + key, frameRate = FrameRate };
                var keyframes = new ObjectReferenceKeyframe[frameCount];
                for (int index = 0; index < frameCount; index++)
                {
                    int frame = FirstFrameNumber + index;
                    string framePath = SpriteDirectory + "/" + GetFrameName(action, direction, frame);
                    keyframes[index] = new ObjectReferenceKeyframe
                    {
                        time = index / FrameRate,
                        value = AssetDatabase.LoadAssetAtPath<Sprite>(framePath),
                    };
                }

                AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve(string.Empty, typeof(SpriteRenderer), "m_Sprite"), keyframes);
                AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
                settings.loopTime = !string.Equals(action, "death", StringComparison.Ordinal);
                AnimationUtility.SetAnimationClipSettings(clip, settings);
                AssetDatabase.CreateAsset(clip, clipPath);
                clips.Add(key, clip);
            }
        }

        return clips;
    }

    private static AnimatorController CreateAnimatorController(Dictionary<string, AnimationClip> clips)
    {
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Direction", AnimatorControllerParameterType.Int);
        controller.AddParameter("AttackTrigger", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Die", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Dead", AnimatorControllerParameterType.Bool);

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
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
                AddTransition(idleStates[sourceDirection], walkStates[targetDirection], false, 0.05f,
                    AnimatorConditionMode.If, 0f, "IsMoving",
                    AnimatorConditionMode.Equals, targetDirection, "Direction");
                AddTransition(walkStates[sourceDirection], idleStates[targetDirection], false, 0.05f,
                    AnimatorConditionMode.IfNot, 0f, "IsMoving",
                    AnimatorConditionMode.Equals, targetDirection, "Direction");
            }
        }

        for (int direction = 0; direction < Directions.Length; direction++)
        {
            string name = Directions[direction];
            AnimatorState attackState = stateMachine.AddState("Attack_" + name);
            attackState.motion = clips["attack_" + name];
            AddAnyStateTransition(stateMachine, attackState, AnimatorConditionMode.If, 0f, "AttackTrigger", AnimatorConditionMode.Equals, direction, "Direction");
            AddTransition(attackState, idleStates[direction], true, 0.05f);

            AnimatorState deathState = stateMachine.AddState("Death_" + name);
            deathState.motion = clips["death_" + name];
            AddAnyStateTransition(stateMachine, deathState, AnimatorConditionMode.If, 0f, "Die", AnimatorConditionMode.Equals, direction, "Direction");
        }

        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static void AddAnyStateTransition(AnimatorStateMachine stateMachine, AnimatorState destination, AnimatorConditionMode firstMode, float firstThreshold, string firstParameter, AnimatorConditionMode secondMode, float secondThreshold, string secondParameter)
    {
        AnimatorStateTransition transition = stateMachine.AddAnyStateTransition(destination);
        transition.hasExitTime = false;
        transition.duration = 0f;
        transition.canTransitionToSelf = false;
        transition.AddCondition(firstMode, firstThreshold, firstParameter);
        transition.AddCondition(secondMode, secondThreshold, secondParameter);
    }

    private static void AddTransition(AnimatorState source, AnimatorState destination, bool hasExitTime, float duration, params object[] conditions)
    {
        AnimatorStateTransition transition = source.AddTransition(destination);
        transition.hasExitTime = hasExitTime;
        transition.exitTime = 1f;
        transition.duration = duration;
        for (int index = 0; index + 2 < conditions.Length; index += 3)
        {
            transition.AddCondition((AnimatorConditionMode)conditions[index], Convert.ToSingle(conditions[index + 1]), (string)conditions[index + 2]);
        }
    }

    private static void BindParticipantPrefabs(AnimatorController controller, AnimationClip idleDown)
    {
        Sprite firstSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteDirectory + "/" + GetFrameName("idle", "down", FirstFrameNumber));
        if (controller == null || idleDown == null || firstSprite == null)
        {
            throw new InvalidOperationException("Actor Common M02 visual assets could not be loaded for prefab binding.");
        }

        for (int index = 0; index < ParticipantPrefabPaths.Length; index++)
        {
            string prefabPath = ParticipantPrefabPaths[index];
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                SpriteRenderer spriteRenderer = prefabRoot.GetComponent<SpriteRenderer>();
                Animator animator = prefabRoot.GetComponent<Animator>();
                if (spriteRenderer == null || animator == null)
                {
                    throw new InvalidOperationException("Participant prefab requires SpriteRenderer and Animator components: " + prefabPath);
                }

                spriteRenderer.sprite = firstSprite;
                spriteRenderer.color = Color.white;
                animator.runtimeAnimatorController = controller;
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }
    }

    private static void ValidateParticipantPrefab(string prefabPath, AnimatorController controller)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        SpriteRenderer spriteRenderer = prefab == null ? null : prefab.GetComponent<SpriteRenderer>();
        Animator animator = prefab == null ? null : prefab.GetComponent<Animator>();
        if (spriteRenderer == null || spriteRenderer.sprite == null || animator == null || animator.runtimeAnimatorController != controller || spriteRenderer.color != Color.white)
        {
            throw new InvalidOperationException("Actor Common M02 prefab binding is invalid: " + prefabPath);
        }
    }

    private static void ValidateControllerParameters(AnimatorController controller)
    {
        AnimatorControllerParameter[] parameters = controller.parameters;
        if (parameters.Length != 5
            || !HasParameter(parameters, "IsMoving", AnimatorControllerParameterType.Bool)
            || !HasParameter(parameters, "Direction", AnimatorControllerParameterType.Int)
            || !HasParameter(parameters, "AttackTrigger", AnimatorControllerParameterType.Trigger)
            || !HasParameter(parameters, "Die", AnimatorControllerParameterType.Trigger)
            || !HasParameter(parameters, "Dead", AnimatorControllerParameterType.Bool))
        {
            throw new InvalidOperationException("Actor Common M02 controller parameters do not match TotemActorService.");
        }
    }

    private static bool HasParameter(AnimatorControllerParameter[] parameters, string name, AnimatorControllerParameterType type)
    {
        for (int index = 0; index < parameters.Length; index++)
        {
            if (parameters[index].name == name && parameters[index].type == type)
            {
                return true;
            }
        }

        return false;
    }

    private static int GetFrameCount(string action)
    {
        return string.Equals(action, "idle", StringComparison.Ordinal) ? 4 : string.Equals(action, "death", StringComparison.Ordinal) ? 8 : 6;
    }

    private static string GetFrameName(string action, string direction, int frame)
    {
        return CharacterId + "_" + action + "_" + direction + "_" + frame.ToString("00") + ".png";
    }
}
#endif
