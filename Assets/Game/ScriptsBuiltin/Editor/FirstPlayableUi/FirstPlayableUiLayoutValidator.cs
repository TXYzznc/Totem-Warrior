#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Keeps the authored First Playable UGUI layout aligned with its semantic
/// screen anchors. It edits the five real GF Form prefabs directly; no nested
/// page-view prefab is created or required.
/// </summary>
internal static class FirstPlayableUiLayoutValidator
{
    private const string MainMenuPath = "Assets/Game/Prefabs/UI/MainMenu.prefab";
    private const string CombatHudPath = "Assets/Game/Prefabs/UI/CombatHUD.prefab";
    private static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);

    [MenuItem("Game Framework/GameTools/First Playable UI/Apply Semantic Screen Anchors")]
    private static void ApplySemanticScreenAnchors()
    {
        ApplyMainMenu();
        ApplyCombatHud();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        ValidateSemanticScreenAnchors();
        Debug.Log("[FirstPlayableUI] Applied semantic anchors directly to MainMenu and CombatHUD prefabs.");
    }

    [MenuItem("Game Framework/GameTools/First Playable UI/Validate Semantic Screen Anchors")]
    private static void ValidateSemanticScreenAnchors()
    {
        ValidateRoot(MainMenuPath);
        ValidateRoot(CombatHudPath);
        Validate(MainMenuPath, "Panel_MenuRail", new Vector2(0f, 0.5f), new Vector2(48f, -15f));
        Validate(MainMenuPath, "Btn_Exit", new Vector2(1f, 0f), new Vector2(-48f, 70f));
        Validate(MainMenuPath, "Txt_Footer", Vector2.zero, new Vector2(78f, 30f));
        Validate(CombatHudPath, "Panel_Phase", new Vector2(0.5f, 1f), new Vector2(0f, -28f));
        Validate(CombatHudPath, "Panel_Actions", new Vector2(1f, 1f), new Vector2(-32f, -118f));
        Validate(CombatHudPath, "Panel_Player", Vector2.zero, new Vector2(32f, 26f));
        Validate(CombatHudPath, "Panel_Minimap", new Vector2(1f, 1f), new Vector2(-32f, -360f));
        Validate(CombatHudPath, "Panel_CombatLog", Vector2.zero, new Vector2(32f, 324f));
        Validate(CombatHudPath, "Panel_Life", new Vector2(0.5f, 0f), new Vector2(0f, 48f));
        Validate(CombatHudPath, "Panel_Spectator", new Vector2(0.5f, 0f), new Vector2(0f, 138f));
        Validate(CombatHudPath, "Img_Reticle", new Vector2(0.5f, 0.5f), Vector2.zero);
        Debug.Log("[FirstPlayableUI] Semantic anchor validation passed.");
    }

    private static void ApplyMainMenu()
    {
        Edit(MainMenuPath, root =>
        {
            NormalizeRoot(root);
            Set(root, "Panel_MenuRail", new Vector2(0f, 0.5f), new Vector2(48f, -15f));
            Set(root, "Btn_Exit", new Vector2(1f, 0f), new Vector2(-48f, 70f));
            Set(root, "Txt_Footer", Vector2.zero, new Vector2(78f, 30f));
        });
    }

    private static void ApplyCombatHud()
    {
        Edit(CombatHudPath, root =>
        {
            NormalizeRoot(root);
            Set(root, "Panel_Phase", new Vector2(0.5f, 1f), new Vector2(0f, -28f));
            Set(root, "Panel_Actions", new Vector2(1f, 1f), new Vector2(-32f, -118f));
            Set(root, "Panel_Player", Vector2.zero, new Vector2(32f, 26f));
            Set(root, "Panel_Minimap", new Vector2(1f, 1f), new Vector2(-32f, -360f));
            Set(root, "Panel_CombatLog", Vector2.zero, new Vector2(32f, 324f));
            Set(root, "Panel_Life", new Vector2(0.5f, 0f), new Vector2(0f, 48f));
            Set(root, "Panel_Spectator", new Vector2(0.5f, 0f), new Vector2(0f, 138f));
            Set(root, "Img_Reticle", new Vector2(0.5f, 0.5f), Vector2.zero);
        });
    }

    private static void Edit(string path, System.Action<GameObject> edit)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            edit(root);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void NormalizeRoot(GameObject root)
    {
        var rect = root.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var scaler = root.GetComponent<UnityEngine.UI.CanvasScaler>();
        if (scaler == null)
        {
            throw new System.InvalidOperationException($"{root.name} is missing CanvasScaler.");
        }
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = ReferenceResolution;
        scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
    }

    private static void Set(GameObject root, string name, Vector2 anchor, Vector2 position)
    {
        RectTransform rect = Find(root, name);
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = position;
    }

    private static void ValidateRoot(string path)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var scaler = root.GetComponent<UnityEngine.UI.CanvasScaler>();
            if (scaler == null || scaler.referenceResolution != ReferenceResolution ||
                scaler.uiScaleMode != UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize)
            {
                throw new System.InvalidOperationException($"{path} does not satisfy the shared CanvasScaler contract.");
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void Validate(string path, string name, Vector2 anchor, Vector2 position)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            RectTransform rect = Find(root, name);
            if (rect.anchorMin != anchor || rect.anchorMax != anchor || rect.pivot != anchor || rect.anchoredPosition != position)
            {
                throw new System.InvalidOperationException($"{path}/{name} has an invalid semantic anchor or anchored position.");
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static RectTransform Find(GameObject root, string name)
    {
        foreach (RectTransform rect in root.GetComponentsInChildren<RectTransform>(true))
        {
            if (rect.name == name) return rect;
        }
        throw new System.InvalidOperationException($"{root.name} is missing RectTransform '{name}'.");
    }
}
#endif
