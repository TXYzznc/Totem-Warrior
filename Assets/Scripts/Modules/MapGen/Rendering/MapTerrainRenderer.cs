using System;
using System.Collections.Generic;
using MapGen.Data;
using MapGen.Events;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace MapGen.Rendering
{
    /// <summary>
    /// MapGridData 的可替换渲染消费者：Tilemap 铺地面，有限数量 billboard 表现物件。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MapTerrainRenderer : MonoBehaviour
    {
        [SerializeField] Tilemap terrainTilemap;
        [SerializeField] TileBase grassTile;
        [SerializeField] TileBase ruinFloorTile;
        [SerializeField] TileBase shoreTile;
        [SerializeField] TileBase waterTile;
        [SerializeField] TileBase swampTile;
        [SerializeField] TileBase wastelandTile;
        [SerializeField] TileBase mountainTile;
        [SerializeField] TileBase forestTile;
        [SerializeField] Transform objectRoot;
        [SerializeField] float objectBillboardHeight = 2.8f;
        [SerializeField] bool renderObjectPlacements = true;
        [SerializeField] bool autoSubscribeToMapGeneratedEvent = true;

        readonly Dictionary<TerrainType, TileBase[]> _tilesByTerrain = new();
        readonly List<TileBase> _runtimeTiles = new();
        readonly List<GameObject> _spawnedObjects = new();
        IDisposable _mapSubscription;
        Sprite _placeholderSprite;

        public int LastRenderedCellCount { get; private set; }
        public int LastRenderedObjectCount { get; private set; }
        public bool AutoSubscribeToMapGeneratedEvent
        {
            get => autoSubscribeToMapGeneratedEvent;
            set => autoSubscribeToMapGeneratedEvent = value;
        }

        public bool RenderObjectPlacements
        {
            get => renderObjectPlacements;
            set => renderObjectPlacements = value;
        }

        void Awake()
        {
            EnsureTilemap();
            BuildTileCache();
        }

        void Update()
        {
            if (autoSubscribeToMapGeneratedEvent && _mapSubscription == null)
                TrySubscribeToMapEvents();
        }

        void OnDestroy()
        {
            _mapSubscription?.Dispose();
            ClearObjects();
            for (int i = 0; i < _runtimeTiles.Count; i++)
            {
                if (_runtimeTiles[i] != null)
                    DestroyObject(_runtimeTiles[i]);
            }
            _runtimeTiles.Clear();
            if (_placeholderSprite != null)
                DestroyObject(_placeholderSprite.texture);
            if (_placeholderSprite != null)
                DestroyObject(_placeholderSprite);
        }

        public void Render(MapGridData map)
        {
            if (map == null)
            {
                Clear();
                return;
            }

            EnsureTilemap();
            BuildTileCache();
            RenderTerrain(map);
            if (renderObjectPlacements)
                RenderObjects(map);
            else
                ClearObjects();
        }

        public void Clear()
        {
            if (terrainTilemap != null)
                terrainTilemap.ClearAllTiles();
            ClearObjects();
            LastRenderedCellCount = 0;
            LastRenderedObjectCount = 0;
        }

        void RenderTerrain(MapGridData map)
        {
            var bounds = new BoundsInt(0, 0, 0, map.Width, map.Height, 1);
            var tiles = new TileBase[map.Width * map.Height];
            int index = 0;
            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                    tiles[index++] = ResolveTile(map.Grid[x, y], x, y);
            }

            terrainTilemap.ClearAllTiles();
            terrainTilemap.SetTilesBlock(bounds, tiles);
            terrainTilemap.transform.localScale = new Vector3(map.CellSize, map.CellSize, 1f);
            LastRenderedCellCount = tiles.Length;
        }

        void RenderObjects(MapGridData map)
        {
            ClearObjects();
            EnsureObjectRoot();

            for (int i = 0; i < map.ObjectPlacements.Count; i++)
                SpawnBillboard(map.ObjectPlacements[i]);

            LastRenderedObjectCount = _spawnedObjects.Count;
        }

        void SpawnBillboard(MapObjectPlacement placement)
        {
            var go = new GameObject($"MapObject_{placement.Kind}_{_spawnedObjects.Count}");
            go.transform.SetParent(objectRoot, worldPositionStays: false);
            go.transform.position = placement.WorldPosition;
            go.transform.localScale = Vector3.one;

            var spriteRenderer = go.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = LoadSprite(placement.AssetKey);
            spriteRenderer.color = ObjectColor(placement.Kind);

            var billboard = go.AddComponent<global::BillboardSprite>();
            billboard.ApplyTilt(55f);
            go.AddComponent<global::DepthSortedSprite>();

            go.transform.localScale = new Vector3(objectBillboardHeight, objectBillboardHeight, objectBillboardHeight);
            _spawnedObjects.Add(go);
        }

        Sprite LoadSprite(string assetKey)
        {
            if (!string.IsNullOrEmpty(assetKey))
            {
                var sprite = Resources.Load<Sprite>(assetKey);
                if (sprite != null)
                    return sprite;
            }
            return GetPlaceholderSprite();
        }

        Sprite GetPlaceholderSprite()
        {
            if (_placeholderSprite != null)
                return _placeholderSprite;

            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, mipChain: false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            texture.SetPixel(0, 0, Color.white);
            texture.Apply(updateMipmaps: false, makeNoLongerReadable: false);
            _placeholderSprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0f), 1f);
            return _placeholderSprite;
        }

        TileBase ResolveTile(TerrainType terrainType, int x, int y)
        {
            if (!_tilesByTerrain.TryGetValue(terrainType, out var tiles) || tiles == null || tiles.Length == 0)
                tiles = _tilesByTerrain[TerrainType.Grass];

            int index = StableVariantIndex(terrainType, x, y, tiles.Length);
            return tiles[index];
        }

        void EnsureTilemap()
        {
            if (terrainTilemap != null)
                return;

            var gridGo = new GameObject("MapTerrain_Grid");
            gridGo.transform.SetParent(transform, worldPositionStays: false);
            gridGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            gridGo.AddComponent<Grid>();

            var tilemapGo = new GameObject("MapTerrain_Tilemap");
            tilemapGo.transform.SetParent(gridGo.transform, worldPositionStays: false);
            terrainTilemap = tilemapGo.AddComponent<Tilemap>();
            tilemapGo.AddComponent<TilemapRenderer>();
        }

        void EnsureObjectRoot()
        {
            if (objectRoot != null)
                return;

            var root = new GameObject("MapTerrain_Objects");
            root.transform.SetParent(transform, worldPositionStays: false);
            objectRoot = root.transform;
        }

        void BuildTileCache()
        {
            if (_tilesByTerrain.Count > 0)
                return;

            _tilesByTerrain[TerrainType.Grass] = ResolveConfiguredTiles(TerrainType.Grass, grassTile, new Color(0.25f, 0.55f, 0.24f));
            _tilesByTerrain[TerrainType.RuinFloor] = ResolveConfiguredTiles(TerrainType.RuinFloor, ruinFloorTile, new Color(0.45f, 0.45f, 0.42f));
            _tilesByTerrain[TerrainType.Shore] = ResolveConfiguredTiles(TerrainType.Shore, shoreTile, new Color(0.62f, 0.57f, 0.37f));
            _tilesByTerrain[TerrainType.Water] = ResolveConfiguredTiles(TerrainType.Water, waterTile, new Color(0.15f, 0.36f, 0.66f));
            _tilesByTerrain[TerrainType.Swamp] = ResolveConfiguredTiles(TerrainType.Swamp, swampTile, new Color(0.20f, 0.34f, 0.25f));
            _tilesByTerrain[TerrainType.Wasteland] = ResolveConfiguredTiles(TerrainType.Wasteland, wastelandTile, new Color(0.42f, 0.34f, 0.30f));
            _tilesByTerrain[TerrainType.Mountain] = ResolveConfiguredTiles(TerrainType.Mountain, mountainTile, new Color(0.30f, 0.30f, 0.32f));
            _tilesByTerrain[TerrainType.Forest] = ResolveConfiguredTiles(TerrainType.Forest, forestTile, new Color(0.10f, 0.36f, 0.16f));
        }

        TileBase[] ResolveConfiguredTiles(TerrainType terrainType, TileBase configuredTile, Color fallbackColor)
        {
            if (configuredTile != null)
                return new[] { configuredTile };

            var formalSprites = LoadFormalTerrainSprites(terrainType);
            if (formalSprites.Count > 0)
            {
                var tiles = new TileBase[formalSprites.Count];
                for (int i = 0; i < formalSprites.Count; i++)
                    tiles[i] = CreateRuntimeTile($"{terrainType}_{i + 1:00}", formalSprites[i], Color.white);
                return tiles;
            }

            var sprite = Resources.Load<Sprite>($"Sprite/Map/AI_RUINS_WETLAND/Terrain/{terrainType}");
            var tile = sprite != null
                ? CreateRuntimeTile(terrainType.ToString(), sprite, Color.white)
                : CreateRuntimeTile(terrainType.ToString(), GetPlaceholderSprite(), fallbackColor);
            return new[] { tile };
        }

        static List<Sprite> LoadFormalTerrainSprites(TerrainType terrainType)
        {
            var all = Resources.LoadAll<Sprite>("Sprite/Map/AI_RUINS_WETLAND/Formal/Terrain");
            var result = new List<Sprite>();
            string prefix = terrainType + "_";
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i].name.StartsWith(prefix, StringComparison.Ordinal))
                    result.Add(all[i]);
            }
            result.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            return result;
        }

        TileBase CreateRuntimeTile(string tileName, Sprite sprite, Color color)
        {
            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.name = $"MapTerrain_{tileName}_RuntimeTile";
            tile.sprite = sprite;
            tile.color = color;
            _runtimeTiles.Add(tile);
            return tile;
        }

        static int StableVariantIndex(TerrainType terrainType, int x, int y, int count)
        {
            if (count <= 1)
                return 0;

            unchecked
            {
                uint hash = (uint)(x * 73856093) ^ (uint)(y * 19349663) ^ (uint)((int)terrainType * 83492791);
                return (int)(hash % (uint)count);
            }
        }

        void TrySubscribeToMapEvents()
        {
            var app = FindObjectOfType<global::GameApp>();
            if (app == null || !app.TryGetRuntime(out var bus, out _))
                return;

            _mapSubscription = bus.Subscribe<MapGeneratedEvent>(e => Render(e.GridData));
        }

        void ClearObjects()
        {
            for (int i = 0; i < _spawnedObjects.Count; i++)
            {
                if (_spawnedObjects[i] != null)
                    DestroyObject(_spawnedObjects[i]);
            }
            _spawnedObjects.Clear();
            LastRenderedObjectCount = 0;
        }

        static void DestroyObject(UnityEngine.Object obj)
        {
            if (Application.isPlaying)
                Destroy(obj);
            else
                DestroyImmediate(obj);
        }

        static Color ObjectColor(MapObjectKind kind)
        {
            return kind switch
            {
                MapObjectKind.Tree => new Color(0.18f, 0.45f, 0.16f),
                MapObjectKind.Rock => new Color(0.45f, 0.45f, 0.46f),
                MapObjectKind.Reed => new Color(0.42f, 0.54f, 0.23f),
                MapObjectKind.RuinDebris => new Color(0.50f, 0.42f, 0.36f),
                MapObjectKind.FeatureBuilding => new Color(0.80f, 0.72f, 0.55f),
                _ => Color.white,
            };
        }
    }
}
