using System;
using UnityEngine;

namespace PCGMap
{
    /// <summary>
    /// 正式地图的地貌布局。每个主题只产出四类基础地貌，资源变体和边界装饰由目录配置决定。
    /// </summary>
    public enum PCGThemeTerrainRole
    {
        Primary,
        Secondary,
        Accent,
        Water,
    }

    public static class PCGThemeMapLayout
    {
        public static PCGThemeTerrainRole[,] Generate(int themeId, int width, int height, int seed)
        {
            width = Mathf.Clamp(width, 8, 128);
            height = Mathf.Clamp(height, 8, 128);
            var terrain = CreateFilledGrid(width, height, PCGThemeTerrainRole.Primary);
            var random = new System.Random(seed);

            switch (themeId)
            {
                case 2:
                    GenerateAlienHive(terrain, random);
                    break;
                case 3:
                    GenerateVirusSwamp(terrain, seed, random);
                    break;
                case 1:
                default:
                    GenerateAiRuins(terrain, random);
                    break;
            }

            return terrain;
        }

        public static string ResolveTerrainId(int themeId, PCGThemeTerrainRole role)
        {
            switch (themeId)
            {
                case 2:
                    return role switch
                    {
                        PCGThemeTerrainRole.Primary => "hive_chitin",
                        PCGThemeTerrainRole.Secondary => "hive_membrane",
                        PCGThemeTerrainRole.Accent => "hive_resin",
                        PCGThemeTerrainRole.Water => "hive_acid",
                        _ => "hive_chitin",
                    };
                case 3:
                    return role switch
                    {
                        PCGThemeTerrainRole.Primary => "swamp_grass",
                        PCGThemeTerrainRole.Secondary => "swamp_mud",
                        PCGThemeTerrainRole.Accent => "swamp_corruption",
                        PCGThemeTerrainRole.Water => "swamp_water",
                        _ => "swamp_grass",
                    };
                case 1:
                default:
                    return role switch
                    {
                        PCGThemeTerrainRole.Primary => "ruins_floor",
                        PCGThemeTerrainRole.Secondary => "ruins_metal",
                        PCGThemeTerrainRole.Accent => "ruins_growth",
                        PCGThemeTerrainRole.Water => "ruins_coolant",
                        _ => "ruins_floor",
                    };
            }
        }

        public static string ResolveBiomeId(int themeId)
        {
            return themeId switch
            {
                2 => "alien_hive",
                3 => "virus_swamp",
                _ => "ai_ruins",
            };
        }

        public static bool IsWalkable(int themeId, PCGThemeTerrainRole role)
        {
            return themeId switch
            {
                // 服务金属区保留为不可穿过的机械障碍；冷却液减速、回收植被提供掩体。
                1 => role != PCGThemeTerrainRole.Secondary,
                // 酸池不可通过，膜层减速、树脂硬壳提供掩体。
                2 => role != PCGThemeTerrainRole.Water,
                // 水域不可通过，泥滩减速、腐化地带提供掩体/危险采样。
                3 => role != PCGThemeTerrainRole.Water,
                _ => true,
            };
        }

        private static PCGThemeTerrainRole[,] CreateFilledGrid(int width, int height, PCGThemeTerrainRole role)
        {
            var terrain = new PCGThemeTerrainRole[width, height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    terrain[x, y] = role;
                }
            }

            return terrain;
        }

        private static void GenerateVirusSwamp(PCGThemeTerrainRole[,] terrain, int seed, System.Random random)
        {
            int width = terrain.GetLength(0);
            int height = terrain.GetLength(1);
            int minimumWaterWidth = Mathf.Clamp(Mathf.Max(2, width / 12), 1, width - 2);
            int maximumWaterWidth = Mathf.Clamp(Mathf.Max(minimumWaterWidth + 1, width / 4), minimumWaterWidth, width - 2);
            bool[,] waterMask = GenerateVariableWaterMask(width, height, minimumWaterWidth, maximumWaterWidth, seed);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (waterMask[x, y])
                    {
                        terrain[x, y] = PCGThemeTerrainRole.Water;
                    }
                }
            }

            // 水域周围留出泥滩，避免大面积草地直接贴水面。
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (terrain[x, y] == PCGThemeTerrainRole.Water)
                    {
                        PaintOrthogonalNeighbours(terrain, x, y, PCGThemeTerrainRole.Primary, PCGThemeTerrainRole.Secondary);
                    }
                }
            }

            int firstCenterX = Mathf.Clamp(width / 3 + random.Next(-Mathf.Max(2, width / 18), Mathf.Max(3, width / 18 + 1)), 2, width - 3);
            int firstCenterY = Mathf.Clamp(height / 3 + random.Next(-Mathf.Max(2, height / 18), Mathf.Max(3, height / 18 + 1)), 2, height - 3);
            int secondCenterX = Mathf.Clamp(width * 2 / 3 + random.Next(-Mathf.Max(2, width / 18), Mathf.Max(3, width / 18 + 1)), 2, width - 3);
            int secondCenterY = Mathf.Clamp(height * 2 / 3 + random.Next(-Mathf.Max(2, height / 18), Mathf.Max(3, height / 18 + 1)), 2, height - 3);
            PaintCircle(terrain, firstCenterX, firstCenterY, Mathf.Max(2, width / 7), PCGThemeTerrainRole.Accent, false);
            PaintCircle(terrain, secondCenterX, secondCenterY, Mathf.Max(2, width / 9), PCGThemeTerrainRole.Accent, false);
        }

        private static void GenerateAiRuins(PCGThemeTerrainRole[,] terrain, System.Random random)
        {
            int width = terrain.GetLength(0);
            int height = terrain.GetLength(1);
            int metalStart = Mathf.Clamp(width / 5 + random.Next(-Mathf.Max(1, width / 32), Mathf.Max(2, width / 32 + 1)), 1, width - 4);
            int growthStart = Mathf.Clamp(width * 3 / 5 + random.Next(-Mathf.Max(1, width / 32), Mathf.Max(2, width / 32 + 1)), 2, width - 4);
            PaintRectangle(terrain, metalStart, 1, Mathf.Max(2, width / 5), height - 2, PCGThemeTerrainRole.Secondary, false);
            PaintRectangle(terrain, growthStart, 1, Mathf.Max(2, width / 5), height - 2, PCGThemeTerrainRole.Accent, false);

            int waterY = Mathf.Clamp(height / 2 + random.Next(-Mathf.Max(1, height / 24), Mathf.Max(2, height / 24 + 1)), 2, height - 3);
            PaintHorizontalChannel(terrain, waterY, Mathf.Max(1, height / 24), PCGThemeTerrainRole.Water);
        }

        private static void GenerateAlienHive(PCGThemeTerrainRole[,] terrain, System.Random random)
        {
            int width = terrain.GetLength(0);
            int height = terrain.GetLength(1);
            int membraneX = Mathf.Clamp(width / 4 + random.Next(-Mathf.Max(1, width / 32), Mathf.Max(2, width / 32 + 1)), 2, width - 3);
            int membraneY = Mathf.Clamp(height * 2 / 3 + random.Next(-Mathf.Max(1, height / 32), Mathf.Max(2, height / 32 + 1)), 2, height - 3);
            int resinX = Mathf.Clamp(width * 3 / 4 + random.Next(-Mathf.Max(1, width / 32), Mathf.Max(2, width / 32 + 1)), 2, width - 3);
            int resinY = Mathf.Clamp(height / 3 + random.Next(-Mathf.Max(1, height / 32), Mathf.Max(2, height / 32 + 1)), 2, height - 3);
            PaintCircle(terrain, membraneX, membraneY, Mathf.Max(2, width / 7), PCGThemeTerrainRole.Secondary, false);
            PaintCircle(terrain, resinX, resinY, Mathf.Max(2, width / 7), PCGThemeTerrainRole.Accent, false);

            int acidY = Mathf.Clamp(height / 2 + random.Next(-Mathf.Max(1, height / 24), Mathf.Max(2, height / 24 + 1)), 2, height - 3);
            PaintHorizontalChannel(terrain, acidY, Mathf.Max(1, height / 24), PCGThemeTerrainRole.Water, Mathf.Max(2, width / 16));
            PaintCircle(terrain, width / 2, acidY, Mathf.Max(2, width / 10), PCGThemeTerrainRole.Water, true);
        }

        private static bool[,] GenerateVariableWaterMask(int width, int height, int minimumWaterWidth, int maximumWaterWidth, int seed)
        {
            var mask = new bool[width, height];
            var random = new System.Random(seed);
            int waterWidth = random.Next(minimumWaterWidth, maximumWaterWidth + 1);
            int previousStartX = (width - waterWidth) / 2;
            int previousEndX = previousStartX + waterWidth - 1;

            int margin = height >= 16 ? 2 : 1;
            int startY = margin;
            int endYExclusive = height - margin;
            for (int y = startY; y < endYExclusive; y++)
            {
                if (y > startY)
                {
                    if (minimumWaterWidth < maximumWaterWidth && (y - startY) % 2 == 0)
                    {
                        int direction = random.Next(0, 2) == 0 ? -1 : 1;
                        int nextWidth = waterWidth + direction;
                        waterWidth = nextWidth < minimumWaterWidth || nextWidth > maximumWaterWidth
                            ? waterWidth - direction
                            : nextWidth;
                    }

                    int desiredStartX = previousStartX + random.Next(-1, 2);
                    int minimumStartX = 1;
                    int maximumStartX = width - waterWidth - 1;
                    int minimumConnectedStartX = previousStartX - waterWidth + 1;
                    int maximumConnectedStartX = previousEndX;
                    int startX = Mathf.Clamp(desiredStartX, minimumStartX, maximumStartX);
                    previousStartX = Mathf.Clamp(startX, minimumConnectedStartX, maximumConnectedStartX);
                    previousEndX = previousStartX + waterWidth - 1;
                }

                for (int offset = 0; offset < waterWidth; offset++)
                {
                    mask[previousStartX + offset, y] = true;
                }
            }

            return mask;
        }

        private static void PaintHorizontalChannel(PCGThemeTerrainRole[,] terrain, int centerY, int halfHeight, PCGThemeTerrainRole role, int horizontalMargin = 0)
        {
            int width = terrain.GetLength(0);
            int height = terrain.GetLength(1);
            for (int y = Mathf.Max(0, centerY - halfHeight); y <= Mathf.Min(height - 1, centerY + halfHeight); y++)
            {
                for (int x = Mathf.Clamp(horizontalMargin, 0, width - 1); x < width - Mathf.Clamp(horizontalMargin, 0, width - 1); x++)
                {
                    terrain[x, y] = role;
                }
            }
        }

        private static void PaintRectangle(PCGThemeTerrainRole[,] terrain, int startX, int startY, int rectangleWidth, int rectangleHeight, PCGThemeTerrainRole role, bool overwriteWater)
        {
            int width = terrain.GetLength(0);
            int height = terrain.GetLength(1);
            for (int y = Mathf.Max(0, startY); y < Mathf.Min(height, startY + rectangleHeight); y++)
            {
                for (int x = Mathf.Max(0, startX); x < Mathf.Min(width, startX + rectangleWidth); x++)
                {
                    if (overwriteWater || terrain[x, y] != PCGThemeTerrainRole.Water)
                    {
                        terrain[x, y] = role;
                    }
                }
            }
        }

        private static void PaintCircle(PCGThemeTerrainRole[,] terrain, int centerX, int centerY, int radius, PCGThemeTerrainRole role, bool overwriteWater)
        {
            int width = terrain.GetLength(0);
            int height = terrain.GetLength(1);
            int radiusSquared = radius * radius;
            for (int y = Mathf.Max(0, centerY - radius); y <= Mathf.Min(height - 1, centerY + radius); y++)
            {
                for (int x = Mathf.Max(0, centerX - radius); x <= Mathf.Min(width - 1, centerX + radius); x++)
                {
                    int offsetX = x - centerX;
                    int offsetY = y - centerY;
                    if (offsetX * offsetX + offsetY * offsetY <= radiusSquared &&
                        (overwriteWater || terrain[x, y] != PCGThemeTerrainRole.Water))
                    {
                        terrain[x, y] = role;
                    }
                }
            }
        }

        private static void PaintOrthogonalNeighbours(PCGThemeTerrainRole[,] terrain, int x, int y, PCGThemeTerrainRole from, PCGThemeTerrainRole to)
        {
            PaintIfMatches(terrain, x - 1, y, from, to);
            PaintIfMatches(terrain, x + 1, y, from, to);
            PaintIfMatches(terrain, x, y - 1, from, to);
            PaintIfMatches(terrain, x, y + 1, from, to);
        }

        private static void PaintIfMatches(PCGThemeTerrainRole[,] terrain, int x, int y, PCGThemeTerrainRole from, PCGThemeTerrainRole to)
        {
            if (x >= 0 && x < terrain.GetLength(0) && y >= 0 && y < terrain.GetLength(1) && terrain[x, y] == from)
            {
                terrain[x, y] = to;
            }
        }
    }
}
