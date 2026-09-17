using Bench.Shared;
using Void.Engine;
using Void.Engine.Graphics;

int spriteCount =
    GetIntArg(
        args,
        "--sprites",
        10_000);

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

IBlendMode blendMode =
    blendName.ToLowerInvariant() switch
    {
        "alpha" => BlendMode.Alpha,
        "opaque" => BlendMode.None,
        "none" => BlendMode.None,
        _ => throw new ArgumentException(
            "--blend must be alpha or opaque.")
    };

int minimumCapacity =
    Math.Max(1, spriteCount);

int batchCapacity =
    GetIntArg(
        args,
        "--capacity",
        minimumCapacity);

if (batchCapacity < minimumCapacity)
{
    throw new ArgumentException(
        $"--capacity must be at least {minimumCapacity:N0} for {spriteCount:N0} sprites.");
}

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

Directory.CreateDirectory("Content");

var settings = GameSettings.Instance
    .SetAppCompany("VOID")
    .SetAppName("VoidBenchmark")
    .SetAppTitle("VOID 2.1.0 Benchmark")
    .SetWindow(
        (uint)config.Width,
        (uint)config.Height)
    .SetViewport(
        (uint)config.Width,
        (uint)config.Height)
    .SetSuperSample(1)
    .SetVsync(false)
    .SetFixedTimeStep(false)
    .SetDefaultSortMode(SortMode.Deferred)
    .SetClearColor(0, 0, 0)
    .Build();

using var game =
    new VoidBenchmarkGame(
        settings,
        config,
        batchCapacity,
        blendMode);

game.Run();

BenchmarkResult result =
    game.Result
    ?? throw new InvalidOperationException(
        "Benchmark did not complete.");

Console.WriteLine();
Console.WriteLine(
    "========================================");

Console.WriteLine(
    "VOID BENCHMARK RESULT");

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
    $"Sprite Size:    {spriteSize}x{spriteSize}");

Console.WriteLine(
    $"Blend:          {blendName}");

Console.WriteLine(
    $"Batch Capacity: {batchCapacity:N0}");

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
