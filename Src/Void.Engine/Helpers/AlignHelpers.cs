namespace Void.Engine.Helpers;

public enum HAlign { Left, Center, Right }
public enum VAlign { Top, Center, Bottom }

/// <summary>
/// Provides helper methods for aligning sizes and positions of UI elements
/// within a parent container or viewport.
/// </summary>
public static class AlignHelpers
{
    /// <summary>
    /// Calculates the horizontal offset needed to align a child width within a parent width,
    /// using the specified alignment and no additional offset.
    /// </summary>
    /// <param name="parent">The total width of the parent container.</param>
    /// <param name="child">The width of the child element.</param>
    /// <param name="align">The horizontal alignment (<see cref="HAlign.Center"/> or <see cref="HAlign.Right"/>).</param>
    /// <returns>
    /// The X-coordinate at which the child should be placed to achieve the requested alignment.
    /// </returns>
    public static float AlignWidth(float parent, float child, HAlign align) =>
        AlignWidth(parent, child, align, 0f);

    /// <summary>
    /// Calculates the horizontal offset needed to align a child width within a parent width,
    /// using the specified alignment and an additional offset.
    /// </summary>
    /// <param name="parent">The total width of the parent container.</param>
    /// <param name="child">The width of the child element.</param>
    /// <param name="align">The horizontal alignment (<see cref="HAlign.Center"/> or <see cref="HAlign.Right"/>).</param>
    /// <param name="offset">An extra horizontal offset to apply after alignment.</param>
    /// <returns>
    /// The X-coordinate at which the child should be placed to achieve the requested alignment, plus the offset.
    /// </returns>
    public static float AlignWidth(float parent, float child, HAlign align, float offset)
    {
        var result = align switch
        {
            HAlign.Center => MathHelper.Center(parent, child, true),
            HAlign.Right => parent - child,
            _ => 0f
        };

        return result + offset;
    }

    /// <summary>
    /// Calculates the vertical offset needed to align a child height within a parent height,
    /// using the specified alignment and no additional offset.
    /// </summary>
    /// <param name="parent">The total height of the parent container.</param>
    /// <param name="child">The height of the child element.</param>
    /// <param name="align">The vertical alignment (<see cref="VAlign.Center"/> or <see cref="VAlign.Bottom"/>).</param>
    /// <returns>
    /// The Y-coordinate at which the child should be placed to achieve the requested alignment.
    /// </returns>
    public static float AlignHeight(float parent, float child, VAlign align) =>
        AlignHeight(parent, child, align, 0f);

    /// <summary>
    /// Calculates the vertical offset needed to align a child height within a parent height,
    /// using the specified alignment and an additional offset.
    /// </summary>
    /// <param name="parent">The total height of the parent container.</param>
    /// <param name="child">The height of the child element.</param>
    /// <param name="align">The vertical alignment (<see cref="VAlign.Center"/> or <see cref="VAlign.Bottom"/>).</param>
    /// <param name="offset">An extra vertical offset to apply after alignment.</param>
    /// <returns>
    /// The Y-coordinate at which the child should be placed to achieve the requested alignment, plus the offset.
    /// </returns>
    public static float AlignHeight(float parent, float child, VAlign align, float offset)
    {
        var result = align switch
        {
            VAlign.Center => MathHelper.Center(parent, child, true),
            VAlign.Bottom => parent - child,
            _ => 0f
        };

        return result + offset;
    }

    /// <summary>
    /// Centers a child size within a parent size with no additional offset.
    /// </summary>
    /// <param name="parent">The size of the parent container.</param>
    /// <param name="child">The size of the child element.</param>
    /// <returns>A <see cref="Vect2"/> representing the centered position.</returns>
    public static Vect2 AlignCenter(Vect2 parent, Vect2 child) =>
        AlignCenter(parent, child, Vect2.Zero);

    /// <summary>
    /// Centers a child size within a parent size with an additional offset.
    /// </summary>
    /// <param name="parent">The size of the parent container.</param>
    /// <param name="child">The size of the child element.</param>
    /// <param name="offset">An extra positional offset to apply after centering.</param>
    /// <returns>A <see cref="Vect2"/> representing the centered position plus offset.</returns>
    public static Vect2 AlignCenter(Vect2 parent, Vect2 child, Vect2 offset) =>
        Vect2.Center(parent, child, true) + offset;

    /// <summary>
    /// Calculates the position needed to align an element of the given size within the viewport,
    /// using the specified horizontal and vertical alignments with no extra offset.
    /// </summary>
    /// <param name="size">The size of the element to align.</param>
    /// <param name="hAlign">Horizontal alignment within the viewport.</param>
    /// <param name="vAlign">Vertical alignment within the viewport.</param>
    /// <returns>A <see cref="Vect2"/> representing the aligned position.</returns>
    public static Vect2 AlignToViewport(Vect2 size, HAlign hAlign, VAlign vAlign) =>
        AlignToViewport(size, hAlign, vAlign, Vect2.Zero);

    /// <summary>
    /// Calculates the position needed to align an element of the given size within the viewport,
    /// using the specified horizontal and vertical alignments with an additional offset.
    /// </summary>
    /// <param name="size">The size of the element to align.</param>
    /// <param name="hAlign">Horizontal alignment within the viewport.</param>
    /// <param name="vAlign">Vertical alignment within the viewport.</param>
    /// <param name="offset">An extra positional offset to apply after alignment.</param>
    /// <returns>A <see cref="Vect2"/> representing the aligned position.</returns>
    public static Vect2 AlignToViewport(Vect2 size, HAlign hAlign, VAlign vAlign, Vect2 offset)
    {
        var v = GameSettings.Instance.Viewport;
        var x = AlignWidth(v.X, size.X, hAlign, offset.X);
        var y = AlignHeight(v.Y, size.Y, vAlign, offset.Y);
        return new Vect2(x, y);
    }

    /// <summary>
    /// Calculates the position needed to align a child size within a parent container,
    /// using the specified horizontal and vertical alignments with no extra offset.
    /// </summary>
    /// <param name="containerSize">The size of the parent container.</param>
    /// <param name="childSize">The size of the child element to align.</param>
    /// <param name="hAlign">Horizontal alignment within the container.</param>
    /// <param name="vAlign">Vertical alignment within the container.</param>
    /// <returns>A <see cref="Vect2"/> representing the aligned position.</returns>
    public static Vect2 AlignToContainer(Vect2 containerSize, Vect2 childSize, HAlign hAlign, VAlign vAlign) =>
        AlignToContainer(containerSize, childSize, hAlign, vAlign, Vect2.Zero);

    /// <summary>
    /// Calculates the position needed to align a child size within a parent container,
    /// using the specified horizontal and vertical alignments with an additional offset.
    /// </summary>
    /// <param name="containerSize">The size of the parent container.</param>
    /// <param name="childSize">The size of the child element to align.</param>
    /// <param name="hAlign">Horizontal alignment within the container.</param>
    /// <param name="vAlign">Vertical alignment within the container.</param>
    /// <param name="offset">An extra positional offset to apply after alignment.</param>
    /// <returns>A <see cref="Vect2"/> representing the aligned position.</returns>
    public static Vect2 AlignToContainer(Vect2 containerSize, Vect2 childSize, HAlign hAlign, VAlign vAlign, Vect2 offset)
    {
        var x = AlignWidth(containerSize.X, childSize.X, hAlign, offset.X);
        var y = AlignHeight(containerSize.Y, childSize.Y, vAlign, offset.Y);
        return new Vect2(x, y);
    }

    /// <summary>
    /// Calculates the remaining space inside a container after placing an element and applying spacing.
    /// </summary>
    /// <param name="containerSize">The total size of the container (width or height).</param>
    /// <param name="elementSize">The size of the placed element (width or height).</param>
    /// <param name="spacing">Additional spacing around or between elements.</param>
    /// <returns>
    /// The non-negative space left over in the container after subtracting <paramref name="elementSize"/> and <paramref name="spacing"/>.
    /// </returns>
    public static float Remaining(float containerSize, float elementSize, float spacing) =>
        MathF.Max(0, containerSize - elementSize - spacing);
}