using Tattoo.Data;

namespace Weapon.Events
{
    /// <summary>
    /// 普攻命中时发布（近战 Hitbox 触碰目标 / 远程 Projectile 命中目标）。
    /// CONTRACT §1.1：含 WeaponId 字段，VFXModule / AudioModule 据此选对应效果。
    /// </summary>
    public sealed class WeaponAttackHitEvent
    {
        public Target Attacker;
        public Target Target;
        public float  BaseDamage;
        public string WeaponId;
        public bool   IsCrit;
        public bool   IsCharged;

        public WeaponAttackHitEvent(Target attacker, Target target, float baseDamage,
            string weaponId, bool isCrit = false, bool isCharged = false)
        {
            Attacker   = attacker;
            Target     = target;
            BaseDamage = baseDamage;
            WeaponId   = weaponId;
            IsCrit     = isCrit;
            IsCharged  = isCharged;
        }
    }

    /// <summary>
    /// 蓄力攻击命中时发布。CONTRACT §1.1 锁定签名。
    /// ChargeRatio = 0.0 (未蓄力) ~ 1.0 (满蓄)。
    /// </summary>
    public sealed class WeaponChargedAttackEvent
    {
        public Target Attacker;
        public Target Target;
        public float  ChargeRatio;
        public float  BaseDamage;
        public string WeaponId;

        public WeaponChargedAttackEvent(Target attacker, Target target,
            float chargeRatio, float baseDamage, string weaponId)
        {
            Attacker   = attacker;
            Target     = target;
            ChargeRatio = chargeRatio;
            BaseDamage = baseDamage;
            WeaponId   = weaponId;
        }
    }

    /// <summary>
    /// 弹药数量变化时发布（每次开火消耗 / 拾取补充）。
    /// UIModule 订阅后刷新武器槽 HUD；空弹时 AudioModule 播放空仓音效。
    /// CONTRACT §1.4 AmmoChangedEvent 对应实现。
    /// </summary>
    public sealed class AmmoChangedEvent
    {
        public Target Actor;
        public string WeaponId;
        public int    OldAmmo;
        public int    NewAmmo;

        public AmmoChangedEvent(Target actor, string weaponId, int oldAmmo, int newAmmo)
        {
            Actor    = actor;
            WeaponId = weaponId;
            OldAmmo  = oldAmmo;
            NewAmmo  = newAmmo;
        }
    }
}
