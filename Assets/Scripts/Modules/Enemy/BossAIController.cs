using UnityEngine;

namespace Tattoo
{
    /// <summary>
    /// Boss 三阶段 AI 控制器。
    /// 阶段切换：HP 100%→Phase1 / ≤60%→Phase2 / ≤30%→Phase3。
    /// 每阶段均有无敌短帧（_transitionTimer > 0 时不受伤）。
    /// 每帧决策（不走 LOD），仇恨分散：OverlapSphere 取最近 3 个 actor。
    /// </summary>
    public sealed class BossAIController
    {
        // ===== 绑定数据 =====
        public EnemyActorData   Actor     { get; }
        public GameObject       Go        { get; }
        public EnemyConfigRow   Config    { get; }
        public BossPhaseConfig  PhaseTable { get; }

        // ===== 阶段状态机 =====
        public int  CurrentPhase    { get; private set; } = 1;
        public bool IsTransitioning => _transitionTimer > 0f;

        // ===== 内部 =====
        Transform _selfTransform;
        float     _transitionTimer;  // 阶段切换无敌窗口（≤ 1s）
        float     _skillCooldown;

        const float TransitionInvincibleDuration = 0.8f;  // 阶段转换无敌时长（s）
        const float SkillCooldown                = 4f;

        // 相位切换监听（由 EnemyModule 订阅 BossPhaseChangedEvent 后回调）
        public System.Action<int, int, float> OnPhaseChanged;

        public BossAIController(
            EnemyActorData actor,
            GameObject go,
            EnemyConfigRow config,
            BossPhaseConfig phaseTable)
        {
            Actor        = actor;
            Go           = go;
            Config       = config;
            PhaseTable   = phaseTable;
            _selfTransform = go.transform;
        }

        /// <summary>每帧由 EnemyModule 调用（Boss 不走 LOD）。</summary>
        public EnemyIntent Tick(float dt, Transform playerTransform)
        {
            if (Actor.IsDead) return default;

            // 阶段切换无敌倒计时
            if (_transitionTimer > 0f)
            {
                _transitionTimer -= dt;
                return default;  // 无敌期间不行动
            }

            if (_skillCooldown > 0f) _skillCooldown -= dt;

            if (playerTransform == null) return default;

            Vector3 toTarget = playerTransform.position - _selfTransform.position;
            float   dist     = toTarget.magnitude;

            // 技能优先（AOE 范围内取最近 3 actor 的逻辑由 EnemyModule 处理）
            if (_skillCooldown <= 0f)
            {
                string skillId = GetPhaseSkillId();
                if (!string.IsNullOrEmpty(skillId))
                {
                    _skillCooldown = SkillCooldown;
                    return new EnemyIntent { SkillId = skillId };
                }
            }

            // 近战攻击
            if (dist <= Config.AttackRange)
                return new EnemyIntent { Attack = true };

            // 追击
            return new EnemyIntent { MoveDir = toTarget.normalized };
        }

        /// <summary>受伤处理。阶段切换时进入无敌。</summary>
        public void OnDamaged(float dmg)
        {
            if (IsTransitioning) return;  // 阶段切换无敌

            Actor.HP -= dmg;
            if (Actor.HP < 0f) Actor.HP = 0f;

            // 检查是否触发阶段切换
            CheckPhaseTransition();
        }

        // ===== 私有方法 =====

        void CheckPhaseTransition()
        {
            if (Actor.IsDead) return;

            float hpRatio = Actor.HP / Actor.MaxHP;

            // Phase 1→2（60% HP）
            if (CurrentPhase == 1 && PhaseTable.TryGetByBossPhase(Actor.EnemyId, 2, out var p2Row)
                && hpRatio <= p2Row.HPThreshold)
            {
                TransitionToPhase(2, p2Row.EnrageMultiplier);
                return;
            }

            // Phase 2→3（30% HP）
            if (CurrentPhase == 2 && PhaseTable.TryGetByBossPhase(Actor.EnemyId, 3, out var p3Row)
                && hpRatio <= p3Row.HPThreshold)
            {
                TransitionToPhase(3, p3Row.EnrageMultiplier);
            }
        }

        void TransitionToPhase(int toPhase, float enrageMultiplier)
        {
            int fromPhase    = CurrentPhase;
            CurrentPhase     = toPhase;
            Actor.EnrageMult = enrageMultiplier;
            _transitionTimer = TransitionInvincibleDuration;

            // 通知 EnemyModule（由其转发 BossPhaseChangedEvent）
            OnPhaseChanged?.Invoke(fromPhase, toPhase, enrageMultiplier);
        }

        string GetPhaseSkillId()
        {
            if (!PhaseTable.TryGetByBossPhase(Actor.EnemyId, CurrentPhase, out var row))
                return null;
            if (string.IsNullOrEmpty(row.NewSkillIds)) return null;
            int comma = row.NewSkillIds.IndexOf(',');
            return comma < 0 ? row.NewSkillIds : row.NewSkillIds.Substring(0, comma);
        }
    }
}
