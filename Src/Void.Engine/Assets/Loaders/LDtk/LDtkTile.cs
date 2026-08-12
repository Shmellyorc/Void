namespace Void.Engine.Assets.Loaders.LDtk;

public readonly struct LDtkTile
{
    public int TilesetId { get; }
    public Rect2 Source { get; }

    internal LDtkTile(int tilesetId, Rect2 source)
    {
        TilesetId = tilesetId;
        Source = source;
    }

    internal static LDtkTile Process(JsonElement e)
    {
        var tilesetId = e.GetPropertyOrDefault<int>("tilesetUid");
        var x = e.GetPropertyOrDefault<int>("x");
        var y = e.GetPropertyOrDefault<int>("y");
        var w = e.GetPropertyOrDefault<int>("w");
        var h = e.GetPropertyOrDefault<int>("h");

        return new LDtkTile(tilesetId, new(x, y, w, h));
    }
}