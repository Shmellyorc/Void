// ============================================================================
//  LogEntry.cs
// ============================================================================
//  Represents one log entry delivered to registered log sinks.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;

using Void.Engine.Logs.Sinks;

namespace Void.Engine.Logs;

/// <summary>
/// Contains the data associated with one log message.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Logger"/> creates log entries and passes them to registered sinks.
/// Custom <see cref="ILogSink"/> implementations can inspect the severity,
/// timestamp, category, message, and optional exception.
/// </para>
/// <para>
/// This is a mutable value type for compatibility with custom sinks. Sink
/// implementations should normally treat entries received from the logger as
/// read-only values.
/// </para>
/// </remarks>
public struct LogEntry
{
    /// <summary>
    /// The severity of the log entry.
    /// </summary>
    public LogLevel Level;

    /// <summary>
    /// The local time at which the entry was created.
    /// </summary>
    public DateTime Timestamp;

    /// <summary>
    /// The message associated with the entry.
    /// </summary>
    public string Message;

    /// <summary>
    /// The optional category used to identify the source or subsystem.
    /// </summary>
    public string Category;

    /// <summary>
    /// The optional exception associated with the entry.
    /// </summary>
    public Exception Exception;
}
