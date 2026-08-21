// ============================================================================
//  AtlasManager.cs
// ============================================================================
//  Manages texture atlasing with automatic page allocation, texture packing,
//  LRU eviction, and fragmentation management. Provides a unified interface
//  for packing textures into atlas pages for efficient batching.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Graphics.Atlas;

/// <summary>
/// Manages texture atlasing with automatic page allocation, texture packing,
/// LRU eviction, and fragmentation management.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="AtlasManager"/> provides a unified system for packing
/// textures into atlas pages to reduce draw calls and improve rendering
/// performance. It handles:
/// <list type="bullet">
///   <item><description>Automatic page allocation and management</description></item>
///   <item><description>Texture packing using configurable algorithms (Guillotine, Skyline)</description></item>
///   <item><description>LRU-based eviction when atlas is full</description></item>
///   <item><description>Automatic defragmentation when fragmentation exceeds threshold</description></item>
///   <item><description>Metrics for monitoring atlas usage</description></item>
/// </list>
/// </para>
/// <para>
/// <b>How It Works:</b>
/// <list type="number">
///   <item><description>Textures are packed into atlas pages (default: 2048x2048)</description></item>
///   <item><description>Each page has a packer that manages free space</description></item>
///   <item><description>If a page is full, the next page is used</description></item>
///   <item><description>If all pages are full, LRU eviction frees space</description></item>
///   <item><description>Fragmentation is monitored and defragmented automatically</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Get the singleton instance
/// var atlas = AtlasManager.Instance;
/// 
/// // Pack a texture
/// if (atlas.TryPack(sfTexture, srcRect, out var packedRect, out var pageId))
/// {
///     // Texture was packed at packedRect on page pageId
///     var pageTexture = atlas.GetPageTexture(pageId);
/// }
/// 
/// // Get atlas metrics
/// var metrics = atlas.GetMetrics();
/// Console.WriteLine($"Atlas usage: {metrics.PercentageFull:F1}%");
/// 
/// // Clear all atlas data
/// atlas.Clear();
/// </code>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// This class is not thread-safe and should be accessed from the main thread.
/// </para>
/// </remarks>
public sealed class AtlasManager
{
    private struct AtlasSlot
    {
        public int PageId;
        public Rect2 PackedRect;
        public LinkedListNode<(uint, Rect2)> LruNode;
    }

    private sealed class AtlasPage
    {
        public SFRenderTexture RenderTexture;
        public Texture Texture;
        public IAtlasPacker Packer;
        public bool IsActive;

        public AtlasPage(int width, int height, IAtlasPacker packer)
        {
            RenderTexture = new SFRenderTexture(new((uint)width, (uint)height));
            Texture = new Texture(RenderTexture);
            Packer = packer;
            IsActive = false;
        }
    }

    private static readonly Lazy<AtlasManager> _instance = new(() => new AtlasManager());
    private readonly Dictionary<(uint NativeHandle, Rect2 SrcRect), AtlasSlot> _packedMap;
    private readonly List<AtlasPage> _pages;
    private readonly LinkedList<(uint NativeHandle, Rect2 SrcRect)> _lruList;
    private int _pageSize;
    private int _pageCount;
    private int _evictionCount;


    /// <summary>
    /// Gets the singleton instance of the atlas manager.
    /// </summary>
    public static AtlasManager Instance => _instance.Value;


    private AtlasManager()
    {
        _packedMap = new Dictionary<(uint, Rect2), AtlasSlot>();
        _pages = new List<AtlasPage>();
        _lruList = new LinkedList<(uint, Rect2)>();
        _evictionCount = 0;

        Initialize();
    }

    private void Initialize()
    {
        var settings = GameSettings.Instance;
        _pageSize = settings.AtlasPageSize;
        _pageCount = settings.AtlasPageCount;

        Logger.Instance.InfoWithCategory("Atlas", "Initializing atlas: {0} pages of {1}x{1}",
            _pageCount, _pageSize);

        for (int i = 0; i < _pageCount; i++)
        {
            var pagePacker = settings.AtlasPacker != null
                ? (IAtlasPacker)Activator.CreateInstance(settings.AtlasPacker.GetType(), [_pageSize, _pageSize])
                : new SkylinePacker(_pageSize, _pageSize);

            _pages.Add(new AtlasPage(_pageSize, _pageSize, pagePacker));
        }
    }

    /// <summary>
    /// Attempts to pack a texture into the atlas.
    /// </summary>
    /// <param name="texture">The SFML texture to pack.</param>
    /// <param name="srcRect">The source rectangle within the texture to pack.</param>
    /// <param name="packedRect">When this method returns, contains the packed position and size if successful; otherwise, <see langword="default"/>.</param>
    /// <param name="pageId">When this method returns, contains the page index if successful; otherwise, -1.</param>
    /// <returns><see langword="true"/> if the texture was successfully packed; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// <para>
    /// This method attempts to pack the specified texture region into the atlas.
    /// If the texture is already packed, it returns the existing packed position.
    /// </para>
    /// <para>
    /// The packing process:
    /// <list type="number">
    ///   <item><description>Checks if the texture is already packed (cache hit)</description></item>
    ///   <item><description>Searches for free space in existing pages</description></item>
    ///   <item><description>Defragments pages if fragmentation exceeds threshold</description></item>
    ///   <item><description>Evicts least recently used textures if all pages are full</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// If successful, the <paramref name="packedRect"/> contains the position
    /// (X, Y) and size where the texture should be used in the atlas page.
    /// </para>
    /// </remarks>
    public bool TryPack(SFTexture texture, Rect2 srcRect, out Rect2 packedRect, out int pageId)
    {
        packedRect = default;
        pageId = -1;

        if (srcRect.Width > _pageSize || srcRect.Height > _pageSize)
        {
            Logger.Instance.WarningWithCategory("Atlas",
                "Texture {0}x{1} exceeds page size {2}x{2}",
                srcRect.Width, srcRect.Height, _pageSize);
            return false;
        }

        if (texture == null || texture.IsInvalid)
            return false;

        var key = (texture.NativeHandle, srcRect);

        if (_packedMap.TryGetValue(key, out var slot))
        {
            packedRect = slot.PackedRect;
            pageId = slot.PageId;

            _lruList.Remove(slot.LruNode);
            _lruList.AddFirst(slot.LruNode);

            return true;
        }

        int width = (int)srcRect.Width;
        int height = (int)srcRect.Height;

        for (int i = 0; i < _pages.Count; i++)
        {
            var page = _pages[i];
            if (!page.IsActive)
            {
                page.IsActive = true;
            }

            if (page.Packer.TryPack(width, height, out var rect))
            {
                CopyTo(page.RenderTexture, texture, srcRect, new Vect2(rect.Left, rect.Top));

                var lruNode = _lruList.AddFirst(key);

                _packedMap[key] = new AtlasSlot
                {
                    PageId = i,
                    PackedRect = rect,
                    LruNode = lruNode
                };

                Logger.Instance.DebugWithCategory("Atlas", "Packed {0}x{1} into page {2} (total: {3})",
                    width, height, i, _packedMap.Count);

                packedRect = rect;
                pageId = i;

                return true;
            }

            if (page.Packer.Fragmentation > GameSettings.Instance.AtlasDefragThreshold)
            {
                page.Packer.Defrag();

                if (page.Packer.TryPack(width, height, out rect))
                {
                    CopyTo(page.RenderTexture, texture, srcRect, new Vect2(rect.Left, rect.Top));

                    var lruNode = _lruList.AddFirst(key);

                    _packedMap[key] = new AtlasSlot
                    {
                        PageId = i,
                        PackedRect = rect,
                        LruNode = lruNode
                    };

                    packedRect = rect;
                    pageId = i;

                    return true;
                }
            }
        }

        if (EvictAndRepack(key, width, height, out packedRect, out pageId))
            return true;

        Logger.Instance.WarningWithCategory("Atlas",
            "Failed to pack texture {0}x{1} - atlas full or too fragmented",
            width, height);

        return false;
    }

    private bool EvictAndRepack((uint, Rect2) key, int width, int height, out Rect2 packedRect, out int pageId)
    {
        packedRect = default;
        pageId = -1;

        float totalUsed = GetTotalUsedPercentage();
        if (totalUsed < 0.8f)
            return false;

        if (_lruList.Count == 0)
            return false;

        var lruKey = _lruList.Last.Value;

        if (_packedMap.TryGetValue(lruKey, out var slot))
        {
            var page = _pages[slot.PageId];

            ClearAtlasArea(page.RenderTexture, slot.PackedRect);

            page.Packer.Free(slot.PackedRect);
            _packedMap.Remove(lruKey);
            _lruList.RemoveLast();
            _evictionCount++;

            // try to pack the new texture in the freed space
            if (page.Packer.TryPack(width, height, out var rect))
            {
                var lruNode = _lruList.AddFirst(key);

                _packedMap[key] = new AtlasSlot
                {
                    PageId = slot.PageId,
                    PackedRect = rect,
                    LruNode = lruNode
                };

                packedRect = rect;
                pageId = slot.PageId;

                Logger.Instance.DebugWithCategory("Atlas",
                    "Evicted texture from page {0} to make room (total evictions: {1})",
                    slot.PageId, _evictionCount);
                return true;
            }
        }

        return false;
    }

    private void ClearAtlasArea(SFRenderTexture renderTexture, Rect2 rect)
    {
        var clearImage = new SFImage(new((uint)rect.Width, (uint)rect.Height), Color.Transparent);
        renderTexture.Texture.Update(clearImage, new((uint)rect.Left, (uint)rect.Top));
    }

    /// <summary>
    /// Gets the texture for a specific atlas page.
    /// </summary>
    /// <param name="pageId">The page index.</param>
    /// <returns>The atlas page texture, or <see langword="null"/> if the page is invalid or inactive.</returns>
    /// <remarks>
    /// <para>
    /// This method returns the texture for the specified atlas page. The texture
    /// contains all packed textures arranged within the page.
    /// </para>
    /// <para>
    /// This is used by the renderer to draw sprites from the atlas.
    /// </para>
    /// </remarks>
    public SFTexture GetPageTexture(int pageId)
    {
        if (pageId < 0 || pageId >= _pages.Count)
            return null;

        var page = _pages[pageId];
        if (!page.IsActive)
            return null;

        return page.Texture;
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

        if (usedSpace == 0)
            return 0f;

        return (float)usedSpace / totalSpace;
    }

    /// <summary>
    /// Gets metrics about the current atlas usage.
    /// </summary>
    /// <returns>An <see cref="AtlasMetrics"/> structure containing usage statistics.</returns>
    /// <remarks>
    /// <para>
    /// This method provides detailed metrics about the atlas state including:
    /// <list type="bullet">
    ///   <item><description>Total and used pages</description></item>
    ///   <item><description>Total and used space in bytes</description></item>
    ///   <item><description>Percentage full</description></item>
    ///   <item><description>Number of packed textures</description></item>
    ///   <item><description>Number of evictions performed</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// These metrics are useful for monitoring atlas efficiency and
    /// diagnosing performance issues.
    /// </para>
    /// </remarks>
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

    /// <summary>
    /// Clears all atlas pages and resets the atlas manager to its initial state.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method removes all packed textures, clears all pages, and resets
    /// the eviction counter. All atlas textures are disposed and will need
    /// to be repacked when used again.
    /// </para>
    /// <para>
    /// This is useful when reloading assets or switching scenes.
    /// </para>
    /// </remarks>
    public void Clear()
    {
        Logger.Instance.InfoWithCategory("Atlas", "Clearing atlas: {0} textures, {1} pages",
            _packedMap.Count, _pages.Count);

        _packedMap.Clear();
        _lruList.Clear();

        foreach (var page in _pages)
        {
            page.Packer.Clear();
            page.Texture?.Dispose();
            page.RenderTexture?.Dispose();
            page.IsActive = false;
        }

        _evictionCount = 0;
    }

    private void CopyTo(SFRenderTexture target, SFTexture texture, Rect2 srcRect, Vect2 destination)
    {
        if (texture == null || texture.IsInvalid || target == null)
            return;

        var sprite = new SFSprite(texture)
        {
            TextureRect = new SFIntRect(
                new((int)srcRect.Left, (int)srcRect.Top),
                new((int)srcRect.Width, (int)srcRect.Height)
            ),
            Position = new SFVector2f(destination.X, destination.Y)
        };

        target.Draw(sprite);
        target.Display();

        sprite.Dispose();
    }
}
