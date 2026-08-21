// ============================================================================
//  AlignHelpers.cs
// ============================================================================
//  Alignment utilities for UI layout including horizontal/vertical alignment,
//  stretching, distribution, and viewport/container positioning.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;

namespace Void.Engine.Helpers;

/// <summary>
/// Defines horizontal alignment options for UI elements.
/// </summary>
public enum HAlign
{
    /// <summary>Aligns the element to the left edge.</summary>
    Left,

    /// <summary>Aligns the element to the center.</summary>
    Center,

    /// <summary>Aligns the element to the right edge.</summary>
    Right,

    /// <summary>Stretches the element to fill the available width.</summary>
    Stretch
}

/// <summary>
/// Defines vertical alignment options for UI elements.
/// </summary>
public enum VAlign
{
    /// <summary>Aligns the element to the top edge.</summary>
    Top,

    /// <summary>Aligns the element to the center.</summary>
    Center,

    /// <summary>Aligns the element to the bottom edge.</summary>
    Bottom,

    /// <summary>Stretches the element to fill the available height.</summary>
    Stretch
}

/// <summary>
/// Provides alignment and distribution utilities for UI layout, including
/// horizontal/vertical alignment, stretching, even distribution, and
/// viewport/container positioning.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="AlignHelpers"/> class provides a comprehensive set of
/// methods for positioning UI elements within containers or the viewport.
/// It supports various alignment modes, padding, offset, and even distribution
/// of multiple elements.
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Align a child element within a container
/// float x = AlignHelpers.AlignWidth(containerWidth, childWidth, HAlign.Center, offsetX);
/// float y = AlignHelpers.AlignHeight(containerHeight, childHeight, VAlign.Top, offsetY);
/// 
/// // Align to viewport
/// Vect2 position = AlignHelpers.AlignToViewport(childSize, HAlign.Center, VAlign.Center, offset);
/// 
/// // Align with padding
/// Vect2 position = AlignHelpers.AlignToContainer(containerSize, padding, childSize, HAlign.Right, VAlign.Bottom);
/// 
/// // Get a complete rectangle
/// Rect2 rect = AlignHelpers.AlignRect(container, childSize, HAlign.Stretch, VAlign.Center);
/// 
/// // Distribute items evenly
/// float[] positions = AlignHelpers.DistributeEvenly(containerWidth, itemCount, itemSize, spacing);
/// </code>
/// </para>
/// </remarks>
public static class AlignHelpers
{
    /// <summary>
    /// Calculates the horizontal position of a child element within a parent.
    /// </summary>
    /// <param name="parent">The width of the parent container.</param>
    /// <param name="child">The width of the child element.</param>
    /// <param name="align">The horizontal alignment mode.</param>
    /// <returns>The X position of the child element.</returns>
    public static float AlignWidth(float parent, float child, HAlign align) =>
        AlignWidth(parent, child, align, 0f);

    /// <summary>
    /// Calculates the horizontal position of a child element within a parent with an offset.
    /// </summary>
    /// <param name="parent">The width of the parent container.</param>
    /// <param name="child">The width of the child element.</param>
    /// <param name="align">The horizontal alignment mode.</param>
    /// <param name="offset">An additional offset to apply to the position.</param>
    /// <returns>The X position of the child element.</returns>
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

    /// <summary>
    /// Calculates the vertical position of a child element within a parent.
    /// </summary>
    /// <param name="parent">The height of the parent container.</param>
    /// <param name="child">The height of the child element.</param>
    /// <param name="align">The vertical alignment mode.</param>
    /// <returns>The Y position of the child element.</returns>
    public static float AlignHeight(float parent, float child, VAlign align) =>
        AlignHeight(parent, child, align, 0f);

    /// <summary>
    /// Calculates the vertical position of a child element within a parent with an offset.
    /// </summary>
    /// <param name="parent">The height of the parent container.</param>
    /// <param name="child">The height of the child element.</param>
    /// <param name="align">The vertical alignment mode.</param>
    /// <param name="offset">An additional offset to apply to the position.</param>
    /// <returns>The Y position of the child element.</returns>
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

    /// <summary>
    /// Calculates the width for a stretched element within a parent.
    /// </summary>
    /// <param name="parent">The width of the parent container.</param>
    /// <param name="padding">The padding to subtract from both sides.</param>
    /// <returns>The stretched width.</returns>
    public static float StretchWidth(float parent, float padding = 0f) =>
        MathF.Max(0, parent - (padding * 2));

    /// <summary>
    /// Calculates the height for a stretched element within a parent.
    /// </summary>
    /// <param name="parent">The height of the parent container.</param>
    /// <param name="padding">The padding to subtract from both sides.</param>
    /// <returns>The stretched height.</returns>
    public static float StretchHeight(float parent, float padding = 0f) =>
        MathF.Max(0, parent - (padding * 2));

    /// <summary>
    /// Aligns a child element to the center of a parent.
    /// </summary>
    /// <param name="parent">The size of the parent container.</param>
    /// <param name="child">The size of the child element.</param>
    /// <returns>The centered position.</returns>
    public static Vect2 AlignCenter(Vect2 parent, Vect2 child) =>
        AlignCenter(parent, child, Vect2.Zero);

    /// <summary>
    /// Aligns a child element to the center of a parent with an offset.
    /// </summary>
    /// <param name="parent">The size of the parent container.</param>
    /// <param name="child">The size of the child element.</param>
    /// <param name="offset">An additional offset to apply to the position.</param>
    /// <returns>The centered position with offset.</returns>
    public static Vect2 AlignCenter(Vect2 parent, Vect2 child, Vect2 offset) =>
        Vect2.Center(parent, child, true) + offset;

    /// <summary>
    /// Aligns a child element within the viewport.
    /// </summary>
    /// <param name="size">The size of the child element.</param>
    /// <param name="hAlign">The horizontal alignment mode.</param>
    /// <param name="vAlign">The vertical alignment mode.</param>
    /// <returns>The aligned position within the viewport.</returns>
    public static Vect2 AlignToViewport(Vect2 size, HAlign hAlign, VAlign vAlign) =>
        AlignToViewport(size, hAlign, vAlign, Vect2.Zero);

    /// <summary>
    /// Aligns a child element within the viewport with an offset.
    /// </summary>
    /// <param name="size">The size of the child element.</param>
    /// <param name="hAlign">The horizontal alignment mode.</param>
    /// <param name="vAlign">The vertical alignment mode.</param>
    /// <param name="offset">An additional offset to apply to the position.</param>
    /// <returns>The aligned position within the viewport with offset.</returns>
    public static Vect2 AlignToViewport(Vect2 size, HAlign hAlign, VAlign vAlign, Vect2 offset)
    {
        var v = GameSettings.Instance.Viewport;
        var x = AlignWidth(v.X, size.X, hAlign, offset.X);
        var y = AlignHeight(v.Y, size.Y, vAlign, offset.Y);
        return new Vect2(x, y);
    }

    /// <summary>
    /// Aligns a child element within a container.
    /// </summary>
    /// <param name="containerSize">The size of the container.</param>
    /// <param name="childSize">The size of the child element.</param>
    /// <param name="hAlign">The horizontal alignment mode.</param>
    /// <param name="vAlign">The vertical alignment mode.</param>
    /// <returns>The aligned position within the container.</returns>
    public static Vect2 AlignToContainer(Vect2 containerSize, Vect2 childSize, HAlign hAlign, VAlign vAlign) =>
        AlignToContainer(containerSize, childSize, hAlign, vAlign, Vect2.Zero);

    /// <summary>
    /// Aligns a child element within a container with an offset.
    /// </summary>
    /// <param name="containerSize">The size of the container.</param>
    /// <param name="childSize">The size of the child element.</param>
    /// <param name="hAlign">The horizontal alignment mode.</param>
    /// <param name="vAlign">The vertical alignment mode.</param>
    /// <param name="offset">An additional offset to apply to the position.</param>
    /// <returns>The aligned position within the container with offset.</returns>
    public static Vect2 AlignToContainer(Vect2 containerSize, Vect2 childSize, HAlign hAlign, VAlign vAlign, Vect2 offset)
    {
        var x = AlignWidth(containerSize.X, childSize.X, hAlign, offset.X);
        var y = AlignHeight(containerSize.Y, childSize.Y, vAlign, offset.Y);
        return new Vect2(x, y);
    }

    /// <summary>
    /// Aligns a child element within a container with padding.
    /// </summary>
    /// <param name="containerSize">The size of the container.</param>
    /// <param name="padding">The padding to apply inside the container.</param>
    /// <param name="childSize">The size of the child element.</param>
    /// <param name="hAlign">The horizontal alignment mode.</param>
    /// <param name="vAlign">The vertical alignment mode.</param>
    /// <returns>The aligned position within the padded container.</returns>
    public static Vect2 AlignToContainer(Vect2 containerSize, Vect2 padding, Vect2 childSize, HAlign hAlign, VAlign vAlign) =>
        AlignToContainer(containerSize, padding, childSize, hAlign, vAlign, Vect2.Zero);

    /// <summary>
    /// Aligns a child element within a container with padding and an offset.
    /// </summary>
    /// <param name="containerSize">The size of the container.</param>
    /// <param name="padding">The padding to apply inside the container.</param>
    /// <param name="childSize">The size of the child element.</param>
    /// <param name="hAlign">The horizontal alignment mode.</param>
    /// <param name="vAlign">The vertical alignment mode.</param>
    /// <param name="offset">An additional offset to apply to the position.</param>
    /// <returns>The aligned position within the padded container with offset.</returns>
    public static Vect2 AlignToContainer(Vect2 containerSize, Vect2 padding, Vect2 childSize, HAlign hAlign, VAlign vAlign, Vect2 offset)
    {
        var availableSize = containerSize - (padding * 2);
        var position = AlignToContainer(availableSize, childSize, hAlign, vAlign, offset);
        return position + padding;
    }

    /// <summary>
    /// Aligns a child element within a container and returns a complete rectangle.
    /// </summary>
    /// <param name="container">The container rectangle.</param>
    /// <param name="childSize">The size of the child element.</param>
    /// <param name="hAlign">The horizontal alignment mode.</param>
    /// <param name="vAlign">The vertical alignment mode.</param>
    /// <returns>The aligned rectangle within the container.</returns>
    public static Rect2 AlignRect(Rect2 container, Vect2 childSize, HAlign hAlign, VAlign vAlign) =>
        AlignRect(container, childSize, hAlign, vAlign, Vect2.Zero);

    /// <summary>
    /// Aligns a child element within a container and returns a complete rectangle with an offset.
    /// </summary>
    /// <param name="container">The container rectangle.</param>
    /// <param name="childSize">The size of the child element.</param>
    /// <param name="hAlign">The horizontal alignment mode.</param>
    /// <param name="vAlign">The vertical alignment mode.</param>
    /// <param name="offset">An additional offset to apply to the position.</param>
    /// <returns>The aligned rectangle within the container with offset.</returns>
    public static Rect2 AlignRect(Rect2 container, Vect2 childSize, HAlign hAlign, VAlign vAlign, Vect2 offset)
    {
        var position = AlignToContainer(container.Size, childSize, hAlign, vAlign, offset);
        var size = new Vect2(
            hAlign == HAlign.Stretch ? container.Width : childSize.X,
            vAlign == VAlign.Stretch ? container.Height : childSize.Y
        );
        return new Rect2(container.Position + position, size);
    }

    /// <summary>
    /// Aligns a child element within a container with padding and returns a complete rectangle.
    /// </summary>
    /// <param name="container">The container rectangle.</param>
    /// <param name="padding">The padding to apply inside the container.</param>
    /// <param name="childSize">The size of the child element.</param>
    /// <param name="hAlign">The horizontal alignment mode.</param>
    /// <param name="vAlign">The vertical alignment mode.</param>
    /// <returns>The aligned rectangle within the padded container.</returns>
    public static Rect2 AlignRect(Rect2 container, Vect2 padding, Vect2 childSize, HAlign hAlign, VAlign vAlign) =>
        AlignRect(container, padding, childSize, hAlign, vAlign, Vect2.Zero);

    /// <summary>
    /// Aligns a child element within a container with padding and an offset, and returns a complete rectangle.
    /// </summary>
    /// <param name="container">The container rectangle.</param>
    /// <param name="padding">The padding to apply inside the container.</param>
    /// <param name="childSize">The size of the child element.</param>
    /// <param name="hAlign">The horizontal alignment mode.</param>
    /// <param name="vAlign">The vertical alignment mode.</param>
    /// <param name="offset">An additional offset to apply to the position.</param>
    /// <returns>The aligned rectangle within the padded container with offset.</returns>
    public static Rect2 AlignRect(Rect2 container, Vect2 padding, Vect2 childSize, HAlign hAlign, VAlign vAlign, Vect2 offset)
    {
        var paddedContainer = new Rect2(
            container.Position + padding,
            container.Size - (padding * 2)
        );
        return AlignRect(paddedContainer, childSize, hAlign, vAlign, offset);
    }

    /// <summary>
    /// Calculates a position based on a percentage of the available space.
    /// </summary>
    /// <param name="parent">The size of the parent container.</param>
    /// <param name="child">The size of the child element.</param>
    /// <param name="percent">The percentage (0-1) of the remaining space to offset.</param>
    /// <returns>The offset position based on the percentage.</returns>
    public static float AlignPercent(float parent, float child, float percent) =>
        (parent - child) * percent;

    /// <summary>
    /// Calculates a position based on percentages of the available space.
    /// </summary>
    /// <param name="parent">The size of the parent container.</param>
    /// <param name="child">The size of the child element.</param>
    /// <param name="percent">The percentages (0-1) of the remaining space to offset.</param>
    /// <returns>The offset position based on the percentages.</returns>
    public static Vect2 AlignPercent(Vect2 parent, Vect2 child, Vect2 percent) =>
        new(
            AlignPercent(parent.X, child.X, percent.X),
            AlignPercent(parent.Y, child.Y, percent.Y)
        );

    /// <summary>
    /// Determines whether the content overflows the container.
    /// </summary>
    /// <param name="containerSize">The size of the container.</param>
    /// <param name="contentSize">The size of the content.</param>
    /// <returns><see langword="true"/> if the content overflows; otherwise, <see langword="false"/>.</returns>
    public static bool HasOverflow(float containerSize, float contentSize) =>
        contentSize > containerSize;

    /// <summary>
    /// Determines whether the content overflows the container in either dimension.
    /// </summary>
    /// <param name="containerSize">The size of the container.</param>
    /// <param name="contentSize">The size of the content.</param>
    /// <returns><see langword="true"/> if the content overflows either dimension; otherwise, <see langword="false"/>.</returns>
    public static bool HasOverflow(Vect2 containerSize, Vect2 contentSize) =>
        contentSize.X > containerSize.X || contentSize.Y > containerSize.Y;

    /// <summary>
    /// Calculates the remaining space in a container after placing an element.
    /// </summary>
    /// <param name="containerSize">The size of the container.</param>
    /// <param name="elementSize">The size of the element.</param>
    /// <param name="spacing">The spacing to subtract.</param>
    /// <returns>The remaining space, clamped to 0.</returns>
    public static float Remaining(float containerSize, float elementSize, float spacing) =>
        MathF.Max(0, containerSize - elementSize - spacing);

    /// <summary>
    /// Distributes multiple items evenly across a container with specified spacing.
    /// </summary>
    /// <param name="containerSize">The size of the container.</param>
    /// <param name="itemCount">The number of items to distribute.</param>
    /// <param name="itemSize">The size of each item.</param>
    /// <param name="spacing">The spacing between items.</param>
    /// <returns>An array of positions for each item.</returns>
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

    /// <summary>
    /// Distributes multiple items evenly across a container with specified spacing (items have zero size).
    /// </summary>
    /// <param name="containerSize">The size of the container.</param>
    /// <param name="itemCount">The number of items to distribute.</param>
    /// <param name="spacing">The spacing between items.</param>
    /// <returns>An array of positions for each item.</returns>
    public static float[] DistributeEvenly(float containerSize, int itemCount, float spacing) =>
        DistributeEvenly(containerSize, itemCount, 0f, spacing);
}