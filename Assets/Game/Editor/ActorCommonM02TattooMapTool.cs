#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Exports TattooMaps exclusively from hand-authored regions. A map pixel stores local part UV
/// in R/G, part id in B, and the approved skin mask in A.
/// </summary>
internal static class ActorCommonM02TattooMapTool
{
    private const int FramePixels = 512;
    private const int PartCount = 6;
    private const string SpriteDirectory = "Assets/Game/Sprites/Actors/ActorCommonM02";
    private const string MapSetDirectory = "Assets/Game/Config/TattooVisual";
    private const string MapSetPath = MapSetDirectory + "/ActorCommonM02RollRightTattooMapSet.asset";
    private const string AuthoringAssetPath = MapSetDirectory + "/ActorCommonM02TattooRegionAuthoring.asset";

    internal static void GenerateCurrentDirectionTattooMaps(string direction)
    {
        direction = NormalizeDirection(direction);
        TattooMapRegionAuthoringAsset authoring = LoadAuthoringOrThrow();
        List<TattooMapFrameAuthoring> frames = GetAuthoredFrames(authoring, direction);
        if (frames.Count == 0)
        {
            throw new InvalidOperationException("当前方向没有已手工标记的帧：" + direction);
        }

        GenerateFrames(direction, frames);
    }

    internal static void GenerateAllAuthoredTattooMaps()
    {
        TattooMapRegionAuthoringAsset authoring = LoadAuthoringOrThrow();
        var directions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int markedFrameCount = 0;
        for (int index = 0; index < authoring.Frames.Count; index++)
        {
            TattooMapFrameAuthoring frame = authoring.Frames[index];
            if (frame == null || frame.regions == null || frame.regions.Count == 0 || !IsSupportedDirection(frame.direction))
            {
                continue;
            }

            directions.Add(frame.direction);
            markedFrameCount++;
        }

        if (directions.Count == 0)
        {
            throw new InvalidOperationException("没有已手工标记的 TattooMap 帧可生成。");
        }

        foreach (string direction in directions)
        {
            GenerateFrames(direction, GetAuthoredFrames(authoring, direction));
        }

        Debug.Log("Generated authored ActorCommonM02 TattooMaps: " + markedFrameCount + " frame(s), " + directions.Count + " direction(s).");
    }

    internal static void ValidateCurrentDirectionTattooMaps(string direction)
    {
        direction = NormalizeDirection(direction);
        TattooMapRegionAuthoringAsset authoring = LoadAuthoringOrThrow();
        List<TattooMapFrameAuthoring> frames = GetAuthoredFrames(authoring, direction);
        if (frames.Count == 0)
        {
            throw new InvalidOperationException("当前方向没有可验证的已手工标记帧：" + direction);
        }

        TotemTattooFrameMapSet mapSet = AssetDatabase.LoadAssetAtPath<TotemTattooFrameMapSet>(MapSetPath);
        if (mapSet == null)
        {
            throw new InvalidOperationException("TattooMap set is missing.");
        }

        for (int index = 0; index < frames.Count; index++)
        {
            TattooMapFrameAuthoring frame = frames[index];
            ValidateFrameBinding(mapSet, GetSourcePath(frame.action, direction, frame.frame), GetDirectionMapPath(frame.action, direction, frame.frame));
        }

        Debug.Log("ActorCommonM02 authored TattooMap validation passed for " + direction + ": " + frames.Count + " frame(s).");
    }

    private static void GenerateFrames(string direction, List<TattooMapFrameAuthoring> frames)
    {
        EnsureFolder(SpriteDirectory + "/TattooMaps");
        var changedImporterPaths = new List<string>(frames.Count);
        var mapPaths = new List<string>(frames.Count);
        try
        {
            for (int index = 0; index < frames.Count; index++)
            {
                TattooMapFrameAuthoring frame = frames[index];
                string sourcePath = GetSourcePath(frame.action, direction, frame.frame);
                EnsureReadableSource(sourcePath, changedImporterPaths);

                Texture2D source = AssetDatabase.LoadAssetAtPath<Texture2D>(sourcePath);
                if (source == null || source.width != FramePixels || source.height != FramePixels)
                {
                    throw new InvalidOperationException("Expected a readable 512x512 source frame: " + sourcePath);
                }

                Color32[] sourcePixels = source.GetPixels32();
                var mapPixels = new Color32[sourcePixels.Length];
                WriteFrameMap(sourcePixels, mapPixels, GetAuthoredRegions(frame), frame.skinTolerance);

                EnsureFolder(GetDirectionActionDirectory(direction, frame.action));
                string mapPath = GetDirectionMapPath(frame.action, direction, frame.frame);
                WritePng(mapPath, mapPixels);
                mapPaths.Add(mapPath);
            }
        }
        finally
        {
            RestoreReadability(changedImporterPaths);
        }

        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        for (int index = 0; index < mapPaths.Count; index++)
        {
            ConfigureTattooMapImporter(mapPaths[index]);
        }

        CreateOrUpdateMapSet();
        AssetDatabase.SaveAssets();
        ValidateCurrentDirectionTattooMaps(direction);
        Debug.Log("Generated authored ActorCommonM02 TattooMaps for " + direction + ": " + frames.Count + " frame(s).");
    }

    private static TattooMapRegionAuthoringAsset LoadAuthoringOrThrow()
    {
        TattooMapRegionAuthoringAsset authoring = AssetDatabase.LoadAssetAtPath<TattooMapRegionAuthoringAsset>(AuthoringAssetPath);
        if (authoring == null)
        {
            throw new InvalidOperationException("No TattooMap authoring asset exists. Mark at least one frame before generating.");
        }

        return authoring;
    }

    private static List<TattooMapFrameAuthoring> GetAuthoredFrames(TattooMapRegionAuthoringAsset authoring, string direction)
    {
        var result = new List<TattooMapFrameAuthoring>();
        for (int index = 0; index < authoring.Frames.Count; index++)
        {
            TattooMapFrameAuthoring frame = authoring.Frames[index];
            if (frame != null && string.Equals(frame.direction, direction, StringComparison.OrdinalIgnoreCase) && frame.regions != null && frame.regions.Count > 0)
            {
                result.Add(frame);
            }
        }

        return result;
    }

    private static void EnsureReadableSource(string sourcePath, List<string> changedImporterPaths)
    {
        var importer = AssetImporter.GetAtPath(sourcePath) as TextureImporter;
        if (importer == null)
        {
            throw new InvalidOperationException("M02 source texture is missing: " + sourcePath);
        }

        if (!importer.isReadable)
        {
            importer.isReadable = true;
            importer.SaveAndReimport();
            changedImporterPaths.Add(sourcePath);
        }
    }

    private static void RestoreReadability(List<string> changedImporterPaths)
    {
        for (int index = 0; index < changedImporterPaths.Count; index++)
        {
            var importer = AssetImporter.GetAtPath(changedImporterPaths[index]) as TextureImporter;
            if (importer != null)
            {
                importer.isReadable = false;
                importer.SaveAndReimport();
            }
        }
    }

    private static void WriteFrameMap(Color32[] sourcePixels, Color32[] mapPixels, TattooRegion[] regions, float skinTolerance)
    {
        for (int imageY = 0; imageY < FramePixels; imageY++)
        {
            int textureY = FramePixels - 1 - imageY;
            for (int x = 0; x < FramePixels; x++)
            {
                int pixelIndex = textureY * FramePixels + x;
                if (!IsSkinPixel(sourcePixels[pixelIndex], skinTolerance))
                {
                    continue;
                }

                for (int regionIndex = 0; regionIndex < regions.Length; regionIndex++)
                {
                    TattooRegion region = regions[regionIndex];
                    if (region.TryMap(x, imageY, out float u, out float v))
                    {
                        mapPixels[pixelIndex] = new Color32(ToByte(u), ToByte(v), (byte)region.PartId, 255);
                        break;
                    }
                }
            }
        }
    }

    private static TattooRegion[] GetAuthoredRegions(TattooMapFrameAuthoring frame)
    {
        var result = new List<TattooRegion>(frame.regions.Count);
        for (int index = 0; index < frame.regions.Count; index++)
        {
            TattooMapRegionAuthoring region = frame.regions[index];
            if (region == null || region.partId < 1 || region.partId > PartCount)
            {
                continue;
            }

            Vector2[] points = TattooMapRegionAuthoringGeometry.GetCorners(region);
            if (points.Length >= 3)
            {
                result.Add(new TattooRegion(region.partId, points));
            }
        }

        return result.ToArray();
    }

    private static bool IsSkinPixel(Color32 color, float tolerance)
    {
        if (color.a < 16)
        {
            return false;
        }

        tolerance = Mathf.Clamp(tolerance, -0.12f, 0.12f);
        float red = color.r / 255f;
        float green = color.g / 255f;
        float blue = color.b / 255f;
        return red > 0.30f - tolerance &&
               green > 0.16f - tolerance &&
               red > green * (1.10f - tolerance * 0.5f) &&
               green > blue * (1.08f - tolerance * 0.4f) &&
               red - blue > 0.20f - tolerance;
    }

    private static void ConfigureTattooMapImporter(string mapPath)
    {
        var importer = AssetImporter.GetAtPath(mapPath) as TextureImporter;
        if (importer == null)
        {
            throw new InvalidOperationException("Could not configure TattooMap importer: " + mapPath);
        }

        importer.textureType = TextureImporterType.Default;
        importer.sRGBTexture = false;
        importer.alphaIsTransparency = false;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = FramePixels;
        importer.isReadable = false;
        importer.SaveAndReimport();
    }

    private static TotemTattooFrameMapSet CreateOrUpdateMapSet()
    {
        EnsureFolder(MapSetDirectory);
        TotemTattooFrameMapSet mapSet = AssetDatabase.LoadAssetAtPath<TotemTattooFrameMapSet>(MapSetPath);
        if (mapSet == null)
        {
            mapSet = ScriptableObject.CreateInstance<TotemTattooFrameMapSet>();
            AssetDatabase.CreateAsset(mapSet, MapSetPath);
        }

        var bindings = new List<TotemTattooFrameBinding>();
        TattooMapRegionAuthoringAsset authoring = AssetDatabase.LoadAssetAtPath<TattooMapRegionAuthoringAsset>(AuthoringAssetPath);
        if (authoring != null)
        {
            for (int index = 0; index < authoring.Frames.Count; index++)
            {
                TattooMapFrameAuthoring frame = authoring.Frames[index];
                if (frame == null || frame.regions == null || frame.regions.Count == 0 || !IsSupportedDirection(frame.direction))
                {
                    continue;
                }

                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(GetSourcePath(frame.action, frame.direction, frame.frame));
                Texture2D map = AssetDatabase.LoadAssetAtPath<Texture2D>(GetDirectionMapPath(frame.action, frame.direction, frame.frame));
                if (sprite != null && map != null)
                {
                    bindings.Add(new TotemTattooFrameBinding { sprite = sprite, tattooMap = map });
                }
            }
        }

        var serializedMapSet = new SerializedObject(mapSet);
        SerializedProperty serializedBindings = serializedMapSet.FindProperty("bindings");
        if (serializedBindings == null)
        {
            throw new InvalidOperationException("TattooMap set does not expose its bindings field.");
        }

        serializedBindings.arraySize = bindings.Count;
        for (int index = 0; index < bindings.Count; index++)
        {
            SerializedProperty binding = serializedBindings.GetArrayElementAtIndex(index);
            binding.FindPropertyRelative("sprite").objectReferenceValue = bindings[index].sprite;
            binding.FindPropertyRelative("tattooMap").objectReferenceValue = bindings[index].tattooMap;
        }

        serializedMapSet.ApplyModifiedPropertiesWithoutUndo();
        mapSet.SetBindings(bindings.ToArray());
        EditorUtility.SetDirty(mapSet);
        return mapSet;
    }

    private static void ValidateFrameBinding(TotemTattooFrameMapSet mapSet, string sourcePath, string mapPath)
    {
        Texture2D source = AssetDatabase.LoadAssetAtPath<Texture2D>(sourcePath);
        Texture2D map = AssetDatabase.LoadAssetAtPath<Texture2D>(mapPath);
        var importer = AssetImporter.GetAtPath(mapPath) as TextureImporter;
        if (source == null || map == null || source.width != FramePixels || source.height != FramePixels || map.width != FramePixels || map.height != FramePixels)
        {
            throw new InvalidOperationException("TattooMap size mismatch: " + mapPath);
        }

        if (importer == null || importer.sRGBTexture || importer.mipmapEnabled || importer.textureCompression != TextureImporterCompression.Uncompressed || importer.filterMode != FilterMode.Point)
        {
            throw new InvalidOperationException("TattooMap importer must be linear, point-filtered, non-mipmapped and uncompressed: " + mapPath);
        }

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(sourcePath);
        if (!mapSet.TryGetTattooMap(sprite, out Texture2D boundMap) || boundMap != map)
        {
            throw new InvalidOperationException("TattooMap binding is missing or incorrect for " + sourcePath);
        }
    }

    private static string NormalizeDirection(string direction)
    {
        if (!IsSupportedDirection(direction))
        {
            throw new ArgumentOutOfRangeException(nameof(direction), direction, "Unsupported TattooMap direction.");
        }

        return direction.Trim().ToLowerInvariant();
    }

    private static bool IsSupportedDirection(string direction)
    {
        return string.Equals(direction, "down", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(direction, "up", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(direction, "left", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(direction, "right", StringComparison.OrdinalIgnoreCase);
    }

    private static void EnsureFolder(string assetFolder)
    {
        if (AssetDatabase.IsValidFolder(assetFolder))
        {
            return;
        }

        string parent = Path.GetDirectoryName(assetFolder)?.Replace('\\', '/');
        string folderName = Path.GetFileName(assetFolder);
        if (string.IsNullOrWhiteSpace(parent) || string.IsNullOrWhiteSpace(folderName))
        {
            throw new InvalidOperationException("Cannot create TattooMap folder: " + assetFolder);
        }

        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, folderName);
    }

    private static string GetSourcePath(string action, string direction, int frame)
    {
        return SpriteDirectory + "/actor_common_m02_" + action + "_" + direction + "_" + frame.ToString("00") + ".png";
    }

    private static string GetDirectionActionDirectory(string direction, string action)
    {
        return SpriteDirectory + "/TattooMaps/" + direction + "/" + action;
    }

    private static string GetDirectionMapPath(string action, string direction, int frame)
    {
        return GetDirectionActionDirectory(direction, action) + "/actor_common_m02_" + action + "_" + direction + "_" + frame.ToString("00") + "_tattoo_map.png";
    }

    private static void WritePng(string destination, Color32[] pixels)
    {
        var texture = new Texture2D(FramePixels, FramePixels, TextureFormat.RGBA32, false, true);
        texture.SetPixels32(pixels);
        texture.Apply(false, false);
        File.WriteAllBytes(Path.GetFullPath(destination), texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);
    }

    private static byte ToByte(float value)
    {
        return (byte)Mathf.Clamp(Mathf.RoundToInt(Mathf.Clamp01(value) * 255f), 0, 255);
    }

    private readonly struct TattooRegion
    {
        public readonly int PartId;
        private readonly Vector2[] points;

        public TattooRegion(int partId, Vector2[] sourcePoints)
        {
            PartId = partId;
            points = (Vector2[])sourcePoints.Clone();
        }

        public bool TryMap(float x, float y, out float u, out float v)
        {
            Vector2 point = new Vector2(x, y);
            if (points.Length == 4)
            {
                if (TryGetBarycentric(point, points[0], points[1], points[2], out Vector3 first))
                {
                    u = first.y + first.z;
                    v = first.z;
                    return true;
                }

                if (TryGetBarycentric(point, points[0], points[2], points[3], out Vector3 second))
                {
                    u = second.y;
                    v = second.y + second.z;
                    return true;
                }
            }

            return TattooMapRegionAuthoringGeometry.TryMapPolygon(point, points, out u, out v);
        }

        private static bool TryGetBarycentric(Vector2 point, Vector2 a, Vector2 b, Vector2 c, out Vector3 barycentric)
        {
            float denominator = (b.y - c.y) * (a.x - c.x) + (c.x - b.x) * (a.y - c.y);
            if (Mathf.Abs(denominator) < 0.0001f)
            {
                barycentric = Vector3.zero;
                return false;
            }

            float first = ((b.y - c.y) * (point.x - c.x) + (c.x - b.x) * (point.y - c.y)) / denominator;
            float second = ((c.y - a.y) * (point.x - c.x) + (a.x - c.x) * (point.y - c.y)) / denominator;
            float third = 1f - first - second;
            barycentric = new Vector3(first, second, third);
            const float edgeEpsilon = -0.0001f;
            return first >= edgeEpsilon && second >= edgeEpsilon && third >= edgeEpsilon;
        }
    }
}
#endif
