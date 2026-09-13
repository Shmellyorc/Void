// ============================================================================
//  IBatcher.cs
// ============================================================================
//  Common batch-rendering contract and batching-related enums.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Graphics;

/// <summary>Specifies texture-coordinate flip effects.</summary>
[Flags]
public enum TextureEffects
{
    /// <summary>Does not flip texture coordinates.</summary>
    None = 0,

    /// <summary>Flips texture coordinates horizontally.</summary>
    Horizontal = 1 << 0,

    /// <summary>Flips texture coordinates vertically.</summary>
    Vertical = 1 << 1
}

/// <summary>Specifies how queued commands are ordered before submission.</summary>
public enum SortMode
{
    /// <summary>Preserves command submission order without a sorting pass.</summary>
    Immediate,

    /// <summary>Sorts commands using the batcher's back-to-front depth ordering.</summary>
    BackToFront,

    /// <summary>Sorts commands using the batcher's front-to-back depth ordering.</summary>
    FrontToBack,

    /// <summary>Preserves command submission order without a sorting pass.</summary>
    Deferred
}

/// <summary>
/// Defines the lifecycle and diagnostics contract shared by batch renderers.
/// </summary>
/// <remarks>
/// <para>
/// Implementations collect draw commands between <see cref="Begin"/> and
/// <see cref="End"/>. <see cref="Flush"/> submits queued work without ending the
/// active batch. The sort mode controls ordering; it does not require an
/// implementation to submit each command immediately.
/// </para>
/// <para>
/// External batcher implementations may implement this interface directly or
/// derive from <see cref="BaseBatcher"/> when VOID's shared vertex submission
/// pipeline fits their design.
/// </para>
/// </remarks>
public interface IBatcher : IDisposable
{
    /// <summary>Begins a new batch.</summary>
    /// <param name="sort">Sort mode, or null to use the implementation's configured default.</param>
    /// <param name="blendMode">Blend mode, or null to use the implementation's configured default.</param>
    /// <param name="camera">Optional <see cref="BaseCamera"/> implementation used for view state.</param>
    /// <param name="renderTarget">Optional target for this batch.</param>
    void Begin(SortMode? sort = null, IBlendMode blendMode = null, BaseCamera camera = null, IRenderTarget renderTarget = null);

    /// <summary>Flushes queued commands and ends the active batch.</summary>
    void End();

    /// <summary>Submits queued commands without ending the active batch.</summary>
    void Flush();

    /// <summary>Gets whether a batch is currently active.</summary>
    bool IsDrawing { get; }

    /// <summary>Gets the draw-call count reported by <see cref="Stats"/>.</summary>
    int DrawCallCount { get; }

    /// <summary>Gets the vertex count reported by <see cref="Stats"/>.</summary>
    int VertexCount { get; }

    /// <summary>Gets the number of commands currently queued and not yet flushed.</summary>
    int CommandCount { get; }

    /// <summary>Gets the implementation name used for diagnostics.</summary>
    string Name { get; }

    /// <summary>Gets the rendering statistics currently reported by the batcher.</summary>
    BatchStats Stats { get; }
}
