using UnityEditor;
using UnityEngine;

/// <summary>
/// Entry point for the large-canvas tattoo authoring window. It lives in the legacy toolbox
/// assembly and invokes the dedicated marker through its stable menu path, avoiding a reverse
/// assembly dependency on gameplay-editor tools.
/// </summary>
[ToolHubItem(
    "美术工具/纹身区域标记器",
    "手工标记角色纹身区域：矩形/钢笔区域、皮肤裁剪预览与批量导出",
    30
)]
public sealed class TattooRegionMarkerPanel : IToolHubPanel
{
    private const string MarkerMenuPath = "Game/Totem/Tattoo/Region Marker";

    public void OnEnable() { }

    public void OnDisable() { }

    public void OnDestroy() { }

    public string GetHelpText()
    {
        return "打开大画布纹身区域标记器。支持矩形与钢笔多边形标记、裁剪预览，以及一次导出所有已标记帧。";
    }

    public void OnGUI()
    {
        EditorGUILayout.Space(12f);
        EditorGUILayout.LabelField("纹身区域标记器", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "标记器使用独立的大画布窗口，方便逐帧缩放、平移和编辑区域。\n\n" +
            "• 矩形工具：拖出规整区域，白色圆点移动整个区域\n" +
            "• 钢笔工具：逐点绘制任意多边形，闭合后可拖动顶点\n" +
            "• 彩色方块：调整端点、矩形角或钢笔顶点\n" +
            "• 彩色圆点：调整肢体宽度或矩形单边\n" +
            "• 人物左/右由你明确选择，工具不会按屏幕左右猜测。",
            MessageType.Info
        );

        EditorGUILayout.Space(8f);
        if (GUILayout.Button("打开纹身区域标记器", GUILayout.Height(38f)))
        {
            ExecuteMenu(MarkerMenuPath);
        }

        EditorGUILayout.HelpBox("生成操作位于标记器窗口顶部：可生成当前方向，或一键生成全部已手工标记帧的 TattooMap。", MessageType.None);
    }

    private static void ExecuteMenu(string menuPath)
    {
        if (!EditorApplication.ExecuteMenuItem(menuPath))
        {
            Debug.LogError("无法执行工具菜单：" + menuPath);
        }
    }
}
