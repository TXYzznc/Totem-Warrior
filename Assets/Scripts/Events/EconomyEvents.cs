using UnityEngine;

// 顶层无 namespace（与项目 WeaponPickupEvents / TattooEvents 风格一致）
// 由 WeaponSpawnerModule 18-A 新增，供 EconomyModule 后续接入。

/// <summary>
/// 宝箱/商人给予金币时发布。
/// 发布方：WeaponSpawnerModule.OnChestOpened（RewardType="Gold"）
/// 订阅方：EconomyModule（后续接入，调用 AddCoin）/ HUD 飘字
/// </summary>
public sealed class EconomyAddGoldEvent
{
    /// <summary>金币数量（正整数）。</summary>
    public int Amount;
    /// <summary>来源世界坐标（宝箱/商人位置，用于飘字特效定位）。</summary>
    public Vector3 SourcePosition;
}
