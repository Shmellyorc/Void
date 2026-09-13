// ============================================================================
//  TextHelper.cs
// ============================================================================
//  Text wrapping, truncation, measurement, cleanup, and formatting helpers.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections.Generic;
using System.Text;
using Void.Engine.Assets.Loaders.Fonts;

namespace Void.Engine.Helpers;

/// <summary>
/// Provides text wrapping, truncation, measurement, cleanup, and formatting helpers.
/// </summary>
/// <remarks>
/// Wrapping uses <see cref="Font.Measure(string)"/> and preserves explicit newline
/// boundaries. Carriage returns are ignored by wrapping so CRLF input behaves like
/// LF input. The internal caches are not synchronized; call these helpers from one
/// thread or provide external synchronization.
/// </remarks>
public static class TextHelper
{
    private const int MaxCacheSize = 1000;

    private static readonly Dictionary<(Font font, string text, float maxWidth), string> WrapCache = new();
    private static readonly Dictionary<(Font font, string text, float maxWidth), float> MeasureCache = new();

    /// <summary>Wraps text to a maximum width, preferring word boundaries.</summary>
    public static string WrapText(Font font, string text, float maxWidth)
    {
        if (string.IsNullOrEmpty(text) || maxWidth <= 0f || font == null)
            return text;

        var cacheKey = (font, text, maxWidth);
        if (WrapCache.TryGetValue(cacheKey, out string cached))
            return cached;

        string wrapped = WrapTextCore(font, text, maxWidth, maxWidth);
        AddToCache(WrapCache, cacheKey, wrapped);
        return wrapped;
    }

    /// <summary>Wraps text character by character to a maximum width.</summary>
    public static string WrapTextCharacter(Font font, string text, float maxWidth)
    {
        if (string.IsNullOrEmpty(text) || maxWidth <= 0f || font == null)
            return text;

        var result = new StringBuilder(text.Length);
        float currentWidth = 0f;

        foreach (char c in text)
        {
            if (c == '\r')
                continue;

            if (c == '\n')
            {
                result.Append('\n');
                currentWidth = 0f;
                continue;
            }

            if (c == '\t')
            {
                AppendMeasuredToken(font, result, "    ", maxWidth, ref currentWidth);
                continue;
            }

            if (c == '\u00A0')
            {
                AppendMeasuredToken(font, result, " ", maxWidth, ref currentWidth);
                continue;
            }

            string value = c.ToString();
            float width = font.Measure(value).X;
            if (currentWidth > 0f && currentWidth + width > maxWidth)
            {
                result.Append('\n');
                currentWidth = 0f;
            }

            result.Append(c);
            currentWidth += width;
        }

        return result.ToString();
    }

    /// <summary>Splits text into logical lines while normalizing CRLF and standalone CR line endings.</summary>
    public static string[] SplitLines(string text)
    {
        if (string.IsNullOrEmpty(text))
            return [];

        return text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
    }

    /// <summary>Truncates text at the end and adds an ellipsis when it exceeds a width.</summary>
    public static string TruncateWithEllipsis(Font font, string text, float maxWidth)
    {
        if (string.IsNullOrEmpty(text) || maxWidth <= 0f || font == null)
            return text;

        if (font.Measure(text).X <= maxWidth)
            return text;

        const string ellipsis = "...";
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
            if (c == '\r')
                continue;

            float charWidth = font.Measure(c.ToString()).X;
            if (currentWidth + charWidth > availableWidth)
                break;

            result.Append(c);
            currentWidth += charWidth;
        }

        return result + ellipsis;
    }

    /// <summary>Truncates text in the middle and inserts an ellipsis when it exceeds a width.</summary>
    public static string EllipsizeMiddle(Font font, string text, float maxWidth)
    {
        if (string.IsNullOrEmpty(text) || maxWidth <= 0f || font == null)
            return text;

        if (font.Measure(text).X <= maxWidth)
            return text;

        const string ellipsis = "...";
        float ellipsisWidth = font.Measure(ellipsis).X;
        if (ellipsisWidth > maxWidth)
            return string.Empty;

        float halfWidth = (maxWidth - ellipsisWidth) / 2f;
        var prefix = new StringBuilder();
        float prefixWidth = 0f;

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (c == '\r')
                continue;

            float charWidth = font.Measure(c.ToString()).X;
            if (prefixWidth + charWidth > halfWidth)
                break;

            prefix.Append(c);
            prefixWidth += charWidth;
        }

        var suffix = new StringBuilder();
        float suffixWidth = 0f;

        for (int i = text.Length - 1; i >= 0; i--)
        {
            char c = text[i];
            if (c == '\r')
                continue;

            float charWidth = font.Measure(c.ToString()).X;
            if (suffixWidth + charWidth > halfWidth)
                break;

            suffix.Insert(0, c);
            suffixWidth += charWidth;
        }

        return prefix.ToString() + ellipsis + suffix;
    }

    /// <summary>Normalizes line endings to LF and removes trailing whitespace from each line.</summary>
    public static string CleanText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var result = new StringBuilder(text.Length);
        int lineStart = 0;

        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] != '\r' && text[i] != '\n')
                continue;

            int lineEnd = i;
            while (lineEnd > lineStart && char.IsWhiteSpace(text[lineEnd - 1]))
                lineEnd--;

            result.Append(text, lineStart, lineEnd - lineStart);
            result.Append('\n');

            if (text[i] == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                i++;

            lineStart = i + 1;
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

    /// <summary>Measures the rendered height after word wrapping to a maximum width.</summary>
    public static float MeasureWrappedHeight(Font font, string text, float maxWidth)
    {
        if (string.IsNullOrEmpty(text) || maxWidth <= 0f || font == null)
            return 0f;

        var cacheKey = (font, text, maxWidth);
        if (MeasureCache.TryGetValue(cacheKey, out float cachedHeight))
            return cachedHeight;

        string wrapped = WrapText(font, text, maxWidth);
        float lineHeight = font.LineHeight + font.LineSpacing;
        float totalHeight = CountLines(wrapped) * lineHeight;

        AddToCache(MeasureCache, cacheKey, totalHeight);
        return totalHeight;
    }

    /// <summary>Counts LF-delimited logical lines in text.</summary>
    public static int CountLines(string text)
    {
        if (string.IsNullOrEmpty(text))
            return 0;

        int count = 1;
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '\n')
                count++;
            else if (text[i] == '\r' && (i + 1 >= text.Length || text[i + 1] != '\n'))
                count++;
        }

        return count;
    }

    /// <summary>Gets the width of the widest logical line.</summary>
    public static float GetWidestLine(Font font, string text)
    {
        if (string.IsNullOrEmpty(text) || font == null)
            return 0f;

        float maxWidth = 0f;
        foreach (string line in SplitLines(text))
            maxWidth = MathF.Max(maxWidth, font.Measure(line).X);

        return maxWidth;
    }

    /// <summary>Pads an integer with leading zeroes to the requested digit count.</summary>
    public static string PadNumber(int number, int digits)
        => number.ToString($"D{digits}");

    /// <summary>Formats seconds as signed M:SS text.</summary>
    public static string FormatTime(float seconds)
    {
        int totalSeconds = (int)Math.Abs(seconds);
        int minutes = totalSeconds / 60;
        int secs = totalSeconds % 60;
        string sign = seconds < 0f ? "-" : "";
        return $"{sign}{minutes}:{secs:00}";
    }

    /// <summary>Formats seconds as signed H:MM:SS text.</summary>
    public static string FormatTimeLong(float seconds)
    {
        int totalSeconds = (int)Math.Abs(seconds);
        int hours = totalSeconds / 3600;
        int minutes = totalSeconds % 3600 / 60;
        int secs = totalSeconds % 60;
        string sign = seconds < 0f ? "-" : "";
        return $"{sign}{hours}:{minutes:00}:{secs:00}";
    }

    /// <summary>
    /// Wraps text so the first rendered line can use the full width and every
    /// subsequent line is indented by approximately <paramref name="indentWidth"/> pixels.
    /// </summary>
    public static string WrapTextWithIndent(Font font, string text, float maxWidth, float indentWidth)
    {
        if (string.IsNullOrEmpty(text) || maxWidth <= 0f || font == null)
            return text;

        indentWidth = MathF.Max(0f, indentWidth);
        if (indentWidth <= 0f)
            return WrapText(font, text, maxWidth);

        float continuationWidth = maxWidth - indentWidth;
        if (continuationWidth <= 0f)
            continuationWidth = maxWidth;

        string wrapped = WrapTextCore(font, text, maxWidth, continuationWidth);
        string[] lines = wrapped.Split('\n');
        if (lines.Length <= 1)
            return wrapped;

        float spaceWidth = font.Measure(" ").X;
        int indentSpaces = spaceWidth > MathHelper.Epsilon
            ? Math.Max(0, (int)(indentWidth / spaceWidth))
            : 0;
        string indent = indentSpaces > 0 ? new string(' ', indentSpaces) : string.Empty;

        var result = new StringBuilder(wrapped.Length + indent.Length * (lines.Length - 1));
        result.Append(lines[0]);

        for (int i = 1; i < lines.Length; i++)
        {
            result.Append('\n');
            result.Append(indent);
            result.Append(lines[i]);
        }

        return result.ToString();
    }

    /// <summary>Wraps text and returns the resulting logical lines.</summary>
    public static string[] WrapTextToLines(Font font, string text, float maxWidth)
    {
        if (string.IsNullOrEmpty(text))
            return [];

        string wrapped = WrapText(font, text, maxWidth);
        return wrapped?.Split('\n') ?? [];
    }

    /// <summary>Clears cached wrap strings and wrapped-height measurements.</summary>
    public static void ClearCaches()
    {
        WrapCache.Clear();
        MeasureCache.Clear();
    }

    private static string WrapTextCore(Font font, string text, float firstLineWidth, float continuationWidth)
    {
        string normalized = text.Replace("\r", string.Empty);
        string[] sourceLines = normalized.Split('\n');
        var result = new StringBuilder(normalized.Length + 16);
        bool firstOutputLine = true;

        for (int i = 0; i < sourceLines.Length; i++)
        {
            if (i > 0)
            {
                result.Append('\n');
                firstOutputLine = false;
            }

            AppendWrappedSourceLine(
                font,
                sourceLines[i],
                firstLineWidth,
                continuationWidth,
                result,
                ref firstOutputLine);
        }

        return result.ToString();
    }

    private static void AppendWrappedSourceLine(
        Font font,
        string line,
        float firstLineWidth,
        float continuationWidth,
        StringBuilder result,
        ref bool firstOutputLine)
    {
        if (line.Length == 0)
            return;

        float currentWidth = 0f;
        float spaceWidth = font.Measure(" ").X;
        string[] words = line.Split(' ');

        foreach (string word in words)
        {
            float lineLimit = firstOutputLine ? firstLineWidth : continuationWidth;
            float wordWidth = font.Measure(word).X;
            float requiredWidth = currentWidth > 0f ? currentWidth + spaceWidth + wordWidth : wordWidth;

            if (currentWidth > 0f && requiredWidth > lineLimit)
            {
                result.Append('\n');
                firstOutputLine = false;
                currentWidth = 0f;
                lineLimit = continuationWidth;
            }

            if (wordWidth > lineLimit && word.Length > 0)
            {
                if (currentWidth > 0f)
                {
                    result.Append('\n');
                    firstOutputLine = false;
                    currentWidth = 0f;
                }

                AppendLongWord(font, word, firstLineWidth, continuationWidth, result, ref currentWidth, ref firstOutputLine);
                continue;
            }

            if (currentWidth > 0f)
            {
                result.Append(' ');
                currentWidth += spaceWidth;
            }

            result.Append(word);
            currentWidth += wordWidth;
        }
    }

    private static void AppendLongWord(
        Font font,
        string word,
        float firstLineWidth,
        float continuationWidth,
        StringBuilder result,
        ref float currentWidth,
        ref bool firstOutputLine)
    {
        foreach (char c in word)
        {
            float lineLimit = firstOutputLine ? firstLineWidth : continuationWidth;

            float charWidth = font.Measure(c.ToString()).X;
            if (currentWidth > 0f && currentWidth + charWidth > lineLimit)
            {
                result.Append('\n');
                firstOutputLine = false;
                currentWidth = 0f;
                lineLimit = continuationWidth;
            }

            result.Append(c);
            currentWidth += charWidth;
        }
    }

    private static void AppendMeasuredToken(Font font, StringBuilder result, string token, float maxWidth, ref float currentWidth)
    {
        float width = font.Measure(token).X;
        if (currentWidth > 0f && currentWidth + width > maxWidth)
        {
            result.Append('\n');
            currentWidth = 0f;
        }

        result.Append(token);
        currentWidth += width;
    }

    private static void AddToCache<TKey, TValue>(Dictionary<TKey, TValue> cache, TKey key, TValue value)
    {
        if (cache.Count >= MaxCacheSize)
            cache.Clear();

        cache[key] = value;
    }
}

/// <summary>Provides small string-oriented utility extensions.</summary>
public static class StringExtensions
{
    /// <summary>Counts occurrences of a character in a string.</summary>
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
