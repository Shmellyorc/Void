// ============================================================================
//  AtlasManager.cs
// ============================================================================
//  Renderer-neutral texture atlas. Pages are CPU-shadowed RGBA8 buffers backed
//  by IGraphicsTexture resources owned by the active renderer.
// ============================================================================

using Void.Engine.Assets.Loaders;
using Void.Engine.Assets.Loaders.Fonts;
using Void.Engine.Graphics.Rendering;

namespace Void.Engine.Graphics.Atlas;

/// <summary>
/// Manages texture atlasing with automatic page allocation, LRU eviction and
/// incremental defragmentation without depending on a specific graphics API.
/// </summary>
public sealed class AtlasManager
{
    private struct AtlasSlot
    {
        public int PageId;
        public Rect2 PackedRect;
        public LinkedListNode<(uint, Rect2)> LruNode;
        public ulong LastUsedBatch;
    }

    private readonly struct PendingDefragMove
    {
        public int PageId { get; }
        public Rect2 OldRect { get; }
        public Rect2 NewRect { get; }

        public PendingDefragMove(int pageId, Rect2 oldRect, Rect2 newRect)
        {
            PageId = pageId;
            OldRect = oldRect;
            NewRect = newRect;
        }
    }

    private sealed class AtlasPage
    {
        public Texture Texture;
        public IGraphicsTexture GraphicsTexture;
        public IGraphicsDevice GraphicsDevice;
        public byte[] Pixels;
        public IAtlasPacker Packer;
        public bool IsActive;

        public AtlasPage(IAtlasPacker packer)
        {
            Packer = packer;
        }

        public void DisposeGraphics()
        {
            Texture?.Dispose();
            Texture = null;

            GraphicsTexture?.Dispose();
            GraphicsTexture = null;
            GraphicsDevice = null;
        }
    }

    private static readonly Lazy<AtlasManager> _instance = new(() => new AtlasManager());
    private readonly Dictionary<(uint TextureId, Rect2 SrcRect), AtlasSlot> _packedMap = [];
    private readonly List<AtlasPage> _pages = [];
    private readonly LinkedList<(uint TextureId, Rect2 SrcRect)> _lruList = [];
    private readonly Queue<PendingDefragMove> _pendingDefragMoves = [];
    private readonly HashSet<int> _pagesWithPendingMoves = [];

    private int _pageSize;
    private int _pageCount;
    private int _evictionCount;
    private ulong _usageBatch;
    private bool _isDefragging;

    public static AtlasManager Instance => _instance.Value;
    public bool IsDefragging => _isDefragging;
    public int PendingDefragMoves => _pendingDefragMoves.Count;

    private AtlasManager()
    {
        Initialize();
    }

    private void Initialize()
    {
        var settings = GameSettings.Instance;
        _pageSize = settings.AtlasPageSize;
        _pageCount = settings.AtlasPageCount;

        Logger.Instance.InfoWithCategory("Atlas", "Initializing renderer atlas: {0} pages of {1}x{1}",
            _pageCount, _pageSize);

        for (int i = 0; i < _pageCount; i++)
        {
            var pagePacker = settings.AtlasPacker != null
                ? (IAtlasPacker)Activator.CreateInstance(settings.AtlasPacker, [_pageSize, _pageSize])
                : new SkylinePacker(_pageSize, _pageSize);

            _pages.Add(new AtlasPage(pagePacker));
        }
    }

    internal void BeginBatchUsage()
    {
        unchecked
        {
            _usageBatch++;
        }
    }

    public bool TryPack(Texture texture, Rect2 srcRect, out Rect2 packedRect, out int pageId)
    {
        if (texture == null)
        {
            packedRect = default;
            pageId = -1;
            return false;
        }

        if (TryGetCached(texture.Id, srcRect, out packedRect, out pageId))
            return true;

        if (!texture.TryCopyPixelRegion(srcRect, out byte[] pixels, out int width, out int height))
        {
            packedRect = default;
            pageId = -1;
            return false;
        }

        return TryPackNew(texture.Id, srcRect, pixels, width, height, out packedRect, out pageId);
    }

    internal bool TryPack(Font font, Rect2 srcRect, out Rect2 packedRect, out int pageId)
    {
        if (font == null)
        {
            packedRect = default;
            pageId = -1;
            return false;
        }

        if (TryGetCached(font.Id, srcRect, out packedRect, out pageId))
            return true;

        if (!font.TryCopyPixelRegion(srcRect, out byte[] pixels, out int width, out int height))
        {
            packedRect = default;
            pageId = -1;
            return false;
        }

        return TryPackNew(font.Id, srcRect, pixels, width, height, out packedRect, out pageId);
    }

    private bool TryGetCached(uint textureId, Rect2 srcRect, out Rect2 packedRect, out int pageId)
    {
        var key = (textureId, srcRect);
        if (_packedMap.TryGetValue(key, out var slot))
        {
            if (_pagesWithPendingMoves.Contains(slot.PageId))
            {
                packedRect = default;
                pageId = -1;
                return false;
            }

            packedRect = slot.PackedRect;
            pageId = slot.PageId;

            if (slot.LastUsedBatch != _usageBatch)
            {
                _lruList.Remove(slot.LruNode);
                _lruList.AddFirst(slot.LruNode);
                slot.LastUsedBatch = _usageBatch;
                _packedMap[key] = slot;
            }

            return true;
        }

        packedRect = default;
        pageId = -1;
        return false;
    }

    private bool TryPackNew(
        uint textureId,
        Rect2 srcRect,
        byte[] pixels,
        int width,
        int height,
        out Rect2 packedRect,
        out int pageId)
    {
        packedRect = default;
        pageId = -1;

        if (width <= 0 || height <= 0 || width > _pageSize || height > _pageSize)
        {
            Logger.Instance.WarningWithCategory("Atlas",
                "Texture {0}x{1} exceeds page size {2}x{2}", width, height, _pageSize);
            return false;
        }

        var key = (textureId, srcRect);

        for (int i = 0; i < _pages.Count; i++)
        {
            if (_pagesWithPendingMoves.Contains(i))
                continue;
            if (!EnsurePageResources(i))
                continue;

            var page = _pages[i];
            if (page.Packer.TryPack(width, height, out var rect))
                return PackIntoPage(pixels, width, height, key, i, rect, out packedRect, out pageId);
        }

        int bestPageIndex = -1;
        float highestFragmentation = 0f;

        for (int i = 0; i < _pages.Count; i++)
        {
            if (_pagesWithPendingMoves.Contains(i))
                continue;

            var page = _pages[i];
            if (page.Packer.Fragmentation > highestFragmentation)
            {
                highestFragmentation = page.Packer.Fragmentation;
                bestPageIndex = i;
            }
        }

        if (bestPageIndex >= 0 && highestFragmentation > GameSettings.Instance.AtlasDefragThreshold)
        {
            var page = _pages[bestPageIndex];
            var pageTextures = _packedMap
                .Where(kvp => kvp.Value.PageId == bestPageIndex)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.PackedRect);

            var moves = page.Packer.Defrag();
            if (moves.Count > 0)
            {
                PrepareAndQueueDefrag(bestPageIndex, moves);

                foreach (var kvp in pageTextures)
                {
                    var move = moves.FirstOrDefault(m => m.OldRect == kvp.Value);
                    if (move != default)
                    {
                        var existingSlot = _packedMap[kvp.Key];
                        existingSlot.PackedRect = move.NewRect;
                        _packedMap[kvp.Key] = existingSlot;
                    }
                }

                // The page is deliberately unavailable until its final layout has
                // been uploaded over the configured number of frames. The caller
                // falls back to the source texture for this draw.
                return false;
            }
        }

        if (EvictAndRepack(key, pixels, width, height, out packedRect, out pageId))
            return true;

        Logger.Instance.WarningWithCategory("Atlas",
            "Failed to pack texture {0}x{1} - atlas full or too fragmented", width, height);
        return false;
    }

    private bool PackIntoPage(
        byte[] pixels,
        int width,
        int height,
        (uint TextureId, Rect2 SrcRect) key,
        int pageIndex,
        Rect2 rect,
        out Rect2 packedRect,
        out int pageId)
    {
        var page = _pages[pageIndex];
        if (!EnsurePageResources(pageIndex))
        {
            page.Packer.Free(rect);
            packedRect = default;
            pageId = -1;
            return false;
        }

        int dstX = (int)rect.X;
        int dstY = (int)rect.Y;
        RgbaPixelBuffer.Blit(pixels, width, height, page.Pixels, _pageSize, _pageSize, dstX, dstY);
        page.GraphicsDevice.UpdateTexture(
            page.GraphicsTexture,
            dstX,
            dstY,
            width,
            height,
            TextureFormat.RGBA8,
            pixels);

        var lruNode = _lruList.AddFirst(key);
        _packedMap[key] = new AtlasSlot
        {
            PageId = pageIndex,
            PackedRect = rect,
            LruNode = lruNode,
            LastUsedBatch = _usageBatch
        };

        Logger.Instance.DebugWithCategory("Atlas", "Packed {0}x{1} into renderer page {2} (total: {3})",
            width, height, pageIndex, _packedMap.Count);

        packedRect = rect;
        pageId = pageIndex;
        return true;
    }

    public bool ProcessPendingDefragMoves(int maxMovesPerFrame = 5)
    {
        if (_pendingDefragMoves.Count == 0)
        {
            _isDefragging = false;
            _pagesWithPendingMoves.Clear();
            return false;
        }

        int movesProcessed = 0;
        while (_pendingDefragMoves.Count > 0 && movesProcessed < maxMovesPerFrame)
        {
            var move = _pendingDefragMoves.Dequeue();
            var page = _pages[move.PageId];

            if (EnsurePageResources(move.PageId))
            {
                UploadRegion(page, move.OldRect);
                UploadRegion(page, move.NewRect);
            }

            movesProcessed++;
        }

        if (_pendingDefragMoves.Count == 0)
        {
            _isDefragging = false;
            _pagesWithPendingMoves.Clear();
            Logger.Instance.DebugWithCategory("Atlas", "Renderer atlas defragmentation complete");
        }

        return _pendingDefragMoves.Count > 0;
    }

    private void PrepareAndQueueDefrag(int pageId, List<(Rect2 OldRect, Rect2 NewRect)> moves)
    {
        var page = _pages[pageId];
        if (!EnsurePageResources(pageId))
            return;

        byte[] sourceSnapshot = page.Pixels.ToArray();
        byte[] finalPixels = sourceSnapshot.ToArray();

        // Build the final page from one stable snapshot so overlapping moves are safe.
        foreach (var move in moves)
            RgbaPixelBuffer.ClearRegion(finalPixels, _pageSize, _pageSize, move.OldRect);

        foreach (var move in moves)
        {
            if (RgbaPixelBuffer.TryCopyRegion(
                    sourceSnapshot,
                    _pageSize,
                    _pageSize,
                    move.OldRect,
                    out byte[] region,
                    out int width,
                    out int height))
            {
                RgbaPixelBuffer.Blit(
                    region,
                    width,
                    height,
                    finalPixels,
                    _pageSize,
                    _pageSize,
                    (int)move.NewRect.X,
                    (int)move.NewRect.Y);
            }

            _pendingDefragMoves.Enqueue(new PendingDefragMove(pageId, move.OldRect, move.NewRect));
        }

        page.Pixels = finalPixels;
        _pagesWithPendingMoves.Add(pageId);
        _isDefragging = true;

        Logger.Instance.DebugWithCategory("Atlas",
            "Queued {0} renderer atlas defrag moves for page {1} (total pending: {2})",
            moves.Count, pageId, _pendingDefragMoves.Count);
    }

    private bool EvictAndRepack(
        (uint TextureId, Rect2 SrcRect) key,
        byte[] pixels,
        int width,
        int height,
        out Rect2 packedRect,
        out int pageId)
    {
        packedRect = default;
        pageId = -1;

        if (GetTotalUsedPercentage() < 0.8f || _lruList.Count == 0)
            return false;

        LinkedListNode<(uint TextureId, Rect2 SrcRect)> node = _lruList.Last;
        while (node != null)
        {
            var previous = node.Previous;
            if (_packedMap.TryGetValue(node.Value, out var slot) &&
                !_pagesWithPendingMoves.Contains(slot.PageId))
            {
                var page = _pages[slot.PageId];
                if (!EnsurePageResources(slot.PageId))
                    return false;

                RgbaPixelBuffer.ClearRegion(page.Pixels, _pageSize, _pageSize, slot.PackedRect);
                UploadRegion(page, slot.PackedRect);

                page.Packer.Free(slot.PackedRect);
                _packedMap.Remove(node.Value);
                _lruList.Remove(node);
                _evictionCount++;

                if (page.Packer.TryPack(width, height, out var rect))
                {
                    bool packed = PackIntoPage(
                        pixels,
                        width,
                        height,
                        key,
                        slot.PageId,
                        rect,
                        out packedRect,
                        out pageId);

                    if (packed)
                    {
                        Logger.Instance.DebugWithCategory("Atlas",
                            "Evicted texture from renderer page {0} to make room (total evictions: {1})",
                            slot.PageId, _evictionCount);
                    }

                    return packed;
                }
            }

            node = previous;
        }

        return false;
    }

    public Texture GetPageTexture(int pageId)
    {
        if (pageId < 0 || pageId >= _pages.Count)
            return null;

        var page = _pages[pageId];
        if (!page.IsActive || _pagesWithPendingMoves.Contains(pageId))
            return null;

        if (!EnsurePageResources(pageId))
            return null;

        return page.Texture;
    }

    private bool EnsurePageResources(int pageId)
    {
        if (pageId < 0 || pageId >= _pages.Count)
            return false;
        if (!RendererRuntime.TryGetDevice(out IGraphicsDevice device))
            return false;

        var page = _pages[pageId];

        if (page.GraphicsTexture != null && !ReferenceEquals(page.GraphicsDevice, device))
            page.DisposeGraphics();

        page.Pixels ??= new byte[checked(_pageSize * _pageSize * 4)];

        if (page.GraphicsTexture == null)
        {
            var description = new TextureDescription(
                _pageSize,
                _pageSize,
                TextureFormat.RGBA8,
                TextureUsage.Sampled | TextureUsage.TransferDestination,
                TextureFilter.Nearest,
                TextureFilter.Nearest,
                TextureWrap.ClampToEdge,
                TextureWrap.ClampToEdge);

            page.GraphicsDevice = device;
            page.GraphicsTexture = device.CreateTexture(description, page.Pixels);
            page.Texture = new Texture(device, page.GraphicsTexture, AssetType.Atlas);
        }

        page.IsActive = true;
        return page.GraphicsTexture.IsValid;
    }

    private void UploadRegion(AtlasPage page, Rect2 rect)
    {
        if (!RgbaPixelBuffer.TryCopyRegion(
                page.Pixels,
                _pageSize,
                _pageSize,
                rect,
                out byte[] region,
                out int width,
                out int height))
        {
            return;
        }

        page.GraphicsDevice.UpdateTexture(
            page.GraphicsTexture,
            (int)rect.X,
            (int)rect.Y,
            width,
            height,
            TextureFormat.RGBA8,
            region);
    }

    private float GetTotalUsedPercentage()
    {
        int totalSpace = 0;
        int usedSpace = 0;

        foreach (var page in _pages)
        {
            if (!page.IsActive)
                continue;

            totalSpace += page.Packer.TotalSpace;
            usedSpace += page.Packer.UsedSpace;
        }

        return totalSpace == 0 ? 0f : (float)usedSpace / totalSpace;
    }

    public AtlasMetrics GetMetrics()
    {
        int totalSpace = 0;
        int usedSpace = 0;
        int activePages = 0;

        foreach (var page in _pages)
        {
            if (!page.IsActive)
                continue;

            activePages++;
            totalSpace += page.Packer.TotalSpace;
            usedSpace += page.Packer.UsedSpace;
        }

        return new AtlasMetrics
        {
            TotalPages = _pages.Count,
            UsedPages = activePages,
            TotalSpaceBytes = totalSpace,
            UsedSpaceBytes = usedSpace,
            PercentageFull = totalSpace > 0 ? (float)usedSpace / totalSpace * 100f : 0f,
            TextureCount = _packedMap.Count,
            EvictionCount = _evictionCount
        };
    }

    public void Clear()
    {
        Logger.Instance.InfoWithCategory("Atlas", "Clearing renderer atlas: {0} textures, {1} pages",
            _packedMap.Count, _pages.Count);

        _packedMap.Clear();
        _lruList.Clear();
        _pendingDefragMoves.Clear();
        _pagesWithPendingMoves.Clear();
        _usageBatch = 0;
        _isDefragging = false;

        foreach (var page in _pages)
        {
            page.Packer.Clear();
            page.DisposeGraphics();
            page.Pixels = null;
            page.IsActive = false;
        }

        _evictionCount = 0;
    }
}
