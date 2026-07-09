#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using GameFramework;
using GameFramework.Procedure;
using UnityEditorInternal;
using System;

namespace UGF.EditorTools
{
    [Flags]
    public enum GameDataType
    {
        DataTable = 1,
        Config = 2,
        Language = 4
    }

    [CustomEditor(typeof(AppConfigs))]
    public class AppConfigsInspector : UnityEditor.Editor
    {
        public const int ONE_LINE_SHOW_COUNT = 3;
        private class ItemData
        {
            public bool isOn;
            public string excelName { get; private set; }

            public ItemData(bool isOn, string dllName)
            {
                this.isOn = isOn;
                this.excelName = dllName;
            }
        }
        private class GameDataScrollView
        {
            public bool foldout = true;
            public GameDataType CfgType { get; private set; }
            public Vector2 scrollPos;//记录滚动列表位置
            public string excelDir;
            public string excelOuputDir;
            private string newExcelName;
            private AppConfigs appConfig;
            public List<ItemData> ExcelItems { get; private set; }
            private GUIStyle normalStyle;
            private GUIStyle selectedStyle;
            private bool loadedFromDirectory;
            GUIContent titleContent;
            public GameDataScrollView(AppConfigs cfg, GameDataType configTp)
            {
                ExcelItems = new List<ItemData>();
                normalStyle = new GUIStyle();
                normalStyle.normal.textColor = Color.white;
                selectedStyle = new GUIStyle();
                selectedStyle.normal.textColor = ColorUtility.TryParseHtmlString("#2BD988", out var textCol) ? textCol : Color.green;
                this.CfgType = configTp;
                this.excelDir = GameDataGenerator.GetGameDataExcelDir(configTp);
                this.excelOuputDir = GameDataGenerator.GetGameDataExcelOutputDir(configTp);
                this.appConfig = cfg;
                titleContent = new GUIContent(configTp.ToString());
                switch (configTp)
                {
                    case GameDataType.DataTable:
                        titleContent.tooltip = "选择项目需要用到的数据表";
                        break;
                    case GameDataType.Config:
                        titleContent.tooltip = "选择项目需要用到的常量配置表";
                        break;
                    case GameDataType.Language:
                        titleContent.tooltip = "选择项目需要用到的多语言表";
                        break;
                    default:
                        break;
                }
            }
            public void Reload()
            {
                ExcelItems ??= new List<ItemData>();
                ExcelItems.Clear();
                loadedFromDirectory = false;
                if (appConfig == null)
                {
                    return;
                }

                if (!Directory.Exists(excelDir))
                {
                    return;
                }

                var mainExcels = GameDataGenerator.GetAllGameDataExcels(this.CfgType, GameDataExcelFileType.MainFile);
                loadedFromDirectory = true;

                string[] desArr = GetGameDataList() ?? Array.Empty<string>();
                foreach (var mainExcelFile in mainExcels)
                {
                    var mainExcelRelativePath = GameDataGenerator.GetGameDataExcelRelativePath(this.CfgType, mainExcelFile);
                    var isOn = ArrayUtility.Contains(desArr, mainExcelRelativePath);
                    ExcelItems.Add(new ItemData(isOn, mainExcelRelativePath));
                }
            }
            public string[] GetSelectedItems()
            {
                if (ExcelItems == null)
                {
                    return Array.Empty<string>();
                }

                if (!loadedFromDirectory)
                {
                    return GetGameDataList() ?? Array.Empty<string>();
                }

                var selectedList = ExcelItems.Where(dt => dt.isOn).ToArray();
                string[] resultArr = new string[selectedList.Length];
                for (int i = 0; i < selectedList.Length; i++)
                {
                    resultArr[i] = selectedList[i].excelName;
                }
                return resultArr;
            }

            internal void SetSelectAll(bool v)
            {
                if (ExcelItems == null)
                {
                    return;
                }

                if (!loadedFromDirectory)
                {
                    return;
                }

                foreach (var item in ExcelItems)
                {
                    item.isOn = v;
                }
            }

            internal bool DrawPanel(GUILayoutOption perItemWidth)
            {
                bool dataChanged = false;
                var dataTypeStr = this.CfgType.ToString();
                this.foldout = EditorGUILayout.Foldout(this.foldout, titleContent);
                if (foldout)
                {
                    ExcelItems ??= new List<ItemData>();
                    EditorGUILayout.BeginVertical();
                    {
                        if (!Directory.Exists(excelDir))
                        {
                            EditorGUILayout.HelpBox($"Excel directory does not exist: {excelDir}", MessageType.Info);
                        }
                        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, "box", GUILayout.MaxHeight(200));
                        {
                            EditorGUI.BeginChangeCheck();
                            for (int i = 0; i < ExcelItems.Count; i++)
                            {
                                if (i % ONE_LINE_SHOW_COUNT == 0)
                                    EditorGUILayout.BeginHorizontal();
                                var item = ExcelItems[i];
                                item.isOn = EditorGUILayout.ToggleLeft(item.excelName, item.isOn, item.isOn ? selectedStyle : normalStyle, perItemWidth);
                                if (i % ONE_LINE_SHOW_COUNT == ONE_LINE_SHOW_COUNT - 1)
                                    EditorGUILayout.EndHorizontal();
                            }
                            if (EditorGUI.EndChangeCheck())
                            {
                                dataChanged = true;
                            }
                            if (ExcelItems.Count % ONE_LINE_SHOW_COUNT != 0)
                                EditorGUILayout.EndHorizontal();
                            EditorGUILayout.EndScrollView();
                        }
                        EditorGUILayout.BeginHorizontal("box");
                        {
                            if (GUILayout.Button("All", GUILayout.Width(50)))
                            {
                                SetSelectAll(true);
                                dataChanged = true;
                            }
                            if (GUILayout.Button("None", GUILayout.Width(50)))
                            {
                                SetSelectAll(false);
                                dataChanged = true;
                            }
                            GUILayout.FlexibleSpace();

                            if (GUILayout.Button("Reveal", GUILayout.Width(70)))
                            {
                                EditorUtility.RevealInFinder(excelDir);
                                GUIUtility.ExitGUI();
                            }
                            if (GUILayout.Button("Export", GUILayout.Width(70)))
                            {
                                Export();
                            }
                            if (this.CfgType == GameDataType.DataTable && GUILayout.Button("AI Reverse", GUILayout.Width(90)))
                            {
                                ReverseAIJsonToExcel();
                            }
                            EditorGUILayout.EndHorizontal();
                        }
                        if (this.CfgType == GameDataType.DataTable || this.CfgType == GameDataType.Config)
                        {
                            EditorGUILayout.BeginHorizontal("box");
                            {
                                newExcelName = EditorGUILayout.TextField(newExcelName);
                                if (GUILayout.Button($"New {dataTypeStr}", GUILayout.Width(100)))
                                {
                                    CreateExcel(newExcelName);
                                }
                                EditorGUILayout.EndHorizontal();
                            }
                        }
                        EditorGUILayout.EndVertical();
                    }
                }
                return dataChanged;
            }
            private void Export()
            {
                switch (this.CfgType)
                {
                    case GameDataType.DataTable:
                        GameDataGenerator.RefreshAllDataTable(GameDataGenerator.GameDataExcelRelative2FullPath(CfgType, GetGameDataList()));
                        GameDataGenerator.GenerateUIFormNamesScript();
                        break;
                    case GameDataType.Config:
                        GameDataGenerator.RefreshAllConfig(GameDataGenerator.GameDataExcelRelative2FullPath(CfgType, GetGameDataList()));
                        break;
                    case GameDataType.Language:
                        GameDataGenerator.RefreshAllLanguage(GameDataGenerator.GameDataExcelRelative2FullPath(CfgType, GetGameDataList()));
                        break;
                }
            }
            private void ReverseAIJsonToExcel()
            {
                if (this.CfgType != GameDataType.DataTable)
                {
                    return;
                }

                bool confirm = EditorUtility.DisplayDialog(
                    "AI Reverse DataTables",
                    "This will overwrite selected DataTable xlsx files from GameData/AIData/DataTables/*.json after validation. Continue?",
                    "Reverse",
                    "Cancel");

                if (!confirm)
                {
                    return;
                }

                var report = AIGameDataTableGenerator.ReverseDataTablesJsonToExcelByRelativePaths(GetSelectedItems());
                string message = report.failureCount > 0
                    ? $"Finished with {report.failureCount} failed, {report.successCount} succeeded. See AIData report for details."
                    : $"Reverse finished. {report.successCount} xlsx files updated.";
                if (report.warningCount > 0)
                {
                    message += $" Warnings: {report.warningCount}.";
                }

                EditorUtility.DisplayDialog("AI Reverse DataTables", message, "OK");
                Reload();
            }
            private string[] GetGameDataList()
            {
                switch (this.CfgType)
                {
                    case GameDataType.DataTable:
                        return appConfig != null ? appConfig.DataTables : Array.Empty<string>();
                    case GameDataType.Config:
                        return appConfig != null ? appConfig.Configs : Array.Empty<string>();
                    case GameDataType.Language:
                        return appConfig != null ? appConfig.Languages : Array.Empty<string>();
                    default:
                        return Array.Empty<string>();
                }
            }
            private void CreateExcel(string newExcelName)
            {
                switch (this.CfgType)
                {
                    case GameDataType.DataTable:
                        CreateDataTableExcel(newExcelName);
                        break;
                    case GameDataType.Config:
                        CreateConfigExcel(newExcelName);
                        break;
                }
            }
            private void CreateDataTableExcel(string v)
            {
                if (string.IsNullOrWhiteSpace(v))
                {
                    return;
                }
                var excelPath = UtilityBuiltin.AssetsPath.GetCombinePath(excelDir, v + ".xlsx");
                if (File.Exists(excelPath))
                {
                    Debug.LogWarning($"创建DataTable失败, 文件已存在:{excelPath}");
                    return;
                }
                if (GameDataGenerator.CreateDataTableExcel(excelPath))
                {
                    Reload();
                    EditorUtility.RevealInFinder(excelPath);
                    GUIUtility.ExitGUI();
                }
            }
            private void CreateConfigExcel(string v)
            {
                if (string.IsNullOrWhiteSpace(v))
                {
                    return;
                }
                var excelPath = UtilityBuiltin.AssetsPath.GetCombinePath(excelDir, v + ".xlsx");
                if (File.Exists(excelPath))
                {
                    Debug.LogWarning($"创建Config失败, 文件已存在:{excelPath}");
                    return;
                }
                if (GameDataGenerator.CreateGameConfigExcel(excelPath))
                {
                    Reload();
                    EditorUtility.RevealInFinder(excelPath);
                    GUIUtility.ExitGUI();
                }
            }
        }
        AppConfigs appConfig;
        GameDataScrollView[] svDataArr;
        bool procedureFoldout = true;
        Vector2 procedureScrollPos;
        ItemData[] procedures;
        bool proceduresLoaded;
        private GUIStyle normalStyle;
        private GUIStyle selectedStyle;
        GUIContent procedureTitleContent;
        GUIContent editorConstSettingsContent;
        GUIContent loadFromBytesContent;
        private void OnEnable()
        {
            appConfig = AppConfigs.ReloadInstanceEditor() ?? target as AppConfigs;
            normalStyle = new GUIStyle();
            normalStyle.normal.textColor = Color.white;
            selectedStyle = new GUIStyle();
            selectedStyle.normal.textColor = ColorUtility.TryParseHtmlString("#2BD988", out var textCol) ? textCol : Color.green;

            procedureTitleContent = new GUIContent("流程(Procedures)", "勾选的流程在有限状态机中有效");
            editorConstSettingsContent = EditorGUIUtility.TrTextContentWithIcon("Path Settings [设置DataTable/Config导入/导出路径]", "Settings");
            loadFromBytesContent = new GUIContent("Load from bytes(勾选:二进制模式; 不勾选:文本模式)", "数据表/配置表/多语言表使用二进制模式");
            svDataArr = new GameDataScrollView[] { new GameDataScrollView(appConfig, GameDataType.DataTable), new GameDataScrollView(appConfig, GameDataType.Config), new GameDataScrollView(appConfig, GameDataType.Language) };
            ReloadScrollView(appConfig);
        }
        private void OnDisable()
        {
            if (appConfig != null)
            {
                SaveConfig(appConfig);
            }
        }
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            if (GUILayout.Button(editorConstSettingsContent))
            {
                InternalEditorUtility.OpenFileAtLineExternal(Path.Combine(Path.GetDirectoryName(ConstEditor.BuiltinAssembly), "../Editor/Common/ConstEditor.cs"), 0);
            }

            EditorGUILayout.Space(10);
            appConfig.LoadFromBytes = EditorGUILayout.ToggleLeft(loadFromBytesContent, appConfig.LoadFromBytes);
            procedureFoldout = EditorGUILayout.Foldout(procedureFoldout, procedureTitleContent);
            var perItemWidth = GUILayout.Width(Mathf.Max(EditorGUIUtility.currentViewWidth / ONE_LINE_SHOW_COUNT - 20, 100));
            if (procedureFoldout)
            {
                EditorGUILayout.BeginVertical();
                {
                    procedureScrollPos = EditorGUILayout.BeginScrollView(procedureScrollPos, "box", GUILayout.Height(200));
                    {
                        EditorGUI.BeginChangeCheck();
                        for (int i = 0; i < procedures.Length; i++)
                        {
                            if (i % ONE_LINE_SHOW_COUNT == 0)
                                EditorGUILayout.BeginHorizontal();
                            var item = procedures[i];
                            item.isOn = EditorGUILayout.ToggleLeft(item.excelName, item.isOn, item.isOn ? selectedStyle : normalStyle, perItemWidth);
                            if (i % ONE_LINE_SHOW_COUNT == ONE_LINE_SHOW_COUNT - 1)
                                EditorGUILayout.EndHorizontal();
                        }
                        if (EditorGUI.EndChangeCheck())
                        {
                            SaveConfig(appConfig);
                        }
                        if (procedures.Length % ONE_LINE_SHOW_COUNT != 0)
                            EditorGUILayout.EndHorizontal();
                        EditorGUILayout.EndScrollView();
                    }
                    EditorGUILayout.EndVertical();
                }
            }
            foreach (var item in svDataArr)
            {
                if (item.DrawPanel(perItemWidth))
                {
                    SaveConfig(appConfig);
                }
            }
            EditorGUILayout.Space(10);
            EditorGUILayout.BeginHorizontal("box");
            {
                if (GUILayout.Button("Reload", GUILayout.Height(30)))
                {
                    ReloadScrollView(appConfig);
                }
                if (GUILayout.Button("Save", GUILayout.Height(30)))
                {
                    SaveConfig(appConfig);
                }
                EditorGUILayout.EndHorizontal();
            }
            serializedObject.ApplyModifiedProperties();
        }

        

        private void SaveConfig(AppConfigs cfg)
        {
            if (cfg == null)
            {
                return;
            }

            foreach (var svData in svDataArr ?? Array.Empty<GameDataScrollView>())
            {
                if (svData.CfgType == GameDataType.DataTable)
                {
                    cfg.GetType().GetField("mDataTables", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(cfg, svData.GetSelectedItems());
                }
                else if (svData.CfgType == GameDataType.Config)
                {
                    cfg.GetType().GetField("mConfigs", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(cfg, svData.GetSelectedItems());
                }
                else if (svData.CfgType == GameDataType.Language)
                {
                    cfg.GetType().GetField("mLanguages", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(cfg, svData.GetSelectedItems());
                }
            }
            if (proceduresLoaded && procedures != null && procedures.Length > 0)
            {
                List<string> selectedProcedures = new List<string>();
                foreach (var item in procedures)
                {
                    if (!item.isOn) continue;
                    selectedProcedures.Add(item.excelName);
                }
                cfg.GetType().GetField("mProcedures", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(cfg, selectedProcedures.ToArray());
            }
            EditorUtility.SetDirty(cfg);
        }
        private void ReloadScrollView(AppConfigs cfg)
        {
            foreach (var item in svDataArr ?? Array.Empty<GameDataScrollView>())
            {
                item.Reload();
            }

            ReloadProcedures(cfg);
        }
        private void ReloadProcedures(AppConfigs cfg)
        {
            proceduresLoaded = false;
            procedures ??= new ItemData[0];
            ArrayUtility.Clear(ref procedures);
            var hotfixDlls = Utility.Assembly.GetAssemblies().Where(dll => HybridCLR.Editor.SettingsUtil.HotUpdateAssemblyNamesIncludePreserved.Contains(dll.GetName().Name)).ToArray();
            string[] selectedProcedures = cfg?.Procedures ?? Array.Empty<string>();

            foreach (var item in hotfixDlls)
            {
                var proceClassArr = item.GetTypes().Where(tp => tp.BaseType == typeof(ProcedureBase)).ToArray();
                foreach (var proceClass in proceClassArr)
                {
                    var proceName = proceClass.FullName;
                    ArrayUtility.Add(ref procedures, new ItemData(selectedProcedures.Contains(proceName), proceName));
                }
            }
            proceduresLoaded = true;
        }
    }

}
#endif
