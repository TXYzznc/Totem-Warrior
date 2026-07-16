using System.Collections.Generic;
using UnityEngine;

namespace PCGMap
{
    public enum PCGPlacedVisualKind
    {
        Object,
    }

    public struct PCGMapCell
    {
        public int X;
        public int Y;
        public string Biome;
        public string Terrain;
        public bool Walkable;
        public bool Occupied;
        public string ZoneId;
        public string BaseAsset;
    }

    public sealed class PCGPlacedVisual
    {
        public string Id;
        public string Asset;
        public PCGPlacedVisualKind Kind;
        public int X;
        public int Y;
        public int Width = 1;
        public int Height = 1;
        public float OffsetX;
        public float OffsetY;
        public float RotationDegrees;
        public int SortingOrder;
        public bool HasSortingOrder;
        public bool BlocksMovement;
        public bool BlocksSight;
        /// <summary>0 表示沿用该可视类型的默认缩放比例。</summary>
        public float ScaleMultiplier;
        public string Role;
    }

    public sealed class PCGValidationReport
    {
        public bool IsValid;
        public int WalkableCells;
        public int ReachableCells;
        public int UnreachableCells;
        public int BlockingObjects;
        public int ResourceObjects;
        public int PoiCount;
        public readonly List<string> Warnings = new();
    }

    public sealed class PCGMapDiagnosticStep
    {
        public string Name;
        public long ElapsedMs;
        public string Detail;
    }

    public sealed class PCGMapDiagnostics
    {
        public long TotalMs;
        public readonly List<PCGMapDiagnosticStep> Steps = new();

        public void AddStep(string name, long elapsedMs, string detail)
        {
            Steps.Add(new PCGMapDiagnosticStep
            {
                Name = name,
                ElapsedMs = elapsedMs,
                Detail = detail,
            });
        }
    }

    public sealed class PCGMapData
    {
        public int Width;
        public int Height;
        public int Seed;
        public PCGMapCell[] Cells;
        public readonly List<PCGPlacedVisual> Visuals = new();
        /// <summary>逻辑世界计划；地图渲染只是它的一个消费者。</summary>
        public PCGWorldPlan WorldPlan;
        public PCGValidationReport Validation;
        public readonly PCGMapDiagnostics Diagnostics = new();
        public ulong ContentHash;

        public PCGMapCell GetCell(int x, int y) => Cells[y * Width + x];
        public void SetCell(int x, int y, PCGMapCell cell) => Cells[y * Width + x] = cell;
    }

    public sealed class PCGMapGenerateRequest
    {
        public int Seed = 1001;
        public int Width = 64;
        public int Height = 64;
        public int ObjectBudget = 160;
        public int ThemeId = 1;
    }

    internal static class PCGHash
    {
        public static float Value01(int x, int y, int seed, int salt)
        {
            unchecked
            {
                uint h = (uint)(seed * 374761393 + salt * 668265263);
                h ^= (uint)(x * 2246822519);
                h = (h << 13) | (h >> 19);
                h ^= (uint)(y * 3266489917);
                h *= 1274126177;
                h ^= h >> 16;
                return (h & 0x00FFFFFF) / 16777215f;
            }
        }

        public static float SmoothValue01(float x, float y, int seed, int salt)
        {
            int x0 = Mathf.FloorToInt(x);
            int y0 = Mathf.FloorToInt(y);
            int x1 = x0 + 1;
            int y1 = y0 + 1;
            float tx = SmoothStep(x - x0);
            float ty = SmoothStep(y - y0);

            float a = Value01(x0, y0, seed, salt);
            float b = Value01(x1, y0, seed, salt);
            float c = Value01(x0, y1, seed, salt);
            float d = Value01(x1, y1, seed, salt);
            return Mathf.Lerp(Mathf.Lerp(a, b, tx), Mathf.Lerp(c, d, tx), ty);
        }

        static float SmoothStep(float t) => t * t * (3f - 2f * t);

        public static ulong Combine(ulong hash, ulong value)
        {
            unchecked
            {
                hash ^= value + 0x9e3779b97f4a7c15UL + (hash << 6) + (hash >> 2);
                return hash;
            }
        }
    }
}
