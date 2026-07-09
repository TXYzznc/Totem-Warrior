using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using MapGen.Data;
using MapGen.Events;
using UnityEngine;

namespace MapGen
{
    /// <summary>
    /// 地图生成模块（v2.1 MVP 占位版）。
    ///
    /// 完整版职责（见 GDD-v2/modules/07-MapGenModule.md）：
    /// Run 开始时一次性完成大地图生成：BSP 分割（150×150m 根节点）→ 房间/走廊几何 →
    /// 关键点位保底分配 → 缩圈中心计算 → 主题 tile 替换 → NavMesh 烘焙 → 发布 MapGeneratedEvent。
    ///
    /// 【MVP 简化路径，本次实现】
    /// - 不做 BSP 分割（TODO 标记），改为 4 个固定大区域：玩家出生 / 工作室 / 商人 / Boss 房
    /// - 不烘焙 NavMesh（agent 直接 transform 更新，后续接入 NavMeshSurface 时再补）
    /// - 不做美术资源加载（CreatePrimitive + 程序化材质）
    /// - 不做圈外稀有节点（v2.1 已决议移除）
    /// - InitializeAsync 末尾用默认 seed 自动触发一次占位生成（真正接入应订阅 RunStartedEvent，此处 TODO）
    /// - ZoneShrink 控制器内置：ITickable 累加 elapsed → 三段切换发 ZoneShrinkPhaseEvent
    ///
    /// 【约束】
    /// - 所有随机决策使用 System.Random(seed)，禁止 UnityEngine.Random（伪联机→真联机迁移要求）
    /// - InitializeAsync 不发事件（框架戒律）
    /// </summary>
    public sealed class MapGenModule : IGameModule, ITickable
    {
        public int ModuleCategory => 3;
        public Type[] Dependencies => new[]
        {
            typeof(DataTableModule),
            typeof(ResourceModule),
        };

        readonly ModuleRunner _runner;
        readonly EventBus _bus;

        // ===== 配置表引用（InitializeAsync 中赋值） =====
        MapTemplateConfig _templateConfig;
        ZoneShrinkConfig _zoneShrinkConfig;

        // ===== 运行时状态 =====
        readonly List<RoomInfo> _rooms = new();
        readonly List<GameObject> _spawnedObjects = new();
        int _currentSeed;
        int _currentThemeId;
        float _mapSize = 150f;
        Vector2 _initialZoneCenter;

        // ===== 缩圈 Tick 状态 =====
        bool _zoneRunning;
        int _currentPhase = -1;
        float _zoneElapsed;
        readonly List<ZoneShrinkConfigRow> _phaseRows = new();

        // ===== 公开只读访问 =====
        /// <summary>已生成的房间列表（其他模块通过 GetModule&lt;MapGenModule&gt;().Rooms 查询当前状态）</summary>
        public IReadOnlyList<RoomInfo> Rooms => _rooms;
        /// <summary>初始缩圈圆心</summary>
        public Vector2 InitialZoneCenter => _initialZoneCenter;
        /// <summary>地图根边界（150m）</summary>
        public float MapSize => _mapSize;

        public MapGenModule(ModuleRunner runner, EventBus bus)
        {
            _runner = runner ?? throw new ArgumentNullException(nameof(runner));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        }

        public UniTask InitializeAsync(CancellationToken ct = default)
        {
            // 仅做结构初始化：缓存配置表、解析缩圈阶段。不生成几何、不发事件。
            var dataTable = _runner.GetModule<DataTableModule>();
            _templateConfig = dataTable.GetTable<MapTemplateConfig>();
            _zoneShrinkConfig = dataTable.GetTable<ZoneShrinkConfig>();

            CacheZonePhases();

            FrameworkLogger.Info("MapGenModule",
                $"Action=Initialized Templates={_templateConfig.All.Count} ZonePhases={_phaseRows.Count}");

            // === MVP 临时触发点 ===
            // TODO(v2.1 后续): 改为订阅 RunStartedEvent，由 GameStateModule.StartGame() 触发。
            //   届时此处只保留 cache，把 GenerateMap 移到 [EventHandler] OnRunStarted。
            // 现阶段为了让 SpawnerModule 等下游能收到 MapGeneratedEvent，在 Init 完成后下一帧自动触发一次。
            // 用 .Forget() 是因为 InitializeAsync 不能发事件（框架戒律），延迟到下一帧再发。
            TriggerDefaultMapAsync(ct).Forget();

            return UniTask.CompletedTask;
        }

        public UniTask ShutdownAsync(CancellationToken ct = default)
        {
            _zoneRunning = false;
            DestroySpawnedObjects();
            _rooms.Clear();
            _phaseRows.Clear();
            FrameworkLogger.Info("MapGenModule", "Action=Shutdown");
            return UniTask.CompletedTask;
        }

        // ===================================================================
        //  公共 API
        // ===================================================================

        /// <summary>
        /// 生成一张地图。同 seed → 同布局（确定性）。
        /// MVP 实现：占位 4 房间 + 边界墙。发布 MapGeneratedEvent。
        /// 完整实现应做 BSP 分割 → 走廊 → 关键点位 → NavMesh bake。
        /// </summary>
        public void GenerateMap(int seed, int themeId)
        {
            DestroySpawnedObjects();
            _rooms.Clear();

            _currentSeed = seed;
            _currentThemeId = themeId;

            // 读模板（MVP：用第一个模板作为默认，themeId 无匹配时降级）
            var templateRow = ResolveTemplate(themeId);
            _mapSize = templateRow != null ? templateRow.MapSize : 150f;

            // 用 System.Random(seed) — 禁止 UnityEngine.Random（伪联机→真联机迁移要求）
            var rng = new System.Random(seed);

            // === 阶段 A：几何 ===
            BuildPlaceholderGeometry(rng);

            // === 阶段 B：缩圈中心（地图中央 1/3 区域 + 小扰动） ===
            float third = _mapSize / 3f;
            float centerMin = third;          // 50
            float centerMax = _mapSize - third; // 100
            float jitterX = (float)rng.NextDouble() * (centerMax - centerMin) + centerMin;
            float jitterY = (float)rng.NextDouble() * (centerMax - centerMin) + centerMin;
            // 地图世界坐标以 (0,0) 为左下角，中心位于 (75,75)
            _initialZoneCenter = new Vector2(jitterX, jitterY);

            // === 阶段 C：NavMesh bake — MVP 跳过 ===
            // TODO(v2.1): 集成 NavMeshSurface.BuildNavMeshAsync()，目标 ≤ 1.5s

            // 发布事件（此时已不在 InitializeAsync 内，可以发）
            _bus.Publish(new MapGeneratedEvent
            {
                Seed = seed,
                ThemeId = themeId,
                Rooms = new List<RoomInfo>(_rooms), // 复制一份避免外部修改
                InitialZoneCenter = _initialZoneCenter,
                MapSize = _mapSize,
            });

            FrameworkLogger.Info("MapGenModule",
                $"Action=MapGenerated Seed={seed} ThemeId={themeId} Rooms={_rooms.Count} " +
                $"ZoneCenter=({_initialZoneCenter.x:F1},{_initialZoneCenter.y:F1}) Size={_mapSize}");

            // 启动缩圈调度
            StartZoneShrink();
        }

        // ===================================================================
        //  ITickable — 缩圈三段调度
        // ===================================================================

        public void OnUpdate(float deltaTime)
        {
            if (!_zoneRunning || _phaseRows.Count == 0) return;

            _zoneElapsed += deltaTime;

            // 找到当前阶段：累加 StartTime 阈值
            int phase = ResolveCurrentPhase(_zoneElapsed);
            if (phase != _currentPhase && phase >= 0 && phase < _phaseRows.Count)
            {
                _currentPhase = phase;
                PublishZonePhase(phase);
            }
        }

        // ===================================================================
        //  内部实现
        // ===================================================================

        async UniTask TriggerDefaultMapAsync(CancellationToken ct)
        {
            try
            {
                // 等一帧确保 InitializeAsync 已返回，避免在初始化期发事件
                await UniTask.NextFrame(ct);

                // MVP 默认：seed=1, themeId=1（与 MapTemplateConfig 第一行对齐）
                GenerateMap(seed: 1, themeId: 1);
            }
            catch (OperationCanceledException)
            {
                // 启动期被取消，正常路径
            }
            catch (Exception ex)
            {
                FrameworkLogger.Error("MapGenModule",
                    $"Action=TriggerDefaultMapFailed Exception={ex.GetType().Name} Msg=\"{ex.Message}\"");
            }
        }

        MapTemplateConfigRow ResolveTemplate(int themeId)
        {
            if (_templateConfig.TryGetById(themeId, out var row))
                return row;

            // 降级：返回第一行
            foreach (var kv in _templateConfig.All)
            {
                FrameworkLogger.Warn("MapGenModule",
                    $"Action=ResolveTemplate ThemeId={themeId} 未找到，降级使用 Id={kv.Key}");
                return kv.Value;
            }
            return null;
        }

        /// <summary>
        /// 占位地图几何构建：地面 Plane + 4 边界墙 + 4 个固定房间。
        /// TODO(v2.1): 替换为 BSP 递归切割（最小房间 15×15m, BspMaxDepth=4），
        ///             生成走廊、关键点位保底分配（≥2 工作室 / ≥1 商人 / =1 Boss / 5-10 EnemySpawner / 15-25 ChestNode）。
        /// </summary>
        void BuildPlaceholderGeometry(System.Random rng)
        {
            // 地面（150×150m，Plane 默认 10×10，scale=15 即可）
            float planeScale = _mapSize / 10f;
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "MapGen_Ground";
            // 地图世界坐标 (0,0) 左下角；Plane 中心位于 (75, 0, 75)
            ground.transform.position = new Vector3(_mapSize * 0.5f, 0f, _mapSize * 0.5f);
            ground.transform.localScale = new Vector3(planeScale, 1f, planeScale);
            SetColor(ground, new Color(0.28f, 0.30f, 0.34f));
            _spawnedObjects.Add(ground);

            // 4 道边界墙（厚 1m，高 3m）
            BuildBoundaryWalls();

            // 4 个固定房间（占位区域：象限化划分 150×150 → 4 个 75×75 子区域，
            // 房间本体取 30×30 居中放置）
            BuildPlaceholderRooms(rng);
        }

        void BuildBoundaryWalls()
        {
            const float wallH = 3f;
            const float wallT = 1f;
            float s = _mapSize;

            // South（z=0）
            AddWall("MapGen_Wall_S", new Vector3(s * 0.5f, wallH * 0.5f, -wallT * 0.5f), new Vector3(s, wallH, wallT));
            // North（z=s）
            AddWall("MapGen_Wall_N", new Vector3(s * 0.5f, wallH * 0.5f, s + wallT * 0.5f), new Vector3(s, wallH, wallT));
            // West（x=0）
            AddWall("MapGen_Wall_W", new Vector3(-wallT * 0.5f, wallH * 0.5f, s * 0.5f), new Vector3(wallT, wallH, s));
            // East（x=s）
            AddWall("MapGen_Wall_E", new Vector3(s + wallT * 0.5f, wallH * 0.5f, s * 0.5f), new Vector3(wallT, wallH, s));
        }

        void AddWall(string name, Vector3 position, Vector3 scale)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.position = position;
            wall.transform.localScale = scale;
            SetColor(wall, new Color(0.15f, 0.15f, 0.18f));
            _spawnedObjects.Add(wall);
        }

        void BuildPlaceholderRooms(System.Random rng)
        {
            // 4 象限分布：(西南/西北/东南/东北) 各放一个固定房间
            //   出生圈   → 西南
            //   工作室   → 西北
            //   商人     → 东北
            //   Boss 房  → 东南
            float q = _mapSize * 0.25f; // 房间中心距边界 37.5m
            float roomSize = 30f;       // 房间外接矩形 30×30

            AddPlaceholderRoom(0, "SpawnRoom",    RoomNodeType.SpawnRoom,    SizeCategory.Medium,
                new Vector2(q, q), roomSize, new Color(0.30f, 0.70f, 1.00f));
            AddPlaceholderRoom(1, "TattooStudio", RoomNodeType.TattooStudio, SizeCategory.Medium,
                new Vector2(q, _mapSize - q), roomSize, new Color(0.85f, 0.45f, 0.90f));
            AddPlaceholderRoom(2, "Merchant",     RoomNodeType.Merchant,     SizeCategory.Medium,
                new Vector2(_mapSize - q, _mapSize - q), roomSize, new Color(1.00f, 0.85f, 0.30f));
            AddPlaceholderRoom(3, "BossRoom",     RoomNodeType.BossRoom,     SizeCategory.Large,
                new Vector2(_mapSize - q, q), roomSize, new Color(0.95f, 0.30f, 0.30f));

            // rng 当前未用到（占位固定布局），保留参数以维持确定性签名 — TODO(BSP) 替换后启用
            _ = rng;
        }

        void AddPlaceholderRoom(int roomId, string label, RoomNodeType type, SizeCategory size,
            Vector2 centerXZ, float footprint, Color tint)
        {
            // 房间地标：半厚的扁 Cube 作为染色地坪
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = $"MapGen_Room_{roomId}_{label}";
            float h = 0.1f;
            floor.transform.position = new Vector3(centerXZ.x, h * 0.5f + 0.01f, centerXZ.y);
            floor.transform.localScale = new Vector3(footprint, h, footprint);
            SetColor(floor, tint);
            _spawnedObjects.Add(floor);

            // 记录 RoomInfo
            var info = new RoomInfo
            {
                RoomId = roomId,
                Bounds = new Rect(centerXZ.x - footprint * 0.5f, centerXZ.y - footprint * 0.5f, footprint, footprint),
                CenterWorld = new Vector3(centerXZ.x, 0f, centerXZ.y),
                NodeType = type,
                Size = size,
                ThemeMetadata = $"Tint=#{(byte)(tint.r * 255):X2}{(byte)(tint.g * 255):X2}{(byte)(tint.b * 255):X2}",
            };
            _rooms.Add(info);
        }

        void DestroySpawnedObjects()
        {
            foreach (var go in _spawnedObjects)
            {
                if (go != null) UnityEngine.Object.Destroy(go);
            }
            _spawnedObjects.Clear();
        }

        // ===================================================================
        //  缩圈调度
        // ===================================================================

        void CacheZonePhases()
        {
            _phaseRows.Clear();
            // 按 Id 升序加入（Id 即 Phase 编号）
            var pairs = new List<KeyValuePair<int, ZoneShrinkConfigRow>>(_zoneShrinkConfig.All);
            pairs.Sort((a, b) => a.Key.CompareTo(b.Key));
            foreach (var kv in pairs)
                _phaseRows.Add(kv.Value);
        }

        void StartZoneShrink()
        {
            _zoneElapsed = 0f;
            _currentPhase = -1;
            _zoneRunning = _phaseRows.Count > 0;

            if (_zoneRunning)
            {
                // 立即发 Phase 0
                _currentPhase = 0;
                PublishZonePhase(0);
            }
        }

        int ResolveCurrentPhase(float elapsed)
        {
            // _phaseRows 已按 Id 升序排序；StartTime 是该阶段进入时刻（秒）
            int result = 0;
            for (int i = 0; i < _phaseRows.Count; i++)
            {
                if (elapsed >= _phaseRows[i].StartTime)
                    result = i;
                else
                    break;
            }
            return result;
        }

        void PublishZonePhase(int phaseIdx)
        {
            var row = _phaseRows[phaseIdx];
            _bus.Publish(new ZoneShrinkPhaseEvent
            {
                Phase = phaseIdx,
                Center = _initialZoneCenter, // MVP：圈心暂不偏移（Phase1 偏移留 TODO）
                TargetRadius = row.TargetRadius,
                Duration = row.Duration,
                OutZoneDamage = row.OutZoneDamage,
            });
            FrameworkLogger.Info("MapGenModule",
                $"Action=ZonePhase Phase={phaseIdx} StartTime={row.StartTime} " +
                $"TargetRadius={row.TargetRadius} Duration={row.Duration} OutZoneDamage={row.OutZoneDamage}");
        }

        // ===================================================================
        //  辅助
        // ===================================================================

        static void SetColor(GameObject go, Color color)
        {
            var rd = go.GetComponent<Renderer>();
            if (rd == null) return;
            var sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null) sh = Shader.Find("Standard");
            var mat = new Material(sh);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color"))     mat.SetColor("_Color", color);
            rd.material = mat;
        }
    }
}
