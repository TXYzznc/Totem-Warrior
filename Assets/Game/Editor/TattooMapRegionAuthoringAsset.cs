using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Editor-only source of truth for hand-authored TattooMap regions. Coordinates are in source
/// image pixels with a top-left origin, matching the marker window and the source PNGs.
/// </summary>
public sealed class TattooMapRegionAuthoringAsset : ScriptableObject
{
    [SerializeField] private List<TattooMapFrameAuthoring> frames = new List<TattooMapFrameAuthoring>();

    public IReadOnlyList<TattooMapFrameAuthoring> Frames => frames;

    public TattooMapFrameAuthoring GetOrCreateFrame(string action, string direction, int frame)
    {
        TattooMapFrameAuthoring result = FindFrame(action, direction, frame);
        if (result != null)
        {
            return result;
        }

        result = new TattooMapFrameAuthoring
        {
            action = action,
            direction = direction,
            frame = frame,
        };
        frames.Add(result);
        return result;
    }

    public TattooMapFrameAuthoring FindFrame(string action, string direction, int frame)
    {
        for (int index = 0; index < frames.Count; index++)
        {
            TattooMapFrameAuthoring candidate = frames[index];
            if (candidate != null && candidate.action == action && candidate.direction == direction && candidate.frame == frame)
            {
                return candidate;
            }
        }

        return null;
    }
}

[Serializable]
public sealed class TattooMapFrameAuthoring
{
    public string action;
    public string direction;
    public int frame;
    // Per-frame skin colour tolerance. 0 keeps the project default; positive values relax the
    // colour gate and negative values make it stricter. Transparency is never relaxed.
    [Range(-0.12f, 0.12f)] public float skinTolerance;
    public List<TattooMapRegionAuthoring> regions = new List<TattooMapRegionAuthoring>();

    public TattooMapRegionAuthoring FindRegion(int partId)
    {
        for (int index = 0; index < regions.Count; index++)
        {
            TattooMapRegionAuthoring region = regions[index];
            if (region != null && region.partId == partId)
            {
                return region;
            }
        }

        return null;
    }

    public void ReplaceRegion(TattooMapRegionAuthoring value)
    {
        for (int index = 0; index < regions.Count; index++)
        {
            if (regions[index] != null && regions[index].partId == value.partId)
            {
                regions[index] = value;
                return;
            }
        }

        regions.Add(value);
    }
}

public enum TattooMapRegionShape
{
    CenterLine,
    OrientedRectangle,
    Polygon,
}

[Serializable]
public sealed class TattooMapRegionAuthoring
{
    [Range(1, 6)] public int partId;
    public TattooMapRegionShape shape;

    // CenterLine uses start/end and width. OrientedRectangle uses center/size/rotationDegrees.
    // Polygon stores clockwise or counter-clockwise image-space vertices from the pen tool.
    public Vector2 start;
    public Vector2 end;
    [Min(1f)] public float width = 36f;
    public Vector2 center;
    public Vector2 size = new Vector2(48f, 72f);
    public float rotationDegrees;
    public List<Vector2> points = new List<Vector2>();
}

/// <summary>Shared geometric conversion used by the marker UI and TattooMap generator.</summary>
public static class TattooMapRegionAuthoringGeometry
{
    public static Vector2[] GetCorners(TattooMapRegionAuthoring region)
    {
        if (region.shape == TattooMapRegionShape.Polygon)
        {
            return region.points == null ? Array.Empty<Vector2>() : region.points.ToArray();
        }

        if (region.shape == TattooMapRegionShape.CenterLine)
        {
            Vector2 direction = region.end - region.start;
            if (direction.sqrMagnitude < 0.0001f)
            {
                direction = Vector2.down;
            }

            direction.Normalize();
            Vector2 perpendicular = new Vector2(-direction.y, direction.x) * (Mathf.Max(1f, region.width) * 0.5f);
            return new[]
            {
                region.start - perpendicular,
                region.start + perpendicular,
                region.end + perpendicular,
                region.end - perpendicular,
            };
        }

        float radians = region.rotationDegrees * Mathf.Deg2Rad;
        Vector2 right = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * (Mathf.Max(1f, region.size.x) * 0.5f);
        Vector2 down = new Vector2(-Mathf.Sin(radians), Mathf.Cos(radians)) * (Mathf.Max(1f, region.size.y) * 0.5f);
        return new[]
        {
            region.center - right - down,
            region.center + right - down,
            region.center + right + down,
            region.center - right + down,
        };
    }

    public static bool Contains(TattooMapRegionAuthoring region, float x, float y, out float u, out float v)
    {
        Vector2[] corners = GetCorners(region);
        if (corners.Length < 3)
        {
            u = 0f;
            v = 0f;
            return false;
        }

        if (region.shape == TattooMapRegionShape.Polygon)
        {
            return TryMapPolygon(new Vector2(x, y), corners, out u, out v);
        }

        return TryMapQuad(new Vector2(x, y), corners[0], corners[1], corners[2], corners[3], out u, out v);
    }

    /// <summary>
    /// A pen region uses its bounding box for local tattoo UVs. The polygon itself remains the
    /// authoritative clip mask, so texture patterning cannot escape the artist-drawn contour.
    /// </summary>
    public static bool TryMapPolygon(Vector2 point, Vector2[] points, out float u, out float v)
    {
        if (points == null || points.Length < 3 || !ContainsPoint(points, point))
        {
            u = 0f;
            v = 0f;
            return false;
        }

        Vector2 min = points[0];
        Vector2 max = points[0];
        for (int index = 1; index < points.Length; index++)
        {
            min = Vector2.Min(min, points[index]);
            max = Vector2.Max(max, points[index]);
        }

        u = Mathf.InverseLerp(min.x, Mathf.Max(min.x + 0.0001f, max.x), point.x);
        v = Mathf.InverseLerp(min.y, Mathf.Max(min.y + 0.0001f, max.y), point.y);
        return true;
    }

    public static bool ContainsPoint(Vector2[] points, Vector2 point)
    {
        bool inside = false;
        for (int index = 0, previous = points.Length - 1; index < points.Length; previous = index++)
        {
            Vector2 current = points[index];
            Vector2 prior = points[previous];
            bool crosses = (current.y > point.y) != (prior.y > point.y);
            if (crosses && point.x < (prior.x - current.x) * (point.y - current.y) / (prior.y - current.y) + current.x)
            {
                inside = !inside;
            }
        }

        return inside;
    }

    private static bool TryMapQuad(Vector2 point, Vector2 upperOuter, Vector2 upperInner, Vector2 lowerInner, Vector2 lowerOuter, out float u, out float v)
    {
        if (TryGetBarycentric(point, upperOuter, upperInner, lowerInner, out Vector3 first))
        {
            u = first.y + first.z;
            v = first.z;
            return true;
        }

        if (TryGetBarycentric(point, upperOuter, lowerInner, lowerOuter, out Vector3 second))
        {
            u = second.y;
            v = second.y + second.z;
            return true;
        }

        u = 0f;
        v = 0f;
        return false;
    }

    private static bool TryGetBarycentric(Vector2 point, Vector2 a, Vector2 b, Vector2 c, out Vector3 result)
    {
        Vector2 v0 = b - a;
        Vector2 v1 = c - a;
        Vector2 v2 = point - a;
        float denominator = v0.x * v1.y - v1.x * v0.y;
        if (Mathf.Abs(denominator) < 0.0001f)
        {
            result = default;
            return false;
        }

        float inverse = 1f / denominator;
        float bWeight = (v2.x * v1.y - v1.x * v2.y) * inverse;
        float cWeight = (v0.x * v2.y - v2.x * v0.y) * inverse;
        float aWeight = 1f - bWeight - cWeight;
        result = new Vector3(aWeight, bWeight, cWeight);
        const float epsilon = -0.0001f;
        return aWeight >= epsilon && bWeight >= epsilon && cWeight >= epsilon;
    }
}
