using MapGen.Data;
using MapGen.Generation;
using MapGen.Rendering;
using UnityEngine;

namespace MapGen.Sandbox
{
    /// <summary>
    /// 区域生长调试驱动。用单张贴图预览 TerrainType 网格，不逐格创建 GameObject。
    /// 按键输入通过 InputModule；无 InputModule 时仍可由按钮/测试调用 RegenerateNextSeed。
    /// </summary>
    public sealed class MapGenSandboxDriver : MonoBehaviour
    {
        [SerializeField] int seed = 1;
        [SerializeField] float mapSize = 100f;
        [SerializeField] float cellSize = 2f;
        [SerializeField] bool useTilemapPreview = true;
        [SerializeField] Renderer targetRenderer;
        [SerializeField] MapTerrainRenderer tilemapRenderer;

        Texture2D _texture;
        Material _runtimeMaterial;
        InputModule _input;
        bool _triedResolveInput;

        public MapGridData CurrentGrid { get; private set; }
        public int Seed => seed;

        void Start()
        {
            Generate(seed);
        }

        void Update()
        {
            if (_input == null && !_triedResolveInput)
                TryResolveInputModule();

            if (_input != null && _input.IsDebugKeyPressed())
                RegenerateNextSeed();
        }

        void OnDestroy()
        {
            if (_texture != null)
                Destroy(_texture);
            if (_runtimeMaterial != null)
                Destroy(_runtimeMaterial);
        }

        public void RegenerateNextSeed()
        {
            Generate(seed + 1);
        }

        public void Generate(int nextSeed)
        {
            seed = nextSeed;
            var config = MapGenerationConfig.CreateDefault(mapSize, cellSize);
            var generator = new RegionGrowthGenerator();
            CurrentGrid = generator.Generate(seed, config);
            Render(CurrentGrid);
        }

        void Render(MapGridData grid)
        {
            if (useTilemapPreview)
            {
                EnsureTilemapRenderer().Render(grid);
                if (targetRenderer != null)
                    targetRenderer.gameObject.SetActive(false);
                return;
            }

            EnsureRenderer();
            targetRenderer.gameObject.SetActive(true);
            EnsureTexture(grid.Width, grid.Height);

            for (int y = 0; y < grid.Height; y++)
            {
                for (int x = 0; x < grid.Width; x++)
                    _texture.SetPixel(x, y, TerrainColor(grid.Grid[x, y]));
            }

            for (int i = 0; i < grid.FeaturePoints.Count; i++)
            {
                var point = grid.FeaturePoints[i];
                PaintFeaturePoint(point.Cell, FeatureColor(point.PointType));
            }

            _texture.Apply(updateMipmaps: false, makeNoLongerReadable: false);
            targetRenderer.sharedMaterial = _runtimeMaterial;
            _runtimeMaterial.mainTexture = _texture;
            targetRenderer.transform.position = new Vector3(grid.MapSize * 0.5f, 0f, grid.MapSize * 0.5f);
            targetRenderer.transform.localScale = new Vector3(grid.MapSize / 10f, 1f, grid.MapSize / 10f);
        }

        MapTerrainRenderer EnsureTilemapRenderer()
        {
            if (tilemapRenderer != null)
                return tilemapRenderer;

            var go = new GameObject("MapGenSandbox_TilemapPreview");
            go.transform.SetParent(transform, worldPositionStays: false);
            tilemapRenderer = go.AddComponent<MapTerrainRenderer>();
            tilemapRenderer.AutoSubscribeToMapGeneratedEvent = false;
            tilemapRenderer.RenderObjectPlacements = false;
            return tilemapRenderer;
        }

        void EnsureRenderer()
        {
            if (targetRenderer != null)
                return;

            var plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            plane.name = "MapGenSandbox_GridPreview";
            plane.transform.SetParent(transform, worldPositionStays: false);
            targetRenderer = plane.GetComponent<Renderer>();
        }

        void EnsureTexture(int width, int height)
        {
            if (_texture != null && _texture.width == width && _texture.height == height)
                return;

            if (_texture != null)
                Destroy(_texture);
            _texture = new Texture2D(width, height, TextureFormat.RGBA32, mipChain: false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };

            if (_runtimeMaterial == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null) shader = Shader.Find("Unlit/Texture");
                if (shader == null) shader = Shader.Find("Standard");
                _runtimeMaterial = new Material(shader)
                {
                    name = "MapGenSandbox_GridPreview_Material",
                };
            }
        }

        void PaintFeaturePoint(Vector2Int center, Color color)
        {
            const int radius = 2;
            for (int dy = -radius; dy <= radius; dy++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    int x = center.x + dx;
                    int y = center.y + dy;
                    if (x < 0 || y < 0 || x >= _texture.width || y >= _texture.height)
                        continue;

                    _texture.SetPixel(x, y, color);
                }
            }
        }

        void TryResolveInputModule()
        {
            _triedResolveInput = true;
            var app = FindObjectOfType<GameApp>();
            if (app == null || !app.TryGetRuntime(out _, out var runner))
                return;

            try
            {
                _input = runner.GetModule<InputModule>();
            }
            catch
            {
                _input = null;
            }
        }

        static Color TerrainColor(TerrainType terrainType)
        {
            return terrainType switch
            {
                TerrainType.Grass => new Color(0.25f, 0.55f, 0.24f),
                TerrainType.RuinFloor => new Color(0.45f, 0.45f, 0.42f),
                TerrainType.Shore => new Color(0.62f, 0.57f, 0.37f),
                TerrainType.Water => new Color(0.15f, 0.36f, 0.66f),
                TerrainType.Swamp => new Color(0.20f, 0.34f, 0.25f),
                TerrainType.Wasteland => new Color(0.42f, 0.34f, 0.30f),
                TerrainType.Mountain => new Color(0.30f, 0.30f, 0.32f),
                TerrainType.Forest => new Color(0.10f, 0.36f, 0.16f),
                _ => Color.magenta,
            };
        }

        static Color FeatureColor(FeaturePointType pointType)
        {
            return pointType switch
            {
                FeaturePointType.Spawn => Color.cyan,
                FeaturePointType.Boss => Color.red,
                FeaturePointType.Merchant => Color.yellow,
                FeaturePointType.TattooStudio => new Color(1f, 0.35f, 1f),
                _ => Color.white,
            };
        }
    }
}
