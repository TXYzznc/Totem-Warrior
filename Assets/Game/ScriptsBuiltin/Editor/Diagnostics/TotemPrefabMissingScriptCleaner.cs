#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UGF.EditorTools
{
    public static class TotemPrefabMissingScriptCleaner
    {
        private static readonly string[] PrefabScanRoots =
        {
            "Assets",
        };

        [MenuItem("Game Framework/GameTools/Diagnostics/Cleanup Prefab Missing Scripts", false, 1024)]
        public static void CleanupPrefabMissingScriptsMenu()
        {
            int removed = CleanupPrefabMissingScripts();
            Debug.Log($"Totem prefab missing script cleanup removed {removed} missing script component(s).");
        }

        public static int CleanupPrefabMissingScripts()
        {
            int removed = 0;
            string[] prefabPaths = FindProjectPrefabPaths();
            for (int i = 0; i < prefabPaths.Length; i++)
            {
                removed += CleanupPrefab(prefabPaths[i]);
            }

            if (removed > 0)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            return removed;
        }

        public static IReadOnlyList<PrefabMissingScriptRecord> FindMissingScriptRecords()
        {
            var records = new List<PrefabMissingScriptRecord>();
            string[] prefabPaths = FindProjectPrefabPaths();
            for (int i = 0; i < prefabPaths.Length; i++)
            {
                CollectPrefabRecords(prefabPaths[i], records);
            }

            return records;
        }

        private static string[] FindProjectPrefabPaths()
        {
            return AssetDatabase.FindAssets("t:Prefab", PrefabScanRoots)
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
        }

        private static int CleanupPrefab(string prefabPath)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            var type = PrefabUtility.GetPrefabAssetType(prefab);
            if (prefab == null || type == PrefabAssetType.Model || type == PrefabAssetType.NotAPrefab)
            {
                return 0;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            int removed = 0;
            try
            {
                var nodes = root.GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < nodes.Length; i++)
                {
                    removed += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(nodes[i].gameObject);
                }

                if (removed > 0)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            return removed;
        }

        private static void CollectPrefabRecords(string prefabPath, List<PrefabMissingScriptRecord> records)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            var type = PrefabUtility.GetPrefabAssetType(prefab);
            if (prefab == null || type == PrefabAssetType.Model || type == PrefabAssetType.NotAPrefab)
            {
                return;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var nodes = root.GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < nodes.Length; i++)
                {
                    int missingCount = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(nodes[i].gameObject);
                    if (missingCount <= 0)
                    {
                        continue;
                    }

                    records.Add(new PrefabMissingScriptRecord(
                        prefabPath,
                        GetHierarchyPath(root.transform, nodes[i]),
                        missingCount));
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static string GetHierarchyPath(Transform root, Transform node)
        {
            if (node == root)
            {
                return root.name;
            }

            var names = new Stack<string>();
            Transform current = node;
            while (current != null)
            {
                names.Push(current.name);
                if (current == root)
                {
                    break;
                }

                current = current.parent;
            }

            return string.Join("/", names);
        }
    }

    public readonly struct PrefabMissingScriptRecord
    {
        public PrefabMissingScriptRecord(string prefabPath, string objectPath, int missingCount)
        {
            PrefabPath = prefabPath;
            ObjectPath = objectPath;
            MissingCount = missingCount;
        }

        public string PrefabPath { get; }

        public string ObjectPath { get; }

        public int MissingCount { get; }
    }
}
#endif
