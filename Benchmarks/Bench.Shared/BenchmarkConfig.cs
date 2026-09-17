namespace Bench.Shared;

public sealed class BenchmarkConfig
{
    public int SpriteCount { get; init; } = 10_000;

    public int Width { get; init; } = 1280;

    public int Height { get; init; } = 720;

    public int WarmupSeconds { get; init; } = 5;

    public int MeasurementSeconds { get; init; } = 10;

    public int RandomSeed { get; init; } = 12345;

    public int SpriteWidth { get; init; } = 32;

    public int SpriteHeight { get; init; } = 32;
}