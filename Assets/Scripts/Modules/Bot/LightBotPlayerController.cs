using Tattoo.Combat;
using Tattoo.Data;
using UnityEngine;

namespace Tattoo.Bot
{
    /// <summary>
    /// v2.1 轻量 Bot controller。
    /// LOD：视野内 0.2s / 视野外 2s（由 BotControllerModule 控制 Tick 频率）。
    /// 决策：随机游走 + 被攻击时还击；不刻纹身、不买货、不施放技能。
    ///
    /// 0 GC：游走方向缓存在字段；只在每次 Repath 时改写。
    /// </summary>
    public sealed class LightBotPlayerController : IPlayerController
    {
        public Target OwnerActor { get; }
        public PlayerControllerType Type => PlayerControllerType.LightBot;

        readonly SpawnerModule _spawner;
        readonly BotConfigRow _cfg;

        Vector2 _wanderDir;
        float _nextRepathTime;
        float _lastAttackTime;
        float _lastDamagedTime = -10f;

        public void NotifyDamaged(float time) { _lastDamagedTime = time; }

        public LightBotPlayerController(Target ownerActor, SpawnerModule spawner, BotConfigRow cfg)
        {
            OwnerActor = ownerActor;
            _spawner = spawner;
            _cfg = cfg;
            _wanderDir = Vector2.right;
        }

        public Vector2 GetMoveInput()
        {
            float now = Time.time;
            // 被攻击时朝攻击者跑（这里简化为最近敌人方向）
            if (now - _lastDamagedTime < 2f)
            {
                var t = FindClosestEnemy(out var dir);
                if (t != null) return dir;
            }
            if (now >= _nextRepathTime)
            {
                _nextRepathTime = now + 3f;
                float ang = Random.value * Mathf.PI * 2f;
                _wanderDir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
            }
            return _wanderDir * 0.5f; // 半速游走
        }

        public Vector3 GetFacingDirection()
        {
            // 朝移动方向
            return new Vector3(_wanderDir.x, 0, _wanderDir.y);
        }

        public Target GetAimTarget()
        {
            FindClosestEnemy(out _);
            return _cached;
        }

        Target _cached;

        public bool ShouldAttack()
        {
            float now = Time.time;
            if (now - _lastAttackTime < _cfg.AttackCooldown) return false;
            // 仅在被攻击窗口内还击
            if (now - _lastDamagedTime > 2f) return false;
            var t = FindClosestEnemy(out var _);
            if (t == null) return false;
            // 反应延迟模拟
            float reactionSec = _cfg.DodgeReactionMs * 0.001f;
            if (now - _lastDamagedTime < reactionSec) return false;
            _lastAttackTime = now;
            return true;
        }

        public bool ShouldChargedAttack() => false;
        public bool ShouldDodge() => false; // 轻量 AI 不闪避，靠数量做填充
        public bool ShouldUseSkill(int slot) => false;
        public bool ShouldInteract() => false;
        public bool ShouldSelfTattoo(out int p, out int c, out int pat) { p = c = pat = 0; return false; }
        public bool ShouldPurchase(out int itemId) { itemId = 0; return false; }

        // ===== 内部 =====
        Target FindClosestEnemy(out Vector2 unitDir)
        {
            unitDir = Vector2.zero;
            _cached = null;
            var self = ResolveSelfGo();
            if (self == null) return null;
            var origin = self.transform.position;

            float min = float.MaxValue;
            GameObject bestGo = null;
            Target best = null;

            if (_spawner.Player != null && _spawner.PlayerTarget != null && _spawner.PlayerTarget.Health > 0f)
            {
                float d = (_spawner.Player.transform.position - origin).sqrMagnitude;
                if (d < min && d < _cfg.VisionRadius * _cfg.VisionRadius)
                {
                    min = d; best = _spawner.PlayerTarget; bestGo = _spawner.Player;
                }
            }
            var list = _spawner.Enemies;
            for (int i = 0; i < list.Count; i++)
            {
                var go = list[i];
                if (go == null || !go.activeSelf) continue;
                var er = go.GetComponent<EntityRef>();
                if (er == null || er.Target == null) continue;
                if (er.Target == OwnerActor || er.Target.Health <= 0f) continue;
                float d = (go.transform.position - origin).sqrMagnitude;
                if (d < min && d < _cfg.VisionRadius * _cfg.VisionRadius)
                { min = d; best = er.Target; bestGo = go; }
            }

            if (bestGo != null)
            {
                var diff = bestGo.transform.position - origin;
                float distXZ = Mathf.Sqrt(diff.x * diff.x + diff.z * diff.z);
                if (distXZ > 0.001f)
                {
                    unitDir = new Vector2(diff.x / distXZ, diff.z / distXZ);
                }
            }
            _cached = best;
            return best;
        }

        GameObject ResolveSelfGo()
        {
            var list = _spawner.Enemies;
            for (int i = 0; i < list.Count; i++)
            {
                var go = list[i];
                if (go == null) continue;
                var er = go.GetComponent<EntityRef>();
                if (er != null && er.Target == OwnerActor) return go;
            }
            return null;
        }
    }
}
