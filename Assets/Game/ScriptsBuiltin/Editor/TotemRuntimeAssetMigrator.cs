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
            new RuntimePrefabRule("actor.player", "Assets/Resources/Prefab/Character/Player1.prefab", $"{TargetRoot}/Player.prefab"),
            new RuntimePrefabRule("actor.smartAi", "Assets/Resources/Prefab/Character/Player2.prefab", $"{TargetRoot}/SmartAI.prefab"),
            new RuntimePrefabRule("actor.lightAi", "Assets/Resources/Prefab/Character/Player3.prefab", $"{TargetRoot}/LightAI.prefab"),
            new RuntimePrefabRule("actor.boss", "Assets/Resources/Prefab/Character/Boss1.prefab", $"{TargetRoot}/Boss.prefab"),
            new RuntimePrefabRule("npc.tattooist", "Assets/Resources/Prefab/Character/Player3.prefab", $"{TargetRoot}/NpcTattooist.prefab"),
            new RuntimePrefabRule("npc.merchant", "Assets/Resources/Prefab/Character/Player2.prefab", $"{TargetRoot}/NpcMerchant.prefab"),
        };

        [MenuItem("Game Framework/GameTools/Totem/Prepare Runtime Entity Prefabs", false, 1032)]
        public static void PrepareRuntimeEntityPrefabs()
        {
            EnsureFolder(TargetRoot);
            int preparedCount = 0;
            for (int i = 0; i < Rules.Length; i++)
            {
                var rule = Rules[i];
                if (!File.Exists(rule.SourcePath))
                {
                    GFTrace.Failure("TotemAssetMigrator", "SourceMissing", null, GFTrace.Data("key", rule.Key, "source", rule.SourcePath));
                    continue;
                }

                if (File.Exists(rule.TargetPath))
                {
                    AssetDatabase.DeleteAsset(rule.TargetPath);
                }

                if (!AssetDatabase.CopyAsset(rule.SourcePath, rule.TargetPath))
                {
                    GFTrace.Failure("TotemAssetMigrator", "CopyFailed", null, GFTrace.Data("key", rule.Key, "source", rule.SourcePath, "target", rule.TargetPath));
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
            public readonly string SourcePath;
            public readonly string TargetPath;

            public RuntimePrefabRule(string key, string sourcePath, string targetPath)
            {
                Key = key;
                SourcePath = sourcePath;
                TargetPath = targetPath;
            }
        }
    }
}
#endif
