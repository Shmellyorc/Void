// ============================================================================
//  LogEntry.cs
// ============================================================================
//  Represents a single log message entry with metadata including level,
//  timestamp, category, and optional exception information.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Logs;

/// <summary>
/// Represents a single log message entry with metadata for the logging system.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="LogEntry"/> structure encapsulates all the information
/// associated with a single log message, including its severity level,
/// timestamp, content, category, and any associated exception.
/// </para>
/// <para>
/// This structure is used internally by the <see cref="Logger"/> to queue
/// and process log messages. Sinks receive instances of this structure
/// through the <see cref="ILogSink.Write"/> method.
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Creating a log entry (typically done by the Logger)
/// var entry = new LogEntry
/// {
///     Level = LogLevel.Info,
///     Timestamp = DateTime.Now,
///     Category = "Network",
///     Message = "Connection established to server",
///     Exception = null
/// };
/// </code>
/// </para>
/// <para>
/// <b>Fields:</b>
/// <list type="bullet">
///   <item><description><see cref="Level"/> - The severity level of the message</description></item>
///   <item><description><see cref="Timestamp"/> - When the message was logged</description></item>
///   <item><description><see cref="Category"/> - Optional category for grouping (e.g., "Network", "Audio")</description></item>
///   <item><description><see cref="Message"/> - The log message content</description></item>
///   <item><description><see cref="Exception"/> - Optional exception associated with the message</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// This structure is immutable and thread-safe by design when all fields are
/// assigned at creation time.
/// </para>
/// </remarks>
public struct LogEntry
{
    /// <summary>
    /// The severity level of the log message.
    /// </summary>
    public LogLevel Level;

    /// <summary>
    /// The timestamp indicating when the log message was created.
    /// </summary>
    public DateTime Timestamp;

    /// <summary>
    /// The log message content.
    /// </summary>
    public string Message;

    /// <summary>
    /// An optional category for grouping log messages.
    /// </summary>
    /// <remarks>
    /// Categories are typically used to filter or organize logs by
    /// subsystem (e.g., "Network", "Audio", "Graphics", "Input").
    /// </remarks>
    public string Category;

    /// <summary>
    /// An optional exception associated with the log message.
    /// </summary>
    /// <remarks>
    /// This field is typically populated for error and fatal level
    /// messages to provide additional context about failures.
    /// </remarks>
    public Exception Exception;
}