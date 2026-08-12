namespace Void.Engine.Assets.Loaders.LDtk.Instances;

public sealed class LDtkTileInstance : ILDtkInstance
{
    public Rect2 Source { get; }
    public TextureEffects Effects { get; }
    public int Tile { get; }
    public float Alpha { get; }
    public Vect2 Location { get; }
    public Vect2 Position { get; }

    internal LDtkTileInstance(Rect2 source, TextureEffects effects, int tile, float alpha,
        Vect2 location, Vect2 position)
    {
        Source = source;
        Effects = effects;
        Tile = tile;
        Alpha = alpha;
        Location = location;
        Position = position;
    }

    internal static List<ILDtkInstance> Process(JsonElement e, int tileSize)
    {
        var result = new List<ILDtkInstance>(e.GetArrayLength());

        foreach (var t in e.EnumerateArray())
        {
            var position = t.GetPosition("px");
            var src = t.GetPosition("src");
            var flag = t.GetPropertyOrDefault<int>("f");
            var tile = t.GetPropertyOrDefault<int>("t");
            var alpha = t.GetPropertyOrDefault<float>("a");
            var location = Vect2.Floor(position / tileSize);
            var srcRect = new Rect2(src, new(tileSize));

            TextureEffects effects = flag switch
            {
                1 => TextureEffects.Horizontal,
                2 => TextureEffects.Vertical,
                3 => TextureEffects.Horizontal | TextureEffects.Vertical,
                _ => TextureEffects.None
            };

            result.Add(new LDtkTileInstance(srcRect, effects, tile, alpha, location, position));
        }

        return result;
    }
}