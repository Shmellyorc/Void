namespace Void.Engine.Helpers;

public static class TextHelper
{
    private const int MaxCacheSize = 1000;

    private static readonly Dictionary<(Font font, string text, float maxWidth), string> WrapCache = new();
    private static readonly Dictionary<(Font font, string text, float maxWidth), float> MeasureCache = new();

    /// <summary>
    /// Wraps text to fit within a specified width, breaking at word boundaries when possible.
    /// </summary>
    /// <param name="font">Font used for text measurement.</param>
    /// <param name="text">Text to wrap.</param>
    /// <param name="maxWidth">Maximum width in pixels.</param>
    /// <returns>Wrapped text with newline characters inserted.</returns>
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

            if (currentWidth + wordWidth > maxWidth && currentWidth > 0f)
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
    public static string[] SplitLines(string text)
    {
        if (string.IsNullOrEmpty(text))
            return [];

        return text.Split('\n');
    }

    /// <summary>
    /// Truncates text with ellipsis to fit within a specified width.
    /// </summary>
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
    /// Truncates text with ellipsis in the middle (useful for file paths).
    /// </summary>
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

        // Get prefix
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

        // Get suffix
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
    public static string CleanText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        // Fix: More efficient approach without creating multiple intermediate arrays
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
    public static int CountLines(string text)
    {
        if (string.IsNullOrEmpty(text))
            return 0;
        
        return text.CountChar('\n') + 1;
    }

    /// <summary>
    /// Gets the width of the widest line in text.
    /// </summary>
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
    public static string PadNumber(int number, int digits)
        => number.ToString($"D{digits}");

    /// <summary>
    /// Formats time in seconds to M:SS format.
    /// </summary>
    public static string FormatTime(float seconds)
    {
        int totalSeconds = (int)Math.Abs(seconds);
        int minutes = totalSeconds / 60;
        int secs = totalSeconds % 60;

        // Fix: Add sign for negative times and pad minutes for consistency
        string sign = seconds < 0 ? "-" : "";
        return $"{sign}{minutes}:{secs:00}";
    }

    /// <summary>
    /// Formats time in seconds to H:MM:SS format.
    /// </summary>
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

// Extension method for character counting (if not already available)
public static class StringExtensions
{
    
}