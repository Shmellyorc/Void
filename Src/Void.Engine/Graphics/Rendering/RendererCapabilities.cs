namespace Void.Engine.Graphics.Rendering;

/// <summary>
/// Capabilities reported by the active renderer. Unsupported optional features
/// remain false/zero rather than leaking backend-specific feature objects.
/// </summary>
public readonly struct RendererCapabilities
{
    public int MaxTextureSize { get; }
    public int MaxTextureUnits { get; }
    public int MaxRenderTargets { get; }
    public int MaxSamples { get; }
    public bool SupportsInstancing { get; }
    public bool SupportsGeometryShaders { get; }
    public bool SupportsComputeShaders { get; }
    public bool SupportsDebugOutput { get; }

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
