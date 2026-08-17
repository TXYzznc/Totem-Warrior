#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

internal static class InteractiveObjImportTool
{
    private const string ModelRoot = "Assets/Game/Models/InteractiveObj";
    private const string TextureRoot = "Assets/Game/Textures/InteractiveObj";
    private const string MaterialRoot = "Assets/Game/Materials/InteractiveObj";
    private const string PrefabRoot = "Assets/Game/Prefabs/InteractiveObj";
    private static readonly Regex PartRegex = new Regex(@"tripo_part_(\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    [MenuItem("GameTools/Art/Migrate Interactive Object Folders")]
    private static void MigrateFolders()
    {
        const string legacyRoot = "Assets/Game/Models/InteractiveObj";
        EnsureFolder("Assets/Game/Textures/InteractiveObj");
        EnsureFolder("Assets/Game/Materials/InteractiveObj");
        EnsureFolder("Assets/Game/Prefabs/InteractiveObj");
        EnsureFolder("Assets/Game/Textures/InteractiveObj/Previews");
        EnsureFolder("Assets/Game/Config/InteractiveObj");

        MoveFolderContents(legacyRoot + "/Models", ModelRoot);
        MoveFolderContents(legacyRoot + "/Textures", TextureRoot);
        MoveFolderContents(legacyRoot + "/Materials", MaterialRoot);
        MoveFolderContents(legacyRoot + "/Prefabs", PrefabRoot);
        MoveFolderContents(legacyRoot + "/Previews", TextureRoot + "/Previews");
        MoveAssetIfPresent(legacyRoot + "/import_manifest.json", "Assets/Game/Config/InteractiveObj/import_manifest.json");

        foreach (string folder in new[] { "Models", "Textures", "Materials", "Prefabs", "Previews" })
        {
            string path = legacyRoot + "/" + folder;
            if (AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.DeleteAsset(path);
            }
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[InteractiveObjImport] Folder migration completed with GUIDs preserved.");
    }

    [MenuItem("GameTools/Art/Import Interactive Objects")]
    private static void ImportAll()
    {
        string[] modelPaths = AssetDatabase.FindAssets("t:Model", new[] { ModelRoot })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path => path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        try
        {
            AssetDatabase.StartAssetEditing();
            ConfigureTextureImporters();
            ConfigureModelImporters(modelPaths);
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        int materialCount = 0;
        int prefabCount = 0;
        foreach (string modelPath in modelPaths)
        {
            string assetId = Path.GetFileNameWithoutExtension(modelPath).Substring("SM_".Length);
            Dictionary<int, Material> materials = CreateMaterials(assetId);
            materialCount += materials.Count;
            CreatePrefab(modelPath, assetId, materials);
            prefabCount++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[InteractiveObjImport] Completed: {modelPaths.Length} models, {materialCount} materials, {prefabCount} prefabs.");
    }

    [MenuItem("GameTools/Art/Validate Interactive Objects")]
    private static void ValidateAll()
    {
        string[] prefabPaths = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabRoot })
            .Select(AssetDatabase.GUIDToAssetPath)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        int rendererCount = 0;
        int materialSlotCount = 0;
        var issues = new List<string>();

        foreach (string prefabPath in prefabPaths)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Renderer[] renderers = prefab != null ? prefab.GetComponentsInChildren<Renderer>(true) : Array.Empty<Renderer>();
            if (prefab == null || renderers.Length == 0)
            {
                issues.Add($"{prefabPath}: prefab missing or has no renderer");
                continue;
            }

            foreach (Renderer renderer in renderers)
            {
                rendererCount++;
                foreach (Material material in renderer.sharedMaterials)
                {
                    materialSlotCount++;
                    if (material == null)
                    {
                        issues.Add($"{prefabPath}/{renderer.name}: null material");
                        continue;
                    }
                    if (material.shader == null ||
                        (material.shader.name != "Universal Render Pipeline/Lit" &&
                         material.shader.name != "Game/InteractiveObj/Smoke Navy Glass"))
                    {
                        issues.Add($"{material.name}: wrong shader");
                    }
                    if (material.GetTexture("_BaseMap") == null || material.GetTexture("_BumpMap") == null || material.GetTexture("_MetallicGlossMap") == null)
                    {
                        issues.Add($"{material.name}: incomplete PBR texture set");
                    }
                }
            }
        }

        if (issues.Count > 0)
        {
            throw new InvalidOperationException("[InteractiveObjImport] Validation failed:\n" + string.Join("\n", issues));
        }
        Debug.Log($"[InteractiveObjImport] Validation passed: {prefabPaths.Length} prefabs, {rendererCount} renderers, {materialSlotCount} material slots, complete URP PBR references.");
    }

    [MenuItem("GameTools/Art/Apply Gun Magazine Glass")]
    private static void ApplyGunMagazineGlass()
    {
        const string sourceMaterialPath = "Assets/Game/Materials/InteractiveObj/GUN_001/MAT_GUN_001_P04.mat";
        const string glassMaterialPath = "Assets/Game/Materials/InteractiveObj/GUN_001/MAT_GUN_001_P04_Glass.mat";
        const string prefabPath = "Assets/Game/Prefabs/InteractiveObj/PF_GUN_001.prefab";
        const string rendererName = "FINAL2_GUN_001_tripo_part_4";

        if (AssetDatabase.LoadAssetAtPath<Material>(glassMaterialPath) == null &&
            !AssetDatabase.CopyAsset(sourceMaterialPath, glassMaterialPath))
        {
            throw new InvalidOperationException($"Could not create {glassMaterialPath}.");
        }

        Material glass = AssetDatabase.LoadAssetAtPath<Material>(glassMaterialPath);
        Shader glassShader = Shader.Find("Game/InteractiveObj/Smoke Navy Glass");
        if (glass == null || glassShader == null)
        {
            throw new InvalidOperationException("The Smoke Navy Glass shader was not found.");
        }

        // Smoke-navy crystal: one transparent forward pass, with a procedural internal energy flow.
        glass.shader = glassShader;
        glass.SetColor("_BaseColor", new Color(0.035f, 0.115f, 0.175f, 1f));
        glass.SetFloat("_BumpScale", 0.72f);
        glass.SetFloat("_Opacity", 0.58f);
        glass.SetFloat("_EdgeOpacity", 0.84f);
        glass.SetFloat("_FresnelPower", 3.2f);
        glass.SetColor("_EdgeColor", new Color(0.18f, 0.62f, 0.82f, 1f));
        glass.SetFloat("_EdgeIntensity", 0.72f);
        glass.SetFloat("_Smoothness", 0.86f);
        glass.SetFloat("_SmokeStrength", 0.34f);
        glass.SetFloat("_NoiseScale", 32f);
        glass.SetColor("_FlowColor", new Color(0.03f, 0.72f, 1.1f, 1f));
        glass.SetFloat("_FlowIntensity", 0.55f);
        glass.SetFloat("_FlowSpeed", 0.11f);
        glass.SetFloat("_FlowTiling", 2.2f);
        glass.SetFloat("_FlowWidth", 0.16f);
        glass.SetFloat("_FlowDistortion", 0.12f);
        glass.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        EditorUtility.SetDirty(glass);

        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            Transform target = root.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(transform => transform.name == rendererName);
            Renderer renderer = target != null ? target.GetComponent<Renderer>() : null;
            if (renderer == null)
            {
                throw new InvalidOperationException($"Renderer {rendererName} was not found in {prefabPath}.");
            }

            Material[] slots = renderer.sharedMaterials;
            for (int index = 0; index < slots.Length; index++)
            {
                slots[index] = glass;
            }
            renderer.sharedMaterials = slots;
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[InteractiveObjImport] Applied navy transparent glass to {rendererName}: {glassMaterialPath}");
    }

    [MenuItem("GameTools/Art/Apply Gun Matte Finish")]
    private static void ApplyGunMatteFinish()
    {
        const string materialFolder = "Assets/Game/Materials/InteractiveObj/GUN_001";
        const string prefabPath = "Assets/Game/Prefabs/InteractiveObj/PF_GUN_001.prefab";
        const int glassPart = 4;
        int updated = 0;

        foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { materialFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Match match = Regex.Match(Path.GetFileNameWithoutExtension(path), @"MAT_GUN_001_P(\d+)$");
            if (!match.Success || int.Parse(match.Groups[1].Value) == glassPart)
            {
                continue;
            }

            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                continue;
            }

            // The MS texture retains panel-to-panel variation; scalar values cap the overall sheen.
            bool darkStructuralPart = int.Parse(match.Groups[1].Value) is 0 or 3 or 5 or 6 or 7 or 8 or 9 or 10 or 11 or 12 or 13 or 14;
            material.shader = Shader.Find("Universal Render Pipeline/Lit");
            material.SetFloat("_Metallic", darkStructuralPart ? 0.16f : 0.08f);
            material.SetFloat("_Smoothness", darkStructuralPart ? 0.46f : 0.38f);
            material.SetFloat("_BumpScale", 0.9f);
            material.SetFloat("_SpecularHighlights", 1f);
            material.SetFloat("_EnvironmentReflections", 0.55f);
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
            EditorUtility.SetDirty(material);
            updated++;
        }

        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            Material glass = AssetDatabase.LoadAssetAtPath<Material>(materialFolder + "/MAT_GUN_001_P04_Glass.mat");
            Transform magazine = root.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(transform => transform.name == "FINAL2_GUN_001_tripo_part_4");
            Renderer renderer = magazine != null ? magazine.GetComponent<Renderer>() : null;
            if (renderer == null || glass == null)
            {
                throw new InvalidOperationException("Gun magazine glass binding could not be preserved.");
            }
            renderer.sharedMaterials = Enumerable.Repeat(glass, renderer.sharedMaterials.Length).ToArray();
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[InteractiveObjImport] Applied matte finish to {updated} gun parts; P04 remains smoke-navy glass.");
    }

    private static void ConfigureTextureImporters()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { TextureRoot }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                continue;
            }

            string stem = Path.GetFileNameWithoutExtension(path);
            bool isNormal = stem.EndsWith("_N", StringComparison.OrdinalIgnoreCase);
            bool isColor = stem.EndsWith("_D", StringComparison.OrdinalIgnoreCase);
            bool isMask = stem.EndsWith("_MS", StringComparison.OrdinalIgnoreCase);

            importer.textureType = isNormal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = isColor;
            importer.alphaSource = isMask ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None;
            importer.mipmapEnabled = true;
            importer.streamingMipmaps = true;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Trilinear;
            importer.anisoLevel = isNormal || isColor ? 4 : 1;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
        }
    }

    private static void ConfigureModelImporters(IEnumerable<string> modelPaths)
    {
        foreach (string path in modelPaths)
        {
            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null)
            {
                continue;
            }

            importer.globalScale = 1f;
            importer.useFileScale = true;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importAnimation = false;
            importer.importBlendShapes = false;
            importer.importVisibility = false;
            importer.importNormals = ModelImporterNormals.Import;
            importer.importTangents = ModelImporterTangents.Import;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.isReadable = false;
            importer.meshCompression = ModelImporterMeshCompression.Off;
            importer.optimizeMeshPolygons = true;
            importer.optimizeMeshVertices = true;
            importer.SaveAndReimport();
        }
    }

    private static Dictionary<int, Material> CreateMaterials(string assetId)
    {
        string textureFolder = $"{TextureRoot}/{assetId}";
        string materialFolder = $"{MaterialRoot}/{assetId}";
        Directory.CreateDirectory(materialFolder);

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            throw new InvalidOperationException("URP Lit shader was not found.");
        }

        var result = new Dictionary<int, Material>();
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { textureFolder }))
        {
            string diffusePath = AssetDatabase.GUIDToAssetPath(guid);
            Match match = Regex.Match(Path.GetFileNameWithoutExtension(diffusePath), @"_P(\d+)_D$", RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                continue;
            }

            int part = int.Parse(match.Groups[1].Value);
            string prefix = $"{textureFolder}/T_{assetId}_P{part:00}";
            Texture2D diffuse = AssetDatabase.LoadAssetAtPath<Texture2D>(prefix + "_D.png");
            Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(prefix + "_N.png");
            Texture2D metallicSmoothness = AssetDatabase.LoadAssetAtPath<Texture2D>(prefix + "_MS.png");
            string materialPath = $"{materialFolder}/MAT_{assetId}_P{part:00}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(shader) { name = $"MAT_{assetId}_P{part:00}" };
                AssetDatabase.CreateAsset(material, materialPath);
            }
            else
            {
                material.shader = shader;
            }

            material.SetTexture("_BaseMap", diffuse);
            material.SetColor("_BaseColor", Color.white);
            material.SetTexture("_BumpMap", normal);
            material.SetFloat("_BumpScale", 1f);
            material.SetTexture("_MetallicGlossMap", metallicSmoothness);
            material.SetFloat("_Metallic", 1f);
            material.SetFloat("_Smoothness", 1f);
            material.SetFloat("_SmoothnessTextureChannel", 0f);
            SetKeyword(material, "_NORMALMAP", normal != null);
            SetKeyword(material, "_METALLICSPECGLOSSMAP", metallicSmoothness != null);
            EditorUtility.SetDirty(material);
            result[part] = material;
        }

        return result;
    }

    private static void CreatePrefab(string modelPath, string assetId, IReadOnlyDictionary<int, Material> materials)
    {
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        GameObject instance = PrefabUtility.InstantiatePrefab(model) as GameObject;
        if (instance == null)
        {
            throw new InvalidOperationException($"Could not instantiate {modelPath}.");
        }

        try
        {
            instance.name = $"PF_{assetId}";
            foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                int part = ResolvePartIndex(renderer);
                if (!materials.TryGetValue(part, out Material material))
                {
                    Debug.LogWarning($"[InteractiveObjImport] Missing material for {assetId}/{renderer.name}, part {part}.");
                    continue;
                }

                if (assetId == "GUN_001" && part == 4)
                {
                    Material glass = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialRoot}/GUN_001/MAT_GUN_001_P04_Glass.mat");
                    if (glass != null)
                    {
                        material = glass;
                    }
                }

                Material[] slots = renderer.sharedMaterials;
                for (int index = 0; index < slots.Length; index++)
                {
                    slots[index] = material;
                }
                renderer.sharedMaterials = slots;
            }

            string prefabPath = $"{PrefabRoot}/PF_{assetId}.prefab";
            PrefabUtility.SaveAsPrefabAsset(instance, prefabPath, out bool success);
            if (!success)
            {
                throw new InvalidOperationException($"Could not save {prefabPath}.");
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(instance);
        }
    }

    private static int ResolvePartIndex(Renderer renderer)
    {
        Match match = PartRegex.Match(renderer.name);
        if (!match.Success)
        {
            foreach (Material material in renderer.sharedMaterials)
            {
                if (material == null)
                {
                    continue;
                }
                match = PartRegex.Match(material.name);
                if (match.Success)
                {
                    break;
                }
            }
        }
        return match.Success ? int.Parse(match.Groups[1].Value) : 0;
    }

    private static void SetKeyword(Material material, string keyword, bool enabled)
    {
        if (enabled)
        {
            material.EnableKeyword(keyword);
        }
        else
        {
            material.DisableKeyword(keyword);
        }
    }

    private static void EnsureFolder(string path)
    {
        string[] segments = path.Split('/');
        string current = segments[0];
        for (int index = 1; index < segments.Length; index++)
        {
            string next = current + "/" + segments[index];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, segments[index]);
            }
            current = next;
        }
    }

    private static void MoveFolderContents(string sourceFolder, string destinationFolder)
    {
        if (!AssetDatabase.IsValidFolder(sourceFolder))
        {
            return;
        }
        EnsureFolder(destinationFolder);
        foreach (string guid in AssetDatabase.FindAssets(string.Empty, new[] { sourceFolder }))
        {
            string sourcePath = AssetDatabase.GUIDToAssetPath(guid);
            if (AssetDatabase.IsValidFolder(sourcePath) || Path.GetDirectoryName(sourcePath)?.Replace('\\', '/') != sourceFolder)
            {
                continue;
            }
            MoveAssetIfPresent(sourcePath, destinationFolder + "/" + Path.GetFileName(sourcePath));
        }
        foreach (string directory in Directory.GetDirectories(sourceFolder))
        {
            string sourcePath = directory.Replace('\\', '/');
            MoveAssetIfPresent(sourcePath, destinationFolder + "/" + Path.GetFileName(sourcePath));
        }
    }

    private static void MoveAssetIfPresent(string sourcePath, string destinationPath)
    {
        if (AssetDatabase.LoadMainAssetAtPath(sourcePath) == null && !AssetDatabase.IsValidFolder(sourcePath))
        {
            return;
        }
        string error = AssetDatabase.MoveAsset(sourcePath, destinationPath);
        if (!string.IsNullOrEmpty(error))
        {
            throw new InvalidOperationException($"Move failed: {sourcePath} -> {destinationPath}: {error}");
        }
    }
}
#endif
