// ============================================================================
//  BaseCamera.cs
// ============================================================================
//  Extensible matrix-driven base camera with cached coordinate conversion data.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Cameras;

/// <summary>
/// Provides the matrix-driven foundation for custom 2D cameras.
/// </summary>
/// <remarks>
/// <para>
/// A camera supplies a view-projection matrix through <see cref="CreateViewProjection"/>.
/// VOID caches both that matrix and its inverse until <see cref="Invalidate"/> is called.
/// </para>
/// <para>
/// Derived cameras may override <see cref="Update(FrameTime)"/> for follow behavior,
/// shake, smoothing, or other time-based camera logic without coupling those features
/// to the base camera itself.
/// </para>
/// </remarks>
public abstract class BaseCamera
{
    private Matrix _viewProjection = Matrix.Identity;
    private Matrix _inverseViewProjection = Matrix.Identity;
    private bool _dirty = true;
    private bool _hasInverse = true;

    /// <summary>
    /// Gets the current view-projection matrix.
    /// </summary>
    /// <remarks>
    /// The matrix is rebuilt only after the camera has been invalidated.
    /// </remarks>
    public Matrix ViewProjection
    {
        get
        {
            EnsureMatrices();
            return _viewProjection;
        }
    }

    /// <summary>
    /// Gets a conservative world-space axis-aligned bounding rectangle for the viewport.
    /// </summary>
    /// <remarks>
    /// The bounds are derived by inverse-transforming the four viewport corners.
    /// Rotated or skewed cameras therefore remain safe for broad-phase render culling.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current view-projection matrix is not invertible.
    /// </exception>
    public Rect2 ViewBounds
    {
        get
        {
            Vect2 viewport = GameSettings.Instance.Viewport;
            if (viewport.X <= 0f || viewport.Y <= 0f)
                return default;

            EnsureInvertible();

            Vect2 topLeft = _inverseViewProjection.TransformPoint(new Vect2(-1f, 1f));
            Vect2 topRight = _inverseViewProjection.TransformPoint(new Vect2(1f, 1f));
            Vect2 bottomLeft = _inverseViewProjection.TransformPoint(new Vect2(-1f, -1f));
            Vect2 bottomRight = _inverseViewProjection.TransformPoint(new Vect2(1f, -1f));

            float minX = MathF.Min(MathF.Min(topLeft.X, topRight.X), MathF.Min(bottomLeft.X, bottomRight.X));
            float minY = MathF.Min(MathF.Min(topLeft.Y, topRight.Y), MathF.Min(bottomLeft.Y, bottomRight.Y));
            float maxX = MathF.Max(MathF.Max(topLeft.X, topRight.X), MathF.Max(bottomLeft.X, bottomRight.X));
            float maxY = MathF.Max(MathF.Max(topLeft.Y, topRight.Y), MathF.Max(bottomLeft.Y, bottomRight.Y));

            return new Rect2(minX, minY, maxX - minX, maxY - minY);
        }
    }

    /// <summary>
    /// Gets the cached inverse view-projection matrix for derived cameras.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current view-projection matrix is not invertible.
    /// </exception>
    protected Matrix InverseViewProjection
    {
        get
        {
            EnsureInvertible();
            return _inverseViewProjection;
        }
    }

    /// <summary>
    /// Updates time-based camera behavior.
    /// </summary>
    /// <param name="frameTime">Timing information for the current update.</param>
    /// <remarks>
    /// <para>
    /// The base implementation does nothing. Derived cameras may override this method
    /// for following, shake, smoothing, transitions, or other camera-specific behavior.
    /// </para>
    /// <para>
    /// Camera instances are owned by game or scene code, so VOID does not automatically
    /// call this method for arbitrary cameras. Call it from the update path that owns the
    /// camera when the camera implements time-based behavior.
    /// </para>
    /// </remarks>
    public virtual void Update(FrameTime frameTime)
    {
    }

    /// <summary>
    /// Converts viewport coordinates to world coordinates.
    /// </summary>
    /// <param name="screenPos">Position within the configured logical viewport.</param>
    /// <returns>
    /// The corresponding world-space position, or <see cref="Vect2.Zero"/> when the
    /// configured viewport has no positive size.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current view-projection matrix is not invertible.
    /// </exception>
    public Vect2 ScreenToWorld(Vect2 screenPos)
    {
        Vect2 viewport = GameSettings.Instance.Viewport;
        if (viewport.X <= 0f || viewport.Y <= 0f)
            return Vect2.Zero;

        EnsureInvertible();

        Vect2 clip = new(
            screenPos.X / viewport.X * 2f - 1f,
            1f - screenPos.Y / viewport.Y * 2f);

        return _inverseViewProjection.TransformPoint(clip);
    }

    /// <summary>
    /// Converts a world-space position to viewport coordinates.
    /// </summary>
    /// <param name="worldPos">World-space position to convert.</param>
    /// <returns>
    /// The corresponding position within the configured logical viewport, or
    /// <see cref="Vect2.Zero"/> when the viewport has no positive size.
    /// </returns>
    public Vect2 WorldToScreen(Vect2 worldPos)
    {
        Vect2 viewport = GameSettings.Instance.Viewport;
        if (viewport.X <= 0f || viewport.Y <= 0f)
            return Vect2.Zero;

        EnsureMatrices();

        Vect2 clip = _viewProjection.TransformPoint(worldPos);

        return new Vect2(
            (clip.X + 1f) * 0.5f * viewport.X,
            (1f - clip.Y) * 0.5f * viewport.Y);
    }

    /// <summary>
    /// Creates the matrix used to transform world coordinates into clip space.
    /// </summary>
    /// <returns>The current 2D view-projection matrix.</returns>
    protected abstract Matrix CreateViewProjection();

    /// <summary>
    /// Marks the cached view-projection and inverse matrices for rebuilding.
    /// </summary>
    /// <remarks>
    /// Derived cameras should call this after changing any state that affects their matrix.
    /// </remarks>
    protected void Invalidate()
        => _dirty = true;

    private void EnsureMatrices()
    {
        if (!_dirty)
            return;

        _viewProjection = CreateViewProjection();
        _hasInverse = Matrix.TryInvert(_viewProjection, out _inverseViewProjection);
        _dirty = false;
    }

    private void EnsureInvertible()
    {
        EnsureMatrices();

        if (!_hasInverse)
            throw new InvalidOperationException("The camera view-projection matrix is not invertible.");
    }
}
