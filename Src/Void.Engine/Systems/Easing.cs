// ============================================================================
//  Easing.cs
// ============================================================================
//  Comprehensive easing function library with 33 different easing types
//  covering quadratic, cubic, sine, exponential, circular, back, elastic,
//  and bounce families with In, Out, InOut, and OutIn directions.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Systems;

/// <summary>
/// Defines all supported easing types by combining an easing family with a direction.
/// </summary>
/// <remarks>
/// <para>
/// Easing functions control the rate of change of a value over time, producing
/// different acceleration and deceleration patterns for animations, transitions,
/// and other time-based effects.
/// </para>
/// <para>
/// The naming convention follows a pattern where the family name is combined
/// with a direction suffix:
/// <list type="bullet">
/// <item><description><see cref="QuadIn"/> - Quadratic ease-in (starts slow, ends fast)</description></item>
/// <item><description><see cref="QuadOut"/> - Quadratic ease-out (starts fast, ends slow)</description></item>
/// <item><description><see cref="QuadInOut"/> - Quadratic with both ease-in and ease-out</description></item>
/// <item><description><see cref="QuadOutIn"/> - Quadratic with ease-out then ease-in</description></item>
/// </list>
/// </para>
/// <para>
/// To apply an easing function, pass the desired <see cref="EaseType"/> and
/// a normalized time value (0-1) to <see cref="Easing.Ease"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// float t = 0.5f; // Halfway through the animation
/// float easedValue = Easing.Ease(EaseType.QuadInOut, t);
/// // Use easedValue to interpolate between start and end values
/// </code>
/// </example>
public enum EaseType
{
    /// <summary>
    /// No easing applied. The value progresses linearly from start to end.
    /// </summary>
    Linear,

    /// <summary>
    /// Quadratic ease-in. The value starts slowly and accelerates toward the end.
    /// </summary>
    QuadIn,

    /// <summary>
    /// Quadratic ease-out. The value starts quickly and decelerates toward the end.
    /// </summary>
    QuadOut,

    /// <summary>
    /// Quadratic ease-in-out. The value starts slow, accelerates, then decelerates at the end.
    /// </summary>
    QuadInOut,

    /// <summary>
    /// Quadratic ease-out-in. The value starts fast, decelerates in the middle, then accelerates again.
    /// </summary>
    QuadOutIn,

    /// <summary>
    /// Cubic ease-in. The value starts very slowly and accelerates toward the end.
    /// </summary>
    CubicIn,

    /// <summary>
    /// Cubic ease-out. The value starts quickly and decelerates toward the end.
    /// </summary>
    CubicOut,

    /// <summary>
    /// Cubic ease-in-out. The value starts slow, accelerates, then decelerates at the end.
    /// </summary>
    CubicInOut,

    /// <summary>
    /// Cubic ease-out-in. The value starts fast, decelerates in the middle, then accelerates again.
    /// </summary>
    CubicOutIn,

    /// <summary>
    /// Quartic ease-in. The value starts very slowly and accelerates strongly toward the end.
    /// </summary>
    QuartIn,

    /// <summary>
    /// Quartic ease-out. The value starts very fast and decelerates strongly toward the end.
    /// </summary>
    QuartOut,

    /// <summary>
    /// Quartic ease-in-out. The value starts slow, accelerates strongly, then decelerates at the end.
    /// </summary>
    QuartInOut,

    /// <summary>
    /// Quartic ease-out-in. The value starts fast, decelerates strongly in the middle, then accelerates again.
    /// </summary>
    QuartOutIn,

    /// <summary>
    /// Quintic ease-in. The value starts extremely slowly and accelerates sharply toward the end.
    /// </summary>
    QuintIn,

    /// <summary>
    /// Quintic ease-out. The value starts extremely fast and decelerates sharply toward the end.
    /// </summary>
    QuintOut,

    /// <summary>
    /// Quintic ease-in-out. The value starts slow, accelerates sharply, then decelerates at the end.
    /// </summary>
    QuintInOut,

    /// <summary>
    /// Quintic ease-out-in. The value starts fast, decelerates sharply in the middle, then accelerates again.
    /// </summary>
    QuintOutIn,

    /// <summary>
    /// Sine ease-in. The value starts slowly with a smooth sinusoidal curve.
    /// </summary>
    SineIn,

    /// <summary>
    /// Sine ease-out. The value ends slowly with a smooth sinusoidal curve.
    /// </summary>
    SineOut,

    /// <summary>
    /// Sine ease-in-out. The value starts and ends slowly with a smooth sinusoidal curve.
    /// </summary>
    SineInOut,

    /// <summary>
    /// Sine ease-out-in. The value has a slow middle section with sinusoidal smoothing.
    /// </summary>
    SineOutIn,

    /// <summary>
    /// Exponential ease-in. The value starts imperceptibly slow and accelerates extremely fast.
    /// </summary>
    ExpoIn,

    /// <summary>
    /// Exponential ease-out. The value starts extremely fast and decelerates imperceptibly.
    /// </summary>
    ExpoOut,

    /// <summary>
    /// Exponential ease-in-out. The value starts imperceptibly slow, accelerates, then decelerates.
    /// </summary>
    ExpoInOut,

    /// <summary>
    /// Exponential ease-out-in. The value starts fast, decelerates, then accelerates imperceptibly.
    /// </summary>
    ExpoOutIn,

    /// <summary>
    /// Circular ease-in. The value follows a circular arc starting slowly.
    /// </summary>
    CircIn,

    /// <summary>
    /// Circular ease-out. The value follows a circular arc ending slowly.
    /// </summary>
    CircOut,

    /// <summary>
    /// Circular ease-in-out. The value follows a circular arc starting and ending slowly.
    /// </summary>
    CircInOut,

    /// <summary>
    /// Circular ease-out-in. The value follows a circular arc with a slow middle.
    /// </summary>
    CircOutIn,

    /// <summary>
    /// Back ease-in. The value overshoots the starting point before moving forward.
    /// </summary>
    BackIn,

    /// <summary>
    /// Back ease-out. The value overshoots the ending point before settling.
    /// </summary>
    BackOut,

    /// <summary>
    /// Back ease-in-out. The value overshoots both the start and end points.
    /// </summary>
    BackInOut,

    /// <summary>
    /// Back ease-out-in. The value overshoots in the middle of the transition.
    /// </summary>
    BackOutIn,

    /// <summary>
    /// Elastic ease-in. The value oscillates with a spring-like effect at the start.
    /// </summary>
    ElasticIn,

    /// <summary>
    /// Elastic ease-out. The value oscillates with a spring-like effect at the end.
    /// </summary>
    ElasticOut,

    /// <summary>
    /// Elastic ease-in-out. The value oscillates with a spring-like effect at both ends.
    /// </summary>
    ElasticInOut,

    /// <summary>
    /// Elastic ease-out-in. The value oscillates with a spring-like effect in the middle.
    /// </summary>
    ElasticOutIn,

    /// <summary>
    /// Bounce ease-in. The value bounces at the start of the transition.
    /// </summary>
    BounceIn,

    /// <summary>
    /// Bounce ease-out. The value bounces at the end of the transition.
    /// </summary>
    BounceOut,

    /// <summary>
    /// Bounce ease-in-out. The value bounces at both the start and end.
    /// </summary>
    BounceInOut,

    /// <summary>
    /// Bounce ease-out-in. The value bounces in the middle of the transition.
    /// </summary>
    BounceOutIn,
}

/// <summary>
/// Provides static methods for evaluating easing functions by type.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="Ease"/> method serves as the primary entry point, accepting
/// an <see cref="EaseType"/> and a normalized time value. All easing functions
/// operate on a time range of 0 to 1, with the output also normalized to 0 to 1.
/// </para>
/// <para>
/// This class is typically used in animation systems, tweening libraries, and
/// any scenario where smooth interpolations are required.
/// </para>
/// </remarks>
public static class Easing
{
    /// <summary>
    /// Evaluates the specified easing function at the given time value.
    /// </summary>
    /// <param name="type">The easing type to evaluate.</param>
    /// <param name="t">The normalized time value between 0 and 1.</param>
    /// <returns>The eased value between 0 and 1.</returns>
    public static float Ease(EaseType type, float t)
    {
        t = Math.Clamp(t, 0f, 1f);

        return type switch
        {
            EaseType.Linear => Linear(t),
            EaseType.QuadIn => QuadIn(t),
            EaseType.QuadOut => QuadOut(t),
            EaseType.QuadInOut => QuadInOut(t),
            EaseType.QuadOutIn => OutIn(QuadOut, QuadIn, t),
            EaseType.CubicIn => CubicIn(t),
            EaseType.CubicOut => CubicOut(t),
            EaseType.CubicInOut => CubicInOut(t),
            EaseType.CubicOutIn => OutIn(CubicOut, CubicIn, t),
            EaseType.QuartIn => QuartIn(t),
            EaseType.QuartOut => QuartOut(t),
            EaseType.QuartInOut => QuartInOut(t),
            EaseType.QuartOutIn => OutIn(QuartOut, QuartIn, t),
            EaseType.QuintIn => QuintIn(t),
            EaseType.QuintOut => QuintOut(t),
            EaseType.QuintInOut => QuintInOut(t),
            EaseType.QuintOutIn => OutIn(QuintOut, QuintIn, t),
            EaseType.SineIn => SineIn(t),
            EaseType.SineOut => SineOut(t),
            EaseType.SineInOut => SineInOut(t),
            EaseType.SineOutIn => OutIn(SineOut, SineIn, t),
            EaseType.ExpoIn => ExpoIn(t),
            EaseType.ExpoOut => ExpoOut(t),
            EaseType.ExpoInOut => ExpoInOut(t),
            EaseType.ExpoOutIn => OutIn(ExpoOut, ExpoIn, t),
            EaseType.CircIn => CircIn(t),
            EaseType.CircOut => CircOut(t),
            EaseType.CircInOut => CircInOut(t),
            EaseType.CircOutIn => OutIn(CircOut, CircIn, t),
            EaseType.BackIn => BackIn(t),
            EaseType.BackOut => BackOut(t),
            EaseType.BackInOut => BackInOut(t),
            EaseType.BackOutIn => OutIn(BackOut, BackIn, t),
            EaseType.ElasticIn => ElasticIn(t),
            EaseType.ElasticOut => ElasticOut(t),
            EaseType.ElasticInOut => ElasticInOut(t),
            EaseType.ElasticOutIn => OutIn(ElasticOut, ElasticIn, t),
            EaseType.BounceIn => BounceIn(t),
            EaseType.BounceOut => BounceOut(t),
            EaseType.BounceInOut => BounceInOut(t),
            EaseType.BounceOutIn => OutIn(BounceOut, BounceIn, t),
            _ => t,
        };
    }

    /// <summary>
    /// Combines an ease-out function with an ease-in function to create an Out-In curve.
    /// </summary>
    private static float OutIn(Func<float, float> easeOut, Func<float, float> easeIn, float t)
    {
        if (t < 0.5f)
            return 0.5f * easeOut(t * 2f);
        else
            return 0.5f * easeIn((t - 0.5f) * 2f) + 0.5f;
    }

    #region Linear
    private static float Linear(float t) => t;
    #endregion

    #region Quadratic
    private static float QuadIn(float t) => t * t;

    private static float QuadOut(float t) => t * (2f - t);

    private static float QuadInOut(float t)
    {
        if (t < 0.5f)
            return 2f * t * t;
        else
            return -1f + (4f - 2f * t) * t;
    }
    #endregion

    #region Cubic
    private static float CubicIn(float t) => t * t * t;

    private static float CubicOut(float t)
    {
        float p = t - 1f;
        return p * p * p + 1f;
    }

    private static float CubicInOut(float t)
    {
        if (t < 0.5f)
            return 4f * t * t * t;
        else
        {
            float p = 2f * t - 2f;
            return 0.5f * p * p * p + 1f;
        }
    }
    #endregion

    #region Quartic
    private static float QuartIn(float t) => t * t * t * t;

    private static float QuartOut(float t)
    {
        float p = t - 1f;
        return 1f - p * p * p * p;
    }

    private static float QuartInOut(float t)
    {
        if (t < 0.5f)
            return 8f * t * t * t * t;
        else
        {
            float p = t - 1f;
            return 1f - 8f * p * p * p * p;
        }
    }
    #endregion

    #region Quintic
    private static float QuintIn(float t) => t * t * t * t * t;

    private static float QuintOut(float t)
    {
        float p = t - 1f;
        return p * p * p * p * p + 1f;
    }

    private static float QuintInOut(float t)
    {
        if (t < 0.5f)
            return 16f * t * t * t * t * t;
        else
        {
            float p = 2f * t - 2f;
            return 0.5f * p * p * p * p * p + 1f;
        }
    }
    #endregion

    #region Sine
    private static float SineIn(float t) => 1f - MathF.Cos(t * MathF.PI / 2f);

    private static float SineOut(float t) => MathF.Sin(t * MathF.PI / 2f);

    private static float SineInOut(float t) => -0.5f * (MathF.Cos(MathF.PI * t) - 1f);
    #endregion

    #region Exponential
    private static float ExpoIn(float t) => t == 0f ? 0f : MathF.Pow(2f, 10f * (t - 1f));

    private static float ExpoOut(float t) => t == 1f ? 1f : 1f - MathF.Pow(2f, -10f * t);

    private static float ExpoInOut(float t)
    {
        if (t == 0f) return 0f;
        if (t == 1f) return 1f;
        if (t < 0.5f)
            return 0.5f * MathF.Pow(2f, 20f * t - 10f);
        else
            return 1f - 0.5f * MathF.Pow(2f, -20f * t + 10f);
    }
    #endregion

    #region Circular
    private static float CircIn(float t) => 1f - MathF.Sqrt(1f - t * t);

    private static float CircOut(float t) => MathF.Sqrt(1f - (t - 1f) * (t - 1f));

    private static float CircInOut(float t)
    {
        if (t < 0.5f)
            return 0.5f * (1f - MathF.Sqrt(1f - 4f * t * t));
        else
            return 0.5f * (MathF.Sqrt(1f - (2f * t - 2f) * (2f * t - 2f)) + 1f);
    }
    #endregion

    #region Back (overshoot)
    private const float BackS = 1.70158f;

    private static float BackIn(float t) => t * t * ((BackS + 1f) * t - BackS);

    private static float BackOut(float t)
    {
        float p = t - 1f;
        return p * p * ((BackS + 1f) * p + BackS) + 1f;
    }

    private static float BackInOut(float t)
    {
        float s = BackS * 1.525f;
        if (t < 0.5f)
        {
            float p = 2f * t;
            return 0.5f * (p * p * ((s + 1f) * p - s));
        }
        else
        {
            float p = 2f * t - 2f;
            return 0.5f * (p * p * ((s + 1f) * p + s) + 2f);
        }
    }
    #endregion

    #region Elastic (oscillatory)
    private static float ElasticIn(float t)
    {
        if (t == 0f) return 0f;
        if (t == 1f) return 1f;
        const float p = 0.3f;
        float s = p / 4f;
        float invT = t - 1f;
        return -MathF.Pow(2f, 10f * invT) * MathF.Sin((invT - s) * (2f * MathF.PI) / p);
    }

    private static float ElasticOut(float t)
    {
        if (t == 0f) return 0f;
        if (t == 1f) return 1f;
        const float p = 0.3f;
        float s = p / 4f;
        return MathF.Pow(2f, -10f * t) * MathF.Sin((t - s) * (2f * MathF.PI) / p) + 1f;
    }

    private static float ElasticInOut(float t)
    {
        if (t == 0f) return 0f;
        if (t == 1f) return 1f;
        const float p = 0.45f;
        float s = p / 4f;
        float invT = 2f * t - 1f;

        if (invT < 0f)
            return -0.5f * MathF.Pow(2f, 10f * invT) * MathF.Sin((invT - s) * (2f * MathF.PI) / p);
        else
            return MathF.Pow(2f, -10f * invT) * MathF.Sin((invT - s) * (2f * MathF.PI) / p) * 0.5f + 1f;
    }
    #endregion

    #region Bounce (piecewise)
    private static float BounceIn(float t) => 1f - BounceOut(1f - t);

    private static float BounceOut(float t)
    {
        const float n1 = 7.5625f;
        const float d1 = 2.75f;

        if (t < 1f / d1)
            return n1 * t * t;
        else if (t < 2f / d1)
        {
            float u = t - 1.5f / d1;
            return n1 * u * u + 0.75f;
        }
        else if (t < 2.5f / d1)
        {
            float u = t - 2.25f / d1;
            return n1 * u * u + 0.9375f;
        }
        else
        {
            float u = t - 2.625f / d1;
            return n1 * u * u + 0.984375f;
        }
    }

    private static float BounceInOut(float t)
    {
        if (t < 0.5f)
            return (1f - BounceOut(1f - 2f * t)) * 0.5f;
        else
            return BounceOut(2f * t - 1f) * 0.5f + 0.5f;
    }
    #endregion
}