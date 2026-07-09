using System;
using System.Collections.Generic;
using Newtonsoft.Json;

// ══════════════════════════════════════════════════════════════════
// SaveData v2.1 — 玩家持久化数据结构
//
// 版本规则：
//   CURRENT_VERSION 只增不减；每次结构变更必须在 SaveMigrator 添加对应迁移函数；
//   旧迁移函数永远不删。
// ══════════════════════════════════════════════════════════════════

/// <summary>
/// 玩家全局存档数据（局外持久化）。
/// 不保存局内实体状态——死亡即全新 Run。
/// </summary>
[Serializable]
public sealed class SaveData
{
    public const int CURRENT_VERSION = 2;

    /// <summary>存档版本号，用于迁移判定。</summary>
    public int Version = CURRENT_VERSION;

    // ── 局外解锁：角色 ─────────────────────────────────────────────
    /// <summary>
    /// 9 个角色槽位解锁状态。
    /// 本期 MVP 仅 slot[0] 对应唯一角色（默认 true），其余默认 false。
    /// </summary>
    public bool[] CharacterSlots = new bool[9];

    // ── 局外解锁：图案配方 ─────────────────────────────────────────
    /// <summary>
    /// key = patternId（字符串），value = bool[6]（6 个解锁位）。
    /// 6 位含义由 TattooModule 约定，SaveModule 只负责存储，不解释语义。
    /// </summary>
    [JsonProperty]
    public Dictionary<string, bool[]> PatternUnlocks = new();

    // ── 局外解锁：装饰 / 衔号 / 画廊 ──────────────────────────────
    /// <summary>已解锁的装饰 ID 集合。</summary>
    [JsonProperty]
    public HashSet<string> UnlockedDecorations = new();
    /// <summary>已解锁的衔号 ID 集合。</summary>
    [JsonProperty]
    public HashSet<string> UnlockedTitles = new();
    /// <summary>已解锁的画廊条目 ID 集合。</summary>
    [JsonProperty]
    public HashSet<string> UnlockedGallery = new();

    // ── 成就 ────────────────────────────────────────────────────────
    /// <summary>已完成的成就 ID 列表（MVP 阶段不单独建模块）。</summary>
    [JsonProperty]
    public List<string> CompletedAchievements = new();

    // ── 统计数据 ────────────────────────────────────────────────────
    /// <summary>总 Run 次数（无论胜负）。</summary>
    public int TotalRuns;
    /// <summary>累计击杀数。</summary>
    public int TotalKills;
    /// <summary>
    /// 纹身配方解锁进度。
    /// key = "partId_colorId_patternId"，value = 解锁次数。
    /// </summary>
    [JsonProperty]
    public Dictionary<string, int> TattooRecipeProgress = new();

    // ── 设置 ────────────────────────────────────────────────────────
    public SettingsData Settings = new();

    // ── 元信息（云存档冲突判定三件套）──────────────────────────────
    /// <summary>最后写盘时间（UTC ISO 8601）。</summary>
    public string LastModifiedUtc = string.Empty;
    /// <summary>写盘设备 ID（`SystemInfo.deviceUniqueIdentifier`）。</summary>
    public string DeviceId = string.Empty;
    /// <summary>总游戏时长（秒）。</summary>
    public float TotalPlayTime;
}

/// <summary>玩家设置数据。</summary>
[Serializable]
public sealed class SettingsData
{
    public float MasterVolume = 1f;
    public float MusicVolume  = 0.8f;
    public float SfxVolume    = 1f;
    /// <summary>对应 Unity QualitySettings.names 索引（2 = Medium）。</summary>
    public int   QualityLevel = 2;
    /// <summary>
    /// 键位重绑定。key = actionName，value = InputSystem BindingOverride JSON。
    /// 读取时调用 `action.ApplyBindingOverridesFromJson(json)`。
    /// </summary>
    [JsonProperty]
    public Dictionary<string, string> KeyBindings = new();
}

/// <summary>
/// 单局结束统计（RunEndedEvent.Stats 的 payload）。
/// 同时作为 SaveModule.AddRunStats 的参数。
/// </summary>
[Serializable]
public sealed class RunStats
{
    /// <summary>本局持续时间（秒）。</summary>
    public float DurationSec;
    /// <summary>本局击杀数。</summary>
    public int   Kills;
    /// <summary>是否获胜。</summary>
    public bool  Win;
}
