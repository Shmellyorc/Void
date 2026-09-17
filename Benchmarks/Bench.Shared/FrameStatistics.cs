namespace Bench.Shared;

public readonly record struct FrameStatistics(
    double MeanMs,
    double MedianMs,
    double P99Ms,
    double MinMs,
    double MaxMs,
    double Fps)
{
    public static FrameStatistics Calculate(ReadOnlySpan<double> frameTimes)
    {
        if (frameTimes.Length == 0)
            return default;

        double[] sorted = frameTimes.ToArray();
        Array.Sort(sorted);

        double total = 0.0;

        for (int i = 0; i < sorted.Length; i++)
            total += sorted[i];

        double mean = total / sorted.Length;

        double median = sorted.Length % 2 == 0
            ? (sorted[(sorted.Length / 2) - 1] + sorted[sorted.Length / 2]) * 0.5
            : sorted[sorted.Length / 2];

        int p99Index = (int)Math.Ceiling(sorted.Length * 0.99) - 1;
        p99Index = Math.Clamp(p99Index, 0, sorted.Length - 1);

        return new FrameStatistics(
            mean,
            median,
            sorted[p99Index],
            sorted[0],
            sorted[^1],
            mean > 0.0 ? 1000.0 / mean : 0.0);
    }
}