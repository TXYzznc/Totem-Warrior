using System.Collections.Generic;
using Tattoo.Strategies;

namespace Tattoo.Data
{
    /// <summary>战斗中的"目标"——可以是敌人/友军/自己。承载血量、状态、印记。</summary>
    public class Target
    {
        public string Name = "目标";
        public float  Health = 100f;

        public List<string> Statuses = new();

        /// <summary>StackingMark 印记表：按 (Shape 实例, 部位名) 做联合键。</summary>
        public Dictionary<(IShapeBehavior Shape, string Part), int> Marks = new();
    }
}
