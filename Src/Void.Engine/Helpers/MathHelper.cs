namespace Void.Engine.Helpers;

public static class MathHelper
{
    public const float PI = MathF.PI;
    public const float TwoPI = MathF.PI * 2f;
    public const float HalfPI = MathF.PI / 2f;
    public const float QuarterPI = MathF.PI / 4f;
    public const float DegToRad = MathF.PI / 180f;
    public const float RadToDeg = 180f / MathF.PI;
    public const float Epsilon = 0.001f;

    public static bool AlmostZero(float v, float e)
        => MathF.Abs(v) <= e;

    public static bool AlmostEquals(float a, float b, float e)
        => MathF.Abs(a - b) <= e;

    public static float ToRadians(float degrees)
        => degrees * DegToRad;

    public static float ToDegress(float radians)
        => radians * RadToDeg;

    public static float Lerp(float a, float b, float t)
        => a + (b - a) * t;

    public static float Clamp(float value, float min, float max)
        => value < min ? min : value > max ? max : value;

    public static float Center(float a, float b)
        => (a - b) / 2f;

    public static float Saturate(float value)
        => value < 0f ? 0f : value > 1f ? 1f : value;

    public static float SmoothStep(float a, float b, float t)
    {
        t = Saturate(t);
        t = t * t * (3f - 2f * t);

        return Lerp(a, b, t);
    }

    public static float PingPong(float value, float length)
    {
        if (length <= 0f)
            return 0f;

        float mod = value % (length * 2f);

        return mod < length ? mod : length * 2f - mod;
    }

    public static float Wrap(float value, float min, float max)
    {
        float range = max - min;

        if (range <= 0f)
            return min;

        value = (value - min) % range;

        if (value < 0f)
            value += range;

        return value + min;
    }

    public static float MoveTowards(float current, float target, float maxDelta)
    {
        if (MathF.Abs(target - current) <= maxDelta)
            return target;

        return current + MathF.Sign(target - current) * maxDelta;
    }
}