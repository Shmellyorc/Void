namespace Void.Engine.Assets.Loaders.LDtk;

public enum LDtkLayerType
{
    None,
    IntGrid,
    Entities,
    Tiles,
    AutoLayer

}

public sealed class MapLayer
{
    public string Name { get; }
    public LDtkLayerType Type { get; }
    public Vect2 GridSize { get; }
    public int TileSize { get; }
    public float Opacity { get; }
    public Vect2 TotalOffset { get; }
    public uint TilesetId { get; }
    public string TilesetPath { get; }
    public string Id { get; }
    public int LevelId { get; }
    public Vect2 Offset { get; }
    public bool Visible { get; }
    public IReadOnlyList<ILDtkInstance> Instances { get; }
    public IReadOnlyList<T> InstanceAs<T>() where T : ILDtkInstance => [.. Instances.OfType<T>()];

    internal MapLayer(string name, LDtkLayerType type, Vect2 gridSize, int tileSize, float opacity,
        Vect2 totalOffset, uint tilesetId, string tilesetPath, string id, int levelId, Vect2 offset,
        bool visible, List<ILDtkInstance> instances)
    {
        Name = name;
        Type = type;
        GridSize = gridSize;
        TileSize = tileSize;
        Opacity = opacity;
        TotalOffset = totalOffset;
        TilesetId = tilesetId;
        TilesetPath = tilesetPath;
        Id = id;
        LevelId = levelId;
        Offset = offset;
        Visible = visible;
        Instances = instances;
    }

    internal static List<MapLayer> Process(JsonElement e)
    {
        var result = new List<MapLayer>(e.GetArrayLength());

        foreach (var t in e.EnumerateArray())
        {
            var name = t.GetPropertyOrDefault("__identifier", string.Empty);
            var type = Enum.Parse<LDtkLayerType>(t.GetPropertyOrDefault("__type", "None"), true);
            var cX = t.GetPropertyOrDefault<int>("__cWid");
            var cY = t.GetPropertyOrDefault<int>("__cHei");
            var tileSize = t.GetPropertyOrDefault<int>("__gridSize");
            var opacity = t.GetPropertyOrDefault<float>("__opacity");
            var totalOffsetX = t.GetPropertyOrDefault<int>("__pxTotalOffsetX");
            var totalOffsetY = t.GetPropertyOrDefault<int>("__pxTotalOffsetY");
            var tilesetId = t.GetPropertyOrDefault("__tilesetDefUid", 0u);
            var tilesetPath = t.GetPropertyOrDefault("__tilesetRelPath", string.Empty);
            var id = t.GetPropertyOrDefault("iid", string.Empty);
            var levelId = t.GetPropertyOrDefault<int>("levelId");
            var offsetX = t.GetPropertyOrDefault<int>("pxOffsetX");
            var offsetY = t.GetPropertyOrDefault<int>("pxOffsetY");
            var visible = t.GetPropertyOrDefault<bool>("visible");
            var gridSize = new Vect2(cX, cY);

            List<ILDtkInstance> instResult = type switch
            {
                LDtkLayerType.IntGrid => LDtkIntGridInstance.Process(t.GetProperty("intGridCsv"), gridSize),
                LDtkLayerType.Entities => LDtkEntityInstance.Process(t.GetProperty("entityInstances")),
                LDtkLayerType.Tiles => LDtkTileInstance.Process(t.GetProperty("gridTiles"), tileSize),
                LDtkLayerType.AutoLayer => LDtkTileInstance.Process(t.GetProperty("autoLayerTiles"), tileSize),
                _ => throw new ArgumentException($"Unable to find Map layer type, it is '{type}'.")
            };

            result.Add(
                new MapLayer(
                    name,
                    type,
                    gridSize,
                    tileSize,
                    opacity,
                    new(totalOffsetX, totalOffsetY),
                    tilesetId,
                    tilesetPath,
                    id,
                    levelId,
                    new(offsetX, offsetY),
                    visible,
                    instResult
                )
            );
        }

        return result;
    }
}