using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

internal static class OasisLookDevAssetUtility
{
    private const string ProfilesRoot = OasisLookDevCatalog.AssetRoot + "/Profiles";
    private const string LightingRoot = OasisLookDevCatalog.AssetRoot + "/LightingSettings";
    private const string BakesRoot = OasisLookDevCatalog.AssetRoot + "/Bakes";

    [MenuItem("Game Framework/GameTools/Oasis City/LookDev/Create or Refresh Look Assets")]
    internal static void CreateOrRefreshAssets()
    {
        try
        {
            CreateOrRefreshAssetsCore();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            throw;
        }
    }

    [MenuItem("Game Framework/GameTools/Oasis City/LookDev/Assign Neutral Preview Baseline")]
    internal static void AssignNeutralPreviewBaseline()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded || !string.Equals(scene.path, "Assets/Game/Scene/OasisCity.unity", StringComparison.Ordinal))
            throw new InvalidOperationException("请先打开 Assets/Game/Scene/OasisCity.unity。");
        OasisLookDevCatalog catalog = OasisLookDevCatalog.Load()
            ?? throw new InvalidOperationException("OasisLookDevCatalog 尚未生成。");
        OasisLookPreset neutral = catalog.Find("NeutralRealistic")
            ?? throw new InvalidOperationException("缺少 NeutralRealistic 预设。");
        Lightmapping.lightingSettings = neutral.PreviewLightingSettings;
        Lightmapping.lightingDataAsset = neutral.GetLightingData(OasisBakeTier.Preview);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
            throw new InvalidOperationException("保存 OasisCity 中性预览基线失败。");
    }

    private static void CreateOrRefreshAssetsCore()
    {
        EnsureFolder(OasisLookDevCatalog.AssetRoot);
        EnsureFolder(ProfilesRoot);
        EnsureFolder(LightingRoot);
        EnsureFolder(BakesRoot);

        OasisLookDevCatalog catalog = LoadOrCreate<OasisLookDevCatalog>(OasisLookDevCatalog.CatalogPath);
        List<OasisLookPreset> presets = new()
        {
            CreatePreset(
                "WarmCinematic", "暖调电影感",
                new Vector3(43f, -38f, 0f), new Color(1f, 0.78f, 0.55f), 1.18f,
                new Color(0.28f, 0.34f, 0.45f), new Color(0.38f, 0.28f, 0.18f), new Color(0.12f, 0.08f, 0.055f),
                new Color(0.55f, 0.42f, 0.29f), 390f, 1320f,
                0.15f, 12f, 6f, 18f, -3f, 0.45f, 1.10f, 0.18f),
            CreatePreset(
                "NeutralRealistic", "中性写实",
                new Vector3(50f, -25f, 0f), new Color(1f, 0.96f, 0.88f), 1.05f,
                new Color(0.34f, 0.40f, 0.46f), new Color(0.30f, 0.28f, 0.24f), new Color(0.13f, 0.12f, 0.10f),
                new Color(0.52f, 0.49f, 0.43f), 470f, 1500f,
                0f, 4f, 0f, 0f, 0f, 0.12f, 1.20f, 0.06f),
            CreatePreset(
                "BoldStylized", "强风格化",
                new Vector3(39f, -52f, 0f), new Color(1f, 0.68f, 0.38f), 1.30f,
                new Color(0.20f, 0.31f, 0.50f), new Color(0.34f, 0.20f, 0.16f), new Color(0.075f, 0.055f, 0.11f),
                new Color(0.42f, 0.29f, 0.32f), 330f, 1180f,
                0.05f, 24f, 18f, 8f, -5f, 0.80f, 0.90f, 0.28f, true),
        };
        catalog.ReplacePresets(presets);
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        foreach (OasisLookPreset preset in presets)
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(preset.VolumeProfile), ImportAssetOptions.ForceUpdate);
        AssetDatabase.Refresh();

        IReadOnlyList<string> errors = catalog.ValidateCatalog();
        if (errors.Count > 0)
            throw new InvalidOperationException(string.Join("\n", errors));
        Debug.Log($"[OasisLookDev] 已创建/刷新三套风格资产：{OasisLookDevCatalog.CatalogPath}");
    }

    private static OasisLookPreset CreatePreset(
        string id, string displayName, Vector3 sunEuler, Color sunColor, float sunIntensity,
        Color sky, Color equator, Color ground, Color fog, float fogStart, float fogEnd,
        float exposure, float contrast, float saturation, float temperature, float tint,
        float bloomIntensity, float bloomThreshold, float vignetteIntensity, bool splitToning = false)
    {
        VolumeProfile profile = LoadOrCreate<VolumeProfile>($"{ProfilesRoot}/{id}.asset");
        ConfigureProfile(profile, exposure, contrast, saturation, temperature, tint, bloomIntensity, bloomThreshold, vignetteIntensity, splitToning);

        LightingSettings preview = LoadOrCreate<LightingSettings>($"{LightingRoot}/{id}_Preview.asset");
        LightingSettings final = LoadOrCreate<LightingSettings>($"{LightingRoot}/{id}_Final.asset");
        ConfigureLightingSettings(preview, false);
        ConfigureLightingSettings(final, true);

        EnsureFolder($"{BakesRoot}/{id}");
        EnsureFolder($"{BakesRoot}/{id}/Preview");
        EnsureFolder($"{BakesRoot}/{id}/Final");
        return OasisLookPreset.Create(id, displayName, profile, preview, final, sunEuler, sunColor, sunIntensity, sky, equator, ground, fog, fogStart, fogEnd);
    }

    private static void ConfigureProfile(
        VolumeProfile profile, float exposure, float contrast, float saturation, float temperature, float tint,
        float bloomIntensity, float bloomThreshold, float vignetteIntensity, bool useSplitToning)
    {
        ClearProfile(profile);
        Tonemapping tonemapping = AddComponent<Tonemapping>(profile, true);
        tonemapping.mode.Override(TonemappingMode.ACES);
        ColorAdjustments color = AddComponent<ColorAdjustments>(profile, true);
        color.postExposure.Override(exposure);
        color.contrast.Override(contrast);
        color.saturation.Override(saturation);
        WhiteBalance whiteBalance = AddComponent<WhiteBalance>(profile, true);
        whiteBalance.temperature.Override(temperature);
        whiteBalance.tint.Override(tint);
        Bloom bloom = AddComponent<Bloom>(profile, true);
        bloom.intensity.Override(bloomIntensity);
        bloom.threshold.Override(bloomThreshold);
        bloom.scatter.Override(0.62f);
        Vignette vignette = AddComponent<Vignette>(profile, true);
        vignette.intensity.Override(vignetteIntensity);
        vignette.smoothness.Override(0.42f);
        if (useSplitToning)
        {
            SplitToning split = AddComponent<SplitToning>(profile, true);
            split.shadows.Override(new Color(0.24f, 0.34f, 0.54f));
            split.highlights.Override(new Color(1f, 0.70f, 0.42f));
            split.balance.Override(8f);
        }

        AddDisabled<MotionBlur>(profile);
        AddDisabled<DepthOfField>(profile);
        AddDisabled<ChromaticAberration>(profile);
        AddDisabled<LensDistortion>(profile);
        AddDisabled<PaniniProjection>(profile);
        EditorUtility.SetDirty(profile);
    }

    private static void ClearProfile(VolumeProfile profile)
    {
        for (int index = profile.components.Count - 1; index >= 0; index--)
        {
            VolumeComponent component = profile.components[index];
            profile.components.RemoveAt(index);
            if (component != null)
                UnityEngine.Object.DestroyImmediate(component, true);
        }
    }

    private static void AddDisabled<T>(VolumeProfile profile) where T : VolumeComponent
    {
        T component = AddComponent<T>(profile, false);
        component.active = false;
    }

    private static T AddComponent<T>(VolumeProfile profile, bool overrides) where T : VolumeComponent
    {
        T component = profile.Add<T>(overrides);
        component.name = typeof(T).Name;
        component.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
        AssetDatabase.AddObjectToAsset(component, AssetDatabase.GetAssetPath(profile));
        return component;
    }

    private static void ConfigureLightingSettings(LightingSettings settings, bool final)
    {
        settings.bakedGI = true;
        settings.realtimeGI = false;
        settings.lightmapper = LightingSettings.Lightmapper.ProgressiveGPU;
        settings.lightmapMaxSize = final ? 4096 : 2048;
        settings.lightmapResolution = final ? 5f : 2f;
        settings.directSampleCount = final ? 128 : 32;
        settings.indirectSampleCount = final ? 512 : 128;
        settings.environmentSampleCount = final ? 256 : 64;
        settings.maxBounces = final ? 4 : 2;
        settings.directionalityMode = LightmapsMode.CombinedDirectional;
        EditorUtility.SetDirty(settings);
    }

    private static T LoadOrCreate<T>(string path) where T : UnityEngine.Object
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null)
            return asset;
        asset = typeof(ScriptableObject).IsAssignableFrom(typeof(T))
            ? ScriptableObject.CreateInstance(typeof(T)) as T
            : Activator.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    private static void EnsureFolder(string assetPath)
    {
        string[] segments = assetPath.Split('/');
        string current = segments[0];
        for (int index = 1; index < segments.Length; index++)
        {
            string next = current + "/" + segments[index];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, segments[index]);
            current = next;
        }
    }
}
