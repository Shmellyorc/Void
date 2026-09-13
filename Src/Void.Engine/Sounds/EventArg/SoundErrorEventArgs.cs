// ============================================================================
//  SoundErrorEventArgs.cs
// ============================================================================
//  Event data for errors reported by sound playback and pool operations.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;

namespace Void.Engine.Sounds.EventArg;

/// <summary>
/// Provides details about an error reported by the sound system.
/// </summary>
public class SoundErrorEventArgs : EventArgs
{
    /// <summary>Gets the associated sound instance, or <see langword="null"/> when the error is not tied to one instance.</summary>
    public SoundInstance Instance { get; }

    /// <summary>Gets the associated sound name, or <c>Unknown</c> when no instance is available.</summary>
    public string SoundName { get; }

    /// <summary>Gets the exception associated with the error.</summary>
    public Exception Exception { get; }

    /// <summary>Gets the contextual error message.</summary>
    public string ErrorMessage { get; }

    internal SoundErrorEventArgs(SoundInstance instance, Exception exception, string message = null)
    {
        Instance = instance;
        SoundName = instance?.SoundName ?? "Unknown";
        Exception = exception;
        ErrorMessage = message ?? exception?.Message ?? "Unknown Error";
    }
}
