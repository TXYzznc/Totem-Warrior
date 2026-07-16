using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace PCGMap
{
    [Serializable]
    public sealed class PCGSizeDefinition
    {
        public int width = 1;
        public int height = 1;
    }

    [Serializable]
    public sealed class TerrainVisualEntry
    {
        public string id;
        public string asset;
        public List<string> variants = new();
        public string biome;
        public string terrain;
        public string useCase;
        public int weight = 1;
        public string[] tags;
    }

    [Serializable]
    public sealed class TerrainVisualCatalog
    {
        public int schemaVersion;
        public string catalogId;
        public string assetPathMode;
        public int pixelsPerUnit = 128;
        public List<TerrainVisualEntry> tiles = new();
    }

    [Serializable]
    public sealed class WorldObjectEntry
    {
        public string id;
        public string asset;
        public string objectRole;
        public string[] allowedBiomes;
        public string[] allowedTerrains;
        public PCGSizeDefinition footprint;
        public int weight = 1;
        public bool allowOnNonWalkable;
        public float scaleMultiplier = 1f;
        public string[] tags;
    }

    [Serializable]
    public sealed class WorldAnchorVisualEntry
    {
        public string id;
        public int themeId;
        public string anchorId;
        public string eventType;
        public string visualRole;
        public string asset;
        public PCGSizeDefinition footprint;
        public float offsetX;
        public float offsetZ;
        public float scaleMultiplier = 1f;
        public int sortingOffset;
        public string[] tags;
    }

    [Serializable]
    public sealed class WorldObjectCatalog
    {
        public int schemaVersion;
        public string catalogId;
        public string assetPathMode;
        public int pixelsPerUnit = 128;
        public List<WorldObjectEntry> objects = new();
        public List<WorldAnchorVisualEntry> anchorVisuals = new();
    }

    public sealed class PCGAssetIndex
    {
        public const string ConfigResourcesRoot = "PCG";
        public const string GameSpriteRoot = "Assets/Game";

        readonly Dictionary<string, WorldAnchorVisualEntry> _visualByAnchor = new();
        readonly Dictionary<string, List<WorldAnchorVisualEntry>> _visualsByRole = new();

        public TerrainVisualCatalog TerrainCatalog { get; private set; }
        public WorldObjectCatalog ObjectCatalog { get; private set; }
        public IReadOnlyList<WorldObjectEntry> Objects
        {
            get { return ObjectCatalog?.objects ?? (IReadOnlyList<WorldObjectEntry>)Array.Empty<WorldObjectEntry>(); }
        }

        public static PCGAssetIndex LoadFromConfig(
            string terrainCatalogPath = ConfigResourcesRoot + "/TerrainVisualCatalog",
            string objectCatalogPath = ConfigResourcesRoot + "/WorldObjectCatalog")
        {
            return FromJson(LoadConfigText(terrainCatalogPath), LoadConfigText(objectCatalogPath));
        }

        public static PCGAssetIndex FromJson(string terrainJson, string objectJson)
        {
            var index = new PCGAssetIndex
            {
                TerrainCatalog = JsonConvert.DeserializeObject<TerrainVisualCatalog>(terrainJson) ?? new TerrainVisualCatalog(),
                ObjectCatalog = JsonConvert.DeserializeObject<WorldObjectCatalog>(objectJson) ?? new WorldObjectCatalog(),
            };
            index.BuildLookup();
            return index;
        }

        public TerrainVisualEntry PickTerrain(string terrain, string useCase, string biome, System.Random random)
        {
            TerrainVisualEntry fallback = null;
            int total = 0;
            for (int i = 0; i < TerrainCatalog.tiles.Count; i++)
            {
                var entry = TerrainCatalog.tiles[i];
                if (entry == null || !string.Equals(entry.terrain, terrain, StringComparison.Ordinal)) continue;
                if (!string.IsNullOrEmpty(entry.biome) && !string.Equals(entry.biome, biome, StringComparison.Ordinal)) continue;
                if (!string.Equals(entry.useCase, useCase, StringComparison.Ordinal)) { fallback ??= entry; continue; }
                total += Mathf.Max(1, entry.weight);
            }
            if (total <= 0) return fallback;
            int roll = random.Next(total);
            for (int i = 0; i < TerrainCatalog.tiles.Count; i++)
            {
                var entry = TerrainCatalog.tiles[i];
                if (entry == null || !string.Equals(entry.terrain, terrain, StringComparison.Ordinal) ||
                    (!string.IsNullOrEmpty(entry.biome) && !string.Equals(entry.biome, biome, StringComparison.Ordinal)) ||
                    !string.Equals(entry.useCase, useCase, StringComparison.Ordinal)) continue;
                roll -= Mathf.Max(1, entry.weight);
                if (roll < 0) return entry;
            }
            return fallback;
        }

        public string PickTerrainAsset(TerrainVisualEntry entry, System.Random random)
        {
            if (entry == null) return string.Empty;
            int variants = entry.variants?.Count ?? 0;
            return variants <= 0 ? entry.asset ?? string.Empty : entry.variants[random.Next(variants)] ?? entry.asset ?? string.Empty;
        }

        public bool TryGetAnchorVisual(int themeId, string anchorId, out WorldAnchorVisualEntry entry)
        {
            return _visualByAnchor.TryGetValue(AnchorKey(themeId, anchorId), out entry);
        }

        public bool TryGetAnchorVisual(int themeId, string anchorId, string visualRole, out WorldAnchorVisualEntry entry)
        {
            if (TryGetAnchorVisual(themeId, anchorId, out entry)) return true;
            if (!_visualsByRole.TryGetValue(AnchorKey(themeId, visualRole), out var choices) || choices.Count == 0)
            {
                entry = null;
                return false;
            }
            entry = choices[StableHash(anchorId) % choices.Count];
            return true;
        }

        public static string NormalizeGameAssetPath(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath)) return string.Empty;
            string result = assetPath.Replace('\\', '/').Trim();
            if (!result.StartsWith("Assets/", StringComparison.Ordinal)) result = GameSpriteRoot + "/" + result;
            return Path.HasExtension(result) ? result : result + ".png";
        }

        public static Sprite LoadGameSprite(string assetPath, Vector2 pivot, float pixelsPerUnit, out bool createdFromTexture)
        {
            createdFromTexture = false;
#if UNITY_EDITOR
            string path = NormalizeGameAssetPath(assetPath);
            var sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null) return sprite;
            var texture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture != null)
            {
                createdFromTexture = true;
                return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), pivot, pixelsPerUnit, 0, SpriteMeshType.FullRect);
            }
#endif
            return null;
        }

        void BuildLookup()
        {
            foreach (var visual in ObjectCatalog.anchorVisuals)
            {
                if (visual == null) continue;
                if (!string.IsNullOrEmpty(visual.anchorId)) _visualByAnchor[AnchorKey(visual.themeId, visual.anchorId)] = visual;
                AddVisualRole(visual.themeId, visual.visualRole, visual);
                AddVisualRole(visual.themeId, visual.eventType, visual);
                if (visual.tags != null) for (int i = 0; i < visual.tags.Length; i++) AddVisualRole(visual.themeId, visual.tags[i], visual);
            }
        }

        void AddVisualRole(int themeId, string role, WorldAnchorVisualEntry visual)
        {
            if (string.IsNullOrWhiteSpace(role)) return;
            string key = AnchorKey(themeId, role);
            if (!_visualsByRole.TryGetValue(key, out var list)) { list = new List<WorldAnchorVisualEntry>(); _visualsByRole[key] = list; }
            list.Add(visual);
        }

        static string LoadConfigText(string resourcePath)
        {
            var asset = Resources.Load<TextAsset>(resourcePath);
            if (asset != null) return asset.text;
#if UNITY_EDITOR
            var editorAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>($"Assets/Resources/{resourcePath}.json");
            if (editorAsset != null) return editorAsset.text;
#endif
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "Assets/Resources/" + resourcePath + ".json");
            if (File.Exists(filePath)) return File.ReadAllText(filePath);
            throw new InvalidOperationException($"PCG catalog not found: {resourcePath}");
        }

        static string AnchorKey(int themeId, string key) => $"{themeId}|{key ?? string.Empty}";
        static int StableHash(string value)
        {
            unchecked
            {
                uint hash = 2166136261;
                for (int i = 0; i < (value?.Length ?? 0); i++) { hash ^= value[i]; hash *= 16777619; }
                return (int)(hash & 0x7fffffff);
            }
        }
    }
}
