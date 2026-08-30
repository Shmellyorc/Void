// ============================================================================
//  TextHelper.cs
// ============================================================================
//  Text manipulation utilities including wrapping, truncation, measurement,
//  and formatting for UI and rendering systems.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections.Generic;
using System.Text;

namespace Void.Engine.Helpers;

/// <summary>
/// Provides text manipulation utilities including wrapping, truncation,
/// measurement, and formatting for UI and rendering systems.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="TextHelper"/> class provides comprehensive text processing
/// functionality for UI systems including word wrapping, character wrapping,
/// truncation with ellipsis, and text formatting.
/// </para>
/// <para>
/// <b>Key Features:</b>
/// <list type="bullet">
///   <item><description>Word wrapping with font measurement</description></item>
///   <item><description>Character-by-character wrapping</description></item>
///   <item><description>Truncation with ellipsis (end and middle)</description></item>
///   <item><description>Text cleaning and normalization</description></item>
///   <item><description>Time formatting (M:SS, H:MM:SS)</description></item>
///   <item><description>Text measurement and line counting</description></item>
///   <item><description>Indentation support for wrapped text</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Wrap text to fit within a width
/// string wrapped = TextHelper.WrapText(font, longText, 200f);
/// 
/// // Truncate with ellipsis
/// string truncated = TextHelper.TruncateWithEllipsis(font, text, 100f);
/// 
/// // Ellipsize in the middle (useful for file paths)
/// string path = TextHelper.EllipsizeMiddle(font, "C:/Users/Username/Documents/file.txt", 150f);
/// 
/// // Format time
/// string time = TextHelper.FormatTime(125.5f); // "2:05"
/// string longTime = TextHelper.FormatTimeLong(3665f); // "1:01:05"
/// 
/// // Clean text (normalize line endings)
/// string cleaned = TextHelper.CleanText(text);
/// 
/// // Wrap with indentation
/// string indented = TextHelper.WrapTextWithIndent(font, text, 200f, 20f);
/// </code>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// This class is not thread-safe. Caching operations should be performed
/// from a single thread.
/// </para>
/// </remarks>
public static class TextHelper
{
    private const int MaxCacheSize = 1000;

    private static readonly Dictionary<(Font font, string text, float maxWidth), string> WrapCache = new();
    private static readonly Dictionary<(Font font, string text, float maxWidth), float> MeasureCache = new();

    /// <summary>
    /// Wraps text to fit within a specified width, breaking at word boundaries when possible.
    /// </summary>
    /// <param name="font">The font used for text measurement.</param>
    /// <param name="text">The text to wrap.</param>
    /// <param name="maxWidth">The maximum width in pixels.</param>
    /// <returns>The wrapped text with newline characters inserted.</returns>
    public static string WrapText(Font font, string text, float maxWidth)
    {
        if (string.IsNullOrEmpty(text) || maxWidth <= 0f || font == null)
            return text;

        var cacheKey = (font, text, maxWidth);
        if (WrapCache.TryGetValue(cacheKey, out string cached))
            return cached;

        var result = new StringBuilder();
        string[] words = text.Split(' ');
        float currentWidth = 0f;
        float spaceWidth = font.Measure(" ").X;

        foreach (string word in words)
        {
            float wordWidth = font.Measure(word).X;

            if (wordWidth > maxWidth)
            {
                if (currentWidth > 0f)
                {
                    result.Append('\n');
                    currentWidth = 0f;
                }

                string brokenWord = BreakLongWord(font, word, maxWidth);
                result.Append(brokenWord);

                currentWidth = GetLastLineWidth(font, brokenWord);
                continue;
            }

            if (currentWidth + spaceWidth + wordWidth > maxWidth && currentWidth > 0f)
            {
                result.Append('\n');
                currentWidth = 0f;
            }
            else if (currentWidth > 0f)
            {
                result.Append(' ');
                currentWidth += spaceWidth;
            }

            result.Append(word);
            currentWidth += wordWidth;
        }

        AddToCache(WrapCache, cacheKey, result.ToString());
        return result.ToString();
    }

    /// <summary>
    /// Wraps text character by character to fit within a specified width.
    /// </summary>
    /// <param name="font">The font used for text measurement.</param>
    /// <param name="text">The text to wrap.</param>
    /// <param name="maxWidth">The maximum width in pixels.</param>
    /// <returns>The wrapped text with newline characters inserted.</returns>
    public static string WrapTextCharacter(Font font, string text, float maxWidth)
    {
        if (string.IsNullOrEmpty(text) || maxWidth <= 0f || font == null)
            return text;

        var result = new StringBuilder();
        float currentWidth = 0f;

        foreach (char c in text)
        {
            if (c == '\n')
            {
                result.Append('\n');
                currentWidth = 0f;
                continue;
            }

            if (c == '\t')
            {
                float tabWidth = font.Measure("    ").X;
                if (currentWidth + tabWidth > maxWidth && currentWidth > 0f)
                {
                    result.Append('\n');
                    currentWidth = 0f;
                }
                result.Append("    ");
                currentWidth += tabWidth;
                continue;
            }

            if (c == '\u00A0') // Non-breaking space
            {
                float nbspWidth = font.Measure(" ").X;
                if (currentWidth + nbspWidth > maxWidth && currentWidth > 0f)
                {
                    result.Append('\n');
                    currentWidth = 0f;
                }
                result.Append(' ');
                currentWidth += nbspWidth;
                continue;
            }

            float charWidth = font.Measure(c.ToString()).X;

            if (currentWidth + charWidth > maxWidth && currentWidth > 0f)
            {
                result.Append('\n');
                currentWidth = 0f;
            }

            result.Append(c);
            currentWidth += charWidth;
        }

        return result.ToString();
    }

    /// <summary>
    /// Splits text into lines at newline characters.
    /// </summary>
    /// <param name="text">The text to split.</param>
    /// <returns>An array of lines.</returns>
    public static string[] SplitLines(string text)
    {
        if (string.IsNullOrEmpty(text))
            return [];

        return text.Split('\n');
    }

    /// <summary>
    /// Truncates text with ellipsis to fit within a specified width.
    /// </summary>
    /// <param name="font">The font used for text measurement.</param>
    /// <param name="text">The text to truncate.</param>
    /// <param name="maxWidth">The maximum width in pixels.</param>
    /// <returns>The truncated text with ellipsis, or the original text if it fits.</returns>
    public static string TruncateWithEllipsis(Font font, string text, float maxWidth)
    {
        if (string.IsNullOrEmpty(text) || maxWidth <= 0f || font == null)
            return text;

        if (font.Measure(text).X <= maxWidth)
            return text;

        string ellipsis = "...";
        float ellipsisWidth = font.Measure(ellipsis).X;

        if (ellipsisWidth > maxWidth)
            return string.Empty;

        float availableWidth = maxWidth - ellipsisWidth;

        if (availableWidth <= 0f)
            return ellipsis;

        var result = new StringBuilder();
        float currentWidth = 0f;

        foreach (char c in text)
        {
            float charWidth = font.Measure(c.ToString()).X;
            if (currentWidth + charWidth > availableWidth)
                break;

            result.Append(c);
            currentWidth += charWidth;
        }

        return result.ToString() + ellipsis;
    }

    /// <summary>
    /// Truncates text with ellipsis in the middle, useful for file paths.
    /// </summary>
    /// <param name="font">The font used for text measurement.</param>
    /// <param name="text">The text to truncate.</param>
    /// <param name="maxWidth">The maximum width in pixels.</param>
    /// <returns>The truncated text with ellipsis in the middle.</returns>
    public static string EllipsizeMiddle(Font font, string text, float maxWidth)
    {
        if (string.IsNullOrEmpty(text) || maxWidth <= 0f || font == null)
            return text;

        if (font.Measure(text).X <= maxWidth)
            return text;

        string ellipsis = "...";
        float ellipsisWidth = font.Measure(ellipsis).X;

        if (ellipsisWidth > maxWidth)
            return string.Empty;

        float availableWidth = maxWidth - ellipsisWidth;
        float halfWidth = availableWidth / 2f;

        var prefix = new StringBuilder();
        float prefixWidth = 0f;
        for (int i = 0; i < text.Length; i++)
        {
            float charWidth = font.Measure(text[i].ToString()).X;
            if (prefixWidth + charWidth > halfWidth)
                break;
            prefix.Append(text[i]);
            prefixWidth += charWidth;
        }

        var suffix = new StringBuilder();
        float suffixWidth = 0f;
        for (int i = text.Length - 1; i >= 0; i--)
        {
            float charWidth = font.Measure(text[i].ToString()).X;
            if (suffixWidth + charWidth > halfWidth)
                break;
            suffix.Insert(0, text[i]);
            suffixWidth += charWidth;
        }

        return prefix.ToString() + ellipsis + suffix.ToString();
    }

    /// <summary>
    /// Cleans text by normalizing line endings and removing trailing whitespace.
    /// </summary>
    /// <param name="text">The text to clean.</param>
    /// <returns>The cleaned text.</returns>
    public static string CleanText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var result = new StringBuilder(text.Length);
        int lineStart = 0;

        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '\r' || text[i] == '\n')
            {
                int lineEnd = i;
                while (lineEnd > lineStart && char.IsWhiteSpace(text[lineEnd - 1]))
                    lineEnd--;

                result.Append(text, lineStart, lineEnd - lineStart);
                result.Append('\n');

                if (text[i] == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                    i++;

                lineStart = i + 1;
            }
        }

        if (lineStart < text.Length)
        {
            int lineEnd = text.Length;
            while (lineEnd > lineStart && char.IsWhiteSpace(text[lineEnd - 1]))
                lineEnd--;

            result.Append(text, lineStart, lineEnd - lineStart);
        }

        return result.ToString();
    }

    /// <summary>
    /// Measures the height of text after wrapping to a specified width.
    /// </summary>
    /// <param name="font">The font used for text measurement.</param>
    /// <param name="text">The text to measure.</param>
    /// <param name="maxWidth">The maximum width in pixels.</param>
    /// <returns>The total height of the wrapped text in pixels.</returns>
    public static float MeasureWrappedHeight(Font font, string text, float maxWidth)
    {
        if (string.IsNullOrEmpty(text) || maxWidth <= 0f || font == null)
            return 0f;

        var cacheKey = (font, text, maxWidth);
        if (MeasureCache.TryGetValue(cacheKey, out float cachedHeight))
            return cachedHeight;

        string wrapped = WrapText(font, text, maxWidth);

        var lines = wrapped.Split('\n');
        float totalHeight = 0f;
        float lineSpacing = font.LineSpacing;

        foreach (var line in lines)
        {
            float lineHeight = font.Measure(line).Y;
            totalHeight += Math.Max(lineHeight, lineSpacing);
        }

        AddToCache(MeasureCache, cacheKey, totalHeight);
        return totalHeight;
    }

    /// <summary>
    /// Counts the number of lines in text.
    /// </summary>
    /// <param name="text">The text to count lines in.</param>
    /// <returns>The number of lines.</returns>
    public static int CountLines(string text)
    {
        if (string.IsNullOrEmpty(text))
            return 0;

        return text.CountChar('\n') + 1;
    }

    /// <summary>
    /// Gets the width of the widest line in text.
    /// </summary>
    /// <param name="font">The font used for text measurement.</param>
    /// <param name="text">The text to measure.</param>
    /// <returns>The width of the widest line in pixels.</returns>
    public static float GetWidestLine(Font font, string text)
    {
        if (string.IsNullOrEmpty(text) || font == null)
            return 0f;

        float maxWidth = 0f;
        var lines = text.Split('\n');

        foreach (var line in lines)
        {
            float width = font.Measure(line).X;
            if (width > maxWidth)
                maxWidth = width;
        }

        return maxWidth;
    }

    /// <summary>
    /// Pads a number with leading zeros.
    /// </summary>
    /// <param name="number">The number to pad.</param>
    /// <param name="digits">The total number of digits.</param>
    /// <returns>The padded number as a string.</returns>
    public static string PadNumber(int number, int digits)
        => number.ToString($"D{digits}");

    /// <summary>
    /// Formats time in seconds to M:SS format.
    /// </summary>
    /// <param name="seconds">The time in seconds.</param>
    /// <returns>The formatted time string.</returns>
    public static string FormatTime(float seconds)
    {
        int totalSeconds = (int)Math.Abs(seconds);
        int minutes = totalSeconds / 60;
        int secs = totalSeconds % 60;

        string sign = seconds < 0 ? "-" : "";
        return $"{sign}{minutes}:{secs:00}";
    }

    /// <summary>
    /// Formats time in seconds to H:MM:SS format.
    /// </summary>
    /// <param name="seconds">The time in seconds.</param>
    /// <returns>The formatted time string.</returns>
    public static string FormatTimeLong(float seconds)
    {
        int totalSeconds = (int)Math.Abs(seconds);
        int hours = totalSeconds / 3600;
        int minutes = (totalSeconds % 3600) / 60;
        int secs = totalSeconds % 60;

        string sign = seconds < 0 ? "-" : "";
        return $"{sign}{hours}:{minutes:00}:{secs:00}";
    }

    /// <summary>
    /// Wraps text with a specified indentation for subsequent lines.
    /// </summary>
    /// <param name="font">The font used for text measurement.</param>
    /// <param name="text">The text to wrap.</param>
    /// <param name="maxWidth">The maximum width in pixels.</param>
    /// <param name="indentWidth">The indentation width in pixels.</param>
    /// <returns>The wrapped text with indentation applied.</returns>
    public static string WrapTextWithIndent(Font font, string text, float maxWidth, float indentWidth)
    {
        if (string.IsNullOrEmpty(text) || maxWidth <= 0f || font == null)
            return text;

        string wrapped = WrapText(font, text, maxWidth - indentWidth);
        var lines = wrapped.Split('\n');

        if (lines.Length <= 1)
            return wrapped;

        var result = new StringBuilder();
        result.Append(lines[0]);

        string indent = new string(' ', (int)(indentWidth / font.Measure(" ").X));
        for (int i = 1; i < lines.Length; i++)
        {
            result.Append('\n');
            result.Append(indent);
            result.Append(lines[i]);
        }

        return result.ToString();
    }

    /// <summary>
    /// Wraps text and returns an array of lines.
    /// </summary>
    /// <param name="font">The font used for text measurement.</param>
    /// <param name="text">The text to wrap.</param>
    /// <param name="maxWidth">The maximum width in pixels.</param>
    /// <returns>An array of wrapped lines.</returns>
    public static string[] WrapTextToLines(Font font, string text, float maxWidth)
    {
        string wrapped = WrapText(font, text, maxWidth);
        return wrapped.Split('\n');
    }

    /// <summary>
    /// Clears all caches. Call this when fonts are unloaded or changed.
    /// </summary>
    public static void ClearCaches()
    {
        WrapCache.Clear();
        MeasureCache.Clear();
    }

    private static string BreakLongWord(Font font, string word, float maxWidth)
    {
        var result = new StringBuilder();
        float currentWidth = 0f;

        foreach (char c in word)
        {
            float charWidth = font.Measure(c.ToString()).X;

            if (currentWidth + charWidth > maxWidth && currentWidth > 0f)
            {
                result.Append('\n');
                currentWidth = 0f;
            }

            result.Append(c);
            currentWidth += charWidth;
        }

        return result.ToString();
    }

    private static float GetLastLineWidth(Font font, string text)
    {
        int lastNewline = text.LastIndexOf('\n');
        string lastLine = lastNewline >= 0 ? text.Substring(lastNewline + 1) : text;
        return font.Measure(lastLine).X;
    }

    private static void AddToCache<TKey, TValue>(Dictionary<TKey, TValue> cache, TKey key, TValue value)
    {
        if (cache.Count >= MaxCacheSize)
            cache.Clear();

        cache[key] = value;
    }
}

/// <summary>
/// Provides extension methods for string manipulation.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Counts the number of occurrences of a character in a string.
    /// </summary>
    /// <param name="str">The string to search.</param>
    /// <param name="ch">The character to count.</param>
    /// <returns>The number of occurrences.</returns>
    public static int CountChar(this string str, char ch)
    {
        int count = 0;
        for (int i = 0; i < str.Length; i++)
        {
            if (str[i] == ch)
                count++;
        }
        return count;
    }
}