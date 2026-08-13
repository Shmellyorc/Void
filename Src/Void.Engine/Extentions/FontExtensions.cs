namespace System;

public static class FontExtensions
{
    /// <summary>
    /// Measures the width of the specified text when rendered with this font.
    /// </summary>
    public static float MeasureWidth(this Font font, string text)
        => font.Measure(text).X;

    /// <summary>
    /// Measures the height of the specified text when rendered with this font.
    /// </summary>
    public static float MeasureHeight(this Font font, string text)
        => font.Measure(text).Y;
}
