using System;
using System.Collections.Generic;

namespace MapGen.Data
{
    public sealed class TerrainTypeDefinition
    {
        public TerrainType Type;
        public string TypeName;
        public string TileAssetKey;
        public bool IsWalkable;
        public float MoveSpeedMul;
        public int GrowthWeight;
        public bool IsFallback;
    }

    public sealed class TerrainAdjacencyRule
    {
        public TerrainType FromType;
        public TerrainType ToType;
        public bool Allowed;
    }

    public sealed class FeatureInjectionDefinition
    {
        public int Id;
        public string FeatureName;
        public TerrainType TerrainType;
        public FeatureSpreadMode SpreadMode;
        public int CountMin;
        public int CountMax;
        public int SizeMin;
        public int SizeMax;
    }

    public sealed class FeaturePointDefinition
    {
        public int Id;
        public FeaturePointType PointType;
        public bool Required;
        public float MinSpacing;
        public float SafeMargin;
        public TerrainType PreferredTerrain;
    }

    public sealed class MapGenerationConfig
    {
        public float MapSize = 400f;
        public float CellSize = 2f;
        public List<TerrainTypeDefinition> Terrains = new();
        public List<TerrainAdjacencyRule> AdjacencyRules = new();
        public List<FeatureInjectionDefinition> Features = new();
        public List<FeaturePointDefinition> FeaturePoints = new();

        public static MapGenerationConfig CreateDefault(float mapSize = 400f, float cellSize = 2f)
        {
            var config = new MapGenerationConfig
            {
                MapSize = mapSize,
                CellSize = cellSize,
            };

            config.Terrains.AddRange(new[]
            {
                Terrain(TerrainType.Grass, "Grass", "Sprite/Map/AI_RUINS_WETLAND/Terrain/Grass", true, 1f, 60, true),
                Terrain(TerrainType.RuinFloor, "RuinFloor", "Sprite/Map/AI_RUINS_WETLAND/Terrain/RuinFloor", true, 1f, 35),
                Terrain(TerrainType.Shore, "Shore", "Sprite/Map/AI_RUINS_WETLAND/Terrain/Shore", true, 0.95f, 24),
                Terrain(TerrainType.Water, "Water", "Sprite/Map/AI_RUINS_WETLAND/Terrain/Water", false, 0f, 8),
                Terrain(TerrainType.Swamp, "Swamp", "Sprite/Map/AI_RUINS_WETLAND/Terrain/Swamp", true, 0.65f, 16),
                Terrain(TerrainType.Wasteland, "Wasteland", "Sprite/Map/AI_RUINS_WETLAND/Terrain/Wasteland", true, 0.9f, 20),
                Terrain(TerrainType.Mountain, "Mountain", "Sprite/Map/AI_RUINS_WETLAND/Terrain/Mountain", false, 0f, 6),
                Terrain(TerrainType.Forest, "Forest", "Sprite/Map/AI_RUINS_WETLAND/Terrain/Forest", true, 0.85f, 16),
            });

            AddSymmetric(config, TerrainType.Water, TerrainType.Water);
            AddSymmetric(config, TerrainType.Water, TerrainType.Shore);
            foreach (TerrainType type in Enum.GetValues(typeof(TerrainType)))
            {
                AddSymmetric(config, TerrainType.Shore, type);
            }

            AddClique(config, TerrainType.Grass, TerrainType.RuinFloor, TerrainType.Swamp, TerrainType.Wasteland,
                TerrainType.Forest);
            AddClique(config, TerrainType.Grass, TerrainType.RuinFloor, TerrainType.Wasteland, TerrainType.Mountain);

            config.Features.AddRange(new[]
            {
                Feature(1, "BrokenCreek", TerrainType.Water, FeatureSpreadMode.Line, 1, 2, 10, 24),
                Feature(2, "WetlandPatch", TerrainType.Swamp, FeatureSpreadMode.Blob, 3, 6, 5, 12),
                Feature(3, "RubbleRidge", TerrainType.Mountain, FeatureSpreadMode.Blob, 2, 4, 4, 10),
                Feature(4, "DeadZone", TerrainType.Wasteland, FeatureSpreadMode.Blob, 2, 5, 6, 14),
            });

            config.FeaturePoints.AddRange(new[]
            {
                Point(2, FeaturePointType.Boss, true, 120f, 36f, TerrainType.RuinFloor),
                Point(3, FeaturePointType.Merchant, true, 80f, 24f, TerrainType.RuinFloor),
                Point(4, FeaturePointType.TattooStudio, true, 80f, 24f, TerrainType.RuinFloor),
            });

            return config;
        }

        static TerrainTypeDefinition Terrain(TerrainType type, string name, string tileAssetKey, bool walkable,
            float moveSpeedMul, int weight, bool fallback = false)
        {
            return new TerrainTypeDefinition
            {
                Type = type,
                TypeName = name,
                TileAssetKey = tileAssetKey,
                IsWalkable = walkable,
                MoveSpeedMul = moveSpeedMul,
                GrowthWeight = weight,
                IsFallback = fallback,
            };
        }

        static FeatureInjectionDefinition Feature(int id, string name, TerrainType terrainType,
            FeatureSpreadMode spreadMode, int countMin, int countMax, int sizeMin, int sizeMax)
        {
            return new FeatureInjectionDefinition
            {
                Id = id,
                FeatureName = name,
                TerrainType = terrainType,
                SpreadMode = spreadMode,
                CountMin = countMin,
                CountMax = countMax,
                SizeMin = sizeMin,
                SizeMax = sizeMax,
            };
        }

        static FeaturePointDefinition Point(int id, FeaturePointType pointType, bool required, float minSpacing,
            float safeMargin, TerrainType preferredTerrain)
        {
            return new FeaturePointDefinition
            {
                Id = id,
                PointType = pointType,
                Required = required,
                MinSpacing = minSpacing,
                SafeMargin = safeMargin,
                PreferredTerrain = preferredTerrain,
            };
        }

        static void AddSymmetric(MapGenerationConfig config, TerrainType a, TerrainType b)
        {
            config.AdjacencyRules.Add(new TerrainAdjacencyRule { FromType = a, ToType = b, Allowed = true });
            config.AdjacencyRules.Add(new TerrainAdjacencyRule { FromType = b, ToType = a, Allowed = true });
        }

        static void AddClique(MapGenerationConfig config, params TerrainType[] terrainTypes)
        {
            for (int i = 0; i < terrainTypes.Length; i++)
            {
                for (int j = i; j < terrainTypes.Length; j++)
                {
                    AddSymmetric(config, terrainTypes[i], terrainTypes[j]);
                }
            }
        }
    }
}
