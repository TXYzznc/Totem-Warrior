#if UNITY_EDITOR
// 独立 Editor-only 工具程序集，单向引用 Builtin.Editor 的工具箱扩展点。
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// 将多个 Assets 路径合并导出为一个 UnityPackage。
/// </summary>
[ToolHubItem(
    "资源工具/UnityPackage一键导出",
    "将多个资源路径合并导出为一个 UnityPackage，可选择是否包含路径外依赖",
    10
)]
public sealed class ResourcePackageExporterPanel : IToolHubPanel
{
    private const string PrefsKey = "GameDesigner.ResourcePackageExporter.State.v1";
    private const float ActionButtonHeight = 30f;
    private const float CombinationPanelWidth = 240f;

    private readonly List<string> targetPaths = new List<string>();
    private readonly List<SavedCombination> savedCombinations = new List<SavedCombination>();
    private string outputPath;
    private string combinationName = string.Empty;
    private string editingCombinationId;
    private bool includeDependencies = true;
    private bool revealAfterExport = true;
    private Vector2 scrollPosition;
    private Vector2 combinationScrollPosition;
    private ExportPreview preview;

    public void OnEnable()
    {
        LoadState();
    }

    public void OnDisable()
    {
        SaveState();
    }

    public void OnDestroy()
    {
        SaveState();
    }

    public string GetHelpText()
    {
        return "把多个 Assets 路径合并导出为一个 .unitypackage；文件夹会递归导出，可选择是否包含列表外依赖。";
    }

    public void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("UnityPackage 一键导出", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "列表中的路径会合并成一个包。文件夹自动递归；.meta 由 Unity 自动包含，不要单独加入列表。",
            MessageType.Info
        );

        EditorGUILayout.Space(6f);
        DrawTargetPathsAndCombinations();
        EditorGUILayout.Space(8f);
        DrawOutputSettings();
        EditorGUILayout.Space(8f);
        DrawExportOptions();
        EditorGUILayout.Space(8f);
        DrawPreview();
        EditorGUILayout.Space(8f);
        DrawActions();
        EditorGUILayout.Space(10f);
        EditorGUILayout.EndScrollView();
    }

    private void DrawTargetPathsAndCombinations()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true)))
                DrawTargetPaths();

            GUILayout.Space(6f);
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(CombinationPanelWidth)))
                DrawSavedCombinations();
        }
    }

    private void DrawTargetPaths()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("目标资源路径", EditorStyles.boldLabel);

            for (int index = 0; index < targetPaths.Count; index++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUI.BeginChangeCheck();
                    string value = EditorGUILayout.DelayedTextField(targetPaths[index]);
                    if (EditorGUI.EndChangeCheck())
                    {
                        targetPaths[index] = ResourcePackageExportUtility.NormalizeAssetPath(value);
                        InvalidatePreview();
                        SaveState();
                    }

                    if (GUILayout.Button("定位", GUILayout.Width(44f)))
                        PingAsset(targetPaths[index]);

                    if (GUILayout.Button("×", GUILayout.Width(26f)))
                    {
                        targetPaths.RemoveAt(index);
                        index--;
                        InvalidatePreview();
                        SaveState();
                    }
                }
            }

            Rect dropRect = GUILayoutUtility.GetRect(0f, 48f, GUILayout.ExpandWidth(true));
            GUI.Box(dropRect, "拖入 Project 中的资源或文件夹", EditorStyles.helpBox);
            HandleDragAndDrop(dropRect);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("添加空路径"))
                {
                    targetPaths.Add("Assets/");
                    InvalidatePreview();
                    SaveState();
                }

                if (GUILayout.Button("添加选中资源"))
                    AddSelectedAssets();

                if (GUILayout.Button("浏览文件夹..."))
                    BrowseAssetFolder();

                using (new EditorGUI.DisabledScope(targetPaths.Count == 0))
                {
                    if (GUILayout.Button("清空", GUILayout.Width(52f)))
                    {
                        targetPaths.Clear();
                        InvalidatePreview();
                        SaveState();
                    }
                }
            }

            EditorGUILayout.LabelField($"已配置 {targetPaths.Count} 个路径", EditorStyles.miniLabel);
            EditorGUILayout.Space(4f);
            DrawCombinationSaveControls();
        }
    }

    private void DrawCombinationSaveControls()
    {
        EditorGUILayout.LabelField(
            string.IsNullOrEmpty(editingCombinationId) ? "保存当前路径组合" : "编辑已保存组合",
            EditorStyles.miniBoldLabel
        );
        using (new EditorGUILayout.HorizontalScope())
        {
            combinationName = EditorGUILayout.TextField(combinationName);
            string actionLabel = string.IsNullOrEmpty(editingCombinationId) ? "保存组合" : "更新组合";
            if (GUILayout.Button(actionLabel, GUILayout.Width(76f)))
                SaveCurrentCombination();
        }

        if (!string.IsNullOrEmpty(editingCombinationId))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("修改路径后点击“更新组合”才会写回。", EditorStyles.miniLabel);
                if (GUILayout.Button("另存为", GUILayout.Width(64f)))
                {
                    editingCombinationId = null;
                    combinationName = string.Empty;
                }
            }
        }
    }

    private void DrawSavedCombinations()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("已保存组合", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("勾选多个组合会合并成一个包", EditorStyles.miniLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("全选", EditorStyles.miniButtonLeft))
                    SetAllCombinationsSelected(true);
                if (GUILayout.Button("全不选", EditorStyles.miniButtonRight))
                    SetAllCombinationsSelected(false);
            }

            combinationScrollPosition = EditorGUILayout.BeginScrollView(
                combinationScrollPosition,
                GUILayout.MinHeight(155f),
                GUILayout.MaxHeight(260f)
            );
            if (savedCombinations.Count == 0)
            {
                EditorGUILayout.HelpBox("尚未保存组合。", MessageType.Info);
            }
            else
            {
                for (int index = 0; index < savedCombinations.Count; index++)
                    DrawSavedCombinationRow(savedCombinations[index]);
            }
            EditorGUILayout.EndScrollView();

            int selectedCount = savedCombinations.Count(item => item.selected);
            EditorGUILayout.LabelField($"已选择 {selectedCount}/{savedCombinations.Count} 个组合", EditorStyles.miniLabel);
            if (selectedCount == 0)
                EditorGUILayout.LabelField("未选组合时导出左侧当前列表", EditorStyles.miniLabel);
        }
    }

    private void DrawSavedCombinationRow(SavedCombination combination)
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginChangeCheck();
                combination.selected = EditorGUILayout.Toggle(combination.selected, GUILayout.Width(18f));
                if (EditorGUI.EndChangeCheck())
                {
                    InvalidatePreview();
                    SaveState();
                }

                GUIStyle nameStyle = new GUIStyle(EditorStyles.miniButton)
                {
                    alignment = TextAnchor.MiddleLeft,
                    fontStyle = combination.id == editingCombinationId ? FontStyle.Bold : FontStyle.Normal,
                };
                if (GUILayout.Button(new GUIContent(combination.name, "载入到左侧进行查看或编辑"), nameStyle))
                    LoadCombination(combination);

                if (GUILayout.Button("×", EditorStyles.miniButton, GUILayout.Width(24f)))
                    DeleteCombination(combination);
            }
            EditorGUILayout.LabelField($"{combination.targetPaths.Count} 个路径", EditorStyles.centeredGreyMiniLabel);
        }
    }

    private void SaveCurrentCombination()
    {
        string trimmedName = combinationName == null ? string.Empty : combinationName.Trim();
        if (string.IsNullOrEmpty(trimmedName))
        {
            EditorUtility.DisplayDialog("无法保存组合", "请输入组合名称。", "确定");
            return;
        }

        ExportPreview validation = ResourcePackageExportUtility.BuildPreview(targetPaths, false);
        if (validation.Errors.Count > 0)
        {
            EditorUtility.DisplayDialog("无法保存组合", string.Join("\n", validation.Errors), "确定");
            return;
        }

        SavedCombination target = savedCombinations.FirstOrDefault(item => item.id == editingCombinationId);
        SavedCombination sameName = savedCombinations.FirstOrDefault(item =>
            string.Equals(item.name, trimmedName, StringComparison.OrdinalIgnoreCase)
        );
        if (target == null && sameName != null)
        {
            if (!EditorUtility.DisplayDialog("覆盖已有组合", $"已存在组合“{sameName.name}”，是否覆盖？", "覆盖", "取消"))
                return;
            target = sameName;
        }
        else if (target != null && sameName != null && sameName != target)
        {
            EditorUtility.DisplayDialog("名称重复", $"已存在组合“{sameName.name}”，请使用其他名称。", "确定");
            return;
        }

        if (target == null)
        {
            target = new SavedCombination { id = Guid.NewGuid().ToString("N"), selected = true };
            savedCombinations.Add(target);
        }

        target.name = trimmedName;
        target.targetPaths = ResourcePackageExportUtility.NormalizeAndDeduplicate(targetPaths);
        target.selected = true;
        editingCombinationId = target.id;
        combinationName = target.name;
        InvalidatePreview();
        SaveState();
    }

    private void LoadCombination(SavedCombination combination)
    {
        targetPaths.Clear();
        targetPaths.AddRange(combination.targetPaths ?? new List<string>());
        combinationName = combination.name;
        editingCombinationId = combination.id;
        InvalidatePreview();
        SaveState();
    }

    private void DeleteCombination(SavedCombination combination)
    {
        if (!EditorUtility.DisplayDialog("删除组合", $"确定删除组合“{combination.name}”吗？", "删除", "取消"))
            return;

        savedCombinations.Remove(combination);
        if (editingCombinationId == combination.id)
        {
            editingCombinationId = null;
            combinationName = string.Empty;
        }
        InvalidatePreview();
        SaveState();
    }

    private void SetAllCombinationsSelected(bool selected)
    {
        foreach (SavedCombination combination in savedCombinations)
            combination.selected = selected;
        InvalidatePreview();
        SaveState();
    }

    private void DrawOutputSettings()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("输出文件", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            outputPath = EditorGUILayout.TextField("Package 路径", outputPath);
            if (EditorGUI.EndChangeCheck())
                SaveState();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("选择保存位置...", GUILayout.Height(24f)))
                    BrowseOutputPath();

                if (GUILayout.Button("恢复默认", GUILayout.Width(80f), GUILayout.Height(24f)))
                {
                    outputPath = GetDefaultOutputPath();
                    SaveState();
                }

                using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(outputPath)))
                {
                    if (GUILayout.Button("打开目录", GUILayout.Width(80f), GUILayout.Height(24f)))
                        RevealOutputDirectory();
                }
            }
        }
    }

    private void DrawExportOptions()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("导出选项", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            includeDependencies = EditorGUILayout.ToggleLeft(
                new GUIContent(
                    "包含路径外依赖资源",
                    "等同 Unity Export Package 窗口中的 Include dependencies；关闭后只导出列表路径及文件夹内容。"
                ),
                includeDependencies
            );
            revealAfterExport = EditorGUILayout.ToggleLeft("导出完成后在文件管理器中定位", revealAfterExport);
            if (EditorGUI.EndChangeCheck())
            {
                InvalidatePreview();
                SaveState();
            }
        }
    }

    private void DrawPreview()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("导出预览", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("刷新统计", GUILayout.Width(88f)))
                    RefreshPreview();
            }

            if (preview == null)
            {
                EditorGUILayout.LabelField("尚未统计。导出前会自动校验和统计。", EditorStyles.miniLabel);
                return;
            }

            if (preview.Errors.Count > 0)
            {
                foreach (string error in preview.Errors)
                    EditorGUILayout.HelpBox(error, MessageType.Error);
                return;
            }

            EditorGUILayout.LabelField("有效目标", preview.TargetPaths.Count.ToString());
            int selectedCombinationCount = savedCombinations.Count(item => item.selected);
            EditorGUILayout.LabelField(
                "导出来源",
                selectedCombinationCount > 0 ? $"{selectedCombinationCount} 个已选组合" : "左侧当前列表"
            );
            EditorGUILayout.LabelField("目标内资源", preview.ExplicitAssetCount.ToString());
            EditorGUILayout.LabelField("最终资源数", preview.TotalAssetCount.ToString());
            if (includeDependencies)
                EditorGUILayout.LabelField("额外依赖", preview.DependencyAssetCount.ToString());
        }
    }

    private void DrawActions()
    {
        using (new EditorGUI.DisabledScope(GetExportTargetPaths().Count == 0))
        {
            if (GUILayout.Button("导出 UnityPackage", GUILayout.Height(38f)))
                ExportPackage();
        }

        using (new EditorGUI.DisabledScope(targetPaths.Count == 0))
        {
            if (GUILayout.Button("整理列表（规范路径并去重）", GUILayout.Height(ActionButtonHeight)))
            {
                List<string> normalized = ResourcePackageExportUtility.NormalizeAndDeduplicate(targetPaths);
                targetPaths.Clear();
                targetPaths.AddRange(normalized);
                InvalidatePreview();
                SaveState();
            }
        }
    }

    private void ExportPackage()
    {
        RefreshPreview();
        if (preview == null || preview.Errors.Count > 0)
        {
            EditorUtility.DisplayDialog("无法导出", "请先修正导出预览中的错误。", "确定");
            return;
        }

        string finalOutputPath = ResourcePackageExportUtility.NormalizeOutputPath(outputPath);
        if (string.IsNullOrEmpty(finalOutputPath))
        {
            EditorUtility.DisplayDialog("无法导出", "请选择有效的 .unitypackage 输出路径。", "确定");
            return;
        }

        string summary =
            $"导出组合：{GetExportSourceDescription()}\n"
            + $"目标路径：{preview.TargetPaths.Count}\n"
            + $"最终资源：{preview.TotalAssetCount}\n"
            + $"包含路径外依赖：{(includeDependencies ? "是" : "否")}\n\n"
            + finalOutputPath;
        if (!EditorUtility.DisplayDialog("确认导出 UnityPackage", summary, "开始导出", "取消"))
            return;

        try
        {
            string directory = Path.GetDirectoryName(finalOutputPath);
            if (string.IsNullOrEmpty(directory))
                throw new InvalidOperationException("无法确定输出目录。");
            Directory.CreateDirectory(directory);

            ExportPackageOptions options = ExportPackageOptions.Recurse;
            if (includeDependencies)
                options |= ExportPackageOptions.IncludeDependencies;

            AssetDatabase.ExportPackage(preview.TargetPaths.ToArray(), finalOutputPath, options);
            if (!File.Exists(finalOutputPath))
                throw new IOException("Unity 未生成目标文件。");

            outputPath = finalOutputPath;
            SaveState();
            Debug.Log(
                $"[ResourcePackageExporter] 导出完成：{finalOutputPath}，组合 {GetExportSourceDescription()}，"
                + $"目标 {preview.TargetPaths.Count}，资源 {preview.TotalAssetCount}"
            );

            if (revealAfterExport)
                EditorUtility.RevealInFinder(finalOutputPath);

            long bytes = new FileInfo(finalOutputPath).Length;
            EditorUtility.DisplayDialog(
                "导出完成",
                $"UnityPackage 已生成\n大小：{EditorUtility.FormatBytes(bytes)}\n\n{finalOutputPath}",
                "确定"
            );
        }
        catch (Exception exception)
        {
            Debug.LogError($"[ResourcePackageExporter] 导出失败：{exception}");
            EditorUtility.DisplayDialog("导出失败", exception.Message, "确定");
        }
    }

    private void RefreshPreview()
    {
        preview = ResourcePackageExportUtility.BuildPreview(GetExportTargetPaths(), includeDependencies);
    }

    private List<string> GetExportTargetPaths()
    {
        List<SavedCombination> selected = savedCombinations.Where(item => item.selected).ToList();
        if (selected.Count == 0)
            return ResourcePackageExportUtility.NormalizeAndDeduplicate(targetPaths);

        return ResourcePackageExportUtility.NormalizeAndDeduplicate(
            selected.SelectMany(item => item.targetPaths ?? new List<string>())
        );
    }

    private string GetExportSourceDescription()
    {
        List<string> selectedNames = savedCombinations
            .Where(item => item.selected)
            .Select(item => item.name)
            .ToList();
        return selectedNames.Count == 0 ? "当前列表" : string.Join("、", selectedNames);
    }

    private void InvalidatePreview()
    {
        preview = null;
    }

    private void AddSelectedAssets()
    {
        AddObjects(Selection.objects);
    }

    private void AddObjects(IEnumerable<Object> objects)
    {
        bool changed = false;
        foreach (Object target in objects)
        {
            string path = AssetDatabase.GetAssetPath(target);
            if (string.IsNullOrEmpty(path) || !path.StartsWith("Assets/", StringComparison.Ordinal))
                continue;

            path = ResourcePackageExportUtility.NormalizeAssetPath(path);
            if (!targetPaths.Contains(path, StringComparer.OrdinalIgnoreCase))
            {
                targetPaths.Add(path);
                changed = true;
            }
        }

        if (!changed)
            return;

        InvalidatePreview();
        SaveState();
    }

    private void HandleDragAndDrop(Rect dropRect)
    {
        Event current = Event.current;
        if (!dropRect.Contains(current.mousePosition))
            return;

        if (current.type == EventType.DragUpdated)
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            current.Use();
        }
        else if (current.type == EventType.DragPerform)
        {
            DragAndDrop.AcceptDrag();
            AddObjects(DragAndDrop.objectReferences);
            current.Use();
        }
    }

    private void BrowseAssetFolder()
    {
        string selected = EditorUtility.OpenFolderPanel("选择要导出的 Assets 文件夹", Application.dataPath, string.Empty);
        if (string.IsNullOrEmpty(selected))
            return;

        if (!ResourcePackageExportUtility.TryConvertAbsoluteToAssetPath(selected, out string assetPath))
        {
            EditorUtility.DisplayDialog("路径无效", "只能选择当前 Unity 项目 Assets 目录内的文件夹。", "确定");
            return;
        }

        if (!targetPaths.Contains(assetPath, StringComparer.OrdinalIgnoreCase))
            targetPaths.Add(assetPath);
        InvalidatePreview();
        SaveState();
    }

    private void BrowseOutputPath()
    {
        string current = ResourcePackageExportUtility.NormalizeOutputPath(outputPath);
        string directory = string.IsNullOrEmpty(current) ? GetDefaultOutputDirectory() : Path.GetDirectoryName(current);
        string fileName = string.IsNullOrEmpty(current) ? "ProjectResources" : Path.GetFileNameWithoutExtension(current);
        string selected = EditorUtility.SaveFilePanel("选择 UnityPackage 输出位置", directory, fileName, "unitypackage");
        if (string.IsNullOrEmpty(selected))
            return;

        outputPath = ResourcePackageExportUtility.NormalizeOutputPath(selected);
        SaveState();
    }

    private void RevealOutputDirectory()
    {
        string path = ResourcePackageExportUtility.NormalizeOutputPath(outputPath);
        string directory = string.IsNullOrEmpty(path) ? GetDefaultOutputDirectory() : Path.GetDirectoryName(path);
        if (string.IsNullOrEmpty(directory))
            return;
        Directory.CreateDirectory(directory);
        EditorUtility.RevealInFinder(directory);
    }

    private static void PingAsset(string path)
    {
        Object asset = AssetDatabase.LoadMainAssetAtPath(ResourcePackageExportUtility.NormalizeAssetPath(path));
        if (asset == null)
            return;
        Selection.activeObject = asset;
        EditorGUIUtility.PingObject(asset);
    }

    private void LoadState()
    {
        targetPaths.Clear();
        savedCombinations.Clear();
        string json = EditorPrefs.GetString(PrefsKey, string.Empty);
        if (!string.IsNullOrEmpty(json))
        {
            try
            {
                PersistedState state = JsonUtility.FromJson<PersistedState>(json);
                if (state != null)
                {
                    targetPaths.AddRange(state.targetPaths ?? new List<string>());
                    savedCombinations.AddRange(state.savedCombinations ?? new List<SavedCombination>());
                    outputPath = state.outputPath;
                    includeDependencies = state.includeDependencies;
                    revealAfterExport = state.revealAfterExport;
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[ResourcePackageExporter] 无法读取旧配置，将使用默认值：{exception.Message}");
            }
        }

        if (string.IsNullOrWhiteSpace(outputPath))
            outputPath = GetDefaultOutputPath();

        foreach (SavedCombination combination in savedCombinations)
        {
            if (string.IsNullOrEmpty(combination.id))
                combination.id = Guid.NewGuid().ToString("N");
            combination.targetPaths = ResourcePackageExportUtility.NormalizeAndDeduplicate(
                combination.targetPaths ?? new List<string>()
            );
        }
    }

    private void SaveState()
    {
        var state = new PersistedState
        {
            targetPaths = new List<string>(targetPaths),
            savedCombinations = new List<SavedCombination>(savedCombinations),
            outputPath = outputPath,
            includeDependencies = includeDependencies,
            revealAfterExport = revealAfterExport,
        };
        EditorPrefs.SetString(PrefsKey, JsonUtility.ToJson(state));
    }

    private static string GetDefaultOutputDirectory()
    {
        string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
        return Path.Combine(projectRoot, "Exports", "UnityPackages");
    }

    private static string GetDefaultOutputPath()
    {
        return Path.Combine(GetDefaultOutputDirectory(), "ProjectResources.unitypackage");
    }

    [Serializable]
    private sealed class PersistedState
    {
        public List<string> targetPaths = new List<string>();
        public List<SavedCombination> savedCombinations = new List<SavedCombination>();
        public string outputPath;
        public bool includeDependencies = true;
        public bool revealAfterExport = true;
    }

    [Serializable]
    private sealed class SavedCombination
    {
        public string id;
        public string name;
        public List<string> targetPaths = new List<string>();
        public bool selected;
    }
}

internal static class ResourcePackageExportUtility
{
    public static ExportPreview BuildPreview(IEnumerable<string> sourcePaths, bool includeDependencies)
    {
        var result = new ExportPreview();
        result.TargetPaths.AddRange(NormalizeAndDeduplicate(sourcePaths));
        if (result.TargetPaths.Count == 0)
        {
            result.Errors.Add("至少需要配置一个目标资源路径。");
            return result;
        }

        foreach (string path in result.TargetPaths)
        {
            if (!path.StartsWith("Assets/", StringComparison.Ordinal) && path != "Assets")
            {
                result.Errors.Add($"路径必须位于当前项目 Assets 目录：{path}");
                continue;
            }
            if (path.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
            {
                result.Errors.Add($"不要单独添加 .meta 文件：{path}");
                continue;
            }
            if (AssetDatabase.LoadMainAssetAtPath(path) == null && !AssetDatabase.IsValidFolder(path))
                result.Errors.Add($"资源路径不存在：{path}");
        }

        if (result.Errors.Count > 0)
            return result;

        HashSet<string> explicitAssets = CollectExplicitAssets(result.TargetPaths);
        result.ExplicitAssetCount = explicitAssets.Count;
        if (!includeDependencies || explicitAssets.Count == 0)
        {
            result.TotalAssetCount = explicitAssets.Count;
            return result;
        }

        string[] dependencies = AssetDatabase.GetDependencies(explicitAssets.ToArray(), true);
        int total = dependencies.Count(path => path.StartsWith("Assets/", StringComparison.Ordinal));
        result.TotalAssetCount = total;
        result.DependencyAssetCount = Math.Max(0, total - explicitAssets.Count);
        return result;
    }

    public static List<string> NormalizeAndDeduplicate(IEnumerable<string> sourcePaths)
    {
        return sourcePaths
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(NormalizeAssetPath)
            .Where(path => !string.IsNullOrEmpty(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static string NormalizeAssetPath(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;
        string normalized = value.Trim().Replace('\\', '/');
        while (normalized.Contains("//"))
            normalized = normalized.Replace("//", "/");
        return normalized.Length > 1 ? normalized.TrimEnd('/') : normalized;
    }

    public static string NormalizeOutputPath(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;
        string fullPath;
        try
        {
            fullPath = Path.GetFullPath(value.Trim());
        }
        catch
        {
            return string.Empty;
        }
        return fullPath.EndsWith(".unitypackage", StringComparison.OrdinalIgnoreCase)
            ? fullPath
            : fullPath + ".unitypackage";
    }

    public static bool TryConvertAbsoluteToAssetPath(string absolutePath, out string assetPath)
    {
        assetPath = string.Empty;
        if (string.IsNullOrWhiteSpace(absolutePath))
            return false;

        string assetsRoot = Path.GetFullPath(Application.dataPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string selected = Path.GetFullPath(absolutePath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (!selected.Equals(assetsRoot, StringComparison.OrdinalIgnoreCase)
            && !selected.StartsWith(assetsRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            return false;

        string relative = selected.Length == assetsRoot.Length
            ? string.Empty
            : selected.Substring(assetsRoot.Length + 1).Replace('\\', '/');
        assetPath = string.IsNullOrEmpty(relative) ? "Assets" : "Assets/" + relative;
        return true;
    }

    private static HashSet<string> CollectExplicitAssets(IEnumerable<string> targetPaths)
    {
        var assets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string path in targetPaths)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                foreach (string guid in AssetDatabase.FindAssets(string.Empty, new[] { path }))
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    if (!string.IsNullOrEmpty(assetPath) && !AssetDatabase.IsValidFolder(assetPath))
                        assets.Add(assetPath);
                }
            }
            else
            {
                assets.Add(path);
            }
        }
        return assets;
    }
}

internal sealed class ExportPreview
{
    public readonly List<string> TargetPaths = new List<string>();
    public readonly List<string> Errors = new List<string>();
    public int ExplicitAssetCount;
    public int DependencyAssetCount;
    public int TotalAssetCount;
}
#endif
