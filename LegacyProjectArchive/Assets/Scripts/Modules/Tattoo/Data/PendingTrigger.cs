using System;
using Tattoo.Strategies;

namespace Tattoo.Data
{
    /// <summary>延迟触发条目。例如左腿闪避把"下次普攻额外触发的形状+元素"打包到这里。</summary>
    public class PendingTrigger
    {
        /// <summary>消耗触发的事件类型（class 类型）。</summary>
        public Type ConsumeOnEventType;

        public IShapeBehavior   Shape;
        public IElementBehavior Element;
        public float            Magnitude;

        /// <summary>来源部位名，调试用。</summary>
        public string Source;

        /// <summary>-1 = 永久；&gt;=0 = 剩余可被消耗次数。</summary>
        public int ExpiresAfter = -1;
    }
}
