using System.Collections.Generic;
using Tattoo.Combat;
using Tattoo.Data;
using Tattoo.Events;
using UnityEngine;

// EntityRef / SpawnerModule / TattooModule 在 namespace Tattoo（无子 namespace）。
// 本文件位于 namespace Tattoo.Bot，C# 嵌套搜索会自动解析父 namespace —— 无需额外 using。

namespace Tattoo.Bot
{
    /// <summary>
    /// v2.1 智能 Bot controller。
    /// LOD：视野内每帧 / 视野外 0.5s 一次（由 BotControllerModule 控制是否调本 Tick）。
    /// 决策：朝最近敌人移动 + 攻击；受击 1s 内 30% 概率主动闪避；技能 Q/E 按 cooldown + 概率使用。
    /// ShouldSelfTattoo：每 RethinkInterval 秒走 BotBuildPlanner，并叠加 SelfTattooBoldness 安全度判定。
    /// ShouldPurchase：本期不实现，后续 NPCInteractStartEvent 触发；恒 false。
    ///
    /// 0 GC：状态用值类型 / 字段缓存，决策只读 SpawnerModule.Enemies 列表 + Player 引用。
    /// </summary>
    public sealed class SmartBotPlayerController : IPlayerController
    {
        public Target OwnerActor { get; }
        public PlayerControllerType Type => PlayerControllerType.SmartBot;

        readonly SpawnerModule _spawner;
        readonly TattooModule _tattoo;
        readonly BotConfigRow _cfg;
        readonly BotBuildPresetRow _preset;

        // ===== 缓存 =====
        Target _cachedAimTarget;                 // 当前锁敌（每帧由本 controller 重新选）
        float _lastAttackTime;
        float _lastSkillTime;                    // 共用一份 cd（Q/E 同源，简化）
        float _lastDamagedTime = -10f;           // 由 ModuleRunner 通过 OnDamaged 写入
        float _lastRethinkTime;
        float _safetyScore = 1f;                 // 0..1，由模块每次 Tick 时写入

        // 由 BotControllerModule 在 [EventHandler] OnDamaged 中写入
        public void NotifyDamaged(float time) { _lastDamagedTime = time; }
        public void NotifySafety(float score)  { _safetyScore = score; }

        public SmartBotPlayerController(
            Target ownerActor,
            SpawnerModule spawner,
            TattooModule tattoo,
            BotConfigRow cfg,
            BotBuildPresetRow preset)
        {
            OwnerActor = ownerActor;
            _spawner = spawner;
            _tattoo = tattoo;
            _cfg = cfg;
            _preset = preset;
        }

        // ===== 移动 / 朝向 =====

        public Vector2 GetMoveInput()
        {
            var self = ResolveSelfGo();
            if (self == null) return Vector2.zero;
            // 读条期间不动
            if (_tattoo.IsInProgress(OwnerActor)) return Vector2.zero;

            var target = SelectAimTarget();
            if (target == null) return Vector2.zero;
            var targetGo = ResolveGo(target);
            if (targetGo == null) return Vector2.zero;

            var pSelf = self.transform.position;
            var pTgt = targetGo.transform.position;
            float dx = pTgt.x - pSelf.x;
            float dz = pTgt.z - pSelf.z;
            float dist = Mathf.Sqrt(dx * dx + dz * dz);
            // 离敌人 < AggroRadius 的内圈时减速到 0，避免顶在脸上
            if (dist < 1.5f) return Vector2.zero;
            float inv = 1f / Mathf.Max(dist, 0.001f);
            return new Vector2(dx * inv, dz * inv);
        }

        public Vector3 GetFacingDirection()
        {
            var t = SelectAimTarget();
            var self = ResolveSelfGo();
            if (t == null || self == null) return Vector3.forward;
            var go = ResolveGo(t);
            if (go == null) return Vector3.forward;
            var d = go.transform.position - self.transform.position;
            d.y = 0;
            return d.sqrMagnitude > 0.001f ? d.normalized : Vector3.forward;
        }

        public Target GetAimTarget() => SelectAimTarget();

        // ===== 动作意图 =====

        public bool ShouldAttack()
        {
            if (_tattoo.IsInProgress(OwnerActor)) return false;
            float now = Time.time;
            if (now - _lastAttackTime < _cfg.AttackCooldown) return false;

            var t = SelectAimTarget();
            if (t == null) return false;
            var self = ResolveSelfGo();
            var go = ResolveGo(t);
            if (self == null || go == null) return false;

            float sqr = (go.transform.position - self.transform.position).sqrMagnitude;
            if (sqr > _cfg.AggroRadius * _cfg.AggroRadius) return false;

            // v2.1 猎物机会：目标正在读条 → 必扑
            bool preyReading = _tattoo.IsInProgress(t);
            if (preyReading || sqr < 4f * 4f) // 4m 近身保底
            {
                _lastAttackTime = now;
                return true;
            }
            return false;
        }

        public bool ShouldChargedAttack() => false; // v2.1 暂未接入

        public bool ShouldDodge()
        {
            if (_tattoo.IsInProgress(OwnerActor)) return false;
            float now = Time.time;
            // 受击后 1s 内 30% × Confidence 概率主动闪避
            if (now - _lastDamagedTime > 1f) return false;
            // 按 reaction ms 模拟反应延迟
            float reactionSec = _cfg.DodgeReactionMs * 0.001f;
            if (now - _lastDamagedTime < reactionSec) return false;
            return Random.value < 0.3f * _cfg.Confidence;
        }

        public bool ShouldUseSkill(int slot)
        {
            if (slot < 0 || slot > 1) return false;
            if (_tattoo.IsInProgress(OwnerActor)) return false;
            float now = Time.time;
            if (now - _lastSkillTime < 3f) return false; // 简易 cd
            // 视野内有敌人且距离够近时 20% × Confidence 几率施放
            var t = SelectAimTarget();
            if (t == null) return false;
            if (Random.value < 0.2f * _cfg.Confidence)
            {
                _lastSkillTime = now;
                return true;
            }
            return false;
        }

        public bool ShouldInteract() => false; // 工作室触发器走事件，不在此轮询

        // ===== Build 决策（低频） =====

        public bool ShouldSelfTattoo(out int partId, out int colorId, out int patternId)
        {
            partId = colorId = patternId = 0;
            float now = Time.time;
            if (now - _lastRethinkTime < _cfg.RethinkInterval) return false;
            _lastRethinkTime = now;

            // 安全度门槛：被围殴时不读条
            if (!BotBuildPlanner.ShouldStartReading(_safetyScore, _cfg.SelfTattooBoldness)) return false;

            if (!BotBuildPlanner.PlanNext(_preset, _tattoo.Equipped, out var plan)) return false;
            partId = plan.PartId;
            colorId = plan.ColorId;
            patternId = plan.PatternId;
            return true;
        }

        public bool ShouldPurchase(out int itemId)
        {
            itemId = 0;
            return false; // 本期未接通 EconomyModule
        }

        // ===== 内部 =====

        Target SelectAimTarget()
        {
            // 优先考虑：玩家 → 其他敌对 actor。简单实现：最近一个非自己且活着的目标。
            var self = ResolveSelfGo();
            if (self == null) return null;
            var origin = self.transform.position;

            float min = float.MaxValue;
            Target best = null;

            // 1) 玩家
            if (_spawner.Player != null && _spawner.PlayerTarget != null && _spawner.PlayerTarget.Health > 0f)
            {
                float d = (_spawner.Player.transform.position - origin).sqrMagnitude;
                if (d < min && d < _cfg.VisionRadius * _cfg.VisionRadius) { min = d; best = _spawner.PlayerTarget; }
            }
            // 2) Enemies 列表（包含其他 Bot 的 actor）
            var list = _spawner.Enemies;
            for (int i = 0; i < list.Count; i++)
            {
                var go = list[i];
                if (go == null || !go.activeSelf) continue;
                var er = go.GetComponent<EntityRef>();
                if (er == null || er.Target == null) continue;
                if (er.Target == OwnerActor) continue;
                if (er.Target.Health <= 0f) continue;
                float d = (go.transform.position - origin).sqrMagnitude;
                if (d < min && d < _cfg.VisionRadius * _cfg.VisionRadius) { min = d; best = er.Target; }
            }
            _cachedAimTarget = best;
            return best;
        }

        GameObject ResolveSelfGo()
        {
            // BotControllerModule 应直接缓存 actor→GameObject，但为简化这里走 Enemies 扫描
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

        GameObject ResolveGo(Target t)
        {
            if (t == null) return null;
            if (t == _spawner.PlayerTarget) return _spawner.Player;
            var list = _spawner.Enemies;
            for (int i = 0; i < list.Count; i++)
            {
                var go = list[i];
                if (go == null) continue;
                var er = go.GetComponent<EntityRef>();
                if (er != null && er.Target == t) return go;
            }
            return null;
        }
    }
}
