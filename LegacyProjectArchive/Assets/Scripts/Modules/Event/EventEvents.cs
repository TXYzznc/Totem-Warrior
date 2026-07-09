using Economy;
using MapGen.Data;

namespace GameEvent.Events
{
    // ═══════════════════════════════════════════════════════════════
    // EventModule 发布 / 订阅的事件定义（CONTRACT §1.7）
    // 字段签名锁定，禁止在此删改——只允许 append 新字段
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// 玩家/Bot 进入 EventRoom 且 EventState == Idle 时触发。
    /// VFXModule / AudioModule 可订阅此事件播放进场特效/音效。
    /// SpawnerModule 订阅此事件处理 combat_event 的敌人 Spawn。
    /// </summary>
    public sealed class RoomEventTriggeredEvent
    {
        /// <summary>进入事件房的 actor</summary>
        public Actor Enterer;
        /// <summary>触发的事件 ID（对应 EventConfig.EventId）</summary>
        public string RoomEventId;

        public RoomEventTriggeredEvent(Actor enterer, string roomEventId)
        {
            Enterer     = enterer;
            RoomEventId = roomEventId;
        }
    }

    /// <summary>
    /// choice_event 触发后，选项池抽签完成，三选一面板即将显示。
    /// UIModule（ThreeChoiceUIForm）订阅此事件打开面板。
    /// 发布时 Time.timeScale 已设为 0。
    /// </summary>
    public sealed class ThreeChoiceShownEvent
    {
        /// <summary>触发选择的 actor</summary>
        public Actor Chooser;
        /// <summary>抽签结果——严格三项，OptionType 互不相同</summary>
        public ThreeChoiceOption[] Options;
        /// <summary>倒计时秒数（v2.1 = 20s）</summary>
        public float TimeoutSec;

        public ThreeChoiceShownEvent(Actor chooser, ThreeChoiceOption[] options, float timeoutSec)
        {
            Chooser    = chooser;
            Options    = options;
            TimeoutSec = timeoutSec;
        }
    }

    /// <summary>
    /// 玩家点击选项或倒计时到期时发布（CONTRACT §1.7 锁定）。
    /// EventModule 订阅此事件路由奖励结算；TattooModule / SkillModule / EconomyModule 等间接响应。
    /// 收到此事件后 Time.timeScale 恢复 1。
    /// </summary>
    public sealed class ThreeChoiceMadeEvent
    {
        /// <summary>做出选择的 actor</summary>
        public Actor Chooser;
        /// <summary>选中项的下标（0/1/2）</summary>
        public int SelectedIndex;
        /// <summary>选中的选项</summary>
        public ThreeChoiceOption Selected;
        /// <summary>是否因倒计时到期而自动选择</summary>
        public bool IsTimeout;

        public ThreeChoiceMadeEvent(Actor chooser, int selectedIndex, ThreeChoiceOption selected, bool isTimeout)
        {
            Chooser       = chooser;
            SelectedIndex = selectedIndex;
            Selected      = selected;
            IsTimeout     = isTimeout;
        }
    }

    /// <summary>
    /// 倒计时进度 Tick（每 0.5s 发一次）。
    /// UIModule 订阅更新倒计时 UI；不发此事件不影响结算逻辑。
    /// </summary>
    public sealed class ChoiceTimerTickEvent
    {
        /// <summary>剩余秒数（unscaled）</summary>
        public float RemainingSeconds;

        public ChoiceTimerTickEvent(float remaining)
        {
            RemainingSeconds = remaining;
        }
    }
}

namespace GameEvent
{
    /// <summary>
    /// 三选一选项 struct（值类型，避免装箱/GC alloc）。
    /// </summary>
    public readonly struct ThreeChoiceOption
    {
        /// <summary>对应 ThreeChoiceOptionConfig.OptionId</summary>
        public readonly string OptionId;
        /// <summary>选项类型（tattoo_recipe / pattern_recipe / weapon_upgrade / ...）</summary>
        public readonly string OptionType;
        /// <summary>显示名本地化 Key</summary>
        public readonly string DisplayName;
        /// <summary>描述本地化 Key</summary>
        public readonly string DescKey;
        /// <summary>内容引用（配方/技能/升级 ID）</summary>
        public readonly string ContentRef;
        /// <summary>skill_upgrade 的目标槽位（值域 0/1/-1）</summary>
        public readonly int SkillSlot;
        /// <summary>数值型内容（金币量/治疗量）</summary>
        public readonly int ValueInt;

        public ThreeChoiceOption(string optionId, string optionType, string displayName,
            string descKey, string contentRef, int skillSlot, int valueInt)
        {
            OptionId    = optionId;
            OptionType  = optionType;
            DisplayName = displayName;
            DescKey     = descKey;
            ContentRef  = contentRef;
            SkillSlot   = skillSlot;
            ValueInt    = valueInt;
        }
    }
}
