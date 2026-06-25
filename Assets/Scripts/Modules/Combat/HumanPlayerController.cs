using Tattoo.Data;
using UnityEngine;

namespace Tattoo.Combat
{
    /// <summary>玩家操控 controller。接 InputModule，把按键 → 高层意图。</summary>
    public sealed class HumanPlayerController : IPlayerController
    {
        readonly InputModule _input;
        readonly SpawnerModule _spawner;
        public Target OwnerActor { get; }
        public PlayerControllerType Type => PlayerControllerType.Human;

        public HumanPlayerController(Target playerActor, InputModule input, SpawnerModule spawner)
        {
            OwnerActor = playerActor;
            _input = input;
            _spawner = spawner;
        }

        public Vector2 GetMoveInput() => _input.GetMoveDirection();
        public Vector3 GetFacingDirection() => Camera.main != null
            ? Camera.main.transform.forward
            : Vector3.forward;

        public Target GetAimTarget()
        {
            if (_spawner?.Player == null) return null;
            var pp = _spawner.Player.transform.position;
            float min = float.MaxValue;
            Target closest = null;
            foreach (var go in _spawner.Enemies)
            {
                if (go == null || !go.activeSelf) continue;
                var er = go.GetComponent<EntityRef>();
                if (er?.Target == null) continue;
                float d = (go.transform.position - pp).sqrMagnitude;
                if (d < min) { min = d; closest = er.Target; }
            }
            return closest;
        }

        public bool ShouldAttack() => _input.IsAttackPressed();
        public bool ShouldChargedAttack() => false; // 接 InputModule 蓄力 API 后接入
        public bool ShouldDodge() => _input.IsDodgePressed();
        public bool ShouldUseSkill(int slot)
        {
            if (slot < 0 || slot > 1) return false;
            // v2.1: 仅 Q / E
            if (slot == 0) return _input.IsSkillPressed();
            // E 键暂复用 IsSkillPressed；后续 InputModule 扩 IsSkill2Pressed 再分开
            return false;
        }
        public bool ShouldInteract() => false;

        public bool ShouldSelfTattoo(out int partId, out int colorId, out int patternId)
        {
            partId = colorId = patternId = 0;
            // 玩家通过 UI Form 触发，不在 Update 轮询。这里恒返回 false。
            // UI Form 直接 publish RequestSelfTattooEvent。
            return false;
        }

        public bool ShouldPurchase(out int itemId)
        {
            itemId = 0;
            return false;
        }
    }
}
