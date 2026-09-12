// ============================================================================
//  BlendMode.cs
// ============================================================================
//  Renderer-neutral blend factors, equations, presets, and custom blend state.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Graphics;

/// <summary>Specifies a source or destination factor used by a blend operation.</summary>
public enum BlendFactor
{
    /// <summary>Uses a factor of zero.</summary>
    Zero,
    /// <summary>Uses a factor of one.</summary>
    One,
    /// <summary>Uses the source color.</summary>
    SrcColor,
    /// <summary>Uses one minus the source color.</summary>
    OneMinusSrcColor,
    /// <summary>Uses the destination color.</summary>
    DstColor,
    /// <summary>Uses one minus the destination color.</summary>
    OneMinusDstColor,
    /// <summary>Uses the source alpha value.</summary>
    SrcAlpha,
    /// <summary>Uses one minus the source alpha value.</summary>
    OneMinusSrcAlpha,
    /// <summary>Uses the destination alpha value.</summary>
    DstAlpha,
    /// <summary>Uses one minus the destination alpha value.</summary>
    OneMinusDstAlpha
}

/// <summary>Specifies how scaled source and destination values are combined.</summary>
public enum BlendEquation
{
    /// <summary>Adds the source and destination terms.</summary>
    Add,
    /// <summary>Subtracts the destination term from the source term.</summary>
    Subtract,
    /// <summary>Subtracts the source term from the destination term.</summary>
    ReverseSubtract,
    /// <summary>Selects the minimum source or destination value.</summary>
    Min,
    /// <summary>Selects the maximum source or destination value.</summary>
    Max
}

/// <summary>
/// Defines renderer-neutral color and alpha blend state.
/// </summary>
/// <remarks>
/// Custom rendering backends should translate these factors and equations into
/// their native graphics API without exposing backend-specific blend types to
/// engine-facing code.
/// </remarks>
public interface IBlendMode
{
    /// <summary>Gets the source factor for color channels.</summary>
    BlendFactor ColorSrcFactor { get; }

    /// <summary>Gets the destination factor for color channels.</summary>
    BlendFactor ColorDstFactor { get; }

    /// <summary>Gets the equation for color channels.</summary>
    BlendEquation ColorEquation { get; }

    /// <summary>Gets the source factor for the alpha channel.</summary>
    BlendFactor AlphaSrcFactor { get; }

    /// <summary>Gets the destination factor for the alpha channel.</summary>
    BlendFactor AlphaDstFactor { get; }

    /// <summary>Gets the equation for the alpha channel.</summary>
    BlendEquation AlphaEquation { get; }
}

/// <summary>Provides built-in blend presets and custom blend-state creation.</summary>
/// <remarks>
/// <para>
/// Blend state is renderer-neutral. The active backend is responsible for mapping
/// <see cref="BlendFactor"/> and <see cref="BlendEquation"/> values to its API.
/// </para>
/// <para><code>
/// batcher.Begin(blendMode: BlendMode.Add);
///
/// IBlendMode custom = BlendMode.Create(
///     colorSrc: BlendFactor.SrcAlpha,
///     colorDst: BlendFactor.One);
/// </code></para>
/// </remarks>
public static class BlendMode
{
    /// <summary>Mutable blend state used by <see cref="Create"/> and custom callers.</summary>
    public struct CustomBlend : IBlendMode
    {
        /// <summary>Gets or sets the source factor for color channels.</summary>
        public BlendFactor ColorSrcFactor { get; set; }

        /// <summary>Gets or sets the destination factor for color channels.</summary>
        public BlendFactor ColorDstFactor { get; set; }

        /// <summary>Gets or sets the equation for color channels.</summary>
        public BlendEquation ColorEquation { get; set; }

        /// <summary>Gets or sets the source factor for the alpha channel.</summary>
        public BlendFactor AlphaSrcFactor { get; set; }

        /// <summary>Gets or sets the destination factor for the alpha channel.</summary>
        public BlendFactor AlphaDstFactor { get; set; }

        /// <summary>Gets or sets the equation for the alpha channel.</summary>
        public BlendEquation AlphaEquation { get; set; }
    }

    private class AlphaBlend : IBlendMode
    {
        public BlendFactor ColorSrcFactor => BlendFactor.SrcAlpha;
        public BlendFactor ColorDstFactor => BlendFactor.OneMinusSrcAlpha;
        public BlendEquation ColorEquation => BlendEquation.Add;
        public BlendFactor AlphaSrcFactor => BlendFactor.One;
        public BlendFactor AlphaDstFactor => BlendFactor.OneMinusSrcAlpha;
        public BlendEquation AlphaEquation => BlendEquation.Add;
    }

    /// <summary>Standard straight-alpha blending.</summary>
    public static readonly IBlendMode Alpha = new AlphaBlend();

    private class AddBlend : IBlendMode
    {
        public BlendFactor ColorSrcFactor => BlendFactor.SrcAlpha;
        public BlendFactor ColorDstFactor => BlendFactor.One;
        public BlendEquation ColorEquation => BlendEquation.Add;
        public BlendFactor AlphaSrcFactor => BlendFactor.One;
        public BlendFactor AlphaDstFactor => BlendFactor.One;
        public BlendEquation AlphaEquation => BlendEquation.Add;
    }

    /// <summary>Additive blending commonly used for glow and light effects.</summary>
    public static readonly IBlendMode Add = new AddBlend();

    private class MultiplyBlend : IBlendMode
    {
        public BlendFactor ColorSrcFactor => BlendFactor.DstColor;
        public BlendFactor ColorDstFactor => BlendFactor.Zero;
        public BlendEquation ColorEquation => BlendEquation.Add;
        public BlendFactor AlphaSrcFactor => BlendFactor.DstAlpha;
        public BlendFactor AlphaDstFactor => BlendFactor.Zero;
        public BlendEquation AlphaEquation => BlendEquation.Add;
    }

    /// <summary>Multiplicative blending using the destination color and alpha.</summary>
    public static readonly IBlendMode Multiply = new MultiplyBlend();

    private class NoneBlend : IBlendMode
    {
        public BlendFactor ColorSrcFactor => BlendFactor.One;
        public BlendFactor ColorDstFactor => BlendFactor.Zero;
        public BlendEquation ColorEquation => BlendEquation.Add;
        public BlendFactor AlphaSrcFactor => BlendFactor.One;
        public BlendFactor AlphaDstFactor => BlendFactor.Zero;
        public BlendEquation AlphaEquation => BlendEquation.Add;
    }

    /// <summary>Source replacement with no destination contribution.</summary>
    public static readonly IBlendMode None = new NoneBlend();

    private class MinBlend : IBlendMode
    {
        public BlendFactor ColorSrcFactor => BlendFactor.One;
        public BlendFactor ColorDstFactor => BlendFactor.One;
        public BlendEquation ColorEquation => BlendEquation.Min;
        public BlendFactor AlphaSrcFactor => BlendFactor.One;
        public BlendFactor AlphaDstFactor => BlendFactor.One;
        public BlendEquation AlphaEquation => BlendEquation.Min;
    }

    /// <summary>Minimum blending for both color and alpha channels.</summary>
    public static readonly IBlendMode Min = new MinBlend();

    private class MaxBlend : IBlendMode
    {
        public BlendFactor ColorSrcFactor => BlendFactor.One;
        public BlendFactor ColorDstFactor => BlendFactor.One;
        public BlendEquation ColorEquation => BlendEquation.Max;
        public BlendFactor AlphaSrcFactor => BlendFactor.One;
        public BlendFactor AlphaDstFactor => BlendFactor.One;
        public BlendEquation AlphaEquation => BlendEquation.Max;
    }

    /// <summary>Maximum blending for both color and alpha channels.</summary>
    public static readonly IBlendMode Max = new MaxBlend();

    private class PremultipliedBlend : IBlendMode
    {
        public BlendFactor ColorSrcFactor => BlendFactor.One;
        public BlendFactor ColorDstFactor => BlendFactor.OneMinusSrcAlpha;
        public BlendEquation ColorEquation => BlendEquation.Add;
        public BlendFactor AlphaSrcFactor => BlendFactor.One;
        public BlendFactor AlphaDstFactor => BlendFactor.OneMinusSrcAlpha;
        public BlendEquation AlphaEquation => BlendEquation.Add;
    }

    /// <summary>Premultiplied-alpha blending for source colors already multiplied by alpha.</summary>
    public static readonly IBlendMode Premultiplied = new PremultipliedBlend();

    /// <summary>Creates a blend mode from independent color and alpha settings.</summary>
    /// <param name="colorSrc">Source factor for color channels.</param>
    /// <param name="colorDst">Destination factor for color channels.</param>
    /// <param name="colorEq">Equation for color channels.</param>
    /// <param name="alphaSrc">Source factor for the alpha channel.</param>
    /// <param name="alphaDst">Destination factor for the alpha channel.</param>
    /// <param name="alphaEq">Equation for the alpha channel.</param>
    /// <returns>A blend mode containing the supplied state.</returns>
    public static IBlendMode Create(
        BlendFactor colorSrc = BlendFactor.SrcAlpha,
        BlendFactor colorDst = BlendFactor.OneMinusSrcAlpha,
        BlendEquation colorEq = BlendEquation.Add,
        BlendFactor alphaSrc = BlendFactor.One,
        BlendFactor alphaDst = BlendFactor.OneMinusSrcAlpha,
        BlendEquation alphaEq = BlendEquation.Add)
    {
        return new CustomBlend
        {
            ColorSrcFactor = colorSrc,
            ColorDstFactor = colorDst,
            ColorEquation = colorEq,
            AlphaSrcFactor = alphaSrc,
            AlphaDstFactor = alphaDst,
            AlphaEquation = alphaEq
        };
    }
}
