#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UGF.EditorTools
{
    public static class TotemRuntimeAssetMigrator
    {
        public const string TargetRoot = "Assets/Game/Prefabs/Entity/Actors";

        private static readonly RuntimePrefabRule[] Rules =
        {
            new RuntimePrefabRule("actor.player", $"{TargetRoot}/Player.prefab"),
            new RuntimePrefabRule("actor.smartAi", $"{TargetRoot}/SmartAI.prefab"),
            new RuntimePrefabRule("actor.lightAi", $"{TargetRoot}/LightAI.prefab"),
            new RuntimePrefabRule("actor.boss", $"{TargetRoot}/Boss.prefab"),
        };

        [MenuItem("Game Framework/GameTools/Totem/Prepare Runtime Entity Prefabs", false, 1032)]
        public static void PrepareRuntimeEntityPrefabs()
        {
            EnsureFolder(TargetRoot);
            int preparedCount = 0;
            for (int i = 0; i < Rules.Length; i++)
            {
                var rule = Rules[i];
                if (!File.Exists(rule.TargetPath))
                {
                    GFTrace.Failure("TotemAssetMigrator", "TargetMissing", null, GFTrace.Data("key", rule.Key, "target", rule.TargetPath));
                    continue;
                }

                PreparePrefab(rule.TargetPath);
                preparedCount++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            GFTrace.Success("TotemAssetMigrator", "PrepareRuntimeEntityPrefabs", null, GFTrace.Data("preparedCount", preparedCount.ToString()));
        }

        public static RuntimePrefabRule[] GetRules()
        {
            var copy = new RuntimePrefabRule[Rules.Length];
            Array.Copy(Rules, copy, Rules.Length);
            return copy;
        }

        private static void PreparePrefab(string targetPath)
        {
            var root = PrefabUtility.LoadPrefabContents(targetPath);
            try
            {
                root.name = Path.GetFileNameWithoutExtension(targetPath);
                RemoveLegacyBehaviours(root);
                PrefabUtility.SaveAsPrefabAsset(root, targetPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void RemoveLegacyBehaviours(GameObject root)
        {
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(root);
            var behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = behaviours.Length - 1; i >= 0; i--)
            {
                if (behaviours[i] != null)
                {
                    UnityEngine.Object.DestroyImmediate(behaviours[i], true);
                }
            }
        }

        private static void EnsureFolder(string path)
        {
            string normalized = path.Replace('\\', '/');
            string[] parts = normalized.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

        public readonly struct RuntimePrefabRule
        {
            public readonly string Key;
            public readonly string TargetPath;

            public RuntimePrefabRule(string key, string targetPath)
            {
                Key = key;
                TargetPath = targetPath;
            }
        }
    }
}
#endif
