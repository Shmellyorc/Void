namespace Void.Engine.Graphics.Rendering;

/// <summary>
/// Small renderer-neutral helpers for working with tightly packed RGBA8 pixel buffers.
/// </summary>
internal static class RgbaPixelBuffer
{
    private const int BytesPerPixel = 4;

    internal static bool TryCopyRegion(
        ReadOnlySpan<byte> source,
        int sourceWidth,
        int sourceHeight,
        Rect2 sourceRect,
        out byte[] pixels,
        out int width,
        out int height)
    {
        pixels = null;
        width = 0;
        height = 0;

        if (sourceWidth <= 0 || sourceHeight <= 0)
            return false;

        int x = (int)sourceRect.X;
        int y = (int)sourceRect.Y;
        width = (int)sourceRect.Width;
        height = (int)sourceRect.Height;

        if (x < 0 || y < 0 || width <= 0 || height <= 0)
            return false;
        if (x + width > sourceWidth || y + height > sourceHeight)
            return false;

        int requiredSourceLength = checked(sourceWidth * sourceHeight * BytesPerPixel);
        if (source.Length < requiredSourceLength)
            return false;

        pixels = new byte[checked(width * height * BytesPerPixel)];
        int rowBytes = checked(width * BytesPerPixel);

        for (int row = 0; row < height; row++)
        {
            int srcOffset = checked(((y + row) * sourceWidth + x) * BytesPerPixel);
            int dstOffset = checked(row * rowBytes);
            source.Slice(srcOffset, rowBytes).CopyTo(pixels.AsSpan(dstOffset, rowBytes));
        }

        return true;
    }

    internal static void Blit(
        ReadOnlySpan<byte> source,
        int sourceWidth,
        int sourceHeight,
        Span<byte> destination,
        int destinationWidth,
        int destinationHeight,
        int destinationX,
        int destinationY)
    {
        if (sourceWidth <= 0 || sourceHeight <= 0)
            throw new ArgumentOutOfRangeException(nameof(sourceWidth));
        if (destinationWidth <= 0 || destinationHeight <= 0)
            throw new ArgumentOutOfRangeException(nameof(destinationWidth));
        if (destinationX < 0 || destinationY < 0 ||
            destinationX + sourceWidth > destinationWidth ||
            destinationY + sourceHeight > destinationHeight)
        {
            throw new ArgumentOutOfRangeException(nameof(destinationX), "RGBA blit exceeds destination bounds.");
        }

        int sourceLength = checked(sourceWidth * sourceHeight * BytesPerPixel);
        int destinationLength = checked(destinationWidth * destinationHeight * BytesPerPixel);
        if (source.Length < sourceLength)
            throw new ArgumentException("RGBA source buffer is smaller than the declared dimensions.", nameof(source));
        if (destination.Length < destinationLength)
            throw new ArgumentException("RGBA destination buffer is smaller than the declared dimensions.", nameof(destination));

        int rowBytes = checked(sourceWidth * BytesPerPixel);
        for (int row = 0; row < sourceHeight; row++)
        {
            int srcOffset = checked(row * rowBytes);
            int dstOffset = checked(((destinationY + row) * destinationWidth + destinationX) * BytesPerPixel);
            source.Slice(srcOffset, rowBytes).CopyTo(destination.Slice(dstOffset, rowBytes));
        }
    }

    internal static void ClearRegion(
        Span<byte> destination,
        int destinationWidth,
        int destinationHeight,
        Rect2 rect)
    {
        int x = (int)rect.X;
        int y = (int)rect.Y;
        int width = (int)rect.Width;
        int height = (int)rect.Height;

        if (x < 0 || y < 0 || width <= 0 || height <= 0 ||
            x + width > destinationWidth || y + height > destinationHeight)
        {
            return;
        }

        int requiredLength = checked(destinationWidth * destinationHeight * BytesPerPixel);
        if (destination.Length < requiredLength)
            throw new ArgumentException("RGBA destination buffer is smaller than the declared dimensions.", nameof(destination));

        int rowBytes = checked(width * BytesPerPixel);
        for (int row = 0; row < height; row++)
        {
            int offset = checked(((y + row) * destinationWidth + x) * BytesPerPixel);
            destination.Slice(offset, rowBytes).Clear();
        }
    }
}
