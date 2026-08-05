using System;

namespace Game.EditorTools.OasisCity
{
    [Serializable]
    internal sealed class OasisCityLayoutData
    {
        public int schemaVersion;
        public float mapWidth;
        public float mapLength;
        public int buildingCount;
        public int spawnCount;
        public OasisBuildingData[] buildings;
        public OasisSpawnData[] spawns;
        public OasisRiverSample[] river;
        public OasisWallData[] walls;
    }

    [Serializable]
    internal sealed class OasisBuildingData
    {
        public string id;
        public int type;
        public float x;
        public float z;
        public float yaw;
        public float sizeX;
        public float sizeY;
        public float sizeZ;
        public string category;
    }

    [Serializable]
    internal sealed class OasisSpawnData
    {
        public string id;
        public float x;
        public float z;
        public float yaw;
    }

    [Serializable]
    internal sealed class OasisRiverSample
    {
        public float z;
        public float leftX;
        public float rightX;
    }

    [Serializable]
    internal sealed class OasisWallData
    {
        public string name;
        public OasisPointData[] points;
    }

    [Serializable]
    internal sealed class OasisPointData
    {
        public float x;
        public float z;
    }
}
