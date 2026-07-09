using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public sealed class TotemAssetService : TotemRuntimeServiceBase
{
    public const string RuntimeAssetCatalogRelativePath = "GameData/AIData/GameplayCatalogs/totem_runtime_assets.json";

    private readonly Dictionary<string, GameObject> prefabCache = new Dictionary<string, GameObject>(64);
    private readonly Dictionary<string, Texture2D> textureCache = new Dictionary<string, Texture2D>(64);
    private readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>(64);

    public override string ServiceName => "Asset";

    public TotemRuntimeAssetCatalog RuntimeAssetCatalog { get; private set; } = TotemRuntimeAssetCatalog.BuildDefault();

    public bool RuntimeAssetCatalogLoadedFromFile { get; private set; }

    public string RuntimeAssetCatalogPath { get; private set; } = string.Empty;

    public string RuntimeAssetCatalogMessage { get; private set; } = "NotLoaded";

    public int MissingEntryCount { get; private set; }

    public int FallbackRequiredCount { get; private set; }

    public string LastFallbackKey { get; private set; } = string.Empty;

    public string LastFallbackReason { get; private set; } = string.Empty;

    public int CacheHitCount { get; private set; }

    public int CacheMissCount { get; private set; }

    public int CachedAssetCount => prefabCache.Count + textureCache.Count + spriteCache.Count;

    public string LastCacheKey { get; private set; } = string.Empty;

    public string LastCacheKind { get; private set; } = string.Empty;

    protected override void OnInitialize(TotemGameRuntime runtime)
    {
        ReloadRuntimeAssetCatalog();
    }

    public void ReloadRuntimeAssetCatalog()
    {
        RuntimeAssetCatalogPath = GetRuntimeAssetCatalogPath();
        RuntimeAssetCatalogLoadedFromFile = false;
        ResetFallbackCounters();
        ResetAssetCaches();

        if (TryLoadRuntimeAssetCatalogFromFile(RuntimeAssetCatalogPath, out var loadedCatalog, out string error))
        {
            var validationErrors = new List<string>();
            if (TotemRuntimeAssetCatalogValidator.Validate(loadedCatalog, validationErrors))
            {
                RuntimeAssetCatalog = loadedCatalog;
                RuntimeAssetCatalogLoadedFromFile = true;
                RuntimeAssetCatalogMessage = "Loaded";
                GFTrace.Success("TotemAsset", "RuntimeAssetCatalog.Loaded", null, GFTrace.Data(
                    "path", RuntimeAssetCatalogPath,
                    "entryCount", RuntimeAssetCatalog.entries.Length.ToString()));
                return;
            }

            RuntimeAssetCatalogMessage = string.Join("; ", validationErrors);
            GFTrace.Warning("TotemAsset", "RuntimeAssetCatalog.Invalid", null, GFTrace.Data("errors", RuntimeAssetCatalogMessage));
        }
        else
        {
            RuntimeAssetCatalogMessage = error;
            GFTrace.Warning("TotemAsset", "RuntimeAssetCatalog.Missing", null, GFTrace.Data("error", error));
        }

        RuntimeAssetCatalog = TotemRuntimeAssetCatalog.BuildDefault();
        RuntimeAssetCatalog.Normalize();
        GFTrace.Warning("TotemAsset", "RuntimeAssetCatalog.FallbackDefault", null, GFTrace.Data("reason", RuntimeAssetCatalogMessage));
    }

    public bool TryInstantiateGameObject(string key, Transform parent, Vector3 position, Vector3 fallbackScale, out GameObject instance)
    {
        instance = null;
        if (!RuntimeAssetCatalog.TryGetEntry(key, out var entry))
        {
            RecordMissingEntry(key);
            return false;
        }

#if UNITY_EDITOR
        if (!prefabCache.TryGetValue(key, out var prefab))
        {
            RecordCacheMiss(key, "Prefab");
            prefab = LoadEditorAsset<GameObject>(entry.activeAssetPath);
            if (prefab == null && !string.IsNullOrWhiteSpace(entry.legacySourcePath))
            {
                prefab = LoadEditorAsset<GameObject>(entry.legacySourcePath);
            }

            if (prefab != null)
            {
                prefabCache[key] = prefab;
            }
        }
        else
        {
            RecordCacheHit(key, "Prefab");
        }

        if (prefab != null)
        {
            instance = UnityEngine.Object.Instantiate(prefab, parent);
            instance.transform.position = position;
            instance.transform.localScale = entry.Scale == Vector3.one ? fallbackScale : entry.Scale;
            ApplyTint(instance, entry.tint);
            GFTrace.Info("TotemAsset", "Instantiate.EditorAsset", null, GFTrace.Data(
                "key", key,
                "asset", entry.activeAssetPath));
            return true;
        }
#endif

        RecordFallbackRequired(key, "Instantiate");
        GFTrace.Warning("TotemAsset", "Instantiate.FallbackRequired", null, GFTrace.Data(
            "key", key,
            "asset", entry.activeAssetPath,
            "fallbackPrimitive", entry.fallbackPrimitive));
        return false;
    }

    public bool TryLoadTexture(string key, out Texture2D texture)
    {
        texture = null;
        if (!RuntimeAssetCatalog.TryGetEntry(key, out var entry))
        {
            RecordMissingEntry(key);
            return false;
        }

#if UNITY_EDITOR
        if (!textureCache.TryGetValue(key, out texture))
        {
            RecordCacheMiss(key, "Texture");
            texture = LoadEditorAsset<Texture2D>(entry.activeAssetPath);
            if (texture == null && !string.IsNullOrWhiteSpace(entry.legacySourcePath))
            {
                texture = LoadEditorAsset<Texture2D>(entry.legacySourcePath);
            }

            if (texture != null)
            {
                textureCache[key] = texture;
            }
        }
        else
        {
            RecordCacheHit(key, "Texture");
        }

        if (texture != null)
        {
            GFTrace.Info("TotemAsset", "Texture.EditorAsset", null, GFTrace.Data(
                "key", key,
                "asset", string.IsNullOrWhiteSpace(entry.activeAssetPath) ? entry.legacySourcePath : entry.activeAssetPath));
            return true;
        }
#endif

        RecordFallbackRequired(key, "Texture");
        GFTrace.Warning("TotemAsset", "Texture.FallbackRequired", null, GFTrace.Data(
            "key", key,
            "asset", entry.activeAssetPath,
            "fallbackPrimitive", entry.fallbackPrimitive));
        return false;
    }

    public bool TryLoadSprite(string key, out Sprite sprite)
    {
        sprite = null;
        if (!RuntimeAssetCatalog.TryGetEntry(key, out var entry))
        {
            RecordMissingEntry(key);
            return false;
        }

#if UNITY_EDITOR
        if (!spriteCache.TryGetValue(key, out sprite))
        {
            RecordCacheMiss(key, "Sprite");
            sprite = LoadEditorSprite(entry.activeAssetPath);
            if (sprite == null && !string.IsNullOrWhiteSpace(entry.legacySourcePath))
            {
                sprite = LoadEditorSprite(entry.legacySourcePath);
            }

            if (sprite != null)
            {
                spriteCache[key] = sprite;
            }
        }
        else
        {
            RecordCacheHit(key, "Sprite");
        }

        if (sprite != null)
        {
            GFTrace.Info("TotemAsset", "Sprite.EditorAsset", null, GFTrace.Data(
                "key", key,
                "asset", string.IsNullOrWhiteSpace(entry.activeAssetPath) ? entry.legacySourcePath : entry.activeAssetPath));
            return true;
        }
#endif

        RecordFallbackRequired(key, "Sprite");
        GFTrace.Warning("TotemAsset", "Sprite.FallbackRequired", null, GFTrace.Data(
            "key", key,
            "asset", entry.activeAssetPath,
            "fallbackPrimitive", entry.fallbackPrimitive));
        return false;
    }

    public bool TryCreateTexturedMaterial(string key, Color fallbackColor, out Material material)
    {
        material = null;
        if (!RuntimeAssetCatalog.TryGetEntry(key, out var entry))
        {
            RecordMissingEntry(key);
            return false;
        }

        Color tint = ParseColor(entry.tint, fallbackColor);
        if (!TryLoadTexture(key, out var texture))
        {
            return false;
        }

        material = CreateMaterial(tint);
        if (material == null)
        {
            return false;
        }

        if (material.HasProperty("_BaseMap"))
        {
            material.SetTexture("_BaseMap", texture);
        }

        if (material.HasProperty("_MainTex"))
        {
            material.SetTexture("_MainTex", texture);
        }

        return true;
    }

    public static string GetRuntimeAssetCatalogPath()
    {
        string projectRoot = GetProjectRoot();
        return Path.GetFullPath(Path.Combine(projectRoot, RuntimeAssetCatalogRelativePath));
    }

    public static bool TryLoadRuntimeAssetCatalogFromFile(string fileName, out TotemRuntimeAssetCatalog catalog, out string error)
    {
        catalog = null;
        error = string.Empty;
        try
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                error = "File path is empty.";
                return false;
            }

            if (!File.Exists(fileName))
            {
                error = "File does not exist.";
                return false;
            }

            string json = File.ReadAllText(fileName, Encoding.UTF8);
            if (string.IsNullOrWhiteSpace(json))
            {
                error = "File is empty.";
                return false;
            }

            catalog = JsonUtility.FromJson<TotemRuntimeAssetCatalog>(json);
            if (catalog == null)
            {
                error = "JsonUtility returned null.";
                return false;
            }

            catalog.Normalize();
            return true;
        }
        catch (Exception exception)
        {
            error = exception.Message;
            catalog = null;
            return false;
        }
    }

    private static void ApplyTint(GameObject instance, string colorText)
    {
        if (instance == null || string.IsNullOrWhiteSpace(colorText) || !ColorUtility.TryParseHtmlString(colorText, out var color))
        {
            return;
        }

        var spriteRenderers = instance.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            spriteRenderers[i].color = color;
        }
    }

    private static Material CreateMaterial(Color color)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        if (shader == null)
        {
            return null;
        }

        var material = new Material(shader);
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

        return material;
    }

    private static Color ParseColor(string colorText, Color fallbackColor)
    {
        if (!string.IsNullOrWhiteSpace(colorText) && ColorUtility.TryParseHtmlString(colorText, out var color))
        {
            return color;
        }

        return fallbackColor;
    }

    private void RecordMissingEntry(string key)
    {
        MissingEntryCount++;
        LastFallbackKey = key ?? string.Empty;
        LastFallbackReason = "MissingEntry";
        GFTrace.Warning("TotemAsset", "Catalog.MissingEntry", null, GFTrace.Data("key", LastFallbackKey));
    }

    private void RecordFallbackRequired(string key, string reason)
    {
        FallbackRequiredCount++;
        LastFallbackKey = key ?? string.Empty;
        LastFallbackReason = reason ?? string.Empty;
    }

    private void ResetFallbackCounters()
    {
        MissingEntryCount = 0;
        FallbackRequiredCount = 0;
        LastFallbackKey = string.Empty;
        LastFallbackReason = string.Empty;
    }

    private void RecordCacheHit(string key, string kind)
    {
        CacheHitCount++;
        LastCacheKey = key ?? string.Empty;
        LastCacheKind = kind ?? string.Empty;
    }

    private void RecordCacheMiss(string key, string kind)
    {
        CacheMissCount++;
        LastCacheKey = key ?? string.Empty;
        LastCacheKind = kind ?? string.Empty;
    }

    private void ResetAssetCaches()
    {
        prefabCache.Clear();
        textureCache.Clear();
        spriteCache.Clear();
        CacheHitCount = 0;
        CacheMissCount = 0;
        LastCacheKey = string.Empty;
        LastCacheKind = string.Empty;
    }

#if UNITY_EDITOR
    private static T LoadEditorAsset<T>(string assetPath) where T : UnityEngine.Object
    {
        return string.IsNullOrWhiteSpace(assetPath) ? null : AssetDatabase.LoadAssetAtPath<T>(assetPath);
    }

    private static Sprite LoadEditorSprite(string assetPath)
    {
        var sprite = LoadEditorAsset<Sprite>(assetPath);
        if (sprite != null)
        {
            return sprite;
        }

        if (string.IsNullOrWhiteSpace(assetPath))
        {
            return null;
        }

        var subAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        for (int i = 0; i < subAssets.Length; i++)
        {
            if (subAssets[i] is Sprite subSprite)
            {
                return subSprite;
            }
        }

        var texture = LoadEditorAsset<Texture2D>(assetPath);
        if (texture == null)
        {
            return null;
        }

        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
    }
#endif

    private static string GetProjectRoot()
    {
        var assetsDirectory = Directory.GetParent(Application.dataPath);
        return assetsDirectory == null ? Directory.GetCurrentDirectory() : assetsDirectory.FullName;
    }
}
