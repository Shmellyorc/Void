// ============================================================================
//  RendererCapabilities.cs
// ============================================================================
//  Renderer-neutral capability limits and optional feature flags.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Graphics.Rendering;

/// <summary>
/// Describes limits and optional features reported by a renderer backend.
/// </summary>
/// <remarks>
/// Unsupported or unavailable optional features should be reported as
/// <see langword="false"/> or zero rather than through backend-specific objects.
/// Renderer implementations should report non-negative numeric limits.
/// </remarks>
public readonly struct RendererCapabilities
{
    /// <summary>Gets the maximum supported texture dimension in pixels.</summary>
    public int MaxTextureSize { get; }

    /// <summary>Gets the maximum number of simultaneously addressable texture units.</summary>
    public int MaxTextureUnits { get; }

    /// <summary>Gets the maximum number of simultaneously supported render targets.</summary>
    public int MaxRenderTargets { get; }

    /// <summary>Gets the maximum supported multisample count.</summary>
    public int MaxSamples { get; }

    /// <summary>Gets whether the backend supports instanced rendering.</summary>
    public bool SupportsInstancing { get; }

    /// <summary>Gets whether the backend supports geometry shaders.</summary>
    public bool SupportsGeometryShaders { get; }

    /// <summary>Gets whether the backend supports compute shaders.</summary>
    public bool SupportsComputeShaders { get; }

    /// <summary>Gets whether the backend supports graphics API debug output.</summary>
    public bool SupportsDebugOutput { get; }

    /// <summary>
    /// Creates a capability snapshot.
    /// </summary>
    /// <param name="maxTextureSize">Maximum texture dimension in pixels.</param>
    /// <param name="maxTextureUnits">Maximum simultaneously addressable texture units.</param>
    /// <param name="maxRenderTargets">Maximum simultaneously supported render targets.</param>
    /// <param name="maxSamples">Maximum multisample count.</param>
    /// <param name="supportsInstancing">Whether instanced rendering is supported.</param>
    /// <param name="supportsGeometryShaders">Whether geometry shaders are supported.</param>
    /// <param name="supportsComputeShaders">Whether compute shaders are supported.</param>
    /// <param name="supportsDebugOutput">Whether graphics API debug output is supported.</param>
    public RendererCapabilities(
        int maxTextureSize,
        int maxTextureUnits,
        int maxRenderTargets,
        int maxSamples,
        bool supportsInstancing,
        bool supportsGeometryShaders,
        bool supportsComputeShaders,
        bool supportsDebugOutput)
    {
        MaxTextureSize = maxTextureSize;
        MaxTextureUnits = maxTextureUnits;
        MaxRenderTargets = maxRenderTargets;
        MaxSamples = maxSamples;
        SupportsInstancing = supportsInstancing;
        SupportsGeometryShaders = supportsGeometryShaders;
        SupportsComputeShaders = supportsComputeShaders;
        SupportsDebugOutput = supportsDebugOutput;
    }
}
