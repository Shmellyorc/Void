// ============================================================================
//  RenderTarget.cs
// ============================================================================
//  Provides pooled renderer-owned off-screen render targets.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Graphics.RenderTargets;

/// <summary>
/// Provides pooled off-screen render targets backed by the active renderer.
/// </summary>
/// <remarks>
/// <para>
/// Targets are pooled by width, height, and sRGB setting. A target borrowed with
/// <see cref="Get(int,int,bool)"/> is exclusively owned by the caller until it is
/// returned with <see cref="Return"/>.
/// </para>
/// <para>
/// Always clear a target before relying on its initial contents. Returned targets
/// are cleared to transparent before entering the pool, but a newly created target
/// does not promise any initial pixel value.
/// </para>
/// <para>
/// Return each borrowed target exactly once and do not continue using the target,
/// or a texture obtained from it, after returning it to the pool. Another caller may
/// immediately receive and modify the same render target.
/// </para>
/// <code>
/// IRenderTarget target = RenderTarget.Get(320, 180);
/// target.Clear(Color.Transparent);
/// // Draw into target.
/// target.Display();
/// Texture renderedTexture = target.GetTexture();
/// // Use renderedTexture before returning target.
/// RenderTarget.Return(target);
/// </code>
/// </remarks>
public static class RenderTarget
{
    private static readonly Dictionary<(int Width, int Height, bool Srgb), Queue<IRenderTarget>> _pool = [];

    /// <summary>
    /// Gets a pooled render target with the requested size and color format.
    /// </summary>
    /// <param name="size">
    /// Requested size in pixels. The vector components are converted to integer dimensions.
    /// </param>
    /// <param name="sRGB">
    /// <see langword="true"/> to request an sRGB color target; otherwise, <see langword="false"/>.
    /// </param>
    /// <returns>A render target matching the requested dimensions and sRGB setting.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a new target must be created but no renderer is initialized.
    /// </exception>
    public static IRenderTarget Get(Vect2 size, bool sRGB = false)
    {
        var key = ((int)size.X, (int)size.Y, sRGB);

        if (_pool.TryGetValue(key, out var queue) && queue.TryDequeue(out var target))
            return target;

        return new TextureRenderTarget((int)size.X, (int)size.Y, sRGB);
    }

    /// <summary>
    /// Gets a pooled render target with the requested size and color format.
    /// </summary>
    /// <param name="width">Requested width in pixels.</param>
    /// <param name="height">Requested height in pixels.</param>
    /// <param name="sRGB">
    /// <see langword="true"/> to request an sRGB color target; otherwise, <see langword="false"/>.
    /// </param>
    /// <returns>A render target matching the requested dimensions and sRGB setting.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a new target must be created but no renderer is initialized.
    /// </exception>
    /// <remarks>
    /// This overload is equivalent to <see cref="Get(Vect2,bool)"/>.
    /// </remarks>
    public static IRenderTarget Get(int width, int height, bool sRGB = false)
        => Get(new(width, height), sRGB);

    /// <summary>
    /// Returns a render target to the pool for later reuse.
    /// </summary>
    /// <param name="target">The borrowed target to return, or null to do nothing.</param>
    /// <remarks>
    /// The target is cleared to transparent before it is queued under its current
    /// width, height, and sRGB setting. The caller must stop using the target and its
    /// exposed texture after this method returns.
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
    /// Gets a render target matching new dimensions, returning the current target
    /// to the pool when replacement is required.
    /// </summary>
    /// <param name="current">The current target, or null when no target has been allocated.</param>
    /// <param name="newWidth">Requested width in pixels.</param>
    /// <param name="newHeight">Requested height in pixels.</param>
    /// <param name="sRGB">
    /// <see langword="true"/> to request an sRGB color target; otherwise, <see langword="false"/>.
    /// </param>
    /// <returns>
    /// <paramref name="current"/> when its dimensions and sRGB setting already match;
    /// otherwise, a target borrowed from the matching pool.
    /// </returns>
    /// <remarks>
    /// When replacement occurs, <paramref name="current"/> has been returned to the
    /// pool and must no longer be used. Continue with the target returned by this method.
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
