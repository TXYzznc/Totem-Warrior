using UnityEngine;

namespace Tattoo
{
    /// <summary>
    /// 怪物 AI 决策结果（struct 避免每帧 alloc）。
    /// </summary>
    public struct EnemyIntent
    {
        public Vector3 MoveDir;
        public bool    Attack;
        public string  SkillId;   // 空字符串表示不释放技能
    }

    /// <summary>
    /// 怪物 FSM 状态枚举（Light / Elite 共用；Boss 不走本 FSM，走 BossAIController）。
    /// </summary>
    public enum EnemyFSMState { Patrol, Chase, Attack, Retreat, Dead }

    /// <summary>
    /// 轻量怪物 AI 控制器。不实现 IPlayerController，不依赖 TattooModule。
    /// Light 杂兵：Patrol→Chase→Attack→Dead（5 节点）。
    /// Elite 精英：追加 UseSkill / Retreat 节点。
    /// 寻路策略：直走 + 单根障碍 Raycast（不走 NavMesh，减少开销）。
    /// </summary>
    public sealed class EnemyAIController
    {
        // ===== 运行时绑定 =====
        public EnemyActorData Actor     { get; }
        public GameObject     Go        { get; }
        public EnemyConfigRow Config    { get; }

        // ===== FSM =====
        public EnemyFSMState State { get; private set; } = EnemyFSMState.Patrol;

        // ===== 内部状态 =====
        Transform  _selfTransform;
        Transform  _targetTransform;   // 当前追击目标（玩家或 Bot），由 EnemyModule 赋值
        float      _skillCooldown;
        float      _retreatTimer;
        float      _lostTrackTimer;    // 失追计时

        // 视野外冻结时的决策积累器
        float _lodTickAccum;

        // ===== LOD 频率设置 =====
        // 热区（≤20m）决策间隔；冷区（>20m）决策间隔由 EnemyModule LOD 调度器控制外部 Tick 频率
        const float SkillCooldownDefault = 5f;
        const float RetreatDuration      = 2f;
        const float LostTrackTimeout     = 3f;

        public EnemyAIController(EnemyActorData actor, GameObject go, EnemyConfigRow config)
        {
            Actor  = actor;
            Go     = go;
            Config = config;
            _selfTransform = go.transform;
        }

        /// <summary>由 EnemyModule 在每帧（或 LOD 降频后）调用。返回本帧意图。</summary>
        public EnemyIntent Tick(float dt, Transform playerTransform)
        {
            if (Actor.IsDead) return default;

            _targetTransform = playerTransform;

            // 技能冷却计时
            if (_skillCooldown > 0f) _skillCooldown -= dt;

            switch (State)
            {
                case EnemyFSMState.Patrol:  return TickPatrol(dt);
                case EnemyFSMState.Chase:   return TickChase(dt);
                case EnemyFSMState.Attack:  return TickAttack(dt);
                case EnemyFSMState.Retreat: return TickRetreat(dt);
                default:                   return default;
            }
        }

        // ----- 受伤回调（由 EnemyModule 调用） -----
        public void OnDamaged(float dmg)
        {
            Actor.HP -= dmg;
            if (Actor.IsDead)
            {
                State = EnemyFSMState.Dead;
                return;
            }

            // Elite 低血逃跑
            if (Config.Tier == "Elite" && Actor.HP / Actor.MaxHP <= 0.3f && State != EnemyFSMState.Retreat)
            {
                State         = EnemyFSMState.Retreat;
                _retreatTimer = RetreatDuration;
            }
        }

        // ===== 各状态 Tick =====

        EnemyIntent TickPatrol(float dt)
        {
            if (_targetTransform == null) return default;

            float sqrDist = (_targetTransform.position - _selfTransform.position).sqrMagnitude;
            float detect  = Config.DetectRange;

            if (sqrDist <= detect * detect)
            {
                State = EnemyFSMState.Chase;
            }

            // 原地小幅游荡（MVP 阶段：静止 Patrol，等待玩家进入感知范围）
            return default;
        }

        EnemyIntent TickChase(float dt)
        {
            if (_targetTransform == null) return default;

            Vector3 toTarget = _targetTransform.position - _selfTransform.position;
            float   dist     = toTarget.magnitude;
            float   detect   = Config.DetectRange;
            float   attack   = Config.AttackRange;

            // 失追判定
            if (dist > detect * 2f)
            {
                _lostTrackTimer += dt;
                if (_lostTrackTimer >= LostTrackTimeout)
                {
                    _lostTrackTimer = 0f;
                    State = EnemyFSMState.Patrol;
                    return default;
                }
            }
            else
            {
                _lostTrackTimer = 0f;
            }

            // 进入攻击范围
            if (dist <= attack)
            {
                State = EnemyFSMState.Attack;
                return default;
            }

            // Elite 技能释放（在追击中检查）
            if (Config.Tier == "Elite" && _skillCooldown <= 0f && dist <= Config.AttackRange * 3f)
            {
                string skillId = GetFirstSkillId();
                if (!string.IsNullOrEmpty(skillId))
                {
                    _skillCooldown = SkillCooldownDefault;
                    return new EnemyIntent { SkillId = skillId };
                }
            }

            // 移动：目标方向 + 单根障碍 Raycast 避障
            Vector3 moveDir = ComputeMoveDir(toTarget.normalized);
            return new EnemyIntent { MoveDir = moveDir };
        }

        EnemyIntent TickAttack(float dt)
        {
            if (_targetTransform == null) return default;

            float dist   = (_targetTransform.position - _selfTransform.position).magnitude;
            float attack = Config.AttackRange;

            if (dist > attack)
            {
                State = EnemyFSMState.Chase;
                return default;
            }

            return new EnemyIntent { Attack = true };
        }

        EnemyIntent TickRetreat(float dt)
        {
            if (_targetTransform == null) return default;

            _retreatTimer -= dt;
            if (_retreatTimer <= 0f)
            {
                State = EnemyFSMState.Chase;
                return default;
            }

            // 背向玩家逃跑
            Vector3 awayDir = (_selfTransform.position - _targetTransform.position).normalized;
            return new EnemyIntent { MoveDir = awayDir };
        }

        // ===== 工具方法 =====

        /// <summary>目标方向 + 单根障碍 Raycast 简易避障（Light 专用，不走 NavMesh）。</summary>
        Vector3 ComputeMoveDir(Vector3 preferred)
        {
            // 前方 1.5m 检测障碍（仅对 Environment 层）
            int envMask = LayerMask.GetMask("Environment", "Default");
            if (Physics.Raycast(_selfTransform.position + Vector3.up * 0.5f, preferred, 1.5f, envMask))
            {
                // 障碍存在：尝试左偏 45 度
                Vector3 alt = Quaternion.Euler(0, 45f, 0) * preferred;
                if (!Physics.Raycast(_selfTransform.position + Vector3.up * 0.5f, alt, 1.5f, envMask))
                    return alt;
                // 再试右偏
                alt = Quaternion.Euler(0, -45f, 0) * preferred;
                return alt;
            }
            return preferred;
        }

        string GetFirstSkillId()
        {
            if (string.IsNullOrEmpty(Config.SkillIds)) return null;
            // SkillIds 以逗号分隔，取第一个（避免 LINQ）
            int comma = Config.SkillIds.IndexOf(',');
            return comma < 0 ? Config.SkillIds : Config.SkillIds.Substring(0, comma);
        }
    }
}
