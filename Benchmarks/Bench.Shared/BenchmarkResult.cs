namespace Bench.Shared;

public sealed class BenchmarkResult
{
    public required string Framework { get; init; }
    public required string FrameworkVersion { get; init; }
    public required string Scenario { get; init; }
    public required BenchmarkConfig Config { get; init; }

    public required FrameStatistics FrameStatistics { get; init; }
    public required FrameStatistics BatchStatistics { get; init; }
    public required FrameStatistics CommandStatistics { get; init; }
    public required FrameStatistics FlushStatistics { get; init; }

    public long FramesMeasured { get; init; }
    public long AllocatedBytes { get; init; }
    public long BatchAllocatedBytes { get; init; }

    public int Gen0Collections { get; init; }
    public int Gen1Collections { get; init; }
    public int Gen2Collections { get; init; }

    public int DrawCalls { get; init; }
}
