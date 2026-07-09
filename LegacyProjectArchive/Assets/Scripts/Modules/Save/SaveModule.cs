using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// SaveModule v2.1：玩家持久化数据管理（局外解锁 / 设置 / 统计）。
///
/// ModuleCategory = 1（系统服务层），Dependencies = []（最早就绪，无依赖）。
///
/// 职责：
///   存：局外解锁（角色 / 图案配方 / 装饰 / 衔号 / 画廊）、设置、统计。
///   不存：局内实体状态（血量 / Build / 地图进度）——死亡 = 全新 Run。
///
/// 写盘：ISaveProvider 抽象 I/O（本期 LocalFileSaveProvider）。
///   LocalFileSaveProvider 走原子重命名（.tmp → .json，旧 .json → .bak）。
/// </summary>
public sealed class SaveModule : IGameModule
{
    // ─────────────────────────────────────────────
    // IGameModule
    // ─────────────────────────────────────────────

    public int    ModuleCategory => 1;
    public Type[] Dependencies   => Type.EmptyTypes;

    // ─────────────────────────────────────────────
    // 私有字段
    // ─────────────────────────────────────────────

    readonly ModuleRunner  _runner;
    readonly EventBus      _bus;
    readonly ISaveProvider _provider;

    SaveData _data;
    bool     _dirty;

    // ─────────────────────────────────────────────
    // 公开属性
    // ─────────────────────────────────────────────

    /// <summary>当前存档快照（只读引用，不得外部修改）。</summary>
    public SaveData Data => _data;

    // ─────────────────────────────────────────────
    // 构造
    // ─────────────────────────────────────────────

    /// <summary>
    /// 主构造（使用默认 LocalFileSaveProvider）。
    /// </summary>
    public SaveModule(ModuleRunner runner, EventBus bus)
        : this(runner, bus, new LocalFileSaveProvider()) { }

    /// <summary>
    /// 可注入 Provider 的构造（测试 / 扩展用）。
    /// </summary>
    public SaveModule(ModuleRunner runner, EventBus bus, ISaveProvider provider)
    {
        _runner   = runner   ?? throw new ArgumentNullException(nameof(runner));
        _bus      = bus      ?? throw new ArgumentNullException(nameof(bus));
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    }

    // ─────────────────────────────────────────────
    // InitializeAsync / ShutdownAsync
    // ─────────────────────────────────────────────

    public async UniTask InitializeAsync(CancellationToken ct = default)
    {
        // 读盘（本地 < 1ms，但保持 async 接口一致性）
        string json = await _provider.LoadJsonAsync(ct);

        // 迁移 & 反序列化
        _data  = SaveMigrator.Load(json);
        _dirty = false;

        FrameworkLogger.Info("SaveModule",
            $"Action=Loaded Version={_data.Version} TotalRuns={_data.TotalRuns} TotalKills={_data.TotalKills}");

        // InitializeAsync 完成后发布（不在 InitializeAsync 内部发送事件）
        // ModuleRunner 在 InitializeAsync 返回后自动扫描 [EventHandler]
        // SaveLoadedEvent 在此处 Publish，其他模块的 [EventHandler] 已注册就绪
        _bus.Publish(new SaveLoadedEvent(_data));
    }

    public async UniTask ShutdownAsync(CancellationToken ct = default)
    {
        // 关闭时若有脏数据，强制写盘一次
        if (_dirty)
        {
            await WriteInternalAsync(ct);
        }
        FrameworkLogger.Info("SaveModule", "Action=Shutdown");
    }

    // ─────────────────────────────────────────────
    // 公开 API
    // ─────────────────────────────────────────────

    /// <summary>手动触发写盘（也可依赖内部自动调用）。</summary>
    public async UniTask SaveAsync()
    {
        await WriteInternalAsync();
    }

    /// <summary>设置角色槽位解锁状态。</summary>
    public void SetCharacterUnlocked(int slot, bool unlocked)
    {
        if (slot < 0 || slot >= _data.CharacterSlots.Length)
        {
            FrameworkLogger.Warn("SaveModule",
                $"Action=SetCharacterUnlocked InvalidSlot={slot}");
            return;
        }
        if (_data.CharacterSlots[slot] == unlocked) return;
        _data.CharacterSlots[slot] = unlocked;
        _dirty = true;
    }

    /// <summary>
    /// 设置图案配方解锁位。
    /// patternId = TattooModule 约定的图案 ID；bitIndex = 0..5。
    /// </summary>
    public void SetPatternUnlocked(string patternId, int bitIndex, bool unlocked)
    {
        if (string.IsNullOrEmpty(patternId) || bitIndex < 0 || bitIndex > 5)
        {
            FrameworkLogger.Warn("SaveModule",
                $"Action=SetPatternUnlocked InvalidArgs patternId={patternId} bitIndex={bitIndex}");
            return;
        }
        if (!_data.PatternUnlocks.TryGetValue(patternId, out var bits))
        {
            bits = new bool[6];
            _data.PatternUnlocks[patternId] = bits;
        }
        if (bits[bitIndex] == unlocked) return;
        bits[bitIndex] = unlocked;
        _dirty = true;
    }

    /// <summary>解锁装饰。已解锁时幂等。</summary>
    public void SetDecorationUnlocked(string decorationId, bool unlocked)
    {
        if (string.IsNullOrEmpty(decorationId)) return;
        bool changed = unlocked
            ? _data.UnlockedDecorations.Add(decorationId)
            : _data.UnlockedDecorations.Remove(decorationId);
        if (changed) _dirty = true;
    }

    /// <summary>解锁衔号。已解锁时幂等。</summary>
    public void SetTitleUnlocked(string titleId, bool unlocked)
    {
        if (string.IsNullOrEmpty(titleId)) return;
        bool changed = unlocked
            ? _data.UnlockedTitles.Add(titleId)
            : _data.UnlockedTitles.Remove(titleId);
        if (changed) _dirty = true;
    }

    /// <summary>解锁画廊条目。已解锁时幂等。</summary>
    public void SetGalleryUnlocked(string galleryId, bool unlocked)
    {
        if (string.IsNullOrEmpty(galleryId)) return;
        bool changed = unlocked
            ? _data.UnlockedGallery.Add(galleryId)
            : _data.UnlockedGallery.Remove(galleryId);
        if (changed) _dirty = true;
    }

    /// <summary>
    /// 累计 Run 统计数据（RunEndedEvent 订阅时内部调用，也可外部调用）。
    /// 累计后立即触发写盘。
    /// </summary>
    public void AddRunStats(RunStats stats)
    {
        if (stats == null) return;
        _data.TotalRuns++;
        _data.TotalKills += stats.Kills;
        _data.TotalPlayTime += stats.DurationSec;
        _dirty = true;
    }

    /// <summary>更新设置数据并标记脏。</summary>
    public void SetSettings(SettingsData settings)
    {
        if (settings == null) return;
        _data.Settings = settings;
        _dirty = true;
    }

    // ─────────────────────────────────────────────
    // 事件处理（[EventHandler] 由 ModuleRunner 自动注册）
    // ─────────────────────────────────────────────

    [EventHandler]
    void OnRunEnded(RunEndedEvent e)
    {
        AddRunStats(e?.Stats);
        if (_dirty)
        {
            // 不在 Update 路径，UniTask.Void 防止 async void 异常丢失
            WriteInternalAsync().Forget();
        }
    }

    [EventHandler]
    void OnAchievementUnlocked(AchievementUnlockedEvent e)
    {
        if (e == null || string.IsNullOrEmpty(e.AchievementId)) return;
        if (_data.CompletedAchievements.Contains(e.AchievementId)) return;
        _data.CompletedAchievements.Add(e.AchievementId);
        _dirty = true;
    }

    // ─────────────────────────────────────────────
    // 私有写盘逻辑
    // ─────────────────────────────────────────────

    async UniTask WriteInternalAsync(CancellationToken ct = default)
    {
        // 更新元信息
        _data.LastModifiedUtc = DateTime.UtcNow.ToString("O");
        _data.DeviceId        = UnityEngine.SystemInfo.deviceUniqueIdentifier;

        string json    = SaveMigrator.Serialize(_data);
        bool   success = false;
        string path    = (_provider is LocalFileSaveProvider lf) ? lf.MainPath : "unknown";

        try
        {
            await _provider.WriteJsonAsync(json, ct);
            success = true;
            _dirty  = false;
            FrameworkLogger.Info("SaveModule",
                $"Action=Written TotalRuns={_data.TotalRuns} Path={path}");
        }
        catch (Exception ex)
        {
            FrameworkLogger.Error("SaveModule",
                $"Action=WriteFailed Exception={ex.GetType().Name} Msg=\"{ex.Message}\"");
        }

        _bus.Publish(new SaveWrittenEvent(success, path));
    }
}
