using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Game.EditorTools.OasisCity
{
    internal static class OasisCityLightmapUvUtility
    {
        private const string ModelRoot = "Assets/Game/Models/Environment/OasisCity";

        [MenuItem("Game Framework/GameTools/Oasis City/LookDev/Preview Secondary UV Changes")]
        internal static void PreviewSecondaryUvChanges()
        {
            string[] paths = FindModelPaths();
            string[] pending = paths.Where(path =>
            {
                ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
                return importer != null && !importer.generateSecondaryUV;
            }).ToArray();
            Debug.Log($"[OasisLookDev] UV2 预览：扫描 {paths.Length} 个 FBX，待修改 {pending.Length} 个。\n{string.Join("\n", pending)}");
        }

        [MenuItem("Game Framework/GameTools/Oasis City/LookDev/Apply Secondary UV Changes")]
        internal static void ApplySecondaryUvChanges()
        {
            string[] paths = FindModelPaths();
            List<string> changed = new();
            foreach (string path in paths)
            {
                if (AssetImporter.GetAtPath(path) is not ModelImporter importer || importer.generateSecondaryUV)
                    continue;
                importer.generateSecondaryUV = true;
                importer.SaveAndReimport();
                changed.Add(path);
            }
            AssetDatabase.Refresh();
            Debug.Log($"[OasisLookDev] 已为 {changed.Count}/{paths.Length} 个 OasisCity FBX 启用次级 UV。" );
        }

        internal static string[] FindModelPaths()
        {
            return AssetDatabase.FindAssets("t:Model", new[] { ModelRoot })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
        }
    }
}
