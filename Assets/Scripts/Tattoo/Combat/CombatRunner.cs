using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Tattoo
{
    /// <summary>
    /// 战斗场景统一入口：Awake 程序化创建 Camera/光照/玩家/敌人，OnGUI 提供装备配置 + 战斗日志。
    /// 把现成 21 原子展开的 336 组合在真实战斗里逐一验证。
    /// </summary>
    public class CombatRunner : MonoBehaviour
    {
        [Header("场景")]
        public int  EnemyCount   = 4;
        public bool ShowGUI      = true;
        public bool SpawnSceneEntities = true;

        [Header("UI 选择")]
        public int PartIndex    = 0;
        public int ColorIndex   = 0;
        public int PatternIndex = 0;

        Player          player;
        readonly List<Enemy> enemies = new();
        Camera          mainCam;

        BodyPart[]   parts;
        ColorSO[]    colors;
        PatternSO[]  patterns;

        Font cnFont;
        GUIStyle headerStyle, labelStyle, btnStyle, logStyle;
        readonly List<string> log = new();
        const int MaxLogLines = 20;

        // ---------- 中文映射 ----------
        static readonly Dictionary<string, string> PartCN = new()
        {
            { "Head", "脑袋" }, { "Torso", "躯干" }, { "LeftArm", "左臂" },
            { "RightArm", "右臂" }, { "LeftLeg", "左腿" }, { "RightLeg", "右腿" },
        };
        static readonly Dictionary<string, string> ColorCN = new()
        {
            { "Red", "红" }, { "Yellow", "黄" }, { "Green", "绿" },
            { "Blue", "蓝" }, { "Purple", "紫" }, { "Gold", "金" }, { "White", "白" },
        };
        static readonly Dictionary<string, string> PatternCN = new()
        {
            { "Line", "直线" }, { "Ring", "圆环" }, { "Spiral", "螺旋" },
            { "Zigzag", "锯齿" }, { "Bolt", "闪电" }, { "Star", "星形" },
            { "Stream", "流线" }, { "Beast", "兽形" },
        };
        static readonly Dictionary<GameEvent, string> EventCN = new()
        {
            { GameEvent.OnAttack, "普攻" }, { GameEvent.OnCrit, "暴击" },
            { GameEvent.OnDamaged, "受伤" }, { GameEvent.OnSkillCast, "技能" },
            { GameEvent.OnDodgePressed, "闪避" }, { GameEvent.OnMoveTick, "移动" },
        };

        void Awake()
        {
            BuildAtoms();
            if (SpawnSceneEntities) CreateScene();
        }

        void CreateScene()
        {
            // 1. Camera
            mainCam = Camera.main;
            if (mainCam == null)
            {
                var camGo = new GameObject("MainCamera");
                mainCam = camGo.AddComponent<Camera>();
                camGo.tag = "MainCamera";
            }
            mainCam.transform.position    = new Vector3(0, 18, -10);
            mainCam.transform.eulerAngles = new Vector3(55, 0, 0);
            mainCam.backgroundColor       = new Color(0.18f, 0.18f, 0.22f);
            mainCam.clearFlags            = CameraClearFlags.SolidColor;

            // 2. Ground
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(6, 1, 6);
            SetColor(ground, new Color(0.3f, 0.3f, 0.34f));

            // 3. Light
            var sun = new GameObject("Sun");
            var light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            sun.transform.eulerAngles = new Vector3(50, 30, 0);

            // 4. Player
            var pGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pGo.name = "Player";
            pGo.transform.position = new Vector3(0, 0.4f, 0);
            pGo.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
            SetColor(pGo, new Color(0.3f, 0.7f, 1f));
            player = pGo.AddComponent<Player>();
            player.OnLog += AppendLog;

            // 5. Enemies
            for (int i = 0; i < EnemyCount; i++)
            {
                var eGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                eGo.name = $"敌人{i + 1}";
                float a = i * Mathf.PI * 2f / EnemyCount;
                eGo.transform.position = new Vector3(Mathf.Cos(a) * 6f, 0.4f, Mathf.Sin(a) * 6f);
                eGo.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
                SetColor(eGo, new Color(0.9f, 0.3f, 0.3f));
                var en = eGo.AddComponent<Enemy>();
                en.Owner = player;
                en.T = new Target { Name = $"敌人{i + 1}", Health = en.MaxHP };
                enemies.Add(en);
            }
            player.Enemies = enemies;
        }

        static void SetColor(GameObject go, Color color)
        {
            var rd = go.GetComponent<Renderer>();
            if (rd == null) return;
            var sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null) sh = Shader.Find("Standard");
            var mat = new Material(sh);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color"))     mat.SetColor("_Color", color);
            rd.material = mat;
        }

        void Update()
        {
            // 相机跟随
            if (player != null && mainCam != null)
            {
                var tgt = player.transform.position + new Vector3(0, 18, -10);
                mainCam.transform.position = Vector3.Lerp(mainCam.transform.position, tgt, 5f * Time.deltaTime);
            }
        }

        Vector2 scrollLog;
        Vector2 scrollPlayer;
        Vector2 scrollEffects;

        void OnGUI()
        {
            if (!ShowGUI || player == null) return;
            EnsureStyles();

            DrawLeftPanel();
            DrawRightTopPlayerPanel();
            DrawRightTopEffectsPanel();
        }

        // ---------- 左侧：装备配置 + 战斗日志 ----------
        void DrawLeftPanel()
        {
            float w = Mathf.Min(500, Screen.width - 20);
            GUILayout.BeginArea(new Rect(10, 10, w, Screen.height - 20), GUI.skin.box);
            GUILayout.Label("纹身实战验证 - WASD移动 / 鼠标左键普攻 / E技能 / 空格闪避", headerStyle);

            GUILayout.Label("【装备配置（实时生效）】", headerStyle);
            GUILayout.Label($"部位 - 触发：{EventCN[parts[PartIndex].TriggerEvent]}", labelStyle);
            PartIndex = GUILayout.SelectionGrid(PartIndex, PartLabels(), 6, btnStyle);
            GUILayout.Label("颜色", labelStyle);
            ColorIndex = GUILayout.SelectionGrid(ColorIndex, ColorLabels(), 7, btnStyle);
            GUILayout.Label("图案", labelStyle);
            PatternIndex = GUILayout.SelectionGrid(PatternIndex, PatternLabels(), 8, btnStyle);

            GUILayout.Label($"当前组合：<b>{CurrentLabel()}</b>", labelStyle);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("装入 Build", btnStyle, GUILayout.Height(28))) EquipCurrent();
            if (GUILayout.Button("清空 Build", btnStyle, GUILayout.Height(28))) ClearBuild();
            if (GUILayout.Button("快速 6 槽", btnStyle, GUILayout.Height(28))) LoadFullBuild();
            GUILayout.EndHorizontal();

            GUILayout.Space(4);
            GUILayout.Label($"敌人血量：", labelStyle);
            foreach (var e in enemies)
                if (e != null) GUILayout.Label($"  · {e.T.Name}: HP={e.HP}/{e.MaxHP}  状态[{TattooI18N.StatusZh(string.Join(",", e.T.Statuses))}]", logStyle);

            GUILayout.Space(6);
            GUILayout.Label("【战斗日志】", headerStyle);
            scrollLog = GUILayout.BeginScrollView(scrollLog, GUILayout.ExpandHeight(true));
            foreach (var line in log) GUILayout.Label(line, logStyle);
            GUILayout.EndScrollView();

            GUILayout.EndArea();
        }

        // ---------- 右上 1：玩家属性 + 装备的所有效果 ----------
        void DrawRightTopPlayerPanel()
        {
            float w = 420;
            float x = Screen.width - w - 10;
            float h = Mathf.Min(380, Screen.height * 0.45f);
            GUILayout.BeginArea(new Rect(x, 10, w, h), GUI.skin.box);

            GUILayout.Label("【玩家状态】", headerStyle);
            GUILayout.Label($"HP <b>{player.HP}/{player.MaxHP}</b>  击杀 <b>{player.Kills}</b>", labelStyle);
            GUILayout.Label($"技能CD {player.SkillCD:F1}s  闪避CD {player.DodgeCD:F1}s  无敌帧 {player.DodgeIFrame:F2}s", labelStyle);

            var self = player.Composer.Player;
            if (self != null)
            {
                // 叠层 / Buff
                if (self.Stacks != null && self.Stacks.Count > 0)
                {
                    GUILayout.Label("叠层 buff：", labelStyle);
                    foreach (var kv in self.Stacks)
                        GUILayout.Label($"  · {TattooI18N.StatusZh(kv.Key)} ×{kv.Value}", logStyle);
                }
                if (self.Buffs != null && self.Buffs.Count > 0)
                {
                    GUILayout.Label("自身 buff：", labelStyle);
                    int show = Mathf.Min(self.Buffs.Count, 6);
                    for (int i = self.Buffs.Count - show; i < self.Buffs.Count; i++)
                        GUILayout.Label($"  · {TattooI18N.StatusZh(self.Buffs[i])}", logStyle);
                }
                if (self.PendingTriggers != null && self.PendingTriggers.Count > 0)
                {
                    GUILayout.Label($"待触发延迟：{self.PendingTriggers.Count} 个", labelStyle);
                    foreach (var pt in self.PendingTriggers)
                        GUILayout.Label($"  · 来自 {TattooI18N.PartZh(pt.Source)}（{TattooI18N.EventZh(pt.ConsumeOnEvent)} 时消耗）", logStyle);
                }
            }

            GUILayout.Label($"【装备 Build {player.Composer.Equipped.Count} 槽 / 被动效果】", headerStyle);
            scrollPlayer = GUILayout.BeginScrollView(scrollPlayer, GUILayout.ExpandHeight(true));
            foreach (var s in player.Composer.Equipped)
                GUILayout.Label($"· {SlotLabel(s)}", labelStyle);
            if (self != null && self.Passive != null && self.Passive.EntryLog != null)
                foreach (var entry in self.Passive.EntryLog)
                    GUILayout.Label($"· 被动: {entry}", logStyle);
            GUILayout.EndScrollView();

            GUILayout.EndArea();
        }

        // ---------- 右上 2：实时触发的所有效果 ----------
        void DrawRightTopEffectsPanel()
        {
            float w = 420;
            float x = Screen.width - w - 10;
            float topH = Mathf.Min(380, Screen.height * 0.45f);
            float y = 10 + topH + 10;
            float h = Screen.height - y - 10;
            GUILayout.BeginArea(new Rect(x, y, w, h), GUI.skin.box);

            GUILayout.Label("【实时触发效果】", headerStyle);
            GUILayout.Label($"最近 {player.RecentEffects.Count} 条（{player.MaxRecentEffects} 上限）", labelStyle);

            scrollEffects = GUILayout.BeginScrollView(scrollEffects, GUILayout.ExpandHeight(true));
            for (int i = player.RecentEffects.Count - 1; i >= 0; i--)
            {
                var r = player.RecentEffects[i];
                GUILayout.Label(TattooI18N.ResultZh(r), logStyle);
            }
            GUILayout.EndScrollView();

            GUILayout.EndArea();
        }

        // ---------- 装备操作 ----------
        public void EquipCurrent()
        {
            player.Composer.Equip(new TattooSlot
            {
                Part    = parts[PartIndex],
                Color   = colors[ColorIndex],
                Pattern = patterns[PatternIndex],
            });
            AppendLog($"装入 Build: {CurrentLabel()}");
        }

        public void ClearBuild()
        {
            player.Composer = new TattooComposer();
            player.Composer.Player = new PlayerSelf { Name = "玩家", Health = player.MaxHP };
            AppendLog("清空 Build");
        }

        public void LoadFullBuild()
        {
            ClearBuild();
            player.Composer.Equip(new TattooSlot { Part = parts[0], Color = colors[0], Pattern = patterns[1] }); // 脑袋红圆
            player.Composer.Equip(new TattooSlot { Part = parts[1], Color = colors[1], Pattern = patterns[1] }); // 躯干黄圆
            player.Composer.Equip(new TattooSlot { Part = parts[2], Color = colors[0], Pattern = patterns[0] }); // 左臂红线
            player.Composer.Equip(new TattooSlot { Part = parts[3], Color = colors[0], Pattern = patterns[0] }); // 右臂红线
            player.Composer.Equip(new TattooSlot { Part = parts[4], Color = colors[1], Pattern = patterns[4] }); // 左腿黄闪电
            player.Composer.Equip(new TattooSlot { Part = parts[5], Color = colors[6], Pattern = patterns[7] }); // 右腿白兽形
            AppendLog("快速装入 6 槽（脑袋红圆 / 躯干黄圆 / 左臂红线 / 右臂红线 / 左腿黄闪电 / 右腿白兽形）");
        }

        // ---------- 内部 ----------

        void AppendLog(string s)
        {
            log.Add(s);
            while (log.Count > MaxLogLines) log.RemoveAt(0);
        }

        void EnsureStyles()
        {
            if (cnFont == null)
            {
                try
                {
                    cnFont = Font.CreateDynamicFontFromOSFont(
                        new[] { "Microsoft YaHei UI", "Microsoft YaHei", "SimHei",
                                "PingFang SC", "Arial Unicode MS" }, 14);
                }
                catch { cnFont = null; }
            }
            if (headerStyle == null)
            {
                headerStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold, richText = true };
                labelStyle  = new GUIStyle(GUI.skin.label) { fontSize = 12, richText = true, wordWrap = true };
                btnStyle    = new GUIStyle(GUI.skin.button) { fontSize = 12, richText = true };
                logStyle    = new GUIStyle(GUI.skin.label) { fontSize = 11, richText = true, wordWrap = true };
            }
            if (cnFont != null)
            {
                headerStyle.font = cnFont; labelStyle.font = cnFont;
                btnStyle.font = cnFont; logStyle.font = cnFont;
            }
        }

        string CurrentLabel()
            => $"{PartCN[parts[PartIndex].PartName]} × {ColorCN[colors[ColorIndex].ColorName]} × {PatternCN[patterns[PatternIndex].PatternName]}";
        string SlotLabel(TattooSlot s)
            => $"{PartCN[s.Part.PartName]}({EventCN[s.Part.TriggerEvent]}) × {ColorCN[s.Color.ColorName]} × {PatternCN[s.Pattern.PatternName]}";

        string[] PartLabels() { var r = new string[parts.Length]; for (int i = 0; i < parts.Length; i++) r[i] = PartCN[parts[i].PartName]; return r; }
        string[] ColorLabels(){ var r = new string[colors.Length]; for (int i = 0; i < colors.Length; i++) r[i] = ColorCN[colors[i].ColorName]; return r; }
        string[] PatternLabels(){ var r = new string[patterns.Length]; for (int i = 0; i < patterns.Length; i++) r[i] = PatternCN[patterns[i].PatternName]; return r; }

        void BuildAtoms()
        {
            parts = new BodyPart[]
            {
                AtomFactory.MakePart<HeadPart>     ("Head",     GameEvent.OnCrit,         StatType.CritMultiplier),
                AtomFactory.MakePart<TorsoPart>    ("Torso",    GameEvent.OnDamaged,      StatType.MaxHealth),
                AtomFactory.MakePart<LeftArmPart>  ("LeftArm",  GameEvent.OnSkillCast,    StatType.SkillPower,    SymmetryGroup.Arms),
                AtomFactory.MakePart<RightArmPart> ("RightArm", GameEvent.OnAttack,       StatType.WeaponDamage,  SymmetryGroup.Arms),
                AtomFactory.MakePart<LeftLegPart>  ("LeftLeg",  GameEvent.OnDodgePressed, StatType.DodgeFrames,   SymmetryGroup.Legs),
                AtomFactory.MakePart<RightLegPart> ("RightLeg", GameEvent.OnMoveTick,     StatType.MoveSpeed,     SymmetryGroup.Legs),
            };
            colors = new[]
            {
                AtomFactory.MakeColor<FireElement>     ("Red"),
                AtomFactory.MakeColor<LightningElement>("Yellow"),
                AtomFactory.MakeColor<NatureElement>   ("Green"),
                AtomFactory.MakeColor<FrostElement>    ("Blue"),
                AtomFactory.MakeColor<MutationElement> ("Purple"),
                AtomFactory.MakeColor<HolyElement>     ("Gold"),
                AtomFactory.MakeColor<PureElement>     ("White"),
            };
            patterns = new[]
            {
                AtomFactory.MakePattern<SingleHitShape>   ("Line"),
                AtomFactory.MakePattern<AOEBurstShape>    ("Ring"),
                AtomFactory.MakePattern<StackingMarkShape>("Spiral"),
                AtomFactory.MakePattern<MultiHitShape>    ("Zigzag"),
                AtomFactory.MakePattern<ChainJumpShape>   ("Bolt"),
                AtomFactory.MakePattern<ProbBurstShape>   ("Star"),
                AtomFactory.MakePattern<TrailZoneShape>   ("Stream"),
                AtomFactory.MakePattern<SummonFormShape>  ("Beast"),
            };
        }
    }
}
