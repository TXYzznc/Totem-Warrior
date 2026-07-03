namespace MapGen.Data
{
    public enum TerrainType
    {
        Grass = 1,
        RuinFloor = 2,
        Shore = 3,
        Water = 4,
        Swamp = 5,
        Wasteland = 6,
        Mountain = 7,
        Forest = 8,
    }

    public enum FeaturePointType
    {
        Spawn = 1,
        Boss = 2,
        Merchant = 3,
        TattooStudio = 4,
    }

    public enum FeatureSpreadMode
    {
        Line = 1,
        Blob = 2,
    }

    public enum MapObjectKind
    {
        Tree = 1,
        Rock = 2,
        Reed = 3,
        RuinDebris = 4,
        FeatureBuilding = 5,
    }
}
