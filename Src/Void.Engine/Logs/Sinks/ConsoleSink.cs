namespace Void.Engine.Logs.Sinks;

public sealed class ConsoleSink : ILogSink
{
    private readonly Lock _lock = new();

    public void Write(LogEntry entry)
    {
        var line = Format(entry);

        lock (_lock)
        {
            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = GetColor(entry.Level);
            Console.WriteLine(line);
            Console.ForegroundColor = originalColor;
        }
    }

    private ConsoleColor GetColor(LogLevel level) => level switch
    {
        LogLevel.Debug => ConsoleColor.Gray,
        LogLevel.Info => ConsoleColor.White,
        LogLevel.Warning => ConsoleColor.Yellow,
        LogLevel.Error => ConsoleColor.Red,
        LogLevel.Fatal => ConsoleColor.DarkRed,
        _ => ConsoleColor.White
    };

    private string Format(LogEntry entry)
    {
        var category = string.IsNullOrEmpty(entry.Category) ? "" : $"[{entry.Category}] ";
        var exception = entry.Exception != null ? $"\n{entry.Exception}" : "";
        return $"[{entry.Timestamp:HH:mm:ss}] [{entry.Level}] {category}{entry.Message}{exception}";
    }
}
