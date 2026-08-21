// ============================================================================
//  BlendMode.cs
// ============================================================================
//  Defines blend factors, equations, and blend modes for controlling
//  how source and destination pixels are combined during rendering.
//  Provides common blend modes (Alpha, Add, Multiply, None, etc.) and
//  a factory method for creating custom blend modes.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Graphics;

/// <summary>
/// Specifies the factor used for blending source and destination colors.
/// </summary>
/// <remarks>
/// <para>
/// Blend factors determine how the source and destination colors are
/// weighted in the blending equation. The blend equation is typically:
/// <c>Result = SourceColor × SourceFactor + DestinationColor × DestinationFactor</c>
/// </para>
/// <para>
/// These factors are used in both color and alpha blending operations.
/// </para>
/// </remarks>
public enum BlendFactor
{
    /// <summary>Zero factor - contributes nothing.</summary>
    Zero,
    /// <summary>One factor - full contribution.</summary>
    One,
    /// <summary>Source color factor (RGB channels).</summary>
    SrcColor,
    /// <summary>One minus source color factor.</summary>
    OneMinusSrcColor,
    /// <summary>Destination color factor (RGB channels).</summary>
    DstColor,
    /// <summary>One minus destination color factor.</summary>
    OneMinusDstColor,
    /// <summary>Source alpha factor.</summary>
    SrcAlpha,
    /// <summary>One minus source alpha factor.</summary>
    OneMinusSrcAlpha,
    /// <summary>Destination alpha factor.</summary>
    DstAlpha,
    /// <summary>One minus destination alpha factor.</summary>
    OneMinusDstAlpha
}

/// <summary>
/// Specifies the equation used to combine source and destination colors.
/// </summary>
public enum BlendEquation
{
    /// <summary>Adds source and destination: <c>Src + Dst</c></summary>
    Add,
    /// <summary>Subtracts destination from source: <c>Src - Dst</c></summary>
    Subtract,
    /// <summary>Subtracts source from destination: <c>Dst - Src</c></summary>
    ReverseSubtract,
    /// <summary>Takes the minimum of source and destination: <c>Min(Src, Dst)</c></summary>
    Min,
    /// <summary>Takes the maximum of source and destination: <c>Max(Src, Dst)</c></summary>
    Max
}

/// <summary>
/// Defines the contract for blend modes used in rendering operations.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="IBlendMode"/> interface provides access to the blend
/// factors and equations used for both color and alpha channels.
/// </para>
/// <para>
/// Blend modes control how rendered pixels are combined with existing
/// pixels in the render target. They are essential for transparency,
/// additive effects, and various visual effects.
/// </para>
/// </remarks>
public interface IBlendMode
{
    /// <summary>Gets the source factor for the color channels.</summary>
    BlendFactor ColorSrcFactor { get; }
    /// <summary>Gets the destination factor for the color channels.</summary>
    BlendFactor ColorDstFactor { get; }
    /// <summary>Gets the equation for combining color channels.</summary>
    BlendEquation ColorEquation { get; }
    /// <summary>Gets the source factor for the alpha channel.</summary>
    BlendFactor AlphaSrcFactor { get; }
    /// <summary>Gets the destination factor for the alpha channel.</summary>
    BlendFactor AlphaDstFactor { get; }
    /// <summary>Gets the equation for combining the alpha channel.</summary>
    BlendEquation AlphaEquation { get; }
}

/// <summary>
/// Provides common blend modes and a factory for creating custom blend modes.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="BlendMode"/> class contains static readonly fields for
/// commonly used blend modes and a factory method <see cref="Create"/> for
/// creating custom blend modes.
/// </para>
/// <para>
/// <b>Common Blend Modes:</b>
/// <list type="bullet">
///   <item><description><see cref="Alpha"/> - Standard alpha blending (transparency)</description></item>
///   <item><description><see cref="Add"/> - Additive blending (glow, lighting)</description></item>
///   <item><description><see cref="Multiply"/> - Multiplicative blending (tinting, darkening)</description></item>
///   <item><description><see cref="None"/> - No blending (opaque overwrite)</description></item>
///   <item><description><see cref="Min"/> - Minimum blending (darken)</description></item>
///   <item><description><see cref="Max"/> - Maximum blending (lighten)</description></item>
///   <item><description><see cref="Premultiplied"/> - Premultiplied alpha blending</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Use standard alpha blending
/// batcher.Begin(blendMode: BlendMode.Alpha);
/// 
/// // Use additive blending for glow effects
/// batcher.Begin(blendMode: BlendMode.Add);
/// 
/// // Create a custom blend mode
/// var custom = BlendMode.Create(
///     colorSrc: BlendFactor.SrcAlpha,
///     colorDst: BlendFactor.One,
///     colorEq: BlendEquation.Add,
///     alphaSrc: BlendFactor.One,
///     alphaDst: BlendFactor.OneMinusSrcAlpha,
///     alphaEq: BlendEquation.Add
/// );
/// </code>
/// </para>
/// </remarks>
public static class BlendMode
{
    /// <summary>
    /// A custom blend mode structure that implements <see cref="IBlendMode"/>.
    /// </summary>
    /// <remarks>
    /// This struct allows for creating custom blend modes with specific
    /// factors and equations for both color and alpha channels.
    /// </remarks>
    public struct CustomBlend : IBlendMode
    {
        /// <summary>Gets or sets the source factor for the color channels.</summary>
        public BlendFactor ColorSrcFactor { get; set; }
        /// <summary>Gets or sets the destination factor for the color channels.</summary>
        public BlendFactor ColorDstFactor { get; set; }
        /// <summary>Gets or sets the equation for combining color channels.</summary>
        public BlendEquation ColorEquation { get; set; }
        /// <summary>Gets or sets the source factor for the alpha channel.</summary>
        public BlendFactor AlphaSrcFactor { get; set; }
        /// <summary>Gets or sets the destination factor for the alpha channel.</summary>
        public BlendFactor AlphaDstFactor { get; set; }
        /// <summary>Gets or sets the equation for combining the alpha channel.</summary>
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

    /// <summary>
    /// Standard alpha blending for transparent objects.
    /// </summary>
    /// <remarks>
    /// Formula: <c>Result = Src × SrcAlpha + Dst × (1 - SrcAlpha)</c>
    /// </remarks>
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

    /// <summary>
    /// Additive blending for glow, lighting, and particle effects.
    /// </summary>
    /// <remarks>
    /// Formula: <c>Result = Src × SrcAlpha + Dst × 1</c>
    /// </remarks>
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

    /// <summary>
    /// Multiplicative blending for tinting and darkening effects.
    /// </summary>
    /// <remarks>
    /// Formula: <c>Result = Src × Dst</c>
    /// </remarks>
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

    /// <summary>
    /// No blending - source overwrites destination (opaque).
    /// </summary>
    /// <remarks>
    /// Formula: <c>Result = Src × 1 + Dst × 0</c>
    /// </remarks>
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

    /// <summary>
    /// Minimum blending for darkening effects.
    /// </summary>
    /// <remarks>
    /// Formula: <c>Result = Min(Src, Dst)</c>
    /// </remarks>
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

    /// <summary>
    /// Maximum blending for lightening effects.
    /// </summary>
    /// <remarks>
    /// Formula: <c>Result = Max(Src, Dst)</c>
    /// </remarks>
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

    /// <summary>
    /// Premultiplied alpha blending for optimized transparency.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Formula: <c>Result = Src × 1 + Dst × (1 - SrcAlpha)</c>
    /// </para>
    /// <para>
    /// This blend mode expects source colors to be pre-multiplied by alpha.
    /// It is more efficient for certain rendering pipelines.
    /// </para>
    /// </remarks>
    public static readonly IBlendMode Premultiplied = new PremultipliedBlend();

    /// <summary>
    /// Creates a custom blend mode with the specified factors and equations.
    /// </summary>
    /// <param name="colorSrc">The source factor for color channels. Default is <see cref="BlendFactor.SrcAlpha"/>.</param>
    /// <param name="colorDst">The destination factor for color channels. Default is <see cref="BlendFactor.OneMinusSrcAlpha"/>.</param>
    /// <param name="colorEq">The equation for color channels. Default is <see cref="BlendEquation.Add"/>.</param>
    /// <param name="alphaSrc">The source factor for the alpha channel. Default is <see cref="BlendFactor.One"/>.</param>
    /// <param name="alphaDst">The destination factor for the alpha channel. Default is <see cref="BlendFactor.OneMinusSrcAlpha"/>.</param>
    /// <param name="alphaEq">The equation for the alpha channel. Default is <see cref="BlendEquation.Add"/>.</param>
    /// <returns>A custom blend mode with the specified settings.</returns>
    /// <remarks>
    /// <para>
    /// This method provides full control over the blending equation:
    /// <c>Result = Src × ColorSrcFactor + Dst × ColorDstFactor</c>
    /// </para>
    /// <para>
    /// Color and alpha channels can use different factors and equations
    /// for advanced blending effects.
    /// </para>
    /// </remarks>
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