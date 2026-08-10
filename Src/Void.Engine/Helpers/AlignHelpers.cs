namespace Void.Engine.Helpers;

public enum HAlign { Left, Center, Right }
public enum VAlign { Top, Center, Bottom }

public static class AlignHelpers
{
    public static float AlignWidth(float a, float b, HAlign align, float offset = 0f) => align switch
    {
        HAlign.Left => 0f,
        HAlign.Center => MathHelper.Center(a, b),
        HAlign.Right => a - b,
        _ => 0f
    } + offset;

    public static float AlignHeight(float a, float b, VAlign align, float offset = 0f) => align switch
    {
        VAlign.Top => 0f,
        VAlign.Center => MathHelper.Center(a, b),
        VAlign.Bottom => a - b,
        _ => 0f
    } + offset;
}
