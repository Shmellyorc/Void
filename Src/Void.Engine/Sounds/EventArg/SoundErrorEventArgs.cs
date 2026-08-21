// ============================================================================
//  SoundErrorEventArgs.cs
// ============================================================================
//  Event arguments for sound system error events.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;

namespace Void.Engine.Sounds.EventArg;

/// <summary>
/// Provides event data for sound system error events.
/// </summary>
/// <remarks>
/// <para>
/// This event is raised when an error occurs during sound playback, loading,
/// or processing. It provides detailed error information including the
/// exception and the sound instance that caused the error.
/// </para>
/// <para>
/// Example usage:
/// <code>
/// SoundInstance.SoundError += (sender, args) =>
/// {
///     Logger.Error($"Sound error: {args.ErrorMessage}");
///     Logger.Error($"Exception: {args.Exception}");
/// };
/// </code>
/// </para>
/// </remarks>
public class SoundErrorEventArgs : EventArgs
{
    /// <summary>
    /// Gets the sound instance that caused the error.
    /// </summary>
    public SoundInstance Instance { get; }

    /// <summary>
    /// Gets the name of the sound that caused the error.
    /// </summary>
    public string SoundName { get; }

    /// <summary>
    /// Gets the exception that occurred.
    /// </summary>
    public Exception Exception { get; }

    /// <summary>
    /// Gets the error message describing what went wrong.
    /// </summary>
    public string ErrorMessage { get; }

    internal SoundErrorEventArgs(SoundInstance instance, Exception exception, string message = null)
    {
        Instance = instance;
        SoundName = instance?.SoundName ?? "Unknown";
        Exception = exception;
        ErrorMessage = message ?? exception?.Message ?? "Unknown Error";
    }
}