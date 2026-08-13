namespace System;

/// <summary>
/// Extension methods for int to simplify game development math.
/// </summary>
public static class IntExtensions
{
    /// <summary>
    /// Clamps the value between min and max.
    /// </summary>
    public static int Clamp(this int value, int min, int max)
        => Math.Clamp(value, min, max);

    /// <summary>
    /// Converts to degrees from radians.
    /// </summary>
    public static float ToRadians(this int value)
        => value * MathHelper.DegToRad;

    /// <summary>
    /// Converts to radians from degrees.
    /// </summary>
    public static float ToDegrees(this int value)
        => value * MathHelper.RadToDeg;

    /// <summary>
    /// Wraps the value between 0 and max (exclusive).
    /// </summary>
    public static int Wrap(this int value, int max)
        => ((value % max) + max) % max;

    /// <summary>
    /// Wraps the value between min and max (exclusive).
    /// </summary>
    public static int Wrap(this int value, int min, int max)
        => min + ((value - min) % (max - min) + (max - min)) % (max - min);

    /// <summary>
    /// Returns true if the value is even.
    /// </summary>
    public static bool IsEven(this int value)
        => (value & 1) == 0;

    /// <summary>
    /// Returns true if the value is odd.
    /// </summary>
    public static bool IsOdd(this int value)
        => (value & 1) == 1;

    /// <summary>
    /// Returns true if the value is a power of two.
    /// </summary>
    public static bool IsPowerOfTwo(this int value)
        => value > 0 && (value & (value - 1)) == 0;

    /// <summary>
    /// Rounds up to the nearest power of two.
    /// </summary>
    public static int NextPowerOfTwo(this int value)
    {
        int result = 1;

        while (result < value)
            result <<= 1;
            
        return result;
    }

    /// <summary>
    /// Returns the sign: -1, 0, or 1.
    /// </summary>
    public static int Sign(this int value)
        => Math.Sign(value);

    /// <summary>
    /// Converts seconds to milliseconds.
    /// </summary>
    public static int SecondsToMilliseconds(this int value)
        => value * 1000;

    /// <summary>
    /// Converts milliseconds to seconds.
    /// </summary>
    public static float MillisecondsToSeconds(this int value)
        => value / 1000f;

    /// <summary>
    /// Converts frames to seconds at the given FPS.
    /// </summary>
    public static float FramesToSeconds(this int frames, int fps = 60)
        => frames / (float)fps;

    /// <summary>
    /// Converts seconds to frames at the given FPS.
    /// </summary>
    public static int SecondsToFrames(this int seconds, int fps = 60)
        => seconds * fps;

    /// <summary>
    /// Converts pixel to tile coordinate.
    /// </summary>
    public static int PixelsToTiles(this int pixels, int tileSize)
        => pixels / tileSize;

    /// <summary>
    /// Converts tile to pixel coordinate.
    /// </summary>
    public static int TilesToPixels(this int tiles, int tileSize)
        => tiles * tileSize;

    /// <summary>
    /// Returns a bool from int (0 = false, anything else = true).
    /// </summary>
    public static bool ToBool(this int value)
        => value != 0;

    /// <summary>
    /// Returns the absolute value.
    /// </summary>
    public static int Abs(this int value)
        => Math.Abs(value);
}
