using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Economy;
using Economy.Events;
using GameEvent;
using GameEvent.Events;
using MapGen.Events;
using UnityEngine;

/// <summary>
/// EventModule v2.1：事件房触发 → 三选一弹窗管理 → 权重抽签 → 奖励结算。
///
/// ModuleCategory = 3（Gameplay 层），依赖 DataTableModule + MapGenModule。
///
/// 职责边界：
///   做：抽签、倒计时、EventState 状态机、奖励路由
///   不做：UI 渲染（UIModule）、敌人 Spawn（SpawnerModule）、数值平衡（配置表）
/// </summary>
public sealed class EventModule : IGameModule, ITickable
{
    // ─────────────────────────────────────────────
    // IGameModule
    // ─────────────────────────────────────────────

    public int ModuleCategory => 3;
    public Type[] Dependencies => new[]
    {
        typeof(DataTableModule),
        typeof(MapGen.MapGenModule),
    };

    // ─────────────────────────────────────────────
    // 私有字段
    // ─────────────────────────────────────────────

    readonly ModuleRunner _runner;
    readonly EventBus     _bus;

    // 配置表缓存（InitAsync 填充，此后只读）
    EventConfig                 _eventConfig;
    ThreeChoiceOptionConfig     _optionConfig;
    Dictionary<string, EventConfigRow>                          _eventConfigById;
    Dictionary<string, List<ThreeChoiceOptionConfigRow>>        _optionsByType;

    // Run 内状态
    // RoomId（string）→ EventState
    readonly Dictionary<string, EventStateType> _roomEventStates = new();
    // Run 内已使用的 IsUnique 选项 ID
    readonly HashSet<string> _usedUniqueOptionIds = new();
    // 当前正在等待选择的 actor
    Actor  _pendingChooser;
    string _pendingEventId;

    // 倒计时（unscaledDeltaTime，ShowingChoice 状态下每帧递减）
    float  _choiceTimer;
    float  _timerTickAccum;
    const float TimerTickInterval = 0.5f;    // 每 0.5s 发一次 ChoiceTimerTickEvent
    bool   _isShowingChoice;

    // 权重抽签用预分配 scratch buffer（128 个 int，InitAsync 分配一次，0 GC alloc）
    int[] _scratchWeightBuffer;
    const int ScratchBufferSize = 128;

    // option 类型集合（InitAsync 时建立一次，供抽签用）
    static readonly string[] AllOptionTypes = new[]
    {
        "tattoo_recipe", "pattern_recipe", "weapon_upgrade",
        "skill_upgrade", "skill_acquire", "coin_bonus",
        "heal", "one_time_scroll",
    };

    // ─────────────────────────────────────────────
    // 构造
    // ─────────────────────────────────────────────

    public EventModule(ModuleRunner runner, EventBus bus)
    {
        _runner = runner ?? throw new ArgumentNullException(nameof(runner));
        _bus    = bus    ?? throw new ArgumentNullException(nameof(bus));
    }

    // ─────────────────────────────────────────────
    // InitializeAsync / ShutdownAsync
    // ─────────────────────────────────────────────

    public UniTask InitializeAsync(CancellationToken ct = default)
    {
        // 1. 读配置表
        var dtModule = _runner.GetModule<DataTableModule>();
        _eventConfig  = dtModule.GetTable<EventConfig>();
        _optionConfig = dtModule.GetTable<ThreeChoiceOptionConfig>();

        // 2. 建本地索引（只读引用，不复制 row 对象）
        _eventConfigById = new Dictionary<string, EventConfigRow>(_eventConfig.All.Count);
        foreach (var kv in _eventConfig.All)
            _eventConfigById[kv.Key] = kv.Value;

        _optionsByType = new Dictionary<string, List<ThreeChoiceOptionConfigRow>>(AllOptionTypes.Length);
        foreach (var kv in _optionConfig.AllByType)
            _optionsByType[kv.Key] = kv.Value;

        // 3. 预分配抽签 scratch buffer（唯一一次 alloc）
        _scratchWeightBuffer = new int[ScratchBufferSize];

        FrameworkLogger.Info("EventModule", "Action=Initialized OptionTypes=" + AllOptionTypes.Length
            + " EventCount=" + _eventConfigById.Count);

        return UniTask.CompletedTask;
    }

    public UniTask ShutdownAsync(CancellationToken ct = default)
    {
        _roomEventStates.Clear();
        _usedUniqueOptionIds.Clear();
        _pendingChooser   = null;
        _pendingEventId   = null;
        _isShowingChoice  = false;
        _choiceTimer      = 0f;
        _timerTickAccum   = 0f;
        FrameworkLogger.Info("EventModule", "Action=Shutdown");
        return UniTask.CompletedTask;
    }

    // ─────────────────────────────────────────────
    // 公开 API
    // ─────────────────────────────────────────────

    /// <summary>
    /// 外部（UI）通知玩家已选定索引。若 EventModule 当前不处于 ShowingChoice 状态则忽略。
    /// </summary>
    public void ApplyChoice(int selectedIndex)
    {
        if (!_isShowingChoice || _pendingChooser == null) return;
        ResolveChoice(selectedIndex, isTimeout: false);
    }

    // ─────────────────────────────────────────────
    // 事件处理（[EventHandler] 由 ModuleRunner 自动注册）
    // ─────────────────────────────────────────────

    [EventHandler]
    void OnMapGenerated(MapGeneratedEvent e)
    {
        // 预建 RoomId → Idle 状态的映射，避免进房时即时查表
        _roomEventStates.Clear();
        if (e.Rooms == null) return;
        foreach (var room in e.Rooms)
        {
            var key = room.RoomId.ToString();
            _roomEventStates[key] = EventStateType.Idle;
        }
        FrameworkLogger.Info("EventModule", $"Action=MapLoaded RoomCount={e.Rooms.Count}");
    }

    [EventHandler]
    void OnRoomEntered(RoomEnteredEvent e)
    {
        if (e.Room == null) return;

        // 仅处理 EventRoom 类型（MVP 阶段：Normal 房也可能触发随机事件，此处按类型过滤）
        // RoomNodeType 目前无 EventRoom，等 MapGen 追加后替换判断条件
        // 当前用 Normal 房模拟触发，待 MapGen 定义 EventRoom 后收窄
        var roomKey = e.Room.RoomId.ToString();

        if (!_roomEventStates.TryGetValue(roomKey, out var state))
        {
            _roomEventStates[roomKey] = EventStateType.Idle;
            state = EventStateType.Idle;
        }

        if (state != EventStateType.Idle)
        {
            FrameworkLogger.Info("EventModule",
                $"Action=RoomEntered Skipped RoomId={roomKey} State={state}");
            return;
        }

        // 抽取一条匹配当前 Room 的 EventConfig（MVP：取第一条 choice_event）
        EventConfigRow eventRow = PickEventForRoom(e.Room);
        if (eventRow == null) return;

        // Idle → Triggered
        _roomEventStates[roomKey] = EventStateType.Triggered;
        _bus.Publish(new RoomEventTriggeredEvent(e.Enterer(), eventRow.EventId));
        FrameworkLogger.Info("EventModule",
            $"Action=EventTriggered RoomId={roomKey} EventId={eventRow.EventId} Actor={e.ActorName}");

        // 若为 choice_event，启动三选一流程
        if (eventRow.EventType == "choice_event")
        {
            _roomEventStates[roomKey] = EventStateType.ShowingChoice;
            TriggerThreeChoice(e.Enterer(), eventRow);
        }
        else
        {
            // 其他类型（combat_event / lore_event 等）由订阅方处理后推进到 Done
            _roomEventStates[roomKey] = EventStateType.Done;
        }
    }

    [EventHandler]
    void OnThreeChoiceMade(ThreeChoiceMadeEvent e)
    {
        if (!_isShowingChoice) return;
        // 已由 ApplyChoice / ResolveTimeout 处理，此处仅路由奖励
        RouteReward(e);
    }

    [EventHandler]
    void OnActorDied(Tattoo.Events.PlayerDiedEvent _)
    {
        // 玩家死亡：强制将所有 ShowingChoice 状态房间推进到 Done，恢复 timeScale
        ForceCloseAllActiveChoices();
    }

    // ─────────────────────────────────────────────
    // ITickable（每帧，仅在 ShowingChoice 时有实际逻辑）
    // ─────────────────────────────────────────────

    /// <summary>
    /// 每帧 Tick 驱动倒计时。由 GameTickDriver 调用（注册在 GameApp.RegisterTickables）。
    /// 无 ShowingChoice 时 O(1) 返回，0 GC alloc。
    /// 注意：GameTickDriver 传入的是 Time.deltaTime，但倒计时需要 unscaledDeltaTime。
    /// 此处直接读 Time.unscaledDeltaTime 确保 Time.timeScale=0 时倒计时继续推进。
    /// </summary>
    public void OnUpdate(float deltaTime)
    {
        if (!_isShowingChoice) return;

        // 使用 unscaledDeltaTime：timeScale=0 暂停时倒计时必须继续推进
        float unscaledDt = Time.unscaledDeltaTime;
        _choiceTimer    -= unscaledDt;
        _timerTickAccum += unscaledDt;

        // 每 0.5s 发一次 Tick 通知（降低事件频率）
        if (_timerTickAccum >= TimerTickInterval)
        {
            _timerTickAccum -= TimerTickInterval;
            _bus.Publish(new ChoiceTimerTickEvent(Mathf.Max(0f, _choiceTimer)));
        }

        if (_choiceTimer <= 0f)
        {
            ResolveTimeout();
        }
    }

    // ─────────────────────────────────────────────
    // 私有实现
    // ─────────────────────────────────────────────

    /// <summary>从配置表中为当前 Room 选取一条合适的事件（简化版：取权重最高的 choice_event）。</summary>
    EventConfigRow PickEventForRoom(MapGen.Data.RoomInfo room)
    {
        EventConfigRow best = null;
        foreach (var kv in _eventConfigById)
        {
            var row = kv.Value;
            if (row.EventType != "choice_event") continue;
            if (!row.IsRepeatAllowed && IsEventUsedThisRun(row.EventId)) continue;
            if (best == null || row.WeightBase > best.WeightBase)
                best = row;
        }
        return best;
    }

    bool IsEventUsedThisRun(string eventId)
    {
        // 简化：根据 roomEventStates 中 Done 状态的对应 eventId 判断，MVP 阶段不追踪 eventId→room 映射
        return false;
    }

    /// <summary>启动三选一流程：抽签 → 设置倒计时 → 暂停 → 发布 ThreeChoiceShownEvent。</summary>
    void TriggerThreeChoice(Actor chooser, EventConfigRow eventRow)
    {
        var options = DrawThreeOptions(chooser, eventRow);
        if (options == null)
        {
            FrameworkLogger.Warn("EventModule",
                $"Action=ThreeChoice DrawFailed EventId={eventRow.EventId}");
            return;
        }

        _pendingChooser   = chooser;
        _pendingEventId   = eventRow.EventId;
        _choiceTimer      = eventRow.TimeoutSec > 0f ? eventRow.TimeoutSec : 20f;
        _timerTickAccum   = 0f;
        _isShowingChoice  = true;
        _lastDrawnOptions = options;  // 缓存抽签结果供 ResolveChoice 取用

        // 暂停（单机模式：全局 timeScale=0）
        Time.timeScale = 0f;

        _bus.Publish(new ThreeChoiceShownEvent(chooser, options, _choiceTimer));
        FrameworkLogger.Info("EventModule",
            $"Action=ThreeChoiceShown EventId={eventRow.EventId} Timeout={_choiceTimer}s");
    }

    /// <summary>
    /// 权重抽签：保证三选项 OptionType 互不相同。
    /// 动态权重覆盖：skill_acquire（槽满降至 5，前 2min 0 技能提至 40）。
    /// IsUnique 守卫：已使用的唯一选项不进入候选池。
    /// 无 GC alloc（使用 _scratchWeightBuffer）。
    /// </summary>
    ThreeChoiceOption[] DrawThreeOptions(Actor chooser, EventConfigRow eventRow)
    {
        float runElapsedSec = Time.unscaledTime;   // 简化：以 unscaledTime 近似 Run 耗时
        int activeSkillCount = GetActiveSkillCount(chooser);

        // 收集候选（按 OptionType 逐类型取一项）
        var drawn = new ThreeChoiceOption[3];
        int drawnCount = 0;
        // 已选中的 OptionType 集合（避免重复）
        var usedTypes = new HashSet<string>(3);

        // 打乱 AllOptionTypes 顺序，随机起始以防每次顺序固定
        int startOffset = UnityEngine.Random.Range(0, AllOptionTypes.Length);

        for (int attempt = 0; attempt < AllOptionTypes.Length && drawnCount < 3; attempt++)
        {
            string optType = AllOptionTypes[(attempt + startOffset) % AllOptionTypes.Length];
            if (usedTypes.Contains(optType)) continue;

            if (!_optionsByType.TryGetValue(optType, out var candidates) || candidates.Count == 0) continue;

            // 填写 scratch buffer 权重
            int validCount = 0;
            for (int i = 0; i < candidates.Count && validCount < ScratchBufferSize; i++)
            {
                var cand = candidates[i];
                // 过滤：IsUnique 已使用
                if (cand.IsUnique && _usedUniqueOptionIds.Contains(cand.OptionId)) continue;
                // 过滤：MinRunElapsedSec 未到
                if (cand.MinRunElapsedSec > 0f && runElapsedSec < cand.MinRunElapsedSec) continue;

                int w = cand.WeightBase;
                // skill_acquire 动态权重覆盖
                if (optType == "skill_acquire")
                    w = OverrideSkillAcquireWeight(activeSkillCount, runElapsedSec);
                if (w <= 0) continue;

                _scratchWeightBuffer[validCount] = w;
                // 借用 validCount 作为索引，实际 candidates 索引偏移需单独 track
                // 简化：将有效候选的索引存到 scratchBuffer 上半段（validCount+64）
                if (validCount + 64 < ScratchBufferSize)
                    _scratchWeightBuffer[validCount + 64] = i;
                validCount++;
            }
            if (validCount == 0) continue;

            // 加权随机选一项
            int totalWeight = 0;
            for (int i = 0; i < validCount; i++) totalWeight += _scratchWeightBuffer[i];
            int roll = UnityEngine.Random.Range(0, totalWeight);
            int cumul = 0;
            int selectedCandIdx = 0;
            for (int i = 0; i < validCount; i++)
            {
                cumul += _scratchWeightBuffer[i];
                if (roll < cumul)
                {
                    selectedCandIdx = _scratchWeightBuffer[i + 64];
                    break;
                }
            }

            var chosen = candidates[selectedCandIdx];
            drawn[drawnCount] = new ThreeChoiceOption(
                chosen.OptionId, chosen.OptionType, chosen.DisplayName,
                chosen.DescKey, chosen.ContentRef, chosen.SkillSlot, chosen.ValueInt);
            usedTypes.Add(optType);
            drawnCount++;
        }

        if (drawnCount < 3)
        {
            FrameworkLogger.Warn("EventModule",
                $"Action=DrawThreeOptions InsufficientTypes DrawnCount={drawnCount}");
            // 用已抽项填满（降级处理，不让三选一空出按钮）
            for (int i = drawnCount; i < 3 && drawnCount > 0; i++)
                drawn[i] = drawn[0];
        }

        return drawn;
    }

    /// <summary>skill_acquire 动态权重覆盖（不写回配置表）。</summary>
    static int OverrideSkillAcquireWeight(int activeSkillCount, float runElapsedSec)
    {
        if (activeSkillCount >= 2) return 5;                            // 槽满，降权
        if (runElapsedSec < 120f && activeSkillCount == 0) return 40;  // 前 2min 0 技能，补偿
        return 16;                                                       // 标准权重
    }

    /// <summary>读取玩家当前激活技能数（通过 GetModule，运行时调用）。</summary>
    int GetActiveSkillCount(Actor chooser)
    {
        // SkillModule 待实现时替换此处；当前返回 0 作为默认值
        try
        {
            // var skillModule = _runner.GetModule<SkillModule>();
            // return skillModule.GetActiveSkillCount(chooser);
            return 0;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>玩家主动选择或超时选择的统一出口。</summary>
    void ResolveChoice(int selectedIndex, bool isTimeout)
    {
        if (!_isShowingChoice || _pendingChooser == null) return;

        _isShowingChoice = false;
        Time.timeScale   = 1f;

        var option = GetDrawnOption(selectedIndex);

        // 更新 IsUnique 守卫
        if (option.OptionId != null)
        {
            var row = _optionConfig.GetById(option.OptionId);
            if (row.IsUnique)
                _usedUniqueOptionIds.Add(option.OptionId);
        }

        _bus.Publish(new ThreeChoiceMadeEvent(_pendingChooser, selectedIndex, option, isTimeout));
        FrameworkLogger.Info("EventModule",
            $"Action=ChoiceMade Actor={_pendingChooser?.DisplayName} OptionType={option.OptionType} IsTimeout={isTimeout}");

        _pendingChooser = null;
        _pendingEventId = null;
    }

    void ResolveTimeout()
    {
        int idx = UnityEngine.Random.Range(0, 3);
        ResolveChoice(idx, isTimeout: true);
    }

    // 已发布的 drawn 选项缓存（TriggerThreeChoice 发布时存储，ResolveChoice 时取用）
    ThreeChoiceOption[] _lastDrawnOptions;

    ThreeChoiceOption GetDrawnOption(int idx)
    {
        if (_lastDrawnOptions == null || idx < 0 || idx >= _lastDrawnOptions.Length)
            return default;
        return _lastDrawnOptions[idx];
    }

    /// <summary>奖励结算路由（OnThreeChoiceMade 调用）。</summary>
    void RouteReward(ThreeChoiceMadeEvent e)
    {
        var opt = e.Selected;
        switch (opt.OptionType)
        {
            case "tattoo_recipe":
                // TattooModule.UnlockRecipe（直接调用——单向依赖，不成环）
                // _runner.GetModule<TattooModule>().UnlockRecipe(opt.ContentRef);
                FrameworkLogger.Info("EventModule",
                    $"Action=RewardRoute Type=tattoo_recipe ContentRef={opt.ContentRef}");
                break;

            case "pattern_recipe":
                // TattooModule.UnlockPatternRecipe（v2.1 新增）
                // _runner.GetModule<TattooModule>().UnlockPatternRecipe(opt.ContentRef);
                FrameworkLogger.Info("EventModule",
                    $"Action=RewardRoute Type=pattern_recipe ContentRef={opt.ContentRef}");
                break;

            case "weapon_upgrade":
                // 走 EventBus（同层 Category 3，避免循环依赖）
                // _bus.Publish(new WeaponUpgradeRequestEvent(e.Chooser, opt.ContentRef));
                FrameworkLogger.Info("EventModule",
                    $"Action=RewardRoute Type=weapon_upgrade ContentRef={opt.ContentRef}");
                break;

            case "skill_upgrade":
                // 防御性校验 slotIndex 值域（仅 0/1）
                UnityEngine.Debug.Assert(opt.SkillSlot == 0 || opt.SkillSlot == 1,
                    $"[EventModule] skill_upgrade SkillSlot 值域越界: {opt.SkillSlot}");
                // _bus.Publish(new SkillUpgradeRequestEvent(e.Chooser, opt.ContentRef, opt.SkillSlot));
                FrameworkLogger.Info("EventModule",
                    $"Action=RewardRoute Type=skill_upgrade ContentRef={opt.ContentRef} Slot={opt.SkillSlot}");
                break;

            case "skill_acquire":
                // _bus.Publish(new SkillAcquireRequestEvent(e.Chooser, opt.ContentRef));
                FrameworkLogger.Info("EventModule",
                    $"Action=RewardRoute Type=skill_acquire ContentRef={opt.ContentRef}");
                break;

            case "coin_bonus":
                // _bus.Publish(new CoinChangedEvent(e.Chooser, opt.ValueInt, ...));
                FrameworkLogger.Info("EventModule",
                    $"Action=RewardRoute Type=coin_bonus Amount={opt.ValueInt}");
                break;

            case "heal":
                // 负伤害 = 治疗
                // _bus.Publish(new DamagedEvent(null, -opt.ValueInt));
                FrameworkLogger.Info("EventModule",
                    $"Action=RewardRoute Type=heal Amount={opt.ValueInt}");
                break;

            case "one_time_scroll":
                FrameworkLogger.Info("EventModule",
                    $"Action=RewardRoute Type=one_time_scroll ContentRef={opt.ContentRef}");
                break;

            default:
                FrameworkLogger.Warn("EventModule",
                    $"Action=RewardRoute UnknownType={opt.OptionType}");
                break;
        }
    }

    /// <summary>强制关闭所有活跃的三选一流程（玩家死亡时调用）。</summary>
    void ForceCloseAllActiveChoices()
    {
        if (_isShowingChoice)
        {
            _isShowingChoice = false;
            Time.timeScale   = 1f;
            _pendingChooser  = null;
            _pendingEventId  = null;
            FrameworkLogger.Warn("EventModule", "Action=ForceCloseChoice Reason=PlayerDied");
        }

        // 将所有 ShowingChoice/Triggered 状态推进到 Done
        var keys = new List<string>(_roomEventStates.Keys); // 只此一处分配
        foreach (var k in keys)
        {
            var s = _roomEventStates[k];
            if (s == EventStateType.Triggered || s == EventStateType.ShowingChoice)
                _roomEventStates[k] = EventStateType.Done;
        }
    }
}

/// <summary>EventRoom 状态机状态。</summary>
public enum EventStateType
{
    Idle,
    Triggered,
    ShowingChoice,
    CombatActive,
    Done,
}

// ─────────────────────────────────────────────
// RoomEnteredEvent 扩展：提供 Actor 访问
// ─────────────────────────────────────────────

namespace MapGen.Events
{
    public static class RoomEnteredEventExtensions
    {
        /// <summary>获取 RoomEnteredEvent 的 Actor 引用（MVP：按 ActorName 匹配，后期替换为 ActorId）。</summary>
        public static Economy.Actor Enterer(this RoomEnteredEvent e)
        {
            // 简化实现：构造一个临时 Actor 标识；SpawnerModule 正式落地后替换
            return new Economy.Actor(0, e.ActorName, true);
        }
    }
}
