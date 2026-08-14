namespace Void.Engine.Graphics.RenderTargets;

public static class RenderTarget
{
    private static readonly Dictionary<(int Width, int Height, bool Srgb), Queue<IRenderTarget>> _pool = new();
    
    public static IRenderTarget Get(int width, int height, bool sRGB = false)
    {
        var key = (width, height, sRGB);
        
        if (_pool.TryGetValue(key, out var queue) && queue.TryDequeue(out var target))
            return target;
        
        return new TextureRenderTarget(width, height, sRGB);
    }
    
    public static void Return(IRenderTarget target)
    {
        if (target == null) return;
        
        var key = (target.Width, target.Height, target.Srgb);
        
        if (!_pool.ContainsKey(key))
            _pool[key] = new Queue<IRenderTarget>();
        
        target.Clear(Color.Transparent);
        _pool[key].Enqueue(target);
    }
    
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