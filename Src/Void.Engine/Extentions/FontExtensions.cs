// ============================================================================
//  FontExtensions.cs
// ============================================================================
//  Extension methods for Font measurement operations.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace System;

/// <summary>
/// Provides convenience methods for measuring text with a <see cref="Font"/>.
/// </summary>
public static class FontExtensions
{
    /// <summary>
    /// Measures the width of text using the font's normal measurement rules.
    /// </summary>
    /// <param name="font">The font used to measure the text.</param>
    /// <param name="text">The text to measure.</param>
    /// <returns>The measured width.</returns>
    public static float MeasureWidth(this Font font, string text)
        => font.Measure(text).X;

    /// <summary>
    /// Measures the height of text using the font's normal measurement rules.
    /// </summary>
    /// <param name="font">The font used to measure the text.</param>
    /// <param name="text">The text to measure.</param>
    /// <returns>The measured height.</returns>
    public static float MeasureHeight(this Font font, string text)
        => font.Measure(text).Y;
}
