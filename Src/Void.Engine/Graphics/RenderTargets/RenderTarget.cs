// ============================================================================
//  RenderTarget.cs
// ============================================================================
//  Provides a factory and pooling system for render target creation,
//  retrieval, and recycling. Manages a pool of render targets to reduce
//  allocations and improve performance.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Graphics.RenderTargets;

/// <summary>
/// Provides a factory and pooling system for render target creation,
/// retrieval, and recycling.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="RenderTarget"/> static class manages a pool of render targets
/// to reduce allocations and improve performance. It provides methods for:
/// <list type="bullet">
///   <item><description>Creating or retrieving render targets from the pool</description></item>
///   <item><description>Returning render targets to the pool for reuse</description></item>
///   <item><description>Resizing render targets with automatic pool management</description></item>
/// </list>
/// </para>
/// <para>
/// <b>How It Works:</b>
/// <list type="number">
///   <item><description>Call <see cref="Get(int,int,bool)"/> to obtain a render target</description></item>
///   <item><description>If a target with matching size and sRGB settings exists in the pool, it is reused</description></item>
///   <item><description>If no target is available, a new one is created</description></item>
///   <item><description>Call <see cref="Return(IRenderTarget)"/> to return the target to the pool when done</description></item>
///   <item><description>The pool automatically clears returned targets to a transparent state</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Get a render target from the pool
/// var renderTarget = RenderTarget.Get(1920, 1080, sRGB: true);
/// 
/// // Use the render target for rendering
/// renderTarget.Clear(Color.Transparent);
/// // ... draw operations ...
/// renderTarget.Display();
/// 
/// // Return the render target to the pool
/// RenderTarget.Return(renderTarget);
/// 
/// // Resize an existing render target
/// var resized = RenderTarget.Resize(renderTarget, 1280, 720);
/// </code>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// This class is not thread-safe and should be accessed from the main thread.
/// </para>
/// </remarks>
public static class RenderTarget
{
    private static readonly Dictionary<(int Width, int Height, bool Srgb), Queue<IRenderTarget>> _pool = [];

    /// <summary>
    /// Gets a render target from the pool or creates a new one.
    /// </summary>
    /// <param name="size">The size of the render target in pixels.</param>
    /// <param name="sRGB">Whether the render target should use sRGB color space.</param>
    /// <returns>A render target of the specified size and sRGB setting.</returns>
    /// <remarks>
    /// <para>
    /// This method first checks the pool for an available render target matching
    /// the requested size and sRGB setting. If found, it is returned. Otherwise,
    /// a new render target is created.
    /// </para>
    /// <para>
    /// The returned render target is cleared to transparent and ready for use.
    /// </para>
    /// </remarks>
    public static IRenderTarget Get(Vect2 size, bool sRGB = false)
    {
        var key = ((int)size.X, (int)size.Y, sRGB);

        if (_pool.TryGetValue(key, out var queue) && queue.TryDequeue(out var target))
            return target;

        return new TextureRenderTarget((int)size.X, (int)size.Y, sRGB);
    }

    /// <summary>
    /// Gets a render target from the pool or creates a new one.
    /// </summary>
    /// <param name="width">The width of the render target in pixels.</param>
    /// <param name="height">The height of the render target in pixels.</param>
    /// <param name="sRGB">Whether the render target should use sRGB color space.</param>
    /// <returns>A render target of the specified size and sRGB setting.</returns>
    /// <remarks>
    /// This is a convenience overload for <see cref="Get(Vect2,bool)"/>.
    /// </remarks>
    public static IRenderTarget Get(int width, int height, bool sRGB = false)
        => Get(new(width, height), sRGB);

    /// <summary>
    /// Returns a render target to the pool for reuse.
    /// </summary>
    /// <param name="target">The render target to return to the pool.</param>
    /// <remarks>
    /// <para>
    /// The render target is cleared to transparent before being added to the pool
    /// to ensure it is in a clean state for future use.
    /// </para>
    /// <para>
    /// The target is pooled by its size and sRGB setting. Future calls to
    /// <see cref="Get"/> with matching settings may reuse this target.
    /// </para>
    /// </remarks>
    public static void Return(IRenderTarget target)
    {
        if (target == null) return;

        var key = (target.Width, target.Height, target.Srgb);

        if (!_pool.ContainsKey(key))
            _pool[key] = new Queue<IRenderTarget>();

        target.Clear(Color.Transparent);
        _pool[key].Enqueue(target);
    }

    /// <summary>
    /// Resizes a render target, returning the existing one to the pool if needed.
    /// </summary>
    /// <param name="current">The current render target to resize.</param>
    /// <param name="newWidth">The new width in pixels.</param>
    /// <param name="newHeight">The new height in pixels.</param>
    /// <param name="sRGB">Whether the render target should use sRGB color space.</param>
    /// <returns>
    /// A render target with the new size and sRGB setting. If the existing target
    /// already matches the requested settings, it is returned unchanged.
    /// </returns>
    /// <remarks>
    /// <para>
    /// If the current render target's size or sRGB setting does not match the
    /// requested values, it is returned to the pool and a new target is retrieved.
    /// </para>
    /// <para>
    /// This method is useful for handling window resize events or changing
    /// render target sizes dynamically.
    /// </para>
    /// </remarks>
    public static IRenderTarget Resize(IRenderTarget current, int newWidth, int newHeight, bool sRGB = false)
    {
        if (current == null)
            return Get(newWidth, newHeight, sRGB);

        if (current.Width == newWidth && current.Height == newHeight && current.Srgb == sRGB)
            return current;

        Return(current);
        return Get(newWidth, newHeight, sRGB);
    }
}