using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

/// <summary>
/// 设置模块（v1.0）。
///
/// 职责：
///   - 启动时从 SaveModule 读已持久化设置 → ApplyAll()（不发事件，符合 InitAsync 约束）
///   - 提供三态 BeginEdit / Preview / Commit / Rollback 接口（即时生效 + 取消回滚）
///   - Commit 后写盘并发布 SettingsAppliedEvent
///   - 通过 SaveModule 统一存档
///
/// 范围（v1.0）：
///   - ✅ BGM / SFX 音量（走 AudioModule）
///   - ✅ 画质档位（QualitySettings.SetQualityLevel）
///   - ⏸ 按键重绑定（数据字段保留，但本期 UI 仅展示，不允许修改；后续 InputModule 升级到 New Input System 后再接通）
///
/// ModuleCategory = 1（系统服务层）
/// Dependencies = [AudioModule, SaveModule]
/// </summary>
public sealed class SettingsModule : IGameModule
{
    public int ModuleCategory => 1;
    public Type[] Dependencies => new[] { typeof(AudioModule), typeof(SaveModule) };

    enum State { Idle, Editing }
    State _state = State.Idle;

    readonly ModuleRunner _runner;
    readonly EventBus     _bus;

    SettingsData _current;
    SettingsData _snapshot;

    public SettingsModule(ModuleRunner runner, EventBus bus)
    {
        _runner = runner ?? throw new ArgumentNullException(nameof(runner));
        _bus    = bus    ?? throw new ArgumentNullException(nameof(bus));
    }

    public UniTask InitializeAsync(CancellationToken ct = default)
    {
        var saveModule = _runner.GetModule<SaveModule>();
        _current = saveModule?.Data?.Settings ?? new SettingsData();
        ApplyAll(_current);

        FrameworkLogger.Info("SettingsModule",
            $"Action=Initialized BgmVolume={_current.MusicVolume:F2} SfxVolume={_current.SfxVolume:F2} QualityLevel={_current.QualityLevel}");
        return UniTask.CompletedTask;
    }

    public UniTask ShutdownAsync(CancellationToken ct = default)
    {
        FrameworkLogger.Info("SettingsModule", "Action=Shutdown");
        return UniTask.CompletedTask;
    }

    public SettingsData GetCurrent() => Clone(_current);

    public void BeginEdit()
    {
        if (_state == State.Editing)
        {
            FrameworkLogger.Warn("SettingsModule", "Action=BeginEdit State=AlreadyEditing 跳过");
            return;
        }
        _snapshot = Clone(_current);
        _state    = State.Editing;
        FrameworkLogger.Info("SettingsModule", "Action=BeginEdit State=Editing");
    }

    public void Preview(SettingsData draft)
    {
        if (_state != State.Editing)
        {
            FrameworkLogger.Warn("SettingsModule", "Action=Preview State=NotEditing 忽略");
            return;
        }
        if (draft == null) return;
        _current = Clone(draft);
        ApplyAll(_current);
    }

    public void Commit()
    {
        if (_state != State.Editing)
        {
            FrameworkLogger.Warn("SettingsModule", "Action=Commit State=NotEditing 忽略");
            return;
        }
        _snapshot = null;
        _state    = State.Idle;

        WriteAsync().Forget();
        _bus.Publish(new SettingsAppliedEvent(Clone(_current)));
        FrameworkLogger.Info("SettingsModule", "Action=Commit State=Idle");
    }

    public void Rollback()
    {
        if (_state != State.Editing)
        {
            FrameworkLogger.Warn("SettingsModule", "Action=Rollback State=NotEditing 忽略");
            return;
        }
        if (_snapshot != null)
        {
            _current = _snapshot;
            ApplyAll(_current);
        }
        _snapshot = null;
        _state    = State.Idle;
        FrameworkLogger.Info("SettingsModule", "Action=Rollback State=Idle 恢复快照");
    }

    void ApplyAll(SettingsData data)
    {
        ApplyVolume(data);
        ApplyQuality(data);
    }

    void ApplyVolume(SettingsData data)
    {
        var audio = _runner.GetModule<AudioModule>();
        if (audio != null)
        {
            audio.SetBgmVolume(data.MusicVolume);
            audio.SetSfxVolume(data.SfxVolume);
        }
        FrameworkLogger.Info("SettingsModule",
            $"Action=ApplyVolume BgmVolume={data.MusicVolume:F2} SfxVolume={data.SfxVolume:F2}");
    }

    void ApplyQuality(SettingsData data)
    {
        int level = Mathf.Clamp(data.QualityLevel, 0, QualitySettings.names.Length - 1);
        QualitySettings.SetQualityLevel(level, applyExpensiveChanges: true);
        FrameworkLogger.Info("SettingsModule",
            $"Action=ApplyQuality Level={level} QualityName={QualitySettings.names[level]}");
    }

    async UniTaskVoid WriteAsync()
    {
        try
        {
            var saveModule = _runner.GetModule<SaveModule>();
            if (saveModule == null)
            {
                FrameworkLogger.Warn("SettingsModule", "Action=Write SaveModule=null 跳过写盘");
                return;
            }
            saveModule.SetSettings(_current);
            await saveModule.SaveAsync();
            FrameworkLogger.Info("SettingsModule", "Action=Written 写盘成功");
        }
        catch (Exception ex)
        {
            FrameworkLogger.Warn("SettingsModule",
                $"Action=WriteFailed Exception={ex.GetType().Name} Msg=\"{ex.Message}\"");
        }
    }

    static SettingsData Clone(SettingsData src)
    {
        if (src == null) return new SettingsData();
        var json   = JsonConvert.SerializeObject(src);
        var cloned = JsonConvert.DeserializeObject<SettingsData>(json) ?? new SettingsData();
        return cloned;
    }
}
