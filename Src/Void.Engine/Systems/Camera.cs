// ============================================================================
//  Camera.cs
// ============================================================================
//  2D camera system with zoom, position tracking, and screen/world coordinate
//  conversion. Used for viewport rendering and spatial transformations.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Systems;

/// <summary>
/// 2D camera for viewport rendering. Controls position, zoom, and coordinate
/// conversion between screen and world space.
/// </summary>
/// <remarks>
/// Example:
/// <code>
/// var camera = new Camera();
/// camera.Position = new Vect2(100, 100);
/// camera.Zoom = 2.0f;
/// 
/// // Convert mouse position to world coordinates
/// var worldPos = camera.ScreenToWorld(mouseScreenPos);
/// </code>
/// </remarks>
public sealed class Camera
{
    internal readonly SFView _view;

    private Vect2 _position;
    private Rect2 _bounds;
    private float _zoom = 1f;

    /// <summary>
    /// Gets or sets the camera's world position.
    /// </summary>
    public Vect2 Position
    {
        get => _position;
        set
        {
            _position = value;
            ApplyBounds();
            _view.Center = _position;
        }
    }

    /// <summary>
    /// Gets or sets the camera's clamping bounds.
    /// </summary>
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
    /// Gets or sets the zoom level. Minimum 0.1x.
    /// </summary>
    public float Zoom
    {
        get => _zoom;
        set
        {
            _zoom = Math.Max(0.1f, value);
            _view.Size = GameSettings.Instance.Viewport / _zoom;
        }
    }

    /// <summary>
    /// Gets the visible world bounds based on current position and zoom.
    /// </summary>
    public Rect2 ViewBounds
    {
        get
        {
            var viewportSize = GameSettings.Instance.Viewport;
            float halfWidth = viewportSize.X / (2f * _zoom);
            float halfHeight = viewportSize.Y / (2f * _zoom);
            return new Rect2(
                _position.X - halfWidth,
                _position.Y - halfHeight,
                viewportSize.X / _zoom,
                viewportSize.Y / _zoom
            );
        }
    }

    /// <summary>
    /// Creates a new camera centered on the viewport.
    /// </summary>
    public Camera()
    {
        var settings = GameSettings.Instance;

        _view = new SFView(new SFFloatRect(Vect2.Zero, settings.Viewport));
        _position = settings.Viewport / 2f;
        _view.Center = _position;
    }

    /// <summary>
    /// Resets zoom to 1x.
    /// </summary>
    public void ResetZoom() => Zoom = 1f;

    /// <summary>
    /// Converts screen coordinates to world coordinates.
    /// </summary>
    /// <param name="screenPos">Position in screen space (pixels).</param>
    /// <returns>Position in world space.</returns>
    public Vect2 ScreenToWorld(Vect2 screenPos)
    {
        var sfPos = new SFVector2i((int)screenPos.X, (int)screenPos.Y);
        var worldPos = Game.Instance.Window._renderTexture.MapPixelToCoords(sfPos, _view);

        return new Vect2(worldPos.X, worldPos.Y);
    }

    /// <summary>
    /// Converts world coordinates to screen coordinates.
    /// </summary>
    /// <param name="worldPos">Position in world space.</param>
    /// <returns>Position in screen space (pixels).</returns>
    public Vect2 WorldToScreen(Vect2 worldPos)
    {
        var sfPos = new SFVector2f(worldPos.X, worldPos.Y);
        var screenPos = Game.Instance.Window._renderTexture.MapCoordsToPixel(sfPos, _view);

        return new Vect2(screenPos.X, screenPos.Y);
    }

    /// <summary>
    /// Implicitly converts Camera to SFView for rendering.
    /// </summary>
    public static implicit operator SFView(Camera v) => v._view;



    private void ApplyBounds()
    {
        if (_bounds.IsEmpty) return;

        float halfWidth = ViewBounds.Width / 2f;
        float halfHeight = ViewBounds.Height / 2f;

        _position.X = Math.Clamp(_position.X, Bounds.Left + halfWidth, Bounds.Right - halfWidth);
        _position.Y = Math.Clamp(_position.Y, Bounds.Top + halfHeight, Bounds.Bottom - halfHeight);
    }
}