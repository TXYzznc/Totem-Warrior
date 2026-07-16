using UnityEngine;

/// <summary>
/// 保留在 PCGTest 场景中的正式 PCG 流程入口。
/// 该控制器不实现任何地图生成或渲染逻辑，只调用 TotemMapService 的正式生成接口。
/// </summary>
[DisallowMultipleComponent]
public sealed class PCGTestSceneController : MonoBehaviour
{
    [Header("正式地图模板")]
    [SerializeField, Tooltip("正式 MapTemplate 的 ID；当前 1=AI 遗迹、2=异形蜂巢、3=病毒沼泽。")]
    private int themeId = 1;

    [SerializeField, Min(0)]
    private int seed = 1;

    [SerializeField]
    private bool generateOnStart = true;

    private TotemGameRuntime runtime;

    public int ThemeId => themeId;

    public int Seed => seed;

    private void Start()
    {
        if (generateOnStart)
        {
            GenerateCurrentTheme();
        }
    }

    [ContextMenu("生成当前主题")]
    public void GenerateCurrentTheme()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[PCGTest] 请先进入播放模式，再生成正式 PCG 地图。", this);
            return;
        }

        EnsureRuntime();
        var mapService = runtime.GetService<TotemMapService>();
        if (mapService == null)
        {
            Debug.LogError("[PCGTest] TotemMapService 未初始化，无法生成正式 PCG 地图。", this);
            return;
        }

        var map = mapService.GenerateMap(seed, Mathf.Max(1, themeId), createObjects: true);
        if (map == null)
        {
            Debug.LogError("[PCGTest] 正式 PCG 地图生成失败。", this);
            return;
        }

        Debug.Log($"[PCGTest] 已使用正式 TotemMapService 生成 {map.ThemeName}，seed={seed}，hash={map.PcgContentHash}。", this);
    }

    [ContextMenu("生成 AI 遗迹")]
    public void GenerateAiRuins()
    {
        GenerateTheme(1);
    }

    [ContextMenu("生成异形蜂巢")]
    public void GenerateAlienHive()
    {
        GenerateTheme(2);
    }

    [ContextMenu("生成病毒沼泽")]
    public void GenerateVirusSwamp()
    {
        GenerateTheme(3);
    }

    private void OnDestroy()
    {
        if (!Application.isPlaying || runtime == null || runtime != TotemGameRuntime.Instance)
        {
            return;
        }

        runtime.ShutdownRuntime();
        Destroy(runtime.gameObject);
    }

    private void GenerateTheme(int requestedThemeId)
    {
        themeId = requestedThemeId;
        GenerateCurrentTheme();
    }

    private void EnsureRuntime()
    {
        if (runtime == null)
        {
            runtime = TotemGameRuntime.EnsureCreated();
        }

        runtime.MarkProcedureEntered(nameof(PCGTestSceneController));
        runtime.StartRuntime();
    }
}
