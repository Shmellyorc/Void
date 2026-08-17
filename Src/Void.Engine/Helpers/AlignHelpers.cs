namespace Void.Engine.Helpers;

public enum HAlign { Left, Center, Right, Stretch }
public enum VAlign { Top, Center, Bottom, Stretch }

public static class AlignHelpers
{
    public static float AlignWidth(float parent, float child, HAlign align) =>
        AlignWidth(parent, child, align, 0f);

    public static float AlignWidth(float parent, float child, HAlign align, float offset)
    {
        var result = align switch
        {
            HAlign.Center => MathHelper.Center(parent, child),
            HAlign.Right => parent - child,
            HAlign.Stretch => 0f,
            _ => 0f
        };

        return result + offset;
    }

    public static float AlignHeight(float parent, float child, VAlign align) =>
        AlignHeight(parent, child, align, 0f);

    public static float AlignHeight(float parent, float child, VAlign align, float offset)
    {
        var result = align switch
        {
            VAlign.Center => MathHelper.Center(parent, child),
            VAlign.Bottom => parent - child,
            VAlign.Stretch => 0f,
            _ => 0f
        };

        return result + offset;
    }

    public static float StretchWidth(float parent, float padding = 0f) =>
        MathF.Max(0, parent - (padding * 2));

    public static float StretchHeight(float parent, float padding = 0f) =>
        MathF.Max(0, parent - (padding * 2));

    public static Vect2 AlignCenter(Vect2 parent, Vect2 child) =>
        AlignCenter(parent, child, Vect2.Zero);

    public static Vect2 AlignCenter(Vect2 parent, Vect2 child, Vect2 offset) =>
        Vect2.Center(parent, child, true) + offset;

    public static Vect2 AlignToViewport(Vect2 size, HAlign hAlign, VAlign vAlign) =>
        AlignToViewport(size, hAlign, vAlign, Vect2.Zero);

    public static Vect2 AlignToViewport(Vect2 size, HAlign hAlign, VAlign vAlign, Vect2 offset)
    {
        var v = GameSettings.Instance.Viewport;
        var x = AlignWidth(v.X, size.X, hAlign, offset.X);
        var y = AlignHeight(v.Y, size.Y, vAlign, offset.Y);
        return new Vect2(x, y);
    }

    public static Vect2 AlignToContainer(Vect2 containerSize, Vect2 childSize, HAlign hAlign, VAlign vAlign) =>
        AlignToContainer(containerSize, childSize, hAlign, vAlign, Vect2.Zero);

    public static Vect2 AlignToContainer(Vect2 containerSize, Vect2 childSize, HAlign hAlign, VAlign vAlign, Vect2 offset)
    {
        var x = AlignWidth(containerSize.X, childSize.X, hAlign, offset.X);
        var y = AlignHeight(containerSize.Y, childSize.Y, vAlign, offset.Y);
        return new Vect2(x, y);
    }

    public static Vect2 AlignToContainer(Vect2 containerSize, Vect2 padding, Vect2 childSize, HAlign hAlign, VAlign vAlign) =>
        AlignToContainer(containerSize, padding, childSize, hAlign, vAlign, Vect2.Zero);

    public static Vect2 AlignToContainer(Vect2 containerSize, Vect2 padding, Vect2 childSize, HAlign hAlign, VAlign vAlign, Vect2 offset)
    {
        var availableSize = containerSize - (padding * 2);
        var position = AlignToContainer(availableSize, childSize, hAlign, vAlign, offset);
        return position + padding;
    }

    public static Rect2 AlignRect(Rect2 container, Vect2 childSize, HAlign hAlign, VAlign vAlign) =>
        AlignRect(container, childSize, hAlign, vAlign, Vect2.Zero);

    public static Rect2 AlignRect(Rect2 container, Vect2 childSize, HAlign hAlign, VAlign vAlign, Vect2 offset)
    {
        var position = AlignToContainer(container.Size, childSize, hAlign, vAlign, offset);
        var size = new Vect2(
            hAlign == HAlign.Stretch ? container.Width : childSize.X,
            vAlign == VAlign.Stretch ? container.Height : childSize.Y
        );
        return new Rect2(container.Position + position, size);
    }

    public static Rect2 AlignRect(Rect2 container, Vect2 padding, Vect2 childSize, HAlign hAlign, VAlign vAlign) =>
        AlignRect(container, padding, childSize, hAlign, vAlign, Vect2.Zero);

    public static Rect2 AlignRect(Rect2 container, Vect2 padding, Vect2 childSize, HAlign hAlign, VAlign vAlign, Vect2 offset)
    {
        var paddedContainer = new Rect2(
            container.Position + padding,
            container.Size - (padding * 2)
        );
        return AlignRect(paddedContainer, childSize, hAlign, vAlign, offset);
    }

    public static float AlignPercent(float parent, float child, float percent) =>
        (parent - child) * percent;

    public static Vect2 AlignPercent(Vect2 parent, Vect2 child, Vect2 percent) =>
        new(
            AlignPercent(parent.X, child.X, percent.X),
            AlignPercent(parent.Y, child.Y, percent.Y)
        );

    public static bool HasOverflow(float containerSize, float contentSize) =>
        contentSize > containerSize;

    public static bool HasOverflow(Vect2 containerSize, Vect2 contentSize) =>
        contentSize.X > containerSize.X || contentSize.Y > containerSize.Y;

    public static float Remaining(float containerSize, float elementSize, float spacing) =>
        MathF.Max(0, containerSize - elementSize - spacing);

    public static float[] DistributeEvenly(float containerSize, int itemCount, float itemSize, float spacing)
    {
        if (itemCount <= 0) return Array.Empty<float>();
        if (itemCount == 1) return [AlignWidth(containerSize, itemSize, HAlign.Center)];

        var totalSpacing = spacing * (itemCount - 1);
        var totalItems = itemSize * itemCount;
        var startOffset = (containerSize - totalItems - totalSpacing) / 2f;

        var positions = new float[itemCount];
        for (var i = 0; i < itemCount; i++)
            positions[i] = startOffset + (i * (itemSize + spacing));

        return positions;
    }

    public static float[] DistributeEvenly(float containerSize, int itemCount, float spacing) =>
        DistributeEvenly(containerSize, itemCount, 0f, spacing);
}