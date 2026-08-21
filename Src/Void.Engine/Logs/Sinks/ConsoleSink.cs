// ============================================================================
//  ConsoleSink.cs
// ============================================================================
//  Log sink that writes formatted log messages to the console with
//  color-coded output based on log level.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;

namespace Void.Engine.Logs.Sinks;

/// <summary>
/// A log sink that writes formatted log messages to the console with
/// color-coded output based on the severity level.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="ConsoleSink"/> implements <see cref="ILogSink"/> and writes
/// log entries to the standard output with color coding:
/// <list type="bullet">
///   <item><description><see cref="LogLevel.Debug"/> - Gray</description></item>
///   <item><description><see cref="LogLevel.Info"/> - White</description></item>
///   <item><description><see cref="LogLevel.Warning"/> - Yellow</description></item>
///   <item><description><see cref="LogLevel.Error"/> - Red</description></item>
///   <item><description><see cref="LogLevel.Fatal"/> - Dark Red</description></item>
/// </list>
/// </para>
/// <para>
/// Each log entry is formatted as:
/// <c>[HH:mm:ss] [Level] [Category] Message</c>
/// If an exception is present, it is included on a new line after the message.
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Add the console sink to the logger
/// Logger.Instance.AddSink(new ConsoleSink());
/// 
/// // Now all log messages will appear in the console with colors
/// Logger.Instance.Info("Game started");
/// Logger.Instance.Warning("Low memory warning");
/// Logger.Instance.Error("Failed to load texture", exception);
/// </code>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// This class is thread-safe. A lock is used to prevent console output
/// from multiple threads from being interleaved.
/// </para>
/// </remarks>
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