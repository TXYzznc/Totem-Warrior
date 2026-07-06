#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Playtest.EditorTools
{
    public sealed class ManualPlaytestLogRecorderWindow : EditorWindow
    {
        const string OutputDir = "tools/playtest/reports/manual-log-sessions";

        static readonly List<TestSection> s_sections = new();
        static readonly List<LogLine> s_currentLogs = new();
        static bool s_recording;
        static DateTime s_recordStart;
        static string s_lastOutputPath = "";

        string _testId = "";
        string _testTitle = "";
        string _testContent = "";
        string _actualEffect = "";
        string _result = "PENDING";
        string _notes = "";
        Vector2 _scroll;

        [MenuItem("Tools/Playtest/Manual Log Recorder", priority = 50)]
        public static void Open()
        {
            var win = GetWindow<ManualPlaytestLogRecorderWindow>("Playtest Logs");
            win.minSize = new Vector2(620, 520);
            win.Show();
        }

        void OnEnable()
        {
            Application.logMessageReceived -= OnLogMessage;
            Application.logMessageReceived += OnLogMessage;
        }

        void OnDisable()
        {
            Application.logMessageReceived -= OnLogMessage;
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("手动 Playtest 日志记录器", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "推荐流程：到达测试点 -> 清理 Console 并开始记录 -> 执行测试 -> 停止记录 -> 填写实际效果 -> 点击 + 加入本测试点 -> 下一个测试点。最后点击保存并输出。",
                MessageType.Info);

            DrawSessionControls();
            EditorGUILayout.Space(8);
            DrawCurrentSectionEditor();
            EditorGUILayout.Space(8);
            DrawSectionsPreview();
        }

        void DrawSessionControls()
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("记录控制", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("状态", s_recording ? $"记录中：{s_currentLogs.Count} 条" : $"未记录：当前缓存 {s_currentLogs.Count} 条");

                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(s_recording))
                    {
                        if (GUILayout.Button("清理 Console 并开始记录", GUILayout.Height(30)))
                            StartRecording(clearConsole: true);
                    }

                    using (new EditorGUI.DisabledScope(s_recording))
                    {
                        if (GUILayout.Button("开始记录（不清理）", GUILayout.Height(30)))
                            StartRecording(clearConsole: false);
                    }

                    using (new EditorGUI.DisabledScope(!s_recording))
                    {
                        if (GUILayout.Button("停止记录", GUILayout.Height(30)))
                            StopRecording();
                    }
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(s_recording || s_sections.Count == 0))
                    {
                        if (GUILayout.Button("保存并输出", GUILayout.Height(28)))
                            SaveSession();
                    }

                    using (new EditorGUI.DisabledScope(s_recording && s_currentLogs.Count > 0))
                    {
                        if (GUILayout.Button("清空本工具记录", GUILayout.Height(28)))
                        {
                            if (EditorUtility.DisplayDialog("清空记录", "会清空本工具中尚未导出的所有测试点记录，确认继续？", "清空", "取消"))
                                ClearSession();
                        }
                    }
                }

                if (!string.IsNullOrEmpty(s_lastOutputPath))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.SelectableLabel(s_lastOutputPath, GUILayout.Height(18));
                        if (GUILayout.Button("定位", GUILayout.Width(64)))
                            EditorUtility.RevealInFinder(s_lastOutputPath);
                    }
                }
            }
        }

        void DrawCurrentSectionEditor()
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("当前测试点记录", EditorStyles.boldLabel);
                _testId = EditorGUILayout.TextField("编号", _testId);
                _testTitle = EditorGUILayout.TextField("测试点", _testTitle);
                _result = EditorGUILayout.TextField("结论", _result);

                EditorGUILayout.LabelField("本次测试内容");
                _testContent = EditorGUILayout.TextArea(_testContent, GUILayout.MinHeight(48));

                EditorGUILayout.LabelField("实际效果");
                _actualEffect = EditorGUILayout.TextArea(_actualEffect, GUILayout.MinHeight(58));

                EditorGUILayout.LabelField("备注 / 截图路径");
                _notes = EditorGUILayout.TextArea(_notes, GUILayout.MinHeight(36));

                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(s_recording || s_currentLogs.Count == 0))
                    {
                        if (GUILayout.Button("+ 加入本测试点并准备下一项", GUILayout.Height(30)))
                            AddCurrentSection();
                    }

                    if (GUILayout.Button("只清空填写内容", GUILayout.Height(30), GUILayout.Width(140)))
                        ClearFields();
                }
            }
        }

        void DrawSectionsPreview()
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField($"已加入测试点：{s_sections.Count}", EditorStyles.boldLabel);
                using (var scope = new EditorGUILayout.ScrollViewScope(_scroll, GUILayout.MinHeight(150)))
                {
                    _scroll = scope.scrollPosition;

                    for (int i = 0; i < s_sections.Count; i++)
                    {
                        var section = s_sections[i];
                        using (new EditorGUILayout.VerticalScope("box"))
                        {
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                EditorGUILayout.LabelField($"{i + 1}. {section.TestId} {section.Title}", EditorStyles.boldLabel);
                                GUILayout.FlexibleSpace();
                                if (GUILayout.Button("移除", GUILayout.Width(56)))
                                {
                                    s_sections.RemoveAt(i);
                                    GUIUtility.ExitGUI();
                                }
                            }
                            EditorGUILayout.LabelField($"结论：{section.Result}  日志：{section.Logs.Count} 条  时间：{section.StartTime:HH:mm:ss} - {section.EndTime:HH:mm:ss}");
                            if (!string.IsNullOrEmpty(section.ActualEffect))
                                EditorGUILayout.HelpBox(section.ActualEffect, MessageType.None);
                        }
                    }
                }
            }
        }

        static void StartRecording(bool clearConsole)
        {
            if (clearConsole)
                ClearUnityConsole();

            s_currentLogs.Clear();
            s_recordStart = DateTime.Now;
            s_recording = true;
            Debug.Log($"[ManualPlaytestLogRecorder] StartRecording ClearConsole={clearConsole}");
        }

        static void StopRecording()
        {
            Debug.Log("[ManualPlaytestLogRecorder] StopRecording");
            s_recording = false;
        }

        void AddCurrentSection()
        {
            var section = new TestSection
            {
                TestId = string.IsNullOrWhiteSpace(_testId) ? $"TC-{s_sections.Count + 1:00}" : _testId.Trim(),
                Title = string.IsNullOrWhiteSpace(_testTitle) ? "未命名测试点" : _testTitle.Trim(),
                TestContent = _testContent.Trim(),
                ActualEffect = _actualEffect.Trim(),
                Result = string.IsNullOrWhiteSpace(_result) ? "PENDING" : _result.Trim(),
                Notes = _notes.Trim(),
                StartTime = s_recordStart,
                EndTime = DateTime.Now,
                Logs = new List<LogLine>(s_currentLogs)
            };

            s_sections.Add(section);
            s_currentLogs.Clear();
            ClearFields();
        }

        void ClearFields()
        {
            _testId = "";
            _testTitle = "";
            _testContent = "";
            _actualEffect = "";
            _result = "PENDING";
            _notes = "";
        }

        static void ClearSession()
        {
            s_sections.Clear();
            s_currentLogs.Clear();
            s_recording = false;
            s_lastOutputPath = "";
        }

        static void SaveSession()
        {
            Directory.CreateDirectory(OutputDir);

            var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            var mdPath = Path.Combine(OutputDir, $"manual-playtest-logs-{stamp}.md").Replace('\\', '/');
            var rawPath = Path.Combine(OutputDir, $"manual-playtest-logs-{stamp}.log").Replace('\\', '/');

            File.WriteAllText(mdPath, BuildMarkdown(stamp), Encoding.UTF8);
            File.WriteAllText(rawPath, BuildRawLog(), Encoding.UTF8);
            AssetDatabase.Refresh();

            s_lastOutputPath = Path.GetFullPath(mdPath);
            Debug.Log($"[ManualPlaytestLogRecorder] Saved Markdown={mdPath} RawLog={rawPath}");
        }

        static string BuildMarkdown(string stamp)
        {
            var sb = new StringBuilder();
            sb.AppendLine("---");
            sb.AppendLine($"test_time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("scenario: manual-playtest-log-session");
            sb.AppendLine($"session_id: {stamp}");
            sb.AppendLine($"sections: {s_sections.Count}");
            sb.AppendLine("---");
            sb.AppendLine();
            sb.AppendLine("# 手动 Playtest 日志记录");
            sb.AppendLine();

            for (int i = 0; i < s_sections.Count; i++)
            {
                var section = s_sections[i];
                sb.AppendLine($"## {i + 1}. {EscapeMd(section.TestId)} {EscapeMd(section.Title)}");
                sb.AppendLine();
                sb.AppendLine($"- **结论**：{EscapeMd(section.Result)}");
                sb.AppendLine($"- **记录时间**：{section.StartTime:yyyy-MM-dd HH:mm:ss} - {section.EndTime:HH:mm:ss}");
                sb.AppendLine($"- **日志数量**：{section.Logs.Count}");
                sb.AppendLine();
                sb.AppendLine("### 本次测试内容");
                sb.AppendLine();
                sb.AppendLine(string.IsNullOrEmpty(section.TestContent) ? "（未填写）" : section.TestContent);
                sb.AppendLine();
                sb.AppendLine("### 实际效果");
                sb.AppendLine();
                sb.AppendLine(string.IsNullOrEmpty(section.ActualEffect) ? "（未填写）" : section.ActualEffect);
                sb.AppendLine();
                sb.AppendLine("### 备注 / 截图路径");
                sb.AppendLine();
                sb.AppendLine(string.IsNullOrEmpty(section.Notes) ? "（无）" : section.Notes);
                sb.AppendLine();
                sb.AppendLine("### Console 日志");
                sb.AppendLine();
                sb.AppendLine("```text");
                foreach (var log in section.Logs)
                    sb.AppendLine(log.ToSingleLine());
                sb.AppendLine("```");
                sb.AppendLine();
            }

            return sb.ToString();
        }

        static string BuildRawLog()
        {
            var sb = new StringBuilder();
            foreach (var section in s_sections)
            {
                sb.AppendLine($"===== {section.TestId} {section.Title} [{section.Result}] =====");
                foreach (var log in section.Logs)
                {
                    sb.AppendLine(log.ToSingleLine());
                    if (!string.IsNullOrEmpty(log.StackTrace))
                    {
                        sb.AppendLine(log.StackTrace);
                    }
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }

        static string EscapeMd(string value)
        {
            return string.IsNullOrEmpty(value) ? "" : value.Replace("\r", "").Replace("\n", " ");
        }

        static void OnLogMessage(string condition, string stackTrace, LogType type)
        {
            if (!s_recording) return;

            s_currentLogs.Add(new LogLine
            {
                Time = DateTime.Now,
                Frame = Time.frameCount,
                Type = type,
                Message = condition ?? "",
                StackTrace = stackTrace ?? ""
            });
        }

        static void ClearUnityConsole()
        {
            var assembly = Assembly.GetAssembly(typeof(SceneView));
            var logEntries = assembly?.GetType("UnityEditor.LogEntries");
            var clearMethod = logEntries?.GetMethod("Clear", BindingFlags.Static | BindingFlags.Public);
            clearMethod?.Invoke(null, null);
        }

        [Serializable]
        sealed class TestSection
        {
            public string TestId;
            public string Title;
            public string TestContent;
            public string ActualEffect;
            public string Result;
            public string Notes;
            public DateTime StartTime;
            public DateTime EndTime;
            public List<LogLine> Logs;
        }

        [Serializable]
        sealed class LogLine
        {
            public DateTime Time;
            public int Frame;
            public LogType Type;
            public string Message;
            public string StackTrace;

            public string ToSingleLine()
            {
                var msg = (Message ?? "").Replace("\r", "\\r").Replace("\n", "\\n");
                return $"[{Time:HH:mm:ss.fff}] [frame:{Frame}] [{Type}] {msg}";
            }
        }
    }
}
#endif
