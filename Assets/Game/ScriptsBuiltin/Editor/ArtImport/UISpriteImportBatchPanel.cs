using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TotemWarrior.EditorTools
{
    [ToolHubItem("美术工具/UI 图片导入初始化", "递归初始化指定文件夹中的图片导入设置", 10)]
    public sealed class UISpriteImportBatchPanel : IToolHubPanel
    {
        private static readonly int[] MaxTextureSizeOptions = { 256, 512, 1024, 2048, 4096, 8192 };
        private static readonly string[] MaxTextureSizeLabels = { "256", "512", "1024", "2048", "4096", "8192" };

        private DefaultAsset targetFolder;
        private int maxTextureSize = UISpriteImportSettings.DefaultMaxTextureSize;
        private Vector2 scrollPosition;
        private readonly List<string> lastProcessedPaths = new();
        private string status = "选择一个 Assets 下的文件夹后执行。";

        public void OnEnable()
        {
            if (targetFolder == null)
            {
                targetFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(UISpriteImportSettings.UISpriteRoot.TrimEnd('/'));
            }
        }

        public void OnDisable()
        {
        }

        public void OnDestroy()
        {
        }

        public string GetHelpText()
        {
            return "对指定文件夹及子文件夹中的 Texture2D 资源应用 UI Sprite 默认导入设置，并按指定 maxTextureSize 重新导入。";
        }

        public void OnGUI()
        {
            EditorGUILayout.LabelField("UI 图片导入初始化", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "会递归处理所选文件夹中的所有 Texture2D：Sprite Single、透明通道、关闭 Mipmap、Bilinear、PPU=100、压缩、指定 Max Texture Size。",
                MessageType.Info);

            targetFolder = (DefaultAsset)EditorGUILayout.ObjectField("目标文件夹", targetFolder, typeof(DefaultAsset), false);
            maxTextureSize = EditorGUILayout.IntPopup("Max Texture Size", maxTextureSize, MaxTextureSizeLabels, MaxTextureSizeOptions);

            bool canProcess = CanProcess(out string canProcessReason);
            using (new EditorGUI.DisabledScope(!canProcess))
            {
                if (GUILayout.Button("递归初始化并重新导入", GUILayout.Height(32)))
                {
                    ProcessFolder();
                }
            }

            if (!string.IsNullOrEmpty(canProcessReason))
            {
                EditorGUILayout.HelpBox(canProcessReason, MessageType.Warning);
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField(status, EditorStyles.wordWrappedLabel);

            if (lastProcessedPaths.Count <= 0)
            {
                return;
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField($"上次处理资源 ({lastProcessedPaths.Count})", EditorStyles.boldLabel);
            using (var scope = new EditorGUILayout.ScrollViewScope(scrollPosition, GUILayout.MinHeight(140)))
            {
                scrollPosition = scope.scrollPosition;
                foreach (string path in lastProcessedPaths)
                {
                    EditorGUILayout.LabelField(path, EditorStyles.miniLabel);
                }
            }
        }

        private bool CanProcess(out string reason)
        {
            reason = string.Empty;
            if (targetFolder == null)
            {
                reason = "请先选择目标文件夹。";
                return false;
            }

            string folderPath = AssetDatabase.GetAssetPath(targetFolder);
            if (string.IsNullOrWhiteSpace(folderPath) || !folderPath.StartsWith("Assets/") || !AssetDatabase.IsValidFolder(folderPath))
            {
                reason = "目标必须是 Assets 下的有效文件夹。";
                return false;
            }

            return true;
        }

        private void ProcessFolder()
        {
            if (!CanProcess(out string reason))
            {
                status = reason;
                return;
            }

            string folderPath = AssetDatabase.GetAssetPath(targetFolder);
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folderPath });
            lastProcessedPaths.Clear();

            int processed = 0;
            try
            {
                for (int i = 0; i < guids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    if (EditorUtility.DisplayCancelableProgressBar(
                            "UI 图片导入初始化",
                            $"{path} ({i + 1}/{guids.Length})",
                            (float)i / Mathf.Max(1, guids.Length)))
                    {
                        status = $"已取消。已处理 {processed} / {guids.Length} 个图片。";
                        return;
                    }

                    if (ApplyToAsset(path))
                    {
                        processed++;
                        lastProcessedPaths.Add(path);
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            status = $"完成。已处理 {processed} / {guids.Length} 个图片，Max Texture Size={maxTextureSize}。";
            Debug.Log($"[UISpriteImportBatchPanel] {status} Folder={folderPath}");
        }

        private bool ApplyToAsset(string path)
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
            {
                return false;
            }

            UISpriteImportSettings.Apply(importer, maxTextureSize);
            importer.SaveAndReimport();
            return true;
        }
    }
}
