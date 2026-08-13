namespace Void.Engine.Helpers;

public static class TextHelper
{
    public static string WrapText(Font font, string text, float maxWidth)
    {
        if (string.IsNullOrEmpty(text) || maxWidth <= 0f || font == null)
            return text;

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

                result.Append(BreakLongWord(font, word, maxWidth));
                currentWidth = wordWidth % maxWidth;
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

        return result.ToString();
    }

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

    public static string[] SplitLines(string text)
    {
        if (string.IsNullOrEmpty(text))
            return [];

        return text.Split('\n');
    }

    public static string TruncateWithEllipsis(Font font, string text, float maxWidth)
    {
        if (string.IsNullOrEmpty(text) || maxWidth <= 0f || font == null)
            return text;

        if (font.Measure(text).X <= maxWidth)
            return text;

        string ellipsis = "...";
        float ellipsisWidth = font.Measure(ellipsis).X;
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

    public static string CleanText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        text = text.Replace("\r\n", "\n").Replace('\r', '\n');

        var lines = text.Split('\n');
        for (int i = 0; i < lines.Length; i++)
            lines[i] = lines[i].TrimEnd();

        return string.Join('\n', lines);
    }

    public static float MeasureWrappedHeight(Font font, string text, float maxWidth)
    {
        string wrapped = WrapText(font, text, maxWidth);

        return font.Measure(wrapped).Y;
    }

    public static int CountLines(string text)
    {
        if (string.IsNullOrEmpty(text))
            return 0;
        
        return text.CountChar('\n') + 1;
    }

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

    public static string PadNumber(int number, int digits)
        => number.ToString($"D{digits}");

    public static string FormatTime(float seconds)
    {
        int minutes = (int)(seconds / 60f);
        int secs = (int)(seconds % 60f);

        return $"{minutes}:{secs:00}";
    }

    public static string FormatTimeLong(float seconds)
    {
        int hours = (int)(seconds / 3600f);
        int minutes = (int)((seconds % 3600f) / 60f);
        int secs = (int)(seconds % 60f);

        return $"{hours}:{minutes:00}:{secs:00}";
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
}