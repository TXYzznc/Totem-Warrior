using System;
using Tattoo.Data;
using Tattoo.Events;
using Weapon.Events;

namespace Tattoo.Strategies.Parts
{
    /// <summary>
    /// 脑袋：暴击事件源。
    ///
    /// change#20 实装：订阅 WeaponAttackHitEvent，按已装备 Head 槽的 PatternMultiplier
    /// 滚一次概率决定是否发 CritHitEvent（替代 CombatModule 原 25% 硬编码暴击）。
    ///
    /// 设计约束：
    /// - 无 Head 槽装备 → 不发暴击（PatternMultiplier = 0 意味着 0% 概率）
    /// - 暴击概率 = HeadSlot.PatternMultiplier（DataTable 里约定范围 0~1）
    ///   × (1 + Player.Passive.CritRateBonus)（被动加成乘区）
    /// - 暴击时伤害 = BaseDamage × CritMultiplier（Stats.CritMultiplier）
    ///
    /// 依赖注入：由 TattooModule.RegisterPartStrategies 传入 EventBus 与 TattooModule 自身引用。
    /// 实现 IDisposable；TattooModule.ShutdownAsync 负责调 Dispose 退订。
    ///
    /// 被动属性 ContributePassive 保持原实现（byte-for-byte 兼容现有 336 TC）。
    /// </summary>
    public sealed class HeadPartBehavior : IPartBehavior, IDisposable
    {
        public string PartName => "Head";

        readonly EventBus     _bus;
        readonly TattooModule _tattoo;
        IDisposable           _subscription;

        // 允许 bus/tattoo 为 null：单测仅校验 ContributePassive 时不需要订阅事件
        public HeadPartBehavior(EventBus bus = null, TattooModule tattoo = null)
        {
            _bus    = bus;
            _tattoo = tattoo;

            // 手动订阅 WeaponAttackHitEvent（HeadPartBehavior 不是 IGameModule，无法用 [EventHandler]）
            if (_bus != null)
                _subscription = _bus.Subscribe<WeaponAttackHitEvent>(OnWeaponAttackHit);
        }

        public void Dispose()
        {
            _subscription?.Dispose();
            _subscription = null;
        }

        // ===== 暴击逻辑 =====

        void OnWeaponAttackHit(WeaponAttackHitEvent e)
        {
            if (e?.Target == null) return;

            // 找当前装备的 Head 槽
            TattooSlot headSlot = default;
            bool found = false;
            var equipped = _tattoo.Equipped;
            for (int i = 0; i < equipped.Count; i++)
            {
                if (equipped[i].PartName == "Head")
                {
                    headSlot = equipped[i];
                    found = true;
                    break;
                }
            }

            if (!found) return; // 未装备 Head 纹身 → 不产生暴击

            // 暴击概率 = PatternMultiplier × (1 + 被动暴击率加成)
            float critProb = headSlot.PatternMultiplier * (1f + _tattoo.Player.Passive.CritRateBonus);
            if (UnityEngine.Random.value >= critProb) return; // 未命中概率 → 普通命中，无需重发

            // 暴击伤害 = BaseDamage × CritMultiplier（默认 1.5，由 DataTable 配置）
            float critMul  = _tattoo.Stats.CritMultiplier > 0f ? _tattoo.Stats.CritMultiplier : 1.5f;
            float critDmg  = e.BaseDamage * critMul;

            FrameworkLogger.Info("HeadPartBehavior",
                $"Action=CritTriggered Target={e.Target.Name} Prob={critProb:F2} CritMul={critMul:F2} Dmg={critDmg:F1}");

            _bus.Publish(new CritHitEvent(e.Target, critDmg));
        }

        // ===== IPartBehavior（其余 hook 头部无额外逻辑）=====

        public void PrepareContext(EffectContext ctx) { /* 目标已经是被暴击的敌人 */ }

        public bool InterceptApply(EffectContext ctx, IShapeBehavior shape, IElementBehavior element, float magnitude) => false;

        public void AffectSelf(PlayerState self, EffectContext ctx) { }

        public void ContributePassive(PassiveStats p, ElementType elem, float strength, string colorName, string patternName)
        {
            p.CritRateBonus += strength * 0.005f;
            p.AddElemBonus(elem, strength * 0.01f);
            p.Add($"Head × {colorName} × {patternName} : 暴击率+{strength * 0.5f:F1}% / {elem} 暴伤+{strength:F1}%");
        }
    }
}
