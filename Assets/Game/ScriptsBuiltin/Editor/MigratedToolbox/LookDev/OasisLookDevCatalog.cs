using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

internal enum OasisBakeTier
{
    Preview,
    Final,
}

[Serializable]
internal sealed class OasisLookPreset
{
    [SerializeField] private string id;
    [SerializeField] private string displayName;
    [SerializeField] private VolumeProfile volumeProfile;
    [SerializeField] private LightingSettings previewLightingSettings;
    [SerializeField] private LightingSettings finalLightingSettings;
    [SerializeField] private LightingDataAsset previewLightingData;
    [SerializeField] private LightingDataAsset finalLightingData;
    [SerializeField] private Vector3 sunEuler;
    [SerializeField] private Color sunColor = Color.white;
    [SerializeField] private float sunIntensity = 1f;
    [SerializeField] private Color ambientSky;
    [SerializeField] private Color ambientEquator;
    [SerializeField] private Color ambientGround;
    [SerializeField] private Color fogColor;
    [SerializeField] private float fogStart = 350f;
    [SerializeField] private float fogEnd = 1400f;

    internal string Id => id;
    internal string DisplayName => displayName;
    internal VolumeProfile VolumeProfile => volumeProfile;
    internal LightingSettings PreviewLightingSettings => previewLightingSettings;
    internal LightingSettings FinalLightingSettings => finalLightingSettings;
    internal Vector3 SunEuler => sunEuler;
    internal Color SunColor => sunColor;
    internal float SunIntensity => sunIntensity;
    internal Color AmbientSky => ambientSky;
    internal Color AmbientEquator => ambientEquator;
    internal Color AmbientGround => ambientGround;
    internal Color FogColor => fogColor;
    internal float FogStart => fogStart;
    internal float FogEnd => fogEnd;

    internal LightingSettings GetLightingSettings(OasisBakeTier tier) =>
        tier == OasisBakeTier.Preview ? previewLightingSettings : finalLightingSettings;

    internal LightingDataAsset GetLightingData(OasisBakeTier tier) =>
        tier == OasisBakeTier.Preview ? previewLightingData : finalLightingData;

    internal void SetLightingData(OasisBakeTier tier, LightingDataAsset value)
    {
        if (tier == OasisBakeTier.Preview)
            previewLightingData = value;
        else
            finalLightingData = value;
    }

    internal static OasisLookPreset Create(
        string id,
        string displayName,
        VolumeProfile profile,
        LightingSettings preview,
        LightingSettings final,
        Vector3 sunEuler,
        Color sunColor,
        float sunIntensity,
        Color sky,
        Color equator,
        Color ground,
        Color fog,
        float fogStart,
        float fogEnd)
    {
        return new OasisLookPreset
        {
            id = id,
            displayName = displayName,
            volumeProfile = profile,
            previewLightingSettings = preview,
            finalLightingSettings = final,
            sunEuler = sunEuler,
            sunColor = sunColor,
            sunIntensity = sunIntensity,
            ambientSky = sky,
            ambientEquator = equator,
            ambientGround = ground,
            fogColor = fog,
            fogStart = fogStart,
            fogEnd = fogEnd,
        };
    }
}

internal sealed class OasisLookDevCatalog : ScriptableObject
{
    internal const string AssetRoot = "Assets/Game/Scene/OasisCityLookDev";
    internal const string CatalogPath = AssetRoot + "/OasisLookDevCatalog.asset";
    internal const string HighFidelityPipelinePath = "Assets/Settings/URP-HighFidelity.asset";
    internal static readonly string[] RequiredIds = { "WarmCinematic", "NeutralRealistic", "BoldStylized" };

    [SerializeField] private List<OasisLookPreset> presets = new();
    [SerializeField] private bool previewApproved;

    internal IReadOnlyList<OasisLookPreset> Presets => presets;
    internal bool PreviewApproved => previewApproved;

    internal OasisLookPreset Find(string id) =>
        presets.FirstOrDefault(item => string.Equals(item.Id, id, StringComparison.Ordinal));

    internal void ReplacePresets(IEnumerable<OasisLookPreset> values)
    {
        presets = values.ToList();
        previewApproved = false;
    }

    internal void SetPreviewApproved(bool value) => previewApproved = value;

    internal IReadOnlyList<string> ValidateCatalog()
    {
        List<string> errors = new();
        if (presets.Count != RequiredIds.Length)
            errors.Add($"预设数量应为 {RequiredIds.Length}，当前为 {presets.Count}。");
        foreach (string id in RequiredIds)
        {
            OasisLookPreset preset = Find(id);
            if (preset == null)
            {
                errors.Add($"缺少预设：{id}");
                continue;
            }
            if (preset.VolumeProfile == null) errors.Add($"{id} 缺少 VolumeProfile。");
            if (preset.PreviewLightingSettings == null) errors.Add($"{id} 缺少预览 LightingSettings。");
            if (preset.FinalLightingSettings == null) errors.Add($"{id} 缺少最终 LightingSettings。");
        }
        if (presets.Where(item => item.VolumeProfile != null).Select(item => item.VolumeProfile).Distinct().Count() != presets.Count)
            errors.Add("三个预设必须使用不同的 VolumeProfile。");
        return errors;
    }

    internal static OasisLookDevCatalog Load() => AssetDatabase.LoadAssetAtPath<OasisLookDevCatalog>(CatalogPath);
}
