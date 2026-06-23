using System.Collections.Generic;
using System.Text;

namespace Tattoo
{
    /// <summary>
    /// 统一的中文映射 + 渲染助手。EffectResult.ToString() 与 UI 文字都走这里。
    /// </summary>
    public static class TattooI18N
    {
        public static readonly Dictionary<string, string> Part = new()
        {
            { "Head", "脑袋" }, { "Torso", "躯干" }, { "LeftArm", "左臂" },
            { "RightArm", "右臂" }, { "LeftLeg", "左腿" }, { "RightLeg", "右腿" },
            { "OnHitExtra", "命中额外" },
        };
        public static readonly Dictionary<string, string> Color = new()
        {
            { "Red", "红" }, { "Yellow", "黄" }, { "Green", "绿" },
            { "Blue", "蓝" }, { "Purple", "紫" }, { "Gold", "金" }, { "White", "白" },
        };
        public static readonly Dictionary<string, string> Pattern = new()
        {
            { "Line", "直线" }, { "Ring", "圆环" }, { "Spiral", "螺旋" },
            { "Zigzag", "锯齿" }, { "Bolt", "闪电" }, { "Star", "星形" },
            { "Stream", "流线" }, { "Beast", "兽形" },
        };
        public static readonly Dictionary<string, string> Shape = new()
        {
            { "SingleHitShape",    "单体直击" },
            { "AOEBurstShape",     "范围爆裂" },
            { "StackingMarkShape", "叠层印记" },
            { "MultiHitShape",     "多段拆击" },
            { "ChainJumpShape",    "连锁跳跃" },
            { "ProbBurstShape",    "概率爆发" },
            { "TrailZoneShape",    "残留轨迹" },
            { "SummonFormShape",   "召唤幻兽" },
            { "Pure×Bolt:额外普攻", "纯能×闪电:额外普攻" },
            { "Pure×Star:刷新冷却", "纯能×星形:刷新冷却" },
            { "Holy×Bolt:跳跳回血", "神圣×闪电:跳跳回血" },
            { "Mutation×Star:现实崩塌", "异变×星形:现实崩塌" },
        };
        public static readonly Dictionary<string, string> Element = new()
        {
            { "Fire", "火焰" }, { "Lightning", "雷电" }, { "Nature", "自然/毒" },
            { "Frost", "冰霜" }, { "Mutation", "异变" }, { "Holy", "神圣" }, { "Pure", "纯能" },
        };
        public static readonly Dictionary<GameEvent, string> Event = new()
        {
            { GameEvent.OnAttack, "普攻命中" },
            { GameEvent.OnCrit, "暴击命中" },
            { GameEvent.OnDamaged, "自身受伤" },
            { GameEvent.OnSkillCast, "释放技能" },
            { GameEvent.OnDodgePressed, "闪避按下" },
            { GameEvent.OnMoveTick, "持续移动" },
        };
        public static readonly Dictionary<StatType, string> Stat = new()
        {
            { StatType.CritMultiplier, "暴击伤害" },
            { StatType.MaxHealth,      "生命上限" },
            { StatType.SkillPower,     "技能强度" },
            { StatType.WeaponDamage,   "武器伤害" },
            { StatType.DodgeFrames,    "闪避帧" },
            { StatType.MoveSpeed,      "移动速度" },
        };
        public static readonly Dictionary<ElementType, string> ElementTypeName = new()
        {
            { ElementType.Fire, "火" }, { ElementType.Lightning, "雷" },
            { ElementType.Nature, "毒" }, { ElementType.Frost, "冰" },
            { ElementType.Mutation, "异变" }, { ElementType.Holy, "神圣" }, { ElementType.Pure, "纯能" },
        };

        public static string PartZh(string raw)    => string.IsNullOrEmpty(raw) ? "?" : (Part.TryGetValue(raw, out var s) ? s : raw);
        public static string ElementZh(string raw) => string.IsNullOrEmpty(raw) ? "?" : (Element.TryGetValue(raw, out var s) ? s : raw);
        public static string EventZh(GameEvent e)  => Event.TryGetValue(e, out var s) ? s : e.ToString();
        public static string StatZh(StatType st)   => Stat.TryGetValue(st, out var s) ? s : st.ToString();
        public static string PatternZh(string raw) => string.IsNullOrEmpty(raw) ? "?" : (Pattern.TryGetValue(raw, out var s) ? s : raw);
        public static string ColorZh(string raw)   => string.IsNullOrEmpty(raw) ? "?" : (Color.TryGetValue(raw, out var s) ? s : raw);

        public static string ShapeZh(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "?";
            foreach (var kv in Shape)
                if (raw.StartsWith(kv.Key))
                    return kv.Value + raw.Substring(kv.Key.Length)
                        .Replace(":Burst", "·爆发")
                        .Replace(":Stack", "·叠层")
                        .Replace(":Miss", "·未触发");
            return raw;
        }

        public static string StatusZh(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            string r = s;
            r = r.Replace("Burn",      "燃烧");
            r = r.Replace("Paralyze",  "麻痹");
            r = r.Replace("Poison",    "中毒");
            r = r.Replace("Slow",      "冰缓");
            r = r.Replace("Mutate-眩晕","异变·眩晕");
            r = r.Replace("Mutate-沉默","异变·沉默");
            r = r.Replace("Mutate-虚弱","异变·虚弱");
            r = r.Replace("Mutate",    "异变");
            r = r.Replace("Holy(heal", "神圣(治疗");
            r = r.Replace("Summon[",   "幻兽[");
            r = r.Replace("InTrail",   "残留区");
            r = r.Replace("BurstAt",   "叠满引爆@");
            r = r.Replace("Stack",     "叠层");
            r = r.Replace("miss",      "未触发");
            r = r.Replace("Intercepted", "拦截");
            r = r.Replace("PendingTrigger", "延迟触发");
            r = r.Replace("ConsumePending", "消耗延迟");
            r = r.Replace("ConsumedPending", "已消耗");
            r = r.Replace("ExtraBasic", "额外普攻");
            r = r.Replace("RefreshPendings", "刷新延迟");
            r = r.Replace("RandomSeedReset", "重置随机");
            r = r.Replace("OnHitExtra", "命中追加");
            return r;
        }

        public static string ResultZh(EffectResult r)
        {
            var sb = new StringBuilder();
            sb.Append('[').Append(PartZh(r.Part ?? "?")).Append("] ");
            sb.Append(ShapeZh(r.Shape ?? "?")).Append('<').Append(ElementZh(r.Element ?? "?")).Append("> ");
            sb.Append("伤害=").Append(r.Damage.ToString("F1"));
            sb.Append(" 命中=").Append(r.HitCount);
            if (!string.IsNullOrEmpty(r.Status)) sb.Append(" 状态[").Append(StatusZh(r.Status)).Append(']');
            if (r.SynergyMul > 0f && r.SynergyMul != 1f) sb.Append(" 联动=").Append(r.SynergyMul.ToString("F2")).Append('×');
            if (!string.IsNullOrEmpty(r.Note))   sb.Append(' ').Append(StatusZh(r.Note));
            return sb.ToString();
        }
    }
}
