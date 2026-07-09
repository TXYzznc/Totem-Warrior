using Tattoo.Data;
using UnityEngine;

namespace Tattoo.Combat
{
    /// <summary>玩家操控 controller。接 InputModule，把按键 → 高层意图。</summary>
    public sealed class HumanPlayerController : IPlayerController
    {
        readonly InputModule  _input;
        readonly SpawnerModule _spawner;
        readonly WeaponModule  _weapon;

        // 预分配 RaycastHit 避免 Update 内 GC alloc
        static readonly RaycastHit[] s_hitBuffer = new RaycastHit[8];

        // 蓄力判定阈值（秒）—— 与 Pillar B "0.3s 手感" 解耦，刻意慢一拍体现挑战感
        const float ChargeThreshold = 0.4f;

        // Enemy Layer mask（Project Settings → Layers 中 "Enemy" 层对应的 mask）
        static readonly int s_enemyLayerMask = LayerMask.GetMask("Enemy");

        public Target OwnerActor { get; }
        public PlayerControllerType Type => PlayerControllerType.Human;

        public HumanPlayerController(Target playerActor, InputModule input, SpawnerModule spawner, WeaponModule weapon)
        {
            OwnerActor = playerActor;
            _input     = input;
            _spawner   = spawner;
            _weapon    = weapon;
        }

        public Vector2 GetMoveInput() => _input.GetMoveDirection();
        public Vector3 GetFacingDirection() => Camera.main != null
            ? Camera.main.transform.forward
            : Vector3.forward;

        public Target GetAimTarget()
        {
            if (_spawner?.Player == null) return null;

            // 读当前武器的瞄准半角与射程
            float aimSpreadHalfDeg = 180f;
            float maxRange         = 30f;
            if (_weapon != null && OwnerActor != null)
            {
                var state = _weapon.GetEquippedWeapon(OwnerActor);
                if (state.Weapon != null)
                {
                    aimSpreadHalfDeg = state.Weapon.AimSpreadHalfDeg;
                    maxRange         = state.Weapon.Range;
                }
            }

            // ── 分支 A：全方位锁定（halfDeg >= 179.99）──────────────────────────────
            if (aimSpreadHalfDeg >= 179.99f)
            {
                return FindClosestEnemy();
            }

            // 鼠标地面投影：屏幕坐标 → 世界坐标（Y=0 平面交点）
            Vector3 aimWorldPoint = GetMouseGroundPoint();
            Vector3 origin        = _spawner.Player.transform.position;
            Vector3 forward       = (aimWorldPoint - origin);
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f) forward = _spawner.Player.transform.forward;
            forward.Normalize();

            // ── 分支 B：严格 Raycast（halfDeg <= 0.01）───────────────────────────────
            if (aimSpreadHalfDeg <= 0.01f)
            {
                // 沿 forward 方向单射线，取第一个命中的 EntityRef
                int hitCount = Physics.RaycastNonAlloc(origin, forward, s_hitBuffer, maxRange, s_enemyLayerMask);
                float nearestDist = float.MaxValue;
                Target result = null;
                for (int i = 0; i < hitCount; i++)
                {
                    var er = s_hitBuffer[i].collider.GetComponent<EntityRef>();
                    if (er?.Target == null || er.Target.Health <= 0f) continue;
                    if (s_hitBuffer[i].distance < nearestDist)
                    {
                        nearestDist = s_hitBuffer[i].distance;
                        result = er.Target;
                    }
                }
                return result;
            }

            // ── 分支 C：半角扇形几何遍历（0.01 < halfDeg < 179.99）────────────────────
            // 评分 = (1 - dot(forward, dirToEnemy)) * 100 + distance；取最小
            float cosHalfAngle = Mathf.Cos(aimSpreadHalfDeg * Mathf.Deg2Rad);
            float bestScore    = float.MaxValue;
            Target bestTarget  = null;

            foreach (var go in _spawner.Enemies)
            {
                if (go == null || !go.activeSelf) continue;
                var er = go.GetComponent<EntityRef>();
                if (er?.Target == null || er.Target.Health <= 0f) continue;

                Vector3 toEnemy = go.transform.position - origin;
                toEnemy.y = 0f;
                float dist = toEnemy.magnitude;
                if (dist > maxRange || dist < 0.001f) continue;

                float dot = Vector3.Dot(forward, toEnemy / dist);
                if (dot < cosHalfAngle) continue; // 在扇形范围外，剔除

                float score = (1f - dot) * 100f + dist;
                if (score < bestScore)
                {
                    bestScore  = score;
                    bestTarget = er.Target;
                }
            }
            return bestTarget;
        }

        public bool ShouldAttack() => _input.IsAttackPressed();

        public bool ShouldChargedAttack() =>
            _input.IsAttackHolding() && _input.GetAttackHoldDuration() >= ChargeThreshold;

        // ─── 私有辅助 ───────────────────────────────────────────────────

        /// <summary>将屏幕鼠标坐标投影到 Y=0 水平面，获取世界坐标瞄准点。</summary>
        static Vector3 GetMouseGroundPoint()
        {
            var cam = Camera.main;
            if (cam == null) return Vector3.zero;
            var ray = cam.ScreenPointToRay(Input.mousePosition);
            var plane = new Plane(Vector3.up, Vector3.zero);
            if (plane.Raycast(ray, out float enter))
                return ray.GetPoint(enter);
            // 相机平行于地面时的退化情况：取射线在 maxDist 处的点
            return ray.GetPoint(50f);
        }

        /// <summary>全方位最近敌人（halfDeg >= 179.99 的 fallback）。不做 maxRange 裁剪。</summary>
        Target FindClosestEnemy()
        {
            var pp = _spawner.Player.transform.position;
            float minSq = float.MaxValue;
            Target closest = null;
            foreach (var go in _spawner.Enemies)
            {
                if (go == null || !go.activeSelf) continue;
                var er = go.GetComponent<EntityRef>();
                if (er?.Target == null || er.Target.Health <= 0f) continue;
                float dSq = (go.transform.position - pp).sqrMagnitude;
                if (dSq < minSq) { minSq = dSq; closest = er.Target; }
            }
            return closest;
        }

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
