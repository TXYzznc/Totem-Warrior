using UnityEditor;
using UnityEngine;

/// <summary>
/// Entry point for the large-canvas tattoo authoring window. It lives in the legacy toolbox
/// assembly and invokes the dedicated marker through its stable menu path, avoiding a reverse
/// assembly dependency on gameplay-editor tools.
/// </summary>
[ToolHubItem(
    "美术工具/纹身区域标记器",
    "手工标记角色纹身区域：规整中心线/矩形、皮肤裁剪预览、移动与边框缩放",
    30
)]
public sealed class TattooRegionMarkerPanel : IToolHubPanel
{
    private const string MarkerMenuPath = "Game/Totem/Tattoo/Region Marker";
    private const string ReviewMenuPath = "Game/Totem/Tattoo/Open Right Direction TattooMap Review";

    public void OnEnable() { }

    public void OnDisable() { }

    public void OnDestroy() { }

    public string GetHelpText()
    {
        return "打开大画布纹身区域标记器。角色左右由美术手工选择，工具只负责规整区域、裁剪预览和导出。";
    }

    public void OnGUI()
    {
        EditorGUILayout.Space(12f);
        EditorGUILayout.LabelField("纹身区域标记器", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "标记器使用独立的大画布窗口，方便逐帧缩放、平移和拖动边框。\n\n" +
            "• 白色圆点：移动整个区域\n" +
            "• 彩色方块：调整端点或矩形角\n" +
            "• 彩色圆点：调整肢体宽度或矩形单边\n" +
            "• 人物左/右由你明确选择，工具不会按屏幕左右猜测。",
            MessageType.Info
        );

        EditorGUILayout.Space(8f);
        if (GUILayout.Button("打开纹身区域标记器", GUILayout.Height(38f)))
        {
            ExecuteMenu(MarkerMenuPath);
        }

        EditorGUILayout.HelpBox("生成操作依赖标记器当前选中的方向，请在大画布窗口中使用“生成当前方向 TattooMap”。", MessageType.None);

        if (GUILayout.Button("打开右向审查场景", GUILayout.Height(28f)))
        {
            ExecuteMenu(ReviewMenuPath);
        }
    }

    private static void ExecuteMenu(string menuPath)
    {
        if (!EditorApplication.ExecuteMenuItem(menuPath))
        {
            Debug.LogError("无法执行工具菜单：" + menuPath);
        }
    }
}
