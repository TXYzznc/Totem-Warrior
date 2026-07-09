using System.Collections.Generic;
using UnityEngine;

namespace Economy
{
    /// <summary>
    /// 局内 Actor 身份标识。玩家 + 每个 Bot 各持有一个实例，贯穿整局生命周期。
    /// InstanceId 从 1 开始自增，局结束时重置（由 EconomyModule.ResetIds 调用）。
    ///
    /// 注意：Actor 是轻量数据对象，不挂载到 GameObject——通过 EntityRef 关联。
    /// </summary>
    public sealed class Actor
    {
        // ───── 身份 ─────

        /// <summary>局内唯一实例 ID（从 1 开始）。EconomyModule 用作库存 Dictionary 键。</summary>
        public int InstanceId { get; }

        /// <summary>显示名称（玩家昵称 / Bot 编号）。</summary>
        public string DisplayName { get; }

        /// <summary>是否为玩家（false = Bot）。</summary>
        public bool IsPlayer { get; }

        // ───── 战斗代理（可选，由 SpawnerModule 注入）─────

        /// <summary>
        /// 关联的 Target（血量/状态容器）。
        /// SpawnerModule 创建实体后注入。EconomyModule 不直接读取，仅 CombatModule 使用。
        /// </summary>
        public Tattoo.Data.Target Target { get; set; }

        /// <summary>
        /// 关联的场景 GameObject。
        /// SpawnerModule 创建实体后注入，用于死亡宝箱坐标读取。
        /// </summary>
        public GameObject GameObject { get; set; }

        // ───── change #18 武器升级 ─────

        /// <summary>
        /// 武器等级字典（key = WeaponId, value = 当前等级 1~3）。
        /// 默认不含条目 = 等级视为 1（lazy init，由 WeaponUpgradeModule.GetWeaponLevel 处理）。
        /// 仅在 IsPlayer == true 时使用，其他 Actor 字典保持空。
        /// 设计：design.md §2.1；spec.md §3。
        /// </summary>
        public Dictionary<string, int> WeaponLevels { get; set; } = new();

        // ───── 构造 ─────

        public Actor(int instanceId, string displayName, bool isPlayer)
        {
            InstanceId  = instanceId;
            DisplayName = displayName;
            IsPlayer    = isPlayer;
        }

        public override string ToString() => $"Actor(Id={InstanceId}, Name={DisplayName}, IsPlayer={IsPlayer})";
    }
}
