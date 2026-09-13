// ============================================================================
//  Camera.cs
// ============================================================================
//  Standard VOID 2D camera with position, zoom, rotation, and world bounds.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Cameras;

/// <summary>
/// Provides VOID's standard matrix-driven 2D camera.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Position"/> is the world-space point shown at the center of the
/// logical viewport. <see cref="Zoom"/> scales the visible world area and
/// <see cref="Rotation"/> rotates the camera in radians.
/// </para>
/// <para>
/// This camera derives all world/screen conversion and conservative visible
/// bounds from <see cref="BaseCamera"/>.
/// </para>
/// </remarks>
public class Camera : BaseCamera
{
    private Vect2 _position;
    private Rect2 _bounds;
    private float _zoom = 1f;
    private float _rotation;

    /// <summary>
    /// Gets or sets the world-space point shown at the center of the viewport.
    /// </summary>
    public Vect2 Position
    {
        get => _position;
        set
        {
            _position = value;
            ApplyBounds();
            Invalidate();
        }
    }

    /// <summary>
    /// Gets or sets the world-space rectangle used to constrain the camera.
    /// </summary>
    /// <remarks>
    /// An empty rectangle disables clamping. Rotation is included when calculating
    /// the visible extents that must remain inside these bounds.
    /// </remarks>
    public Rect2 Bounds
    {
        get => _bounds;
        set
        {
            _bounds = value;
            ApplyBounds();
            Invalidate();
        }
    }

    /// <summary>
    /// Gets or sets the camera zoom factor.
    /// </summary>
    /// <remarks>
    /// Values below <c>0.1</c> are clamped to <c>0.1</c>.
    /// </remarks>
    public float Zoom
    {
        get => _zoom;
        set
        {
            _zoom = MathF.Max(0.1f, value);
            ApplyBounds();
            Invalidate();
        }
    }

    /// <summary>
    /// Gets or sets the camera rotation in radians.
    /// </summary>
    /// <remarks>
    /// Positive values follow VOID's positive-Y-down 2D rotation convention.
    /// </remarks>
    public float Rotation
    {
        get => _rotation;
        set
        {
            _rotation = value;
            ApplyBounds();
            Invalidate();
        }
    }

    /// <summary>
    /// Creates a camera whose initial position is the center of the logical viewport.
    /// </summary>
    /// <remarks>
    /// The camera starts with a zoom of <c>1.0</c>, zero rotation, and no world bounds.
    /// </remarks>
    public Camera()
    {
        _position = GameSettings.Instance.Viewport * 0.5f;
    }

    /// <summary>
    /// Resets <see cref="Zoom"/> to <c>1.0</c>.
    /// </summary>
    public void ResetZoom()
        => Zoom = 1f;

    /// <summary>
    /// Creates the standard world-to-clip transform used by this camera.
    /// </summary>
    /// <returns>
    /// A matrix that centers <see cref="Position"/>, applies the inverse camera
    /// <see cref="Rotation"/>, applies <see cref="Zoom"/>, and projects into clip space.
    /// </returns>
    protected override Matrix CreateViewProjection()
    {
        Vect2 viewport = GameSettings.Instance.Viewport;

        if (viewport.X <= 0f || viewport.Y <= 0f)
            return Matrix.Identity;

        return
            Matrix.CreateTranslation(-_position)
            * Matrix.CreateRotation(-_rotation)
            * Matrix.CreateScale(_zoom)
            * Matrix.CreateOrthographic(viewport.X, viewport.Y);
    }

    private void ApplyBounds()
    {
        if (_bounds.IsEmpty)
            return;

        Vect2 viewport = GameSettings.Instance.Viewport;
        if (viewport.X <= 0f || viewport.Y <= 0f)
            return;

        float halfWidth = viewport.X / (2f * _zoom);
        float halfHeight = viewport.Y / (2f * _zoom);

        float cos = MathF.Abs(MathF.Cos(_rotation));
        float sin = MathF.Abs(MathF.Sin(_rotation));

        // Axis-aligned extents of the rotated viewport. Using these conservative
        // extents ensures all four visible corners stay inside Bounds.
        float extentX = cos * halfWidth + sin * halfHeight;
        float extentY = sin * halfWidth + cos * halfHeight;

        float minX = _bounds.Left + extentX;
        float maxX = _bounds.Right - extentX;
        float minY = _bounds.Top + extentY;
        float maxY = _bounds.Bottom - extentY;

        _position.X = minX <= maxX
            ? Math.Clamp(_position.X, minX, maxX)
            : (_bounds.Left + _bounds.Right) * 0.5f;

        _position.Y = minY <= maxY
            ? Math.Clamp(_position.Y, minY, maxY)
            : (_bounds.Top + _bounds.Bottom) * 0.5f;
    }
}
