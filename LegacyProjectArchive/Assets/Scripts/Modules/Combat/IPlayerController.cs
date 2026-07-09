using Tattoo.Data;
using UnityEngine;

namespace Tattoo.Combat
{
    /// <summary>
    /// v2.1 核心契约：玩家与所有 actor（人 / AI / 未来网络回放）共用本接口。
    /// 业务模块（CombatModule / WeaponModule / SkillModule）永不感知背后是谁在驱动。
    /// </summary>
    public interface IPlayerController
    {
        Target OwnerActor { get; }
        PlayerControllerType Type { get; }

        Vector2 GetMoveInput();
        Vector3 GetFacingDirection();
        Target GetAimTarget();

        bool ShouldAttack();
        bool ShouldChargedAttack();
        bool ShouldDodge();
        bool ShouldUseSkill(int slot);   // v2.1: slot 仅 0/1
        bool ShouldInteract();

        // Build 决策（低频）
        bool ShouldSelfTattoo(out int partId, out int colorId, out int patternId);
        bool ShouldPurchase(out int itemId);
    }

    public enum PlayerControllerType { Human, SmartBot, LightBot, NetworkReplay }
}
