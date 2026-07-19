using UnityEngine;

public enum PCGTestMapTheme
{
    AiRuins = 1,
    AlienHive = 2,
    VirusSwamp = 3,
}

/// <summary>PCGTest 场景的正式生成入口；只暴露仍参与 World Plan 的调试参数。</summary>
[DisallowMultipleComponent]
public sealed class PCGTestSceneController : MonoBehaviour
{
    [SerializeField] private PCGTestMapTheme mapTheme = PCGTestMapTheme.AiRuins;
    [SerializeField, Min(0)] private int seed = 1;
    [SerializeField] private bool generateOnStart = true;
    [SerializeField] private bool spawnBusinessPlayerAndFollowCamera = true;
    [SerializeField] private bool showPlayerPreviewTestPanel = true;
    [SerializeField, Range(16, 128)] private int mapWidth = TotemMapService.PcgMapWidth;
    [SerializeField, Range(16, 128)] private int mapHeight = TotemMapService.PcgMapHeight;
    [SerializeField, Min(0)] private int objectBudget;
    [SerializeField, Min(0)] private int maxVisualSprites;

    private TotemGameRuntime runtime;

    public PCGTestMapTheme MapTheme => mapTheme;
    public int Seed => seed;

    private void Start()
    {
        EnsurePlayerPreviewTestPanel();
        if (generateOnStart) GenerateCurrentTheme();
    }

    [ContextMenu("Generate Current Theme")]
    public void GenerateCurrentTheme()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[PCGTest] Enter Play Mode before generating a PCG map.", this);
            return;
        }

        EnsureRuntime();
        var mapService = runtime.GetService<TotemMapService>();
        var flowService = runtime.GetService<TotemGameFlowService>();
        var actorService = runtime.GetService<TotemActorService>();
        var combatService = runtime.GetService<TotemCombatService>();
        var readinessService = runtime.GetService<TotemParticipantReadinessService>();
        if (mapService == null)
        {
            Debug.LogError("[PCGTest] TotemMapService is not available.", this);
            return;
        }

        if (spawnBusinessPlayerAndFollowCamera && flowService?.CurrentState == TotemGameFlowState.CombatHud)
        {
            // 先走业务状态退出，确保旧地图上的玩家、相机跟随和战斗服务正确收尾。
            flowService.EnterStartupSelect();
        }

        TotemMapSnapshot map;
        if (spawnBusinessPlayerAndFollowCamera)
        {
            // 地图请求在进入 CombatHud 时由 MapService 消费；随后 Actor/Camera 服务
            // 在同一条业务状态链上使用该地图创建玩家并建立跟随。
            mapService.RequestNextCombatMap(seed, (int)mapTheme, BuildRuntimeSettingsOverride());
            actorService?.RequestNextCombatRoster(TotemCombatRosterMode.PlayerOnlyPreview);
            combatService?.RequestNextCombatRunMode(TotemCombatRunMode.ExplorationPreview);
            flowService?.EnterCombatHud();
            readinessService?.NotifyLocalClientReady(actorService?.Player, "PCGTestLocalPreview");
            map = mapService.CurrentMap;
        }
        else
        {
            using (TotemMapService.UsePcgRuntimeSettingsOverride(BuildRuntimeSettingsOverride()))
            {
                map = mapService.GenerateMap(seed, (int)mapTheme, true);
            }
        }

        if (map == null)
        {
            Debug.LogError("[PCGTest] Map generation failed.", this);
            return;
        }

        Debug.Log($"[PCGTest] Generated {map.ThemeName}; seed={seed}; hash={map.PcgContentHash}; " +
                  $"businessPlayer={spawnBusinessPlayerAndFollowCamera}; previewRoster=PlayerOnly.", this);
    }

    [ContextMenu("Generate Random Seed")]
    public void GenerateRandomSeed()
    {
        seed = Random.Range(1, int.MaxValue);
        GenerateCurrentTheme();
    }

    [ContextMenu("Generate AI Ruins")]
    public void GenerateAiRuins() => GenerateTheme(PCGTestMapTheme.AiRuins);

    [ContextMenu("Generate Alien Hive")]
    public void GenerateAlienHive() => GenerateTheme(PCGTestMapTheme.AlienHive);

    [ContextMenu("Generate Virus Swamp")]
    public void GenerateVirusSwamp() => GenerateTheme(PCGTestMapTheme.VirusSwamp);

    public void SetTheme(PCGTestMapTheme theme) => mapTheme = theme;

    public void GenerateTheme(PCGTestMapTheme theme)
    {
        mapTheme = theme;
        GenerateCurrentTheme();
    }

    private void OnDestroy()
    {
        if (!Application.isPlaying || runtime == null || runtime != TotemGameRuntime.Instance) return;
        runtime.ShutdownRuntime();
        Destroy(runtime.gameObject);
    }

    private void OnValidate()
    {
        var settings = BuildRuntimeSettingsOverride();
        mapWidth = settings.Width;
        mapHeight = settings.Height;
        objectBudget = settings.ObjectBudget;
        maxVisualSprites = settings.MaxVisualSprites;
        seed = Mathf.Max(0, seed);
    }

    private void EnsureRuntime()
    {
        if (runtime == null) runtime = TotemGameRuntime.EnsureCreated();
        runtime.MarkProcedureEntered(nameof(PCGTestSceneController));
        runtime.StartRuntime();
    }

    private void EnsurePlayerPreviewTestPanel()
    {
        if (!showPlayerPreviewTestPanel || !Application.isEditor)
        {
            return;
        }

        if (GetComponent<PCGPlayerPreviewTestPanel>() == null)
        {
            gameObject.AddComponent<PCGPlayerPreviewTestPanel>();
        }
    }

    private TotemPcgRuntimeSettingsOverride BuildRuntimeSettingsOverride()
    {
        return new TotemPcgRuntimeSettingsOverride
        {
            Width = mapWidth,
            Height = mapHeight,
            ObjectBudget = objectBudget <= 0 ? TotemMapService.PcgObjectBudget : objectBudget,
            MaxVisualSprites = maxVisualSprites,
        }.Normalized();
    }
}
