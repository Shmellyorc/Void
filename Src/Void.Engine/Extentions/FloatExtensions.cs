namespace System;

public static class FloatExtensions
{
    public static float Clamp(this float value, float min, float max)
        => Math.Clamp(value, min, max);

    public static float Saturate(this float value)
        => Math.Clamp(value, 0f, 1f);

    public static float Wrap(this float value, float max)
        => ((value % max) + max) % max;

    public static float Wrap(this float value, float min, float max)
        => min + ((value - min) % (max - min) + (max - min)) % (max - min);

    public static float ToRadians(this float value)
        => value * MathHelper.DegToRad;

    public static float ToDegrees(this float value)
        => value * MathHelper.RadToDeg;

    public static float Sign(this float value)
        => MathF.Sign(value);

    public static float Abs(this float value)
        => MathF.Abs(value);

    public static bool IsZero(this float value)
        => MathF.Abs(value) < MathHelper.Epsilon;

    public static bool ApproxEquals(this float value, float other)
        => MathF.Abs(value - other) < MathHelper.Epsilon;

    public static int RoundToInt(this float value)
        => (int)MathF.Round(value);

    public static int FloorToInt(this float value)
        => (int)MathF.Floor(value);

    public static int CeilToInt(this float value)
        => (int)MathF.Ceiling(value);

    public static float Round(this float value, int decimals = 0)
        => MathF.Round(value, decimals);

    public static float Snap(this float value, float gridSize)
        => MathF.Round(value / gridSize) * gridSize;

    public static string ToPercent(this float value)
        => $"{value * 100f:0}%";

    public static string ToTimeString(this float value)
    {
        int minutes = (int)(value / 60f);
        int seconds = (int)(value % 60f);
        return $"{minutes}:{seconds:00}";
    }

    public static bool ToBool(this float value)
        => value != 0f;

    public static float LerpTo(this float from, float to, float t)
        => from + (to - from) * t;
}