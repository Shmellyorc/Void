using Bench.Shared;
using Microsoft.Xna.Framework.Graphics;

int spriteCount =
    GetIntArg(
        args,
        "--sprites",
        10_000);

bool offscreen =
    GetBoolArg(
        args,
        "--offscreen",
        true);

int spriteSize =
    GetPositiveIntArg(
        args,
        "--size",
        32);

string blendName =
    GetStringArg(
        args,
        "--blend",
        "alpha");

BlendState spriteBlendState =
    blendName.ToLowerInvariant() switch
    {
        "alpha" => BlendState.AlphaBlend,
        "opaque" => BlendState.Opaque,
        _ => throw new ArgumentException(
            "--blend must be alpha or opaque.")
    };

var config = new BenchmarkConfig
{
    SpriteCount = spriteCount,
    Width = 1280,
    Height = 720,
    SpriteWidth = spriteSize,
    SpriteHeight = spriteSize,
    WarmupSeconds = 5,
    MeasurementSeconds = 10,
    RandomSeed = 12345
};

using var game =
    new MonoGameBenchmarkGame(
        config,
        offscreen,
        spriteBlendState);

game.Run();

BenchmarkResult result =
    game.Result
    ?? throw new InvalidOperationException(
        "Benchmark did not complete.");

Console.WriteLine();
Console.WriteLine(
    "========================================");

Console.WriteLine(
    "MONOGAME BENCHMARK RESULT");

Console.WriteLine(
    "========================================");

Console.WriteLine(
    $"Framework:      {result.Framework}");

Console.WriteLine(
    $"Version:        {result.FrameworkVersion}");

Console.WriteLine(
    $"Scenario:       {result.Scenario}");

Console.WriteLine(
    $"Sprites:        {result.Config.SpriteCount:N0}");

Console.WriteLine(
    $"Offscreen:      {(offscreen ? "Enabled" : "Disabled")}");

Console.WriteLine(
    $"Sprite Size:    {spriteSize}x{spriteSize}");

Console.WriteLine(
    $"Blend:          {blendName}");

Console.WriteLine(
    $"Resolution:     {result.Config.Width}x{result.Config.Height}");

Console.WriteLine(
    $"Frames:         {result.FramesMeasured:N0}");

PrintStats(
    "Whole Frame",
    result.FrameStatistics);

PrintStats(
    "Batch CPU",
    result.BatchStatistics);

PrintStats(
    "Draw Calls CPU",
    result.CommandStatistics);

PrintStats(
    "End / Flush CPU",
    result.FlushStatistics);

Console.WriteLine();

Console.WriteLine(
    $"Draw Calls:     {result.DrawCalls}");

Console.WriteLine(
    $"Allocated:      {result.AllocatedBytes:N0} bytes");

Console.WriteLine(
    $"Gen0:           {result.Gen0Collections}");

Console.WriteLine(
    $"Gen1:           {result.Gen1Collections}");

Console.WriteLine(
    $"Gen2:           {result.Gen2Collections}");

Console.WriteLine(
    "========================================");

static void PrintStats(
    string name,
    FrameStatistics stats)
{
    Console.WriteLine();
    Console.WriteLine($"{name}:");

    Console.WriteLine(
        $"  Mean:         {stats.MeanMs:F4} ms");

    Console.WriteLine(
        $"  Median:       {stats.MedianMs:F4} ms");

    Console.WriteLine(
        $"  P99:          {stats.P99Ms:F4} ms");

    Console.WriteLine(
        $"  Min:          {stats.MinMs:F4} ms");

    Console.WriteLine(
        $"  Max:          {stats.MaxMs:F4} ms");
}

static int GetIntArg(
    string[] args,
    string name,
    int defaultValue)
{
    for (int i = 0;
         i < args.Length - 1;
         i++)
    {
        if (!string.Equals(
                args[i],
                name,
                StringComparison.OrdinalIgnoreCase))
        {
            continue;
        }

        if (!int.TryParse(
                args[i + 1],
                out int value)
            || value < 0)
        {
            throw new ArgumentException(
                $"{name} must be a non-negative integer.");
        }

        return value;
    }

    return defaultValue;
}

static int GetPositiveIntArg(
    string[] args,
    string name,
    int defaultValue)
{
    int value =
        GetIntArg(
            args,
            name,
            defaultValue);

    if (value <= 0)
    {
        throw new ArgumentException(
            $"{name} must be greater than zero.");
    }

    return value;
}

static bool GetBoolArg(
    string[] args,
    string name,
    bool defaultValue)
{
    for (int i = 0;
         i < args.Length - 1;
         i++)
    {
        if (!string.Equals(
                args[i],
                name,
                StringComparison.OrdinalIgnoreCase))
        {
            continue;
        }

        if (!bool.TryParse(
                args[i + 1],
                out bool value))
        {
            throw new ArgumentException(
                $"{name} must be true or false.");
        }

        return value;
    }

    return defaultValue;
}

static string GetStringArg(
    string[] args,
    string name,
    string defaultValue)
{
    for (int i = 0;
         i < args.Length - 1;
         i++)
    {
        if (!string.Equals(
                args[i],
                name,
                StringComparison.OrdinalIgnoreCase))
        {
            continue;
        }

        string value =
            args[i + 1].Trim();

        if (value.Length == 0)
        {
            throw new ArgumentException(
                $"{name} cannot be empty.");
        }

        return value;
    }

    return defaultValue;
}
