using System;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering;

internal sealed class OasisLookDevSession
{
    private sealed class Snapshot
    {
        internal int QualityLevel;
        internal LightingDataAsset LightingData;
        internal LightingSettings LightingSettings;
        internal bool HadLightingSettings;
        internal Light Sun;
        internal Quaternion SunRotation;
        internal Color SunColor;
        internal float SunIntensity;
        internal LightmapBakeType SunBakeType;
        internal LightShadows SunShadows;
        internal AmbientMode AmbientMode;
        internal Color AmbientSky;
        internal Color AmbientEquator;
        internal Color AmbientGround;
        internal bool Fog;
        internal FogMode FogMode;
        internal Color FogColor;
        internal float FogStart;
        internal float FogEnd;
    }

    private Snapshot snapshot;
    private GameObject volumeObject;
    private Volume volume;

    internal string ActiveLookId { get; private set; }
    internal OasisBakeTier ActiveTier { get; private set; }
    internal bool IsActive => snapshot != null;

    internal bool Apply(OasisLookPreset preset, OasisBakeTier tier, out string error)
    {
        error = null;
        if (preset == null || preset.VolumeProfile == null)
        {
            error = "风格预设或 VolumeProfile 无效。";
            return false;
        }

        Light sun = FindSun();
        if (sun == null)
        {
            error = "当前场景缺少 Sun_Main Directional Light。";
            return false;
        }

        if (snapshot == null)
            CaptureSnapshot(sun);

        int highFidelity = FindQualityLevel("High Fidelity");
        if (highFidelity >= 0 && QualitySettings.GetQualityLevel() != highFidelity)
            QualitySettings.SetQualityLevel(highFidelity, true);

        EnsureTemporaryVolume();
        volume.sharedProfile = preset.VolumeProfile;

        sun.transform.rotation = Quaternion.Euler(preset.SunEuler);
        sun.color = preset.SunColor;
        sun.intensity = preset.SunIntensity;
        sun.lightmapBakeType = LightmapBakeType.Baked;
        sun.shadows = LightShadows.Soft;
        RenderSettings.sun = sun;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = preset.AmbientSky;
        RenderSettings.ambientEquatorColor = preset.AmbientEquator;
        RenderSettings.ambientGroundColor = preset.AmbientGround;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = preset.FogColor;
        RenderSettings.fogStartDistance = preset.FogStart;
        RenderSettings.fogEndDistance = preset.FogEnd;

        Lightmapping.lightingSettings = preset.GetLightingSettings(tier);
        Lightmapping.lightingDataAsset = preset.GetLightingData(tier);
        ActiveLookId = preset.Id;
        ActiveTier = tier;
        RepaintViews();
        return true;
    }

    internal void Restore()
    {
        if (snapshot == null)
            return;

        Lightmapping.lightingDataAsset = snapshot.LightingData;
        if (snapshot.HadLightingSettings && snapshot.LightingSettings != null)
            Lightmapping.lightingSettings = snapshot.LightingSettings;
        if (snapshot.Sun != null)
        {
            snapshot.Sun.transform.rotation = snapshot.SunRotation;
            snapshot.Sun.color = snapshot.SunColor;
            snapshot.Sun.intensity = snapshot.SunIntensity;
            snapshot.Sun.lightmapBakeType = snapshot.SunBakeType;
            snapshot.Sun.shadows = snapshot.SunShadows;
        }
        RenderSettings.sun = snapshot.Sun;
        RenderSettings.ambientMode = snapshot.AmbientMode;
        RenderSettings.ambientSkyColor = snapshot.AmbientSky;
        RenderSettings.ambientEquatorColor = snapshot.AmbientEquator;
        RenderSettings.ambientGroundColor = snapshot.AmbientGround;
        RenderSettings.fog = snapshot.Fog;
        RenderSettings.fogMode = snapshot.FogMode;
        RenderSettings.fogColor = snapshot.FogColor;
        RenderSettings.fogStartDistance = snapshot.FogStart;
        RenderSettings.fogEndDistance = snapshot.FogEnd;
        if (QualitySettings.GetQualityLevel() != snapshot.QualityLevel)
            QualitySettings.SetQualityLevel(snapshot.QualityLevel, true);
        DestroyTemporaryVolume();
        snapshot = null;
        ActiveLookId = null;
        RepaintViews();
    }

    private void CaptureSnapshot(Light sun)
    {
        snapshot = new Snapshot
        {
            QualityLevel = QualitySettings.GetQualityLevel(),
            LightingData = Lightmapping.lightingDataAsset,
            HadLightingSettings = Lightmapping.TryGetLightingSettings(out LightingSettings currentLightingSettings),
            LightingSettings = currentLightingSettings,
            Sun = sun,
            SunRotation = sun.transform.rotation,
            SunColor = sun.color,
            SunIntensity = sun.intensity,
            SunBakeType = sun.lightmapBakeType,
            SunShadows = sun.shadows,
            AmbientMode = RenderSettings.ambientMode,
            AmbientSky = RenderSettings.ambientSkyColor,
            AmbientEquator = RenderSettings.ambientEquatorColor,
            AmbientGround = RenderSettings.ambientGroundColor,
            Fog = RenderSettings.fog,
            FogMode = RenderSettings.fogMode,
            FogColor = RenderSettings.fogColor,
            FogStart = RenderSettings.fogStartDistance,
            FogEnd = RenderSettings.fogEndDistance,
        };
    }

    private void EnsureTemporaryVolume()
    {
        if (volumeObject != null)
            return;
        volumeObject = new GameObject("__OasisLookDevPreviewVolume")
        {
            hideFlags = HideFlags.HideAndDontSave,
        };
        volume = volumeObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 10000f;
        volume.weight = 1f;
    }

    private void DestroyTemporaryVolume()
    {
        if (volumeObject != null)
            UnityEngine.Object.DestroyImmediate(volumeObject);
        volumeObject = null;
        volume = null;
    }

    private static Light FindSun()
    {
        GameObject named = GameObject.Find("Sun_Main");
        if (named != null && named.TryGetComponent(out Light namedLight) && namedLight.type == LightType.Directional)
            return namedLight;
        return RenderSettings.sun != null && RenderSettings.sun.type == LightType.Directional ? RenderSettings.sun : null;
    }

    private static int FindQualityLevel(string name)
    {
        string[] names = QualitySettings.names;
        for (int index = 0; index < names.Length; index++)
        {
            if (string.Equals(names[index], name, StringComparison.OrdinalIgnoreCase))
                return index;
        }
        return -1;
    }

    private static void RepaintViews()
    {
        SceneView.RepaintAll();
        InternalEditorUtility.RepaintAllViews();
    }
}
