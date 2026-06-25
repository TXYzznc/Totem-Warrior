using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Tattoo.Events;
using UnityEngine;

namespace Tattoo
{
    /// <summary>
    /// v2.1 EnemyModule：
    /// 负责 Light / Elite 怪物的 spawn、LOD 驱动的 AI 决策、生命周期管理与掉落触发。
    /// Boss 生命周期由本模块管理，阶段状态机委托给 BossAIController。
    ///
    /// MVP 简化：
    ///  - 不依赖 MapGenModule（地图尚未实现）：初始化时直接在玩家周围 Spawn 占位怪。
    ///  - Prefab 用 CreatePrimitive 占位（Sphere=Light，Cube=Elite，大 Cube=Boss）。
    ///  - AI = 朝玩家直走 + 近战攻击。
    ///  - Boss 在 10min 时 spawn，三阶段 HP 切换发事件。
    ///
    /// 依赖：DataTableModule（读 EnemyConfig / BossPhaseConfig）。
    /// MapGenModule 依赖项在 GDD §7.2 NavMesh 就绪后补充，当前不进 Dependencies。
    /// </summary>
    public sealed class EnemyModule : IGameModule, ITickable
    {
        public int    ModuleCategory => 3;
        public Type[] Dependencies   => new[] { typeof(DataTableModule) };

        // ===== 框架依赖 =====
        readonly ModuleRunner _runner;
        readonly EventBus     _bus;

        // ===== 配置表 =====
        EnemyConfig      _enemyConfig;
        BossPhaseConfig  _bossPhaseConfig;

        // ===== 存活怪物（按 LOD 分桶）=====
        readonly List<EnemyAIController> _hotList  = new();  // 视野内（≤20m）
        readonly List<EnemyAIController> _coldList = new();  // 视野外（>20m）
        BossAIController _bossCtrl;

        // ===== 常量 =====
        const float LodRadius    = 20f;
        const float LodSqr       = LodRadius * LodRadius;
        const float LodScanInterval = 0.2f;  // 桶迁移扫描间隔

        // LOD：热区决策间隔（由 Tick 频率保证），冷区通过 stride 均摊
        const float HotTickInterval  = 0.2f;   // 5 Hz
        const float ColdTickInterval = 2f;      // 0.5 Hz

        // Boss spawn 时间（Run 第 10 分钟）
        const float BossSpawnTimeSec = 600f;

        // ===== 运行时状态 =====
        Transform _playerTransform;
        float     _runElapsedSec;
        bool      _bossSpawned;
        float     _lodScanAccum;
        float     _hotTickAccum;
        float     _coldTickAccum;
        int       _coldStride;        // round-robin 索引

        // ===== 攻击节流（避免每帧发攻击事件）=====
        readonly Dictionary<EnemyAIController, float> _attackCooldowns = new();
        const float EnemyAttackCooldown = 1.5f;

        public EnemyModule(ModuleRunner runner, EventBus bus)
        {
            _runner = runner ?? throw new ArgumentNullException(nameof(runner));
            _bus    = bus    ?? throw new ArgumentNullException(nameof(bus));
        }

        // ===== IGameModule =====

        public UniTask InitializeAsync(CancellationToken ct = default)
        {
            // 读取配置表
            var dtModule     = _runner.GetModule<DataTableModule>();
            _enemyConfig     = dtModule.GetTable<EnemyConfig>();
            _bossPhaseConfig = dtModule.GetTable<BossPhaseConfig>();

            // 不在 InitAsync 里 GetModule<SpawnerModule>（不在 Dependencies 内）。
            // 玩家 transform 在首帧 OnUpdate 时懒加载（SpawnerModule 已先于 EnemyModule 完成）。

            // MVP：初始化时 spawn 占位怪（替代 MapGeneratedEvent 触发）
            SpawnInitialEnemies();

            FrameworkLogger.Info("EnemyModule", "Action=Initialized Light+Elite占位怪已生成");
            return UniTask.CompletedTask;
        }

        public UniTask ShutdownAsync(CancellationToken ct = default)
        {
            // 销毁所有怪物 GameObject
            for (int i = 0; i < _hotList.Count; i++)
            {
                if (_hotList[i]?.Go != null)
                    UnityEngine.Object.Destroy(_hotList[i].Go);
            }
            for (int i = 0; i < _coldList.Count; i++)
            {
                if (_coldList[i]?.Go != null)
                    UnityEngine.Object.Destroy(_coldList[i].Go);
            }
            if (_bossCtrl?.Go != null)
                UnityEngine.Object.Destroy(_bossCtrl.Go);

            _hotList.Clear();
            _coldList.Clear();
            _bossCtrl = null;
            _attackCooldowns.Clear();

            FrameworkLogger.Info("EnemyModule", "Action=Shutdown");
            return UniTask.CompletedTask;
        }

        // ===== ITickable =====

        /// <summary>每帧由 GameTickDriver 调用。budget：怪物侧 ≤2.3ms/帧。</summary>
        public void OnUpdate(float dt)
        {
            _runElapsedSec += dt;

            // Boss spawn 计时（Run 第 10min）
            if (!_bossSpawned && _runElapsedSec >= BossSpawnTimeSec)
                SpawnBoss();

            // LOD 桶迁移扫描（0.2s 一次，避免每帧做 sqrMagnitude 遍历）
            _lodScanAccum += dt;
            if (_lodScanAccum >= LodScanInterval)
            {
                _lodScanAccum = 0f;
                RebucketEnemies();
            }

            // 热区 Tick（5 Hz）
            _hotTickAccum += dt;
            if (_hotTickAccum >= HotTickInterval)
            {
                TickEnemyList(_hotList, _hotTickAccum);
                _hotTickAccum = 0f;
            }

            // 冷区 Tick（round-robin stride，0.5 Hz 均摊）
            _coldTickAccum += dt;
            if (_coldTickAccum >= ColdTickInterval && _coldList.Count > 0)
            {
                // 每次只 Tick 一只，分散到多帧
                _coldStride = _coldStride % _coldList.Count;
                TickSingleEnemy(_coldList[_coldStride], _coldTickAccum / _coldList.Count);
                _coldStride++;
                if (_coldStride >= _coldList.Count) { _coldStride = 0; _coldTickAccum = 0f; }
            }

            // Boss 每帧 Tick
            if (_bossCtrl != null && !_bossCtrl.Actor.IsDead)
                TickBoss(dt);
        }

        // ===== 内部方法 =====

        /// <summary>Tick 整个列表，处理攻击意图。</summary>
        void TickEnemyList(List<EnemyAIController> list, float dt)
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                var ctrl = list[i];
                if (ctrl == null || ctrl.Actor.IsDead) continue;
                TickSingleEnemy(ctrl, dt);
            }
        }

        void TickSingleEnemy(EnemyAIController ctrl, float dt)
        {
            if (_playerTransform == null) return;

            EnemyIntent intent = ctrl.Tick(dt, _playerTransform);

            // 移动
            if (intent.MoveDir.sqrMagnitude > 0.001f && ctrl.Go != null)
            {
                float speed = ctrl.Config.MoveSpeed;
                ctrl.Go.transform.position += intent.MoveDir * speed * dt;
            }

            // 攻击：发 AttackHitEvent 到 EventBus，由 CombatModule / 玩家 HP 系统消费
            if (intent.Attack)
            {
                if (!_attackCooldowns.TryGetValue(ctrl, out float cd) || cd <= 0f)
                {
                    _attackCooldowns[ctrl] = EnemyAttackCooldown;
                    float dmg = ctrl.Config.BaseDamage * ctrl.Actor.EnrageMult;
                    _bus.Publish(new EnemyAttackEvent(ctrl.Actor, dmg));
                }
            }

            // 扣除攻击冷却
            if (_attackCooldowns.TryGetValue(ctrl, out float accd) && accd > 0f)
                _attackCooldowns[ctrl] = accd - dt;
        }

        void TickBoss(float dt)
        {
            if (_playerTransform == null) return;

            EnemyIntent intent = _bossCtrl.Tick(dt, _playerTransform);

            if (intent.MoveDir.sqrMagnitude > 0.001f && _bossCtrl.Go != null)
            {
                float speed = _bossCtrl.Config.MoveSpeed;
                _bossCtrl.Go.transform.position += intent.MoveDir * speed * dt;
            }

            if (intent.Attack)
            {
                float dmg = _bossCtrl.Config.BaseDamage * _bossCtrl.Actor.EnrageMult;
                _bus.Publish(new EnemyAttackEvent(_bossCtrl.Actor, dmg));
            }
        }

        /// <summary>按距离把怪物迁移到 hot/cold 桶。禁止在此处发事件或 alloc。</summary>
        void RebucketEnemies()
        {
            if (_playerTransform == null) return;
            Vector3 pPos = _playerTransform.position;

            // hot → cold
            for (int i = _hotList.Count - 1; i >= 0; i--)
            {
                var ctrl = _hotList[i];
                if (ctrl == null || ctrl.Actor.IsDead || ctrl.Go == null) { _hotList.RemoveAt(i); continue; }
                if ((ctrl.Go.transform.position - pPos).sqrMagnitude > LodSqr)
                {
                    _coldList.Add(ctrl);
                    _hotList.RemoveAt(i);
                }
            }

            // cold → hot
            for (int i = _coldList.Count - 1; i >= 0; i--)
            {
                var ctrl = _coldList[i];
                if (ctrl == null || ctrl.Actor.IsDead || ctrl.Go == null) { _coldList.RemoveAt(i); continue; }
                if ((ctrl.Go.transform.position - pPos).sqrMagnitude <= LodSqr)
                {
                    _hotList.Add(ctrl);
                    _coldList.RemoveAt(i);
                }
            }
        }

        // ===== Spawn =====

        /// <summary>MVP 占位：初始化时 spawn 2 只 Light + 1 只 Elite。</summary>
        void SpawnInitialEnemies()
        {
            // Light 杂兵（用 Sphere 占位）
            if (_enemyConfig.TryGetById("enemy_common_light_01", out var lightCfg))
            {
                SpawnEnemy(lightCfg, new Vector3(5f,  0.4f, 0f));
                SpawnEnemy(lightCfg, new Vector3(-5f, 0.4f, 3f));
            }

            // Elite 精英（用橙色 Capsule 占位）
            if (_enemyConfig.TryGetById("enemy_common_elite_01", out var eliteCfg))
                SpawnEnemy(eliteCfg, new Vector3(0f, 0.4f, 7f));
        }

        EnemyAIController SpawnEnemy(EnemyConfigRow cfg, Vector3 pos)
        {
            bool  isLight  = cfg.Tier == "Light";
            var   primitive = isLight ? PrimitiveType.Sphere : PrimitiveType.Capsule;
            var   go       = GameObject.CreatePrimitive(primitive);
            go.name        = $"[Enemy]{cfg.EnemyId}";
            go.transform.position  = pos;
            go.transform.localScale = isLight ? Vector3.one * 0.6f : Vector3.one * 0.8f;

            // 占位颜色：Light=绿，Elite=橙
            Color color = isLight ? new Color(0.3f, 0.8f, 0.3f) : new Color(0.9f, 0.55f, 0.1f);
            SetColor(go, color);

            var actor = new EnemyActorData
            {
                EnemyId  = cfg.EnemyId,
                Tier     = cfg.Tier == "Light" ? EnemyTier.Light : EnemyTier.Elite,
                MaxHP    = cfg.BaseHP,
                HP       = cfg.BaseHP,
                BaseDmg  = cfg.BaseDamage,
            };

            // 挂 EntityRef 以便 CombatModule 通过 GetComponent 识别
            var eRef   = go.AddComponent<EntityRef>();
            eRef.IsPlayer = false;
            eRef.MaxHP    = cfg.BaseHP;

            var ctrl = new EnemyAIController(actor, go, cfg);
            _hotList.Add(ctrl);
            _attackCooldowns[ctrl] = 0f;

            EnemyTier tier = actor.Tier;
            _bus.Publish(new EnemySpawnedEvent(cfg.EnemyId, tier, pos, go));

            FrameworkLogger.Info("EnemyModule",
                $"Action=EnemySpawned EnemyId={cfg.EnemyId} Tier={tier} Pos={pos}");
            return ctrl;
        }

        /// <summary>Boss spawn（Run 第 10min 触发）。</summary>
        void SpawnBoss()
        {
            _bossSpawned = true;

            if (!_enemyConfig.TryGetById("enemy_ai_ruins_boss_01", out var bossCfg))
            {
                FrameworkLogger.Warn("EnemyModule", "Action=BossSpawnSkipped Reason=ConfigNotFound");
                return;
            }

            // 红色大 Cube 占位
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name               = "[Boss]enemy_ai_ruins_boss_01";
            go.transform.position = new Vector3(0f, 1f, 15f);
            go.transform.localScale = new Vector3(2f, 2f, 2f);
            SetColor(go, new Color(0.85f, 0.1f, 0.1f));

            var actor = new EnemyActorData
            {
                EnemyId = bossCfg.EnemyId,
                Tier    = EnemyTier.Boss,
                MaxHP   = bossCfg.BaseHP,
                HP      = bossCfg.BaseHP,
                BaseDmg = bossCfg.BaseDamage,
            };

            var eRef      = go.AddComponent<EntityRef>();
            eRef.IsPlayer = false;
            eRef.MaxHP    = bossCfg.BaseHP;

            _bossCtrl = new BossAIController(actor, go, bossCfg, _bossPhaseConfig);

            // 绑定阶段切换回调
            _bossCtrl.OnPhaseChanged = (from, to, enrage) =>
            {
                _bus.Publish(new BossPhaseChangedEvent(actor.EnemyId, from, to, enrage));
                FrameworkLogger.Info("EnemyModule",
                    $"Action=BossPhaseChanged BossId={actor.EnemyId} From={from} To={to} Enrage={enrage}");

                // Phase3 死亡时处理必掉配方（由 EnemyModule 直接发 DeathChest 占位信号）
                // 注：实际项目由 EconomyModule 订阅 ActorDiedEvent 完成，此处为 MVP 预留
            };

            var bossTarget = new Tattoo.Data.Target { Name = bossCfg.EnemyId, Health = actor.MaxHP };
            _bus.Publish(new BossSpawnedEvent(bossTarget, go.transform.position));
            FrameworkLogger.Info("EnemyModule",
                $"Action=BossSpawned BossId={bossCfg.EnemyId} RunElapsed={_runElapsedSec:F0}s");
        }

        // ===== [EventHandler] 系统事件 =====

        /// <summary>全部模块就绪后，安全获取 SpawnerModule 的玩家 transform。</summary>
        [EventHandler]
        void OnGameReady(GameReadyEvent e)
        {
            var spawner = _runner.GetModule<SpawnerModule>();
            if (spawner?.Player != null)
                _playerTransform = spawner.Player.transform;
        }

        /// <summary>
        /// 监听 EffectAppliedEvent（来自 TattooModule / CombatModule）。
        /// HP ≤ 0 则触发死亡流程。
        /// </summary>
        [EventHandler]
        void OnEffectApplied(EffectAppliedEvent e)
        {
            // 遍历 hotList + coldList，判定 HP 扣减
            // 注意：EffectAppliedEvent 目前仅包含对 Tattoo.Data.Target 的结果；
            // 此处为占位，实际伤害传递在接入 CombatModule 攻击逻辑后完善
            // （EnemyAttackEvent 由本模块发出，玩家受伤由 CombatModule 处理）
        }

        // ===== 工具 =====

        void HandleEnemyDeath(EnemyAIController ctrl)
        {
            if (ctrl.Go != null) ctrl.Go.SetActive(false);

            // 构造掉落数据
            string[] guaranteed = ParseIds(ctrl.Config.GuaranteedLootIds);

            _bus.Publish(new EnemyDiedEvent(ctrl.Actor, null, ctrl.Go != null ? ctrl.Go.transform.position : Vector3.zero));
            _bus.Publish(new DeathChestSpawnedEvent(ctrl.Actor, ctrl.Go != null ? ctrl.Go.transform.position : Vector3.zero, guaranteed));

            FrameworkLogger.Info("EnemyModule",
                $"Action=EnemyDied EnemyId={ctrl.Actor.EnemyId} Tier={ctrl.Actor.Tier} ElitePaintDrop={ctrl.Config.ElitePaintDropRare}");
        }

        void HandleBossDeath()
        {
            if (_bossCtrl.Go != null) _bossCtrl.Go.SetActive(false);

            // 从 Phase3 BossPhaseConfig 取 DeathPatternRecipeId
            string recipeId = string.Empty;
            if (_bossPhaseConfig.TryGetByBossPhase(_bossCtrl.Actor.EnemyId, 3, out var p3))
                recipeId = p3.DeathPatternRecipeId ?? string.Empty;

            string[] guaranteed = string.IsNullOrEmpty(recipeId)
                ? Array.Empty<string>()
                : new[] { recipeId };

            _bus.Publish(new EnemyDiedEvent(_bossCtrl.Actor, null, _bossCtrl.Go != null ? _bossCtrl.Go.transform.position : Vector3.zero));
            _bus.Publish(new DeathChestSpawnedEvent(_bossCtrl.Actor, _bossCtrl.Go != null ? _bossCtrl.Go.transform.position : Vector3.zero, guaranteed));

            FrameworkLogger.Info("EnemyModule",
                $"Action=BossDied BossId={_bossCtrl.Actor.EnemyId} DeathPatternRecipeId={recipeId}");
        }

        /// <summary>逗号分隔字符串 → 数组（避免 LINQ，去空）。</summary>
        static string[] ParseIds(string csv)
        {
            if (string.IsNullOrEmpty(csv)) return Array.Empty<string>();
            var parts = csv.Split(',');
            int count = 0;
            for (int i = 0; i < parts.Length; i++)
                if (!string.IsNullOrWhiteSpace(parts[i])) count++;
            var result = new string[count];
            int j = 0;
            for (int i = 0; i < parts.Length; i++)
                if (!string.IsNullOrWhiteSpace(parts[i])) result[j++] = parts[i].Trim();
            return result;
        }

        static void SetColor(GameObject go, Color color)
        {
            var rd = go.GetComponent<Renderer>();
            if (rd == null) return;
            var sh  = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null) sh = Shader.Find("Standard");
            var mat = new Material(sh);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color"))     mat.SetColor("_Color", color);
            rd.material = mat;
        }

        // ===== 暴露给外部的方法（供 CombatModule / TestRunner 调用）=====

        /// <summary>对某只怪物施加伤害并检查死亡。由 CombatModule 接入后调用。</summary>
        public void ApplyDamageToEnemy(EnemyActorData actor, float dmg)
        {
            // 查找对应 controller
            var ctrl = FindController(actor);
            if (ctrl != null)
            {
                ctrl.OnDamaged(dmg);
                if (ctrl.Actor.IsDead) HandleEnemyDeath(ctrl);
                return;
            }

            // Boss
            if (_bossCtrl != null && _bossCtrl.Actor == actor)
            {
                _bossCtrl.OnDamaged(dmg);
                if (_bossCtrl.Actor.IsDead) HandleBossDeath();
            }
        }

        EnemyAIController FindController(EnemyActorData actor)
        {
            for (int i = 0; i < _hotList.Count; i++)
                if (_hotList[i].Actor == actor) return _hotList[i];
            for (int i = 0; i < _coldList.Count; i++)
                if (_coldList[i].Actor == actor) return _coldList[i];
            return null;
        }

        /// <summary>返回所有存活怪物的 GameObject 列表（供 CombatModule.CollectNearbyEnemies 使用）。</summary>
        public void CollectAliveEnemyGameObjects(List<GameObject> result)
        {
            result.Clear();
            for (int i = 0; i < _hotList.Count; i++)
                if (_hotList[i] != null && !_hotList[i].Actor.IsDead && _hotList[i].Go != null && _hotList[i].Go.activeSelf)
                    result.Add(_hotList[i].Go);
            for (int i = 0; i < _coldList.Count; i++)
                if (_coldList[i] != null && !_coldList[i].Actor.IsDead && _coldList[i].Go != null && _coldList[i].Go.activeSelf)
                    result.Add(_coldList[i].Go);
            if (_bossCtrl != null && !_bossCtrl.Actor.IsDead && _bossCtrl.Go != null && _bossCtrl.Go.activeSelf)
                result.Add(_bossCtrl.Go);
        }
    }

    // ===== 补充事件：怪物攻击玩家信号 =====

    namespace Events
    {
        /// <summary>怪物向玩家/Bot 发起攻击时广播。CombatModule 订阅后扣玩家 HP。</summary>
        public sealed class EnemyAttackEvent
        {
            public EnemyActorData Attacker { get; }
            public float          Damage   { get; }

            public EnemyAttackEvent(EnemyActorData attacker, float damage)
            {
                Attacker = attacker;
                Damage   = damage;
            }
        }
    }
}
