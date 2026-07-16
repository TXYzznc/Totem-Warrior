using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace PCGMap
{
    [Serializable]
    public sealed class PCGWorldProfileCatalog
    {
        public const string ResourcePath = "PCG/WorldGenerationProfiles";

        public int schemaVersion;
        public string catalogId;
        public List<PCGThemeWorldProfile> themes = new();

        public PCGThemeWorldProfile Resolve(int themeId)
        {
            for (int i = 0; i < themes.Count; i++)
            {
                if (themes[i] != null && themes[i].themeId == themeId)
                {
                    return themes[i];
                }
            }

            return themes.Count > 0 ? themes[0] : null;
        }

        public static PCGWorldProfileCatalog LoadFromResources()
        {
            var asset = Resources.Load<TextAsset>(ResourcePath);
            string json = asset == null ? null : asset.text;

#if UNITY_EDITOR
            if (string.IsNullOrEmpty(json))
            {
                var editorAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Resources/PCG/WorldGenerationProfiles.json");
                json = editorAsset == null ? null : editorAsset.text;
            }
#endif

            if (string.IsNullOrEmpty(json))
            {
                string path = Path.Combine(Directory.GetCurrentDirectory(), "Assets/Resources/PCG/WorldGenerationProfiles.json");
                if (File.Exists(path))
                {
                    json = File.ReadAllText(path);
                }
            }

            if (string.IsNullOrWhiteSpace(json))
            {
                throw new InvalidOperationException("PCG world generation profile catalog was not found.");
            }

            return FromJson(json);
        }

        public static PCGWorldProfileCatalog FromJson(string json)
        {
            var catalog = JsonConvert.DeserializeObject<PCGWorldProfileCatalog>(json);
            if (catalog == null || catalog.themes == null || catalog.themes.Count == 0)
            {
                throw new InvalidOperationException("PCG world generation profile catalog contains no themes.");
            }

            return catalog;
        }
    }

    [Serializable]
    public sealed class PCGThemeWorldProfile
    {
        public int themeId;
        public string themeKey;
        public string biomeId;
        public string version;
        public string baseTerrain;
        public List<PCGTerrainProfile> terrains = new();
        public List<PCGTerrainFeatureRecipe> features = new();
        public List<PCGEventLayoutRule> events = new();
        public PCGVisualPlacementProfile visualPlacement = new();

        public PCGTerrainProfile FindTerrain(string terrainId)
        {
            for (int i = 0; i < terrains.Count; i++)
            {
                if (string.Equals(terrains[i]?.terrainId, terrainId, StringComparison.Ordinal))
                {
                    return terrains[i];
                }
            }

            return null;
        }
    }

    [Serializable]
    public sealed class PCGTerrainProfile
    {
        public string terrainId;
        public float minAreaRatio;
        public float maxAreaRatio = 1f;
        public string visualRole;
        public string[] futureCapabilities;
    }

    [Serializable]
    public sealed class PCGTerrainFeatureRecipe
    {
        public string id;
        public string operation;
        public string terrainId;
        public string sourceTerrain;
        public int minCount = 1;
        public int maxCount = 1;
        public int minRadius = 1;
        public int maxRadius = 3;
        public int width = 1;
        public int edgeMargin = 2;
        public float noise = 0.2f;
        public int priority;
    }

    [Serializable]
    public sealed class PCGEventLayoutRule
    {
        public string eventType;
        public string visualRole;
        public int minCount;
        public int maxCount;
        public float minSpacingCells = 2f;
        public int edgeMargin = 2;
        public Dictionary<string, float> terrainAffinity = new();
        public Dictionary<string, float> regionAffinity = new();
    }

    /// <summary>仅控制静态视觉的构图密度；不改变通行、交互或事件数量。</summary>
    [Serializable]
    public sealed class PCGVisualPlacementProfile
    {
        [Range(0f, 1f)] public float ambientRatio = 0.58f;
        [Range(0f, 1f)] public float clusterDensityBias = 0.75f;
        [Min(1)] public int clusterRadius = 3;
        [Min(0f)] public float eventClearanceCells = 2f;
    }
}
