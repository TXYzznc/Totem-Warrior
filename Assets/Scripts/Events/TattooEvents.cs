using System.Collections.Generic;
using Tattoo.Data;

namespace Tattoo.Events
{
    // ========== 战斗触发事件（替代原 enum GameEvent）==========

    /// <summary>右臂触发：普攻命中。</summary>
    public class AttackHitEvent
    {
        public Target Target;
        public float  BaseDamage;
        public AttackHitEvent(Target target, float baseDamage)
        {
            Target = target;
            BaseDamage = baseDamage;
        }
    }

    /// <summary>脑袋触发：暴击命中。</summary>
    public class CritHitEvent
    {
        public Target Target;
        public float  BaseDamage;
        public CritHitEvent(Target target, float baseDamage)
        {
            Target = target;
            BaseDamage = baseDamage;
        }
    }

    /// <summary>躯干触发：玩家自身受伤。</summary>
    public class DamagedEvent
    {
        public Target Attacker;
        public float  Damage;
        public DamagedEvent(Target attacker, float damage)
        {
            Attacker = attacker;
            Damage = damage;
        }
    }

    /// <summary>左臂触发：技能释放（不要求技能成功）。</summary>
    public class SkillCastEvent
    {
        public string SkillId;
        public SkillCastEvent(string skillId)
        {
            SkillId = skillId;
        }
    }

    /// <summary>左腿触发：闪避按下（不要求成功）。</summary>
    public class DodgePressedEvent { }

    /// <summary>右腿触发：移动 tick（每 0.5s）。</summary>
    public class MoveTickEvent
    {
        public Target[] Path;
        public float    Distance;
        public MoveTickEvent(Target[] path, float distance)
        {
            Path = path;
            Distance = distance;
        }
    }

    // ========== 战斗结果事件（TattooModule → 外部）==========

    /// <summary>一次效果应用完成后广播。UI / CombatModule 等订阅。</summary>
    public class EffectAppliedEvent
    {
        public IReadOnlyList<EffectResult> Results;
        public EffectAppliedEvent(IReadOnlyList<EffectResult> results)
        {
            Results = results;
        }
    }

    /// <summary>目标血量归零。CombatModule 订阅后判定胜负。</summary>
    public class TargetKilledEvent
    {
        public Target Target;
        public TargetKilledEvent(Target target)
        {
            Target = target;
        }
    }

    /// <summary>玩家血量归零。</summary>
    public class PlayerDiedEvent { }

    /// <summary>纹身槽位触发后投递给 VFX 层的视觉信号。带主目标和最多 N 个附近目标，便于线条/粒子从玩家飞到目标。</summary>
    public class VFXTriggerEvent
    {
        public string PartName;
        public string ElementName;
        public string ShapeName;
        public Target PrimaryTarget;
        public Target[] NearbyTargets;
        public float Magnitude;
        public bool Intercepted; // 左腿打包 → 暂不画弹道，画一个自身的环

        public VFXTriggerEvent(string part, string element, string shape, Target primary, Target[] nearby, float magnitude, bool intercepted)
        {
            PartName = part;
            ElementName = element;
            ShapeName = shape;
            PrimaryTarget = primary;
            NearbyTargets = nearby;
            Magnitude = magnitude;
            Intercepted = intercepted;
        }
    }

    // ========== Build / 装备事件 ==========

    /// <summary>装备变化（Equip / Clear）后广播。UI 重绘装备列表。</summary>
    public class BuildChangedEvent
    {
        public IReadOnlyList<TattooSlot> Equipped;
        public BuildChangedEvent(IReadOnlyList<TattooSlot> equipped)
        {
            Equipped = equipped;
        }
    }

    /// <summary>被动属性重算后广播。UI 重绘被动条目。</summary>
    public class PassiveRecomputedEvent
    {
        public PassiveStats Stats;
        public PassiveRecomputedEvent(PassiveStats stats)
        {
            Stats = stats;
        }
    }

    // ========== 阶段事件 ==========

    /// <summary>ModuleRunner 全部模块就绪。SpawnerModule 发出后 CombatModule 才开始接收输入。</summary>
    public class GameReadyEvent { }

    /// <summary>战斗开始。携带敌人数。</summary>
    public class CombatStartedEvent
    {
        public int EnemyCount;
        public CombatStartedEvent(int enemyCount)
        {
            EnemyCount = enemyCount;
        }
    }

    /// <summary>战斗结束。携带胜负。</summary>
    public class CombatEndedEvent
    {
        public bool PlayerWin;
        public CombatEndedEvent(bool playerWin)
        {
            PlayerWin = playerWin;
        }
    }

    // ========== 输入事件（InputModule → CombatModule，避免 CombatModule 直接读 Input）==========

    /// <summary>玩家按下攻击键（鼠标左键）。</summary>
    public class InputAttackEvent { }

    /// <summary>玩家按下技能键（E）。</summary>
    public class InputSkillEvent { }

    /// <summary>玩家按下闪避键（空格）。</summary>
    public class InputDodgeEvent { }
}
