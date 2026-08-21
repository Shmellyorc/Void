// ============================================================================
//  FontExtensions.cs
// ============================================================================
//  Extension methods for Font measurement operations.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace System;

/// <summary>
/// Provides extension methods for Font measurement operations.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="FontExtensions"/> class provides convenience methods for
/// measuring text dimensions using a font, simplifying the API for common
/// measurement tasks.
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// Font font = // ... load font
/// 
/// // Measure text width
/// float width = font.MeasureWidth("Hello World");
/// 
/// // Measure text height
/// float height = font.MeasureHeight("Hello World");
/// </code>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// These extension methods are thread-safe as they delegate to the underlying
/// font's measurement method.
/// </para>
/// </remarks>
public static class FontExtensions
{
    /// <summary>
    /// Measures the width of the specified text when rendered with this font.
    /// </summary>
    /// <param name="font">The font to use for measurement.</param>
    /// <param name="text">The text to measure.</param>
    /// <returns>The width of the text in pixels.</returns>
    public static float MeasureWidth(this Font font, string text)
        => font.Measure(text).X;

    /// <summary>
    /// Measures the height of the specified text when rendered with this font.
    /// </summary>
    /// <param name="font">The font to use for measurement.</param>
    /// <param name="text">The text to measure.</param>
    /// <returns>The height of the text in pixels.</returns>
    public static float MeasureHeight(this Font font, string text)
        => font.Measure(text).Y;
}