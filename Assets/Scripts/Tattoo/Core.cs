using System.Collections.Generic;

namespace Tattoo
{
    public enum GameEvent
    {
        OnAttack,         // 普攻命中（右臂）
        OnCrit,           // 暴击命中（脑袋）
        OnDamaged,        // 自身受伤（躯干）
        OnSkillCast,      // 释放技能（左臂）
        OnDodgePressed,   // 闪避按下，无需成功（左腿）
        OnMoveTick,       // 移动 tick（右腿，每 0.5s）
    }

    public enum StatType
    {
        CritMultiplier,   // 暴击伤害
        MaxHealth,        // 生命上限
        SkillPower,       // 技能强度
        WeaponDamage,     // 武器伤害
        DodgeFrames,      // 闪避无敌帧
        MoveSpeed,        // 移动速度
    }

    public enum SymmetryGroup { None, Arms, Legs }

    public enum ElementType
    {
        Fire, Lightning, Nature, Frost, Mutation, Holy, Pure,
    }

    // ========== Player state ==========

    public class PlayerStats
    {
        public float CritMultiplier = 1.5f;
        public float MaxHealth      = 100f;
        public float SkillPower     = 20f;
        public float WeaponDamage   = 10f;
        public float DodgeFrames    = 0.3f;
        public float MoveSpeed      = 5f;

        public float Get(StatType s) => s switch
        {
            StatType.CritMultiplier => CritMultiplier,
            StatType.MaxHealth      => MaxHealth,
            StatType.SkillPower     => SkillPower,
            StatType.WeaponDamage   => WeaponDamage,
            StatType.DodgeFrames    => DodgeFrames,
            StatType.MoveSpeed      => MoveSpeed,
            _ => 1f,
        };
    }

    public class PassiveStats
    {
        // 每元素的"属性增量"——由部位 passiveDimension + 颜色元素 + 图案数值生成
        public Dictionary<ElementType, float> Resistance   = new();
        public Dictionary<ElementType, float> ElementBonus = new();
        public float CritRateBonus     = 0f;
        public float MaxHealthBonus    = 0f;
        public float SkillPowerBonus   = 0f;
        public float WeaponDmgBonus    = 0f;
        public float DodgeFramesBonus  = 0f;
        public float MoveSpeedBonus    = 0f;

        public readonly List<string> EntryLog = new(); // 装备时累计的可读日志

        public void Add(string entry) => EntryLog.Add(entry);
        public void AddResist(ElementType e, float v)  { Resistance.TryGetValue(e, out var x); Resistance[e] = x + v; }
        public void AddElemBonus(ElementType e, float v){ ElementBonus.TryGetValue(e, out var x); ElementBonus[e] = x + v; }
    }

    public class PendingTrigger
    {
        public GameEvent       ConsumeOnEvent;
        public EffectShape     Shape;
        public ElementBehavior Element;
        public float           Magnitude;
        public string          Source;     // 部位名，调试用
        public int             ExpiresAfter = -1; // -1 = 永久；>=0 = 剩余可被消耗次数
    }

    public class PlayerSelf
    {
        public string Name = "玩家";
        public float  Health = 100f;
        public List<string> Buffs   = new();
        public Dictionary<string, int>   Stacks = new();  // 叠层 buff（如 Focus）
        public Dictionary<string, float> Cooldowns = new();
        public List<PendingTrigger> PendingTriggers = new();
        public PassiveStats Passive = new();
        public bool ShortInvincible = false; // 左腿闪避瞬间
    }

    public class Target
    {
        public string Name = "目标";
        public float  Health = 100f;
        public List<string> Statuses = new();
        public Dictionary<(EffectShape Shape, string Part), int> Marks = new();
    }

    public class EffectContext
    {
        public GameEvent           Event;
        public PlayerStats         Stats;
        public PlayerSelf          Self;
        public Target              PrimaryTarget;
        public List<Target>        NearbyTargets = new();
        public Target              LastAttacker;     // 躯干用：把它设为 PrimaryTarget
        public List<Target>        MovementPath = new(); // 右腿用：移动路径上的敌人
        public List<EffectResult>  Log = new();
    }

    public struct EffectResult
    {
        public string Element;
        public string Shape;
        public string Part;
        public float  Damage;
        public int    HitCount;
        public string Status;
        public float  SynergyMul;
        public string Note; // 额外标注，如 "Intercepted/PendingTrigger" 或 "ConsumedPending"

        public override string ToString() => TattooI18N.ResultZh(this);
    }

    public class TattooSlot
    {
        public BodyPart  Part;
        public ColorSO   Color;
        public PatternSO Pattern;
    }
}
