#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace UGF.EditorTools
{
    public static class TotemFirstSlicePrefabMigrator
    {
        private const string TargetRoot = "Assets/Game/Prefabs/UI";

        private static readonly PrefabMigrationRule[] Rules =
        {
            new PrefabMigrationRule("MainMenu", typeof(TotemMainMenuForm)),
            new PrefabMigrationRule("CharacterSelect", typeof(TotemCharacterSelectForm)),
            new PrefabMigrationRule("StartupSelect", typeof(TotemStartupSelectForm)),
            new PrefabMigrationRule("CombatHUD", typeof(TotemCombatHUDForm)),
            new PrefabMigrationRule("Shop", typeof(TotemShopForm)),
            new PrefabMigrationRule("ThreeChoice", typeof(TotemThreeChoiceForm)),
            new PrefabMigrationRule("TattooStudio", typeof(TotemTattooStudioForm)),
            new PrefabMigrationRule("PauseMenu", typeof(TotemPauseMenuForm)),
            new PrefabMigrationRule("RunResult", typeof(TotemRunResultForm)),
            new PrefabMigrationRule("Settings", typeof(TotemSettingsForm)),
            new PrefabMigrationRule("SelfTattoo", typeof(TotemSelfTattooForm)),
            new PrefabMigrationRule("TattooEnchant", typeof(TotemTattooEnchantForm)),
        };

        [MenuItem("Game Framework/GameTools/Totem/Prepare First Slice UI Prefabs", false, 1031)]
        public static void PrepareFirstSliceUIPrefabs()
        {
            Directory.CreateDirectory(TargetRoot);

            int preparedCount = 0;
            for (int i = 0; i < Rules.Length; i++)
            {
                var rule = Rules[i];
                string targetPath = GetTargetPath(rule.PrefabName);
                if (!File.Exists(targetPath))
                {
                    Debug.LogError($"First slice UI prefab does not exist: {targetPath}");
                    GFTrace.Failure("TotemPrefabMigrator", "TargetMissing", null, GFTrace.Data("prefab", rule.PrefabName, "target", targetPath));
                    continue;
                }

                PreparePrefab(targetPath, rule.FormType);
                preparedCount++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Prepared first slice UI prefabs. prepared={preparedCount}");
            GFTrace.Success("TotemPrefabMigrator", "PrepareFirstSliceUIPrefabs", null, GFTrace.Data(
                "preparedCount", preparedCount.ToString()));
        }

        public static string GetTargetPath(string prefabName)
        {
            return $"{TargetRoot}/{prefabName}.prefab";
        }

        private static void PreparePrefab(string targetPath, Type formType)
        {
            var root = PrefabUtility.LoadPrefabContents(targetPath);
            try
            {
                RemoveMissingScripts(root);
                EnsureRootComponents(root);
                NormalizeLayer(root);
                ReplaceFormComponent(root, formType);
                ClearButtonPersistentCalls(root);
                PrefabUtility.SaveAsPrefabAsset(root, targetPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void RemoveMissingScripts(GameObject root)
        {
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(transforms[i].gameObject);
            }
        }

        private static void EnsureRootComponents(GameObject root)
        {
            var canvas = root.GetOrAddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.worldCamera = null;
            canvas.planeDistance = 100f;
            canvas.overrideSorting = false;
            root.GetOrAddComponent<CanvasGroup>();
            root.GetOrAddComponent<GraphicRaycaster>();
        }

        private static void NormalizeLayer(GameObject root)
        {
            int uiLayer = LayerMask.NameToLayer("UI");
            if (root == null || uiLayer < 0)
            {
                return;
            }

            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                transforms[i].gameObject.layer = uiLayer;
                EditorUtility.SetDirty(transforms[i].gameObject);
            }
        }

        private static void ReplaceFormComponent(GameObject root, Type formType)
        {
            var forms = root.GetComponents<TotemUIFormBase>();
            for (int i = 0; i < forms.Length; i++)
            {
                if (forms[i] != null && forms[i].GetType() != formType)
                {
                    UnityEngine.Object.DestroyImmediate(forms[i], true);
                }
            }

            if (root.GetComponent(formType) == null)
            {
                root.AddComponent(formType);
            }
        }

        private static void ClearButtonPersistentCalls(GameObject root)
        {
            var buttons = root.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                buttons[i].onClick = new Button.ButtonClickedEvent();
                EditorUtility.SetDirty(buttons[i]);
            }
        }

        private readonly struct PrefabMigrationRule
        {
            public readonly string PrefabName;
            public readonly Type FormType;

            public PrefabMigrationRule(string prefabName, Type formType)
            {
                PrefabName = prefabName;
                FormType = formType;
            }
        }
    }
}
#endif
