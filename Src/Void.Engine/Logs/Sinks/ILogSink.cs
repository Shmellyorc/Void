// ============================================================================
//  ILogSink.cs
// ============================================================================
//  Defines the extension point used to receive log entries.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using Void.Engine.Logs;

namespace Void.Engine.Logs.Sinks;

/// <summary>
/// Defines a destination that receives log entries from <see cref="Logger"/>.
/// </summary>
/// <remarks>
/// <para>
/// Register sinks with <see cref="Logger.AddSink"/>. VOID includes
/// <see cref="ConsoleSink"/> and <see cref="FileSink"/>, and applications can
/// provide their own implementations for other destinations.
/// </para>
/// <para>
/// The logger serializes calls to registered sinks. Implementations that are also
/// called directly from other threads should provide any additional synchronization
/// required by their own destination.
/// </para>
/// </remarks>
public interface ILogSink
{
    /// <summary>
    /// Writes one log entry to the sink's destination.
    /// </summary>
    /// <param name="entry">The log entry to write.</param>
    void Write(LogEntry entry);
}
