using Spectre.Console;

namespace Void.Packer.CLI.UI;

public class ProgressData
{
    public int TotalFiles { get; set; }
    public int CompletedFiles { get; set; }
    public string CurrentFile { get; set; } = string.Empty;
    public long CurrentFileSize { get; set; }
    public DateTime StartTime { get; set; }
    public long TotalBytesProcessed { get; set; }
}

public static class ProgressRenderer
{
    public static void Render(ProgressData data)
    {
        var progress = data.TotalFiles > 0 
            ? (double)data.CompletedFiles / data.TotalFiles 
            : 0;

        var elapsed = DateTime.Now - data.StartTime;
        var estimatedTotal = data.CompletedFiles > 0 
            ? elapsed.TotalSeconds / data.CompletedFiles * data.TotalFiles 
            : 0;
        var remaining = TimeSpan.FromSeconds(Math.Max(0, estimatedTotal - elapsed.TotalSeconds));

        // Build the progress bar
        var bar = new ProgressBar(progress)
            .SetColor(Color.Green)
            .SetRemainingColor(Color.Grey);

        // Create the status line
        var status = $"[bold blue]▶[/] Processing: [bold]{data.CurrentFile}[/]";
        if (data.CurrentFileSize > 0)
            status += $" [grey]({FormatSize(data.CurrentFileSize)})[/]";

        // Create the stats line
        var stats = $"📁 {data.CompletedFiles}/{data.TotalFiles} files  |  ⏱ {FormatTime(elapsed)}";
        if (data.CompletedFiles > 5) // Only show ETA after enough data
            stats += $"  |  ⏳ {FormatTime(remaining)}";

        // Render
        // AnsiConsole.Clear();
        AnsiConsole.MarkupLine($"\n{bar}");
        AnsiConsole.MarkupLine($"  {status}");
        AnsiConsole.MarkupLine($"  [grey]{stats}[/]\n");
    }

    private static string FormatSize(long bytes)
    {
        string[] sizes = ["B", "KB", "MB", "GB", "TB"];
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }

    private static string FormatTime(TimeSpan time)
    {
        if (time.TotalHours >= 1)
            return $"{time:hh\\:mm\\:ss}";
        if (time.TotalMinutes >= 1)
            return $"{time:mm\\:ss}";
        return $"{time:ss\\s}";
    }
}

// Simple progress bar implementation for Spectre.Console
public class ProgressBar
{
    private readonly double _progress;
    private readonly int _width = 50;
    private readonly Color _color;
    private readonly Color _remainingColor;

    public ProgressBar(double progress, Color color = default, Color remainingColor = default)
    {
        _progress = Math.Clamp(progress, 0, 1);
        _color = color == default ? Color.Green : color;
        _remainingColor = remainingColor == default ? Color.Grey : remainingColor;
    }

    public ProgressBar SetColor(Color color) => new(_progress, color, _remainingColor);
    public ProgressBar SetRemainingColor(Color color) => new(_progress, _color, color);

    public override string ToString()
    {
        var filled = (int)(_progress * _width);
        var empty = _width - filled;

        var filledBar = new string('█', filled);
        var emptyBar = new string('░', empty);

        var percent = (_progress * 100).ToString("F0");

        return $"[{_color}]{filledBar}[/][{_remainingColor}]{emptyBar}[/] {percent}%";
    }
}