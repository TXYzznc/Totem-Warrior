using System.Collections.Generic;

namespace PCGMap
{
    public static class PCGMapValidator
    {
        public static PCGValidationReport Validate(PCGMapData map)
        {
            var report = new PCGValidationReport();
            if (map == null || map.Cells == null || map.Cells.Length == 0)
            {
                report.Warnings.Add("Map is empty.");
                return report;
            }

            CountCells(map, report);
            FloodFillReachability(map, report);
            CountVisuals(map, report);

            if (report.PoiCount < 3)
                report.Warnings.Add($"POI count is low: {report.PoiCount}.");
            if (report.ResourceObjects < 8)
                report.Warnings.Add($"Resource object count is low: {report.ResourceObjects}.");
            if (report.UnreachableCells > report.WalkableCells / 8)
                report.Warnings.Add($"Too many unreachable walkable cells: {report.UnreachableCells}.");

            report.IsValid = report.Warnings.Count == 0;
            return report;
        }

        static void CountCells(PCGMapData map, PCGValidationReport report)
        {
            for (int i = 0; i < map.Cells.Length; i++)
            {
                if (map.Cells[i].Walkable && !map.Cells[i].Occupied)
                    report.WalkableCells++;
            }
        }

        static void FloodFillReachability(PCGMapData map, PCGValidationReport report)
        {
            var visited = new bool[map.Cells.Length];
            var queue = new Queue<int>();
            int start = FindFirstWalkable(map);
            if (start < 0)
            {
                report.Warnings.Add("No walkable cell found.");
                return;
            }

            visited[start] = true;
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                int index = queue.Dequeue();
                report.ReachableCells++;
                int x = index % map.Width;
                int y = index / map.Width;

                TryVisit(map, visited, queue, x + 1, y);
                TryVisit(map, visited, queue, x - 1, y);
                TryVisit(map, visited, queue, x, y + 1);
                TryVisit(map, visited, queue, x, y - 1);
            }

            report.UnreachableCells = report.WalkableCells - report.ReachableCells;
        }

        static int FindFirstWalkable(PCGMapData map)
        {
            for (int i = 0; i < map.Cells.Length; i++)
            {
                var cell = map.Cells[i];
                if (cell.Walkable && !cell.Occupied)
                    return i;
            }
            return -1;
        }

        static void TryVisit(PCGMapData map, bool[] visited, Queue<int> queue, int x, int y)
        {
            if (x < 0 || y < 0 || x >= map.Width || y >= map.Height)
                return;

            int index = y * map.Width + x;
            if (visited[index])
                return;

            var cell = map.Cells[index];
            if (!cell.Walkable || cell.Occupied)
                return;

            visited[index] = true;
            queue.Enqueue(index);
        }

        static void CountVisuals(PCGMapData map, PCGValidationReport report)
        {
            foreach (var visual in map.Visuals)
            {
                if (visual.Kind == PCGPlacedVisualKind.Poi)
                    report.PoiCount++;

                if (visual.BlocksMovement)
                    report.BlockingObjects++;

                if (visual.Role != null &&
                    (visual.Role.Contains("resource") || visual.Role.Contains("loot") || visual.Role.Contains("gather")))
                {
                    report.ResourceObjects++;
                }
            }
        }
    }
}
