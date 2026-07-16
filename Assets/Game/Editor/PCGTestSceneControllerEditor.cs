using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PCGTestSceneController))]
public sealed class PCGTestSceneControllerEditor : Editor
{
    private SerializedProperty mapTheme;
    private SerializedProperty seed;
    private SerializedProperty generateOnStart;
    private SerializedProperty spawnBusinessPlayerAndFollowCamera;
    private SerializedProperty mapWidth;
    private SerializedProperty mapHeight;
    private SerializedProperty maxVisualSprites;
    private SerializedProperty objectBudget;

    private void OnEnable()
    {
        mapTheme = serializedObject.FindProperty("mapTheme");
        seed = serializedObject.FindProperty("seed");
        generateOnStart = serializedObject.FindProperty("generateOnStart");
        spawnBusinessPlayerAndFollowCamera = serializedObject.FindProperty("spawnBusinessPlayerAndFollowCamera");
        mapWidth = serializedObject.FindProperty("mapWidth");
        mapHeight = serializedObject.FindProperty("mapHeight");
        maxVisualSprites = serializedObject.FindProperty("maxVisualSprites");
        objectBudget = serializedObject.FindProperty("objectBudget");
    }

    public override void OnInspectorGUI()
    {
        var controller = (PCGTestSceneController)target;
        serializedObject.Update();

        EditorGUILayout.HelpBox(
            "本场景始终调用正式 TotemMapService。这里的参数只在 PCGTest 当前一次生成期间临时覆盖，不会写入正式业务默认配置。",
            MessageType.Info);

        DrawMapSection();
        DrawParameterSection();
        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space();
        DrawGenerationButtons(controller);
        DrawRuntimeSummary();
    }

    private void DrawMapSection()
    {
        EditorGUILayout.LabelField("地图与种子", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(mapTheme, new GUIContent("地图主题", "选择正式 MapTemplate 的主题。"));
        EditorGUILayout.PropertyField(seed, new GUIContent("固定种子", "相同种子和参数会产生相同地图。"));
        EditorGUILayout.PropertyField(generateOnStart, new GUIContent("进入播放时自动生成"));
        EditorGUILayout.PropertyField(spawnBusinessPlayerAndFollowCamera,
            new GUIContent("生成业务玩家并跟随", "生成后进入正式 CombatHud 流程：WASD/方向键控制玩家，业务相机自动跟随。"));

        using (new EditorGUILayout.HorizontalScope())
        {
            DrawThemeButton("AI 遗迹", PCGTestMapTheme.AiRuins);
            DrawThemeButton("异形蜂巢", PCGTestMapTheme.AlienHive);
            DrawThemeButton("病毒沼泽", PCGTestMapTheme.VirusSwamp);
        }
    }

    private void DrawParameterSection()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("PCG 参数（仅测试场景）", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(mapWidth, new GUIContent("地图宽度（格）", "16～128。数值越大，地图细节和生成耗时越高。"));
        EditorGUILayout.PropertyField(mapHeight, new GUIContent("地图高度（格）", "16～128。数值越大，地图细节和生成耗时越高。"));
        EditorGUILayout.PropertyField(maxVisualSprites, new GUIContent("最多渲染装饰数", "0 表示不限制；用于控制装饰 Sprite 的渲染数量。"));

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("视觉内容数量上限", EditorStyles.miniBoldLabel);
        EditorGUILayout.PropertyField(objectBudget, new GUIContent("地物数量上限", "0 表示跟随正式业务的默认数量。"));
    }

    private void DrawThemeButton(string label, PCGTestMapTheme theme)
    {
        if (!GUILayout.Button(label))
        {
            return;
        }

        serializedObject.Update();
        mapTheme.enumValueIndex = (int)theme - 1;
        serializedObject.ApplyModifiedProperties();

        if (EditorApplication.isPlaying)
        {
            ((PCGTestSceneController)target).GenerateTheme(theme);
        }
    }

    private void DrawGenerationButtons(PCGTestSceneController controller)
    {
        EditorGUILayout.LabelField("生成操作", EditorStyles.boldLabel);
        if (!EditorApplication.isPlaying)
        {
            EditorGUILayout.HelpBox("请进入播放模式后生成。可以先在此处调整主题、种子与参数。", MessageType.None);
            return;
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("使用当前参数生成", GUILayout.Height(28f)))
            {
                controller.GenerateCurrentTheme();
            }

            if (GUILayout.Button("随机种子生成", GUILayout.Height(28f)))
            {
                controller.GenerateRandomSeed();
            }
        }
    }

    private static void DrawRuntimeSummary()
    {
        if (!EditorApplication.isPlaying)
        {
            return;
        }

        var map = TotemGameRuntime.Instance?.GetService<TotemMapService>()?.CurrentMap;
        if (map == null)
        {
            return;
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            $"当前地图：{map.ThemeName} | 种子：{map.Seed} | 格数：{map.PcgWidth}×{map.PcgHeight} | 可视元素：{map.PcgVisualCount} | 内容哈希：{map.PcgContentHash}",
            MessageType.None);
    }
}
