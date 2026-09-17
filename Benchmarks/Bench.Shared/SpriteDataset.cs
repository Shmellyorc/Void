namespace Bench.Shared;

public static class SpriteDataset
{
    public const int SpriteWidth = 32;
    public const int SpriteHeight = 32;

    public static SpriteInstance[] Create(BenchmarkConfig config)
    {
        SpriteInstance[] sprites = new SpriteInstance[config.SpriteCount];

        uint state = unchecked((uint)config.RandomSeed);

        int maxX = Math.Max(1, config.Width - config.SpriteWidth);
        int maxY = Math.Max(1, config.Height - config.SpriteHeight);

        for (int i = 0; i < sprites.Length; i++)
        {
            float x = Next(ref state) % (uint)maxX;
            float y = Next(ref state) % (uint)maxY;

            sprites[i] = new SpriteInstance(x, y);
        }

        return sprites;
    }

    private static uint Next(ref uint state)
    {
        state ^= state << 13;
        state ^= state >> 17;
        state ^= state << 5;

        return state;
    }
}