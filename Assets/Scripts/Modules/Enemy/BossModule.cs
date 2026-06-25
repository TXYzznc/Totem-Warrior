using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Tattoo.Events;

namespace Tattoo
{
    /// <summary>
    /// BossModule：EnemyModule 的子系统包装。
    ///
    /// 当前职责（MVP）：
    ///  - 依赖 EnemyModule 确保初始化顺序（Boss 相关逻辑内嵌于 EnemyModule）
    ///  - 订阅 BossPhaseChangedEvent，记录当前 Boss 阶段，供查询
    ///  - 订阅 BossSpawnedEvent，暴露 CurrentBossId / IsBossAlive 给外部系统
    ///
    /// 注：Boss 的 spawn / AI / 死亡判定 由 EnemyModule 直接处理（见 §二注释）；
    ///     BossModule 仅做状态镜像和外部查询门面，避免重复持有逻辑。
    /// </summary>
    public sealed class BossModule : IGameModule
    {
        public int    ModuleCategory => 3;
        public Type[] Dependencies   => new[] { typeof(EnemyModule) };

        readonly EventBus _bus;

        // ===== 可查询状态 =====
        public string CurrentBossId    { get; private set; } = string.Empty;
        public int    CurrentBossPhase { get; private set; } = 0;
        public bool   IsBossAlive      { get; private set; } = false;

        // 订阅句柄（防内存泄漏）
        IDisposable _subSpawned;
        IDisposable _subPhase;
        IDisposable _subDied;

        public BossModule(EventBus bus)
        {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        }

        public UniTask InitializeAsync(CancellationToken ct = default)
        {
            // 监听 Boss 生命周期事件（EnemyModule 发出）
            _subSpawned = _bus.Subscribe<BossSpawnedEvent>(OnBossSpawned);
            _subPhase   = _bus.Subscribe<BossPhaseChangedEvent>(OnBossPhaseChanged);
            _subDied    = _bus.Subscribe<EnemyDiedEvent>(OnEnemyDied);

            FrameworkLogger.Info("BossModule", "Action=Initialized");
            return UniTask.CompletedTask;
        }

        public UniTask ShutdownAsync(CancellationToken ct = default)
        {
            _subSpawned?.Dispose();
            _subPhase?.Dispose();
            _subDied?.Dispose();

            FrameworkLogger.Info("BossModule", "Action=Shutdown");
            return UniTask.CompletedTask;
        }

        // ===== 事件处理 =====

        void OnBossSpawned(BossSpawnedEvent e)
        {
            CurrentBossId    = e.Boss?.Name ?? string.Empty;
            CurrentBossPhase = 1;
            IsBossAlive      = true;
            FrameworkLogger.Info("BossModule",
                $"Action=BossSpawned BossId={CurrentBossId} Pos={e.SpawnPosition}");
        }

        void OnBossPhaseChanged(BossPhaseChangedEvent e)
        {
            if (e.BossId != CurrentBossId) return;
            CurrentBossPhase = e.ToPhase;
            FrameworkLogger.Info("BossModule",
                $"Action=PhaseChanged BossId={e.BossId} Phase={e.FromPhase}→{e.ToPhase} Enrage={e.NewEnrageMultiplier}");
        }

        void OnEnemyDied(EnemyDiedEvent e)
        {
            if (e.DeadActor == null || string.IsNullOrEmpty(CurrentBossId)) return;
            if (e.DeadActor.Tier != EnemyTier.Boss) return;
            if (e.DeadActor.EnemyId != CurrentBossId) return;
            IsBossAlive = false;
            FrameworkLogger.Info("BossModule",
                $"Action=BossDied BossId={CurrentBossId}");
        }
    }
}
