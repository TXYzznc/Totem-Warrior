using MapGen.Data;
using UnityEngine;

namespace MapGen.Runtime
{
    /// <summary>
    /// 查询玩家所在地形并输出移动速度倍率。调用方可每帧 Tick，本类内部按固定间隔采样。
    /// </summary>
    public sealed class TerrainEffectTracker
    {
        public const float TickInterval = 0.2f;
        public const float DefaultMoveSpeedMultiplier = 1f;
        public const float SwampMoveSpeedMultiplier = 0.65f;

        MapGridData _grid;
        float _elapsed;
        bool _hasSample;

        public TerrainType CurrentTerrain { get; private set; } = TerrainType.Grass;
        public float CurrentMoveSpeedMultiplier { get; private set; } = DefaultMoveSpeedMultiplier;

        public void SetMap(MapGridData grid)
        {
            _grid = grid;
            _elapsed = TickInterval;
            _hasSample = false;
            CurrentTerrain = TerrainType.Grass;
            CurrentMoveSpeedMultiplier = DefaultMoveSpeedMultiplier;
        }

        public void Clear()
        {
            _grid = null;
            _elapsed = 0f;
            _hasSample = false;
            CurrentTerrain = TerrainType.Grass;
            CurrentMoveSpeedMultiplier = DefaultMoveSpeedMultiplier;
        }

        public float Tick(Vector3 worldPosition, float deltaTime)
        {
            if (_grid == null)
            {
                CurrentTerrain = TerrainType.Grass;
                CurrentMoveSpeedMultiplier = DefaultMoveSpeedMultiplier;
                return CurrentMoveSpeedMultiplier;
            }

            _elapsed += Mathf.Max(0f, deltaTime);
            if (_hasSample && _elapsed < TickInterval)
                return CurrentMoveSpeedMultiplier;

            _elapsed = 0f;
            _hasSample = true;
            var cell = _grid.WorldToCell(worldPosition);
            CurrentTerrain = _grid.Grid[cell.x, cell.y];
            CurrentMoveSpeedMultiplier = GetMoveSpeedMultiplier(CurrentTerrain);
            return CurrentMoveSpeedMultiplier;
        }

        public static float GetMoveSpeedMultiplier(TerrainType terrain)
        {
            return terrain switch
            {
                TerrainType.Swamp => SwampMoveSpeedMultiplier,
                TerrainType.Shore => 0.95f,
                TerrainType.Forest => 0.85f,
                TerrainType.Wasteland => 0.9f,
                _ => DefaultMoveSpeedMultiplier,
            };
        }
    }
}
