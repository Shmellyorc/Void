// ============================================================================
//  Easing.cs
// ============================================================================
//  Easing function library with 41 easing types across common curve families
//  and In, Out, InOut, and OutIn directions.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Systems;

/// <summary>
/// Identifies an easing curve used to transform normalized time.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Easing.Ease"/> clamps its input to the range from 0 to 1 before
/// evaluating the selected curve.
/// </para>
/// <para>
/// Most curves remain within the normalized output range. Back and elastic
/// curves intentionally overshoot to create their characteristic motion.
/// </para>
/// </remarks>
public enum EaseType
{
    /// <summary>Linear interpolation with no easing.</summary>
    Linear,

    /// <summary>Quadratic ease-in.</summary>
    QuadIn,

    /// <summary>Quadratic ease-out.</summary>
    QuadOut,

    /// <summary>Quadratic ease-in followed by ease-out.</summary>
    QuadInOut,

    /// <summary>Quadratic ease-out followed by ease-in.</summary>
    QuadOutIn,

    /// <summary>Cubic ease-in.</summary>
    CubicIn,

    /// <summary>Cubic ease-out.</summary>
    CubicOut,

    /// <summary>Cubic ease-in followed by ease-out.</summary>
    CubicInOut,

    /// <summary>Cubic ease-out followed by ease-in.</summary>
    CubicOutIn,

    /// <summary>Quartic ease-in.</summary>
    QuartIn,

    /// <summary>Quartic ease-out.</summary>
    QuartOut,

    /// <summary>Quartic ease-in followed by ease-out.</summary>
    QuartInOut,

    /// <summary>Quartic ease-out followed by ease-in.</summary>
    QuartOutIn,

    /// <summary>Quintic ease-in.</summary>
    QuintIn,

    /// <summary>Quintic ease-out.</summary>
    QuintOut,

    /// <summary>Quintic ease-in followed by ease-out.</summary>
    QuintInOut,

    /// <summary>Quintic ease-out followed by ease-in.</summary>
    QuintOutIn,

    /// <summary>Sinusoidal ease-in.</summary>
    SineIn,

    /// <summary>Sinusoidal ease-out.</summary>
    SineOut,

    /// <summary>Sinusoidal ease-in followed by ease-out.</summary>
    SineInOut,

    /// <summary>Sinusoidal ease-out followed by ease-in.</summary>
    SineOutIn,

    /// <summary>Exponential ease-in.</summary>
    ExpoIn,

    /// <summary>Exponential ease-out.</summary>
    ExpoOut,

    /// <summary>Exponential ease-in followed by ease-out.</summary>
    ExpoInOut,

    /// <summary>Exponential ease-out followed by ease-in.</summary>
    ExpoOutIn,

    /// <summary>Circular ease-in.</summary>
    CircIn,

    /// <summary>Circular ease-out.</summary>
    CircOut,

    /// <summary>Circular ease-in followed by ease-out.</summary>
    CircInOut,

    /// <summary>Circular ease-out followed by ease-in.</summary>
    CircOutIn,

    /// <summary>Back ease-in with intentional overshoot.</summary>
    BackIn,

    /// <summary>Back ease-out with intentional overshoot.</summary>
    BackOut,

    /// <summary>Back ease-in followed by ease-out with intentional overshoot.</summary>
    BackInOut,

    /// <summary>Back ease-out followed by ease-in with intentional overshoot.</summary>
    BackOutIn,

    /// <summary>Elastic ease-in with spring-like oscillation.</summary>
    ElasticIn,

    /// <summary>Elastic ease-out with spring-like oscillation.</summary>
    ElasticOut,

    /// <summary>Elastic ease-in followed by ease-out with spring-like oscillation.</summary>
    ElasticInOut,

    /// <summary>Elastic ease-out followed by ease-in with spring-like oscillation.</summary>
    ElasticOutIn,

    /// <summary>Bounce ease-in.</summary>
    BounceIn,

    /// <summary>Bounce ease-out.</summary>
    BounceOut,

    /// <summary>Bounce ease-in followed by ease-out.</summary>
    BounceInOut,

    /// <summary>Bounce ease-out followed by ease-in.</summary>
    BounceOutIn,
}

/// <summary>
/// Provides easing functions for normalized animation and interpolation time.
/// </summary>
/// <remarks>
/// <para>
/// Use <see cref="Ease"/> to evaluate one of the 41 <see cref="EaseType"/> values.
/// Input time is clamped to the normalized range before evaluation.
/// </para>
/// <para>
/// Back and elastic easing may return values outside the normalized range because
/// their curves intentionally overshoot.
/// </para>
/// <para>
/// Example:
/// <code>
/// float eased = Easing.Ease(EaseType.QuadInOut, 0.5f);
/// </code>
/// </para>
/// </remarks>
public static class Easing
{
    /// <summary>
    /// Evaluates an easing curve at the specified normalized time.
    /// </summary>
    /// <param name="type">The easing curve to evaluate.</param>
    /// <param name="t">The time value to evaluate. Values outside the normalized range are clamped.</param>
    /// <returns>
    /// The eased value. Back and elastic curves may intentionally return values
    /// outside the normalized range.
    /// </returns>
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
