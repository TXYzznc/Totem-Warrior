using System;
using System.Collections.Generic;
using System.Threading;
using AttackSystem.Events;
using Cysharp.Threading.Tasks;
using Economy;
using Tattoo.Data;
using Tattoo.Events;
using UnityEngine;

namespace Tattoo
{
    /// <summary>
    /// 场景实体生成器。Init 时程序化创建相机/灯/地面/玩家/敌人。
    ///
    /// #17：从 Resources 加载 Prefab/Character/Player1 实例化玩家与 actor；Boss 从 Prefab/Character/Boss1 加载。
    /// Prefab 缺失时 fallback 到 Cube + FrameworkLogger.Warn，避免阻塞早期开发。
    /// 实体与 Target 通过 EntityRef MonoBehaviour 双向绑定。
    /// </summary>
    public sealed class SpawnerModule : IGameModule
    {
        public int ModuleCategory => 3;
        public Type[] Dependencies => Type.EmptyTypes;

        readonly ModuleRunner _runner;
        readonly EventBus _bus;

        public GameObject  Player    { get; private set; }
        public Target      PlayerTarget { get; private set; }
        public Actor       PlayerActor  { get; private set; }
        public List<GameObject> Enemies { get; } = new();
        public float       PlayerMaxHp { get; private set; } = 100f;
        public GameObject  BossGameObject { get; private set; }

        /// <summary>初始敌人数量。v2.1：49 个 actor 占位（20 Smart + 29 Light），由 BotControllerModule 装配 controller。
        /// Player1 prefab 实例化，shader 染色推 #19。</summary>
        public int InitialEnemyCount = 49;

        public SpawnerModule(ModuleRunner runner, EventBus bus)
        {
            _runner = runner ?? throw new ArgumentNullException(nameof(runner));
            _bus    = bus    ?? throw new ArgumentNullException(nameof(bus));
        }

        public UniTask InitializeAsync(CancellationToken ct = default)
        {
            CreateScene();
            SpawnBoss();
            // change#20 阶段 3 Agent F：
            // 玩家在 StartupSelectForm 确认前保持「裸装」状态。
            // 装备流程由 [EventHandler] OnStartupSelected 驱动（见下方）。
            FrameworkLogger.Info("SpawnerModule",
                $"Action=Initialized Player=1 Enemies={Enemies.Count} Boss={(BossGameObject != null ? 1 : 0)}");
            return UniTask.CompletedTask;
        }

        public UniTask ShutdownAsync(CancellationToken ct = default)
        {
            if (Player != null) UnityEngine.Object.Destroy(Player);
            foreach (var e in Enemies) if (e != null) UnityEngine.Object.Destroy(e);
            Enemies.Clear();
            if (BossGameObject != null) UnityEngine.Object.Destroy(BossGameObject);
            FrameworkLogger.Info("SpawnerModule", "Action=Shutdown");
            return UniTask.CompletedTask;
        }

        void CreateScene()
        {
            // 相机
            var mainCam = Camera.main;
            if (mainCam == null)
            {
                var camGo = new GameObject("MainCamera");
                mainCam = camGo.AddComponent<Camera>();
                camGo.tag = "MainCamera";
            }
            mainCam.transform.position    = new Vector3(0, 18, -10);
            mainCam.transform.eulerAngles = new Vector3(55, 0, 0);
            mainCam.backgroundColor       = new Color(0.18f, 0.18f, 0.22f);
            mainCam.clearFlags            = CameraClearFlags.SolidColor;

            // 地面
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(6, 1, 6);
            var groundRenderer = ground.GetComponent<Renderer>();
            if (groundRenderer != null) groundRenderer.material.color = new Color(0.3f, 0.3f, 0.34f);

            // 太阳灯
            var sun = new GameObject("Sun");
            var light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            sun.transform.eulerAngles = new Vector3(50, 30, 0);

            // 玩家 —— 从 Resources 加载 Player1 prefab，找不到时 fallback Cube
            var playerPrefab = Resources.Load<GameObject>("Prefab/Character/Player1");
            GameObject pGo;
            if (playerPrefab != null)
            {
                pGo = UnityEngine.Object.Instantiate(playerPrefab);
            }
            else
            {
                FrameworkLogger.Warn("SpawnerModule", "Prefab=Prefab/Character/Player1 missing, fallback to Cube");
                pGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            }
            pGo.name = "Player";
            pGo.transform.position = new Vector3(0, 0.4f, 0);
            pGo.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);

            // prefab 已经带 EntityRef（AnimatorGenerator.BuildPrefab 添加）；fallback Cube 没带 → 复用或新建
            var playerRef = pGo.GetComponent<EntityRef>() ?? pGo.AddComponent<EntityRef>();
            playerRef.IsPlayer = true;
            playerRef.MaxHP = 100f;
            playerRef.Target = new Target { Name = "玩家", Health = 100f };

            Player = pGo;
            PlayerTarget = playerRef.Target;
            PlayerMaxHp = 100f;
            PlayerActor = new Actor(1, "玩家", isPlayer: true)
            {
                Target = PlayerTarget,
                GameObject = pGo,
            };

            var bridge = pGo.AddComponent<PlayerAnimatorBridge>();
            bridge.Init(_bus, _runner);

            var mounter = pGo.AddComponent<PlayerWeaponMounter>();
            mounter.Init(_bus, _runner);

            // v2.1：49 个 actor 占位（20 Smart + 29 Light）—— 分多圈布点避免重叠
            // 圈 1：半径 8m，14 个；圈 2：半径 13m，17 个；圈 3：半径 18m，18 个
            // Player1 prefab 实例化，shader 染色推 #19
            int[] ringCounts = { 14, 17, 18 };
            float[] ringRadii = { 8f, 13f, 18f };
            int idx = 0;
            for (int r = 0; r < ringCounts.Length && idx < InitialEnemyCount; r++)
            {
                int cnt = ringCounts[r];
                float rad = ringRadii[r];
                for (int k = 0; k < cnt && idx < InitialEnemyCount; k++, idx++)
                {
                    GameObject eGo;
                    if (playerPrefab != null)
                    {
                        eGo = UnityEngine.Object.Instantiate(playerPrefab);
                    }
                    else
                    {
                        eGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    }
                    eGo.name = $"Actor{idx + 1}";
                    float a = (k + r * 0.3f) * Mathf.PI * 2f / cnt;
                    eGo.transform.position = new Vector3(Mathf.Cos(a) * rad, 0.4f, Mathf.Sin(a) * rad);
                    eGo.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);

                    bool isSmart = idx < 20;
                    var eRef = eGo.GetComponent<EntityRef>() ?? eGo.AddComponent<EntityRef>();
                    eRef.IsPlayer = false;
                    eRef.MaxHP = 50f;
                    eRef.Target = new Target { Name = isSmart ? $"智能{idx + 1}" : $"轻量{idx - 19}", Health = eRef.MaxHP };

                    Enemies.Add(eGo);
                }
            }
        }

        void SpawnBoss()
        {
            var prefab = Resources.Load<GameObject>("Prefab/Character/Boss1");
            if (prefab == null)
            {
                FrameworkLogger.Warn("SpawnerModule", "Prefab=Prefab/Character/Boss1 missing, Boss not spawned");
                return;
            }
            var bossGo = UnityEngine.Object.Instantiate(prefab);
            bossGo.name = "Boss1";
            bossGo.transform.position = new Vector3(0, 0.4f, 15);

            var bossRef = bossGo.GetComponent<EntityRef>() ?? bossGo.AddComponent<EntityRef>();
            bossRef.IsPlayer = false;
            bossRef.MaxHP = 300f;
            bossRef.Target = new Target { Name = "Boss", Health = 300f };

            BossGameObject = bossGo;
        }

        // ── change#20 阶段 3 Agent F ────────────────────────────────────
        // TattooModule / WeaponModule 不在 Dependencies 中。
        // 必须在事件处理方法（运行时）调用 GetModule，不能在 InitializeAsync 缓存。

        /// <summary>
        /// 起手三选确认后，装备玩家纹身与武器。
        /// 订阅 StartupSelectedEvent（由 StartupSelectForm.OnConfirm 发布）。
        /// </summary>
        [EventHandler]
        void OnStartupSelected(StartupSelectedEvent e)
        {
            if (PlayerActor == null)
            {
                FrameworkLogger.Error("SpawnerModule",
                    "Action=OnStartupSelected Error=PlayerActor is null, SpawnBoss not complete?");
                return;
            }

            // ── 纹身：右臂（partId=4）装备玩家选择的颜色 + 第一个图案 ──
            // TattooModule.Equip(partId, colorId, patternId) → 内部发 BuildChangedEvent
            int patternId = e.PatternIds != null && e.PatternIds.Length > 0 ? e.PatternIds[0] : 1;
            _runner.GetModule<TattooModule>().Equip(4 /*RightArm*/, e.ColorId, patternId);

            // ── 武器：为玩家 Target 装备武器；WeaponModule 内部发 WeaponEquippedEvent ──
            // PlayerWeaponMounter MonoBehaviour 订阅 WeaponEquippedEvent 完成 prefab 挂载
            _runner.GetModule<WeaponModule>().EquipWeapon(PlayerActor.Target, e.WeaponId);

            FrameworkLogger.Info("SpawnerModule",
                $"Action=OnStartupSelected Color={e.ColorId} Weapon={e.WeaponId} Patterns=[{string.Join(",", e.PatternIds ?? Array.Empty<int>())}]");
        }

        // ── change#18 B2 骨架：精英标记占位 ─────────────────────────
        /// <summary>
        /// 在已生成的敌人中标记 N 个为精英（Tier=Elite），用于触发 WeaponSpawnerModule 的掉落逻辑。
        /// B2 占位接口；B3-A 实现：选择 N 个 actor 写入 EnemyActorData.Tier=Elite + 视觉标记。
        /// </summary>
        public void SpawnElites()
        {
            // TODO B3-A 实现：标记 N 个精英敌人（数量从 DataTable 读，或暂定 2~3 个）
            FrameworkLogger.Info("SpawnerModule", "Action=SpawnElites (B2 skeleton, no-op)");
        }
    }
}
