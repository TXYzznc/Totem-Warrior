#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Builds an isolated, Transform-cutout preview for the skeletal M02 pipeline.
/// It never edits Player, SmartAI, LightAI, or the legacy frame-animation controller.
/// </summary>
internal static class ActorCommonM02SkeletalImportTool
{
    private const string SpriteDirectory = "Assets/Game/Sprite/Actors/ActorCommonM02Skeletal/Down";
    private const string AnimationDirectory = "Assets/Game/Animation/Actors/ActorCommonM02Skeletal";
    private const string PrefabDirectory = "Assets/Game/Prefabs/Entity/Actors/Preview";
    private const string ControllerPath = AnimationDirectory + "/ActorCommonM02Skeletal.controller";
    private const string PreviewPrefabPath = PrefabDirectory + "/PlayerSkeletalPreview.prefab";
    private const float PixelsPerUnit = 256f;

    private static readonly string[] RequiredParts =
    {
        "head_neck", "torso_skin", "vest_overlay", "left_upper_arm", "left_lower_arm_hand",
        "right_upper_arm", "right_lower_arm_hand", "pelvis_shorts", "left_leg", "left_foot", "right_leg",
    };

    [MenuItem("Game/Totem/Art/Build M02 Skeletal Preview")]
    private static void BuildPreview()
    {
        EnsureFolder("Assets/Game/Sprite");
        EnsureFolder("Assets/Game/Sprite/Actors");
        EnsureFolder("Assets/Game/Sprite/Actors/ActorCommonM02Skeletal");
        EnsureFolder(SpriteDirectory);
        EnsureFolder("Assets/Game/Animation");
        EnsureFolder("Assets/Game/Animation/Actors");
        EnsureFolder(AnimationDirectory);
        EnsureFolder("Assets/Game/Prefabs");
        EnsureFolder("Assets/Game/Prefabs/Entity");
        EnsureFolder("Assets/Game/Prefabs/Entity/Actors");
        EnsureFolder(PrefabDirectory);

        Dictionary<string, Sprite> sprites = LoadAndConfigureSprites();
        ValidateSprites(sprites);
        AnimatorController controller = CreateController();

        GameObject preview = CreatePreviewObject(sprites, controller);
        try
        {
            PrefabUtility.SaveAsPrefabAsset(preview, PreviewPrefabPath);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(preview);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Debug.Log("Built isolated M02 skeletal preview at " + PreviewPrefabPath + ". Legacy frame assets were not modified.");
    }

    [MenuItem("Game/Totem/Art/Validate M02 Skeletal Preview")]
    private static void ValidatePreview()
    {
        ValidateLegacyContract();
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PreviewPrefabPath);
        if (prefab == null)
        {
            throw new InvalidOperationException("M02 skeletal preview Prefab is missing: " + PreviewPrefabPath);
        }

        TotemTransformSkeletalRig rig = prefab.GetComponent<TotemTransformSkeletalRig>();
        if (rig == null)
        {
            throw new InvalidOperationException("M02 skeletal preview is missing TotemTransformSkeletalRig.");
        }

        foreach (TotemSkeletalBodyPart part in Enum.GetValues(typeof(TotemSkeletalBodyPart)))
        {
            if (!rig.TryGetTattooAnchor(part, out _))
            {
                throw new InvalidOperationException("M02 skeletal preview is missing tattoo anchor: " + part);
            }
        }

        Animator animator = prefab.GetComponent<Animator>();
        AnimatorController controller = animator == null ? null : animator.runtimeAnimatorController as AnimatorController;
        if (controller == null)
        {
            throw new InvalidOperationException("M02 skeletal preview is missing its isolated AnimatorController.");
        }

        string[] requiredParameters = { "Direction", "IsMoving", "AttackTrigger", "HitTrigger", "DodgeTrigger", "IsSprinting", "Die", "Dead" };
        foreach (string parameterName in requiredParameters)
        {
            bool found = false;
            foreach (AnimatorControllerParameter parameter in controller.parameters)
            {
                if (parameter.name == parameterName)
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                throw new InvalidOperationException("M02 skeletal controller is missing parameter: " + parameterName);
            }
        }

        Debug.Log("M02 skeletal preview validation passed. Legacy M02 frame assets remain present and the preview exposes six tattoo anchors.");
    }

    private static Dictionary<string, Sprite> LoadAndConfigureSprites()
    {
        Dictionary<string, Sprite> result = new Dictionary<string, Sprite>(StringComparer.Ordinal);
        foreach (string part in RequiredParts)
        {
            string path = SpriteDirectory + "/actor_common_m02_skeletal_down_" + part + ".png";
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                continue;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
            result[part] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        return result;
    }

    private static void ValidateSprites(Dictionary<string, Sprite> sprites)
    {
        foreach (string part in RequiredParts)
        {
            if (!sprites.TryGetValue(part, out Sprite sprite) || sprite == null)
            {
                throw new InvalidOperationException("Missing skeletal M02 Down part: " + part);
            }
        }
    }

    private static AnimatorController CreateController()
    {
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null)
        {
            AssetDatabase.DeleteAsset(ControllerPath);
        }

        AnimationClip idleClip = CreateClip("SkeletalIdle_Down", false);
        AnimationClip walkClip = CreateClip("SkeletalWalk_Down", true);
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.AddParameter("Direction", AnimatorControllerParameterType.Int);
        controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
        controller.AddParameter("AttackTrigger", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("HitTrigger", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("DodgeTrigger", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("IsSprinting", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Die", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Dead", AnimatorControllerParameterType.Bool);

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        AnimatorState idle = stateMachine.AddState("Idle_Down");
        idle.motion = idleClip;
        stateMachine.defaultState = idle;
        AnimatorState walk = stateMachine.AddState("Walk_Down");
        walk.motion = walkClip;
        AnimatorStateTransition toWalk = idle.AddTransition(walk);
        toWalk.hasExitTime = false;
        toWalk.duration = 0.05f;
        toWalk.AddCondition(AnimatorConditionMode.If, 0f, "IsMoving");
        AnimatorStateTransition toIdle = walk.AddTransition(idle);
        toIdle.hasExitTime = false;
        toIdle.duration = 0.05f;
        toIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsMoving");
        return controller;
    }

    private static AnimationClip CreateClip(string name, bool walk)
    {
        string path = AnimationDirectory + "/" + name + ".anim";
        if (AssetDatabase.LoadAssetAtPath<AnimationClip>(path) != null)
        {
            AssetDatabase.DeleteAsset(path);
        }

        AnimationClip clip = new AnimationClip { name = name, frameRate = 12f, wrapMode = WrapMode.Loop };
        float armAmount = walk ? 20f : 3f;
        float legAmount = walk ? 15f : 2f;
        SetRotationCurve(clip, "RigRoot/Pelvis/Chest/LeftUpperArm", armAmount);
        SetRotationCurve(clip, "RigRoot/Pelvis/Chest/RightUpperArm", -armAmount);
        SetRotationCurve(clip, "RigRoot/Pelvis/LeftLeg", -legAmount);
        SetRotationCurve(clip, "RigRoot/Pelvis/RightLeg", legAmount);
        if (!walk)
        {
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("RigRoot", typeof(Transform), "m_LocalPosition.y"),
                new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.5f, 0.025f), new Keyframe(1f, 0f)));
        }

        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    private static void SetRotationCurve(AnimationClip clip, string path, float amount)
    {
        AnimationCurve curve = new AnimationCurve(
            new Keyframe(0f, -amount),
            new Keyframe(0.5f, amount),
            new Keyframe(1f, -amount));
        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), "localEulerAnglesRaw.z"), curve);
    }

    private static GameObject CreatePreviewObject(Dictionary<string, Sprite> sprites, AnimatorController controller)
    {
        GameObject root = new GameObject("PlayerSkeletalPreview");
        Animator animator = root.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        TotemTransformSkeletalRig rig = root.AddComponent<TotemTransformSkeletalRig>();

        Transform rigRoot = CreateBone(root.transform, "RigRoot", new Vector3(0f, 0f, 0f));
        Transform pelvis = CreateBone(rigRoot, "Pelvis", new Vector3(0f, 0f, 0f));
        Transform chest = CreateBone(pelvis, "Chest", new Vector3(0f, 1.05f, 0f));
        Transform head = CreateBone(chest, "Head", new Vector3(0f, 1.15f, 0f));
        Transform leftUpperArm = CreateBone(chest, "LeftUpperArm", new Vector3(-0.55f, 0.1f, 0f));
        Transform leftLowerArm = CreateBone(leftUpperArm, "LeftLowerArm", new Vector3(-0.5f, -0.55f, 0f));
        Transform rightUpperArm = CreateBone(chest, "RightUpperArm", new Vector3(0.55f, 0.1f, 0f));
        Transform rightLowerArm = CreateBone(rightUpperArm, "RightLowerArm", new Vector3(0.5f, -0.55f, 0f));
        Transform leftLeg = CreateBone(pelvis, "LeftLeg", new Vector3(-0.3f, -0.55f, 0f));
        Transform rightLeg = CreateBone(pelvis, "RightLeg", new Vector3(0.3f, -0.55f, 0f));

        AddPart(head, sprites["head_neck"], "HeadSprite", new Vector3(0f, 0.18f, 0f), 10);
        AddPart(chest, sprites["torso_skin"], "TorsoSkinSprite", Vector3.zero, 5);
        AddPart(chest, sprites["vest_overlay"], "VestOverlaySprite", Vector3.zero, 6);
        AddPart(leftUpperArm, sprites["left_upper_arm"], "LeftUpperArmSprite", new Vector3(-0.1f, -0.25f, 0f), 4);
        AddPart(leftLowerArm, sprites["left_lower_arm_hand"], "LeftLowerArmSprite", new Vector3(-0.1f, -0.22f, 0f), 4);
        AddPart(rightUpperArm, sprites["right_upper_arm"], "RightUpperArmSprite", new Vector3(0.1f, -0.25f, 0f), 4);
        AddPart(rightLowerArm, sprites["right_lower_arm_hand"], "RightLowerArmSprite", new Vector3(0.1f, -0.22f, 0f), 4);
        AddPart(pelvis, sprites["pelvis_shorts"], "PelvisSprite", Vector3.zero, 7);
        AddPart(leftLeg, sprites["left_leg"], "LeftLegSprite", new Vector3(0f, -0.75f, 0f), 3);
        AddPart(leftLeg, sprites["left_foot"], "LeftFootSprite", new Vector3(0f, -1.1f, 0f), 4);
        AddPart(rightLeg, sprites["right_leg"], "RightLegSprite", new Vector3(0f, -0.75f, 0f), 3);

        TotemSkeletalTattooAnchor[] anchors =
        {
            AddAnchor(head, TotemSkeletalBodyPart.Head, new Rect(-0.20f, -0.15f, 0.4f, 0.45f)),
            AddAnchor(chest, TotemSkeletalBodyPart.Torso, new Rect(-0.35f, -0.45f, 0.7f, 0.75f)),
            AddAnchor(leftUpperArm, TotemSkeletalBodyPart.LeftArm, new Rect(-0.20f, -0.45f, 0.4f, 0.75f)),
            AddAnchor(rightUpperArm, TotemSkeletalBodyPart.RightArm, new Rect(-0.20f, -0.45f, 0.4f, 0.75f)),
            AddAnchor(leftLeg, TotemSkeletalBodyPart.LeftLeg, new Rect(-0.20f, -0.65f, 0.4f, 1.0f)),
            AddAnchor(rightLeg, TotemSkeletalBodyPart.RightLeg, new Rect(-0.20f, -0.65f, 0.4f, 1.0f)),
        };
        rig.Configure(rigRoot, anchors);
        return root;
    }

    private static Transform CreateBone(Transform parent, string name, Vector3 localPosition)
    {
        GameObject bone = new GameObject(name);
        bone.transform.SetParent(parent, false);
        bone.transform.localPosition = localPosition;
        return bone.transform;
    }

    private static void AddPart(Transform parent, Sprite sprite, string name, Vector3 localPosition, int sortingOrder)
    {
        GameObject visual = new GameObject(name);
        visual.transform.SetParent(parent, false);
        visual.transform.localPosition = localPosition;
        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = sortingOrder;
    }

    private static TotemSkeletalTattooAnchor AddAnchor(Transform parent, TotemSkeletalBodyPart part, Rect bounds)
    {
        GameObject anchorObject = new GameObject(part + "TattooAnchor");
        anchorObject.transform.SetParent(parent, false);
        TotemSkeletalTattooAnchor anchor = anchorObject.AddComponent<TotemSkeletalTattooAnchor>();
        anchor.Configure(part, bounds, new Vector2(0.5f, 0.5f), 1f);
        return anchor;
    }

    private static void ValidateLegacyContract()
    {
        const string legacyControllerPath = "Assets/Game/Animation/Actors/ActorCommonM02/ActorCommonM02.controller";
        const string legacySpriteDirectory = "Assets/Game/Sprite/Actors/ActorCommonM02";
        const string legacyAnimationDirectory = "Assets/Game/Animation/Actors/ActorCommonM02";
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(legacyControllerPath) == null)
        {
            throw new InvalidOperationException("Legacy M02 controller is missing: " + legacyControllerPath);
        }

        int spriteCount = AssetDatabase.FindAssets("t:Sprite", new[] { legacySpriteDirectory }).Length;
        if (spriteCount != 168)
        {
            throw new InvalidOperationException("Legacy M02 frame count changed. Expected 168 Sprites, found " + spriteCount + ".");
        }

        int clipCount = AssetDatabase.FindAssets("t:AnimationClip", new[] { legacyAnimationDirectory }).Length;
        if (clipCount != 28)
        {
            throw new InvalidOperationException("Legacy M02 clip count changed. Expected 28 clips, found " + clipCount + ".");
        }
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        int slash = path.LastIndexOf('/');
        if (slash <= 0)
        {
            throw new InvalidOperationException("Cannot create Unity folder: " + path);
        }

        EnsureFolder(path.Substring(0, slash));
        AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
    }
}
#endif
