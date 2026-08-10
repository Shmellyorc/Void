namespace Void.Engine.Graphics;

public enum BlendFactor
{
    Zero,
    One,
    SrcColor,
    OneMinusSrcColor,
    DstColor,
    OneMinusDstColor,
    SrcAlpha,
    OneMinusSrcAlpha,
    DstAlpha,
    OneMinusDstAlpha
}

public enum BlendEquation
{
    Add,
    Subtract,
    ReverseSubtract,
    Min,
    Max
}

public interface IBlendMode
{
    BlendFactor ColorSrcFactor { get; }
    BlendFactor ColorDstFactor { get; }
    BlendEquation ColorEquation { get; }
    BlendFactor AlphaSrcFactor { get; }
    BlendFactor AlphaDstFactor { get; }
    BlendEquation AlphaEquation { get; }
}

public static class BlendMode
{
    public struct CustomBlend : IBlendMode
    {
        public BlendFactor ColorSrcFactor { get; set; }
        public BlendFactor ColorDstFactor { get; set; }
        public BlendEquation ColorEquation { get; set; }
        public BlendFactor AlphaSrcFactor { get; set; }
        public BlendFactor AlphaDstFactor { get; set; }
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
    public static readonly IBlendMode Premultiplied = new PremultipliedBlend();

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
