// ============================================================================
//  ConsoleSink.cs
// ============================================================================
//  Writes formatted log entries to the console.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Threading;

using Void.Engine.Logs;

namespace Void.Engine.Logs.Sinks;

/// <summary>
/// Writes log entries to the console using a color associated with each severity.
/// </summary>
/// <remarks>
/// <para>
/// Entries are written as <c>[HH:mm:ss] [Level] [Category] Message</c>. The
/// category segment is omitted when no category is present. An associated
/// exception is appended on the following line as part of the same colored output.
/// </para>
/// <para>
/// Debug entries use gray, informational entries white, warnings yellow, errors red,
/// and fatal entries dark red. Console writes are synchronized so one entry is not
/// interleaved with another call to this sink.
/// </para>
/// </remarks>
public sealed class ConsoleSink : ILogSink
{
    private readonly Lock _lock = new();

    /// <summary>
    /// Writes a log entry to the console.
    /// </summary>
    /// <param name="entry">The entry to format and write.</param>
    public void Write(LogEntry entry)
    {
        string line = Format(entry);

        lock (_lock)
        {
            ConsoleColor originalColor = Console.ForegroundColor;
            Console.ForegroundColor = GetColor(entry.Level);
            Console.WriteLine(line);
            Console.ForegroundColor = originalColor;
        }
    }

    private static ConsoleColor GetColor(LogLevel level) => level switch
    {
        LogLevel.Debug => ConsoleColor.Gray,
        LogLevel.Info => ConsoleColor.White,
        LogLevel.Warning => ConsoleColor.Yellow,
        LogLevel.Error => ConsoleColor.Red,
        LogLevel.Fatal => ConsoleColor.DarkRed,
        _ => ConsoleColor.White
    };

    private static string Format(LogEntry entry)
    {
        string category = string.IsNullOrEmpty(entry.Category) ? "" : $"[{entry.Category}] ";
        string exception = entry.Exception != null ? $"\n{entry.Exception}" : "";

        return $"[{entry.Timestamp:HH:mm:ss}] [{entry.Level}] {category}{entry.Message}{exception}";
    }
}
