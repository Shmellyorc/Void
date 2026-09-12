// ============================================================================
//  ImageDecoder.cs
// ============================================================================
//  Renderer-independent image decoding before GPU upload.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using StbImageSharp;

namespace Void.Engine.Graphics.Rendering;

// Decoded image data stays renderer-neutral so backends only receive raw RGBA8 pixels.
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

// Encoded PNG, JPG, and other supported image data is decoded here rather than
// inside a renderer backend.
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
