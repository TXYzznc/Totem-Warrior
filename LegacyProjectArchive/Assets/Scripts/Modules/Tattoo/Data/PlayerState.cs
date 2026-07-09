using System.Collections.Generic;

namespace Tattoo.Data
{
    /// <summary>战斗中玩家自身状态。包含 Buff/叠层/冷却/被动/PendingTrigger。</summary>
    public class PlayerState
    {
        public string Name = "玩家";
        public float  Health = 100f;

        public List<string>              Buffs     = new();
        public Dictionary<string, int>   Stacks    = new();
        public Dictionary<string, float> Cooldowns = new();

        public List<PendingTrigger> PendingTriggers = new();
        public PassiveStats         Passive         = new();

        /// <summary>左腿闪避瞬间的短暂无敌标记，由战斗逻辑读取后归零。</summary>
        public bool ShortInvincible = false;
    }
}
