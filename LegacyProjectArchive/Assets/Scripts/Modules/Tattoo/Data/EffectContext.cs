using System;
using System.Collections.Generic;

namespace Tattoo.Data
{
    /// <summary>一次"事件→策略"调度的上下文。由 TattooModule.Fire 构造并填充。</summary>
    public class EffectContext
    {
        /// <summary>当前触发的事件类型（class 类型，例如 typeof(AttackHitEvent)）。</summary>
        public Type EventType;

        public PlayerStats  Stats;
        public PlayerState  Self;

        public Target        PrimaryTarget;
        public List<Target>  NearbyTargets = new();

        /// <summary>躯干（Torso）需要把它设为 PrimaryTarget——刚刚攻击你的人。</summary>
        public Target        LastAttacker;

        /// <summary>右腿（RightLeg）需要：移动路径上的敌人。</summary>
        public List<Target>  MovementPath = new();

        /// <summary>本次调度累积的所有效果结果。Fire 完成后通过 EffectAppliedEvent 广播。</summary>
        public List<EffectResult> Log = new();
    }
}
