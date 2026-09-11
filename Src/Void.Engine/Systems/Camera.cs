// ============================================================================
//  Camera.cs
// ============================================================================
//  2D camera system with renderer-neutral world/screen transforms.
//
//  Renderer-neutral camera; no SFML view object is required.
// ============================================================================

using System.Numerics;

namespace Void.Engine.Systems;

/// <summary>
/// 2D camera for viewport rendering. Controls position, zoom, clamping and
/// coordinate conversion between screen and world space.
/// </summary>
public sealed class Camera
{
    private Vect2 _position;
    private Rect2 _bounds;
    private float _zoom = 1f;

    /// <summary>Gets or sets the camera's world-space center position.</summary>
    public Vect2 Position
    {
        get => _position;
        set
        {
            _position = value;
            ApplyBounds();
        }
    }

    /// <summary>Gets or sets the camera's clamping bounds.</summary>
    public Rect2 Bounds
    {
        get => _bounds;
        set
        {
            _bounds = value;
            ApplyBounds();
        }
    }

    /// <summary>Gets or sets the zoom level. Minimum 0.1x.</summary>
    public float Zoom
    {
        get => _zoom;
        set
        {
            _zoom = Math.Max(0.1f, value);
            ApplyBounds();
        }
    }

    /// <summary>Gets the visible world bounds based on current position and zoom.</summary>
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

    /// <summary>
    /// Renderer-neutral world-to-clip matrix used by VOID's built-in 2D shader.
    /// Positive Y remains downward, matching the existing engine coordinate system.
    /// </summary>
    internal Matrix4x4 ViewProjectionMatrix => CreateViewProjection(ViewBounds);

    /// <summary>Creates a new camera centered on the configured viewport.</summary>
    public Camera()
    {
        GameSettings settings = GameSettings.Instance;
        _position = settings.Viewport / 2f;


    }

    /// <summary>Resets zoom to 1x.</summary>
    public void ResetZoom() => Zoom = 1f;

    /// <summary>Converts viewport pixel coordinates to world coordinates.</summary>
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

    /// <summary>Converts world coordinates to viewport pixel coordinates.</summary>
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


    /// <summary>
    /// Creates the no-camera transform: viewport (0,0) is the top-left and the
    /// configured viewport size maps to the bottom-right.
    /// </summary>
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
