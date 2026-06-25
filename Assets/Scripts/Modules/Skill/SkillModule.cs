using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Economy.Events;
using Skill.Events;
using Tattoo;
using Tattoo.Data;
using Tattoo.Events;

/// <summary>
/// 技能模块 v2.1。
/// 职责：
/// - 维护 50 actor × 2 槽位的运行时冷却 / 充能 / 蓄力状态
/// - 消费 CombatModule 发的 SkillCastEvent（SkillId = "slot0"/"slot1"）→
///   解析 slot 索引 → 校验就绪 → 帧相位推进 → 在 Active 帧第 1 帧发 SkillActivatedEvent
/// - 响应 ItemPickedEvent（道具 ItemId 命中 SkillConfig 时装槽）
/// - 逐帧 Tick：无 GC alloc
/// </summary>
public sealed class SkillModule : IGameModule, ITickable
{
    // ===== IGameModule =====
    public int    ModuleCategory => 3;
    public Type[] Dependencies   => new[] { typeof(DataTableModule), typeof(WeaponModule) };

    // ===== 私有字段 =====
    readonly ModuleRunner _runner;
    readonly EventBus     _bus;

    WeaponModule _weapon;

    /// <summary>技能配置索引：SkillId → Row</summary>
    readonly Dictionary<string, SkillConfigRow> _configById = new(16);

    /// <summary>ItemId → SkillId 反查索引（仅 ItemId > 0 的行）</summary>
    readonly Dictionary<int, string> _itemIdToSkillId = new(16);

    /// <summary>per-actor 运行时状态</summary>
    readonly Dictionary<Target, SkillActorState> _states = new(64);

    // ===== 帧相位枚举 =====
    enum SkillPhase { Idle, Startup, Active, Recovery }

    // ===== per-actor 状态（预分配，零 GC）=====
    sealed class SkillActorState
    {
        /// <summary>2 槽当前装备 SkillId，null 表示空</summary>
        public readonly string[] SlotToSkillId    = new string[2];
        public readonly SkillPhase[] Phases        = new SkillPhase[2];
        /// <summary>冷却剩余 (s)，ChargeModel=0 用</summary>
        public readonly float[]  CooldownRemaining = new float[2];
        /// <summary>当前充能层数，ChargeModel=1 用</summary>
        public readonly int[]    CurrentCharges    = new int[2];
        /// <summary>单层充能恢复计时 (s)，ChargeModel=1 用</summary>
        public readonly float[]  ChargeRegenElapsed= new float[2];
        /// <summary>蓄力已持续时长 (s)，ChargeModel=2 用</summary>
        public readonly float[]  HoldElapsed       = new float[2];
        /// <summary>当前帧相位（Startup/Active/Recovery）内已耗时 (s)</summary>
        public readonly float[]  PhaseElapsed      = new float[2];
        /// <summary>目标缓存：Active 帧开始时锁定，用于 SkillActivatedEvent</summary>
        public readonly Target[] AimTarget         = new Target[2];
        /// <summary>是否在本 Tick 刚刚进入 Active（防重复发事件）</summary>
        public readonly bool[]   ActiveFired       = new bool[2];
    }

    // ===== 构造 =====
    public SkillModule(ModuleRunner runner, EventBus bus)
    {
        _runner = runner ?? throw new ArgumentNullException(nameof(runner));
        _bus    = bus    ?? throw new ArgumentNullException(nameof(bus));
    }

    // ===== InitializeAsync =====
    public UniTask InitializeAsync(CancellationToken ct = default)
    {
        _weapon = _runner.GetModule<WeaponModule>();

        // 读 SkillConfig 表，建立索引
        var table = _runner.GetModule<DataTableModule>().GetTable<SkillConfig>();
        foreach (var kv in table.All)
        {
            var row = kv.Value;
            _configById[row.SkillId] = row;
            if (row.ItemId > 0)
                _itemIdToSkillId[row.ItemId] = row.SkillId;
        }

        FrameworkLogger.Info("SkillModule",
            $"Action=Initialized SkillCount={_configById.Count} ItemMappings={_itemIdToSkillId.Count}");
        return UniTask.CompletedTask;
    }

    public UniTask ShutdownAsync(CancellationToken ct = default)
    {
        _states.Clear();
        FrameworkLogger.Info("SkillModule", "Action=Shutdown");
        return UniTask.CompletedTask;
    }

    // ===== 公开 API =====

    /// <summary>
    /// 为指定 actor 装备技能。EquipSkill 是内部 + 事件触发的统一入口。
    /// 外部（NPCModule / 拾取系统）通过 ItemPickedEvent / ShopPurchaseEvent 驱动，不直接调用。
    /// </summary>
    public void EquipSkill(Target actor, int slot, string skillId)
    {
        if (slot < 0 || slot >= 2)
        {
            FrameworkLogger.Warn("SkillModule",
                $"Action=EquipSkill SlotIndex={slot} out of range [0,1], ignored");
            return;
        }
        var state = GetOrCreateState(actor);
        // 若旧槽有技能，先发 IsEquipped=false
        var old = state.SlotToSkillId[slot];
        if (old != null)
        {
            _bus.Publish(new SkillSlotChangedEvent(actor, slot, old, false));
            FrameworkLogger.Info("SkillModule",
                $"Action=SkillUnequipped Actor={actor.Name} Slot={slot} OldSkill={old}");
        }
        state.SlotToSkillId[slot]     = skillId;
        state.Phases[slot]             = SkillPhase.Idle;
        state.CooldownRemaining[slot]  = 0f;
        // 充能模型初始满充
        if (_configById.TryGetValue(skillId, out var row) && row.ChargeModel == 1)
            state.CurrentCharges[slot] = row.MaxCharges;

        _bus.Publish(new SkillSlotChangedEvent(actor, slot, skillId, true));
        FrameworkLogger.Info("SkillModule",
            $"Action=SkillEquipped Actor={actor.Name} Slot={slot} SkillId={skillId}");
    }

    /// <summary>查询槽位冷却剩余秒数（UI 用）。</summary>
    public float GetCooldownRemaining(Target actor, int slot)
    {
        if (slot < 0 || slot >= 2) return 0f;
        if (!_states.TryGetValue(actor, out var s)) return 0f;
        return s.CooldownRemaining[slot];
    }

    /// <summary>查询槽位当前充能层数（UI 用）。</summary>
    public int GetCurrentCharges(Target actor, int slot)
    {
        if (slot < 0 || slot >= 2) return 0;
        if (!_states.TryGetValue(actor, out var s)) return 0;
        return s.CurrentCharges[slot];
    }

    // ===== ITickable =====

    /// <summary>每帧驱动：50 actor × 2 slot = 100 次循环，0 GC alloc。</summary>
    public void OnUpdate(float dt)
    {
        foreach (var kv in _states)
        {
            var actor = kv.Key;
            var state = kv.Value;
            for (int slot = 0; slot < 2; slot++)
            {
                TickSlot(actor, state, slot, dt);
            }
        }
    }

    // ===== 事件处理 =====

    /// <summary>
    /// CombatModule 在 ShouldUseSkill(slot)==true 时发布 SkillCastEvent(skillId="slot0"/"slot1")。
    /// SkillModule 解析 slot 字符串 → 校验就绪 → 驱动帧相位。
    /// SlotIndex 值域：0 或 1。任何越界值记录 Warn 后忽略。
    /// </summary>
    [EventHandler]
    void OnSkillCastInput(Tattoo.Events.SkillCastEvent e)
    {
        // 解析 "slot0" / "slot1"
        int slot = -1;
        if (e.SkillId == "slot0") slot = 0;
        else if (e.SkillId == "slot1") slot = 1;

        if (slot < 0)
        {
            // 非 slotX 格式说明是其他系统（如 TattooModule）发的真实技能 ID，不处理
            return;
        }

        // MVP 阶段：从 _states 中取玩家 actor（按 SpawnerModule 的 PlayerTarget）
        // 晚绑定：SpawnerModule 在同一 Category 3，运行时可获取
        var spawner = _runner.GetModule<SpawnerModule>();
        var actor   = spawner?.PlayerTarget;
        if (actor == null) return;

        HandleSkillInput(actor, slot);
    }

    /// <summary>校验并触发技能帧相位的内部方法，供 OnSkillCastInput 和 BotAI 扩展点调用。</summary>
    void HandleSkillInput(Target actor, int slot)
    {
        if (slot < 0 || slot >= 2)
        {
            FrameworkLogger.Warn("SkillModule",
                $"Action=HandleSkillInput SlotIndex={slot} out of range [0,1], ignored");
            return;
        }
        var state = GetOrCreateState(actor);

        // 槽位为空 → 忽略
        var skillId = state.SlotToSkillId[slot];
        if (skillId == null) return;

        // 不在 Idle → 忽略（已在释放中）
        if (state.Phases[slot] != SkillPhase.Idle) return;

        // 校验就绪
        if (!_configById.TryGetValue(skillId, out var row)) return;
        if (!IsReady(state, slot, row)) return;

        // 进入 Startup
        state.Phases[slot]       = SkillPhase.Startup;
        state.PhaseElapsed[slot] = 0f;
        state.ActiveFired[slot]  = false;

        FrameworkLogger.Info("SkillModule",
            $"Action=SkillStartup Actor={actor.Name} Slot={slot} SkillId={skillId}");
    }

    /// <summary>
    /// 响应 ItemPickedEvent：ItemId 命中 SkillConfig 时装入空槽（优先 slot 0）。
    /// 两槽均有装备时装入 slot 0（替换）。
    /// </summary>
    [EventHandler]
    void OnItemPicked(ItemPickedEvent e)
    {
        if (!_itemIdToSkillId.TryGetValue(e.ItemId, out var skillId)) return;
        var actor = e.Picker?.Target;
        if (actor == null) return;

        var state   = GetOrCreateState(actor);
        int target  = state.SlotToSkillId[0] == null ? 0
                    : state.SlotToSkillId[1] == null ? 1
                    : 0;

        EquipSkill(actor, target, skillId);
    }

    /// <summary>
    /// 响应 ShopPurchaseEvent：ItemId 命中 SkillConfig 时装入空槽。
    /// </summary>
    [EventHandler]
    void OnShopPurchase(ShopPurchaseEvent e)
    {
        if (!_itemIdToSkillId.TryGetValue(e.ItemId, out var skillId)) return;
        var actor = e.Buyer?.Target;
        if (actor == null) return;

        var state   = GetOrCreateState(actor);
        int target  = state.SlotToSkillId[0] == null ? 0
                    : state.SlotToSkillId[1] == null ? 1
                    : 0;

        EquipSkill(actor, target, skillId);
    }

    /// <summary>冷却缩减扩展点（MVP 阶段不启用）。</summary>
    [EventHandler]
    void OnEffectApplied(EffectAppliedEvent e)
    {
        // MVP 阶段留空：未来在此处读 stat CDR 并缩减 CooldownRemaining
    }

    // ===== 私有辅助 =====

    SkillActorState GetOrCreateState(Target actor)
    {
        if (_states.TryGetValue(actor, out var s)) return s;
        s = new SkillActorState();
        _states[actor] = s;
        return s;
    }

    /// <summary>判断槽位当前是否可释放技能。</summary>
    bool IsReady(SkillActorState state, int slot, SkillConfigRow row)
    {
        switch (row.ChargeModel)
        {
            case 0: // 纯冷却：CD 必须耗尽
                return state.CooldownRemaining[slot] <= 0f;
            case 1: // 充能：至少有 1 层
                return state.CurrentCharges[slot] > 0;
            case 2: // 蓄力：Idle 状态即可开始（持续按住才推进）
                return true;
            default:
                return false;
        }
    }

    /// <summary>每帧推进单个槽位的相位和 CD / 充能计时。无 GC alloc。</summary>
    void TickSlot(Target actor, SkillActorState state, int slot, float dt)
    {
        var skillId = state.SlotToSkillId[slot];
        if (skillId == null) return;
        if (!_configById.TryGetValue(skillId, out var row)) return;

        // ---- 冷却 & 充能回复（与帧相位无关，始终推进）----
        TickCooldownAndCharge(state, slot, row, dt);

        // ---- 帧相位推进 ----
        var phase = state.Phases[slot];
        if (phase == SkillPhase.Idle) return;

        state.PhaseElapsed[slot] += dt;

        switch (phase)
        {
            case SkillPhase.Startup:
            {
                float startupSec = row.StartupFrames / 60f;
                if (state.PhaseElapsed[slot] >= startupSec)
                {
                    // Startup 结束 → 进入 Active
                    state.Phases[slot]      = SkillPhase.Active;
                    state.PhaseElapsed[slot] = 0f;
                    state.ActiveFired[slot]  = false;
                }
                break;
            }
            case SkillPhase.Active:
            {
                // Active 帧第 1 Tick 发布 SkillActivatedEvent
                if (!state.ActiveFired[slot])
                {
                    state.ActiveFired[slot] = true;
                    _bus.Publish(new SkillActivatedEvent(
                        actor, slot, skillId, state.AimTarget[slot]));
                    FrameworkLogger.Info("SkillModule",
                        $"Action=SkillActivated Actor={actor.Name} Slot={slot} SkillId={skillId}");
                }

                float activeSec = row.ActiveFrames / 60f;
                if (state.PhaseElapsed[slot] >= activeSec)
                {
                    // Active 结束 → Recovery
                    state.Phases[slot]      = SkillPhase.Recovery;
                    state.PhaseElapsed[slot] = 0f;
                    // 扣冷却 / 充能（在 Active 结束时扣）
                    ConsumeResource(state, slot, row);
                }
                break;
            }
            case SkillPhase.Recovery:
            {
                float recoverySec = row.RecoveryFrames / 60f;
                if (state.PhaseElapsed[slot] >= recoverySec)
                {
                    // Recovery 结束 → Idle
                    state.Phases[slot]      = SkillPhase.Idle;
                    state.PhaseElapsed[slot] = 0f;
                }
                break;
            }
        }
    }

    /// <summary>推进冷却倒计时与充能回复计时。</summary>
    void TickCooldownAndCharge(SkillActorState state, int slot, SkillConfigRow row, float dt)
    {
        switch (row.ChargeModel)
        {
            case 0: // 纯冷却
                if (state.CooldownRemaining[slot] > 0f)
                    state.CooldownRemaining[slot] = Math.Max(0f, state.CooldownRemaining[slot] - dt);
                break;
            case 1: // 充能回复
                if (state.CurrentCharges[slot] < row.MaxCharges)
                {
                    state.ChargeRegenElapsed[slot] += dt;
                    if (state.ChargeRegenElapsed[slot] >= row.ChargeRegenTime)
                    {
                        state.ChargeRegenElapsed[slot] -= row.ChargeRegenTime;
                        state.CurrentCharges[slot]++;
                    }
                }
                break;
            // ChargeModel=2（蓄力）的计时在蓄力期间由 CombatModule 推进 HoldElapsed，此处不处理
        }
    }

    /// <summary>Active 结束时扣除冷却 / 充能资源。</summary>
    void ConsumeResource(SkillActorState state, int slot, SkillConfigRow row)
    {
        switch (row.ChargeModel)
        {
            case 0: // 纯冷却：开始冷却
                state.CooldownRemaining[slot] = row.Cooldown;
                break;
            case 1: // 充能：消耗 1 层
                if (state.CurrentCharges[slot] > 0)
                    state.CurrentCharges[slot]--;
                // 若尚未开始回复计时，重置计时器
                if (state.CurrentCharges[slot] < row.MaxCharges && state.ChargeRegenElapsed[slot] <= 0f)
                    state.ChargeRegenElapsed[slot] = 0f;
                break;
            case 2: // 蓄力：重置蓄力计时
                state.HoldElapsed[slot] = 0f;
                break;
        }
    }
}
