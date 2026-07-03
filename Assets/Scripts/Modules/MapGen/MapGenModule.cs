using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using MapGen.Data;
using MapGen.Events;
using MapGen.Generation;
using MapGen.Rendering;
using UnityEngine;

namespace MapGen
{
    /// <summary>
    /// 地图生成模块（区域生长数据版）。
    ///
    /// 完整版职责（见 GDD-v2/modules/07-MapGenModule.md）：
    /// Run 开始时一次性完成大地图生成：区域生长纯数据 → 关键点位保底分配 →
    /// 缩圈中心计算 → 发布 MapGeneratedEvent。Tilemap/美术替换在后续阶段接入。
    ///
    /// 【当前简化路径】
    /// - 不再做 BSP 分割，改为 RegionGrowthGenerator 生成 TerrainType 网格
    /// - 不烘焙 NavMesh（agent 直接 transform 更新，后续接入 NavMeshSurface 时再补）
    /// - 不做逐格美术资源加载（只创建地面、边界墙、功能点标记）
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
        MapTerrainRenderer _terrainRenderer;
        MapGridData _currentGrid;
        int _currentSeed;
        int _currentThemeId;
        float _mapSize = 400f;
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
        /// <summary>地图根边界（单位 m）</summary>
        public float MapSize => _mapSize;
        /// <summary>最近一次区域生长生成的纯数据网格。</summary>
        public MapGridData CurrentGrid => _currentGrid;

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
        /// 实现：区域生长生成纯数据网格 + 功能点兼容 Rooms + 边界预览。发布 MapGeneratedEvent。
        /// </summary>
        public void GenerateMap(int seed, int themeId)
        {
            DestroySpawnedObjects();
            _rooms.Clear();
            _currentGrid = null;

            _currentSeed = seed;
            _currentThemeId = themeId;

            // 读模板（themeId 无匹配时降级）
            var templateRow = ResolveTemplate(themeId);
            _mapSize = templateRow != null ? templateRow.MapSize : 400f;

            var generationConfig = MapGenerationConfig.CreateDefault(_mapSize, cellSize: 2f);
            var generator = new RegionGrowthGenerator();
            _currentGrid = generator.Generate(seed, generationConfig);
            BuildRoomsFromFeaturePoints(_currentGrid);
            BuildRegionGrowthPreviewGeometry(_currentGrid);
            EnsureTerrainRenderer().Render(_currentGrid);

            // 用 System.Random(seed) — 禁止 UnityEngine.Random（伪联机→真联机迁移要求）
            var rng = new System.Random(seed);

            // === 缩圈中心（地图中央 1/3 区域 + 小扰动） ===
            float third = _mapSize / 3f;
            float centerMin = third;
            float centerMax = _mapSize - third;
            float jitterX = (float)rng.NextDouble() * (centerMax - centerMin) + centerMin;
            float jitterY = (float)rng.NextDouble() * (centerMax - centerMin) + centerMin;
            _initialZoneCenter = new Vector2(jitterX, jitterY);

            // === NavMesh bake — 当前阶段跳过 ===
            // TODO(v2.1): 集成 NavMeshSurface.BuildNavMeshAsync()，目标 ≤ 1.5s

            // 发布事件（此时已不在 InitializeAsync 内，可以发）
            _bus.Publish(new MapGeneratedEvent
            {
                Seed = seed,
                ThemeId = themeId,
                Rooms = new List<RoomInfo>(_rooms), // 复制一份避免外部修改
                InitialZoneCenter = _initialZoneCenter,
                MapSize = _mapSize,
                GridData = _currentGrid,
                CellSize = _currentGrid.CellSize,
            });

            FrameworkLogger.Info("MapGenModule",
                $"Action=MapGenerated Seed={seed} ThemeId={themeId} Rooms={_rooms.Count} " +
                $"Grid={_currentGrid.Width}x{_currentGrid.Height} Cell={_currentGrid.CellSize} " +
                $"ZoneCenter=({_initialZoneCenter.x:F1},{_initialZoneCenter.y:F1}) Size={_mapSize}");
            LogGenerationWarnings(_currentGrid);

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

        void BuildRegionGrowthPreviewGeometry(MapGridData grid)
        {
            // 当前阶段只建极少量调试对象。正式地形由 MapTerrainRenderer 走 Tilemap 批量铺设。
            BuildBoundaryWalls();

            foreach (var point in grid.FeaturePoints)
                AddFeaturePointMarker(point);
        }

        void BuildRoomsFromFeaturePoints(MapGridData grid)
        {
            for (int i = 0; i < grid.FeaturePoints.Count; i++)
            {
                var point = grid.FeaturePoints[i];
                float footprint = point.PointType == FeaturePointType.Boss ? 34f : 24f;
                _rooms.Add(new RoomInfo
                {
                    RoomId = i,
                    Bounds = new Rect(
                        point.WorldPosition.x - footprint * 0.5f,
                        point.WorldPosition.z - footprint * 0.5f,
                        footprint,
                        footprint),
                    CenterWorld = point.WorldPosition,
                    NodeType = ToRoomNodeType(point.PointType),
                    Size = point.PointType == FeaturePointType.Boss ? SizeCategory.Large : SizeCategory.Medium,
                    ThemeMetadata = $"FeaturePoint={point.PointType};Terrain={point.PreferredTerrain}",
                });
            }
        }

        static RoomNodeType ToRoomNodeType(FeaturePointType pointType)
        {
            return pointType switch
            {
                FeaturePointType.Spawn => RoomNodeType.SpawnRoom,
                FeaturePointType.Boss => RoomNodeType.BossRoom,
                FeaturePointType.Merchant => RoomNodeType.Merchant,
                FeaturePointType.TattooStudio => RoomNodeType.TattooStudio,
                _ => RoomNodeType.Normal,
            };
        }

        void AddFeaturePointMarker(MapFeaturePoint point)
        {
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.name = $"MapGen_Feature_{point.PointType}";
            marker.transform.position = point.WorldPosition + Vector3.up * 0.55f;
            marker.transform.localScale = new Vector3(8f, 1.1f, 8f);
            SetColor(marker, GetFeaturePointColor(point.PointType));
            _spawnedObjects.Add(marker);
        }

        static Color GetFeaturePointColor(FeaturePointType pointType)
        {
            return pointType switch
            {
                FeaturePointType.Spawn => new Color(0.30f, 0.70f, 1.00f),
                FeaturePointType.Boss => new Color(0.95f, 0.30f, 0.30f),
                FeaturePointType.Merchant => new Color(1.00f, 0.85f, 0.30f),
                FeaturePointType.TattooStudio => new Color(0.85f, 0.45f, 0.90f),
                _ => Color.white,
            };
        }

        void LogGenerationWarnings(MapGridData grid)
        {
            if (grid.Warnings == null || grid.Warnings.Count == 0)
                return;

            for (int i = 0; i < grid.Warnings.Count; i++)
                FrameworkLogger.Warn("MapGenModule", $"Action=MapGenerationWarning {grid.Warnings[i]}");
        }

        MapTerrainRenderer EnsureTerrainRenderer()
        {
            if (_terrainRenderer != null)
                return _terrainRenderer;

            var rendererGo = new GameObject("MapGen_TerrainRenderer");
            rendererGo.transform.SetParent(null, worldPositionStays: true);
            _terrainRenderer = rendererGo.AddComponent<MapTerrainRenderer>();
            _terrainRenderer.AutoSubscribeToMapGeneratedEvent = false;
            _spawnedObjects.Add(rendererGo);
            return _terrainRenderer;
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
