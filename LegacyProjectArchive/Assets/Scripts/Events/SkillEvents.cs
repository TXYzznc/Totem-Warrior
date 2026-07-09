using Tattoo.Data;

namespace Skill.Events
{
    /// <summary>
    /// 技能进入 Active 帧第 1 帧时发布。
    /// CONTRACT §1.1 锁定签名——TattooModule 订阅触发左臂联动。
    /// SlotIndex 值域：0 或 1。
    /// </summary>
    public sealed class SkillActivatedEvent
    {
        public Target Caster;
        public int    SlotIndex;
        public string SkillId;
        public Target AimTarget;

        public SkillActivatedEvent(Target caster, int slotIndex, string skillId, Target aimTarget)
        {
            Caster    = caster;
            SlotIndex = slotIndex;
            SkillId   = skillId;
            AimTarget = aimTarget;
        }
    }

    /// <summary>
    /// 技能槽位装备变化（装入 / 清除）。
    /// 供 UIModule 刷新 2 槽技能图标。
    /// 暂为 internal，CONTRACT §1.4 追加后改 public。
    /// </summary>
    internal sealed class SkillSlotChangedEvent
    {
        public Target Owner;
        public int    SlotIndex;
        public string SkillId;
        /// <summary>true=装入，false=卸下/替换</summary>
        public bool   IsEquipped;

        public SkillSlotChangedEvent(Target owner, int slotIndex, string skillId, bool isEquipped)
        {
            Owner      = owner;
            SlotIndex  = slotIndex;
            SkillId    = skillId;
            IsEquipped = isEquipped;
        }
    }

    /// <summary>
    /// 玩家技能输入请求。CombatModule 在 ShouldUseSkill(slot)==true 时发布。
    /// SlotIndex 值域：0 或 1。
    /// </summary>
    public sealed class InputSkillSlotEvent
    {
        public Target Actor;
        public int    SlotIndex;

        public InputSkillSlotEvent(Target actor, int slotIndex)
        {
            Actor     = actor;
            SlotIndex = slotIndex;
        }
    }

    // ShopPurchaseEvent / ItemPickedEvent 已在 Economy.Events 命名空间定义
    // （Modules/Economy/EconomyEvents.cs），消费方请引用 Economy.Events 版本。
}
