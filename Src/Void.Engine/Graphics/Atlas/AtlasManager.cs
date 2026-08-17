using Void.Engine.Logs;

namespace Void.Engine.Graphics.Atlas;

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
            RenderTexture = new SFRenderTexture((uint)width, (uint)height);
            Texture = new Texture(RenderTexture);
            Packer = packer;
            IsActive = false;
        }
    }

    private readonly Dictionary<(uint NativeHandle, Rect2 SrcRect), AtlasSlot> _packedMap;
    private readonly List<AtlasPage> _pages;
    private readonly LinkedList<(uint NativeHandle, Rect2 SrcRect)> _lruList;
    private int _pageSize;
    private int _pageCount;
    private int _evictionCount;

    public static AtlasManager Instance { get; private set; }


    internal AtlasManager()
    {
        Instance ??= this;

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
        var clearImage = new SFImage((uint)rect.Width, (uint)rect.Height, Color.Transparent);
        renderTexture.Texture.Update(clearImage, (uint)rect.Left, (uint)rect.Top);
    }

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
                (int)srcRect.Left,
                (int)srcRect.Top,
                (int)srcRect.Width,
                (int)srcRect.Height
            ),
            Position = new SFVector2f(destination.X, destination.Y)
        };

        target.Draw(sprite);
        target.Display();

        sprite.Dispose();
    }
}
