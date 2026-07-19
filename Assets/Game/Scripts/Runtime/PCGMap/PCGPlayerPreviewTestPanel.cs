using UnityEngine;

/// <summary>
/// PCG 地图游览的临时玩家调试面板。
/// 仅由 PCGTestSceneController 在编辑器预览时创建，不会进入正式 UI、存档或数据表。
/// </summary>
[DisallowMultipleComponent]
public sealed class PCGPlayerPreviewTestPanel : MonoBehaviour
{
    private const float WindowWidth = 430f;
    private const float WindowHeight = 640f;
    private const float MinHealth = 1f;
    private const float MaxHealth = 500f;
    private const float MinScale = 0.5f;
    private const float MaxScale = 3f;

    private static readonly string[] PartNames =
    {
        "头部", "躯干", "左臂", "右臂", "左腿", "右腿",
    };

    private static readonly string[] ColorNames =
    {
        "红", "黄", "绿", "蓝", "紫", "金", "白",
    };

    private static readonly string[] PatternNames =
    {
        "线", "环", "旋", "锯", "闪", "星", "流", "兽",
    };

    private static readonly Color[] InkColors =
    {
        new Color32(0xC9, 0x3D, 0x38, 0xFF),
        new Color32(0xD4, 0xA6, 0x2B, 0xFF),
        new Color32(0x45, 0x9A, 0x62, 0xFF),
        new Color32(0x3D, 0x79, 0xB5, 0xFF),
        new Color32(0x7C, 0x4C, 0x98, 0xFF),
        new Color32(0xC9, 0x91, 0x35, 0xFF),
        new Color32(0xE7, 0xE0, 0xD0, 0xFF),
    };

    private readonly int[] colorIds = new int[TotemTattooService.PartCount];
    private readonly int[] patternIds = new int[TotemTattooService.PartCount];
    private Rect windowRect = new Rect(16f, 16f, WindowWidth, WindowHeight);
    private Vector2 scrollPosition;
    private TotemActorModel boundPlayer;
    private GameObject scaleTarget;
    private Vector3 baseScale = Vector3.one;
    private float maxHealth = 100f;
    private float currentHealth = 100f;
    private float moveSpeed = 5f;
    private float sizeMultiplier = 1f;
    private bool visible = true;
    private GUIStyle sectionStyle;

    private void Awake()
    {
        for (int index = 0; index < TotemTattooService.PartCount; index++)
        {
            colorIds[index] = index % TotemTattooService.ColorCount + 1;
            patternIds[index] = index % TotemTattooService.PatternCount + 1;
        }
    }

    private void OnGUI()
    {
        if (!Application.isEditor)
        {
            return;
        }

        if (!visible)
        {
            if (GUI.Button(new Rect(16f, 16f, 116f, 30f), "PCG 玩家测试"))
            {
                visible = true;
            }

            return;
        }

        windowRect = GUI.Window(GetInstanceID(), windowRect, DrawWindow, "PCG 玩家测试");
    }

    private void DrawWindow(int windowId)
    {
        var runtime = TotemGameRuntime.Instance;
        TotemActorService actorService = runtime?.GetService<TotemActorService>();
        TotemCombatService combatService = runtime?.GetService<TotemCombatService>();
        TotemTattooService tattooService = runtime?.GetService<TotemTattooService>();
        TotemActorModel player = actorService?.Player;
        if (player == null || combatService == null || !combatService.IsExplorationPreview)
        {
            GUILayout.Label("等待 PCG 探索预览中的玩家生成…");
            DrawFooter();
            return;
        }

        BindPlayer(player, combatService, tattooService);
        scrollPosition = GUILayout.BeginScrollView(scrollPosition);
        GUILayout.Label("仅影响本次 PCG 游览预览，不写入正式玩法或存档。", GUI.skin.box);
        DrawSection("基础属性");
        DrawHealthControls(player);
        DrawMoveSpeedControl(combatService);
        DrawSizeControl(player);
        DrawSection("纹身预览");
        GUILayout.Label("每个部位可独立选择颜色与纹样；修改后立即走正式纹身服务和 Shader 渲染。", GUI.skin.label);

        for (int index = 0; index < TotemTattooService.PartCount; index++)
        {
            DrawTattooPart(index, tattooService);
        }

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("应用彩虹预设"))
        {
            ApplyRainbowPreset(tattooService);
        }

        if (GUILayout.Button("清除纹身"))
        {
            tattooService?.Clear();
        }

        GUILayout.EndHorizontal();
        GUILayout.EndScrollView();
        DrawFooter();
        GUI.DragWindow(new Rect(0f, 0f, WindowWidth, 22f));
    }

    private void DrawHealthControls(TotemActorModel player)
    {
        float nextMaxHealth = DrawSlider("最大生命", maxHealth, MinHealth, MaxHealth, "F0");
        float nextCurrentHealth = DrawSlider("当前生命", currentHealth, MinHealth, nextMaxHealth, "F0");
        if (!Mathf.Approximately(nextMaxHealth, maxHealth) || !Mathf.Approximately(nextCurrentHealth, currentHealth))
        {
            maxHealth = nextMaxHealth;
            currentHealth = Mathf.Clamp(nextCurrentHealth, MinHealth, maxHealth);
            player.ResetHealth(maxHealth);
            player.ApplyDamage(maxHealth - currentHealth);
        }
    }

    private void DrawMoveSpeedControl(TotemCombatService combatService)
    {
        float nextMoveSpeed = DrawSlider("移速", moveSpeed, 0.5f, 15f, "F1");
        if (!Mathf.Approximately(nextMoveSpeed, moveSpeed))
        {
            moveSpeed = nextMoveSpeed;
            combatService.SetExplorationPreviewPlayerMoveSpeed(moveSpeed);
        }
    }

    private void DrawSizeControl(TotemActorModel player)
    {
        float nextSize = DrawSlider("角色大小", sizeMultiplier, MinScale, MaxScale, "F2");
        if (!Mathf.Approximately(nextSize, sizeMultiplier))
        {
            sizeMultiplier = nextSize;
            ApplySize(player);
        }
    }

    private void DrawTattooPart(int partIndex, TotemTattooService tattooService)
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label(PartNames[partIndex]);
        GUILayout.BeginHorizontal();
        GUILayout.Label("颜色", GUILayout.Width(38f));
        for (int colorIndex = 0; colorIndex < TotemTattooService.ColorCount; colorIndex++)
        {
            Color previousColor = GUI.backgroundColor;
            GUI.backgroundColor = InkColors[colorIndex];
            bool selected = colorIds[partIndex] == colorIndex + 1;
            if (GUILayout.Toggle(selected, ColorNames[colorIndex], GUI.skin.button, GUILayout.Width(42f)) && !selected)
            {
                colorIds[partIndex] = colorIndex + 1;
                ApplyTattooPart(partIndex, tattooService);
            }

            GUI.backgroundColor = previousColor;
        }

        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        GUILayout.Label("纹样", GUILayout.Width(38f));
        int selectedPattern = GUILayout.SelectionGrid(patternIds[partIndex] - 1, PatternNames, 8, GUILayout.Height(24f));
        if (selectedPattern != patternIds[partIndex] - 1)
        {
            patternIds[partIndex] = selectedPattern + 1;
            ApplyTattooPart(partIndex, tattooService);
        }

        GUILayout.EndHorizontal();
        GUILayout.EndVertical();
    }

    private float DrawSlider(string label, float value, float min, float max, string format)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, GUILayout.Width(66f));
        float nextValue = GUILayout.HorizontalSlider(value, min, max, GUILayout.Width(220f));
        GUILayout.Label(nextValue.ToString(format), GUILayout.Width(52f));
        GUILayout.EndHorizontal();
        return nextValue;
    }

    private void BindPlayer(TotemActorModel player, TotemCombatService combatService, TotemTattooService tattooService)
    {
        if (boundPlayer == player)
        {
            return;
        }

        boundPlayer = player;
        maxHealth = player.MaxHealth;
        currentHealth = player.Health;
        moveSpeed = combatService.CurrentPlayerMoveSpeed;
        sizeMultiplier = 1f;
        scaleTarget = player.GameObject;
        baseScale = scaleTarget == null ? Vector3.one : scaleTarget.transform.localScale;
        ApplyRainbowPreset(tattooService);
    }

    private void ApplyTattooPart(int partIndex, TotemTattooService tattooService)
    {
        tattooService?.Equip(partIndex + 1, colorIds[partIndex], patternIds[partIndex]);
    }

    private void ApplyRainbowPreset(TotemTattooService tattooService)
    {
        if (tattooService == null)
        {
            return;
        }

        tattooService.Clear();
        for (int index = 0; index < TotemTattooService.PartCount; index++)
        {
            colorIds[index] = index % TotemTattooService.ColorCount + 1;
            patternIds[index] = index % TotemTattooService.PatternCount + 1;
            ApplyTattooPart(index, tattooService);
        }
    }

    private void ApplySize(TotemActorModel player)
    {
        if (player?.GameObject == null)
        {
            return;
        }

        if (scaleTarget != player.GameObject)
        {
            scaleTarget = player.GameObject;
            baseScale = scaleTarget.transform.localScale;
        }

        scaleTarget.transform.localScale = baseScale * sizeMultiplier;
    }

    private void DrawSection(string title)
    {
        sectionStyle ??= new GUIStyle(GUI.skin.label)
        {
            fontStyle = FontStyle.Bold,
            fontSize = 13,
        };
        GUILayout.Space(6f);
        GUILayout.Label(title, sectionStyle);
    }

    private void DrawFooter()
    {
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("收起面板"))
        {
            visible = false;
        }
    }
}
