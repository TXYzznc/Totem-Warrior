using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Tattoo
{
    /// <summary>
    /// 运行时演示：IMGUI 左侧面板自由切换 部位/颜色/图案 + 触发查看效果，Console 中文输出。
    /// 适配 v2 三层架构（BodyPart 子类家族 + ElementBehavior 子类家族）。
    /// </summary>
    public class TattooDemoRunner : MonoBehaviour
    {
        // ---------- 中文映射 ----------
        static readonly Dictionary<string, string> PartCN = new()
        {
            { "Head", "脑袋" }, { "Torso", "躯干" },
            { "LeftArm", "左臂" }, { "RightArm", "右臂" },
            { "LeftLeg", "左腿" }, { "RightLeg", "右腿" },
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
        static readonly Dictionary<string, string> ShapeCN = new()
        {
            { "SingleHitShape",    "单体直击" }, { "AOEBurstShape",     "范围爆裂" },
            { "StackingMarkShape", "叠层印记" }, { "MultiHitShape",     "多段拆击" },
            { "ChainJumpShape",    "连锁跳跃" }, { "ProbBurstShape",    "概率爆发" },
            { "TrailZoneShape",    "残留轨迹" }, { "SummonFormShape",   "召唤幻兽" },
        };
        static readonly Dictionary<string, string> ElementCN = new()
        {
            { "Fire", "火焰" }, { "Lightning", "雷电" }, { "Nature", "自然/毒" },
            { "Frost", "冰霜" }, { "Mutation", "异变" }, { "Holy", "神圣" }, { "Pure", "纯能" },
        };
        static readonly Dictionary<GameEvent, string> EventCN = new()
        {
            { GameEvent.OnAttack,        "普攻命中" },
            { GameEvent.OnCrit,          "暴击命中" },
            { GameEvent.OnDamaged,       "自身受伤" },
            { GameEvent.OnSkillCast,     "释放技能" },
            { GameEvent.OnDodgePressed,  "闪避按下" },
            { GameEvent.OnMoveTick,      "持续移动" },
        };

        [Header("当前选择")]
        public int PartIndex    = 0;
        public int ColorIndex   = 0;
        public int PatternIndex = 0;

        [Header("演示设置")]
        public int  DummyHp    = 1000;
        public int  NearbyCount = 2;
        public bool ShowGUI    = true;

        BodyPart[]   parts;
        ColorSO[]    colors;
        PatternSO[]  patterns;
        TattooComposer composer;
        Target dummy;
        readonly List<Target> nearby = new();
        readonly List<string> log = new();
        const int MaxLogLines = 16;

        Font cnFont;
        GUIStyle headerStyle, labelStyle, btnStyle, logStyle;
        Vector2 scroll;

        public TattooComposer Composer => composer;
        public Target         Dummy    => dummy;
        public IReadOnlyList<Target> NearbyView => nearby;

        public void Initialize() => EnsureInit();

        public void LoadFullBuild()
        {
            EnsureInit();
            composer = new TattooComposer();
            composer.Equip(new TattooSlot { Part = parts[0], Color = colors[0], Pattern = patterns[1] }); // 脑袋红圆环
            composer.Equip(new TattooSlot { Part = parts[1], Color = colors[1], Pattern = patterns[1] }); // 躯干黄圆环
            composer.Equip(new TattooSlot { Part = parts[2], Color = colors[0], Pattern = patterns[0] }); // 左臂红直线
            composer.Equip(new TattooSlot { Part = parts[3], Color = colors[0], Pattern = patterns[0] }); // 右臂红直线
            composer.Equip(new TattooSlot { Part = parts[4], Color = colors[1], Pattern = patterns[4] }); // 左腿黄闪电
            composer.Equip(new TattooSlot { Part = parts[5], Color = colors[6], Pattern = patterns[7] }); // 右腿白兽形
        }

        public List<EffectResult> TriggerEvent(GameEvent ev)
        {
            EnsureInit();
            var ctx = MakeCtx();
            composer.Fire(ev, ctx);
            Report($"[Build 触发]", ev, ctx);
            return ctx.Log;
        }

        public void FireCurrentSingle()
        {
            EnsureInit();
            var slot = new TattooSlot
            {
                Part    = parts[PartIndex],
                Color   = colors[ColorIndex],
                Pattern = patterns[PatternIndex],
            };
            var one = new TattooComposer();
            one.Player = composer.Player; // 共享玩家自身状态，方便看叠层
            one.Equip(slot);
            var ctx = MakeCtx();
            one.Fire(slot.Part.TriggerEvent, ctx);
            Report("[单槽触发]", slot.Part.TriggerEvent, ctx);
        }

        public void EquipCurrent()
        {
            EnsureInit();
            composer.Equip(new TattooSlot
            {
                Part    = parts[PartIndex],
                Color   = colors[ColorIndex],
                Pattern = patterns[PatternIndex],
            });
            AppendLog($"<color=#7fff7f>装入 Build：</color>{CurrentLabel()}（共 {composer.Equipped.Count} 槽）");
        }

        public void ClearBuild()
        {
            EnsureInit();
            composer = new TattooComposer();
            AppendLog("<color=#ffaaaa>清空 Build</color>");
        }

        public void ResetTargets()
        {
            dummy = new Target { Name = "假人", Health = DummyHp };
            nearby.Clear();
            for (int i = 0; i < NearbyCount; i++)
                nearby.Add(new Target { Name = $"敌人{i + 1}", Health = DummyHp });
            if (composer != null) { composer.Player = new PlayerSelf { Name = "玩家", Health = 100f }; composer.RecomputePassive(); }
            AppendLog($"<color=#aaccff>目标 & 玩家状态已重置</color>");
        }

        EffectContext MakeCtx()
        {
            var ctx = new EffectContext { PrimaryTarget = dummy };
            foreach (var t in nearby) ctx.NearbyTargets.Add(t);
            ctx.LastAttacker = nearby.Count > 0 ? nearby[0] : null;
            ctx.MovementPath = new List<Target>(nearby);
            return ctx;
        }

        void Start()
        {
            EnsureInit();
            AppendLog("演示就绪：左侧切换部位/颜色/图案，单槽触发或装入 Build 看联动。");
        }

        void OnGUI()
        {
            if (!ShowGUI) return;
            EnsureInit();
            EnsureStyles();

            var areaW = Mathf.Min(820, Screen.width - 20);
            GUILayout.BeginArea(new Rect(10, 10, areaW, Screen.height - 20), GUI.skin.box);
            GUILayout.Label("纹身原子系统 v2 演示（6 部位 × 7 颜色 × 8 图案 = 336）", headerStyle);

            var p = parts[PartIndex];
            GUILayout.Label($"部位（WHEN/SCALE）  触发：{EventCN[p.TriggerEvent]}  缩放：{ScaleStatCN(p.ScaleStat)}", labelStyle);
            PartIndex = GUILayout.SelectionGrid(PartIndex, PartLabels(), 6, btnStyle);

            GUILayout.Label("颜色（WHAT 元素）", labelStyle);
            ColorIndex = GUILayout.SelectionGrid(ColorIndex, ColorLabels(), 7, btnStyle);

            GUILayout.Label("图案（HOW 形状）", labelStyle);
            PatternIndex = GUILayout.SelectionGrid(PatternIndex, PatternLabels(), 8, btnStyle);

            GUILayout.Space(6);
            GUILayout.Label($"当前组合：<b>{CurrentLabel()}</b>", labelStyle);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("单槽触发", btnStyle, GUILayout.Height(30))) FireCurrentSingle();
            if (GUILayout.Button("装入 Build", btnStyle, GUILayout.Height(30))) EquipCurrent();
            if (GUILayout.Button("清空 Build", btnStyle, GUILayout.Height(30))) ClearBuild();
            if (GUILayout.Button("重置目标", btnStyle, GUILayout.Height(30))) ResetTargets();
            GUILayout.EndHorizontal();

            GUILayout.Space(4);
            GUILayout.Label($"已装备 Build（{composer.Equipped.Count} 槽）：", labelStyle);
            foreach (var s in composer.Equipped) GUILayout.Label($"  · {SlotLabel(s)}", labelStyle);

            if (composer.Equipped.Count > 0)
            {
                GUILayout.Label("对 Build 触发某事件：", labelStyle);
                GUILayout.BeginHorizontal();
                foreach (GameEvent ev in System.Enum.GetValues(typeof(GameEvent)))
                    if (GUILayout.Button(EventCN[ev], btnStyle, GUILayout.Height(26)))
                        TriggerEvent(ev);
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(6);
            GUILayout.Label($"主目标 {dummy.Name}：HP={dummy.Health:F1}  状态[{StatusZh(dummy.Statuses)}]", labelStyle);
            foreach (var t in nearby) GUILayout.Label($"  附近 {t.Name}：HP={t.Health:F1}  状态[{StatusZh(t.Statuses)}]", labelStyle);

            var pl = composer.Player;
            GUILayout.Label($"玩家 HP={pl.Health:F1}  叠层[{string.Join(",", System.Linq.Enumerable.Select(pl.Stacks, kv => kv.Key + ":" + kv.Value))}]  待触发{pl.PendingTriggers.Count}", labelStyle);

            GUILayout.Label($"被动 modifier ({pl.Passive.EntryLog.Count} 条)：", labelStyle);
            foreach (var entry in pl.Passive.EntryLog) GUILayout.Label("  · " + entry, logStyle);

            GUILayout.Space(6);
            GUILayout.Label("日志：", labelStyle);
            scroll = GUILayout.BeginScrollView(scroll, GUILayout.ExpandHeight(true));
            foreach (var line in log) GUILayout.Label(line, logStyle);
            GUILayout.EndScrollView();

            GUILayout.EndArea();
        }

        // ---------- 内部 ----------
        void EnsureInit()
        {
            if (parts != null) return;
            BuildAtoms();
            composer = new TattooComposer();
            ResetTargets();
        }

        void EnsureStyles()
        {
            if (cnFont == null)
            {
                try
                {
                    cnFont = Font.CreateDynamicFontFromOSFont(
                        new[] { "Microsoft YaHei UI", "Microsoft YaHei", "SimHei",
                                "PingFang SC", "Hiragino Sans GB", "Arial Unicode MS" }, 14);
                }
                catch { cnFont = null; }
            }
            if (headerStyle == null)
            {
                headerStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold, richText = true };
                labelStyle  = new GUIStyle(GUI.skin.label) { fontSize = 13, richText = true, wordWrap = true };
                btnStyle    = new GUIStyle(GUI.skin.button) { fontSize = 13, richText = true };
                logStyle    = new GUIStyle(GUI.skin.label) { fontSize = 12, richText = true, wordWrap = true };
            }
            if (cnFont != null)
            {
                headerStyle.font = cnFont; labelStyle.font = cnFont;
                btnStyle.font = cnFont; logStyle.font = cnFont;
            }
        }

        void Report(string prefix, GameEvent ev, EffectContext ctx)
        {
            AppendLog($"<b>{prefix} 事件={EventCN[ev]}</b>  共 {ctx.Log.Count} 条效果");
            foreach (var r in ctx.Log)
            {
                string partZh  = PartCN.TryGetValue(r.Part ?? "", out var p) ? p : (r.Part ?? "?");
                string elemZh  = ElementCN.TryGetValue(r.Element ?? "", out var e) ? e : (r.Element ?? "?");
                string shapeZh = ShapeZh(r.Shape);
                AppendLog($"  → {partZh} | {elemZh} | {shapeZh}：伤害={r.Damage:F1}  命中={r.HitCount}  联动={r.SynergyMul:F2}× 状态[{r.Status}] {r.Note}");
            }
            AppendLog($"  主目标 HP={dummy.Health:F1} 状态[{StatusZh(dummy.Statuses)}]");
        }

        void AppendLog(string s)
        {
            log.Add(s);
            while (log.Count > MaxLogLines) log.RemoveAt(0);
            Debug.Log("[纹身] " + StripRich(s));
        }
        static string StripRich(string s) => Regex.Replace(s ?? "", "<.*?>", "");

        string CurrentLabel()
            => $"{PartCN[parts[PartIndex].PartName]} × {ColorCN[colors[ColorIndex].ColorName]} × {PatternCN[patterns[PatternIndex].PatternName]}";

        string SlotLabel(TattooSlot s)
            => $"{PartCN[s.Part.PartName]}（{EventCN[s.Part.TriggerEvent]}）× {ColorCN[s.Color.ColorName]} × {PatternCN[s.Pattern.PatternName]}";

        string ShapeZh(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "?";
            foreach (var kv in ShapeCN)
                if (raw.StartsWith(kv.Key))
                    return kv.Value + (raw.Length > kv.Key.Length ? raw.Substring(kv.Key.Length).Replace(":Burst", "·爆发").Replace(":Stack", "·叠层").Replace(":Miss", "·未触发") : "");
            return raw;
        }

        string StatusZh(List<string> ss)
        {
            if (ss == null || ss.Count == 0) return "";
            var sb = new StringBuilder();
            for (int i = 0; i < ss.Count; i++) { if (i > 0) sb.Append(", "); sb.Append(ss[i]); }
            return sb.ToString();
        }

        static string ScaleStatCN(StatType s) => s switch
        {
            StatType.CritMultiplier => "暴击伤害",
            StatType.WeaponDamage   => "武器伤害",
            StatType.MaxHealth      => "生命上限",
            StatType.SkillPower     => "技能强度",
            StatType.MoveSpeed      => "移动速度",
            StatType.DodgeFrames    => "闪避帧",
            _ => s.ToString(),
        };

        string[] PartLabels()
        {
            var r = new string[parts.Length];
            for (int i = 0; i < parts.Length; i++) r[i] = PartCN[parts[i].PartName];
            return r;
        }
        string[] ColorLabels()
        {
            var r = new string[colors.Length];
            for (int i = 0; i < colors.Length; i++) r[i] = ColorCN[colors[i].ColorName];
            return r;
        }
        string[] PatternLabels()
        {
            var r = new string[patterns.Length];
            for (int i = 0; i < patterns.Length; i++) r[i] = PatternCN[patterns[i].PatternName];
            return r;
        }

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

    /// <summary>构造内存中 SO 的统一工厂，供 Demo 和测试共用。</summary>
    public static class AtomFactory
    {
        public static T MakePart<T>(string name, GameEvent ev, StatType st,
            SymmetryGroup sym = SymmetryGroup.None, float scale = -1f) where T : BodyPart
        {
            var p = ScriptableObject.CreateInstance<T>();
            p.PartName = name; p.TriggerEvent = ev; p.ScaleStat = st;
            p.SymmetryGroup = sym;
            p.ScaleFactor = scale > 0 ? scale : p.DefaultScaleFactor;
            return p;
        }
        public static ColorSO MakeColor<T>(string name, float mul = 1f) where T : ElementBehavior
        {
            var el = ScriptableObject.CreateInstance<T>();
            el.ElementName = typeof(T).Name.Replace("Element", "");
            var c = ScriptableObject.CreateInstance<ColorSO>();
            c.ColorName = name; c.Element = el; c.ColorMultiplier = mul;
            return c;
        }
        public static PatternSO MakePattern<T>(string name, float mul = 1f) where T : EffectShape
        {
            var sh = ScriptableObject.CreateInstance<T>();
            var p = ScriptableObject.CreateInstance<PatternSO>();
            p.PatternName = name; p.Shape = sh; p.PatternMultiplier = mul;
            return p;
        }
    }
}
