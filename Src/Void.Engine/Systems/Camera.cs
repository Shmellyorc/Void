// ============================================================================
//  Camera.cs
// ============================================================================
//  Renderer-neutral 2D camera with world/screen coordinate conversion,
//  zoom, visible bounds, and optional world-space clamping.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System.Numerics;

namespace Void.Engine.Systems;

/// <summary>
/// Represents a 2D camera for world-space rendering.
/// </summary>
/// <remarks>
/// <para>
/// The camera tracks a world-space center position and zoom level, exposes the
/// currently visible world rectangle, and converts coordinates between world
/// space and the configured viewport.
/// </para>
/// <para>
/// When <see cref="Bounds"/> is non-empty, the camera position is constrained
/// so the visible view stays within those bounds where possible. If the visible
/// view is larger than the bounds on an axis, the camera is centered on that axis.
/// </para>
/// <para>
/// Example:
/// <code>
/// var camera = new Camera
/// {
///     Position = new Vect2(160f, 90f),
///     Zoom = 2f
/// };
///
/// Vect2 screen = camera.WorldToScreen(new Vect2(160f, 90f));
/// </code>
/// </para>
/// </remarks>
public sealed class Camera
{
    private Vect2 _position;
    private Rect2 _bounds;
    private float _zoom = 1f;

    /// <summary>
    /// Gets or sets the camera center in world coordinates.
    /// </summary>
    /// <remarks>
    /// Assigning a position reapplies <see cref="Bounds"/> when camera clamping is enabled.
    /// </remarks>
    public Vect2 Position
    {
        get => _position;
        set
        {
            _position = value;
            ApplyBounds();
        }
    }

    /// <summary>
    /// Gets or sets the world-space rectangle used to constrain the camera.
    /// </summary>
    /// <remarks>
    /// An empty rectangle disables clamping. Assigning new bounds immediately
    /// reapplies the constraint to the current position.
    /// </remarks>
    public Rect2 Bounds
    {
        get => _bounds;
        set
        {
            _bounds = value;
            ApplyBounds();
        }
    }

    /// <summary>
    /// Gets or sets the camera zoom factor.
    /// </summary>
    /// <remarks>
    /// Values below <c>0.1</c> are clamped to <c>0.1</c>. Increasing the zoom
    /// factor reduces the visible world area.
    /// </remarks>
    public float Zoom
    {
        get => _zoom;
        set
        {
            _zoom = Math.Max(0.1f, value);
            ApplyBounds();
        }
    }

    /// <summary>
    /// Gets the world-space rectangle currently visible through the camera.
    /// </summary>
    /// <remarks>
    /// The visible size is derived from <see cref="GameSettings.Viewport"/> and
    /// the current <see cref="Zoom"/>.
    /// </remarks>
    public Rect2 ViewBounds
    {
        get
        {
            Vect2 viewportSize = GameSettings.Instance.Viewport;
            float width = viewportSize.X / _zoom;
            float height = viewportSize.Y / _zoom;

            return new Rect2(
                _position.X - width * 0.5f,
                _position.Y - height * 0.5f,
                width,
                height);
        }
    }

    internal Matrix4x4 ViewProjectionMatrix => CreateViewProjection(ViewBounds);

    /// <summary>
    /// Creates a camera centered on the configured viewport with a zoom of <c>1.0</c>.
    /// </summary>
    public Camera()
    {
        GameSettings settings = GameSettings.Instance;
        _position = settings.Viewport / 2f;


    }

    /// <summary>
    /// Resets <see cref="Zoom"/> to <c>1.0</c>.
    /// </summary>
    public void ResetZoom() => Zoom = 1f;

    /// <summary>
    /// Converts viewport coordinates to world coordinates.
    /// </summary>
    /// <param name="screenPos">The position within the configured viewport.</param>
    /// <returns>
    /// The corresponding world-space position, or the camera position when the
    /// configured viewport has no positive size.
    /// </returns>
    public Vect2 ScreenToWorld(Vect2 screenPos)
    {
        Vect2 viewport = GameSettings.Instance.Viewport;
        Rect2 view = ViewBounds;

        if (viewport.X <= 0f || viewport.Y <= 0f)
            return _position;

        return new Vect2(
            view.Left + (screenPos.X / viewport.X) * view.Width,
            view.Top + (screenPos.Y / viewport.Y) * view.Height);
    }

    /// <summary>
    /// Converts a world-space position to viewport coordinates.
    /// </summary>
    /// <param name="worldPos">The world-space position to convert.</param>
    /// <returns>
    /// The corresponding viewport position, or <see cref="Vect2.Zero"/> when
    /// the current view has no positive size.
    /// </returns>
    public Vect2 WorldToScreen(Vect2 worldPos)
    {
        Vect2 viewport = GameSettings.Instance.Viewport;
        Rect2 view = ViewBounds;

        if (view.Width <= 0f || view.Height <= 0f)
            return Vect2.Zero;

        return new Vect2(
            ((worldPos.X - view.Left) / view.Width) * viewport.X,
            ((worldPos.Y - view.Top) / view.Height) * viewport.Y);
    }


    internal static Matrix4x4 CreateDefaultViewProjection()
    {
        Vect2 viewport = GameSettings.Instance.Viewport;
        return CreateViewProjection(new Rect2(0f, 0f, viewport.X, viewport.Y));
    }

    private static Matrix4x4 CreateViewProjection(Rect2 view)
    {
        float width = view.Width;
        float height = view.Height;

        if (width <= 0f || height <= 0f)
            return Matrix4x4.Identity;

        // System.Numerics stores matrices in row-major field order. GLShaderProgram
        // uploads this memory with transpose=false, which lets GLSL's column-vector
        // multiplication consume this transform correctly. The negative Y scale
        // preserves VOID's existing top-left / positive-Y-down coordinate system.
        float scaleX = 2f / width;
        float scaleY = -2f / height;
        float translateX = -(view.Right + view.Left) / width;
        float translateY = (view.Bottom + view.Top) / height;

        return new Matrix4x4(
            scaleX, 0f, 0f, 0f,
            0f, scaleY, 0f, 0f,
            0f, 0f, 1f, 0f,
            translateX, translateY, 0f, 1f);
    }

    private void ApplyBounds()
    {
        if (_bounds.IsEmpty)
            return;

        float halfWidth = GameSettings.Instance.Viewport.X / (2f * _zoom);
        float halfHeight = GameSettings.Instance.Viewport.Y / (2f * _zoom);

        // Avoid Math.Clamp(min > max) when a bounds rectangle is smaller than
        // the current camera view. In that case, center on the constrained axis.
        float minX = Bounds.Left + halfWidth;
        float maxX = Bounds.Right - halfWidth;
        float minY = Bounds.Top + halfHeight;
        float maxY = Bounds.Bottom - halfHeight;

        _position.X = minX <= maxX
            ? Math.Clamp(_position.X, minX, maxX)
            : (Bounds.Left + Bounds.Right) * 0.5f;

        _position.Y = minY <= maxY
            ? Math.Clamp(_position.Y, minY, maxY)
            : (Bounds.Top + Bounds.Bottom) * 0.5f;
    }

}
