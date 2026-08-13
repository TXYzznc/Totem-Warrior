using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public sealed class OasisLookDevTests
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private const string Root = "Assets/Game/Scene/OasisCityLookDev";

    [Test]
    public void GeneratedCatalog_HasExactlyThreeDistinctCompletePresets()
    {
        ScriptableObject catalog = AssetDatabase.LoadAssetAtPath<ScriptableObject>(Root + "/OasisLookDevCatalog.asset");
        Assert.That(catalog, Is.Not.Null);
        SerializedProperty presets = new SerializedObject(catalog).FindProperty("presets");
        Assert.That(presets, Is.Not.Null);
        Assert.That(presets.arraySize, Is.EqualTo(3));

        string[] expected = { "WarmCinematic", "NeutralRealistic", "BoldStylized" };
        string[] ids = new string[3];
        UnityEngine.Object[] profiles = new UnityEngine.Object[3];
        for (int index = 0; index < presets.arraySize; index++)
        {
            SerializedProperty preset = presets.GetArrayElementAtIndex(index);
            ids[index] = preset.FindPropertyRelative("id").stringValue;
            profiles[index] = preset.FindPropertyRelative("volumeProfile").objectReferenceValue;
            Assert.That(preset.FindPropertyRelative("previewLightingSettings").objectReferenceValue, Is.Not.Null);
            Assert.That(preset.FindPropertyRelative("finalLightingSettings").objectReferenceValue, Is.Not.Null);
        }
        CollectionAssert.AreEquivalent(expected, ids);
        Assert.That(profiles.Distinct().Count(), Is.EqualTo(3));
    }

    [TestCase("WarmCinematic")]
    [TestCase("NeutralRealistic")]
    [TestCase("BoldStylized")]
    public void Profile_ContainsRequiredOverrides_AndBaselineLensEffectsAreDisabled(string id)
    {
        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>($"{Root}/Profiles/{id}.asset");
        Assert.That(profile, Is.Not.Null);
        Assert.That(profile.TryGet(out Tonemapping tonemapping) && tonemapping.active, Is.True);
        Assert.That(profile.TryGet(out ColorAdjustments color) && color.active, Is.True);
        Assert.That(profile.TryGet(out WhiteBalance whiteBalance) && whiteBalance.active, Is.True);
        Assert.That(profile.TryGet(out Bloom bloom) && bloom.active, Is.True);
        Assert.That(profile.TryGet(out Vignette vignette) && vignette.active, Is.True);
        AssertDisabled<MotionBlur>(profile);
        AssertDisabled<DepthOfField>(profile);
        AssertDisabled<ChromaticAberration>(profile);
        AssertDisabled<LensDistortion>(profile);
        AssertDisabled<PaniniProjection>(profile);
    }

    [Test]
    public void NeutralProfile_IsMoreRestrainedThanWarmAndBold()
    {
        VolumeProfile warm = AssetDatabase.LoadAssetAtPath<VolumeProfile>($"{Root}/Profiles/WarmCinematic.asset");
        VolumeProfile neutral = AssetDatabase.LoadAssetAtPath<VolumeProfile>($"{Root}/Profiles/NeutralRealistic.asset");
        VolumeProfile bold = AssetDatabase.LoadAssetAtPath<VolumeProfile>($"{Root}/Profiles/BoldStylized.asset");
        warm.TryGet(out ColorAdjustments warmColor);
        neutral.TryGet(out ColorAdjustments neutralColor);
        bold.TryGet(out ColorAdjustments boldColor);
        warm.TryGet(out Bloom warmBloom);
        neutral.TryGet(out Bloom neutralBloom);
        bold.TryGet(out Bloom boldBloom);
        Assert.That(Mathf.Abs(neutralColor.contrast.value), Is.LessThan(Mathf.Abs(warmColor.contrast.value)));
        Assert.That(Mathf.Abs(neutralColor.contrast.value), Is.LessThan(Mathf.Abs(boldColor.contrast.value)));
        Assert.That(neutralBloom.intensity.value, Is.LessThan(warmBloom.intensity.value));
        Assert.That(neutralBloom.intensity.value, Is.LessThan(boldBloom.intensity.value));
    }

    [Test]
    public void LookSession_RestoresSunEnvironmentAndQuality()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject sunObject = new("Sun_Main");
        Light sun = sunObject.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = Color.magenta;
        sun.intensity = 0.37f;
        RenderSettings.sun = sun;
        Color originalSky = new(0.1f, 0.2f, 0.3f);
        RenderSettings.ambientSkyColor = originalSky;
        int originalQuality = QualitySettings.GetQualityLevel();
        LightingSettings originalLightingSettings = new();
        Lightmapping.lightingSettings = originalLightingSettings;

        Type catalogType = Type.GetType("OasisLookDevCatalog, Builtin.Editor", true);
        UnityEngine.Object catalog = AssetDatabase.LoadAssetAtPath(Root + "/OasisLookDevCatalog.asset", catalogType);
        object preset = catalogType.GetMethod("Find", Members).Invoke(catalog, new object[] { "WarmCinematic" });
        Type sessionType = Type.GetType("OasisLookDevSession, Builtin.Editor", true);
        object session = Activator.CreateInstance(sessionType);
        object[] arguments = { preset, Enum.Parse(Type.GetType("OasisBakeTier, Builtin.Editor", true), "Preview"), null };
        bool applied = (bool)sessionType.GetMethod("Apply", Members).Invoke(session, arguments);
        Assert.That(applied, Is.True, arguments[2] as string);
        sessionType.GetMethod("Restore", Members).Invoke(session, null);

        Assert.That(sun.color, Is.EqualTo(Color.magenta));
        Assert.That(sun.intensity, Is.EqualTo(0.37f).Within(0.001f));
        Assert.That(RenderSettings.ambientSkyColor, Is.EqualTo(originalSky));
        Assert.That(QualitySettings.GetQualityLevel(), Is.EqualTo(originalQuality));
        Assert.That(Lightmapping.lightingSettings, Is.SameAs(originalLightingSettings));
        UnityEngine.Object.DestroyImmediate(sunObject);
        UnityEngine.Object.DestroyImmediate(originalLightingSettings);
    }

    [Test]
    public void AllOasisCityModels_HaveImportedSecondaryUvData()
    {
        string[] modelPaths = AssetDatabase.FindAssets("t:Model", new[] { "Assets/Game/Models/Environment/OasisCity" })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path => path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        Assert.That(modelPaths.Length, Is.EqualTo(55));
        foreach (string path in modelPaths)
        {
            Mesh[] meshes = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Mesh>().ToArray();
            Assert.That(meshes.Length, Is.GreaterThan(0), $"No mesh sub-assets: {path}");
            foreach (Mesh mesh in meshes)
                Assert.That(mesh.uv2.Length, Is.EqualTo(mesh.vertexCount), $"Invalid UV2: {path}/{mesh.name}");
        }
    }

    [Test]
    public void GeneratedOasisCity_HasDeterministicGiProbesAndPostProcessCameras()
    {
        EditorSceneManager.OpenScene("Assets/Game/Scene/OasisCity.unity", OpenSceneMode.Single);
        GameObject root = GameObject.Find("ENV_OasisCity");
        Assert.That(root, Is.Not.Null);

        Camera[] cameras = root.GetComponentsInChildren<Camera>(true);
        Assert.That(cameras.Length, Is.EqualTo(14));
        foreach (Camera camera in cameras)
        {
            UniversalAdditionalCameraData data = camera.GetUniversalAdditionalCameraData();
            Assert.That(data.renderPostProcessing, Is.True, camera.name);
        }

        LightProbeGroup probeGroup = root.GetComponentInChildren<LightProbeGroup>(true);
        Assert.That(probeGroup, Is.Not.Null);
        Assert.That(probeGroup.probePositions.Length, Is.EqualTo(585));
        ReflectionProbe[] reflectionProbes = root.GetComponentsInChildren<ReflectionProbe>(true);
        Assert.That(reflectionProbes.Length, Is.EqualTo(6));
        Assert.That(reflectionProbes.All(probe => probe.mode == ReflectionProbeMode.Baked && probe.resolution == 256), Is.True);

        Light sun = root.GetComponentsInChildren<Light>(true).Single(light => light.name == "Sun_Main");
        Assert.That(sun.lightmapBakeType, Is.EqualTo(LightmapBakeType.Baked));

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        Renderer[] giRenderers = renderers.Where(renderer =>
            (GameObjectUtility.GetStaticEditorFlags(renderer.gameObject) & StaticEditorFlags.ContributeGI) != 0).ToArray();
        Assert.That(giRenderers.Length, Is.GreaterThan(500));
        foreach (Renderer renderer in giRenderers.Take(50))
        {
            SerializedObject serializedRenderer = new(renderer);
            Assert.That(serializedRenderer.FindProperty("m_ReceiveGI").intValue, Is.EqualTo((int)ReceiveGI.Lightmaps), renderer.name);
            Assert.That(serializedRenderer.FindProperty("m_ScaleInLightmap").floatValue, Is.GreaterThan(0f), renderer.name);
        }

        string[] generatedMeshGuids = AssetDatabase.FindAssets("t:Mesh", new[] { "Assets/Game/Scene/OasisCityData/Meshes" });
        Assert.That(generatedMeshGuids.Length, Is.GreaterThan(0));
        foreach (string guid in generatedMeshGuids)
        {
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(AssetDatabase.GUIDToAssetPath(guid));
            Assert.That(mesh.uv2.Length, Is.EqualTo(mesh.vertexCount), mesh.name);
        }
    }

    [Test]
    public void FinalBake_IsBlockedUntilPreviewApproval()
    {
        Type catalogType = Type.GetType("OasisLookDevCatalog, Builtin.Editor", true);
        UnityEngine.Object catalog = AssetDatabase.LoadAssetAtPath(Root + "/OasisLookDevCatalog.asset", catalogType);
        Assert.That(new SerializedObject(catalog).FindProperty("previewApproved").boolValue, Is.False);

        Type sessionType = Type.GetType("OasisLookDevSession, Builtin.Editor", true);
        object session = Activator.CreateInstance(sessionType);
        Type controllerType = Type.GetType("OasisLookDevBakeController, Builtin.Editor", true);
        object controller = Activator.CreateInstance(controllerType, Members, null, new[] { session }, null);
        Type tierType = Type.GetType("OasisBakeTier, Builtin.Editor", true);
        MethodInfo start = controllerType.GetMethod("Start", Members);
        TargetInvocationException exception = Assert.Throws<TargetInvocationException>(() =>
            start.Invoke(controller, new[] { catalog, Enum.Parse(tierType, "Final") }));
        Assert.That(exception.InnerException, Is.TypeOf<InvalidOperationException>());
        Assert.That(exception.InnerException.Message, Does.Contain("预览审批闸门"));
    }

    private static void AssertDisabled<T>(VolumeProfile profile) where T : VolumeComponent
    {
        Assert.That(profile.TryGet(out T component), Is.True, $"Missing {typeof(T).Name}");
        Assert.That(component.active, Is.False, $"{typeof(T).Name} must be disabled");
    }
}
