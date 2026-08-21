// ============================================================================
//  ILogSink.cs
// ============================================================================
//  Interface for log sinks that receive and process log messages from
//  the logging system.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Logs.Sinks;

/// <summary>
/// Defines the contract for log sinks that receive and process log messages
/// from the logging system.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="ILogSink"/> interface is implemented by classes that consume
/// log entries from the <see cref="Logger"/> and write them to their
/// respective destinations (console, file, network, etc.).
/// </para>
/// <para>
/// Implementations should handle their own thread safety, as the logger
/// may call <see cref="Write"/> from its background processing thread.
/// </para>
/// <para>
/// <b>Built-in Implementations:</b>
/// <list type="bullet">
///   <item><description><see cref="ConsoleSink"/> - Writes to the console with color coding</description></item>
///   <item><description><see cref="FileSink"/> - Writes to daily rotating text files</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Create a custom sink
/// public class NetworkSink : ILogSink
/// {
///     public void Write(LogEntry entry)
///     {
///         // Send log entry to a remote server
///         var json = JsonSerializer.Serialize(entry);
///         HttpClient.PostAsync("https://logs.example.com", json);
///     }
/// }
/// 
/// // Add the sink to the logger
/// Logger.Instance.AddSink(new NetworkSink());
/// </code>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// The <see cref="Write"/> method may be called from the logger's background
/// thread. Implementations should be thread-safe or use synchronization
/// mechanisms to handle concurrent writes.
/// </para>
/// </remarks>
public interface ILogSink
{
    /// <summary>
    /// Writes a log entry to the sink's destination.
    /// </summary>
    /// <param name="entry">The log entry containing the message and metadata to write.</param>
    void Write(LogEntry entry);
}