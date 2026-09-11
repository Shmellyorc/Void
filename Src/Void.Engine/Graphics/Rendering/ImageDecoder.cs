using StbImageSharp;

namespace Void.Engine.Graphics.Rendering;

/// <summary>
/// Renderer-independent decoded image data used before GPU upload.
/// </summary>
internal readonly struct DecodedImage
{
    internal int Width { get; }
    internal int Height { get; }
    internal byte[] Pixels { get; }

    internal DecodedImage(int width, int height, byte[] pixels)
    {
        if (width <= 0)
            throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0)
            throw new ArgumentOutOfRangeException(nameof(height));
        ArgumentNullException.ThrowIfNull(pixels);

        int expectedLength = checked(width * height * 4);
        if (pixels.Length != expectedLength)
            throw new ArgumentException($"RGBA image data must contain exactly {expectedLength} bytes.", nameof(pixels));

        Width = width;
        Height = height;
        Pixels = pixels;
    }
}

/// <summary>
/// Central image decoder. Encoded PNG/JPG/etc. data is decoded here so renderer
/// backends only ever receive raw pixel bytes.
/// </summary>
internal static class ImageDecoder
{
    internal static DecodedImage DecodeRgba(byte[] encodedData)
    {
        ArgumentNullException.ThrowIfNull(encodedData);
        if (encodedData.Length == 0)
            throw new ArgumentException("Encoded image data cannot be empty.", nameof(encodedData));

        ImageResult image = ImageResult.FromMemory(encodedData, ColorComponents.RedGreenBlueAlpha);
        return new DecodedImage(image.Width, image.Height, image.Data);
    }
}
