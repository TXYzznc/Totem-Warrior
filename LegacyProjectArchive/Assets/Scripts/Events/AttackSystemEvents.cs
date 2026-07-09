using Tattoo.Data;

namespace AttackSystem.Events
{
    // ============================================================
    // change #20 player-attack-system —— 新增事件总表
    //
    // 本文件冻结于阶段 2 公共骨架；阶段 3 fan-out 各 agent 不得修改任一签名。
    // CONTRACT.md §1.1 / §1.3 是 source of truth。
    // ============================================================

    /// <summary>
    /// 起手三选确认。StartupSelectForm.OnConfirm 发布；SpawnerModule 订阅后装备玩家。
    /// </summary>
    public sealed class StartupSelectedEvent
    {
        public int    ColorId;
        public string WeaponId;
        public int[]  PatternIds;

        public StartupSelectedEvent(int colorId, string weaponId, int[] patternIds)
        {
            ColorId    = colorId;
            WeaponId   = weaponId;
            PatternIds = patternIds;
        }
    }

    /// <summary>
    /// 武器装备完成。WeaponModule.EquipWeapon 内发布；PlayerWeaponMounter MonoBehaviour 订阅卸旧装新。
    /// WeaponPrefabPath 来自 WeaponConfig.WeaponPrefabPath（相对 Resources/，不带扩展名）。
    /// </summary>
    public sealed class WeaponEquippedEvent
    {
        public Target Actor;
        public string WeaponId;
        public string WeaponPrefabPath;

        public WeaponEquippedEvent(Target actor, string weaponId, string weaponPrefabPath)
        {
            Actor            = actor;
            WeaponId         = weaponId;
            WeaponPrefabPath = weaponPrefabPath;
        }
    }

    /// <summary>
    /// 状态效果首次添加到 target（如 Burn / Poison）。
    /// StatusEffectModule.ApplyStatus 发布；HUD / VFX 订阅添加图标和粒子。
    /// </summary>
    public sealed class StatusEffectAppliedEvent
    {
        public Target Target;
        public string StatusName;
        public float  DPS;
        public float  Duration;
        public Target Source;

        public StatusEffectAppliedEvent(Target target, string statusName, float dps, float duration, Target source)
        {
            Target     = target;
            StatusName = statusName;
            DPS        = dps;
            Duration   = duration;
            Source     = source;
        }
    }

    /// <summary>
    /// 单次 tick 扣血结果（每 0.5s）。
    /// StatusEffectModule.OnUpdate 发布；HUD 用作浮动数字。
    /// </summary>
    public sealed class StatusEffectTickedEvent
    {
        public Target Target;
        public string StatusName;
        public float  Damage;

        public StatusEffectTickedEvent(Target target, string statusName, float damage)
        {
            Target     = target;
            StatusName = statusName;
            Damage     = damage;
        }
    }

    /// <summary>
    /// 状态效果到期。StatusEffectModule 在 RemainingSec ≤ 0 时发布；HUD 移除图标。
    /// </summary>
    public sealed class StatusEffectExpiredEvent
    {
        public Target Target;
        public string StatusName;

        public StatusEffectExpiredEvent(Target target, string statusName)
        {
            Target     = target;
            StatusName = statusName;
        }
    }

    /// <summary>
    /// 玩家 HP 变化（受击 / 治疗 / 复活）。PlayerDamageReceiver 在 HP 变更后发布；HUD 血条订阅。
    /// </summary>
    public sealed class PlayerHealthChangedEvent
    {
        public float Current;
        public float Max;
        public float Delta;

        public PlayerHealthChangedEvent(float current, float max, float delta)
        {
            Current = current;
            Max     = max;
            Delta   = delta;
        }
    }
}
